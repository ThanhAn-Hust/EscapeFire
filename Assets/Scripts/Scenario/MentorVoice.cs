using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using EscapeFire.Core;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Speaks every AI Mentor message in Vietnamese. Priority: a pre-recorded clip whose text matches → the voice pack
    /// (Resources/Voice/&lt;key&gt;.wav, rendered by the edge laptop's Piper voice, works with the server off) → the edge
    /// server's /tts (Piper, for lines the AI writes during play; cached) → registered TTS → subtitle only.
    /// Lines in "[Name] ..." form are characters talking, not the Mentor: shown, not spoken in the Mentor's voice.
    /// In the editor every line without a voice is listed in Logs/voice_lines.txt so the pack can be completed
    /// (Server: uv run python -m escapefire_edge.voice_pack).
    /// A new message interrupts the current one; identical messages within <see cref="dedupeSeconds"/> are skipped.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MentorVoice : MonoBehaviour
    {
        [Serializable]
        public struct VoiceLine
        {
            [TextArea] public string text;
            public AudioClip clip;
        }

        [SerializeField] private VoiceLine[] recordedLines;
        [SerializeField] private float dedupeSeconds = 5f;

        private AudioSource _source;
        private readonly Dictionary<string, AudioClip> _fetched = new Dictionary<string, AudioClip>();
        private static readonly HashSet<string> _logged = new HashSet<string>();

        /// <summary>Key of a line in the voice pack: first 16 hex digits of MD5(UTF-8(text.Trim())), as on the server.</summary>
        public static string LineKey(string text)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var h = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text.Trim()));
                var sb = new System.Text.StringBuilder(16);
                for (int i = 0; i < 8; i++) sb.Append(h[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public string LastSpoken { get; private set; } = "";
        public string LastSource { get; private set; } = "";
        private string _lastMessage;
        private float _lastTime = float.NegativeInfinity;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // walkie-talkie voice in the ear
        }

        private void OnEnable() => EventBus<MentorMessageEvent>.Subscribe(OnMentorMessage);

        private void OnDisable() => EventBus<MentorMessageEvent>.Unsubscribe(OnMentorMessage);

        private void OnMentorMessage(MentorMessageEvent e)
        {
            if (string.IsNullOrEmpty(e.Message)) return;
            if (e.Message == _lastMessage && Time.time - _lastTime < dedupeSeconds) return;
            _lastMessage = e.Message;
            _lastTime = Time.time;

            if (e.Message.StartsWith("[")) return; // a character speaking, not the Mentor

            var clip = FindClip(e.Message);
            string source = "recorded";
            string key = LineKey(e.Message);
            if (clip == null) { clip = Resources.Load<AudioClip>("Voice/" + key); source = "pack"; }
            if (clip == null && _fetched.TryGetValue(key, out var got)) { clip = got; source = "edge"; }
            if (clip != null)
            {
                Play(clip, e.Message, source);
                return;
            }
            LogMissing(e.Message);
            // no audio device (batch tests) = nothing to play: never decode audio then (it crashed the editor)
            bool canPlay = !Application.isBatchMode && AudioSettings.GetConfiguration().sampleRate > 0;
            if (canPlay && !string.IsNullOrEmpty(GameSession.EdgeHttpBase) && isActiveAndEnabled)
            {
                StartCoroutine(FetchAndSpeak(e.Message, key));
                return;
            }

            var tts = SpeechServices.Output;
            if (tts != null && tts.IsReady)
            {
                _source.Stop();
                tts.Speak(e.Message);
            }
        }

        private void Play(AudioClip clip, string text, string source)
        {
            SpeechServices.Output?.Stop();
            _source.Stop();
            _source.clip = clip;
            _source.Play();
            LastSpoken = text;
            LastSource = source;
        }

        /// <summary>A line the AI wrote during play: ask the edge laptop to speak it (Piper), keep the clip.</summary>
        private IEnumerator FetchAndSpeak(string text, string key)
        {
            string body = "{\"text\":" + Newtonsoft.Json.JsonConvert.ToString(text) + "}";
            using (var req = new UnityWebRequest(GameSession.EdgeHttpBase.TrimEnd('/') + "/tts", "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerAudioClip(req.url, AudioType.WAV);
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 4;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) yield break; // server off: the subtitle is enough
                var clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip == null) yield break;
                _fetched[key] = clip;
                if (_lastMessage == text) Play(clip, text, "edge"); // still the current line
            }
        }

        private static void LogMissing(string text)
        {
#if UNITY_EDITOR
            if (!_logged.Add(text)) return;
            try
            {
                System.IO.Directory.CreateDirectory("Logs");
                System.IO.File.AppendAllText("Logs/voice_lines.txt", text.Replace("\n", " ") + "\n");
            }
            catch (System.IO.IOException) { }
#endif
        }

        private AudioClip FindClip(string message)
        {
            if (recordedLines == null) return null;
            for (int i = 0; i < recordedLines.Length; i++)
            {
                if (recordedLines[i].clip != null && recordedLines[i].text == message) return recordedLines[i].clip;
            }
            return null;
        }
    }
}
