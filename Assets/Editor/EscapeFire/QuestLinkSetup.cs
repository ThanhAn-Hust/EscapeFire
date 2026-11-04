using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// Menu: EscapeFire/Quest Link/Check &amp; Fix Setup — makes "plug Quest 2 via Link cable, press Play" work.
    /// Fixes what can be fixed inside the project; reports what must be done on the PC (runtime, Link app).
    /// </summary>
    public static class QuestLinkSetup
    {
        [MenuItem("EscapeFire/Quest Link/Check & Fix Setup")]
        public static void CheckAndFix()
        {
            var ok = new List<string>();
            var fixedItems = new List<string>();
            var todo = new List<string>();

            // 1. Standalone XR loader = OpenXR, initialize on start.
            var perTarget = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (perTarget == null || perTarget.Manager == null)
            {
                todo.Add("Project Settings > XR Plug-in Management > tab PC: tick OpenXR.");
            }
            else
            {
                bool hasOpenXR = false;
                foreach (var l in perTarget.Manager.activeLoaders) hasOpenXR |= l is OpenXRLoader;
                if (hasOpenXR) ok.Add("PC loader: OpenXR");
                else todo.Add("Project Settings > XR Plug-in Management > tab PC: tick OpenXR.");

                if (!perTarget.InitManagerOnStart)
                {
                    perTarget.InitManagerOnStart = true;
                    EditorUtility.SetDirty(perTarget);
                    fixedItems.Add("Enabled 'Initialize XR on Startup' for PC.");
                }
                else ok.Add("Initialize XR on Startup: on");
            }

            // 2. Quest controller interaction profiles for PC.
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (settings != null)
            {
                EnableFeature<OculusTouchControllerProfile>(settings, "Oculus Touch Controller Profile", ok, fixedItems);
                EnableFeature<MetaQuestTouchPlusControllerProfile>(settings, "Meta Quest Touch Plus Controller Profile", ok, fixedItems);
            }

            // 3. Editor must target Windows for Link play mode.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                todo.Add($"Active build target is {EditorUserBuildSettings.activeBuildTarget}. For Link play mode switch to Windows " +
                         "(File > Build Profiles > Windows > Switch Platform). Switch back to Android only to build the APK. " +
                         "(Play mode still works on Android target, but PC OpenXR features are what Link uses.)");
            }
            else ok.Add("Active build target: Windows");

            // 4. Windows OpenXR runtime must be Meta (Oculus) for Link.
            string runtime = ReadActiveOpenXRRuntime();
            if (string.IsNullOrEmpty(runtime))
                todo.Add("No active OpenXR runtime found. Install Meta Quest Link app (meta.com/quest/setup).");
            else if (runtime.ToLowerInvariant().Contains("oculus"))
                ok.Add($"Active OpenXR runtime: Meta/Oculus ({runtime})");
            else
                todo.Add($"Active OpenXR runtime is '{runtime}'. Open Meta Quest Link app > Settings > General > " +
                         "'OpenXR Runtime' > Set Meta Quest Link as active.");

            AssetDatabase.SaveAssets();

            var sb = new StringBuilder("[EscapeFire] Quest Link setup\n");
            foreach (var s in ok) sb.AppendLine("  OK    " + s);
            foreach (var s in fixedItems) sb.AppendLine("  FIXED " + s);
            foreach (var s in todo) sb.AppendLine("  TODO  " + s);
            sb.AppendLine("Each test: open Meta Quest Link app → put on headset → enable Link (cable or Air Link) → press Play.");
            if (todo.Count > 0) Debug.LogWarning(sb.ToString());
            else Debug.Log(sb.ToString());
        }

        private static void EnableFeature<T>(OpenXRSettings settings, string label, List<string> ok, List<string> fixedItems)
            where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            var feature = settings.GetFeature<T>();
            if (feature == null) return;
            if (feature.enabled)
            {
                ok.Add(label);
                return;
            }
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
            fixedItems.Add($"Enabled {label} (PC).");
        }

        private static string ReadActiveOpenXRRuntime()
        {
#if UNITY_EDITOR_WIN
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Khronos\OpenXR\1"))
                {
                    return key?.GetValue("ActiveRuntime") as string ?? string.Empty;
                }
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
#else
            return string.Empty;
#endif
        }
    }
}
