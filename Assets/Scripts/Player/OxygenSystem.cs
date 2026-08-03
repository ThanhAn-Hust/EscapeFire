using System;
using UnityEngine;

namespace EscapeFire.Player
{
    /// <summary>
    /// Manages player oxygen levels based on smoke density and crouching posture.
    /// </summary>
    public class OxygenSystem : MonoBehaviour
    {
        [Header("Oxygen Settings")]
        [SerializeField] private float maxOxygen = 100f;
        [SerializeField] private float depletionRateInSmoke = 10f; // Oxygen lost per second when standing in smoke
        [SerializeField] private float recoveryRate = 5f;

        [Header("References")]
        [SerializeField] private VRHeightDetector heightDetector;

        public float MaxOxygen { get => maxOxygen; set => maxOxygen = value; }
        public float CurrentOxygen { get; private set; }
        public bool IsDepleted => CurrentOxygen <= 0f;

        public event Action<float> OnOxygenChanged;
        public event Action OnOxygenDepleted;

        private void Awake()
        {
            if (heightDetector == null)
            {
                heightDetector = GetComponent<VRHeightDetector>();
            }
            ResetOxygen();
        }

        public void ResetOxygen()
        {
            CurrentOxygen = maxOxygen;
            OnOxygenChanged?.Invoke(CurrentOxygen);
        }

        private void Update()
        {
            // If standing upright in smoke area (heightDetector.IsCrouching is false)
            if (heightDetector != null && !heightDetector.IsCrouching)
            {
                DepleteOxygen(depletionRateInSmoke * Time.deltaTime);
            }
            else if (CurrentOxygen < maxOxygen)
            {
                RecoverOxygen(recoveryRate * Time.deltaTime);
            }
        }

        public void DepleteOxygen(float amount)
        {
            if (IsDepleted) return;

            CurrentOxygen = Mathf.Max(0f, CurrentOxygen - amount);
            OnOxygenChanged?.Invoke(CurrentOxygen);

            if (CurrentOxygen <= 0f)
            {
                OnOxygenDepleted?.Invoke();
            }
        }

        public void RecoverOxygen(float amount)
        {
            CurrentOxygen = Mathf.Min(maxOxygen, CurrentOxygen + amount);
            OnOxygenChanged?.Invoke(CurrentOxygen);
        }
    }
}
