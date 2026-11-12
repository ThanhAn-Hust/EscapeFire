using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Lesson;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/01_Lesson.unity (plan V1.4): an interactive lesson room with five stations, each a sourced
    /// PCCC rule + a hands-on practice with the VR hands + a fingertip quiz. Uses the same XR rig and the same
    /// realistic interactions as the lab (RigRealismSetup). Regenerates the scene from scratch (idempotent).
    /// </summary>
    public static class LessonRoomBuilder
    {
        public const string PATH = "Assets/Scenes/" + SceneNames.LESSON + ".unity";
        private const string XR_RIG = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string EXTINGUISHER = "Assets/PolygonOffice/Prefabs/Props/Wall Props/SM_Prop_Fire_Extinguisher_01.prefab";
        private const string FIRE = "Assets/Real Fire & Smoke/Prefabs/Fire Low-Res/Fire/Fire.prefab";
        private const string FONT = "Assets/TextMesh Pro/Resources/Fonts & Materials/EF_Vietnamese SDF.asset";
        private const float TableTop = 0.9f;

        private static TMP_FontAsset _font;

        [MenuItem("EscapeFire/V1/Build Lesson Room")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);

            Room();
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(XR_RIG));
            rig.transform.SetPositionAndRotation(new Vector3(0f, 0f, -1f), Quaternion.identity);

            var stations = new List<LessonStation>
            {
                Station1_FireTriangle(new Vector3(-4f, 0f, 4.4f)),
                Station2_RightAgent(new Vector3(-1.35f, 0f, 4.4f)),
                Station3_Pass(new Vector3(1.35f, 0f, 4.4f)),
                Station4_Alarm(new Vector3(4f, 0f, 4.4f)),
                Station5_StayLow()
            };

            var controllerGo = new GameObject("[Lesson]");
            var controller = controllerGo.AddComponent<LessonController>();
            var progress = Text(null, "ProgressBoard", new Vector3(0f, 2.15f, -4.95f), Quaternion.Euler(0f, 180f, 0f), new Vector2(3.2f, 0.9f), 0.9f);
            Table("CompletionTable", new Vector3(0f, 0f, -4.45f), new Vector3(1.0f, TableTop, 0.5f));
            var next = Button("ContinueButton", new Vector3(0f, TableTop + 0.015f, -4.45f), "SANG THỰC HÀNH", new Color(0.2f, 0.7f, 0.3f), null, 180f);
            var poke = next.AddComponent<PokeAction>();
            UnityEventTools.AddVoidPersistentListener(next.GetComponent<XRSimpleInteractable>().selectEntered, poke.Press);
            UnityEventTools.AddVoidPersistentListener(poke.OnPressed, controller.GoToGuidedDrill);
            // V5.1: always a way back to the menu
            var back = Button("BackToMenuButton", new Vector3(0.35f, TableTop + 0.015f, -4.45f), "VỀ MENU", new Color(0.4f, 0.4f, 0.45f), null, 180f);
            var backPoke = back.AddComponent<PokeAction>();
            UnityEventTools.AddVoidPersistentListener(back.GetComponent<XRSimpleInteractable>().selectEntered, backPoke.Press);
            UnityEventTools.AddVoidPersistentListener(backPoke.OnPressed, controller.BackToMenu);
            controller.Configure(stations.ToArray(), progress, next);

            var log = new List<string>();
            RigRealismSetup.Apply(log);
            VietnameseFontSetup.AssignInOpenScene(VietnameseFontSetup.GetOrCreate());
            EditorSceneManager.SaveScene(scene, PATH);
            AddToBuildSettings();
            Debug.Log("[EscapeFire] Lesson room built: " + string.Join("; ", log));
        }

        // ------------------------------------------------------------------ stations

        private static LessonStation Station1_FireTriangle(Vector3 p)
        {
            var root = new GameObject("Station1_FireTriangle");
            var (board, feedback, lamp) = Board(root.transform, p);
            Table("Table", p + new Vector3(0f, 0f, -0.4f), new Vector3(1.2f, TableTop, 0.6f), root.transform);
            Vector3 trayPos = p + new Vector3(-0.15f, TableTop + 0.025f, -0.4f);
            var tray = Prim(PrimitiveType.Cylinder, "Tray", root.transform, trayPos, new Vector3(0.24f, 0.025f, 0.24f), Mat("EF_TrayMetal", new Color(0.35f, 0.35f, 0.37f), 0.6f, 0.8f));
            var fire = SmallFire("TrayFire", root.transform, trayPos + Vector3.up * 0.03f, 0.25f, 30f);
            var lid = Prim(PrimitiveType.Cylinder, "Lid", root.transform, p + new Vector3(0.3f, TableTop + 0.01f, -0.45f), new Vector3(0.28f, 0.006f, 0.28f), Mat("EF_LidMetal", new Color(0.7f, 0.7f, 0.72f), 0.7f, 0.9f));
            Prim(PrimitiveType.Cylinder, "Knob", lid.transform, lid.transform.position + Vector3.up * 0.02f, new Vector3(0.04f, 0.015f, 0.04f), Mat("EF_LeverBlack", new Color(0.05f, 0.05f, 0.05f)));
            Grabbable(lid, 0.4f);
            var check = root.AddComponent<TrayLidCheck>();
            check.Configure(lid.transform, tray.transform, fire);
            return Station(root, p, board, feedback, lamp, check,
                "1. Tam giác cháy",
                "Đám cháy cần đủ ba yếu tố: CHẤT CHÁY, NHIỆT và OXY. Loại bỏ một yếu tố là lửa tắt. Bình CO2 làm lửa tắt vì đẩy oxy ra và làm lạnh.",
                "R12_FIRE_TRIANGLE",
                "Đậy nắp làm lửa tắt vì đã loại bỏ yếu tố nào?",
                new[] { "Nhiệt", "Oxy", "Chất cháy" }, 1,
                "Nắp kín ngăn không khí, lửa thiếu oxy nên tắt.");
        }

        private static LessonStation Station2_RightAgent(Vector3 p)
        {
            var root = new GameObject("Station2_RightAgent");
            var (board, feedback, lamp) = Board(root.transform, p);
            Table("Table", p + new Vector3(0f, 0f, -0.4f), new Vector3(1.2f, TableTop, 0.6f), root.transform);
            // "Electrical fire" stand with a socket for the chosen extinguisher.
            Vector3 standPos = p + new Vector3(0.35f, TableTop, -0.4f);
            Prim(PrimitiveType.Cube, "Stand", root.transform, standPos + Vector3.up * 0.01f, new Vector3(0.28f, 0.02f, 0.28f), Mat("EF_StandRed", new Color(0.6f, 0.15f, 0.1f)));
            Text(root.transform, "StandLabel", standPos + new Vector3(0f, 0.05f, 0.16f), Quaternion.Euler(60f, 0f, 0f), new Vector2(0.4f, 0.1f), 0.25f).text = "TỦ ĐIỆN ĐANG CHÁY";
            var socketGo = new GameObject("Socket");
            socketGo.transform.SetParent(root.transform, false);
            socketGo.transform.position = standPos + Vector3.up * 0.2f;
            var sc = socketGo.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.18f;
            var socket = socketGo.AddComponent<XRSocketInteractor>();
            var specs = new (ExtinguisherType type, string name, Color band)[]
            {
                (ExtinguisherType.WaterFoam, "NƯỚC", new Color(0.2f, 0.45f, 0.95f)),
                (ExtinguisherType.DryPowder, "BỌT / BỘT", new Color(0.95f, 0.85f, 0.2f)),
                (ExtinguisherType.CO2, "CO2", new Color(0.05f, 0.05f, 0.05f))
            };
            for (int i = 0; i < specs.Length; i++)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EXTINGUISHER), root.transform);
                model.name = "Mini_" + specs[i].type;
                model.transform.position = p + new Vector3(-0.45f + i * 0.17f, TableTop, -0.35f);
                model.transform.localScale *= 0.45f;
                Prim(PrimitiveType.Cylinder, "Band", model.transform, model.transform.position + Vector3.up * 0.18f, new Vector3(0.075f, 0.02f, 0.075f), Mat("EF_Band_" + specs[i].type, specs[i].band));
                var label = model.AddComponent<AgentLabel>();
                label.Type = specs[i].type;
                label.DisplayName = specs[i].name;
                FitBox(model);
                Grabbable(model, 1.5f);
                Text(root.transform, "Label_" + specs[i].type, p + new Vector3(-0.45f + i * 0.17f, TableTop + 0.005f, -0.55f), Quaternion.Euler(90f, 0f, 0f), new Vector2(0.17f, 0.06f), 0.2f).text = specs[i].name;
            }
            var check = root.AddComponent<SocketAgentCheck>();
            check.Configure(socket, ExtinguisherType.CO2);
            return Station(root, p, board, feedback, lamp, check,
                "2. Chọn đúng bình",
                "Cháy thiết bị điện: KHÔNG dùng nước hay bọt (dẫn điện, gây giật). Dùng bình CO2 (hoặc bột phù hợp) và ngắt điện trước khi dập.",
                "R03_RIGHT_AGENT",
                "Tủ server đang có điện bốc cháy. Việc đầu tiên nên làm là gì?",
                new[] { "Xịt nước ngay", "Ngắt Aptomat rồi dùng CO2", "Mở cửa sổ cho thoáng" }, 1,
                "Ngắt nguồn trước, rồi dùng chất chữa cháy không dẫn điện.");
        }

        private static LessonStation Station3_Pass(Vector3 p)
        {
            var root = new GameObject("Station3_PASS");
            var (board, feedback, lamp) = Board(root.transform, p);
            Table("Table", p + new Vector3(0f, 0f, -0.4f), new Vector3(1.2f, TableTop, 0.6f), root.transform);
            // Training CO2 extinguisher on a wall hook by the board, same realistic setup as in the lab.
            var ext = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EXTINGUISHER), root.transform);
            ext.name = "Training_CO2";
            ext.transform.SetPositionAndRotation(p + new Vector3(0.75f, 0.55f, 0.35f), Quaternion.identity);
            FitBox(ext);
            var rb = ext.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            ext.AddComponent<XRGrabInteractable>();
            var fe = ext.AddComponent<FireExtinguisher>();
            var so = new SerializedObject(fe);
            so.FindProperty("type").enumValueIndex = (int)ExtinguisherType.CO2;
            so.ApplyModifiedPropertiesWithoutUndo();
            ext.AddComponent<ExtinguisherXRBinding>();
            // Training fire 2.4 m out from the wall, in a steel pan on the floor.
            Vector3 panPos = p + new Vector3(0f, 0.04f, -2.6f);
            Prim(PrimitiveType.Cylinder, "FirePan", root.transform, panPos, new Vector3(0.6f, 0.04f, 0.6f), Mat("EF_TrayMetal", new Color(0.35f, 0.35f, 0.37f), 0.6f, 0.8f));
            var fire = SmallFire("TrainingFire", root.transform, panPos + Vector3.up * 0.05f, 0.6f, 40f);
            var check = root.AddComponent<FireOutCheck>();
            check.Configure(fire, null);
            return Station(root, p, board, feedback, lamp, check,
                "3. Kỹ thuật PASS",
                "P – Rút chốt (tay kia giữ bình). A – Chĩa loa vào GỐC lửa. S – Bóp cò. S – Quét qua lại. Đứng cách khoảng 1,8–2,4 m. Bình CO2 5 kg chỉ phun được ~10–12 giây.",
                "R04_PASS",
                "Khi dập lửa phải chĩa loa phun vào đâu?",
                new[] { "Ngọn lửa trên cao", "Gốc lửa", "Khói phía trên" }, 1,
                "Chất chữa cháy phải phủ vào gốc, nơi chất cháy đang cháy.");
        }

        private static LessonStation Station4_Alarm(Vector3 p)
        {
            var root = new GameObject("Station4_Alarm");
            var (board, feedback, lamp) = Board(root.transform, p);
            Table("Table", p + new Vector3(0f, 0f, -0.4f), new Vector3(1.2f, TableTop, 0.6f), root.transform);
            // Manual call point on the wall next to the board.
            var cp = Prim(PrimitiveType.Cube, "CallPoint", root.transform, new Vector3(p.x + 0.8f, 1.4f, 4.9f), new Vector3(0.12f, 0.12f, 0.06f), Mat("EF_CallPointRed", new Color(0.8f, 0.05f, 0.05f)));
            cp.AddComponent<BoxCollider>();
            var alarm = cp.AddComponent<FireAlarmButton>();
            var si = cp.AddComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(si.selectEntered, alarm.PushAlarmButton);
            Text(root.transform, "CallPointLabel", new Vector3(p.x + 0.8f, 1.53f, 4.88f), Quaternion.identity, new Vector2(0.3f, 0.06f), 0.2f).text = "ẤN KHI CÓ CHÁY";
            var check = root.AddComponent<CallPointCheck>();
            check.Configure(alarm);
            return Station(root, p, board, feedback, lamp, check,
                "4. Báo cháy và gọi 114",
                "Phát hiện cháy: hô to \"Cháy! Cháy!\", ấn nút báo cháy, gọi 114. Khi gọi nói rõ: địa chỉ, tầng, cái gì đang cháy, có người mắc kẹt không.",
                "R01_ALARM_EARLY",
                "Khi gọi 114 cần nói những gì?",
                new[] { "Chỉ nói \"có cháy\"", "Địa chỉ, tầng, đang cháy gì, có người kẹt không", "Tên và số điện thoại" }, 1,
                "Lực lượng chữa cháy cần biết nơi nào, cháy gì, có người mắc kẹt không để xuất phát đúng.");
        }

        private static LessonStation Station5_StayLow()
        {
            var root = new GameObject("Station5_StayLow");
            Vector3 entry = new Vector3(-4.5f, 0f, -1.0f), exit = new Vector3(-4.5f, 0f, -4.0f);
            // Low walls along the corridor and a smoke layer from 1.2 m to the ceiling.
            var wallMat = Mat("EF_LessonWall", new Color(0.78f, 0.78f, 0.76f));
            Prim(PrimitiveType.Cube, "CorridorWall_E", root.transform, new Vector3(-3.45f, 1.5f, -2.5f), new Vector3(0.1f, 3f, 3.0f), wallMat).AddComponent<BoxCollider>();
            var smoke = Prim(PrimitiveType.Cube, "SmokeLayer", root.transform, new Vector3(-4.5f, 2.1f, -2.5f), new Vector3(2.0f, 1.8f, 3.0f), SmokeMat());
            var check = root.AddComponent<SmokeCorridorCheck>();
            check.Configure(entry, exit, 2.0f, 1.2f);
            Vector3 p = new Vector3(-3.0f, 0f, -4.4f);
            var (board, feedback, lamp) = Board(root.transform, new Vector3(-5.95f, 0f, -0.2f), Quaternion.Euler(0f, -90f, 0f));
            Table("Table", p, new Vector3(0.6f, TableTop, 1.2f), root.transform);
            return Station(root, p, board, feedback, lamp, check,
                "5. Thoát nạn trong khói",
                "Khói và khí nóng bốc lên cao, không khí sạch ở sát sàn. Cúi thấp hoặc bò, che mũi miệng bằng khăn ướt. KHÔNG dùng thang máy; đi cầu thang bộ theo biển EXIT. Sờ cửa bằng mu bàn tay trước khi mở.",
                "R07_STAY_LOW",
                "Hành lang có khói, tầng của bạn có thang máy và cầu thang bộ. Bạn đi lối nào?",
                new[] { "Thang máy cho nhanh", "Cầu thang bộ, cúi thấp", "Chạy thẳng người qua khói" }, 1,
                "Thang máy có thể mất điện và kẹt; khói ở trên cao nên phải cúi thấp.", sideways: true);
        }

        // ------------------------------------------------------------------ building blocks

        private static LessonStation Station(GameObject root, Vector3 tablePos, TMP_Text board, TMP_Text feedback, Renderer lamp, MonoBehaviour check,
            string title, string text, string rule, string q, string[] options, int correct, string why, bool sideways = false)
        {
            var st = root.AddComponent<LessonStation>();
            st.Configure(title, text, rule, q, options, correct, why, check, board, feedback, lamp);
            for (int i = 0; i < options.Length; i++)
            {
                Vector3 bp = sideways
                    ? tablePos + new Vector3(0.05f, TableTop + 0.015f, 0.3f - i * 0.3f) // viewer faces +X: A on the left (+Z)
                    : tablePos + new Vector3(-0.3f + i * 0.3f, TableTop + 0.015f, -0.55f);
                var btn = Button($"Answer_{(char)('A' + i)}", bp, ((char)('A' + i)).ToString(), new Color(0.15f, 0.35f, 0.75f), root.transform, sideways ? 90f : 0f);
                var qb = btn.AddComponent<LessonQuizButton>();
                qb.Configure(st, i);
                UnityEventTools.AddVoidPersistentListener(btn.GetComponent<XRSimpleInteractable>().selectEntered, qb.Press);
            }
            return st;
        }

        private static (TMP_Text board, TMP_Text feedback, Renderer lamp) Board(Transform parent, Vector3 p, Quaternion? rot = null)
        {
            Quaternion r = rot ?? Quaternion.identity;
            Vector3 wall = rot.HasValue ? p : new Vector3(p.x, 0f, 4.97f);
            Vector3 fwdOut = r * Vector3.back; // towards the learner
            Prim(PrimitiveType.Cube, "BoardPanel", parent, wall + Vector3.up * 1.75f - fwdOut * 0.0f, r * new Vector3(1.25f, 1.1f, 0.02f), Mat("EF_BoardDark", new Color(0.08f, 0.1f, 0.13f)), r);
            var board = Text(parent, "BoardText", wall + Vector3.up * 1.78f + fwdOut * 0.015f, r, new Vector2(1.15f, 1.0f), 0.62f);
            board.alignment = TextAlignmentOptions.TopLeft;
            var feedback = Text(parent, "Feedback", wall + Vector3.up * 1.12f + fwdOut * 0.015f, r, new Vector2(1.2f, 0.18f), 0.32f);
            var lamp = Prim(PrimitiveType.Sphere, "Lamp", parent, wall + Vector3.up * 2.4f + fwdOut * 0.05f, Vector3.one * 0.09f, Mat("EF_LampOff", new Color(0.3f, 0.3f, 0.3f)));
            return (board, feedback, lamp.GetComponent<Renderer>());
        }

        private static GameObject Button(string name, Vector3 pos, string label, Color color, Transform parent = null, float viewerYaw = 0f)
        {
            var b = Prim(PrimitiveType.Cube, name, parent, pos, new Vector3(0.11f, 0.03f, 0.11f), Mat("EF_Btn_" + ColorUtility.ToHtmlStringRGB(color), color));
            b.AddComponent<BoxCollider>();
            var si = b.AddComponent<XRSimpleInteractable>();
            RigRealismSetup.MakePokeable(si, Vector3.down, 0.009f);
            // Lying on the table, readable for someone facing viewerYaw (0 = north wall stations).
            var t = Text(parent, name + "_Label", pos + new Vector3(0f, 0.02f, 0f), Quaternion.Euler(90f, viewerYaw, 0f), new Vector2(0.3f, 0.1f), 0.35f);
            t.text = $"<b>{label}</b>";
            return b;
        }

        private static FireHazard SmallFire(string name, Transform parent, Vector3 pos, float scale, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var holder = new GameObject("FlameScale");
            holder.transform.SetParent(go.transform, false);
            holder.transform.localScale = Vector3.one * scale;
            var flames = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FIRE), holder.transform);
            flames.transform.localPosition = Vector3.zero;
            foreach (var ps in flames.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            var fire = go.AddComponent<FireHazard>();
            var so = new SerializedObject(fire);
            so.FindProperty("initialIntensity").floatValue = intensity;
            so.FindProperty("spreadRate").floatValue = 0.5f;
            var rootPs = flames.GetComponent<ParticleSystem>();
            so.FindProperty("fireParticles").objectReferenceValue = rootPs != null ? rootPs : flames.GetComponentInChildren<ParticleSystem>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return fire;
        }

        private static void Grabbable(GameObject go, float mass)
        {
            if (go.GetComponent<Collider>() == null) go.AddComponent<BoxCollider>();
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            var g = go.AddComponent<XRGrabInteractable>();
            g.movementType = XRBaseInteractable.MovementType.Instantaneous;
            g.throwOnDetach = false;
            RigRealismSetup.EnsureNearOnly(g, 0.15f);
        }

        private static void FitBox(GameObject go)
        {
            bool any = false;
            Bounds b = new Bounds(go.transform.position, Vector3.one * 0.2f);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            var box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(b.center);
            Vector3 s = go.transform.lossyScale;
            box.size = new Vector3(b.size.x / Mathf.Max(1e-4f, s.x), b.size.y / Mathf.Max(1e-4f, s.y), b.size.z / Mathf.Max(1e-4f, s.z));
        }

        private static void Room()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.75f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.35f, 0.35f);
            var floorMat = Mat("EF_LessonFloor", new Color(0.62f, 0.64f, 0.68f));
            var wallMat = Mat("EF_LessonWall", new Color(0.78f, 0.78f, 0.76f));
            Prim(PrimitiveType.Cube, "Floor", null, new Vector3(0f, -0.05f, 0f), new Vector3(12f, 0.1f, 10f), floorMat).AddComponent<BoxCollider>();
            Prim(PrimitiveType.Cube, "Ceiling", null, new Vector3(0f, 3.25f, 0f), new Vector3(12f, 0.1f, 10f), wallMat);
            Prim(PrimitiveType.Cube, "Wall_N", null, new Vector3(0f, 1.6f, 5.05f), new Vector3(12f, 3.2f, 0.1f), wallMat).AddComponent<BoxCollider>();
            Prim(PrimitiveType.Cube, "Wall_S", null, new Vector3(0f, 1.6f, -5.05f), new Vector3(12f, 3.2f, 0.1f), wallMat).AddComponent<BoxCollider>();
            Prim(PrimitiveType.Cube, "Wall_E", null, new Vector3(6.05f, 1.6f, 0f), new Vector3(0.1f, 3.2f, 10f), wallMat).AddComponent<BoxCollider>();
            Prim(PrimitiveType.Cube, "Wall_W", null, new Vector3(-6.05f, 1.6f, 0f), new Vector3(0.1f, 3.2f, 10f), wallMat).AddComponent<BoxCollider>();
            for (int i = -1; i <= 1; i++)
            {
                var l = new GameObject("CeilingLight").AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 9f;
                l.intensity = 1.2f;
                l.transform.position = new Vector3(i * 3.5f, 3f, 0.5f);
            }
        }

        private static void Table(string name, Vector3 p, Vector3 size, Transform parent = null)
        {
            var top = Prim(PrimitiveType.Cube, name, parent, new Vector3(p.x, size.y - 0.025f, p.z), new Vector3(size.x, 0.05f, size.z), Mat("EF_TableTop", new Color(0.55f, 0.42f, 0.3f)));
            top.AddComponent<BoxCollider>();
            Prim(PrimitiveType.Cube, name + "_Base", parent, new Vector3(p.x, (size.y - 0.05f) / 2f, p.z), new Vector3(size.x * 0.9f, size.y - 0.05f, size.z * 0.8f), Mat("EF_TableBase", new Color(0.3f, 0.3f, 0.32f)));
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, rot ?? Quaternion.identity);
            Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = type == PrimitiveType.Cylinder
                ? new Vector3(size.x / ps.x, size.y / ps.y, size.z / ps.z) // cylinder height is 2 units: y is half-height
                : new Vector3(size.x / ps.x, size.y / ps.y, size.z / ps.z);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static TMP_Text Text(Transform parent, string name, Vector3 pos, Quaternion rot, Vector2 size, float fontSize)
        {
            // Create with a RectTransform from the start: converting Transform -> RectTransform when TMP is added
            // drops the x/y position on save.
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.SetPositionAndRotation(pos, rot);
            var t = go.AddComponent<TextMeshPro>();
            rt.sizeDelta = size;
            rt.SetPositionAndRotation(pos, rot);
            t.fontSize = fontSize;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.color = Color.white;
            if (_font != null) t.font = _font;
            return t;
        }

        private static Material Mat(string name, Color c, float smooth = 0.3f, float metal = 0f)
        {
            string path = "Assets/Settings/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material SmokeMat()
        {
            string path = "Assets/Settings/EF_LessonSmoke.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.13f, 0.8f));
            RuntimeVisuals.MakeTransparent(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void AddToBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == PATH)) return;
            list.Insert(Mathf.Min(1, list.Count), new EditorBuildSettingsScene(PATH, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
