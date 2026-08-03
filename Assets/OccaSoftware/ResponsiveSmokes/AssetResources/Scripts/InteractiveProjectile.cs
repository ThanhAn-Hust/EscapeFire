using System.Collections;

using UnityEngine;

namespace OccaSoftware.ResponsiveSmokes.Runtime
{
    [AddComponentMenu("OccaSoftware/Responsive Smoke/Interactive Projectile")]
    public class InteractiveProjectile : MonoBehaviour
    {
        [Header("Size")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Projectiles erode the smoke along the line of travel. This property controls the size of that erosion.")]
        float radius = 0.2f;

        [Header("Lifetime")]
        [SerializeField]
        [Min(0)]
        [Tooltip("The startup lifetime of the projectile.")]
        float fadeInDuration = 0.1f;

        [SerializeField]
        [Min(0)]
        [Tooltip("The cleanup lifetime of the projectile.")]
        float fadeOutDuration = 0.2f;

        [SerializeField]
        [Min(0)]
        [Tooltip("The active lifetime of the projectile.")]
        float activeLifetime = 0.1f;

        [Header("Layer Masks")]
        [SerializeField]
        [Tooltip(
            "The layer for Interactive Smokes. The system will check if any colliders in the direction of travel are on this layer and will attempt to inform them of the projectile."
        )]
        private LayerMask interactiveSmokeLayer;

        [SerializeField]
        [Tooltip("Any layers that should block the projectile cast.")]
        private LayerMask blockingLayerMask = 1;

        [Header("Options")]
        [SerializeField]
        [Tooltip("Set what the smoke should do when it has finished.")]
        private CleanupOptions cleanupOptions = CleanupOptions.Destroy;

        [SerializeField]
        [Tooltip("Set what the smoke should do when it is enabled.")]
        private StartupOptions startupOptions = StartupOptions.OnEnable;

        #region Private Variables
        private float currentIntensity = 1f;
        private bool isAlive = false;
        private Vector3 origin;
        private Vector3 direction;
        private Vector3 endPoint;
        private float cachedRadius;
        #endregion

        #region Public Getters
        /// <summary>
        /// Get the radius of the projectile.
        /// </summary>
        /// <returns></returns>
        public float GetRadius()
        {
            return cachedRadius;
        }

        /// <summary>
        /// Get the current intensity of the projectile.
        /// </summary>
        /// <returns></returns>
        public float GetIntensity()
        {
            return currentIntensity;
        }

        /// <summary>
        /// Returns the start point of the projectile
        /// </summary>
        /// <returns></returns>
        public Vector3 GetStartPoint()
        {
            return origin;
        }

        /// <summary>
        /// Returns the end point of the projectile
        /// </summary>
        /// <returns></returns>
        public Vector3 GetEndPoint()
        {
            return endPoint;
        }

        public bool IsAlive()
        {
            return isAlive;
        }
        #endregion

        private void OnEnable()
        {
            if (startupOptions == StartupOptions.OnEnable)
            {
                Shoot();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 10f);
        }

        /// <summary>
        /// This method triggers the interactive projectile.
        /// Any nearby volumetric smokes will be notified.
        /// </summary>
        public void Shoot()
        {
            Setup();

            InteractiveSmokeHitData hitData = Utilities.ProjectileCast(origin, direction, blockingLayerMask.value, interactiveSmokeLayer.value, this);

            if (hitData.count > 0)
            {
                endPoint = origin + direction * hitData.distance;
                StartCoroutine(ManageLifecycle());
            }
            else
            {
                Cleanup();
            }
        }

        private void Setup()
        {
            origin = transform.position;
            direction = transform.forward;
            cachedRadius = radius;
        }

        private IEnumerator ManageLifecycle()
        {
            isAlive = true;
            yield return StartCoroutine(AdjustIntensity(fadeInDuration, 0f, 1f));
            yield return new WaitForSeconds(activeLifetime);
            yield return StartCoroutine(AdjustIntensity(fadeOutDuration, 1f, 0f));
            Cleanup();
            isAlive = false;
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
    }

    internal struct InteractiveSmokeHitData
    {
        public int count;
        public float distance;

        public InteractiveSmokeHitData(int count, float distance)
        {
            this.count = count;
            this.distance = distance;
        }
    }

    internal static class Utilities
    {
        public static InteractiveSmokeHitData ProjectileCast(
            Vector3 origin,
            Vector3 direction,
            int blockingLayerMask,
            int interactiveSmokeLayer,
            InteractiveProjectile projectileToAdd
        )
        {
            float nearestBlockingHit = GetDistanceToClosestBlocker(origin, direction, blockingLayerMask);

            Collider[] colliders = new Collider[10];
            int sphereCheck = Physics.OverlapSphereNonAlloc(origin, 0.1f, colliders, interactiveSmokeLayer, QueryTriggerInteraction.Collide);

            for (int i = 0; i < sphereCheck; i++)
            {
                if (colliders[i].TryGetComponent(out InteractiveSmoke smoke))
                {
                    smoke.TryAddProjectile(projectileToAdd);
                }
            }

            RaycastHit[] positiveHits = new RaycastHit[10];
            int rayCheck = Physics.RaycastNonAlloc(
                origin,
                direction,
                positiveHits,
                nearestBlockingHit,
                interactiveSmokeLayer,
                QueryTriggerInteraction.Collide
            );

            for (int i = 0; i < rayCheck; i++)
            {
                if (positiveHits[i].collider.TryGetComponent(out InteractiveSmoke smoke))
                {
                    smoke.TryAddProjectile(projectileToAdd);
                }
            }

            return new InteractiveSmokeHitData(rayCheck + sphereCheck, nearestBlockingHit);
        }

        private static float GetDistanceToClosestBlocker(Vector3 origin, Vector3 direction, int layerMask)
        {
            float closestHit = 999f;
            if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, closestHit, layerMask))
            {
                closestHit = hitInfo.distance;
            }

            return closestHit;
        }
    }
}
