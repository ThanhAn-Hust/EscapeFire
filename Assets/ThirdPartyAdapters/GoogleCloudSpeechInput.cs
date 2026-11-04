using System;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.Tools;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.V1;
using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.Adapters
{
    /// <summary>
    /// Plugs Frostweep Google Cloud Speech-to-Text (vi-VN) into <see cref="SpeechServices"/>.
    /// Needs the GCSpeechRecognition prefab in the scene with a valid API key and internet access.
    /// Without them the voice command simply falls back to the physical alarm button.
    /// </summary>
    public class GoogleCloudSpeechInput : MonoBehaviour, ISpeechInput
    {
        [SerializeField] private Enumerators.LanguageCode language = Enumerators.LanguageCode.vi_VN;

        private GCSpeechRecognition _stt;

        public event Action<string> TranscriptReady;

        public bool IsReady => _stt != null && _stt.HasConnectedMicrophoneDevices();

        private void Start()
        {
            _stt = GCSpeechRecognition.Instance;
            if (_stt == null)
            {
                Debug.LogWarning("[GoogleCloudSpeechInput] GCSpeechRecognition prefab not in scene; voice commands disabled.");
                return;
            }
            _stt.RequestMicrophonePermission(null);
            if (_stt.HasConnectedMicrophoneDevices()) _stt.SetMicrophoneDevice(_stt.GetMicrophoneDevices()[0]);
            _stt.FinishedRecordEvent += OnFinishedRecord;
            _stt.RecognizeSuccessEvent += OnRecognized;
            _stt.RecognizeFailedEvent += OnFailed;
            SpeechServices.Register(this);
        }

        private void OnDestroy()
        {
            SpeechServices.Unregister(this);
            if (_stt == null) return;
            _stt.FinishedRecordEvent -= OnFinishedRecord;
            _stt.RecognizeSuccessEvent -= OnRecognized;
            _stt.RecognizeFailedEvent -= OnFailed;
        }

        public void BeginListening()
        {
            if (IsReady) _stt.StartRecord(false);
        }

        public void EndListening()
        {
            if (_stt != null) _stt.StopRecord();
        }

        private void OnFinishedRecord(AudioClip clip, float[] raw)
        {
            if (clip == null || raw == null) return;
            var config = RecognitionConfig.GetDefault();
            config.languageCode = language.Parse();
            config.audioChannelCount = clip.channels;
            _stt.Recognize(new GeneralRecognitionRequest
            {
                audio = new RecognitionAudioContent { content = raw.ToBase64() },
                config = config
            });
        }

        private void OnRecognized(RecognitionResponse response)
        {
            if (response?.results == null || response.results.Length == 0) return;
            var alternatives = response.results[0].alternatives;
            if (alternatives == null || alternatives.Length == 0) return;
            TranscriptReady?.Invoke(alternatives[0].transcript);
        }

        private void OnFailed(string error)
        {
            Debug.LogWarning($"[GoogleCloudSpeechInput] Recognition failed: {error}");
        }
    }
}
