using System;

namespace EscapeFire.Core
{
    /// <summary>Text-to-speech backend (Overtone offline TTS adapter lives in Assets/ThirdPartyAdapters).</summary>
    public interface ISpeechOutput
    {
        bool IsReady { get; }
        void Speak(string text);
        void Stop();
    }

    /// <summary>Speech-to-text backend (Google Cloud STT adapter lives in Assets/ThirdPartyAdapters).</summary>
    public interface ISpeechInput
    {
        bool IsReady { get; }
        void BeginListening();
        void EndListening();
        event Action<string> TranscriptReady;
    }

    /// <summary>
    /// Registry so EscapeFire.Runtime can use third-party plugins that compile into Assembly-CSharp
    /// (Overtone, Frostweep have no asmdef and cannot be referenced directly).
    /// </summary>
    public static class SpeechServices
    {
        public static ISpeechOutput Output { get; private set; }
        public static ISpeechInput Input { get; private set; }

        public static void Register(ISpeechOutput output) => Output = output;
        public static void Register(ISpeechInput input) => Input = input;

        public static void Unregister(ISpeechOutput output)
        {
            if (ReferenceEquals(Output, output)) Output = null;
        }

        public static void Unregister(ISpeechInput input)
        {
            if (ReferenceEquals(Input, input)) Input = null;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Output = null;
            Input = null;
        }
    }
}
