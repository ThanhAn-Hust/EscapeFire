using System;
using UnityEngine;

namespace EscapeFire.Interactions
{
    public enum ExtinguisherType
    {
        CO2,        // Carbon Dioxide - Safe for electrical fires
        WaterFoam,  // Water/Foam - Dangerous on live electrical fires!
        DryPowder   // MFZ4 Powder - General purpose
    }

    /// <summary>
    /// Controls 6DOF physical fire extinguisher behavior (safety pin, nozzle spraying, shock hazard detection).
    /// </summary>
    public class FireExtinguisher : MonoBehaviour
    {
        [Header("Extinguisher Properties")]
        [SerializeField] private ExtinguisherType type = ExtinguisherType.CO2;
        [SerializeField] private float sprayForce = 10f;
        [SerializeField] private float extinguishPowerPerSecond = 25f;

        [Header("Visual Effects & Audio")]
        [SerializeField] private ParticleSystem sprayParticles;
        [SerializeField] private GameObject safetyPinObject;
        [SerializeField] private AudioSource sprayAudioSource;

        public ExtinguisherType Type => type;
        public float ExtinguishPowerPerSecond => extinguishPowerPerSecond;
        public bool IsPinPulled { get; set; } = false;
        public bool IsSpraying { get; private set; } = false;

        public event Action OnSafetyPinPulled;
        public event Action OnShockHazardTriggered;

        public void PullSafetyPin()
        {
            if (IsPinPulled) return;

            IsPinPulled = true;
            if (safetyPinObject != null)
            {
                safetyPinObject.SetActive(false);
            }
            OnSafetyPinPulled?.Invoke();
        }

        public bool TrySpray(bool isElectricalPowerLive = false)
        {
            if (!IsPinPulled)
            {
                Debug.LogWarning("[FireExtinguisher] Cannot spray: Safety pin is still attached!");
                return false;
            }

            // DANGEROUS RULE: Water/Foam used on LIVE electrical circuit triggers electrocution!
            if (type == ExtinguisherType.WaterFoam && isElectricalPowerLive)
            {
                Debug.LogError("[FireExtinguisher] CRITICAL HAZARD: Water/Foam sprayed on LIVE electrical fire!");
                OnShockHazardTriggered?.Invoke();
                return false;
            }

            IsSpraying = true;
            if (sprayParticles != null && !sprayParticles.isPlaying)
            {
                sprayParticles.Play();
            }
            if (sprayAudioSource != null && !sprayAudioSource.isPlaying)
            {
                sprayAudioSource.Play();
            }

            return true;
        }

        public void StopSpray()
        {
            IsSpraying = false;
            if (sprayParticles != null && sprayParticles.isPlaying)
            {
                sprayParticles.Stop();
            }
            if (sprayAudioSource != null && sprayAudioSource.isPlaying)
            {
                sprayAudioSource.Stop();
            }
        }
    }
}
