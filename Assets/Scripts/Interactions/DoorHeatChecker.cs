using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Analytics;
using EscapeFire.Scenario;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Checks door handle temperature before opening exit door.
    /// Prevents Backdraft explosions and awards procedure points for checking with back of hand.
    /// </summary>
    public class DoorHeatChecker : MonoBehaviour
    {
        [Header("Door Heat Settings")]
        [Tooltip("The lab exit leads to a safe hallway, so its handle is cool. Set true for a door with fire behind it.")]
        [SerializeField] private bool isDoorHot = false;
        [SerializeField] private float heatCheckDistance = 0.25f;

        [Header("Visuals & Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hotWarningSound;

        public bool HasCheckedHeat { get; private set; } = false;
        public bool IsDoorHot => isDoorHot;

        public void CheckDoorHeatWithHand()
        {
            if (HasCheckedHeat) return;

            HasCheckedHeat = true;
            Debug.Log("[DoorHeatChecker] Player checked door handle heat with back of hand!");

            // Checking is the correct procedure whatever the result (NFPA 101), so it always earns the points.
            if (ScoringManager.Instance != null)
            {
                ScoringManager.Instance.AddScore(10, "Kiểm tra nhiệt độ tay nắm cửa bằng mu bàn tay (+10đ quy trình)");
            }
            if (isDoorHot)
            {
                Debug.LogWarning("[DoorHeatChecker] WARNING: Door handle is extremely HOT!");
                if (audioSource != null && hotWarningSound != null)
                {
                    audioSource.PlayOneShot(hotWarningSound);
                }
            }

            EventBus<DoorHeatCheckedEvent>.Publish(new DoorHeatCheckedEvent(isDoorHot));
        }

        /// <summary>Opens the exit door (after the heat check; an unchecked hot door costs points first).</summary>
        public void OpenDoor()
        {
            CanOpenDoorSafely();
            var door = GetComponentInParent<ExitDoor>();
            if (door == null) door = GetComponentInChildren<ExitDoor>();
            if (door != null) door.Open();
        }

        public bool CanOpenDoorSafely()
        {
            if (isDoorHot && !HasCheckedHeat)
            {
                Debug.LogWarning("[DoorHeatChecker] Unsafe door opening without heat check!");
                if (ScoringManager.Instance != null)
                {
                    ScoringManager.Instance.DeductScore(15, "Mở cửa vội vàng không kiểm tra nhiệt độ tay nắm (-15đ)");
                }
            }
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.name.Contains("Hand") || other.name.Contains("Controller") || other.CompareTag("Player"))
            {
                // First touch: back of the hand on the handle. Touching again once it is known to be cool opens the door.
                if (!HasCheckedHeat) CheckDoorHeatWithHand();
                else if (!isDoorHot) OpenDoor();
            }
        }
    }
}
