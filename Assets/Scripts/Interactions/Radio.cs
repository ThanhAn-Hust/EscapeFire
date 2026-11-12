using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Core;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Handheld radio / phone for the 114 call (plan Phần B.1-2): pick it up, bring it to the mouth (≤ 15 cm) and hold
    /// the push-to-talk button (trigger) while speaking. Only then the microphone listens (speech-to-text through
    /// <see cref="SpeechServices.Input"/>); talking with PTT pressed far from the mouth gets a hint. What was said goes
    /// to <see cref="TranscriptHeard"/> (the 114 dialogue).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class Radio : MonoBehaviour
    {
        [SerializeField] private float mouthDistance = 0.15f;
        [Tooltip("Speech-to-text results arriving this long after PTT is released still count (STT latency).")]
        [SerializeField] private float lateTranscriptSeconds = 4f;
        [SerializeField] private Renderer txLight;

        private XRGrabInteractable _grab;
        private Transform _head;
        private ISpeechInput _input;
        private float _releasedAt = -100f;
        private bool _farHintShown;

        public bool IsTransmitting { get; private set; }
        public int Transmissions { get; private set; }
        public event Action<string> TranscriptHeard;

        private void Awake() => _grab = GetComponent<XRGrabInteractable>();

        private void OnDisable() => Bind(null);

        private void Bind(ISpeechInput input)
        {
            if (ReferenceEquals(_input, input)) return;
            if (_input != null) _input.TranscriptReady -= OnTranscript;
            _input = input;
            if (_input != null) _input.TranscriptReady += OnTranscript;
        }

        private void Update()
        {
            Bind(SpeechServices.Input);
            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
            }

            bool ptt = false;
            if (_grab.isSelected && _grab.firstInteractorSelecting is XRBaseInputInteractor hand)
                ptt = hand.activateInput.ReadIsPerformed();
            bool nearMouth = _head != null && Vector3.Distance(transform.position, WetTowel.Mouth(_head)) <= mouthDistance;
            bool tx = ptt && nearMouth;

            if (ptt && !nearMouth && !_farHintShown)
            {
                _farHintShown = true;
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Đưa bộ đàm sát miệng rồi mới bấm nói, nếu không 114 không nghe rõ."));
            }

            if (tx && !IsTransmitting)
            {
                Transmissions++;
                _input?.BeginListening();
            }
            else if (!tx && IsTransmitting)
            {
                _releasedAt = Time.time;
                _input?.EndListening();
            }
            IsTransmitting = tx;
            if (txLight != null) txLight.enabled = tx;
        }

        private void OnTranscript(string text)
        {
            if (!IsTransmitting && Time.time - _releasedAt > lateTranscriptSeconds) return; // not keyed: nobody heard it
            TranscriptHeard?.Invoke(text);
        }
    }
}
