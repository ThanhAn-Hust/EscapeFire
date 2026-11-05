using UnityEngine;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// Controls volumetric smoke density and smoke layer lowering over time.
    /// </summary>
    public class SmokeDensityController : MonoBehaviour
    {
        [Header("Smoke Settings")]
        [SerializeField] private float initialSmokeHeight = 3.2f;
        [SerializeField] private float minSmokeHeight = 1.0f; // Height smoke lowers down to
        [SerializeField] private float smokeLoweringSpeed = 0.05f; // Meters per second

        [Header("References")]
        [SerializeField] private FireHazard targetFire;
        [SerializeField] private Transform smokeVolumeTransform;

        public float CurrentSmokeHeight { get; private set; }
        public bool HasSurged { get; private set; }

        /// <summary>True while the layer is high enough for a sudden drop to matter.</summary>
        public bool CanSurge => !HasSurged && CurrentSmokeHeight - minSmokeHeight > 0.9f;

        /// <summary>Anomaly TRIGGER_SMOKE_SURGE: thick smoke suddenly rolls down and keeps lowering faster.</summary>
        public bool Surge(float drop = 0.8f, float speedMultiplier = 2f)
        {
            if (!CanSurge) return false;
            HasSurged = true;
            CurrentSmokeHeight = Mathf.Max(minSmokeHeight, CurrentSmokeHeight - drop);
            smokeLoweringSpeed *= speedMultiplier;
            if (smokeVolumeTransform != null)
            {
                Vector3 pos = smokeVolumeTransform.position;
                pos.y = CurrentSmokeHeight;
                smokeVolumeTransform.position = pos;
            }
            return true;
        }

        private void Start()
        {
            if (targetFire == null && Scenario.LabProgressiveScenarioController.Instance != null)
            {
                targetFire = Scenario.LabProgressiveScenarioController.Instance.ServerFire;
            }
        }

        private void Awake()
        {
            CurrentSmokeHeight = initialSmokeHeight;
        }

        private void Update()
        {
            // Only lower smoke while the fire is actually burning (inactive in Phase 0/1).
            if (targetFire != null && targetFire.isActiveAndEnabled && !targetFire.IsExtinguished)
            {
                // Smoke lowers down as fire burns
                CurrentSmokeHeight = Mathf.Max(minSmokeHeight, CurrentSmokeHeight - (smokeLoweringSpeed * Time.deltaTime));

                if (smokeVolumeTransform != null)
                {
                    Vector3 pos = smokeVolumeTransform.position;
                    pos.y = CurrentSmokeHeight;
                    smokeVolumeTransform.position = pos;
                }
            }
        }
    }
}
