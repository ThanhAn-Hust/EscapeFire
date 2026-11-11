using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Core;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Safety pin (PASS "Pull", plan Phần B.1-5): one hand holds the bottle, the OTHER hand grips the ring and pulls
    /// it straight out along the pin axis. Pulling ≥ <see cref="pullDistance"/> breaks the seal and removes the pin.
    /// A jammed pin (AI anomaly) springs back the first time and needs a second firm pull.
    /// Without the bottle held in the other hand, the pin does not move (you cannot pull it with the bottle on the wall).
    /// </summary>
    public class ExtinguisherPin : MonoBehaviour
    {
        [SerializeField] private FireExtinguisher extinguisher;
        [SerializeField] private XRGrabInteractable bottle;
        [Tooltip("Pin axis in the bottle's local space (direction the pin slides out).")]
        [SerializeField] private Vector3 localAxis = Vector3.right;
        [SerializeField] private float pullDistance = 0.06f;
        [SerializeField] private float gripReach = 0.07f;

        private Vector3 _restLocalPos;
        private Quaternion _restLocalRot;
        private XRBaseInputInteractor _puller;
        private float _grabOffset;
        private bool _attemptSpent;
        private bool _out;
        private bool _hintShown;

        public bool IsOut => _out;
        public float Displacement { get; private set; }

        public void Configure(FireExtinguisher ext, XRGrabInteractable bottleGrab, Vector3 axisLocalToBottle)
        {
            extinguisher = ext;
            bottle = bottleGrab;
            localAxis = axisLocalToBottle;
        }

        private void Start()
        {
            _restLocalPos = transform.localPosition;
            _restLocalRot = transform.localRotation;
        }

        private void LateUpdate()
        {
            if (_out || extinguisher == null || bottle == null) return;
            if (extinguisher.IsPinPulled) { Remove(); return; }

            Vector3 axis = transform.parent.TransformDirection(localAxis).normalized;
            Vector3 rest = transform.parent.TransformPoint(_restLocalPos);

            if (_puller == null)
            {
                var hand = SecondHand.FindGripping(bottle, transform.position, gripReach);
                if (hand != null)
                {
                    if (!SecondHand.IsHeld(bottle))
                    {
                        if (!_hintShown && GameSession.Mode == TrainingMode.Guided)
                        {
                            _hintShown = true;
                            EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Nhấc bình xuống và giữ chắc thân bình bằng một tay, rồi mới rút chốt bằng tay kia."));
                        }
                        return;
                    }
                    _puller = hand;
                    _grabOffset = Vector3.Dot(hand.transform.position - rest, axis);
                    _attemptSpent = false;
                }
            }

            if (_puller != null)
            {
                if (!_puller.selectInput.ReadIsPerformed() || !SecondHand.IsHeld(bottle))
                {
                    _puller = null; // let go: a jammed pin springs back
                    Displacement = 0f;
                    transform.localPosition = _restLocalPos;
                    return;
                }
                Displacement = Mathf.Clamp(Vector3.Dot(_puller.transform.position - rest, axis) - _grabOffset, 0f, pullDistance + 0.01f);
                transform.SetPositionAndRotation(rest + axis * Displacement, transform.parent.rotation * _restLocalRot);
                if (Displacement >= pullDistance && !_attemptSpent)
                {
                    _attemptSpent = true;
                    extinguisher.PullSafetyPin();
                    if (extinguisher.IsPinPulled) Remove();
                }
            }
        }

        /// <summary>Pin and seal come off: it drops to the floor.</summary>
        private void Remove()
        {
            _out = true;
            transform.SetParent(null, true);
            var rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.mass = 0.02f;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
        }
    }
}
