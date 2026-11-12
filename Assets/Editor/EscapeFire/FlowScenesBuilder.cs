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
            Debug.Log("[EscapeFire] V5.1 flow scenes rebuilt: " + string.Join("; ", log));
        }

        private static void ApplyRig(string path, List<string> log)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            RigRealismSetup.Apply(log);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
