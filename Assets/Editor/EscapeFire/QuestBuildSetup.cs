using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// M6: Quest 2 standalone build profile (spec: >= 72 FPS on XR2) and one-click APK build.
    /// </summary>
    public static class QuestBuildSetup
    {
        private const string APK_PATH = "Builds/EscapeFire_Quest2.apk";

        [MenuItem("EscapeFire/M6/Configure Quest 2 Build Settings")]
        public static void Configure()
        {
            var log = new List<string>();
            var android = NamedBuildTarget.Android;

            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.forceInternetPermission = true; // edge server + LMS over LAN
            // The edge AI server / mock LMS run on the instructor laptop over plain http:// on the LAN.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetMobileMTRendering(android, true);
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(android)) ||
                PlayerSettings.GetApplicationIdentifier(android).StartsWith("com.Default"))
            {
                PlayerSettings.SetApplicationIdentifier(android, "vn.edu.hust.escapefire");
            }
            log.Add("Android: IL2CPP, ARM64, API 32+, Vulkan, Linear, ASTC, multithreaded rendering, Internet permission.");

            // OpenXR for Quest on Android: Meta Quest support + Touch controllers, single-pass instanced.
            var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (xr != null)
            {
                xr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                xr.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth16Bit;
                EditorUtility.SetDirty(xr);
                foreach (var feature in xr.GetFeatures<OpenXRFeature>())
                {
                    string type = feature.GetType().Name;
                    if (type == "MetaQuestFeature" || feature is OculusTouchControllerProfile || feature is MetaQuestTouchPlusControllerProfile)
                    {
                        if (!feature.enabled)
                        {
                            feature.enabled = true;
                            EditorUtility.SetDirty(feature);
                            log.Add($"Enabled OpenXR feature {type} (Android).");
                        }
                    }
                }
                log.Add("OpenXR Android: single-pass instanced, 16-bit depth submission.");
            }

            // URP asset used by the Android quality level.
            int androidLevel = QualityLevelFor("Android");
            var levelAsset = QualitySettings.GetRenderPipelineAssetAt(androidLevel);
            var urp = (levelAsset != null ? levelAsset : GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.msaaSampleCount = 4;
                urp.supportsHDR = false;
                urp.renderScale = 1f;
                urp.shadowDistance = 15f;
                urp.supportsCameraDepthTexture = false;
                urp.supportsCameraOpaqueTexture = false; // Real Fire refraction falls back gracefully
                EditorUtility.SetDirty(urp);
                log.Add($"URP '{urp.name}': MSAA 4x, HDR off, render scale 1.0, shadows 15 m, no depth/opaque textures.");
            }
            else
            {
                log.Add("WARNING: no URP asset found for the Android quality level.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[EscapeFire] Quest 2 build configured:\n- " + string.Join("\n- ", log));
        }

        [MenuItem("EscapeFire/M6/Build Quest 2 APK")]
        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUtility.DisplayDialog("Switch to Android?",
                    "Building the Quest APK switches the active build target to Android (re-imports assets). Continue?",
                    "Switch & Build", "Cancel"))
            {
                return;
            }

            Configure();
            Directory.CreateDirectory(Path.GetDirectoryName(APK_PATH));
            var scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = APK_PATH,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"[EscapeFire] APK built: {APK_PATH} ({report.summary.totalSize / (1024f * 1024f):F1} MB). Install: adb install -r {APK_PATH}");
            else
                Debug.LogError($"[EscapeFire] APK build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
        }

        private static int QualityLevelFor(string platform)
        {
            var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; perPlatform != null && i < perPlatform.arraySize; i++)
            {
                var entry = perPlatform.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == platform) return entry.FindPropertyRelative("second").intValue;
            }
            return QualitySettings.GetQualityLevel();
        }
    }
}
