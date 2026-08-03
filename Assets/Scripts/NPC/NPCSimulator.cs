using UnityEngine;
using EscapeFire.Analytics;

namespace EscapeFire.NPC
{
    public enum NPCRole { MinhPanic, HoaFaint }

    /// <summary>
    /// Controls NPC behaviors during fire emergency:
    /// - Minh: Panics, knocks over chair, runs towards exit door.
    /// - Hoa: Faints from smoke, acts as an optional rescue objective.
    /// </summary>
    public class NPCSimulator : MonoBehaviour
    {
        [Header("NPC Settings")]
        [SerializeField] private NPCRole role = NPCRole.MinhPanic;
        [SerializeField] private float runSpeed = 3.5f;

        [Header("References")]
        [SerializeField] private Transform exitDoorTarget;
        [SerializeField] private GameObject chairToKnockOver;
        [SerializeField] private Animator animator;

        public bool IsRescued { get; private set; } = false;
        public bool IsFainted { get; private set; } = false;

        public void TriggerEmergencyReaction()
        {
            if (role == NPCRole.MinhPanic)
            {
                // Minh panics, knocks over chair, runs to door
                if (chairToKnockOver != null)
                {
                    chairToKnockOver.transform.Rotate(Vector3.right, 75f);
                }
                Debug.Log("[NPC] Minh panicked and ran towards exit door!");
                if (animator != null) animator.SetTrigger("PanicRun");
            }
            else if (role == NPCRole.HoaFaint)
            {
                // Hoa faints near wall
                IsFainted = true;
                if (animator != null) animator.SetTrigger("Faint");
                Debug.Log("[NPC] Hoa fainted from smoke inhalation!");
            }
        }

        public void RescueNPC()
        {
            if (role == NPCRole.HoaFaint && IsFainted && !IsRescued)
            {
                IsRescued = true;
                Debug.Log("[NPC] Hoa was successfully rescued by student!");
                if (ScoringManager.Instance != null)
                {
                    ScoringManager.Instance.AddScore(25, "Cứu hộ thành công bạn học Hoa khỏi khói độc");
                }
            }
        }

        private void Update()
        {
            if (role == NPCRole.MinhPanic && exitDoorTarget != null)
            {
                // Smoothly move towards exit door
                transform.position = Vector3.MoveTowards(transform.position, exitDoorTarget.position, runSpeed * Time.deltaTime);
            }
        }
    }
}
