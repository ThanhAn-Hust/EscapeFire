using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Scenario;
using EscapeFire.UI;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// One-click scene setup for milestone M1 (menu: EscapeFire/...).
    /// Generates 00_MainMenu and 02_AAR_Evaluation, wires the lab scene, and fixes Build Settings order.
    /// Safe to re-run: generated scenes are overwritten, lab wiring only adds what is missing.
    /// </summary>
    public static class EscapeFireSceneBuilder
    {
        private const string SCENES_DIR = "Assets/Scenes";
        private const string XR_ORIGIN_PREFAB = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";
        internal static string MenuPath => $"{SCENES_DIR}/{SceneNames.MAIN_MENU}.unity";
        private static string LabPath => $"{SCENES_DIR}/{SceneNames.LAB}.unity";
        internal static string AARPath => $"{SCENES_DIR}/{SceneNames.AAR}.unity";

        [MenuItem("EscapeFire/M1/Build Everything (Menu + AAR + Wire Lab + Build Settings)")]
        public static void BuildEverything()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMainMenuScene();
            BuildAARScene();
            WireLabScene();
            ApplyBuildSettings();
            EditorSceneManager.OpenScene(MenuPath);
            Debug.Log("[EscapeFire] M1 scenes built. Press Play in 00_MainMenu to run the full loop.");
        }

        [MenuItem("EscapeFire/M1/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEnvironment("Lobby (5m x 5m)", new Color(0.82f, 0.80f, 0.76f));
            var canvas = CreateWorldCanvas("MainMenuCanvas", new Vector3(0f, 1.45f, 1.2f), new Vector2(1000f, 860f));

            var controllerGo = new GameObject("[MainMenu]");
            var menu = controllerGo.AddComponent<MainMenuController>();

            CreateText(canvas, "Title", "DIỄN TẬP PCCC THỰC TẾ ẢO\nPhòng Lab Máy tính", 54, new Vector2(0f, 330f), new Vector2(950f, 150f));
            var status = CreateText(canvas, "Status", "", 32, new Vector2(0f, 190f), new Vector2(950f, 110f));
            SetPrivateField(menu, "statusText", status);

            CreateButton(canvas, "XÁC NHẬN CHIỀU CAO (đứng thẳng)", new Vector2(0f, 65f), menu.CalibrateHeight);
            CreateButton(canvas, "1. BÀI GIẢNG (bắt đầu từ đây)", new Vector2(0f, -30f), menu.StartLesson, 700f, new Color(0.15f, 0.45f, 0.75f));
            CreateButton(canvas, "2. CHẾ ĐỘ: CÓ HƯỚNG DẪN", new Vector2(-240f, -125f), menu.SelectGuidedMode, 440f);
            CreateButton(canvas, "3. CHẾ ĐỘ: SÁT HẠCH", new Vector2(240f, -125f), menu.SelectExamMode, 440f);
            CreateButton(canvas, "BẮT ĐẦU BÀI TẬP PHÒNG LAB", new Vector2(0f, -230f), menu.StartDrill, 700f, new Color(0.75f, 0.15f, 0.12f));
            CreateButton(canvas, "4. PHÒNG MỚI (sinh từ mô tả)", new Vector2(0f, -335f), menu.StartGeneratedRoom, 700f, new Color(0.45f, 0.25f, 0.65f));

            SaveScene(scene, MenuPath);
        }

        [MenuItem("EscapeFire/M1/Build AAR Scene")]
        public static void BuildAARScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEnvironment("Hologram Command Room", new Color(0.12f, 0.16f, 0.22f));
            var canvas = CreateWorldCanvas("AARCanvas", new Vector3(0f, 1.6f, 1.4f), new Vector2(1400f, 900f));

            var controllerGo = new GameObject("[AAR]");
            var aar = controllerGo.AddComponent<AARController>();

            var headline = CreateText(canvas, "Headline", "KẾT QUẢ DIỄN TẬP", 60, new Vector2(0f, 360f), new Vector2(1300f, 100f));
            var summary = CreateText(canvas, "Summary", "", 28, new Vector2(-330f, 90f), new Vector2(660f, 480f), TextAlignmentOptions.TopLeft);
            var log = CreateText(canvas, "ComplianceLog", "", 20, new Vector2(350f, 170f), new Vector2(640f, 300f), TextAlignmentOptions.TopLeft);
            var debrief = CreateText(canvas, "AIDebrief", "", 24, new Vector2(350f, -90f), new Vector2(640f, 200f), TextAlignmentOptions.TopLeft);
            debrief.color = new Color(0.6f, 0.9f, 1f);
            SetPrivateField(aar, "headlineText", headline);
            SetPrivateField(aar, "summaryText", summary);
            SetPrivateField(aar, "logText", log);
            SetPrivateField(aar, "debriefText", debrief);

            // Holographic mini-map table with the replayed path (red = standing in smoke).
            var table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "HoloTable";
            // V5.1: beside the learner, not between them and the result board (it blocked the buttons)
            table.transform.position = new Vector3(-1.4f, 0.4f, 0.5f);
            table.transform.localScale = new Vector3(1.4f, 0.4f, 1.4f);
            var map = new GameObject("TrajectoryMiniMap");
            map.transform.position = new Vector3(-1.4f, 0.81f, 0.5f);
            map.AddComponent<TrajectoryView>();

            var radar = new GameObject("SkillRadar");
            radar.transform.position = new Vector3(1.6f, 1.5f, 1.2f);
            radar.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            radar.AddComponent<RadarChartView>();

            CreateButton(canvas, "LÀM LẠI BÀI TẬP", new Vector2(-260f, -300f), aar.Retry, 480f);
            CreateButton(canvas, "VỀ MENU CHÍNH", new Vector2(260f, -300f), aar.BackToMenu, 480f);

            SaveScene(scene, AARPath);
        }

        [MenuItem("EscapeFire/M1/Wire Lab Scene")]
        public static void WireLabScene()
        {
            var scene = EditorSceneManager.OpenScene(LabPath, OpenSceneMode.Single);
            var report = new List<string>();

            var controller = Object.FindFirstObjectByType<LabProgressiveScenarioController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                report.Add("MISSING LabProgressiveScenarioController — add it to [SYSTEM] object manually.");
            }
            else if (controller.GetComponent<SessionOutcomeReporter>() == null)
            {
                controller.gameObject.AddComponent<SessionOutcomeReporter>();
                report.Add($"Added SessionOutcomeReporter to '{controller.name}'.");
            }

            foreach (var ext in Object.FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (ext.GetComponent<Rigidbody>() == null) ext.gameObject.AddComponent<Rigidbody>();
                if (ext.GetComponent<Collider>() == null) ext.gameObject.AddComponent<BoxCollider>();
                if (ext.GetComponent<XRGrabInteractable>() == null) ext.gameObject.AddComponent<XRGrabInteractable>();
                if (ext.GetComponent<ExtinguisherXRBinding>() == null)
                {
                    ext.gameObject.AddComponent<ExtinguisherXRBinding>();
                    report.Add($"Made '{ext.name}' grabbable (grip) and sprayable (trigger).");
                }
            }

            WireSimpleInteractable<CircuitBreakerSwitch>(c => c.TogglePower, report);
            WireSimpleInteractable<FireAlarmButton>(c => c.PushAlarmButton, report);
            WireSimpleInteractable<DoorHeatChecker>(c => c.CheckDoorHeatWithHand, report);

            if (Object.FindFirstObjectByType<EvacuationExitZone>(FindObjectsInactive.Include) == null)
            {
                var hallway = GameObject.Find("Hallway_Floor");
                var zone = new GameObject("EvacuationExitZone");
                var box = zone.AddComponent<BoxCollider>();
                box.isTrigger = true;
                if (hallway != null && hallway.TryGetComponent<Renderer>(out var r))
                {
                    zone.transform.position = r.bounds.center + Vector3.up * 1.25f;
                    box.size = new Vector3(r.bounds.size.x, 2.5f, r.bounds.size.z);
                    report.Add("Created EvacuationExitZone over 'Hallway_Floor'.");
                }
                else
                {
                    box.size = new Vector3(2f, 2.5f, 2f);
                    report.Add("Created EvacuationExitZone at origin — MOVE IT past the exit door (no 'Hallway_Floor' found).");
                }
                zone.AddComponent<EvacuationExitZone>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EscapeFire] Lab wiring:\n- " + string.Join("\n- ", report));
        }

        [MenuItem("EscapeFire/M2/Add Adaptive AI Director To Lab")]
        public static void AddAdaptiveDirector()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(LabPath, OpenSceneMode.Single);
            var report = new List<string>();

            var director = FindOrCreate("[AI_DIRECTOR_SYSTEM]");
            AddIfMissing<PerformanceBudgetGovernor>(director, report);
            AddIfMissing<CognitiveFrictionTracker>(director, report);
            AddIfMissing<FM200System>(director, report);
            AddIfMissing<MicroAIGovernor>(director, report);
            AddIfMissing<EdgeDirectorClient>(director, report);
            AddIfMissing<SessionFactsCollector>(director, report);
            report.Add("SET EdgeDirectorClient.serverUrl to ws://<laptop LAN IP>:8765/session (M3 edge server).");

            if (Object.FindFirstObjectByType<SecondaryFireSlot>(FindObjectsInactive.Include) == null)
            {
                var controller = Object.FindFirstObjectByType<LabProgressiveScenarioController>(FindObjectsInactive.Include);
                Vector3 origin = controller != null && controller.ServerFire != null
                    ? controller.ServerFire.transform.position
                    : Vector3.zero;
                CreateSlot("SecondaryFireSlot_ChemicalTray", origin + new Vector3(2.5f, 0f, 0f), "khay hóa chất tẩy mạch", report);
                CreateSlot("SecondaryFireSlot_CableDesk", origin + new Vector3(-2.5f, 0f, 1.5f), "bàn kỹ thuật đầy dây cáp", report);
                report.Add("CHECK: move the two SecondaryFireSlot objects onto real desks/trays (orange gizmos); never next to the exit door.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EscapeFire] M2 director:\n- " + string.Join("\n- ", report));
        }

        private static void AddIfMissing<T>(GameObject go, List<string> report) where T : Component
        {
            if (Object.FindFirstObjectByType<T>(FindObjectsInactive.Include) != null) return;
            go.AddComponent<T>();
            report.Add($"Added {typeof(T).Name} to {go.name}.");
        }

        private static void CreateSlot(string name, Vector3 position, string description, List<string> report)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var slot = go.AddComponent<SecondaryFireSlot>();
            var so = new SerializedObject(slot);
            so.FindProperty("description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
            report.Add($"Created {name} at {position}.");
        }

        [MenuItem("EscapeFire/M4/Add Socratic Voice Coach To Lab")]
        public static void AddVoiceCoach()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(LabPath, OpenSceneMode.Single);
            var report = new List<string>();

            var voice = FindOrCreate("[AI_MENTOR_VOICE]");
            if (voice.GetComponent<AudioSource>() == null) voice.AddComponent<AudioSource>();
            AddIfMissing<MentorVoice>(voice, report);
            AddIfMissing<DeterministicVoiceTriggerManager>(voice, report);
            AddIfMissing<VoiceCommandRouter>(voice, report);
            report.Add("Optional: add Overtone TTSPlayer + OvertoneSpeechOutput (Vietnamese voice) and the Frostweep " +
                       "GCSpeechRecognition prefab + GoogleCloudSpeechInput (API key) to [AI_MENTOR_VOICE].");
            report.Add("Optional: assign pre-recorded clips for the 5 Socratic lines in MentorVoice.recordedLines.");
            report.Add("Test without STT: in Play mode release key V (or left X button) → fallback transcript 'Cháy! Có cháy!'.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EscapeFire] M4 voice coach:\n- " + string.Join("\n- ", report));
        }

        [MenuItem("EscapeFire/Setup All (Menu + AAR + Lab + AI + Voice + Real Assets)")]
        public static void SetupAll()
        {
            BuildEverything();
            AddAdaptiveDirector();
            AddVoiceCoach();
            SimulatorSetup.Configure(); // keyboard/mouse testing without a headset
            LabSceneDresser.DressLab(); // real assets, grounding, smoke, HUD + runs the scene audit
            EditorSceneManager.OpenScene(MenuPath);
        }

        [MenuItem("EscapeFire/M1/Apply Build Settings")]
        public static void ApplyBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in new[] { MenuPath, LessonRoomBuilder.PATH, LabPath, AARPath })
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) scenes.Add(new EditorBuildSettingsScene(path, true));
                else Debug.LogWarning($"[EscapeFire] Scene not found, skipped in Build Settings: {path}");
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go : new GameObject(name);
        }

        private static void WireSimpleInteractable<T>(System.Func<T, UnityAction> action, List<string> report) where T : Component
        {
            foreach (var target in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (target.GetComponent<XRBaseInteractable>() != null) continue; // already interactive, respect manual setup
                if (target.GetComponent<Collider>() == null) target.gameObject.AddComponent<BoxCollider>();
                var interactable = target.gameObject.AddComponent<XRSimpleInteractable>();
                UnityAction call = action(target);
                // Poke/ray select → parameterless method; persistent so it is saved in the scene.
                UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, call);
                report.Add($"Added XRSimpleInteractable to '{target.name}' → {call.Method.Name}().");
            }
        }

        private static void CreateEnvironment(string floorName, Color floorColor)
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = floorName;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = floorColor };
            floor.GetComponent<Renderer>().sharedMaterial = mat;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XR_ORIGIN_PREFAB);
            if (prefab != null)
            {
                // V5.1: the learner must start facing the board (the prefab came in turned 130 degrees away)
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"[EscapeFire] XR Origin prefab not found at {XR_ORIGIN_PREFAB}; adding a plain camera.");
                var cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
                cam.transform.position = new Vector3(0f, 1.6f, 0f);
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            }
        }

        private static RectTransform CreateWorldCanvas(string name, Vector3 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.position = position;
            rt.localScale = Vector3.one * 0.0015f;

            var bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(rt, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.88f);
            return rt;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, string text, float size,
            Vector2 pos, Vector2 dims, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = dims;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }

        private static void CreateButton(RectTransform parent, string label, Vector2 pos, UnityAction onClick,
            float width = 700f, Color? color = null)
        {
            var go = new GameObject($"Button_{label}", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, 90f);
            go.GetComponent<Image>().color = color ?? new Color(0.16f, 0.42f, 0.70f);

            var button = go.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, onClick);
            // V5.1: pressable with the fingertip too (thin collider over the button, XRI poke into the canvas)
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(width, 90f, 20f);
            var poke = go.AddComponent<UIButtonPoke>();
            var si = go.AddComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(si.selectEntered, poke.Press);
            RigRealismSetup.MakePokeable(si, parent.forward, 0.009f);

            var text = CreateText(rt, "Label", label, 30, Vector2.zero, new Vector2(width - 20f, 80f));
            text.raycastTarget = false;
        }

        private static void SetPrivateField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[EscapeFire] Field '{field}' not found on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[EscapeFire] Saved {path}");
        }
    }
}
