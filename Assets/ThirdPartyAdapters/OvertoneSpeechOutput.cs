using LeastSquares.Overtone;
using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.Adapters
{
    /// <summary>
    /// Plugs Overtone offline TTS into <see cref="SpeechServices"/>. Lives outside the EscapeFire asmdefs because
    /// Overtone compiles into Assembly-CSharp. Put it on the same GameObject as Overtone's TTSPlayer
    /// (with TTSEngine + TTSVoice configured, e.g. a downloaded Vietnamese voice).
    /// </summary>
    [RequireComponent(typeof(TTSPlayer))]
    public class OvertoneSpeechOutput : MonoBehaviour, ISpeechOutput
    {
        private TTSPlayer _player;

        public bool IsReady =>
            _player != null && _player.Engine != null && _player.Engine.Loaded &&
            _player.Voice != null && _player.Voice.VoiceModel != null && _player.source != null;

        private void Awake() => _player = GetComponent<TTSPlayer>();

        private void OnEnable() => SpeechServices.Register(this);

        private void OnDisable() => SpeechServices.Unregister(this);

        public async void Speak(string text)
        {
            if (!IsReady || string.IsNullOrEmpty(text)) return;
            try
            {
                await _player.Speak(text);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[OvertoneSpeechOutput] TTS failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_player != null && _player.source != null) _player.source.Stop();
        }
    }
}
