using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Management;

namespace EscapeFire.Core
{
    /// <summary>
    /// "Plug in Quest 2 → press Play" support. Runs automatically in every scene (no component to place):
    /// - If OpenXR started with a real headset (Quest Link / Air Link), disables any XR Device / Interaction
    ///   Simulator in the scene so it cannot hijack the HMD and controllers.
    /// - If no headset is available, leaves the simulator on so keyboard + mouse testing still works.
    /// </summary>
    public static class HeadsetPlayModeGuard
    {
        public static bool HeadsetActive { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply(SceneManager.GetActiveScene().name);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene.name);

        private static void Apply(string sceneName)
        {
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            var loader = manager != null ? manager.activeLoader : null;
            HeadsetActive = loader != null;

            if (!HeadsetActive)
            {
                Debug.Log($"[HeadsetGuard] {sceneName}: no XR headset/runtime → using XR Device Simulator (keyboard + mouse).");
                return;
            }

            int disabled = 0;
            foreach (var sim in Object.FindObjectsByType<XRDeviceSimulator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                sim.gameObject.SetActive(false);
                disabled++;
            }
            foreach (var sim in Object.FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                sim.gameObject.SetActive(false);
                disabled++;
            }
            Debug.Log($"[HeadsetGuard] {sceneName}: headset active via {loader.name} → disabled {disabled} simulator(s).");
        }
    }
}
