using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Interactions;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// The 114 operator on the other end of the radio (plan Phần B.1-2): checks the caller's report with
    /// <see cref="Call114Checker"/>, asks back for whatever is missing (spoken by TTS when available, shown as a mentor
    /// line otherwise) and confirms when address, floor, what is burning and trapped people were all given.
    /// </summary>
    public class Call114Dialogue : MonoBehaviour
    {
        [SerializeField] private Radio radio;

        private readonly Call114Checker _checker = new Call114Checker();

        public static Call114Dialogue Instance { get; private set; }
        public Call114Checker Checker => _checker;
        public bool Started { get; private set; }
        public bool IsComplete => _checker.IsComplete;
        public int AskBacks { get; private set; }
        public string LastReply { get; private set; }

        public void Configure(Radio r) => radio = r;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (radio == null) radio = FindFirstObjectByType<Radio>();
            if (radio != null) radio.TranscriptHeard += Hear;
        }

        private void OnDisable()
        {
            if (radio != null) radio.TranscriptHeard -= Hear;
        }

        public void Hear(string transcript)
        {
            if (_checker.IsComplete) return;
            Started = true;
            _checker.Hear(transcript);
            if (_checker.IsComplete)
            {
                Reply("114 đã nhận tin. Xe chữa cháy đang tới. Hướng dẫn mọi người thoát nạn bằng thang bộ, không dùng thang máy!");
                EventBus<Call114CompletedEvent>.Publish(new Call114CompletedEvent(AskBacks));
                return;
            }
            AskBacks++;
            Reply(_checker.NextQuestion());
        }

        private void Reply(string text)
        {
            LastReply = text;
            EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("[114] " + text));
            var tts = SpeechServices.Output;
            if (tts != null && tts.IsReady) tts.Speak(text);
        }
    }
}
