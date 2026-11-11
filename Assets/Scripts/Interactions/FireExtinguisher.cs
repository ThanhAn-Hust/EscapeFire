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

        [Header("Realism (plan Phần B.1-5)")]
        [Tooltip("Discharge time of a full bottle (rule R05: CO2 MT5 5 kg ≥ 10 s, EU 5 kg 13–15 s → 12 s).")]
        [SerializeField] private float fullChargeSeconds = 12f;
        [Tooltip("Horn / hose nozzle tip. The jet leaves along its forward axis.")]
        [SerializeField] private Transform nozzle;

        public ExtinguisherType Type => type;
        /// <summary>Nozzle transform (the jet points along its forward axis); aim it at the base of the fire.</summary>
        public Transform Nozzle => nozzle != null ? nozzle : sprayParticles != null ? sprayParticles.transform : transform;
        /// <summary>How hard the trigger is squeezed (0..1). Drives the flow rate.</summary>
        public float Flow { get; set; } = 1f;
        public float FullChargeSeconds => fullChargeSeconds;

        private void Awake()
        {
            RemainingAgentSeconds = fullChargeSeconds > 0f ? fullChargeSeconds : float.PositiveInfinity;
            if (sprayParticles == null) sprayParticles = BuildSprayJet();
            if (nozzle != null) SetNozzle(nozzle);
            if (GetComponent<ExtinguisherSprayEffect>() == null) gameObject.AddComponent<ExtinguisherSprayEffect>();
        }

        /// <summary>Moves the jet to a horn / hose tip so the learner aims with the horn, not the bottle.</summary>
        public void SetNozzle(Transform tip)
        {
            nozzle = tip;
            if (sprayParticles != null && tip != null)
            {
                sprayParticles.transform.SetParent(tip, false);
                sprayParticles.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        /// <summary>
        /// No spray effect was assigned in the lab scene, so pulling the trigger showed nothing. Build a jet at the top
        /// of the bottle: CO2 = white freezing cloud, water/foam = pale blue mist.
        /// </summary>
        private ParticleSystem BuildSprayJet()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(transform.position, Vector3.one * 0.4f);
            foreach (var r in renderers) b.Encapsulate(r.bounds);

            var go = new GameObject("SprayJet");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(new Vector3(b.center.x, b.max.y - 0.05f, b.center.z) + transform.forward * 0.12f, transform.rotation);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = type == ExtinguisherType.CO2 ? new Color(0.95f, 0.97f, 1f, 0.85f) : new Color(0.75f, 0.88f, 1f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 700;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate; // empty bounds would otherwise never start

            var emission = ps.emission;
            emission.rateOverTime = 260f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 7f;
            shape.radius = 0.02f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 4f));
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = grad;
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 2.5f;
            limit.dampen = 0.08f;

            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = RuntimeVisuals.ParticleMaterial(Color.white);
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }
        public float ExtinguishPowerPerSecond => extinguishPowerPerSecond;
        public bool IsPinPulled { get; set; } = false;
        public bool IsSpraying { get; private set; } = false;
        public bool IsPinJammed => _pullsRemaining > 1;

        /// <summary>Seconds of extinguishing agent left (infinite unless the low-pressure anomaly is active).</summary>
        public float RemainingAgentSeconds { get; private set; } = float.PositiveInfinity;
        public bool IsEmpty => RemainingAgentSeconds <= 0f;
        /// <summary>AI anomaly applied (gauge in the red): only a few seconds of agent.</summary>
        public bool HasLowPressure { get; private set; }

        /// <summary>Anomaly TRIGGER_EXTINGUISHER_LOW_PRESSURE: the gauge is in the red, only a few seconds of agent.</summary>
        public void SetLowPressure(float secondsOfAgent)
        {
            HasLowPressure = true;
            RemainingAgentSeconds = Mathf.Min(RemainingAgentSeconds, Mathf.Max(0.5f, secondsOfAgent));
        }

        private void Update()
        {
            if (!IsSpraying || float.IsPositiveInfinity(RemainingAgentSeconds)) return;
            RemainingAgentSeconds -= Time.deltaTime * Mathf.Clamp01(Flow);
            if (RemainingAgentSeconds > 0f) return;
            RemainingAgentSeconds = 0f;
            StopSpray();
            EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(
                "Bình đã hết! Còn bình nào an toàn cho cháy điện không, hay đã đến lúc sơ tán?"));
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
