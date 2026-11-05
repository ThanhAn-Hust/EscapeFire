using System;
using UnityEngine;
using EscapeFire.Core;

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
        public bool IsPinJammed => _pullsRemaining > 1;

        /// <summary>Seconds of extinguishing agent left (infinite unless the low-pressure anomaly is active).</summary>
        public float RemainingAgentSeconds { get; private set; } = float.PositiveInfinity;
        public bool IsEmpty => RemainingAgentSeconds <= 0f;
        public bool HasLowPressure => !float.IsPositiveInfinity(RemainingAgentSeconds);

        /// <summary>Anomaly TRIGGER_EXTINGUISHER_LOW_PRESSURE: the gauge is in the red, only a few seconds of agent.</summary>
        public void SetLowPressure(float secondsOfAgent)
        {
            RemainingAgentSeconds = Mathf.Max(0.5f, secondsOfAgent);
        }

        private void Update()
        {
            if (!IsSpraying || !HasLowPressure) return;
            RemainingAgentSeconds -= Time.deltaTime;
            if (RemainingAgentSeconds > 0f) return;
            RemainingAgentSeconds = 0f;
            StopSpray();
            EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(
                "Bình đã hết áp! Còn bình nào an toàn cho cháy điện không, hay đã đến lúc sơ tán?"));
        }

        private int _pullsRemaining = 1;

        public event Action OnSafetyPinPulled;
        public event Action OnShockHazardTriggered;

        /// <summary>Anomaly: the safety pin sticks and needs <paramref name="requiredPulls"/> firm pulls.</summary>
        public void JamSafetyPin(int requiredPulls = 2)
        {
            if (IsPinPulled) return;
            _pullsRemaining = Mathf.Max(1, requiredPulls);
        }

        public void PullSafetyPin()
        {
            if (IsPinPulled) return;
            if (_pullsRemaining > 1)
            {
                _pullsRemaining--;
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(
                    "Chốt an toàn bị kẹt! Giữ chắc thân bình, xoay nhẹ chốt và giật mạnh thêm lần nữa."));
                return;
            }

            IsPinPulled = true;
            if (safetyPinObject != null)
            {
                safetyPinObject.SetActive(false);
            }
            OnSafetyPinPulled?.Invoke();
            EventBus<ExtinguisherPinPulledEvent>.Publish(new ExtinguisherPinPulledEvent(type));
        }

        public bool TrySpray(bool isElectricalPowerLive = false)
        {
            if (IsEmpty) return false;
            if (!IsPinPulled)
            {
                Debug.LogWarning("[FireExtinguisher] Cannot spray: Safety pin is still attached!");
                return false;
            }

            if (!IsSpraying) EventBus<ExtinguisherSprayEvent>.Publish(new ExtinguisherSprayEvent(type, isElectricalPowerLive));

            // DANGEROUS RULE: Water/Foam used on LIVE electrical circuit triggers electrocution!
            if (type == ExtinguisherType.WaterFoam && isElectricalPowerLive)
            {
                Debug.LogWarning("[FireExtinguisher] CRITICAL HAZARD: Water/Foam sprayed on LIVE electrical fire!");
                OnShockHazardTriggered?.Invoke();
                EventBus<ShockHazardEvent>.Publish(new ShockHazardEvent(type));
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
