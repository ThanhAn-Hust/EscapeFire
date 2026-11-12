using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EscapeFire.Generation
{
    /// <summary>
    /// Turns a <see cref="ScenarioSpec"/> into a concrete floor plan (plan V4.4) and checks it — the Tier-2 "always an
    /// exit" rule on the real layout. Room coordinates: centre at (0,0), x east, z north, walls at ±W/2, ±D/2.
    /// - A 1.5 m approach in front of every exit stays empty; extinguishers hang on the wall next to the first exit.
    /// - Wall items go along the walls, free-standing items keep 0.9 m aisles, chairs go next to desks / tables.
    /// - Walkability: a 0.25 m grid with obstacles grown by a 0.3 m body radius; from the exits every cell next to the
    ///   fire, every extinguisher, every NPC and the start must be reachable, and >= 90 % of the free floor. Otherwise
    ///   free-standing items are removed one by one (never the fire source) until it passes.
    /// Pure logic (no UnityEngine), deterministic for a seed.
    /// </summary>
    public sealed class LayoutSolver
    {
        public const float Cell = 0.25f;
        public const float DoorWidth = 1.0f;
        public const float BodyRadius = 0.3f;
        public const float Approach = 1.5f;
        public const float MinReachable = 0.9f;

        public struct Item { public string Category; public float X, Z, Yaw, W, D; public bool IsFire; public bool Wall; }
        public struct Spot { public string Kind; public float X, Z, Yaw; }

        public sealed class Layout
        {
            public bool Ok;
            public string Reason = "";
            public float Width, Depth;
            public readonly List<Item> Items = new List<Item>();
            public readonly List<Spot> Exits = new List<Spot>();          // Kind = wall; X/Z = door centre on the wall line
            public readonly List<Spot> Extinguishers = new List<Spot>();  // Kind = type
            public readonly List<Spot> Npcs = new List<Spot>();           // Kind = role
            public Spot Start;
            public float ReachableFraction;
            public int Removed;
        }

        private readonly ScenarioSpec _spec;
        private readonly JObject _cats;
        private readonly Random _rng;
        private int _nx, _nz;
        private float _w, _d;
        private bool[,] _blocked;   // furniture footprints
        private bool[,] _reserved;  // exit approaches, extinguisher access

        public LayoutSolver(ScenarioSpec spec, int seed)
        {
            _spec = spec;
            _cats = (JObject)V4Catalog.Scenario["categories"];
            _rng = new Random(seed);
        }

        public Layout Solve()
        {
            var L = new Layout { Width = _spec.Width, Depth = _spec.Depth };
            _w = _spec.Width; _d = _spec.Depth;
            _nx = (int)Math.Round(_w / Cell); _nz = (int)Math.Round(_d / Cell);
            _blocked = new bool[_nx, _nz];
            _reserved = new bool[_nx, _nz];

            foreach (var e in _spec.Exits) L.Exits.Add(ExitSpot(e));
            foreach (var e in L.Exits) Reserve(Inward(e, Approach * 0.5f), (e.Kind == "north" || e.Kind == "south") ? DoorWidth + 0.4f : Approach,
                                                                         (e.Kind == "north" || e.Kind == "south") ? Approach : DoorWidth + 0.4f);
            var first = L.Exits[0];
            var inward = Inward(first, 1.0f);
            L.Start = new Spot { Kind = "start", X = inward.X, Z = inward.Z, Yaw = YawInward(first.Kind) };

            // extinguishers on the wall beside the first exit (alternating sides, 0.9 m from the door centre)
            int side = 1, k = 0;
            foreach (var x in _spec.Extinguishers)
                for (int i = 0; i < x.Count; i++, k++)
                {
                    var s = AlongWall(first, side * (0.9f + 0.6f * (k / 2)));
                    side = -side;
                    var spot = new Spot { Kind = x.Type, X = s.X, Z = s.Z, Yaw = YawInward(first.Kind) };
                    L.Extinguishers.Add(spot);
                    var access = Inward(new Spot { Kind = first.Kind, X = s.X, Z = s.Z }, 0.5f);
                    Reserve(access, 0.8f, 0.8f);
                }

            // the fire source goes first, then wall items, free items, chairs
            var queue = new List<string>();
            bool hasSource = false;
            foreach (var f in _spec.Items)
                for (int i = 0; i < f.Count; i++)
                {
                    queue.Add(f.Category);
                    if (f.Category == _spec.FireSource) hasSource = true;
                }
            if (!hasSource) queue.Add(_spec.FireSource);
            queue.Remove(_spec.FireSource);
            queue.Insert(0, _spec.FireSource);
            queue.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
            bool fireDone = false;
            foreach (var cat in queue)
            {
                bool isFire = !fireDone && cat == _spec.FireSource;
                if (TryPlace(cat, isFire, L, out var item))
                {
                    L.Items.Add(item);
                    if (isFire) fireDone = true;
                }
            }
            if (!fireDone) return Fail(L, "fire_source_does_not_fit");

            // NPCs: weak / unconscious 1.5-3.5 m from the fire, panic near the middle
            var fire = L.Items.Find(i => i.IsFire);
            foreach (var role in _spec.Npcs)
            {
                Spot npc = default;
                bool placed = false;
                for (int tries = 0; tries < 6 && !placed; tries++)
                {
                    placed = PlaceNpc(role, fire, L, 1.5f, 3.5f, out npc) || PlaceNpc(role, fire, L, 1.0f, 6f, out npc) || PlaceNpc(role, fire, L, 0.8f, 99f, out npc);
                    if (placed) break;
                    int idx = L.Items.FindLastIndex(i => !i.IsFire && !i.Wall); // make room: drop a free-standing item
                    if (idx < 0) break;
                    Unmark(L.Items[idx]);
                    L.Items.RemoveAt(idx);
                    L.Removed++;
                }
                if (!placed) return Fail(L, "npc_does_not_fit:" + role);
                L.Npcs.Add(npc);
            }

            // reachability; drop free-standing items until every key place can reach an exit
            for (int attempt = 0; attempt < 12; attempt++)
            {
                string why = CheckReach(L);
                if (why == null) { L.Ok = true; return L; }
                int idx = L.Items.FindLastIndex(i => !i.IsFire && !i.Wall);
                if (idx < 0) idx = L.Items.FindLastIndex(i => !i.IsFire);
                if (idx < 0) return Fail(L, why);
                Unmark(L.Items[idx]);
                L.Items.RemoveAt(idx);
                L.Removed++;
            }
            return Fail(L, CheckReach(L) ?? "unreachable");
        }

        private static Layout Fail(Layout L, string why) { L.Ok = false; L.Reason = why; return L; }

        private int Rank(string cat)
        {
            if (cat == _spec.FireSource) return 0;
            if (cat == "chair") return 3;
            return (bool)_cats[cat]["wall"] ? 1 : 2;
        }

        // ---------------------------------------------------------------------------------------------- geometry

        private Spot ExitSpot(ScenarioSpec.Exit e)
        {
            switch (e.Wall)
            {
                case "north": return new Spot { Kind = e.Wall, X = -_w / 2 + e.Position * _w, Z = _d / 2 };
                case "south": return new Spot { Kind = e.Wall, X = -_w / 2 + e.Position * _w, Z = -_d / 2 };
                case "east": return new Spot { Kind = e.Wall, X = _w / 2, Z = -_d / 2 + e.Position * _d };
                default: return new Spot { Kind = e.Wall, X = -_w / 2, Z = -_d / 2 + e.Position * _d };
            }
        }

        public static float YawInward(string wall)
        {
            switch (wall) { case "north": return 180f; case "south": return 0f; case "east": return 270f; default: return 90f; }
        }

        private static (float X, float Z) Normal(string wall)
        {
            switch (wall) { case "north": return (0, -1); case "south": return (0, 1); case "east": return (-1, 0); default: return (1, 0); }
        }

        private static Spot Inward(Spot onWall, float distance)
        {
            var n = Normal(onWall.Kind);
            return new Spot { Kind = onWall.Kind, X = onWall.X + n.X * distance, Z = onWall.Z + n.Z * distance };
        }

        private static Spot AlongWall(Spot onWall, float offset)
        {
            bool ns = onWall.Kind == "north" || onWall.Kind == "south";
            return new Spot { Kind = onWall.Kind, X = onWall.X + (ns ? offset : 0), Z = onWall.Z + (ns ? 0 : offset) };
        }

        private int Ix(float x) => Math.Max(0, Math.Min(_nx - 1, (int)Math.Floor((x + _w / 2) / Cell)));
        private int Iz(float z) => Math.Max(0, Math.Min(_nz - 1, (int)Math.Floor((z + _d / 2) / Cell)));
        private float Cx(int i) => -_w / 2 + (i + 0.5f) * Cell;
        private float Cz(int j) => -_d / 2 + (j + 0.5f) * Cell;

        private void Reserve(Spot centre, float sx, float sz)
        {
            for (int i = Ix(centre.X - sx / 2); i <= Ix(centre.X + sx / 2 - 0.001f); i++)
                for (int j = Iz(centre.Z - sz / 2); j <= Iz(centre.Z + sz / 2 - 0.001f); j++) _reserved[i, j] = true;
        }

        /// <summary>True if the rectangle (plus margin) is inside the room and free of furniture and reserved cells.</summary>
        private bool Free(float x, float z, float sx, float sz, float margin, bool allowReservedMargin = false)
        {
            float x0 = x - sx / 2, x1 = x + sx / 2, z0 = z - sz / 2, z1 = z + sz / 2;
            if (x0 < -_w / 2 + 0.05f || x1 > _w / 2 - 0.05f || z0 < -_d / 2 + 0.05f || z1 > _d / 2 - 0.05f) return false;
            for (int i = Ix(x0 - margin); i <= Ix(x1 + margin - 0.001f); i++)
                for (int j = Iz(z0 - margin); j <= Iz(z1 + margin - 0.001f); j++)
                {
                    bool inner = Cx(i) >= x0 && Cx(i) <= x1 && Cz(j) >= z0 && Cz(j) <= z1;
                    if (_blocked[i, j]) return false;
                    if (_reserved[i, j] && (inner || !allowReservedMargin)) return false;
                }
            return true;
        }

        private void Mark(Item it, bool value)
        {
            bool rotated = Math.Abs(it.Yaw % 180f) > 1f;
            float sx = rotated ? it.D : it.W, sz = rotated ? it.W : it.D;
            for (int i = Ix(it.X - sx / 2); i <= Ix(it.X + sx / 2 - 0.001f); i++)
                for (int j = Iz(it.Z - sz / 2); j <= Iz(it.Z + sz / 2 - 0.001f); j++) _blocked[i, j] = value;
        }

        private void Unmark(Item it) => Mark(it, false);

        // ---------------------------------------------------------------------------------------------- placement

        private bool TryPlace(string cat, bool isFire, Layout L, out Item item)
        {
            var c = _cats[cat];
            float w = (float)c["w"], d = (float)c["d"];
            bool wall = (bool)c["wall"];
            var candidates = new List<Item>();
            if (wall)
            {
                foreach (var side in new[] { "north", "south", "east", "west" })
                {
                    bool ns = side == "north" || side == "south";
                    float len = ns ? _w : _d;
                    for (float t = -len / 2 + w / 2 + 0.3f; t <= len / 2 - w / 2 - 0.3f; t += Cell)
                    {
                        var n = Normal(side);
                        float off = (ns ? _d : _w) / 2 - d / 2 - 0.06f;
                        float x = ns ? t : -n.X * off, z = ns ? -n.Z * off : t;
                        float yaw = YawInward(side);
                        bool rot = Math.Abs(yaw % 180f) > 1f;
                        if (Free(x, z, rot ? d : w, rot ? w : d, 0.05f))
                            candidates.Add(new Item { Category = cat, X = x, Z = z, Yaw = yaw, W = w, D = d, IsFire = isFire, Wall = true });
                    }
                }
            }
            else
            {
                float margin = cat == "chair" ? 0.1f : 0.45f;
                for (float x = -_w / 2 + 0.5f; x <= _w / 2 - 0.5f; x += Cell)
                    for (float z = -_d / 2 + 0.5f; z <= _d / 2 - 0.5f; z += Cell)
                        foreach (float yaw in new[] { 0f, 90f })
                        {
                            bool rot = yaw > 1f;
                            if (!Free(x, z, rot ? d : w, rot ? w : d, margin)) continue;
                            if (cat == "chair" && !NearTable(L, x, z)) continue;
                            candidates.Add(new Item { Category = cat, X = x, Z = z, Yaw = yaw, W = w, D = d, IsFire = isFire, Wall = false });
                        }
                if (cat == "chair" && candidates.Count == 0) // no tables: free chairs anywhere
                    for (float x = -_w / 2 + 0.5f; x <= _w / 2 - 0.5f; x += Cell)
                        for (float z = -_d / 2 + 0.5f; z <= _d / 2 - 0.5f; z += Cell)
                            if (Free(x, z, w, d, 0.3f)) candidates.Add(new Item { Category = cat, X = x, Z = z, Yaw = 0f, W = w, D = d });
            }
            item = default;
            if (candidates.Count == 0) return false;
            item = candidates[_rng.Next(candidates.Count)];
            if (cat == "chair") item.Yaw = FaceNearestTable(L, item);
            Mark(item, true);
            return true;
        }

        private static bool IsTable(string cat) => cat == "desk" || cat == "computer_desk" || cat == "table" || cat == "round_table" || cat == "conference_table" || cat == "drafting_table";

        private static bool NearTable(Layout L, float x, float z)
        {
            foreach (var it in L.Items)
                if (IsTable(it.Category) && Math.Abs(it.X - x) <= Math.Max(it.W, it.D) / 2 + 0.6f && Math.Abs(it.Z - z) <= Math.Max(it.W, it.D) / 2 + 0.6f)
                    return true;
            return false;
        }

        private static float FaceNearestTable(Layout L, Item chair)
        {
            float best = float.MaxValue, yaw = chair.Yaw;
            foreach (var it in L.Items)
            {
                if (!IsTable(it.Category)) continue;
                float dx = it.X - chair.X, dz = it.Z - chair.Z, dist = dx * dx + dz * dz;
                if (dist < best) { best = dist; yaw = (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI); }
            }
            return yaw;
        }

        private bool PlaceNpc(string role, Item fire, Layout L, float minDist, float maxDist, out Spot npc)
        {
            float len = role == "unconscious" ? 1.9f : 0.6f;
            var options = new List<Spot>();
            for (float x = -_w / 2 + 0.6f; x <= _w / 2 - 0.6f; x += Cell)
                for (float z = -_d / 2 + 0.6f; z <= _d / 2 - 0.6f; z += Cell)
                {
                    float dist = (float)Math.Sqrt((x - fire.X) * (x - fire.X) + (z - fire.Z) * (z - fire.Z));
                    bool ok = role == "panic" && maxDist < 10f ? Math.Abs(x) < _w / 4 && Math.Abs(z) < _d / 4 : dist >= minDist && dist <= maxDist;
                    if (!ok || !Free(x, z, role == "unconscious" ? len : 0.6f, 0.6f, 0.2f)) continue;
                    options.Add(new Spot { Kind = role, X = x, Z = z, Yaw = 90f });
                }
            npc = default;
            if (options.Count == 0) return false;
            npc = options[_rng.Next(options.Count)];
            Reserve(npc, role == "unconscious" ? len : 0.6f, 0.6f);
            return true;
        }

        // ---------------------------------------------------------------------------------------------- reachability

        private string CheckReach(Layout L)
        {
            int r = (int)Math.Ceiling(BodyRadius / Cell);
            var walk = new bool[_nx, _nz];
            for (int i = 0; i < _nx; i++)
                for (int j = 0; j < _nz; j++)
                {
                    bool ok = i >= r - 1 && j >= r - 1 && i <= _nx - r && j <= _nz - r; // body cannot touch the walls
                    for (int a = -r + 1; ok && a <= r - 1; a++)
                        for (int b = -r + 1; ok && b <= r - 1; b++)
                        {
                            int ii = i + a, jj = j + b;
                            if (ii >= 0 && jj >= 0 && ii < _nx && jj < _nz && _blocked[ii, jj]) ok = false;
                        }
                    walk[i, j] = ok;
                }
            var seen = new bool[_nx, _nz];
            var q = new Queue<(int, int)>();
            foreach (var e in L.Exits)
            {
                var inside = Inward(e, 0.4f);
                int i = Ix(inside.X), j = Iz(inside.Z);
                if (walk[i, j] && !seen[i, j]) { seen[i, j] = true; q.Enqueue((i, j)); }
            }
            if (q.Count == 0) return "exit_blocked";
            while (q.Count > 0)
            {
                var (i, j) = q.Dequeue();
                foreach (var (a, b) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int ii = i + a, jj = j + b;
                    if (ii < 0 || jj < 0 || ii >= _nx || jj >= _nz || seen[ii, jj] || !walk[ii, jj]) continue;
                    seen[ii, jj] = true;
                    q.Enqueue((ii, jj));
                }
            }
            int free = 0, reached = 0;
            for (int i = 0; i < _nx; i++)
                for (int j = 0; j < _nz; j++)
                    if (walk[i, j]) { free++; if (seen[i, j]) reached++; }
            L.ReachableFraction = free == 0 ? 0f : (float)reached / free;

            if (!Near(seen, L.Start.X, L.Start.Z, 0.5f)) return "start_cut_off";
            var fire = L.Items.Find(i => i.IsFire);
            if (!Near(seen, fire.X, fire.Z, Math.Max(fire.W, fire.D) / 2 + 1.2f)) return "fire_unreachable";
            foreach (var x in L.Extinguishers)
                if (!Near(seen, Inward(new Spot { Kind = L.Exits[0].Kind, X = x.X, Z = x.Z }, 0.5f).X, Inward(new Spot { Kind = L.Exits[0].Kind, X = x.X, Z = x.Z }, 0.5f).Z, 0.6f))
                    return "extinguisher_unreachable";
            foreach (var n in L.Npcs)
                if (!Near(seen, n.X, n.Z, 1.0f)) return "npc_cut_off:" + n.Kind;
            if (L.ReachableFraction < MinReachable) return "floor_cut_off";
            return null;
        }

        private bool Near(bool[,] seen, float x, float z, float radius)
        {
            for (int i = Ix(x - radius); i <= Ix(x + radius); i++)
                for (int j = Iz(z - radius); j <= Iz(z + radius); j++)
                    if (seen[i, j]) return true;
            return false;
        }
    }
}
