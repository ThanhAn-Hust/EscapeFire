using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// V5.1 — rebuilds the scenes of the menu flow (main menu, AAR, lesson room) with fingertip-pressable buttons, the
    /// "new room" entry and the lesson's way back, the same realistic rig (hands, posture, near-only) as the lab, and the
    /// Vietnamese font.
    /// </summary>
    public static class FlowScenesBuilder
    {
        [MenuItem("EscapeFire/V5/Rebuild Menu Flow Scenes (Menu + AAR + Lesson)")]
        public static void Run()
        {
            var log = new List<string>();
            EscapeFireSceneBuilder.BuildMainMenuScene();
            ApplyRig(EscapeFireSceneBuilder.MenuPath, log);
            EscapeFireSceneBuilder.BuildAARScene();
            ApplyRig(EscapeFireSceneBuilder.AARPath, log);
            LessonRoomBuilder.Build();
            VietnameseFontSetup.Run();
            RemoveSceneManagers("Assets/Scenes/BasicScene.unity", log);
            Debug.Log("[EscapeFire] V5.1 flow scenes rebuilt: " + string.Join("; ", log));
        }

        /// <summary>
        /// V5.3: one XR Interaction Manager for the whole game (XRI keeps it across scenes). A scene-level one next to it
        /// splits interactors from interactables; interactables that pointed at it fall back to the shared one.
        /// </summary>
        private static void RemoveSceneManagers(string path, List<string> log)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = 0;
            foreach (var m in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var go = m.gameObject;
                Object.DestroyImmediate(m);
                if (go.GetComponents<Component>().Length == 1 && go.transform.childCount == 0) Object.DestroyImmediate(go);
                n++;
            }
            if (n > 0) EditorSceneManager.SaveScene(scene);
            log.Add($"removed {n} scene XR Interaction Manager(s) from {path}");
        }

        /// <summary>
        /// V5.4: bakes occlusion culling for the drill building (walls marked Occluder by RescueBuilder), so rooms behind
        /// walls are not drawn. Run after RescueBuilder.Run; takes a few minutes.
        /// </summary>
        [MenuItem("EscapeFire/V5/Bake Occlusion Culling (BasicScene)")]
        public static void BakeOcclusion()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BasicScene.unity", OpenSceneMode.Single);
            StaticOcclusionCulling.smallestOccluder = 1.5f;
            StaticOcclusionCulling.smallestHole = 0.25f;
            var t = System.Diagnostics.Stopwatch.StartNew();
            bool ok = StaticOcclusionCulling.Compute();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[EscapeFire] V5.4 occlusion bake ok={ok} in {t.Elapsed.TotalSeconds:F0}s, data size {StaticOcclusionCulling.umbraDataSize} bytes");
        }

        private static void ApplyRig(string path, List<string> log)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RigRealismSetup.Apply(log);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
