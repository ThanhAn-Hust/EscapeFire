using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using EscapeFire.Hazards;
using EscapeFire.NPC;

namespace EscapeFire.Generation
{
    /// <summary>
    /// Builds a playable drill room from a <see cref="ScenarioSpec"/> at runtime (plan V4.4): shell with door openings
    /// and short exit corridors, catalog furniture fitted to its footprint, the fire on the fire source, working
    /// extinguishers beside the first exit, NPCs (an unconscious one can be Rautek-dragged), EXIT signs, a title board
    /// and the learner at the start. Uses <see cref="LayoutSolver"/> (Tier-2 path check) — a spec whose layout fails is
    /// not built.
    /// </summary>
    public class ScenarioBuilder : MonoBehaviour
    {
        public const float WallHeight = 3.0f;
        public const float DoorHeight = 2.2f;
        public const float StubDepth = 2.0f;

        [SerializeField] private ScenarioAssetLibrary library;
        [SerializeField] private string defaultSample = "library";
        [SerializeField] private GeneratedDrill drill;

        public ScenarioSpec Spec { get; private set; }
        public LayoutSolver.Layout Layout { get; private set; }
        public List<string> Errors { get; private set; } = new List<string>();
        public Transform Root { get; private set; }
        public bool Built { get; private set; }

        private System.Random _rng;
        private readonly Dictionary<Color, Material> _mats = new Dictionary<Color, Material>();

        public void Configure(ScenarioAssetLibrary lib, GeneratedDrill d) { library = lib; drill = d; }

        private void Awake()
        {
            if (library == null) library = Resources.Load<ScenarioAssetLibrary>("ScenarioAssetLibrary");
            var spec = GeneratedScenario.Pending;
            if (spec == null)
            {
                var sample = Resources.Load<TextAsset>("Scenarios/" + defaultSample);
                spec = sample != null ? JObject.Parse(sample.text) : null;
            }
            Build(spec, GeneratedScenario.Seed);
        }

        private void Start()
        {
            if (Built) PlaceLearner();
        }

        /// <summary>Validates, solves the layout and builds. False (with <see cref="Errors"/>) when anything fails.</summary>
        public bool Build(JToken specJson, int seed)
        {
            Built = false;
            if (Root != null) Destroy(Root.gameObject);
            Spec = ScenarioSpec.Parse(specJson, out var errors);
            Errors = errors;
            if (Spec == null) { Debug.LogWarning("[ScenarioBuilder] spec rejected: " + string.Join(", ", errors)); return false; }
            Layout = new LayoutSolver(Spec, seed).Solve();
            if (!Layout.Ok)
            {
                Errors = new List<string> { "layout:" + Layout.Reason };
                Debug.LogWarning("[ScenarioBuilder] layout failed: " + Layout.Reason);
                return false;
            }
            _rng = new System.Random(seed);
            Root = new GameObject("[GENERATED_ROOM]").transform;
            Shell();
            FireHazard fire = null;
            foreach (var item in Layout.Items)
            {
                var placed = Furniture(item);
                if (item.IsFire) fire = Fire(item, placed);
            }
            foreach (var x in Layout.Extinguishers) Extinguisher(x);
            var exits = new List<Bounds>();
            foreach (var e in Layout.Exits) exits.Add(ExitBounds(e));
            foreach (var n in Layout.Npcs) Npc(n, exits[0]);
            TitleBoard();
            if (drill == null) drill = GetComponent<GeneratedDrill>();
            if (drill == null) drill = gameObject.AddComponent<GeneratedDrill>();
            drill.Begin(Spec, fire, exits);
            Built = true;
            Debug.Log($"[ScenarioBuilder] '{Spec.Title}': {Layout.Items.Count} items, {Layout.Extinguishers.Count} extinguishers, {Layout.Npcs.Count} NPCs, reachable {Layout.ReachableFraction:P0}, removed {Layout.Removed}");
            return true;
        }

        /// <summary>Scenario setup: the learner starts just inside the first exit, facing into the room.</summary>
        public void PlaceLearner()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) return;
            var cc = origin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            origin.transform.SetPositionAndRotation(new Vector3(Layout.Start.X, 0f, Layout.Start.Z), Quaternion.Euler(0f, Layout.Start.Yaw, 0f));
            if (cc != null) cc.enabled = true;
        }

        // ------------------------------------------------------------------ shell

        private static readonly Dictionary<string, Color> RoomColors = new Dictionary<string, Color>
        {
            { "lab", new Color(0.78f, 0.82f, 0.86f) }, { "library", new Color(0.86f, 0.8f, 0.68f) },
            { "office", new Color(0.82f, 0.84f, 0.8f) }, { "canteen", new Color(0.9f, 0.84f, 0.74f) },
            { "workshop", new Color(0.7f, 0.72f, 0.7f) }
        };

        private void Shell()
        {
            float w = Spec.Width, d = Spec.Depth;
            Color wallC = RoomColors.TryGetValue(Spec.RoomType, out var c) ? c : Color.gray;
            Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(w + 0.4f, 0.2f, d + 0.4f), new Color(0.55f, 0.55f, 0.58f), true);
            Box("Ceiling", new Vector3(0f, WallHeight + 0.1f, 0f), new Vector3(w + 0.4f, 0.2f, d + 0.4f), new Color(0.92f, 0.92f, 0.92f), true);
            foreach (var side in new[] { "north", "south", "east", "west" })
            {
                bool ns = side == "north" || side == "south";
                float len = ns ? w : d;
                float line = side == "north" ? d / 2 + 0.1f : side == "south" ? -d / 2 - 0.1f : side == "east" ? w / 2 + 0.1f : -w / 2 - 0.1f;
                var gaps = new List<float>();
                foreach (var e in Layout.Exits) if (e.Kind == side) gaps.Add(ns ? e.X : e.Z);
                gaps.Sort();
                float from = -len / 2 - 0.2f;
                foreach (float g in gaps)
                {
                    float to = g - LayoutSolver.DoorWidth / 2;
                    WallPiece(ns, line, from, to, 0f, WallHeight, wallC);
                    WallPiece(ns, line, to, g + LayoutSolver.DoorWidth / 2, DoorHeight, WallHeight, wallC); // lintel
                    from = g + LayoutSolver.DoorWidth / 2;
                }
                WallPiece(ns, line, from, len / 2 + 0.2f, 0f, WallHeight, wallC);
            }
            for (int i = 0; i < 4; i++)
            {
                var l = new GameObject("CeilingLight_" + i).AddComponent<Light>();
                l.transform.SetParent(Root, false);
                l.type = LightType.Point;
                l.range = Mathf.Max(w, d);
                l.intensity = 1.2f;
                l.transform.position = new Vector3((i % 2 == 0 ? -1 : 1) * w / 4, WallHeight - 0.3f, (i < 2 ? -1 : 1) * d / 4);
            }
            foreach (var e in Layout.Exits) ExitStub(e);
        }

        private void WallPiece(bool ns, float line, float from, float to, float y0, float y1, Color c)
        {
            if (to - from < 0.01f) return;
            float mid = (from + to) / 2, len = to - from;
            Box("Wall", ns ? new Vector3(mid, (y0 + y1) / 2, line) : new Vector3(line, (y0 + y1) / 2, mid),
                ns ? new Vector3(len, y1 - y0, 0.2f) : new Vector3(0.2f, y1 - y0, len), c, true);
        }

        private static Vector3 Outward(string wall) =>
            wall == "north" ? Vector3.forward : wall == "south" ? Vector3.back : wall == "east" ? Vector3.right : Vector3.left;

        /// <summary>A short corridor beyond each door (the learner leaves the room through it) with an EXIT sign.</summary>
        private void ExitStub(LayoutSolver.Spot e)
        {
            Vector3 o = Outward(e.Kind);
            Vector3 side = Vector3.Cross(Vector3.up, o);
            Vector3 door = new Vector3(e.X, 0f, e.Z);
            Vector3 mid = door + o * (StubDepth / 2 + 0.2f);
            var grey = new Color(0.6f, 0.6f, 0.62f);
            Box("ExitFloor", mid + Vector3.down * 0.1f, Abs(o * (StubDepth + 0.4f) + side * 1.6f) + Vector3.up * 0.2f, grey, true);
            Box("ExitWallL", mid + side * 0.8f + Vector3.up * DoorHeight / 2, Abs(o * StubDepth + side * 0.1f) + Vector3.up * DoorHeight, grey, true);
            Box("ExitWallR", mid - side * 0.8f + Vector3.up * DoorHeight / 2, Abs(o * StubDepth + side * 0.1f) + Vector3.up * DoorHeight, grey, true);
            Box("ExitEnd", door + o * (StubDepth + 0.3f) + Vector3.up * DoorHeight / 2, Abs(o * 0.1f + side * 1.6f) + Vector3.up * DoorHeight, grey, true);
            Box("ExitCeil", mid + Vector3.up * (DoorHeight + 0.05f), Abs(o * (StubDepth + 0.4f) + side * 1.6f) + Vector3.up * 0.1f, grey, true);
            var inward = Quaternion.LookRotation(-o);
            var sign = Box("ExitSign", door - o * 0.12f + Vector3.up * (DoorHeight + 0.25f), Abs(side * 0.6f + o * 0.05f) + Vector3.up * 0.2f, new Color(0.05f, 0.75f, 0.3f), false);
            Label("EXIT", sign.transform.position - o * 0.03f, Quaternion.LookRotation(o), new Vector2(0.6f, 0.2f), 1.0f, "<b>EXIT  LỐI THOÁT</b>");
            var glow = new GameObject("ExitGlow").AddComponent<Light>();
            glow.transform.SetParent(Root, false);
            glow.transform.SetPositionAndRotation(door - o * 0.4f + Vector3.up * 2.3f, inward);
            glow.type = LightType.Point; glow.color = new Color(0.2f, 1f, 0.4f); glow.range = 2.5f; glow.intensity = 0.6f;
        }

        public static Bounds ExitBounds(LayoutSolver.Spot e)
        {
            Vector3 o = Outward(e.Kind);
            Vector3 c = new Vector3(e.X, 1.5f, e.Z) + o * (StubDepth * 0.5f + 0.4f);
            return new Bounds(c, Abs(o * (StubDepth - 0.4f) + Vector3.Cross(Vector3.up, o) * 1.6f) + Vector3.up * 3f);
        }

        // ------------------------------------------------------------------ content

        private GameObject Furniture(LayoutSolver.Item item)
        {
            var entry = library != null ? library.Find(item.Category) : null;
            Quaternion rot = Quaternion.Euler(0f, item.Yaw, 0f);
            if (entry == null || entry.prefabs.Length == 0)
                return Box(item.Category, new Vector3(item.X, 0.45f, item.Z), new Vector3(item.W, 0.9f, item.D), new Color(0.5f, 0.4f, 0.3f), true);
            var go = Instantiate(entry.prefabs[_rng.Next(entry.prefabs.Length)], Root);
            go.name = item.Category;
            Fit(go, item.W, item.D, new Vector3(item.X, 0f, item.Z), rot);
            if (entry.shelfFill != null && entry.shelfFill.Length > 0) FillShelves(go, item, entry.shelfFill);
            if (entry.toppers.Length > 0)
            {
                var top = Bounds(go);
                var t = Instantiate(entry.toppers[_rng.Next(entry.toppers.Length)], go.transform);
                t.transform.SetPositionAndRotation(new Vector3(item.X, top.max.y, item.Z), rot);
                var tb = Bounds(t);
                t.transform.position += Vector3.up * (top.max.y - tb.min.y);
            }
            return go;
        }

        /// <summary>
        /// V5.2: books on a bookshelf — finds the shelf boards by casting down inside the shelf (the model gets temporary
        /// mesh colliders) and puts two groups on each board, so nothing floats or sinks into a board.
        /// </summary>
        private void FillShelves(GameObject shelf, LayoutSolver.Item item, GameObject[] fill)
        {
            // the prefab's own collider is one box around the whole shelf: switch it off and probe the real boards
            var own = new List<Collider>();
            foreach (var c in shelf.GetComponentsInChildren<Collider>()) if (c.enabled) { c.enabled = false; own.Add(c); }
            var temp = new List<MeshCollider>();
            foreach (var mf in shelf.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                temp.Add(mc);
            }
            Physics.SyncTransforms();
            var b = Bounds(shelf);
            Quaternion rot = Quaternion.Euler(0f, item.Yaw, 0f);
            Vector3 side = rot * Vector3.right;
            float w = Mathf.Min(item.W, b.size.x > b.size.z ? b.size.x : b.size.z);
            foreach (float off in new[] { -w / 4f, w / 4f })
            {
                Vector3 start = new Vector3(b.center.x, b.max.y - 0.05f, b.center.z) + side * off;
                float y = start.y;
                for (int level = 0; level < 6; level++)
                {
                    if (!Physics.Raycast(new Vector3(start.x, y, start.z), Vector3.down, out var hit, y - b.min.y + 0.01f, ~0, QueryTriggerInteraction.Ignore)) break;
                    if (!temp.Exists(c => c == hit.collider)) break;
                    float clearance = y - hit.point.y;
                    if (clearance > 0.18f || level == 0)
                    {
                        var g = Instantiate(fill[_rng.Next(fill.Length)], shelf.transform);
                        g.transform.SetPositionAndRotation(hit.point, rot * Quaternion.Euler(0f, _rng.Next(2) * 180f, 0f));
                        var gb = Bounds(g);
                        float s = Mathf.Min(1f, (w / 2.3f) / Mathf.Max(0.05f, Mathf.Max(gb.size.x, gb.size.z)), Mathf.Max(0.1f, clearance - 0.03f) / Mathf.Max(0.05f, gb.size.y));
                        g.transform.localScale *= s;
                        gb = Bounds(g);
                        g.transform.position += Vector3.up * (hit.point.y - gb.min.y);
                    }
                    y = hit.point.y - 0.03f; // continue below this board
                }
            }
            foreach (var c in temp) Destroy(c);
            foreach (var c in own) c.enabled = true;
        }

        /// <summary>Uniform scale so the model's footprint fits w x d, bottom on the floor, centred on the spot.</summary>
        private static void Fit(GameObject go, float w, float d, Vector3 at, Quaternion rot)
        {
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var b = Bounds(go);
            float s = Mathf.Min(w / Mathf.Max(0.05f, b.size.x), d / Mathf.Max(0.05f, b.size.z));
            s = Mathf.Clamp(s, 0.4f, 2.5f);
            go.transform.localScale *= s;
            b = Bounds(go);
            go.transform.rotation = rot;
            Vector3 centre = rot * (b.center - go.transform.position);
            go.transform.position = at - new Vector3(centre.x, 0f, centre.z) + Vector3.up * (-b.min.y);
        }

        private static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        private FireHazard Fire(LayoutSolver.Item item, GameObject burning)
        {
            if (library == null || library.fire == null) return null;
            var go = Instantiate(library.fire, Root);
            go.name = "Fire_" + item.Category;
            var fire = go.GetComponent<FireHazard>();
            SecondaryFireSlot.HideEverythingButFlames(fire);
            var top = burning != null ? Bounds(burning) : new Bounds(new Vector3(item.X, 0.9f, item.Z), Vector3.one);
            go.transform.position = new Vector3(top.center.x, 0f, top.center.z);
            if (fire.Particles != null) fire.Particles.transform.position = new Vector3(top.center.x, Mathf.Max(0.3f, top.max.y - 0.2f), top.center.z);
            fire.ResetFire();
            return fire;
        }

        private void Extinguisher(LayoutSolver.Spot x)
        {
            var prefab = library == null ? null : x.Kind == "CO2" ? library.co2Extinguisher : library.waterExtinguisher;
            if (prefab == null) return;
            var wall = Layout.Exits[0].Kind;
            Vector3 inward = -Outward(wall);
            var pos = new Vector3(x.X, 0.55f, x.Z) + inward * 0.12f;
            var go = Instantiate(prefab, pos, Quaternion.LookRotation(inward), Root);
            go.name = "Extinguisher_" + x.Kind;
            // wall sign above it (TCVN 3890: visible, marked location)
            var sign = Box("ExtinguisherSign", new Vector3(x.X, 1.55f, x.Z) + inward * 0.11f, Abs(Vector3.Cross(Vector3.up, inward) * 0.32f + inward * 0.02f) + Vector3.up * 0.32f, new Color(0.8f, 0.1f, 0.08f), false);
            Label("ExtinguisherLabel", sign.transform.position + inward * 0.012f, Quaternion.LookRotation(-inward), new Vector2(0.3f, 0.3f), 0.32f, x.Kind == "CO2" ? "<b>BÌNH\nCO2</b>" : "<b>BÌNH\nNƯỚC/BỌT</b>");
        }

        private void Npc(LayoutSolver.Spot n, Bounds safe)
        {
            if (library == null || library.people.Length == 0) return;
            var prefab = library.people[_rng.Next(library.people.Length)];
            if (n.Kind == "unconscious")
            {
                var v = new GameObject("Victim").transform;
                v.SetParent(Root, false);
                v.SetPositionAndRotation(new Vector3(n.X - 0.8f, 0f, n.Z), Quaternion.Euler(0f, 90f, 0f));
                var tilt = new GameObject("Tilt").transform;
                tilt.SetParent(v, false);
                var model = Instantiate(prefab, tilt);
                FitHeight(model, 1.7f);
                model.transform.localPosition = Vector3.zero;
                RuntimePose.Stand(model);
                model.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                model.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                var chest = new GameObject("Chest").transform;
                chest.SetParent(tilt, false);
                chest.localPosition = new Vector3(0f, 0.12f, 1.38f);
                v.gameObject.AddComponent<VictimBody>().Configure(tilt, chest, safe, "nạn nhân");
                return;
            }
            var p = Instantiate(prefab, Root);
            p.name = n.Kind == "weak" ? "NPC_Weak" : "NPC_Panic";
            FitHeight(p, n.Kind == "weak" ? 1.45f : 1.7f); // the weak one is slumped / crouched
            p.transform.SetPositionAndRotation(new Vector3(n.X, 0f, n.Z), Quaternion.Euler(0f, n.Yaw, 0f));
            RuntimePose.Stand(p);
        }

        private static void FitHeight(GameObject model, float height)
        {
            model.transform.localScale = Vector3.one;
            var b = Bounds(model);
            if (b.size.y > 0.1f) model.transform.localScale *= height / b.size.y;
        }

        private void TitleBoard()
        {
            var e = Layout.Exits[0];
            Vector3 inward = -Outward(e.Kind);
            Vector3 side = Vector3.Cross(Vector3.up, inward);
            Vector3 at = new Vector3(e.X, 1.9f, e.Z) + inward * 0.12f - side * 1.9f;
            Box("TitleBoard", at, Abs(side * 1.4f + inward * 0.03f) + Vector3.up * 0.7f, new Color(0.1f, 0.2f, 0.45f), false);
            string goals = string.Join(", ", Spec.Goals);
            Label("TitleText", at + inward * 0.02f, Quaternion.LookRotation(-inward), new Vector2(1.35f, 0.65f), 0.42f,
                $"<b>{Spec.Title}</b>\n<size=75%>Cháy lớp {Spec.FireClass} — mục tiêu: {goals}</size>");
        }

        // ------------------------------------------------------------------ helpers

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        private GameObject Box(string name, Vector3 pos, Vector3 size, Color color, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(Root, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        private Material Mat(Color c)
        {
            if (_mats.TryGetValue(c, out var m)) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader != null ? shader : Shader.Find("Standard"));
            m.SetColor("_BaseColor", c);
            m.color = c;
            _mats[c] = m;
            return m;
        }

        private void Label(string name, Vector3 pos, Quaternion rot, Vector2 size, float fontSize, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(Root, false);
            rt.sizeDelta = size;
            rt.SetPositionAndRotation(pos, rot);
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            if (library != null && library.font != null) t.font = library.font;
            t.text = text;
        }
    }
}
