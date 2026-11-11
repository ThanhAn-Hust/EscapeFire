using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Player;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V0.1 "real-life" rig (plan Phần B.0 / B.2), idempotent:
    /// human hands with finger curl instead of controller models, posture-limited walking speed, tunneling
    /// vignette on all locomotion, and near-only (hand-at-object) interaction on every PCCC interactable.
    /// </summary>
    public static class RigRealismSetup
    {
        private const string LAB = "Assets/Scenes/BasicScene.unity";
        private const string HANDS = "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Models/";
        private const string ACTIONS = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";
        private const string VIGNETTE = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/TunnelingVignette/TunnelingVignette.prefab";

        [MenuItem("EscapeFire/V0/Setup Realistic Rig (Lab)")]
        public static void RunOnLab()
        {
            var scene = EditorSceneManager.OpenScene(LAB, OpenSceneMode.Single);
            var log = new List<string>();
            Apply(log);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EscapeFire] Realistic rig: " + (log.Count > 0 ? string.Join("; ", log) : "already set up"));
        }

        public static void Apply(List<string> log)
        {
            var origin = Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (origin == null) { log.Add("no XR Origin"); return; }

            if (origin.GetComponent<PostureLocomotion>() == null)
            {
                origin.gameObject.AddComponent<PostureLocomotion>();
                log.Add("PostureLocomotion on XR Origin");
            }

            SetupHand(origin, "Left Controller", true, log);
            SetupHand(origin, "Right Controller", false, log);
            SetupVignette(origin, log);
            AddNearOnlyFilters(log);
            SetupCallPoints(log);
        }

        /// <summary>
        /// B.1-1: a manual call point is pressed with a fingertip, ≥ 8 mm into the face, towards the wall.
        /// Grabbing it with a fist or pointing from afar does nothing.
        /// </summary>
        private static void SetupCallPoints(List<string> log)
        {
            foreach (var alarm in Object.FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var interactable = alarm.GetComponent<XRBaseInteractable>();
                if (interactable == null) continue;
                var near = alarm.GetComponent<NearOnlyFilter>();
                if (near != null) { near.PokeOnly = true; near.Reach = 0.12f; EditorUtility.SetDirty(near); }

                Vector3 intoWall = IntoWall(alarm.transform.position);
                PokeAxis axis = LocalAxis(alarm.transform, intoWall);
                var box = alarm.GetComponent<BoxCollider>();
                MakeFacePlate(box, axis, CallPointTravel);
                // XRI selects when the fingertip reaches the interactable's pivot; the pivot sits behind the face,
                // so shift the trigger point forward to "face + travel".
                float faceToPivot = box != null ? Vector3.Dot(alarm.transform.position - box.bounds.center, intoWall) + box.bounds.extents[intoWall.x != 0f ? 0 : 2] : 0f;
                var poke = alarm.GetComponent<XRPokeFilter>();
                if (poke == null) poke = alarm.gameObject.AddComponent<XRPokeFilter>();
                poke.pokeConfiguration = new UnityEngine.XR.Interaction.Toolkit.Filtering.PokeThresholdDatumProperty(new PokeThresholdData
                {
                    pokeDirection = axis,
                    interactionDepthOffset = Mathf.Max(0f, faceToPivot - CallPointTravel),
                    enablePokeAngleThreshold = true,
                    pokeAngleThreshold = 60f
                });
                EditorUtility.SetDirty(poke);
                log.Add($"call point '{alarm.name}': fingertip poke {LocalAxis(alarm.transform, intoWall)}");
            }
        }

        /// <summary>
        /// XRI fires a poke when the fingertip has travelled through the whole collider along the poke axis, so the
        /// collider becomes a thin plate at the front face: its thickness is the real button travel (~9 mm).
        /// </summary>
        public const float CallPointTravel = 0.009f;

        public static void MakeFacePlate(BoxCollider box, PokeAxis axis, float travelMeters)
        {
            if (box == null) return;
            int i = axis == PokeAxis.X || axis == PokeAxis.NegativeX ? 0 : axis == PokeAxis.Y || axis == PokeAxis.NegativeY ? 1 : 2;
            float sign = axis == PokeAxis.X || axis == PokeAxis.Y || axis == PokeAxis.Z ? -1f : 1f; // front face is opposite the poke direction
            float scale = Mathf.Abs(box.transform.lossyScale[i]);
            if (scale < 1e-5f) return;
            Vector3 size = box.size, center = box.center;
            float front = center[i] + sign * size[i] * 0.5f;
            size[i] = travelMeters / scale;
            center[i] = front - sign * size[i] * 0.5f;
            box.size = size;
            box.center = center;
            EditorUtility.SetDirty(box);
        }

        /// <summary>Walls are axis-aligned: the direction from the room centre towards the wall the object hangs on.</summary>
        public static Vector3 IntoWall(Vector3 p)
        {
            Vector3 d = new Vector3(p.x, 0f, p.z);
            return Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(d.z));
        }

        public static PokeAxis LocalAxis(Transform t, Vector3 worldDir)
        {
            var axes = new (Vector3 dir, PokeAxis axis)[]
            {
                (t.right, PokeAxis.X), (-t.right, PokeAxis.NegativeX), (t.up, PokeAxis.Y), (-t.up, PokeAxis.NegativeY),
                (t.forward, PokeAxis.Z), (-t.forward, PokeAxis.NegativeZ)
            };
            return axes.OrderByDescending(a => Vector3.Dot(a.dir.normalized, worldDir)).First().axis;
        }

        private static void SetupHand(XROrigin origin, string controllerName, bool left, List<string> log)
        {
            var controller = origin.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == controllerName);
            if (controller == null) { log.Add($"{controllerName} missing"); return; }
            var hand = controller.GetComponent<ControllerHand>();
            bool added = hand == null;
            if (added) hand = controller.gameObject.AddComponent<ControllerHand>();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(HANDS + (left ? "LeftHand.fbx" : "RightHand.fbx"));
            string map = left ? "XRI Left Interaction" : "XRI Right Interaction";
            hand.Configure(model, left, new InputActionProperty(Action(map, "Select Value")), new InputActionProperty(Action(map, "Activate Value")));
            EditorUtility.SetDirty(hand);
            if (added) log.Add($"hand model on {controllerName}");
        }

        private static InputActionReference Action(string map, string action) =>
            AssetDatabase.LoadAllAssetsAtPath(ACTIONS).OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.actionMap != null && r.action.actionMap.name == map && r.action.name == action);

        private static void SetupVignette(XROrigin origin, List<string> log)
        {
            if (origin.Camera == null) return;
            var controller = origin.GetComponentInChildren<TunnelingVignetteController>(true);
            if (controller == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIGNETTE);
                if (prefab == null) { log.Add("vignette prefab missing"); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, origin.Camera.transform);
                go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                controller = go.GetComponent<TunnelingVignetteController>();
                log.Add("tunneling vignette under Main Camera");
            }
            var providers = new List<LocomotionVignetteProvider>();
            foreach (var p in origin.GetComponentsInChildren<LocomotionProvider>(true))
                providers.Add(new LocomotionVignetteProvider { locomotionProvider = p, enabled = true });
            controller.locomotionVignetteProviders = providers;
            EditorUtility.SetDirty(controller);
        }

        /// <summary>Everything the learner presses, pulls or grabs in real life must be reached with the hand.</summary>
        private static void AddNearOnlyFilters(List<string> log)
        {
            int n = 0;
            var targets = new List<GameObject>();
            foreach (var c in Object.FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<CircuitBreakerSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<DoorHeatChecker>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var c in Object.FindObjectsByType<NPCSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None)) targets.Add(c.gameObject);
            foreach (var go in targets)
                foreach (var interactable in go.GetComponentsInChildren<XRBaseInteractable>(true))
                    if (EnsureNearOnly(interactable, go.GetComponent<NPCSimulator>() != null ? 0.35f : 0.15f)) n++;
            if (n > 0) log.Add($"near-only filter on {n} interactable(s)");
        }

        public static bool EnsureNearOnly(XRBaseInteractable interactable, float reach)
        {
            var filter = interactable.GetComponent<NearOnlyFilter>();
            bool added = filter == null;
            if (added) filter = interactable.gameObject.AddComponent<NearOnlyFilter>();
            filter.Reach = reach;
            EditorUtility.SetDirty(filter);
            var so = new SerializedObject(interactable);
            bool changed = AddToList(so.FindProperty("m_StartingHoverFilters"), filter) | AddToList(so.FindProperty("m_StartingSelectFilters"), filter);
            so.ApplyModifiedPropertiesWithoutUndo();
            return added || changed;
        }

        private static bool AddToList(SerializedProperty list, Object item)
        {
            if (list == null) return false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == item) return false;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            return true;
        }
    }
}
