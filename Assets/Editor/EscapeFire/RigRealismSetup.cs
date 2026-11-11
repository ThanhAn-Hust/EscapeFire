using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
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
