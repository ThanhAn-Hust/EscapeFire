using UnityEngine;
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
        [SerializeField] private bool isDoorHot = true;
        [SerializeField] private float heatCheckDistance = 0.25f;

        [Header("Visuals & Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hotWarningSound;

        public bool HasCheckedHeat { get; private set; } = false;

        public void CheckDoorHeatWithHand()
        {
            if (HasCheckedHeat) return;

            HasCheckedHeat = true;
            Debug.Log("[DoorHeatChecker] Player checked door handle heat with back of hand!");

            if (isDoorHot)
            {
                Debug.LogWarning("[DoorHeatChecker] WARNING: Door handle is extremely HOT!");
                if (audioSource != null && hotWarningSound != null)
                {
                    audioSource.PlayOneShot(hotWarningSound);
                }

                if (ScoringManager.Instance != null)
                {
                    ScoringManager.Instance.AddScore(10, "Kiểm tra nhiệt độ tay nắm cửa bằng mu bàn tay (+10đ quy trình)");
                }
            }
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
                CheckDoorHeatWithHand();
            }
        }
    }
}
