using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeFire.Player
{
    /// <summary>
    /// Shows a human hand instead of the controller model (plan Phần B.0-7/B.2). Fingers curl with the
    /// controller: Grip closes middle/ring/little + thumb, Trigger curls the index finger, so a grab looks like a
    /// grab and a squeeze looks like a squeeze. The visible hand also stops at walls instead of passing through.
    /// </summary>
    public class ControllerHand : MonoBehaviour
    {
        [SerializeField] private GameObject handModelPrefab;
        [SerializeField] private bool isLeft;
        [SerializeField] private InputActionProperty gripValue;
        [SerializeField] private InputActionProperty triggerValue;
        [Tooltip("Model offset so the palm sits around the controller grip.")]
        [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.02f, -0.06f);
        [SerializeField] private Vector3 localEuler = new Vector3(0f, 0f, 0f);
        [SerializeField] private LayerMask blockingLayers = 1; // Default (walls, furniture)
        [SerializeField] private float maxCurlDegrees = 75f;
        [Tooltip("Flexion direction around the joint X axis (OpenXR hand joints).")]
        [SerializeField] private float curlSign = 1f;

        private Transform _visual;
        private Transform _head;
        private readonly List<(Transform bone, Quaternion rest, bool index, bool thumb)> _fingers = new List<(Transform, Quaternion, bool, bool)>();

        public float Grip { get; private set; }
        public float Trigger { get; private set; }

        public void Configure(GameObject prefab, bool left, InputActionProperty grip, InputActionProperty trigger)
        {
            handModelPrefab = prefab;
            isLeft = left;
            gripValue = grip;
            triggerValue = trigger;
        }

        private void Start()
        {
            if (Camera.main != null) _head = Camera.main.transform;
            // Hide the stock controller model (the hand replaces it); keep interactors and their visuals for rays.
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("controller") || n.Contains("thumbstick") || n.Contains("trigger") || n.Contains("button") || n.Contains("grip")
                    || n.Contains("bumper") || n.Contains("touchpad") || n.Contains("pinch_pointer"))
                    r.enabled = false;
            }
            if (handModelPrefab == null) return;
            _visual = Instantiate(handModelPrefab, transform).transform;
            _visual.name = isLeft ? "EF_LeftHand" : "EF_RightHand";
            _visual.SetLocalPositionAndRotation(localOffset, Quaternion.Euler(localEuler));
            foreach (var t in _visual.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                bool finger = n.Contains("Proximal") || n.Contains("Intermediate") || n.Contains("Distal");
                if (!finger) continue;
                _fingers.Add((t, t.localRotation, n.Contains("Index"), n.Contains("Thumb")));
            }
            gripValue.action?.Enable();
            triggerValue.action?.Enable();
        }

        private void LateUpdate()
        {
            Grip = gripValue.action != null ? gripValue.action.ReadValue<float>() : 0f;
            Trigger = triggerValue.action != null ? triggerValue.action.ReadValue<float>() : 0f;
            if (_visual == null) return;
            foreach (var (bone, rest, index, thumb) in _fingers)
            {
                float k = index ? Trigger : thumb ? Mathf.Max(Grip, Trigger) * 0.6f : Grip;
                bone.localRotation = rest * Quaternion.Euler(curlSign * k * maxCurlDegrees, 0f, 0f);
            }

            // Visible hand stops at solid surfaces (the tracked controller keeps its real pose for interaction).
            Vector3 target = transform.TransformPoint(localOffset);
            Vector3 from = _head != null ? _head.position : transform.position;
            Vector3 dir = target - from;
            if (dir.sqrMagnitude > 1e-4f && Physics.SphereCast(from, 0.03f, dir.normalized, out var hit, dir.magnitude, blockingLayers, QueryTriggerInteraction.Ignore)
                && hit.rigidbody == null)
                _visual.position = hit.point + hit.normal * 0.035f;
            else
                _visual.localPosition = localOffset;
        }
    }
}
