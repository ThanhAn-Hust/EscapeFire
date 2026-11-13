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

        // V5.2: darkness of a space whose lights failed (LIGHTS_OFF in the corridor / a stairwell), while the head is in it
        private float _darkRequest = -1f;
        private bool _darkApplied;
        private float _ambientBeforeDark;
        private readonly System.Collections.Generic.List<(Light light, float intensity)> _sunsBeforeDark = new System.Collections.Generic.List<(Light, float)>();

        /// <summary>Ambient intensity to use this frame (lowest request wins); nothing requested = restore.</summary>
        public static void RequestDarkness(float ambient)
        {
            if (_instance == null)
            {
                var go = new GameObject("_SmokeFog");
                _instance = go.AddComponent<SmokeFog>();
            }
            if (_instance._darkRequest < 0f || ambient < _instance._darkRequest) _instance._darkRequest = ambient;
        }

        public static bool IsDark => _instance != null && _instance._darkApplied;

        private void ApplyDarkness()
        {
            if (_darkRequest >= 0f)
            {
                if (!_darkApplied)
                {
                    _darkApplied = true;
                    _ambientBeforeDark = RenderSettings.ambientIntensity;
                    _sunsBeforeDark.Clear();
                    foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                        if (l.type == LightType.Directional) _sunsBeforeDark.Add((l, l.intensity));
                }
                RenderSettings.ambientIntensity = Mathf.MoveTowards(RenderSettings.ambientIntensity, _darkRequest, 3f * Time.deltaTime);
                SetOverlay(Mathf.MoveTowards(_overlayAlpha, 0.62f, 2f * Time.deltaTime));
                foreach (var (l, i) in _sunsBeforeDark) if (l != null) l.intensity = Mathf.MoveTowards(l.intensity, i * 0.1f, 3f * i * Time.deltaTime);
            }
            else if (_darkApplied)
            {
                _darkApplied = false;
                RenderSettings.ambientIntensity = _ambientBeforeDark;
                foreach (var (l, i) in _sunsBeforeDark) if (l != null) l.intensity = i;
            }
            if (!_darkApplied && _overlayAlpha > 0f) SetOverlay(Mathf.MoveTowards(_overlayAlpha, 0f, 3f * Time.deltaTime));
            _darkRequest = -1f;
        }

        // Lights in this project are partly baked, so switching realtime lights off barely darkens a space: a dark veil in
        // front of the eyes (on the camera, so both VR eyes get it) does. Bright EXIT signs stay readable through it.
        private Renderer _overlay;
        private float _overlayAlpha;

        private void SetOverlay(float alpha)
        {
            _overlayAlpha = alpha;
            var cam = Camera.main;
            if (_overlay == null && cam != null && alpha > 0f)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "DarknessVeil";
                Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(cam.transform, false);
                q.transform.localPosition = new Vector3(0f, 0f, cam.nearClipPlane + 0.05f);
                q.transform.localScale = new Vector3(2f, 2f, 1f);
                _overlay = q.GetComponent<Renderer>();
                _overlay.sharedMaterial = EscapeFire.Core.RuntimeVisuals.ParticleMaterial(new Color(0f, 0f, 0f, 0f));
                _overlay.sharedMaterial.renderQueue = 4000;
            }
            if (_overlay == null) return;
            _overlay.enabled = alpha > 0.01f;
            _overlay.sharedMaterial.SetColor("_BaseColor", new Color(0f, 0f, 0f, alpha));
        }

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
            ApplyDarkness();
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
