using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V6 — the menu, quiz and debrief boards used to float over a flat plane under an open sky, and the lesson room was
    /// a grey box. This dresses them as a fire-safety training centre: wooden floor, two-tone walls with skirting,
    /// suspended ceiling with light panels, a big banner, PCCC posters (PASS, 114, fire triangle, "4 tại chỗ", escape
    /// plan), a wall fire-hose cabinet, an exit door with its lit sign, plants and benches. Rebuilt into
    /// "[TRAINING_CENTRE]"; walls carry colliders only where the room had none.
    /// </summary>
    public static class TrainingCentreBuilder
    {
        private const string ROOT = "[TRAINING_CENTRE]";

        [MenuItem("EscapeFire/V6/Dress Menu, AAR and Lesson as a Training Centre")]
        public static void Run()
        {
            Dress(EscapeFireSceneBuilder.MenuPath, Menu);
            Dress(EscapeFireSceneBuilder.AARPath, Debrief);
            Dress("Assets/Scenes/01_Lesson.unity", Lesson);
            Debug.Log("[EscapeFire] V6 training centre dressed: menu, AAR, lesson room");
        }

        private static void Dress(string path, System.Action<Transform> build)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var old = GameObject.Find(ROOT);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(ROOT).transform;
            build(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ------------------------------------------------------------------ the three rooms

        /// <summary>Menu / quiz: the board stands at z 1.2 in front of the learner (origin, facing +z).</summary>
        private static void Menu(Transform root)
        {
            HideFlatFloor();
            Shell(root, -4f, 4f, -3.5f, 5f, 3.3f);
            // the board hides the wall behind it up to ~4 m: the banner heads the board itself, posters stand aside
            BoardBanner("MainMenuCanvas", "MenuPanel/Title", "MenuPanel/Status", "EF_Banner_TrainingCentre");
            Sign(root, "Poster_PASS", "EF_Poster_PASS", new Vector3(-3.15f, 1.55f, 4.97f), Vector3.back, 0.75f, 1.0f);
            Sign(root, "Poster_114", "EF_Poster_114", new Vector3(3.15f, 1.55f, 4.97f), Vector3.back, 0.75f, 1.0f);
            Sign(root, "Poster_Triangle", "EF_Poster_Triangle", new Vector3(3.97f, 1.55f, 2.2f), Vector3.left, 0.75f, 1.0f);
            Sign(root, "Poster_4TaiCho", "EF_Poster_4TaiCho", new Vector3(3.97f, 1.55f, 0.6f), Vector3.left, 0.75f, 1.0f);
            Sign(root, "EscapePlan", "EF_Sign_EscapePlan", new Vector3(3.97f, 1.55f, -1.1f), Vector3.left, 0.9f, 0.68f);
            HoseCabinet(root, new Vector3(-3.92f, 0f, 2.0f), Vector3.right);
            ExitDoor(root, new Vector3(-1.5f, 0f, -3.48f), Vector3.forward);
            Decor(root, new[] { new Vector3(-3.4f, 0f, 4.4f), new Vector3(3.4f, 0f, 4.4f), new Vector3(3.4f, 0f, -2.9f) },
                new[] { (new Vector3(-3.5f, 0f, -0.5f), Vector3.right) });
        }

        /// <summary>AAR: the result board at z 1.4, the holo table on the left, the skill radar on the right.</summary>
        private static void Debrief(Transform root)
        {
            HideFlatFloor();
            Shell(root, -4.5f, 4.5f, -3.5f, 5f, 3.3f);
            Opaque("AARCanvas");
            Sign(root, "Banner", "EF_Banner_Debrief", new Vector3(-4.47f, 2.45f, 1.6f), Vector3.right, 3.2f, 0.8f);
            Sign(root, "Poster_PASS", "EF_Poster_PASS", new Vector3(-4.47f, 1.3f, 2.6f), Vector3.right, 0.6f, 0.8f);
            Sign(root, "Poster_114", "EF_Poster_114", new Vector3(-4.47f, 1.3f, 0.6f), Vector3.right, 0.6f, 0.8f);
            Sign(root, "Poster_Triangle", "EF_Poster_Triangle", new Vector3(4.47f, 1.55f, 3.0f), Vector3.left, 0.75f, 1.0f);
            Sign(root, "Poster_4TaiCho", "EF_Poster_4TaiCho", new Vector3(4.47f, 1.55f, -0.6f), Vector3.left, 0.75f, 1.0f);
            ExitDoor(root, new Vector3(1.6f, 0f, -3.48f), Vector3.forward);
            Decor(root, new[] { new Vector3(-3.9f, 0f, 4.4f), new Vector3(3.9f, 0f, 4.4f) },
                new[] { (new Vector3(-4.0f, 0f, -2.0f), Vector3.right) });
        }

        /// <summary>Lesson room (12 x 10 m, walls already there): finishes, banner and posters, no new colliders.</summary>
        private static void Lesson(Transform root)
        {
            float x0 = -6f, x1 = 6f, z0 = -5f, z1 = 5f, h = 3.2f;
            Floor(root, x0, x1, z0, z1);
            BuildingDressing.CeilingInto(root, "Lesson", x0, x1, z0, z1, h, new[] { new Vector2(-3f, 2.5f), new Vector2(3f, 2.5f), new Vector2(-3f, -2f), new Vector2(3f, -2f) });
            Trim(root, x0, x1, z0, z1);
            Sign(root, "Banner", "EF_Banner_Lesson", new Vector3(5.97f, 2.5f, 0f), Vector3.left, 3.6f, 0.9f);
            Sign(root, "Poster_4TaiCho", "EF_Poster_4TaiCho", new Vector3(5.97f, 1.4f, 2.6f), Vector3.left, 0.6f, 0.8f);
            Sign(root, "Poster_114", "EF_Poster_114", new Vector3(5.97f, 1.4f, -2.6f), Vector3.left, 0.6f, 0.8f);
            Sign(root, "EscapePlan", "EF_Sign_EscapePlan", new Vector3(1.5f, 1.6f, -4.97f), Vector3.forward, 0.9f, 0.68f);
            Sign(root, "Poster_PASS", "EF_Poster_PASS", new Vector3(3.2f, 1.5f, -4.97f), Vector3.forward, 0.6f, 0.8f);
            Decor(root, new[] { new Vector3(5.4f, 0f, 4.4f), new Vector3(5.4f, 0f, -4.4f) }, new (Vector3, Vector3)[0]);
            Lamp(root, new Vector3(0f, 2.9f, 1.5f), 9f, 1.0f);
            Lamp(root, new Vector3(0f, 2.9f, -2.5f), 9f, 0.8f);
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>Makes a world canvas' background opaque (the room behind no longer shows through the text).</summary>
        private static UnityEngine.UI.Image Opaque(string canvas)
        {
            var c = GameObject.Find(canvas);
            var bg = c != null ? c.transform.Find("Background")?.GetComponent<UnityEngine.UI.Image>() : null;
            if (bg != null) bg.color = new Color(0.07f, 0.1f, 0.15f, 0.97f);
            return bg;
        }

        /// <summary>Replaces a board's text title by the banner image at the top of the board.</summary>
        private static void BoardBanner(string canvas, string titlePath, string statusPath, string texture)
        {
            Opaque(canvas);
            var c = GameObject.Find(canvas);
            if (c == null) return;
            var title = c.transform.Find(titlePath);
            if (title != null) title.gameObject.SetActive(false);
            var parent = title != null ? title.parent : c.transform;
            var old = parent.Find("BannerImage");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject("BannerImage", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchoredPosition = new Vector2(0f, 385f);
            rt.sizeDelta = new Vector2(960f, 210f);
            var img = go.GetComponent<UnityEngine.UI.RawImage>();
            img.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BuildingDressing.SIGNS + texture + ".png");
            img.raycastTarget = false;
            var status = c.transform.Find(statusPath) as RectTransform;
            if (status != null) status.anchoredPosition = new Vector2(0f, 228f);
        }

        private static Material M(string name, Color c, float smooth = 0.3f, float metal = 0f) => BuildingBuilder.Mat(name, c, smooth, metal);

        private static void HideFlatFloor()
        {
            // the old plane stays as the walking surface (collider), the wooden floor is drawn on top
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null && r.GetComponent<MeshFilter>().sharedMesh.name == "Plane")
                    r.enabled = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.77f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.58f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.5f, 0.45f);
        }

        /// <summary>A closed room: floor, walls with colliders, ceiling with light panels, trim, lamps.</summary>
        private static void Shell(Transform root, float x0, float x1, float z0, float z1, float h)
        {
            Floor(root, x0, x1, z0, z1);
            var wall = M("EF_CentreWall", new Color(0.93f, 0.91f, 0.86f));
            float t = 0.15f;
            Wall(root, "Wall_N", new Vector3((x0 + x1) / 2f, h / 2f, z1 + t / 2f), new Vector3(x1 - x0 + 2 * t, h, t), wall);
            Wall(root, "Wall_S", new Vector3((x0 + x1) / 2f, h / 2f, z0 - t / 2f), new Vector3(x1 - x0 + 2 * t, h, t), wall);
            Wall(root, "Wall_E", new Vector3(x1 + t / 2f, h / 2f, (z0 + z1) / 2f), new Vector3(t, h, z1 - z0), wall);
            Wall(root, "Wall_W", new Vector3(x0 - t / 2f, h / 2f, (z0 + z1) / 2f), new Vector3(t, h, z1 - z0), wall);
            BuildingBuilder.Box(root, "Slab", new Vector3((x0 + x1) / 2f, h + 0.1f, (z0 + z1) / 2f), Quaternion.identity, new Vector3(x1 - x0 + 0.4f, 0.2f, z1 - z0 + 0.4f), wall, false);
            BuildingDressing.CeilingInto(root, "Centre", x0, x1, z0, z1, h, new[] { new Vector2(-1.8f, 0.5f), new Vector2(1.8f, 0.5f), new Vector2(-1.8f, 3.5f), new Vector2(1.8f, 3.5f) });
            Trim(root, x0, x1, z0, z1);
            Lamp(root, new Vector3(-1.8f, h - 0.4f, 1.5f), 7f, 1.0f);
            Lamp(root, new Vector3(1.8f, h - 0.4f, 1.5f), 7f, 1.0f);
        }

        private static void Wall(Transform root, string name, Vector3 c, Vector3 size, Material m) =>
            BuildingBuilder.Box(root, name, c, Quaternion.identity, size, m, true);

        private static void Floor(Transform root, float x0, float x1, float z0, float z1)
        {
            var g = new GameObject("Floor").transform;
            g.SetParent(root, false);
            int nx = Mathf.CeilToInt((x1 - x0) / 2.5f), nz = Mathf.CeilToInt((z1 - z0) / 2.5f);
            float sx = (x1 - x0) / (nx * 2.5f), sz = (z1 - z0) / (nz * 2.5f);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++) // tile pivot: max-x / min-z corner
                    BuildingDressing.Place("Buildings/SM_Bld_Floor_Wood_01", new Vector3(x0 + (i + 1) * 2.5f * sx, 0.005f, z0 + j * 2.5f * sz), Quaternion.identity, g, new Vector3(sx, 1f, sz));
        }

        /// <summary>Two-tone walls: a painted dado band, a rail and a dark skirting along the four walls.</summary>
        private static void Trim(Transform root, float x0, float x1, float z0, float z1)
        {
            var g = new GameObject("Trim").transform;
            g.SetParent(root, false);
            var dado = M("EF_CentreDado", new Color(0.36f, 0.5f, 0.6f));
            var rail = M("EF_DadoRail", new Color(0.95f, 0.95f, 0.93f));
            var skirt = M("EF_Skirting", new Color(0.25f, 0.25f, 0.27f));
            var runs = new[]
            {
                (c: new Vector3((x0 + x1) / 2f, 0f, z1 - 0.006f), len: x1 - x0, o: Vector3.back),
                (c: new Vector3((x0 + x1) / 2f, 0f, z0 + 0.006f), len: x1 - x0, o: Vector3.forward),
                (c: new Vector3(x1 - 0.006f, 0f, (z0 + z1) / 2f), len: z1 - z0, o: Vector3.left),
                (c: new Vector3(x0 + 0.006f, 0f, (z0 + z1) / 2f), len: z1 - z0, o: Vector3.right),
            };
            foreach (var r in runs)
            {
                var q = Quaternion.LookRotation(r.o);
                BuildingBuilder.Box(g, "Dado", r.c + Vector3.up * 0.55f, q, new Vector3(r.len, 1.1f, 0.01f), dado, false);
                BuildingBuilder.Box(g, "Rail", r.c + Vector3.up * 1.1f + r.o * 0.01f, q, new Vector3(r.len, 0.05f, 0.025f), rail, false);
                BuildingBuilder.Box(g, "Skirting", r.c + Vector3.up * 0.05f + r.o * 0.01f, q, new Vector3(r.len, 0.1f, 0.025f), skirt, false);
            }
        }

        private static void Sign(Transform root, string name, string tex, Vector3 c, Vector3 outward, float w, float h) =>
            BuildingDressing.Sign(root, name, tex, c, outward, w, h, false);

        private static void Lamp(Transform root, Vector3 p, float range, float intensity)
        {
            var l = new GameObject("CentreLight").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.position = p;
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = new Color(1f, 0.95f, 0.86f);
            l.shadows = LightShadows.None;
        }

        /// <summary>Wall fire-hose cabinet (tủ chữa cháy vách tường): red box, glass front, coiled hose, label.</summary>
        private static void HoseCabinet(Transform root, Vector3 floorAtWall, Vector3 outward)
        {
            var g = new GameObject("FireHoseCabinet").transform;
            g.SetParent(root, false);
            Quaternion q = Quaternion.LookRotation(outward);
            Vector3 c = floorAtWall + Vector3.up * 1.15f + outward * 0.13f;
            BuildingBuilder.Box(g, "Body", c, q, new Vector3(0.8f, 1.1f, 0.25f), M("EF_HoseCabinetRed", new Color(0.75f, 0.08f, 0.07f), 0.5f), false);
            BuildingBuilder.Box(g, "Glass", c + outward * 0.13f + Vector3.down * 0.1f, q, new Vector3(0.66f, 0.7f, 0.01f), M("EF_WindowGlass", new Color(0.62f, 0.78f, 0.86f, 0.28f), 0.95f), false);
            var hose = M("EF_Hose", new Color(0.85f, 0.8f, 0.7f));
            for (int i = 0; i < 4; i++) // the coiled hose behind the glass: stacked rings
            {
                var ring = BuildingBuilder.Cylinder(g, "HoseCoil", Vector3.zero, Vector3.up * 0.01f, 0.52f - i * 0.1f, i % 2 == 0 ? hose : M("EF_HoseCabinetRed", Color.red));
                ring.transform.SetPositionAndRotation(c + outward * (0.02f + i * 0.012f) + Vector3.down * 0.1f, q * Quaternion.Euler(90f, 0f, 0f));
            }
            BuildingDressing.Sign(g, "Label", "EF_Label_HoseCabinet", c + Vector3.up * 0.42f + outward * 0.135f, outward, 0.5f, 0.25f, true);
            BuildingDressing.Place("Props/Wall Props/SM_Prop_Fire_Extinguisher_01", floorAtWall + outward * 0.1f + q * Vector3.right * 0.75f, q, g);
            BuildingDressing.Place("Props/Wall Props/SM_Prop_FireAlarm_Switch_01", floorAtWall + Vector3.up * 1.45f + q * Vector3.left * 0.65f, q, g);
        }

        private static void ExitDoor(Transform root, Vector3 sill, Vector3 inward)
        {
            var g = new GameObject("ExitDoor").transform;
            g.SetParent(root, false);
            Quaternion q = Quaternion.LookRotation(inward);
            BuildingBuilder.Box(g, "Leaf", sill + Vector3.up * 1.05f + inward * 0.02f, q, new Vector3(1.0f, 2.1f, 0.05f), M("EF_FireDoor", new Color(0.55f, 0.12f, 0.1f), 0.4f, 0.3f), false);
            var frame = M("EF_DoorFrame", new Color(0.3f, 0.31f, 0.33f), 0.5f, 0.6f);
            foreach (float s in new[] { -1f, 1f })
                BuildingBuilder.Box(g, "Jamb", sill + q * new Vector3(s * 0.54f, 1.07f, 0.04f), q, new Vector3(0.08f, 2.15f, 0.1f), frame, false);
            BuildingBuilder.Box(g, "Head", sill + q * new Vector3(0f, 2.17f, 0.04f), q, new Vector3(1.16f, 0.08f, 0.1f), frame, false);
            BuildingBuilder.Box(g, "Bar", sill + q * new Vector3(0.1f, 1.0f, 0.09f), q, new Vector3(0.7f, 0.05f, 0.05f), M("EF_Rail", new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.9f), false);
            BuildingDressing.Sign(g, "ExitSign", "EF_Sign_Exit_Down", sill + Vector3.up * 2.5f + inward * 0.04f, inward, 0.7f, 0.26f, true);
        }

        private static void Decor(Transform root, Vector3[] plants, (Vector3 at, Vector3 facing)[] benches)
        {
            var rng = new System.Random(3);
            var g = new GameObject("Decor").transform;
            g.SetParent(root, false);
            foreach (var p in plants)
                BuildingDressing.Place(rng.Next(2) == 0 ? "Props/Misc/SM_Prop_Plant_14" : "Props/Misc/SM_Prop_Plant_06", p, Quaternion.Euler(0f, rng.Next(360), 0f), g);
            foreach (var b in benches)
                BuildingDressing.Place("Props/Exterior/SM_Prop_Benchseat_01", b.at, Quaternion.LookRotation(b.facing), g);
        }
    }
}
