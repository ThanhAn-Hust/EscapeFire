using System;
using UnityEngine;
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
        public event Action OnAlarmActivated;

        public void PushAlarmButton()
        {
            if (IsActivated) return;

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
