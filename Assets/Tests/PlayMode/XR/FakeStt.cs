using System;
using EscapeFire.Core;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// Stand-in for the speech-to-text adapter: the test "says" a sentence and it comes back as the transcript, as
    /// the real STT would after the learner spoke into the radio. Only the microphone is replaced, not the radio.
    /// </summary>
    internal sealed class FakeStt : ISpeechInput
    {
        public bool IsReady => true;
        public bool Listening { get; private set; }
        public void BeginListening() => Listening = true;
        public void EndListening() => Listening = false;
        public event Action<string> TranscriptReady;
        public void Say(string text) => TranscriptReady?.Invoke(text);
    }
}
