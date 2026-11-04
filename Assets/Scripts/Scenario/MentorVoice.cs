using System;
using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Speaks every AI Mentor message. Priority: a pre-recorded clip whose text matches (0 ms, best quality for the
    /// fixed Socratic lines) → registered TTS (Overtone offline) → text-only on the wrist HUD.
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

            var clip = FindClip(e.Message);
            if (clip != null)
            {
                SpeechServices.Output?.Stop();
                _source.Stop();
                _source.clip = clip;
                _source.Play();
                return;
            }

            var tts = SpeechServices.Output;
            if (tts != null && tts.IsReady)
            {
                _source.Stop();
                tts.Speak(e.Message);
            }
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
