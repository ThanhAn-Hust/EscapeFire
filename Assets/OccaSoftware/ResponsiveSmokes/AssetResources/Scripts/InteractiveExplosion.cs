using System.Collections;

using UnityEngine;

namespace OccaSoftware.ResponsiveSmokes.Runtime
{
    [AddComponentMenu("OccaSoftware/Responsive Smoke/Interactive Explosion")]
    public class InteractiveExplosion : MonoBehaviour
    {
        [Header("Size")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Explosions erode a sphere around the explosion point. This property controls the radius of that sphere.")]
        float radius = 2;

        [Header("Lifetime")]
        [SerializeField]
        [Min(0)]
        [Tooltip("The startup lifetime of the explosion.")]
        float fadeInDuration = 0.2f;

        [SerializeField]
        [Min(0)]
        [Tooltip("The cleanup lifetime of the explosion.")]
        float fadeOutDuration = 1f;

        [SerializeField]
        [Min(0)]
        [Tooltip("The active lifetime of the explosion.")]
        float activeLifetime = 5f;

        [Header("Layer Masks")]
        [SerializeField]
        [Tooltip(
            "The layer for Interactive Smokes. The system will check if any nearby colliders are on this layer and will attempt to inform them of the explosion."
        )]
        private LayerMask interactiveSmokeLayer;

        [Header("Options")]
        [SerializeField]
        [Tooltip("Set what the smoke should do when it has finished.")]
        private CleanupOptions cleanupOptions = CleanupOptions.Destroy;

        [SerializeField]
        [Tooltip("Set what the smoke should do when it is enabled.")]
        private StartupOptions startupOptions = StartupOptions.OnEnable;

        #region Private Variables
        private bool isAlive = false;
        private float currentIntensity = 1f;
        private Vector3 origin;
        private float cachedRadius;
        #endregion

        #region Public Getters
        /// <summary>
        /// Get the radius of the explosion.
        /// </summary>
        /// <returns></returns>
        public float GetRadius()
        {
            return cachedRadius;
        }

        /// <summary>
        /// Get the current intensity of the explosion.
        /// </summary>
        /// <returns></returns>
        public float GetIntensity()
        {
            return currentIntensity;
        }

        /// <summary>
        /// Get whether the explosion is currently active.
        /// </summary>
        /// <returns></returns>
        public bool IsAlive()
        {
            return isAlive;
        }

        public Vector3 GetOrigin()
        {
            return origin;
        }
        #endregion

        private void OnEnable()
        {
            if (startupOptions == StartupOptions.OnEnable)
            {
                Explode();
            }
        }

        /// <summary>
        /// This method triggers the interactive explosion.
        /// Any nearby volumetric smokes will be notified.
        /// </summary>
        public void Explode()
        {
            origin = transform.position;
            cachedRadius = radius;

            Collider[] colliders = new Collider[10];

            int count = Physics.OverlapSphereNonAlloc(origin, cachedRadius, colliders, interactiveSmokeLayer.value, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].TryGetComponent(out InteractiveSmoke smoke))
                {
                    smoke.TryAddExplosion(this);
                }
            }

            if (count > 0)
            {
                StartCoroutine(ManageLifecycle());
            }
        }

        private IEnumerator ManageLifecycle()
        {
            isAlive = true;
            yield return StartCoroutine(AdjustIntensity(fadeInDuration, 0f, 1f));
            yield return new WaitForSeconds(activeLifetime);
            yield return StartCoroutine(AdjustIntensity(fadeOutDuration, 1f, 0f));
            isAlive = false;
            Cleanup();
        }

        private void Cleanup()
        {
            if (Application.isPlaying)
            {
                if (cleanupOptions == CleanupOptions.Destroy)
                {
                    Destroy(gameObject);
                }
                else if (cleanupOptions == CleanupOptions.Disable)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator AdjustIntensity(float duration, float start, float end)
        {
            float timer = 0;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                currentIntensity = Mathf.Lerp(start, end, timer / duration);
                yield return null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, cachedRadius);
        }
    }
}
