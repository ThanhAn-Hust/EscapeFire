using System;
using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// Manages fire intensity, particle growth, and extinction logic when sprayed by CO2 / powder.
    /// </summary>
    public class FireHazard : MonoBehaviour
    {
        [Header("Fire Properties")]
        [SerializeField] private float initialIntensity = 100f;
        [SerializeField] private float spreadRate = 2f; // Intensity growth per second if left unextinguished

        [Header("Visual Effects & Audio")]
        [SerializeField] private ParticleSystem fireParticles;
        [SerializeField] private AudioSource fireAudioSource;

        public float InitialIntensity { get => initialIntensity; set => initialIntensity = value; }
        public float FireIntensity { get; private set; }
        public bool IsExtinguished => FireIntensity <= 0f;
        public ParticleSystem Particles => fireParticles;
        /// <summary>Base of the flames: where the agent has to go (PASS "Aim at the base").</summary>
        public Vector3 BasePoint => fireParticles != null ? fireParticles.transform.position : transform.position;

        private static readonly System.Collections.Generic.List<FireHazard> s_active = new System.Collections.Generic.List<FireHazard>();
        /// <summary>Fires currently enabled in the scene (burning or not).</summary>
        public static System.Collections.Generic.IReadOnlyList<FireHazard> Active => s_active;
        /// <summary>True when the PerformanceBudgetGovernor refused this emitter (fire stays dark, no events).</summary>
        public bool IsSuppressed { get; private set; }

        public event Action OnFireExtinguished;

        private void Awake()
        {
            ResolveParticles();
            ResetFire();
        }

        private void OnEnable()
        {
            if (!s_active.Contains(this)) s_active.Add(this);
            var governor = PerformanceBudgetGovernor.Instance;
            IsSuppressed = governor != null && !governor.TryRegisterFire(this);
            if (IsSuppressed) GoDark();
        }

        private void OnDisable()
        {
            s_active.Remove(this);
            if (PerformanceBudgetGovernor.Instance != null) PerformanceBudgetGovernor.Instance.UnregisterFire(this);
        }

        private void GoDark()
        {
            FireIntensity = 0f;
            if (fireParticles != null) fireParticles.Stop();
            if (fireAudioSource != null) fireAudioSource.Stop();
        }

        /// <summary>
        /// The lab scene leaves <see cref="fireParticles"/> unassigned on the server rack; fall back to the first
        /// (root) flame system under this object, including the inactive one the scene keeps hidden until phase 2.
        /// </summary>
        public void ResolveParticles()
        {
            if (fireParticles == null) fireParticles = GetComponentInChildren<ParticleSystem>(true);
        }

        public void ResetFire()
        {
            if (IsSuppressed)
            {
                GoDark();
                return;
            }
            FireIntensity = initialIntensity;
            if (fireParticles != null)
            {
                // The scene keeps the flame object switched off until the fire starts; Play() alone does nothing then.
                if (!fireParticles.gameObject.activeSelf) fireParticles.gameObject.SetActive(true);
                if (!fireParticles.isPlaying) fireParticles.Play();
            }
            if (fireAudioSource != null && !fireAudioSource.isPlaying)
            {
                fireAudioSource.Play();
            }
        }

        private void Update()
        {
            if (!IsExtinguished)
            {
                // Fire slowly grows if not being sprayed
                FireIntensity += spreadRate * Time.deltaTime;
                UpdateFireScale();
            }
        }

        public void Extinguish(float amount)
        {
            if (IsExtinguished) return;

            FireIntensity = Mathf.Max(0f, FireIntensity - amount);
            UpdateFireScale();

            if (IsExtinguished)
            {
                if (fireParticles != null) fireParticles.Stop();
                if (fireAudioSource != null) fireAudioSource.Stop();
                if (PerformanceBudgetGovernor.Instance != null) PerformanceBudgetGovernor.Instance.UnregisterFire(this);
                OnFireExtinguished?.Invoke();
                EventBus<FireExtinguishedEvent>.Publish(new FireExtinguishedEvent(GetInstanceID()));
            }
        }

        private void UpdateFireScale()
        {
            if (fireParticles != null)
            {
                float scale = Mathf.Clamp(FireIntensity / initialIntensity, 0.1f, 2.0f);
                fireParticles.transform.localScale = Vector3.one * scale;
            }
        }

        private void OnParticleCollision(GameObject other)
        {
            // Detect if colliding with extinguisher spray particles
            var extinguisher = other.GetComponentInParent<Interactions.FireExtinguisher>();
            if (extinguisher != null && extinguisher.IsSpraying)
            {
                Extinguish(extinguisher.ExtinguishPowerPerSecond * Time.deltaTime);
            }
        }
    }
}
