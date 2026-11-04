using UnityEngine;
using UnityEngine.InputSystem;
using EscapeFire.Core;
using EscapeFire.Interactions;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Walkie-talkie: hold the push-to-talk button (default: left controller X / primaryButton), speak, release.
    /// The transcript is parsed by <see cref="VoiceCommandParser"/>; "Cháy! Có cháy!" raises the fire alarm even
    /// when the alarm cable was cut (anomaly TRIGGER_ALARM_CABLE_CUT).
    /// Without an STT backend (no Google key / offline), pressing the key in the Editor injects
    /// <see cref="editorFallbackTranscript"/> so the flow can be tested.
    /// </summary>
    public class VoiceCommandRouter : MonoBehaviour
    {
        [SerializeField] private InputActionReference pushToTalk;
        [SerializeField] private Key editorKey = Key.V;
        [SerializeField] private string editorFallbackTranscript = "Cháy! Có cháy! Báo cháy tầng này!";

        private InputAction _fallbackAction;
        private bool _listening;
        private ISpeechInput _subscribedInput;

        private InputAction TalkAction => pushToTalk != null ? pushToTalk.action : _fallbackAction;

        private void Awake()
        {
            if (pushToTalk == null)
            {
                _fallbackAction = new InputAction("PushToTalk", InputActionType.Button);
                _fallbackAction.AddBinding("<XRController>{LeftHand}/primaryButton");
                _fallbackAction.AddBinding("<Keyboard>/" + editorKey.ToString().ToLowerInvariant());
            }
        }

        private void OnEnable()
        {
            var action = TalkAction;
            action.started += OnTalkStarted;
            action.canceled += OnTalkReleased;
            action.Enable();
        }

        private void OnDisable()
        {
            var action = TalkAction;
            action.started -= OnTalkStarted;
            action.canceled -= OnTalkReleased;
            if (pushToTalk == null) action.Disable();
            if (_subscribedInput != null) _subscribedInput.TranscriptReady -= HandleTranscript;
            _subscribedInput = null;
        }

        private void OnDestroy() => _fallbackAction?.Dispose();

        private void OnTalkStarted(InputAction.CallbackContext _)
        {
            var input = SpeechServices.Input;
            if (input == null || !input.IsReady) return;
            if (_subscribedInput != input)
            {
                if (_subscribedInput != null) _subscribedInput.TranscriptReady -= HandleTranscript;
                input.TranscriptReady += HandleTranscript;
                _subscribedInput = input;
            }
            _listening = true;
            input.BeginListening();
        }

        private void OnTalkReleased(InputAction.CallbackContext _)
        {
            if (_listening && SpeechServices.Input != null)
            {
                _listening = false;
                SpeechServices.Input.EndListening();
                return;
            }
            if (Application.isEditor) HandleTranscript(editorFallbackTranscript);
        }

        /// <summary>Entry point for any STT backend (or tests / debug UI).</summary>
        public void HandleTranscript(string transcript)
        {
            var command = VoiceCommandParser.Parse(transcript);
            Debug.Log($"[VoiceCommand] \"{transcript}\" -> {command}");
            EventBus<VoiceCommandEvent>.Publish(new VoiceCommandEvent(command, transcript ?? string.Empty));

            switch (command)
            {
                case VoiceCommand.RaiseFireAlarm:
                    foreach (var alarm in FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    {
                        alarm.RaiseAlarmByVoice();
                    }
                    break;

                case VoiceCommand.CallForHelp:
                    var controller = LabProgressiveScenarioController.Instance;
                    controller?.SetAIMentorGuidance("Đã nhận yêu cầu cứu hộ. Lực lượng PCCC 114 đang trên đường. Tiếp tục khom người và di chuyển về lối thoát!");
                    break;
            }
        }
    }
}
