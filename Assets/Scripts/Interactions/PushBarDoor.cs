using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Stairwell fire door with a push bar (plan Phần B.1-9, rule R10): push the bar with a hand (≥ 2 cm) and the door
    /// swings open the way you push; it stays open while someone is in the doorway, then the door closer shuts it
    /// again (a closed fire door keeps the smoke out of the stairs).
    /// </summary>
    public class PushBarDoor : MonoBehaviour
    {
        [SerializeField] private Transform leaf;           // hinge pivot of the door leaf
        [SerializeField] private Collider bar;             // the push bar (its collider defines where to push)
        [SerializeField] private Collider blocker;         // closed-door collider
        [Tooltip("World direction the door is pushed (away from the side the bar is on).")]
        [SerializeField] private Vector3 pushDirection = Vector3.forward;
        [SerializeField] private float openAngle = 95f;
        [SerializeField] private float openSign = 1f;
        [SerializeField] private float barTravel = 0.02f;
        [SerializeField] private float openSeconds = 0.9f;
        [SerializeField] private float closeSeconds = 3.5f;
        [SerializeField] private float holdOpenSeconds = 2.0f;
        [SerializeField] private Vector3 doorwayHalfSize = new Vector3(0.7f, 1.2f, 0.6f);

        private Quaternion _leafRest;
        private float _angle;
        private float _lastActivity = -100f;
        private Transform _head;

        public bool IsOpen => _angle > 30f;
        public float Angle => _angle;
        /// <summary>Total seconds this door has stood open (smoke leak into the stairs).</summary>
        public float OpenSeconds { get; private set; }
        public int TimesOpened { get; private set; }

        public void Configure(Transform leafPivot, Collider pushBar, Collider closedBlocker, Vector3 pushDir, float sign)
        {
            leaf = leafPivot; bar = pushBar; blocker = closedBlocker; pushDirection = pushDir.normalized; openSign = sign;
        }

        private void Awake()
        {
            if (leaf != null) _leafRest = leaf.localRotation;
        }

        private void Update()
        {
            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
            }
            bool pushed = BarPushed();
            bool someoneInDoorway = _head != null && InDoorway(_head.position);
            if (pushed || (someoneInDoorway && _angle > 5f)) _lastActivity = Time.time;
            bool wantOpen = pushed || Time.time - _lastActivity < holdOpenSeconds;
            float before = _angle;
            _angle = wantOpen
                ? Mathf.MoveTowards(_angle, openAngle, openAngle / openSeconds * Time.deltaTime)
                : Mathf.MoveTowards(_angle, 0f, openAngle / closeSeconds * Time.deltaTime);
            if (before <= 30f && _angle > 30f) TimesOpened++;
            if (IsOpen) OpenSeconds += Time.deltaTime;
            if (leaf != null) leaf.localRotation = _leafRest * Quaternion.AngleAxis(_angle * openSign, Vector3.up);
            if (blocker != null) blocker.enabled = _angle < 30f;
        }

        private bool BarPushed()
        {
            if (bar == null) return false;
            Bounds b = bar.bounds;
            Vector3 face = b.center - pushDirection * Vector3.Dot(b.extents, new Vector3(Mathf.Abs(pushDirection.x), Mathf.Abs(pushDirection.y), Mathf.Abs(pushDirection.z)));
            foreach (var i in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (!i.isActiveAndEnabled) continue;
                Vector3 h = i.transform.position;
                float depth = Vector3.Dot(h - face, pushDirection);
                Vector3 lateral = h - face - pushDirection * depth;
                Vector3 e = b.extents;
                bool overBar = Mathf.Abs(Vector3.Dot(lateral, Vector3.up)) <= e.y + 0.05f &&
                               Vector3.ProjectOnPlane(lateral, Vector3.up).magnitude <= Mathf.Max(e.x, e.z) + 0.05f;
                if (overBar && depth >= barTravel && depth <= 0.35f) return true;
            }
            return false;
        }

        private bool InDoorway(Vector3 p)
        {
            Vector3 local = transform.InverseTransformPoint(p);
            return Mathf.Abs(local.x) <= doorwayHalfSize.x && local.y >= -0.5f && local.y <= 2.6f && Mathf.Abs(local.z) <= doorwayHalfSize.z;
        }
    }
}
