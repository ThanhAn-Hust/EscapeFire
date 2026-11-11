using System;
using UnityEngine;
using EscapeFire.Core;

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
        [Tooltip("Real lever pulled down by hand (B.1-3). When set, the breaker is only switched by moving it.")]
        [SerializeField] private HingeGrab lever;

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
            EventBus<PowerStateChangedEvent>.Publish(new PowerStateChangedEvent(IsPowerOn));
        }

        public void TogglePower()
        {
            SetPowerState(!IsPowerOn);
        }

        /// <summary>Called when the learner has pulled the lever through its full travel.</summary>
        public void LeverPulledOff() => SetPowerState(false);

        private void UpdateSwitchVisuals()
        {
            if (lever != null && !IsPowerOn && !lever.IsLatched) lever.ForceLatch();
            if (switchHandle != null)
            {
                switchHandle.localEulerAngles = IsPowerOn ? onRotation : offRotation;
            }
        }
    }
}
