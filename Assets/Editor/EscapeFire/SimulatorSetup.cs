using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// Keyboard + mouse testing without a headset, the XRI 3.x way: the official XR Interaction Simulator prefab
    /// is auto-instantiated in the Editor for every scene (Menu, Lab, AAR). HeadsetPlayModeGuard disables it when a
    /// Quest is connected over Link. Removes the bare XR Device Simulator that had no input actions assigned
    /// (it logged an error on every Play and keyboard control did not work).
    /// </summary>
    public static class SimulatorSetup
    {
        private const string SETTINGS = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
        private const string PREFAB = "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Interaction Simulator/XR Interaction Simulator.prefab";

        [MenuItem("EscapeFire/Quest Link/Setup Keyboard Simulator (no headset)")]
        public static void Configure()
        {
            var settings = AssetDatabase.LoadMainAssetAtPath(SETTINGS);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            if (settings == null || prefab == null)
            {
                Debug.LogError($"[EscapeFire] Simulator setup failed: settings={(settings != null)}, prefab={(prefab != null)} ({PREFAB}).");
                return;
            }

            var so = new SerializedObject(settings);
            so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue = true;
            so.FindProperty("m_UseClassic").boolValue = false;
            so.FindProperty("m_SimulatorPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            int removed = 0;
            foreach (var path in new[] { "Assets/Scenes/BasicScene.unity", "Assets/Scenes/00_MainMenu.unity", "Assets/Scenes/02_AAR_Evaluation.unity" })
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool dirty = false;
                foreach (var sim in Object.FindObjectsByType<XRDeviceSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Object.DestroyImmediate(sim.gameObject);
                    removed++;
                    dirty = true;
                }
                if (dirty)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            Debug.Log($"[EscapeFire] Keyboard simulator: XR Interaction Simulator auto-instantiated in Editor; removed {removed} unconfigured XR Device Simulator(s).");
        }
    }
}
