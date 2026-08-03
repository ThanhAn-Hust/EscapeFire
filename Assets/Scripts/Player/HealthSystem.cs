using System;
using UnityEngine;

namespace EscapeFire.Player
{
    /// <summary>
    /// Manages player overall health, fire damage, and electrical shock status.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        
        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float> OnHealthChanged;
        public event Action<string> OnPlayerDied;

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        public void TakeDamage(float amount, string cause = "Unknown Hazard")
        {
            if (IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth);

            if (CurrentHealth <= 0f)
            {
                OnPlayerDied?.Invoke(cause);
            }
        }
    }
}
