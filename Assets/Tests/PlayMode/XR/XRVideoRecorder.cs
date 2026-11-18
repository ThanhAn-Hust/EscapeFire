using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// Records what the learner sees to an MP4 while a simulated-VR test plays: a capture camera follows the head
    /// camera (same position, field of view and layers; rotation lightly smoothed so the video is watchable) and
    /// draws a Vietnamese caption for the current step. It only watches: the test's input drives the game.
    /// </summary>
    public class XRVideoRecorder : MonoBehaviour
    {
        private const int W = 1280, H = 720, FPS = 30;

        private Camera _cam;
        private RenderTexture _rt;
        private Texture2D _tex;
        private MediaEncoder _encoder;
        private float _start;
        private long _frames;
        private TextMeshProUGUI _caption, _clock, _done;
        private GameObject _summaryBox;
        private TextMeshProUGUI _summary;
        private float _doneUntil;
        private readonly StringBuilder _timeline = new StringBuilder();
        private string _dir;
        private Quaternion _rot;
        private bool _hasRot;

        public string VideoPath { get; private set; }
        public long Frames => _frames;

        public static XRVideoRecorder Begin(string dir)
        {
            Directory.CreateDirectory(dir);
            var go = new GameObject("_GameplayRecorder");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<XRVideoRecorder>();
            r.Setup(dir);
            return r;
        }

        private void Setup(string dir)
        {
            _dir = dir;
            _cam = gameObject.AddComponent<Camera>();
            _cam.enabled = false;
            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            _cam.targetTexture = _rt;
            EnableUrpPost(_cam);
            _tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            BuildOverlay();
            VideoPath = Path.Combine(dir, "gameplay.mp4");
            _encoder = new MediaEncoder(VideoPath, new VideoTrackAttributes
            {
                frameRate = new MediaRational(FPS),
                width = W,
                height = H,
                includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.High
            });
            _start = Time.unscaledTime;
            _timeline.AppendLine("| Thời điểm | Bước |");
            _timeline.AppendLine("| ---: | :--- |");
        }

        public void Caption(string text)
        {
            _caption.text = text;
            _timeline.AppendLine($"| {Time.unscaledTime - _start:0.0}s | {text} |");
        }

        /// <summary>A short "✓ done" line for the step just finished.</summary>
        public void Done(string text)
        {
            _done.text = "✓ " + text;
            _doneUntil = Time.unscaledTime + 3.5f;
        }

        public void Summary(string text)
        {
            _summary.text = text;
            _summaryBox.SetActive(true);
            _timeline.AppendLine($"| {Time.unscaledTime - _start:0.0}s | Kết quả: {text.Replace("\n", "; ")} |");
        }

        private void LateUpdate()
        {
            var main = Camera.main;
            if (main == null || _encoder == null) return;
            // follow the head: exact position, rotation smoothed a little (raw simulated head turns are instant)
            Quaternion target = main.transform.rotation;
            _rot = _hasRot ? Quaternion.Slerp(_rot, target, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime)) : target;
            _hasRot = true;
            _cam.transform.SetPositionAndRotation(main.transform.position, _rot);
            _cam.fieldOfView = Mathf.Max(70f, main.fieldOfView);
            _cam.nearClipPlane = Mathf.Min(0.03f, main.nearClipPlane);
            _cam.farClipPlane = Mathf.Max(200f, main.farClipPlane);
            _cam.cullingMask = main.cullingMask;
            _cam.clearFlags = main.clearFlags;
            _cam.backgroundColor = main.backgroundColor;

            float t = Time.unscaledTime - _start;
            _clock.text = $"{(int)t / 60:00}:{(int)t % 60:00}";
            _done.transform.parent.gameObject.SetActive(Time.unscaledTime < _doneUntil);

            // the comfort vignette narrows the view while moving: right in the headset, but in a video it hides the
            // scene and the captions, so it is left out of the capture only (the game still runs it)
            var hidden = new List<Renderer>();
            foreach (var v in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController>(FindObjectsSortMode.None))
                foreach (var r in v.GetComponentsInChildren<Renderer>())
                    if (r.enabled) { r.enabled = false; hidden.Add(r); }
            _cam.Render();
            foreach (var r in hidden) r.enabled = true;
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            _tex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false);
            _tex.Apply(false);
            RenderTexture.active = prev;
            long due = (long)(t * FPS) + 1;
            int added = 0;
            while (_frames < due && added < FPS) { _encoder.AddFrame(_tex); _frames++; added++; }
            if (_frames < due) _frames = due; // a long hitch (scene load) is dropped, not frozen
        }

        public void Stop()
        {
            if (_encoder != null) { _encoder.Dispose(); _encoder = null; }
            File.WriteAllText(Path.Combine(_dir, "timeline.md"), _timeline.ToString());
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            if (_tex != null) Destroy(_tex);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ overlay

        private void BuildOverlay()
        {
            var go = new GameObject("_GameplayOverlay");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _cam;
            canvas.planeDistance = 0.1f;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);

            var title = Box(go.transform, new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(520f, 40f), 18, TextAlignmentOptions.MidlineLeft, out _);
            title.text = "<b>EscapeFire</b> · Diễn tập PCCC · thao tác bằng tay cầm VR";
            _clock = Box(go.transform, new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(90f, 40f), 18, TextAlignmentOptions.Center, out _);
            _caption = Box(go.transform, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1180f, 84f), 23, TextAlignmentOptions.Center, out _);
            _done = Box(go.transform, new Vector2(0f, 1f), new Vector2(14f, -60f), new Vector2(560f, 40f), 18, TextAlignmentOptions.MidlineLeft, out var doneBox);
            doneBox.GetComponent<Image>().color = new Color(0.05f, 0.45f, 0.2f, 0.75f);
            doneBox.SetActive(false);
            _summary = Box(go.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(760f, 420f), 20, TextAlignmentOptions.TopLeft, out _summaryBox);
            _summaryBox.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
            _summaryBox.SetActive(false);
        }

        private static TextMeshProUGUI Box(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, out GameObject box)
        {
            box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            var rt = (RectTransform)box.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            box.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(box.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14f, 6f);
            trt.offsetMax = new Vector2(-14f, -6f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/EF_Vietnamese SDF");
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            return tmp;
        }

        private static void EnableUrpPost(Camera cam)
        {
            var type = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) return;
            var data = cam.GetComponent(type) ?? cam.gameObject.AddComponent(type);
            type.GetProperty("renderPostProcessing")?.SetValue(data, true);
        }
    }
}
