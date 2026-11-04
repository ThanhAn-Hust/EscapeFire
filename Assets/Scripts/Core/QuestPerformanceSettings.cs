using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Meta;

namespace EscapeFire.Core
{
    /// <summary>
    /// Runtime XR performance profile for Quest 2 (spec: lock >= 72 FPS, frame budget 13.8 ms).
    /// Requests the target refresh rate and fixed-foveated rendering once the XR display is running.
    /// Runs automatically in every scene (no component to place). No-ops in the simulator / editor without HMD.
    /// </summary>
    public static class QuestPerformanceSettings
    {
        public const float TARGET_REFRESH_RATE = 72f;
        public const float FOVEATION_LEVEL = 0.75f; // 0 = off, 1 = max; high is safe for a dark smoky room

        private static readonly List<XRDisplaySubsystem> _displays = new List<XRDisplaySubsystem>();
        public static float AppliedRefreshRate { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            SubsystemManager.GetSubsystems(_displays);
            for (int i = 0; i < _displays.Count; i++)
            {
                var display = _displays[i];
                if (!display.running) continue;

                display.foveatedRenderingLevel = FOVEATION_LEVEL;
                display.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.GazeAllowed;

                try
                {
                    if (display.TryRequestDisplayRefreshRate(TARGET_REFRESH_RATE)) AppliedRefreshRate = TARGET_REFRESH_RATE;
                }
                catch (System.Exception)
                {
                    // Meta display feature disabled or PC Link runtime: the headset keeps its own rate.
                }
                Debug.Log($"[QuestPerf] Foveation {FOVEATION_LEVEL:F2}, refresh request {TARGET_REFRESH_RATE} Hz (applied: {AppliedRefreshRate}).");
            }
        }
    }
}
