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
