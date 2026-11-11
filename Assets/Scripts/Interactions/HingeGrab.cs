using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Something that turns on a hinge when the hand grips it and moves: a panel door, a breaker lever, a door
    /// handle, a door leaf (plan Phần B.1-3, B.1-9). The angle follows the gripping hand around the hinge axis,
    /// limited to [minAngle, maxAngle]. Optional:
    /// - <see cref="latchFraction"/>: past this fraction of travel it latches at the end (breaker OFF, latch released);
    /// - <see cref="springBack"/>: released before latching, it returns to rest (spring-loaded lever / handle);
    /// - <see cref="requires"/>: another hinge that must be open first (lever behind a closed panel door).
    /// Driven by the controller's real grip through XRI, so it works the same on the headset and in tests.
    /// </summary>
    public class HingeGrab : MonoBehaviour
    {
        [SerializeField] private Vector3 localAxis = Vector3.up;
        [SerializeField] private float minAngle = 0f;
        [SerializeField] private float maxAngle = 90f;
        [Tooltip("Where the hand takes hold (lever tip, door edge, handle end).")]
        [SerializeField] private Transform grabPoint;
        [SerializeField] private float grabReach = 0.08f;
        [SerializeField] private bool springBack;
        [Range(0f, 1f)] [SerializeField] private float latchFraction;
        [SerializeField] private HingeGrab requires;
        [SerializeField] private float requiresAngle = 60f;
        [SerializeField] private UnityEvent onLatched = new UnityEvent();

        private Quaternion _rest;
        private XRBaseInputInteractor _holder;
        private Vector3 _grabRef;
        private float _angleAtGrab;

        public float Angle { get; private set; }
        public bool IsLatched { get; private set; }
        public bool IsHeld => _holder != null;
        public float Fraction => Mathf.Approximately(maxAngle, minAngle) ? 0f : Mathf.InverseLerp(minAngle, maxAngle, Angle);
        public UnityEvent OnLatched => onLatched;

        public void Configure(Vector3 axis, float min, float max, Transform grab, bool spring, float latch, HingeGrab requiredOpen = null, float requiredAngle = 60f)
        {
            localAxis = axis;
            minAngle = min;
            maxAngle = max;
            grabPoint = grab;
            springBack = spring;
            latchFraction = latch;
            requires = requiredOpen;
            requiresAngle = requiredAngle;
        }

        private void Awake() => _rest = transform.localRotation;

        private Vector3 AxisWorld => (transform.parent != null ? transform.parent.rotation * (_rest * localAxis) : _rest * localAxis).normalized;

        private Vector3 Project(Vector3 p)
        {
            Vector3 v = p - transform.position;
            return Vector3.ProjectOnPlane(v, AxisWorld);
        }

        private void LateUpdate()
        {
            if (IsLatched) return;
            if (_holder == null)
            {
                if (springBack && Mathf.Abs(Angle - minAngle) > 0.01f) SetAngle(Mathf.MoveTowards(Angle, minAngle, 360f * Time.deltaTime));
                if (requires != null && Mathf.Abs(requires.Angle) < requiresAngle) return; // blocked (e.g. panel door still closed)
                var hand = FindHand();
                if (hand == null) return;
                _holder = hand;
                _grabRef = Project(hand.transform.position);
                _angleAtGrab = Angle;
            }
            if (!_holder.selectInput.ReadIsPerformed())
            {
                _holder = null;
                return;
            }
            Vector3 now = Project(_holder.transform.position);
            if (now.sqrMagnitude < 1e-6f || _grabRef.sqrMagnitude < 1e-6f) return;
            float delta = Vector3.SignedAngle(_grabRef, now, AxisWorld);
            SetAngle(Mathf.Clamp(_angleAtGrab + delta, Mathf.Min(minAngle, maxAngle), Mathf.Max(minAngle, maxAngle)));
            if (latchFraction > 0f && Fraction >= latchFraction)
            {
                SetAngle(maxAngle);
                IsLatched = true;
                _holder = null;
                onLatched.Invoke();
            }
        }

        private XRBaseInputInteractor FindHand()
        {
            Vector3 p = grabPoint != null ? grabPoint.position : transform.position;
            XRBaseInputInteractor best = null;
            float bestSqr = grabReach * grabReach;
            foreach (var i in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (!i.isActiveAndEnabled || i.hasSelection) continue; // a hand already holding something cannot take hold
                if (!i.selectInput.ReadIsPerformed()) continue;
                float d = (i.transform.position - p).sqrMagnitude;
                if (d <= bestSqr) { bestSqr = d; best = i; }
            }
            return best;
        }

        /// <summary>Puts it at the end of travel without a hand (state changed elsewhere, e.g. power cut by script).</summary>
        public void ForceLatch()
        {
            if (IsLatched) return;
            SetAngle(maxAngle);
            IsLatched = true;
            _holder = null;
        }

        private void SetAngle(float a)
        {
            Angle = a;
            transform.localRotation = _rest * Quaternion.AngleAxis(a, localAxis);
        }
    }
}
