using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V2 — the lab becomes floor 3 of a building (plan Phần D V2): two enclosed stairwells with switchback flights
    /// (ramp colliders under visible steps, 35 deg < the rig's 45 deg slope limit), landings, handrails, floor signs,
    /// emergency lights, push-bar fire doors with closers, an elevator decoy, smoke zones for the corridor and stairs,
    /// the building facade, a yard and the assembly point (new win zone). Idempotent: rebuilds "[BUILDING_V2]".
    /// </summary>
    public static class BuildingBuilder
    {
        private const string LAB = "Assets/Scenes/BasicScene.unity";
        private const string ROOT = "[BUILDING_V2]";
        private const string FONT = "Assets/TextMesh Pro/Resources/Fonts & Materials/EF_Vietnamese SDF.asset";
        public const float Storey = 4.0f;          // floor-to-floor height
        public const float GroundY = -2f * Storey; // lab = floor 3, two storeys above ground
        private const float FlightRun = 2.8f;      // 10 treads x 0.28 m
        private const int Risers = 11;             // 2.0 m / 11 = 0.18 m
        private const float Landing = 1.3f;
        private const float HalfWidth = 1.3f;

        private static Transform _root;
        private static TMP_FontAsset _font;
        private static readonly List<string> Log = new List<string>();

        [MenuItem("EscapeFire/V2/Build Building (stairs, fire doors, elevator, yard)")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(LAB, OpenSceneMode.Single);
            Build();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EscapeFire] Building V2: " + string.Join("; ", Log));
        }

        public static void Build()
        {
            Log.Clear();
            var old = GameObject.Find(ROOT);
            if (old != null) Object.DestroyImmediate(old);
            _root = new GameObject(ROOT).transform;
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);

            // ---- openings in the floor-3 corridor
            var green = GameObject.Find("Emergency_Stairs_Final_Exit_Door");
            if (green != null)
            {
                foreach (var r in green.GetComponentsInChildren<Renderer>()) r.enabled = false;
                foreach (var c in green.GetComponentsInChildren<Collider>()) c.enabled = false;
            }
            Carve("Hallway_End_Wall", new Vector3(0f, 0f, -13.2f), 1.3f, 2.3f);
            Carve("Hallway_Wall_Right", new Vector3(3f, 0f, -10.5f), 1.3f, 2.3f);
            Carve("Hallway_Wall_Left", new Vector3(-3f, 0f, -10.5f), 1.4f, 2.3f);

            var labSmoke = Object.FindFirstObjectByType<SmokeDensityController>(FindObjectsInactive.Include);
            var labExit = Object.FindFirstObjectByType<ExitDoor>(FindObjectsInactive.Include);
            var corridor = Zone("Smoke_Corridor", new Bounds(new Vector3(0f, 2.25f, -10.1f), new Vector3(6f, 4.5f, 6.2f)));
            corridor.Configure(corridor.Space, labSmoke, null, labExit != null ? new[] { labExit } : new ExitDoor[0], new PushBarDoor[0], corridor.transform.Find("Layer"));

            var a = Stairwell("StairwellA", new Vector3(0f, 0f, -13.2f), Vector3.back, +1, corridor);
            var b = Stairwell("StairwellB", new Vector3(3.0f, 0f, -10.5f), Vector3.right, +1, corridor);
            Elevator(new Vector3(-3f, 0f, -10.5f));
            ExitSign(new Vector3(0f, 2.55f, -13.0f), Quaternion.Euler(0f, 180f, 0f), "EXIT  ↓  CẦU THANG A");
            ExitSign(new Vector3(2.85f, 2.55f, -10.5f), Quaternion.Euler(0f, 90f, 0f), "EXIT  →  CẦU THANG B");
            Facade();
            AssemblyPoint(new Vector3(-7f, GroundY, -23f));
            ApplyFont();
            RescueBuilder.Build(Log); // V3 props (sink + towel, radio, victim, Minh at the lift, roll call)
            Log.Add($"stairwells A/B down to y={GroundY}, fire doors {a.Count + b.Count}, elevator decoy, smoke zones, yard + assembly point");
        }

        // ------------------------------------------------------------------ stairwell

        /// <summary>Builds a stairwell whose top doorway is at <paramref name="door"/>; it extends along <paramref name="fwd"/>.</summary>
        private static List<PushBarDoor> Stairwell(string name, Vector3 door, Vector3 fwd, int exitSide, SmokeZone corridor)
        {
            var root = new GameObject(name).transform;
            root.SetParent(_root, false);
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 L(float x, float y, float z) => door + right * x + Vector3.up * y + fwd * z;
            Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);
            float far = Landing + FlightRun + Landing;
            var concrete = Mat("EF_Concrete", new Color(0.66f, 0.66f, 0.64f));
            var stairMat = Mat("EF_StairTread", new Color(0.5f, 0.5f, 0.5f));
            var nosing = Mat("EF_StairNosing", new Color(0.95f, 0.8f, 0.1f));
            var railMat = Mat("EF_Rail", new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.9f);
            var doors = new List<PushBarDoor>();

            // landings: floor landings near the door end, mid landings at the far end
            for (int f = 0; f <= 2; f++)
            {
                float y = -f * Storey;
                // landings start 0.4 m back so they close the gap under the doorway / wall thickness
                var land = Box(root, $"Landing_F{3 - f}", L(0f, y - 0.1f, (Landing - 0.4f) * 0.5f), rot, new Vector3(HalfWidth * 2f, 0.2f, Landing + 0.4f), concrete, true);
                TeleportOnly(land);
                if (f < 2)
                {
                    var mid = Box(root, $"MidLanding_{3 - f}", L(0f, y - Storey * 0.5f - 0.1f, far - Landing * 0.5f), rot, new Vector3(HalfWidth * 2f, 0.2f, Landing), concrete, true);
                    TeleportOnly(mid);
                }
                // on the inside of the near wall, readable for someone in the stairwell looking back at it
                FloorSign(root, L(0f, y + 2.2f, 0.03f), rot * Quaternion.Euler(0f, 180f, 0f), f == 2 ? "TẦNG 1 — LỐI RA" : $"TẦNG {3 - f}", f == 2);
                var lamp = new GameObject($"EmergencyLight_F{3 - f}").AddComponent<Light>();
                lamp.transform.SetParent(root, false);
                lamp.type = LightType.Point;
                lamp.range = 7f;
                lamp.intensity = 1.1f;
                lamp.color = new Color(1f, 0.95f, 0.85f);
                lamp.transform.position = L(0f, y + 2.8f, far * 0.5f);
            }

            // four flights: down-away on the left, down-back on the right, twice
            for (int k = 0; k < 4; k++)
            {
                float yTop = -k * Storey * 0.5f;
                bool away = k % 2 == 0;
                float x = away ? -HalfWidth * 0.5f : HalfWidth * 0.5f;
                float zStart = away ? Landing : Landing + FlightRun;
                float dz = away ? 1f : -1f;
                float h = Storey * 0.5f;
                for (int i = 0; i < Risers; i++)
                {
                    float stepTop = yTop - (i + 1) * h / Risers;
                    float zc = zStart + dz * (i + 0.5f) * (FlightRun / Risers);
                    float height = (stepTop - (yTop - h)) + 0.2f;
                    Box(root, $"Flight{k}_Step{i:00}", L(x, stepTop - height * 0.5f, zc), rot, new Vector3(HalfWidth - 0.05f, height, FlightRun / Risers), stairMat, false);
                    Box(root, $"Flight{k}_Nosing{i:00}", L(x, stepTop + 0.002f, zc - dz * (FlightRun / Risers) * 0.45f), rot, new Vector3(HalfWidth - 0.05f, 0.006f, 0.03f), nosing, false);
                }
                // walking surface: an invisible ramp along the step nosings
                Vector3 top = L(x, yTop, zStart), bottom = L(x, yTop - h, zStart + dz * FlightRun);
                var ramp = new GameObject($"Flight{k}_Ramp");
                ramp.transform.SetParent(root, false);
                ramp.transform.SetPositionAndRotation((top + bottom) * 0.5f + Vector3.down * 0.03f, Quaternion.LookRotation(bottom - top, Vector3.up));
                var rc = ramp.AddComponent<BoxCollider>();
                rc.size = new Vector3(HalfWidth - 0.05f, 0.06f, (bottom - top).magnitude + 0.1f);
                TeleportOnly(ramp);
                // handrail on the wall side, 0.9 m above the nosings
                float rx = away ? -HalfWidth + 0.05f : HalfWidth - 0.05f;
                Vector3 r0 = L(rx, yTop + 0.9f, zStart), r1 = L(rx, yTop - h + 0.9f, zStart + dz * FlightRun);
                var rail = Cylinder(root, $"Handrail_{k}", r0, r1, 0.045f, railMat);
                rail.AddComponent<Handrail>().Configure(r0, r1);
            }

            // shaft walls (skip the top doorway, keep a gap for the ground exit)
            float bottomY = GroundY - 0.2f, topY = 4.5f, wallH = topY - bottomY;
            Box(root, "Wall_Far", L(0f, (topY + bottomY) * 0.5f, far + 0.1f), rot, new Vector3(HalfWidth * 2f + 0.4f, wallH, 0.2f), concrete, true);
            Box(root, "Wall_Near_Below", L(0f, (0f + bottomY) * 0.5f - 0.1f, -0.1f), rot, new Vector3(HalfWidth * 2f + 0.4f, -bottomY - 0.2f, 0.2f), concrete, true);
            Box(root, "Roof", L(0f, topY + 0.1f, far * 0.5f), rot, new Vector3(HalfWidth * 2f + 0.4f, 0.2f, far + 0.4f), concrete, true);
            for (int s = -1; s <= 1; s += 2)
            {
                float wx = s * (HalfWidth + 0.1f);
                if (s == exitSide)
                {
                    // ground exit opening 1.2 x 2.3 at the ground landing
                    float exitTop = GroundY + 2.3f;
                    Box(root, "Wall_Side_ExitTop", L(wx, (exitTop + topY) * 0.5f, far * 0.5f), rot, new Vector3(0.2f, topY - exitTop, far), concrete, true);
                    Box(root, "Wall_Side_ExitFar", L(wx, (bottomY + exitTop) * 0.5f, (1.3f + far) * 0.5f), rot, new Vector3(0.2f, exitTop - bottomY, far - 1.3f), concrete, true);
                    Box(root, "Wall_Side_ExitNear", L(wx, (bottomY + exitTop) * 0.5f, 0.05f), rot, new Vector3(0.2f, exitTop - bottomY, 0.1f), concrete, true);
                    doors.Add(FireDoor(root, "ExitDoor_Ground", L(wx, GroundY, Landing * 0.5f + 0.05f), Quaternion.LookRotation(right * s, Vector3.up), 1.2f, true));
                    ExitSign(L(wx - s * 0.12f, GroundY + 2.5f, Landing * 0.5f), Quaternion.LookRotation(right * s, Vector3.up), "EXIT — RA SÂN");
                }
                else Box(root, "Wall_Side_" + s, L(wx, (topY + bottomY) * 0.5f, far * 0.5f), rot, new Vector3(0.2f, wallH, far), concrete, true);
            }

            // doors: floor 3 (from the corridor, push into the stairs), floor 2 (closed)
            doors.Add(FireDoor(root, "FireDoor_F3", door + fwd * 0.0f, rot, 1.2f, true));
            FireDoor(root, "FireDoor_F2", L(0f, -Storey, -0.0f), rot, 1.2f, false);

            var zone = Zone(name + "_Smoke", new Bounds(L(0f, (topY + GroundY) * 0.5f, far * 0.5f), Abs(rot * new Vector3(HalfWidth * 2f, topY - GroundY, far))));
            zone.transform.SetParent(root, true);
            zone.Configure(zone.Space, null, corridor, new ExitDoor[0], new[] { doors[doors.Count - 1] }, zone.transform.Find("Layer"));
            return doors;
        }

        private static PushBarDoor FireDoor(Transform parent, string name, Vector3 sill, Quaternion facing, float width, bool working)
        {
            // facing: the door's forward is the push / egress direction.
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(sill, facing);
            var leafMat = Mat("EF_FireDoor", new Color(0.55f, 0.12f, 0.1f), 0.4f, 0.3f);
            var barMat = Mat("EF_Rail", new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.9f);
            var pivot = new GameObject("LeafPivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
            var leaf = Box(pivot, "Leaf", pivot.TransformPoint(new Vector3(width * 0.5f, 1.1f, 0f)), facing, new Vector3(width - 0.04f, 2.2f, 0.05f), leafMat, false);
            var window = Box(pivot, "Window", pivot.TransformPoint(new Vector3(width * 0.5f, 1.55f, 0f)), facing, new Vector3(0.25f, 0.5f, 0.06f), Mat("EF_WindowGlass", new Color(0.62f, 0.78f, 0.86f, 0.28f), 0.95f), false);
            var bar = Box(pivot, "PushBar", pivot.TransformPoint(new Vector3(width * 0.55f, 1.0f, -0.07f)), facing, new Vector3(width * 0.75f, 0.06f, 0.06f), barMat, false);
            var barCol = bar.AddComponent<BoxCollider>();
            barCol.isTrigger = true;
            var blocker = root.gameObject.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, 1.1f, 0f);
            blocker.size = new Vector3(width, 2.2f, 0.08f);
            if (!working) return null;
            var d = root.gameObject.AddComponent<PushBarDoor>();
            // opening = pushing the free edge (local +x side) forward: positive rotation about up moves +x towards -z,
            // so the leaf swings forward with a negative angle.
            d.Configure(pivot, barCol, blocker, facing * Vector3.forward, -1f);
            return d;
        }

        // ------------------------------------------------------------------ elevator

        private static void Elevator(Vector3 opening)
        {
            var root = new GameObject("Elevator").transform;
            root.SetParent(_root, false);
            var steel = Mat("EF_ElevatorSteel", new Color(0.7f, 0.72f, 0.75f), 0.8f, 0.9f);
            var car = Mat("EF_ElevatorCar", new Color(0.55f, 0.5f, 0.45f));
            Vector3 inward = Vector3.left; // the car is behind the corridor's west wall
            Vector3 c = opening + inward * 1.1f;
            Box(root, "CarFloor", c + Vector3.down * 0.05f, Quaternion.identity, new Vector3(2.0f, 0.1f, 1.6f), car, true);
            Box(root, "CarBack", c + inward * 1.0f + Vector3.up * 1.3f, Quaternion.identity, new Vector3(0.1f, 2.6f, 1.6f), car, true);
            Box(root, "CarSideN", c + new Vector3(0f, 1.3f, 0.8f), Quaternion.identity, new Vector3(2.0f, 2.6f, 0.1f), car, true);
            Box(root, "CarSideS", c + new Vector3(0f, 1.3f, -0.8f), Quaternion.identity, new Vector3(2.0f, 2.6f, 0.1f), car, true);
            Box(root, "CarCeiling", c + Vector3.up * 2.65f, Quaternion.identity, new Vector3(2.0f, 0.1f, 1.6f), car, false);
            var l = Box(root, "DoorLeft", opening + new Vector3(0.02f, 1.15f, -0.35f), Quaternion.identity, new Vector3(0.05f, 2.3f, 0.7f), steel, true);
            var r = Box(root, "DoorRight", opening + new Vector3(0.02f, 1.15f, 0.35f), Quaternion.identity, new Vector3(0.05f, 2.3f, 0.7f), steel, true);
            var trap = root.gameObject.AddComponent<ElevatorTrap>();
            trap.Configure(l.transform, r.transform, new Vector3(0f, 0f, 0.65f), new Bounds(c + Vector3.up * 1.3f, new Vector3(1.9f, 2.6f, 1.5f)));
            var btn = Box(root, "CallButton", opening + new Vector3(0.12f, 1.1f, -1.0f), Quaternion.identity, new Vector3(0.04f, 0.09f, 0.07f), Mat("EF_ElevatorButton", new Color(0.9f, 0.9f, 0.85f)), true);
            var si = btn.AddComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(si.selectEntered, trap.CallPressed);
            RigRealismSetup.MakePokeable(si, Vector3.left, 0.009f);
            Label(root, "ElevatorSign", opening + new Vector3(0.12f, 2.6f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector2(1.6f, 0.35f), 0.9f,
                "<b>THANG MÁY</b>\n<color=#ff5040>KHÔNG SỬ DỤNG KHI CÓ CHÁY</color>");
            Box(root, "ShaftBelow", c + Vector3.down * (-GroundY * 0.5f + 0.1f), Quaternion.identity, new Vector3(2.2f, -GroundY, 1.8f), Mat("EF_Concrete", new Color(0.66f, 0.66f, 0.64f)), true);
        }

        // ------------------------------------------------------------------ outside

        private static void Facade()
        {
            var concrete = Mat("EF_Facade", new Color(0.72f, 0.7f, 0.66f));
            float h = -GroundY;
            Box(_root, "Facade_Lab", new Vector3(0f, GroundY + h * 0.5f - 0.2f, 0f), Quaternion.identity, new Vector3(18.4f, h, 14.4f), concrete, true);
            Box(_root, "Facade_Corridor", new Vector3(-0.1f, GroundY + h * 0.5f - 0.2f, -10.1f), Quaternion.identity, new Vector3(6.1f, h, 6.2f), concrete, true);
            var yard = Box(_root, "Yard", new Vector3(0f, GroundY - 0.1f, -10f), Quaternion.identity, new Vector3(140f, 0.2f, 140f), Mat("EF_Grass", new Color(0.32f, 0.5f, 0.28f)), true);
            TeleportOnly(yard);
            Box(_root, "Path", new Vector3(-3.5f, GroundY + 0.005f, -19f), Quaternion.identity, new Vector3(3f, 0.01f, 12f), Mat("EF_Path", new Color(0.6f, 0.58f, 0.55f)), false);
        }

        private static void AssemblyPoint(Vector3 p)
        {
            var root = new GameObject("AssemblyPoint").transform;
            root.SetParent(_root, false);
            root.position = p;
            var green = Mat("EF_AssemblyGreen", new Color(0.1f, 0.65f, 0.25f));
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Marking";
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(root, false);
            disc.transform.SetPositionAndRotation(p + Vector3.up * 0.01f, Quaternion.identity);
            disc.transform.localScale = new Vector3(5f, 0.01f, 5f);
            disc.GetComponent<Renderer>().sharedMaterial = green;
            Box(root, "SignPost", p + new Vector3(0f, 1.2f, 2.6f), Quaternion.identity, new Vector3(0.08f, 2.4f, 0.08f), Mat("EF_Rail", new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.9f), true);
            Box(root, "SignBoard", p + new Vector3(0f, 2.3f, 2.6f), Quaternion.identity, new Vector3(1.6f, 0.7f, 0.05f), green, false);
            Label(root, "SignText", p + new Vector3(0f, 2.3f, 2.57f), Quaternion.identity, new Vector2(1.5f, 0.6f), 1.4f, "<b>ĐIỂM TẬP KẾT</b>\nASSEMBLY POINT");
            var zone = Object.FindFirstObjectByType<EvacuationExitZone>(FindObjectsInactive.Include);
            if (zone != null)
            {
                zone.transform.SetPositionAndRotation(p + Vector3.up * 1.5f, Quaternion.identity);
                zone.transform.localScale = Vector3.one;
                var box = zone.GetComponent<BoxCollider>();
                if (box != null) { box.isTrigger = true; box.center = Vector3.zero; box.size = new Vector3(5f, 3.5f, 5f); }
                EditorUtility.SetDirty(zone.transform);
                Log.Add("win zone moved to the assembly point");
            }
        }

        // ------------------------------------------------------------------ helpers

        private static SmokeZone Zone(string name, Bounds b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = b.center;
            var layer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            layer.name = "Layer";
            Object.DestroyImmediate(layer.GetComponent<Collider>());
            layer.transform.SetParent(go.transform, true);
            layer.transform.position = new Vector3(b.center.x, b.max.y - 0.1f, b.center.z);
            layer.transform.localScale = new Vector3(b.size.x, 0.1f, b.size.z);
            layer.GetComponent<Renderer>().enabled = false;
            var z = go.AddComponent<SmokeZone>();
            z.Configure(b, null, null, new ExitDoor[0], new PushBarDoor[0], layer.transform);
            return z;
        }

        /// <summary>Splits an axis-aligned wall into pieces around a doorway (idempotent: pieces live under the V2 root).</summary>
        private static void Carve(string wallName, Vector3 at, float width, float height)
        {
            var wall = GameObject.Find(wallName);
            if (wall == null || !wall.TryGetComponent<Renderer>(out var wr)) { Log.Add($"wall {wallName} not found"); return; }
            Bounds w = wr.bounds;
            bool alongX = w.size.x >= w.size.z;
            float min = alongX ? w.min.x : w.min.z, max = alongX ? w.max.x : w.max.z;
            float c = alongX ? at.x : at.z, lo = c - width * 0.5f, hi = c + width * 0.5f, thick = alongX ? w.size.z : w.size.x;
            float top = w.min.y + height;
            Vector3 P(float along, float y) => alongX ? new Vector3(along, y, w.center.z) : new Vector3(w.center.x, y, along);
            Vector3 S(float len, float h) => alongX ? new Vector3(len, h, thick) : new Vector3(thick, h, len);
            var m = wr.sharedMaterial;
            if (lo > min) Box(_root, wallName + "_A", P((min + lo) * 0.5f, w.center.y), Quaternion.identity, S(lo - min, w.size.y), m, true);
            if (hi < max) Box(_root, wallName + "_B", P((hi + max) * 0.5f, w.center.y), Quaternion.identity, S(max - hi, w.size.y), m, true);
            Box(_root, wallName + "_Lintel", P(c, (top + w.max.y) * 0.5f), Quaternion.identity, S(width, w.max.y - top), m, true);
            wr.enabled = false;
            if (wall.TryGetComponent<Collider>(out var wc)) wc.enabled = false;
        }

        /// <summary>
        /// Teleport target reachable only by the teleport ray ("Teleport" interaction layer), so a hand gripping near
        /// the floor (e.g. under a victim's arms) does not grab the floor.
        /// </summary>
        private static void TeleportOnly(GameObject go)
        {
            var area = go.AddComponent<TeleportationArea>();
            area.interactionLayers = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Teleport");
        }

        internal static GameObject Box(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 size, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(size.x / ps.x, size.y / ps.y, size.z / ps.z);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        internal static GameObject Cylinder(Transform parent, string name, Vector3 a, Vector3 b, float diameter, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.FromToRotation(Vector3.up, (b - a).normalized));
            go.transform.localScale = new Vector3(diameter, (b - a).magnitude * 0.5f, diameter);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void FloorSign(Transform parent, Vector3 pos, Quaternion rot, string text, bool exit)
        {
            Box(parent, "FloorSignPlate_" + text, pos, rot, new Vector3(1.0f, 0.35f, 0.02f), Mat(exit ? "EF_AssemblyGreen" : "EF_SignBlue", exit ? new Color(0.1f, 0.65f, 0.25f) : new Color(0.1f, 0.25f, 0.6f)), false);
            Label(parent, "FloorSign_" + text, pos - rot * Vector3.forward * 0.02f, rot, new Vector2(0.95f, 0.3f), 1.0f, "<b>" + text + "</b>");
        }

        private static void ExitSign(Vector3 pos, Quaternion rot, string text)
        {
            Box(_root, "ExitSignBox_" + text, pos, rot, new Vector3(0.9f, 0.22f, 0.06f), Mat("EF_ExitGreen", new Color(0.05f, 0.75f, 0.3f)), false);
            Label(_root, "ExitSign_" + text, pos - rot * Vector3.forward * 0.04f, rot, new Vector2(0.88f, 0.2f), 0.8f, "<b>" + text + "</b>");
            var glow = new GameObject("ExitGlow").AddComponent<Light>();
            glow.transform.SetParent(_root, false);
            glow.type = LightType.Point;
            glow.color = new Color(0.2f, 1f, 0.4f);
            glow.range = 2.5f;
            glow.intensity = 0.6f;
            glow.transform.position = pos - rot * Vector3.forward * 0.3f;
        }

        internal static void Label(Transform parent, string name, Vector3 pos, Quaternion rot, Vector2 size, float fontSize, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.SetPositionAndRotation(pos, rot);
            var t = go.AddComponent<TextMeshPro>();
            rt.sizeDelta = size;
            rt.SetPositionAndRotation(pos, rot);
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.text = text;
            if (_font != null) t.font = _font;
        }

        private static void ApplyFont()
        {
            if (_font == null) return;
            foreach (var t in _root.GetComponentsInChildren<TMP_Text>(true)) t.font = _font;
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        internal static Material Mat(string name, Color c, float smooth = 0.3f, float metal = 0f)
        {
            string path = "Assets/Settings/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                m.SetColor("_BaseColor", c);
                m.SetFloat("_Smoothness", smooth);
                m.SetFloat("_Metallic", metal);
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }
    }
}
