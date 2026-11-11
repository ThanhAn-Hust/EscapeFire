using UnityEngine;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// One owner for RenderSettings fog: every smoke source (lab layer, corridor / stairwell zones) requests a density
    /// for the space the learner's head is in; the densest request this frame wins, none = clear air.
    /// </summary>
    public class SmokeFog : MonoBehaviour
    {
        private static SmokeFog _instance;
        private float _density = -1f;
        private Color _color;
        private bool _baseFog;
        private float _baseDensity;
        private Color _baseColor;
        private FogMode _baseMode;

        public static float LastAppliedDensity { get; private set; }

        public static void Request(float density, Color color)
        {
            if (_instance == null)
            {
                var go = new GameObject("_SmokeFog");
                _instance = go.AddComponent<SmokeFog>();
            }
            if (density > _instance._density) { _instance._density = density; _instance._color = color; }
        }

        private void Awake()
        {
            _baseFog = RenderSettings.fog; _baseDensity = RenderSettings.fogDensity;
            _baseColor = RenderSettings.fogColor; _baseMode = RenderSettings.fogMode;
        }

        private void LateUpdate()
        {
            if (_density >= 0f)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = _color;
                RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, _density, 0.2f * Time.deltaTime);
                LastAppliedDensity = RenderSettings.fogDensity;
            }
            else if (RenderSettings.fog)
            {
                RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, 0f, 0.2f * Time.deltaTime);
                if (RenderSettings.fogDensity <= 0.0005f) RenderSettings.fog = _baseFog;
                LastAppliedDensity = RenderSettings.fogDensity;
            }
            _density = -1f;
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null;
            RenderSettings.fog = _baseFog; RenderSettings.fogDensity = _baseDensity;
            RenderSettings.fogColor = _baseColor; RenderSettings.fogMode = _baseMode;
        }
    }
}
