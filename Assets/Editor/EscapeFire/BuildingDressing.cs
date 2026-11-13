using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EscapeFire.Interactions;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V6 graphics — dresses the grey-box drill building with PolygonOffice pieces and the Vietnamese fire-safety signs
    /// (Tools/make_signs.py): suspended ceilings with light panels, sprinklers and smoke detectors; door frames,
    /// baseboards and a painted dado in the corridor; an escape plan and the "no lift in a fire" sign; stair balusters
    /// and floor finishes; facade windows and floor bands; trees, bushes, benches and lamp posts in the yard.
    /// Visual only: the gameplay colliders stay the grey-box ones (interior dressing has its colliders removed), and no
    /// decorative extinguisher or call point is added (the learner must not mistake a prop for the real one).
    /// Rebuilt by RescueBuilder.Build into "[DRESSING]".
    /// </summary>
    public static class BuildingDressing
    {
        private const string ROOT = "[DRESSING]";
        private const string PO = "Assets/PolygonOffice/Prefabs/";
        internal const string SIGNS = "Assets/Art/Signs/";
        private static Transform _root;
        private static int _pieces;

        public static void Build(List<string> log)
        {
            var old = GameObject.Find(ROOT);
            if (old != null) Object.DestroyImmediate(old);
            _root = new GameObject(ROOT).transform;
            _pieces = 0;

            Palette();
            // light bouncing off the pale floors: ceilings (faces looking down) were lit by a dark ground colour
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.49f, 0.47f);
            Ceiling("Lab", -9f, 9f, -7f, 7f, 4.5f, new[] { new Vector2(-5f, -3.5f), new Vector2(0f, -3.5f), new Vector2(5f, -3.5f), new Vector2(-5f, 2.5f), new Vector2(0f, 2.5f), new Vector2(5f, 2.5f) });
            Ceiling("Corridor", -2.95f, 2.95f, -13.15f, -7f, 4.5f, new[] { new Vector2(0f, -8.6f), new Vector2(0f, -11.6f) });
            Corridor();
            foreach (var name in new[] { "StairwellA", "StairwellB" }) Stairwell(name);
            StairWindows("StairwellA", new Vector3(0f, 0f, -13.2f), Vector3.back);
            StairWindows("StairwellB", new Vector3(3.0f, 0f, -10.5f), Vector3.right);
            CorridorLight();
            Facade();
            Yard();
            log.Add($"dressing: {_pieces} pieces (ceilings, lights, sprinklers, detectors, frames, signs, facade, yard)");
        }

        // ------------------------------------------------------------------ materials

        private static Material M(string name, Color c, float smooth = 0.3f, float metal = 0f) => BuildingBuilder.Mat(name, c, smooth, metal);

        /// <summary>Real finishes instead of flat grey: painted walls, terrazzo stairs, grass and paving.</summary>
        private static void Palette()
        {
            Recolor("EF_Concrete", new Color(0.86f, 0.85f, 0.81f));   // painted stairwell walls
            Recolor("EF_StairTread", new Color(0.64f, 0.62f, 0.58f)); // terrazzo
            Recolor("EF_Facade", new Color(0.83f, 0.8f, 0.74f));
            Recolor("EF_Grass", new Color(0.34f, 0.5f, 0.24f));
            Recolor("EF_Path", new Color(0.66f, 0.64f, 0.6f));
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.name.StartsWith("Landing_F") || r.name.StartsWith("MidLanding_")) r.sharedMaterial = M("EF_StairTread", Color.gray);
        }

        private static void Recolor(string name, Color c)
        {
            var m = M(name, c);
            m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
        }

        internal static Material SignMaterial(string texture, bool glowing)
        {
            string path = SIGNS + "Materials/" + texture + (glowing ? "_Lit" : "") + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            string tex = SIGNS + texture + ".png";
            if (AssetImporter.GetAtPath(tex) is TextureImporter ti && (ti.wrapMode != TextureWrapMode.Clamp || ti.maxTextureSize != 1024))
            {
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = 1024;
                ti.anisoLevel = 4;
                ti.SaveAndReimport();
            }
            System.IO.Directory.CreateDirectory(SIGNS + "Materials");
            // a backlit sign is unlit (reads in the smoke and the dark); a printed one takes the room's light
            m = new Material(Shader.Find(glowing ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
            m.SetColor("_BaseColor", Color.white);
            if (!glowing) m.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>
        /// A sign: frame plus a printed / backlit face. <paramref name="outward"/> is the side it is read from.
        /// </summary>
        internal static GameObject Sign(Transform parent, string name, string texture, Vector3 centre, Vector3 outward, float width, float height, bool glowing, bool twoSided = false)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(centre, Quaternion.LookRotation(outward, Vector3.up));
            float depth = glowing ? 0.06f : 0.015f;
            var frame = BuildingBuilder.Box(root, "Frame", centre - outward * depth * 0.5f, root.rotation, new Vector3(width + 0.04f, height + 0.04f, depth),
                M("EF_SignFrame", new Color(0.92f, 0.92f, 0.9f), 0.5f, 0.2f), false);
            Face(root, texture, glowing, centre + outward * 0.002f, outward, width, height);
            if (twoSided) Face(root, texture, glowing, centre - outward * (depth + 0.002f), -outward, width, height);
            _pieces++;
            return root.gameObject;
        }

        private static void Face(Transform root, string texture, bool glowing, Vector3 pos, Vector3 outward, float width, float height)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Face";
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(root, false);
            // a quad is seen from its -z side
            q.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-outward, Vector3.up));
            q.transform.localScale = new Vector3(width, height, 1f);
            q.GetComponent<Renderer>().sharedMaterial = SignMaterial(texture, glowing);
        }

        // ------------------------------------------------------------------ prefab placement

        private static GameObject Place(string prefab, Vector3 pos, Quaternion rot, Transform parent, Vector3? scale = null, bool keepColliders = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PO + prefab + ".prefab");
            if (asset == null) { Debug.LogWarning("[Dressing] missing " + prefab); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetPositionAndRotation(pos, rot);
            if (scale.HasValue) go.transform.localScale = scale.Value;
            if (!keepColliders)
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            _pieces++;
            return go;
        }

        private static Transform Group(string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_root, false);
            return t;
        }

        // ------------------------------------------------------------------ ceilings

        /// <summary>Suspended ceiling tiles just under the structural ceiling, light panels, sprinklers and detectors.</summary>
        private static void Ceiling(string room, float x0, float x1, float z0, float z1, float y, Vector2[] lights)
        {
            var g = Group("Ceiling_" + room);
            float top = y - 0.01f;
            int nx = Mathf.CeilToInt((x1 - x0) / 2.5f), nz = Mathf.CeilToInt((z1 - z0) / 2.5f);
            float sx = (x1 - x0) / (nx * 2.5f), sz = (z1 - z0) / (nz * 2.5f);
            // the atlas colour of the Synty tile is a dark grey: paint the tiles off-white (their bevels keep the grid)
            var tileMat = M("EF_CeilingTile", new Color(0.9f, 0.9f, 0.87f), 0.15f);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++) // tile pivot: max-x / min-z corner, top face
                {
                    var tile = Place((i + j) % 3 == 0 ? "Buildings/SM_Bld_Ceiling_Panel_02" : "Buildings/SM_Bld_Ceiling_Panel_01",
                        new Vector3(x0 + (i + 1) * 2.5f * sx, top, z0 + j * 2.5f * sz), Quaternion.identity, g, new Vector3(sx, 1f, sz));
                    if (tile != null) foreach (var r in tile.GetComponentsInChildren<Renderer>()) r.sharedMaterial = tileMat;
                }
            var glow = DiffuserMaterial();
            foreach (var l in lights)
            {
                Place("Buildings/SM_Bld_Ceiling_Panel_Light_02", new Vector3(l.x, top - 0.045f, l.y), Quaternion.identity, g);
                BuildingBuilder.Box(g, "Diffuser", new Vector3(l.x, top - 0.08f, l.y), Quaternion.identity, new Vector3(0.95f, 0.01f, 2.0f), glow, false);
            }
            // sprinklers on a ~3 m grid, smoke detectors one per ~30 m2
            for (float x = x0 + 1.5f; x < x1 - 0.5f; x += 3f)
                for (float z = z0 + 1.5f; z < z1 - 0.5f; z += 3f)
                    Place("Props/Roof Props/SM_Prop_Sprinkler", new Vector3(x, top - 0.04f, z), Quaternion.identity, g);
            for (float x = x0 + 3f; x < x1 - 1f; x += 6f)
                for (float z = z0 + 3f; z < z1 - 1f; z += 5f)
                    Place("Props/Wall Props/SM_Prop_Fire_Detector_01", new Vector3(x + 0.7f, top - 0.04f, z + 0.4f), Quaternion.Euler(90f, 0f, 0f), g);
        }

        /// <summary>The lit face of a light panel: unlit warm white, so it reads as a lamp whatever the room light.</summary>
        private static Material DiffuserMaterial()
        {
            const string path = "Assets/Settings/EF_LightDiffuser.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", new Color(1f, 0.97f, 0.9f));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ------------------------------------------------------------------ corridor

        private struct WallRun { public Vector3 A, B, Out; public float[] Gaps; public float GapHalf; }

        private static void Corridor()
        {
            var g = Group("Corridor");
            var paint = M("EF_DadoPaint", new Color(0.6f, 0.67f, 0.66f));
            var rail = M("EF_DadoRail", new Color(0.95f, 0.95f, 0.93f));
            var skirting = M("EF_Skirting", new Color(0.25f, 0.25f, 0.27f));
            var frame = M("EF_DoorFrame", new Color(0.3f, 0.31f, 0.33f), 0.5f, 0.6f);
            // inner wall faces of the floor-3 corridor and the doorways in them (centre, half width)
            var runs = new[]
            {
                (a: new Vector3(-2.9f, 0f, -7.1f), b: new Vector3(-2.9f, 0f, -13.1f), o: Vector3.right, gap: -10.5f, half: 0.7f),
                (a: new Vector3(2.9f, 0f, -7.1f), b: new Vector3(2.9f, 0f, -13.1f), o: Vector3.left, gap: -10.5f, half: 0.65f),
                (a: new Vector3(-2.9f, 0f, -13.1f), b: new Vector3(2.9f, 0f, -13.1f), o: Vector3.forward, gap: 0f, half: 0.65f),
                (a: new Vector3(-2.9f, 0f, -7.1f), b: new Vector3(2.9f, 0f, -7.1f), o: Vector3.back, gap: 0f, half: 1.0f),
            };
            foreach (var r in runs)
            {
                Vector3 dir = (r.b - r.a).normalized;
                float len = (r.b - r.a).magnitude;
                bool alongZ = Mathf.Abs(dir.z) > 0.5f;
                float gapAt = alongZ ? Mathf.Abs(r.gap - r.a.z) : Mathf.Abs(r.gap - r.a.x);
                var pieces = new[] { (s: 0f, e: gapAt - r.half), (s: gapAt + r.half, e: len) };
                foreach (var p in pieces)
                {
                    if (p.e - p.s < 0.05f) continue;
                    Vector3 mid = r.a + dir * (p.s + p.e) * 0.5f;
                    Quaternion q = Quaternion.LookRotation(r.o, Vector3.up);
                    float l = p.e - p.s;
                    BuildingBuilder.Box(g, "Dado", mid + r.o * 0.006f + Vector3.up * 0.6f, q, new Vector3(l, 1.2f, 0.01f), paint, false);
                    BuildingBuilder.Box(g, "DadoRail", mid + r.o * 0.015f + Vector3.up * 1.2f, q, new Vector3(l, 0.05f, 0.025f), rail, false);
                    BuildingBuilder.Box(g, "Skirting", mid + r.o * 0.015f + Vector3.up * 0.05f, q, new Vector3(l, 0.1f, 0.025f), skirting, false);
                    _pieces += 3;
                }
                // steel door frame around the doorway
                Vector3 c = r.a + dir * gapAt;
                float h = r.half >= 1f ? 2.52f : 2.3f;
                Quaternion fq = Quaternion.LookRotation(r.o, Vector3.up);
                foreach (float s in new[] { -1f, 1f })
                    BuildingBuilder.Box(g, "DoorJamb", c + dir * s * (r.half + 0.04f) + Vector3.up * h * 0.5f, fq, new Vector3(0.08f, h, 0.3f), frame, false);
                BuildingBuilder.Box(g, "DoorHead", c + Vector3.up * (h + 0.04f), fq, new Vector3(r.half * 2f + 0.16f, 0.08f, 0.3f), frame, false);
                _pieces += 3;
            }
            // escape plan by the lab door, "no lift" sign beside the lift, the stair names beside the stair doors
            Sign(g, "EscapePlan", "EF_Sign_EscapePlan", new Vector3(2.885f, 1.68f, -8.0f), Vector3.left, 0.8f, 0.6f, false);
            Sign(g, "NoLiftInFire", "EF_Sign_NoElevator", new Vector3(-2.885f, 1.58f, -11.75f), Vector3.right, 0.4f, 0.5f, false);
            DoorPlate(g, "CẦU THANG A", new Vector3(0.95f, 1.55f, -13.085f), Vector3.forward);
            DoorPlate(g, "CẦU THANG B", new Vector3(2.885f, 1.55f, -11.45f), Vector3.left);
            // a tall plant in the corner and wall vents high up
            Place("Props/Misc/SM_Prop_Plant_14", new Vector3(-2.45f, 0f, -7.65f), Quaternion.Euler(0f, 30f, 0f), g);
            Place("Props/Wall Props/SM_Prop_AirVent_Wall_01", new Vector3(2.89f, 3.6f, -9.0f), Quaternion.LookRotation(Vector3.left), g);
            Place("Props/Wall Props/SM_Prop_AirVent_Wall_01", new Vector3(-2.89f, 3.6f, -8.5f), Quaternion.LookRotation(Vector3.right), g);
        }

        private static void DoorPlate(Transform g, string text, Vector3 pos, Vector3 outward)
        {
            BuildingBuilder.Box(g, "DoorPlate_" + text, pos - outward * 0.005f, Quaternion.LookRotation(outward), new Vector3(0.42f, 0.13f, 0.01f), M("EF_SignBlue", new Color(0.1f, 0.25f, 0.6f)), false);
            BuildingBuilder.Label(g, "DoorPlateText_" + text, pos + outward * 0.002f, Quaternion.LookRotation(-outward), new Vector2(0.4f, 0.11f), 0.5f, "<b>" + text + "</b>");
            _pieces++;
        }

        // ------------------------------------------------------------------ stairs

        /// <summary>Balusters under the handrails and a light fitting on each landing.</summary>
        private static void Stairwell(string name)
        {
            var well = GameObject.Find("[BUILDING_V2]/" + name);
            if (well == null) return;
            var g = Group("Stairs_" + name);
            var steel = M("EF_Rail", new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.9f);
            foreach (var rail in well.GetComponentsInChildren<Handrail>(true))
            {
                var t = rail.transform;
                Vector3 a = t.position - t.up * t.localScale.y, b = t.position + t.up * t.localScale.y;
                int n = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / 0.9f));
                for (int i = 0; i <= n; i++)
                {
                    Vector3 top = Vector3.Lerp(a, b, i / (float)n);
                    BuildingBuilder.Cylinder(g, "Baluster", top, top + Vector3.down * 0.88f, 0.03f, steel);
                    _pieces++;
                }
            }
            foreach (var lamp in well.GetComponentsInChildren<Light>(true))
            {
                var fitting = Place("Props/Roof Props/SM_Prop_Light_06", lamp.transform.position + Vector3.up * 0.15f, Quaternion.identity, g);
                if (fitting == null) continue;
            }
        }

        /// <summary>The corridor had no lamp of its own: one warm ceiling light over the light panels.</summary>
        private static void CorridorLight()
        {
            CeilingLight("CorridorCeilingLight", new Vector3(0f, 4.1f, -10.1f), 8f, 1.2f);
            // the lab ceiling was only lit from below by the window: two lamps under the light panels
            CeilingLight("LabCeilingLight_W", new Vector3(-4.5f, 4.0f, -0.5f), 10f, 0.9f);
            CeilingLight("LabCeilingLight_E", new Vector3(4.5f, 4.0f, -0.5f), 10f, 0.9f);
        }

        private static void CeilingLight(string name, Vector3 pos, float range, float intensity)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(_root, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = new Color(1f, 0.96f, 0.88f);
            l.shadows = LightShadows.None;
        }

        /// <summary>Narrow windows in the outside walls of a stairwell (seen from the yard), one per half storey.</summary>
        private static void StairWindows(string name, Vector3 door, Vector3 fwd)
        {
            var g = Group("StairWindows_" + name);
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            var glass = M("EF_FacadeGlass", new Color(0.22f, 0.3f, 0.36f), 0.9f, 0.3f);
            var frame = M("EF_WindowFrame", new Color(0.35f, 0.36f, 0.38f), 0.5f, 0.5f);
            for (int f = 0; f < 3; f++)
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 outward = right * side;
                    Vector3 c = door + outward * 1.52f + fwd * 2.7f + Vector3.up * (-f * BuildingBuilder.Storey + 1.6f);
                    Quaternion q = Quaternion.LookRotation(outward, Vector3.up);
                    BuildingBuilder.Box(g, "StairWindowFrame", c, q, new Vector3(0.8f, 1.2f, 0.04f), frame, false);
                    BuildingBuilder.Box(g, "StairWindowGlass", c + outward * 0.015f, q, new Vector3(0.68f, 1.08f, 0.02f), glass, false);
                    _pieces += 2;
                }
        }

        // ------------------------------------------------------------------ outside

        private static void Facade()
        {
            var g = Group("Facade");
            var glass = M("EF_FacadeGlass", new Color(0.22f, 0.3f, 0.36f), 0.9f, 0.3f);
            var frame = M("EF_WindowFrame", new Color(0.35f, 0.36f, 0.38f), 0.5f, 0.5f);
            var band = M("EF_FacadeBand", new Color(0.92f, 0.91f, 0.88f));
            float g0 = BuildingBuilder.GroundY;
            // the two storeys under the lab (and the lab storey on the south side)
            for (int f = 0; f < 3; f++)
            {
                float floor = g0 + f * BuildingBuilder.Storey;
                foreach (float x in new[] { -7.8f, -5.4f, 4.6f, 7.0f }) Window(g, new Vector3(x, floor, -7.13f - (f == 2 ? 0.0f : 0.1f)), Vector3.back, glass, frame);
                if (f == 2) continue;
                foreach (float z in new[] { -5f, -2.5f, 0f, 2.5f, 5f })
                {
                    Window(g, new Vector3(-9.23f, floor, z), Vector3.left, glass, frame);
                    Window(g, new Vector3(9.23f, floor, z), Vector3.right, glass, frame);
                }
                float by = floor + BuildingBuilder.Storey - 0.15f;
                BuildingBuilder.Box(g, "Band_S", new Vector3(0f, by, -7.24f), Quaternion.identity, new Vector3(18.5f, 0.3f, 0.1f), band, false);
                BuildingBuilder.Box(g, "Band_W", new Vector3(-9.24f, by, 0f), Quaternion.identity, new Vector3(0.1f, 0.3f, 14.5f), band, false);
                BuildingBuilder.Box(g, "Band_E", new Vector3(9.24f, by, 0f), Quaternion.identity, new Vector3(0.1f, 0.3f, 14.5f), band, false);
                _pieces += 3;
            }
        }

        private static void Window(Transform g, Vector3 sillCentre, Vector3 outward, Material glass, Material frame)
        {
            Quaternion q = Quaternion.LookRotation(outward, Vector3.up);
            Vector3 c = sillCentre + Vector3.up * (0.9f + 0.75f);
            BuildingBuilder.Box(g, "WindowFrame", c, q, new Vector3(1.7f, 1.6f, 0.05f), frame, false);
            BuildingBuilder.Box(g, "WindowGlass", c + outward * 0.02f, q, new Vector3(1.55f, 1.45f, 0.03f), glass, false);
            BuildingBuilder.Box(g, "WindowMullion", c + outward * 0.04f, q, new Vector3(0.04f, 1.45f, 0.02f), frame, false);
            BuildingBuilder.Box(g, "WindowSill", sillCentre + Vector3.up * 0.86f + outward * 0.06f, q, new Vector3(1.8f, 0.05f, 0.14f), frame, false);
            _pieces += 4;
        }

        private static void Yard()
        {
            var g = Group("Yard");
            float y = BuildingBuilder.GroundY;
            var rng = new System.Random(7);
            var trees = new[]
            {
                new Vector2(-13f, -14f), new Vector2(-14.5f, -20f), new Vector2(-12.5f, -28.5f), new Vector2(-4f, -31f), new Vector2(3.5f, -27f),
                new Vector2(8.5f, -21.5f), new Vector2(12.5f, -16f), new Vector2(13.5f, -8f), new Vector2(-14f, -6f), new Vector2(-16f, 2f),
                new Vector2(14.5f, 2f), new Vector2(6.5f, -31.5f), new Vector2(-18f, -25f), new Vector2(17f, -24f),
            };
            foreach (var p in trees)
                Place($"Generic/SM_Generic_Tree_0{1 + rng.Next(4)}", new Vector3(p.x, y, p.y), Quaternion.Euler(0f, rng.Next(360), 0f), g, Vector3.one * (1.2f + (float)rng.NextDouble() * 0.4f), true);
            foreach (float x in new[] { -8f, -6f, 5f, 7.2f })
                Place(rng.Next(2) == 0 ? "Props/Misc/SM_Prop_Plant_12" : "Props/Misc/SM_Prop_Plant_13", new Vector3(x, y, -8.1f), Quaternion.Euler(0f, rng.Next(360), 0f), g, null, false);
            foreach (float z in new[] { -15f, -20f, -25f })
                Place("Props/Exterior/SM_Prop_LampPost_01", new Vector3(-1.5f, y, z), Quaternion.LookRotation(Vector3.left), g, null, true);
            Place("Props/Exterior/SM_Prop_Benchseat_01", new Vector3(-11.6f, y, -21f), Quaternion.LookRotation(Vector3.right), g, null, true);
            Place("Props/Exterior/SM_Prop_Benchseat_01", new Vector3(-2.2f, y, -28f), Quaternion.LookRotation(Vector3.forward), g, null, true);
        }
    }
}
