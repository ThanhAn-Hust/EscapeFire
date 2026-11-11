using System.Collections;
using UnityEngine;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Double exit door: two leaves on hinge pivots that swing out into the hallway when opened.
    /// Built by EscapeFire &gt; M6 &gt; Fix Room Shell &amp; Exit Door; opened through <see cref="DoorHeatChecker.OpenDoor"/>.
    /// </summary>
    public class ExitDoor : MonoBehaviour
    {
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [SerializeField] private float openDegrees = 100f;
        [SerializeField] private float openSeconds = 1.4f;
        [Tooltip("Solid collider of the closed door; disabled once open so the learner can walk through.")]
        [SerializeField] private Collider blockingCollider;

        public bool IsOpen { get; private set; }

        public void Configure(Transform left, Transform right, Collider blocker)
        {
            leftHinge = left;
            rightHinge = right;
            blockingCollider = blocker;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (blockingCollider != null) blockingCollider.enabled = false;
            StartCoroutine(Swing());
        }

        private IEnumerator Swing()
        {
            Quaternion l0 = leftHinge != null ? leftHinge.localRotation : Quaternion.identity;
            Quaternion r0 = rightHinge != null ? rightHinge.localRotation : Quaternion.identity;
            // Leaves open outward (towards the hallway, local -Z): left turns one way, right the other.
            Quaternion l1 = l0 * Quaternion.Euler(0f, openDegrees, 0f);
            Quaternion r1 = r0 * Quaternion.Euler(0f, -openDegrees, 0f);
            for (float t = 0f; t < openSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / openSeconds);
                if (leftHinge != null) leftHinge.localRotation = Quaternion.Slerp(l0, l1, k);
                if (rightHinge != null) rightHinge.localRotation = Quaternion.Slerp(r0, r1, k);
                yield return null;
            }
            if (leftHinge != null) leftHinge.localRotation = l1;
            if (rightHinge != null) rightHinge.localRotation = r1;
        }
    }
}
