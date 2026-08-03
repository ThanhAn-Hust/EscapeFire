using System;
using UnityEngine;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Controls the electrical panel main breaker switch (Aptomat).
    /// Prevents electrical shock hazards when power is cut off.
    /// </summary>
    public class CircuitBreakerSwitch : MonoBehaviour
    {
        [Header("Switch Visuals")]
        [SerializeField] private Transform switchHandle;
        [SerializeField] private Vector3 onRotation = new Vector3(0, 0, 45);
        [SerializeField] private Vector3 offRotation = new Vector3(0, 0, -45);
        
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip switchSound;

        public bool IsPowerOn { get; private set; } = true;
        public event Action<bool> OnPowerStateChanged;

        private void Start()
        {
            UpdateSwitchVisuals();
        }

        public void SetPowerState(bool powerOn)
        {
            if (IsPowerOn == powerOn) return;

            IsPowerOn = powerOn;
            UpdateSwitchVisuals();

            if (audioSource != null && switchSound != null)
            {
                audioSource.PlayOneShot(switchSound);
            }

            OnPowerStateChanged?.Invoke(IsPowerOn);
        }

        public void TogglePower()
        {
            SetPowerState(!IsPowerOn);
        }

        private void UpdateSwitchVisuals()
        {
            if (switchHandle != null)
            {
                switchHandle.localEulerAngles = IsPowerOn ? onRotation : offRotation;
            }
        }
    }
}
