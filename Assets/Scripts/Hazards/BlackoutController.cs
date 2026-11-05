using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// Anomaly TRIGGER_BLACKOUT: normal lighting fails, only battery-backed emergency lighting stays on
    /// (dim red ceiling light + green EXIT light over the door, as required by fire codes).
    /// The exit stays visible, so the life-safety invariant holds. Cheap on Quest: two lights, no shadows.
    /// </summary>
    public class BlackoutController : MonoBehaviour
    {
        [SerializeField] private float remainingLightFraction = 0.12f;
        [SerializeField] private float ambientDuringBlackout = 0.18f;

        private readonly List<(Light light, float intensity)> _normalLights = new List<(Light, float)>();
        private float _ambientBefore = 1f;

        public bool IsBlackout { get; private set; }

        public bool Trigger()
        {
            if (IsBlackout) return false;
            IsBlackout = true;

            _normalLights.Clear();
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (l.name.StartsWith("Emergency")) continue;
                _normalLights.Add((l, l.intensity));
                l.intensity *= remainingLightFraction;
            }
            _ambientBefore = RenderSettings.ambientIntensity;
            RenderSettings.ambientIntensity = ambientDuringBlackout;

            CreateEmergencyLight("EmergencyLight_Ceiling", new Vector3(0f, 4.2f, 0f), new Color(1f, 0.15f, 0.1f), 1.6f, 14f);
            var door = GameObject.Find("Main_Exit_Door_Double");
            Vector3 exitPos = door != null ? door.transform.position + new Vector3(0f, 1.6f, 0.6f) : new Vector3(0f, 2.8f, -6.2f);
            CreateEmergencyLight("EmergencyLight_EXIT", exitPos, new Color(0.15f, 1f, 0.3f), 2.2f, 5f);
            return true;
        }

        /// <summary>Restores normal lighting (used when a drill restarts in the same scene instance).</summary>
        public void Restore()
        {
            if (!IsBlackout) return;
            foreach (var (light, intensity) in _normalLights) if (light != null) light.intensity = intensity;
            RenderSettings.ambientIntensity = _ambientBefore;
            foreach (Transform child in transform) if (child.name.StartsWith("Emergency")) Destroy(child.gameObject);
            IsBlackout = false;
        }

        private void CreateEmergencyLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
        }
    }
}
