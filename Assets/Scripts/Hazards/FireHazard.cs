using System;
using UnityEngine;

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

        public event Action OnFireExtinguished;

        private void Awake()
        {
            ResetFire();
        }

        public void ResetFire()
        {
            FireIntensity = initialIntensity;
            if (fireParticles != null && !fireParticles.isPlaying)
            {
                fireParticles.Play();
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
                OnFireExtinguished?.Invoke();
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
