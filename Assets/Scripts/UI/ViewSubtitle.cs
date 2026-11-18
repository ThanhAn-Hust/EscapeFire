using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>
    /// Playtest 18/11: the Mentor only wrote on the wrist HUD, which a frightened first-timer never looks at. Every
    /// Mentor line is now also a subtitle floating in front of the eyes (lower part of the view, follows the head
    /// smoothly), shown for a few seconds. The guided coach can pin its current step line here. Added to the camera
    /// of every scene automatically.
    /// </summary>
    public class ViewSubtitle : MonoBehaviour
    {
        public static ViewSubtitle Instance { get; private set; }

        private TextMeshPro _text;
        private Transform _panel;
        private Renderer _back;
        private string _transient = "", _pinned = "";
        private float _transientUntil;

        public string Shown => _text != null ? _text.text : "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Attach();
        }

        private static void OnSceneLoaded(Scene s, LoadSceneMode m) => Attach();

        private static void Attach()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (cam.GetComponent<ViewSubtitle>() == null) cam.gameObject.AddComponent<ViewSubtitle>();
            // every scene gets the Mentor's voice (the lab has its own; the lesson, menu and generated rooms did not)
            if (FindFirstObjectByType<EscapeFire.Scenario.MentorVoice>() == null) cam.gameObject.AddComponent<EscapeFire.Scenario.MentorVoice>();
        }

        private void Awake()
        {
            Instance = this;
            _panel = new GameObject("ViewSubtitle").transform;
            _panel.SetParent(null, false);
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "Back";
            Destroy(back.GetComponent<Collider>());
            back.transform.SetParent(_panel, false);
            back.transform.localScale = new Vector3(0.62f, 0.13f, 1f);
            back.transform.localPosition = new Vector3(0f, 0f, 0.002f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetFloat("_Surface", 1f); // transparent
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3990;
            mat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0.62f));
            _back = back.GetComponent<Renderer>();
            _back.sharedMaterial = mat;
            _text = new GameObject("Text").AddComponent<TextMeshPro>();
            _text.transform.SetParent(_panel, false);
            _text.rectTransform.sizeDelta = new Vector2(0.6f, 0.12f);
            _text.fontSize = 0.32f;
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 0.18f;
            _text.fontSizeMax = 0.32f;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = Color.white;
            _text.renderer.sharedMaterial.renderQueue = 4000; // over smoke / veil
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/EF_Vietnamese SDF");
            if (font != null) _text.font = font;
            Show();
        }

        private void OnEnable() => EventBus<MentorMessageEvent>.Subscribe(OnMentor);

        private void OnDisable() => EventBus<MentorMessageEvent>.Unsubscribe(OnMentor);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_panel != null) Destroy(_panel.gameObject);
        }

        private void OnMentor(MentorMessageEvent e)
        {
            _transient = e.Message;
            _transientUntil = Time.time + Mathf.Clamp(2.5f + e.Message.Length * 0.06f, 4f, 9f);
            Show();
        }

        /// <summary>A line that stays while it is relevant (the guided coach's current step); null clears it.</summary>
        public void Pin(string line)
        {
            _pinned = line ?? "";
            Show();
        }

        private void Show()
        {
            if (_text == null) return;
            bool transient = Time.time < _transientUntil && _transient.Length > 0;
            string s = transient ? _transient : _pinned;
            _text.text = s;
            _panel.gameObject.SetActive(s.Length > 0);
        }

        private void LateUpdate()
        {
            if (Time.time >= _transientUntil && _transient.Length > 0) { _transient = ""; Show(); }
            if (!_panel.gameObject.activeSelf) return;
            // lower part of the view, 0.9 m ahead; follows the head's yaw smoothly (no jitter with head bobbing)
            Vector3 fwd = transform.forward; fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
            Vector3 target = transform.position + fwd * 0.9f + Vector3.up * -0.28f;
            _panel.position = Vector3.Lerp(_panel.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
            _panel.rotation = Quaternion.Slerp(_panel.rotation, Quaternion.LookRotation(fwd, Vector3.up), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }
}
