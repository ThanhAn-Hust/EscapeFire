using System;
using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Analytics;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Interactive Red Fire Alarm Push Button on wall near exit door.
    /// Awards +10 procedure score when activated by player.
    /// </summary>
    public class FireAlarmButton : MonoBehaviour
    {
        [Header("Visuals & Audio")]
        [SerializeField] private MeshRenderer buttonRenderer;
        [SerializeField] private Color activatedColor = Color.yellow;
        [SerializeField] private AudioSource alarmAudio;

        public bool IsActivated { get; private set; } = false;
        public bool IsCableCut { get; private set; } = false;
        public event Action OnAlarmActivated;

        /// <summary>Anomaly: signal cable cut, the bell stays silent. The learner must raise the alarm by voice.</summary>
        public void CutCable()
        {
            if (IsActivated) return;
            IsCableCut = true;
        }

        /// <summary>Voice command / walkie-talkie alternative (M4) — works even when the cable is cut.</summary>
        public void RaiseAlarmByVoice()
        {
            if (IsActivated) return;
            IsCableCut = false;
            PushAlarmButton();
        }

        public void PushAlarmButton()
        {
            if (IsActivated) return;
            if (IsCableCut)
            {
                if (buttonRenderer != null) buttonRenderer.material.color = Color.gray;
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(
                    "Chuông không kêu — cáp tín hiệu có thể đã đứt! Còn cách nào khác để báo động cho cả tầng?"));
                return;
            }

            IsActivated = true;
            Debug.Log("[FireAlarmButton] Manual Fire Alarm Button Pressed!");

            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = activatedColor;
            }

            if (alarmAudio != null && !alarmAudio.isPlaying)
            {
                alarmAudio.Play();
            }

            if (ScoringManager.Instance != null)
            {
                ScoringManager.Instance.AddScore(10, "Kích hoạt nút nhấn Báo cháy khẩn cấp (+10đ quy trình)");
            }

            OnAlarmActivated?.Invoke();
            EventBus<AlarmActivatedEvent>.Publish(default);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Triggered when VR Hand controller presses button
            if (other.CompareTag("Player") || other.name.Contains("Controller") || other.name.Contains("Hand"))
            {
                PushAlarmButton();
            }
        }
    }
}
