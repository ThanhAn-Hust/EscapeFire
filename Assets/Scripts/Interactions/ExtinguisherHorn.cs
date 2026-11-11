using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// CO2 horn / hose nozzle (PASS "Aim", plan Phần B.1-5): the second hand holds the horn by its handle and points it
    /// at the base of the fire; the hose limits how far it reaches. Not held, the horn hangs in its clip on the bottle
    /// and the jet goes wherever the bottle points. A hose is drawn from the valve to the horn.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ExtinguisherHorn : MonoBehaviour
    {
        [SerializeField] private FireExtinguisher extinguisher;
        [SerializeField] private XRGrabInteractable bottle;
        [Tooltip("Hose root on the valve, child of the bottle.")]
        [SerializeField] private Transform hoseRoot;
        [SerializeField] private float hoseLength = 0.7f;
        [SerializeField] private float gripReach = 0.08f;
        [Tooltip("Hand-to-horn offset while held (horn sits just in front of the fist).")]
        [SerializeField] private Vector3 heldLocalOffset = new Vector3(0f, 0f, 0.06f);

        private Transform _restParent;
        private Vector3 _restLocalPos;
        private Quaternion _restLocalRot;
        private XRBaseInputInteractor _holder;
        private LineRenderer _hose;

        public bool IsHeld => _holder != null;

        public void Configure(FireExtinguisher ext, XRGrabInteractable bottleGrab, Transform root)
        {
            extinguisher = ext;
            bottle = bottleGrab;
            hoseRoot = root;
        }

        private void Start()
        {
            _restParent = transform.parent;
            _restLocalPos = transform.localPosition;
            _restLocalRot = transform.localRotation;
            _hose = GetComponent<LineRenderer>();
            _hose.positionCount = 10;
            _hose.useWorldSpace = true;
            if (_hose.sharedMaterial == null)
            {
                _hose.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.05f, 0.05f, 0.05f) };
                _hose.widthMultiplier = 0.018f;
            }
        }

        private void LateUpdate()
        {
            if (bottle == null) return;
            if (_holder == null)
            {
                var hand = SecondHand.FindGripping(bottle, transform.position, gripReach);
                if (hand != null) _holder = hand;
            }
            if (_holder != null && !_holder.selectInput.ReadIsPerformed()) _holder = null;

            if (_holder != null)
            {
                Transform h = _holder.transform;
                Vector3 target = h.TransformPoint(heldLocalOffset);
                Vector3 root = hoseRoot != null ? hoseRoot.position : bottle.transform.position;
                Vector3 fromRoot = target - root;
                if (fromRoot.magnitude > hoseLength) target = root + fromRoot.normalized * hoseLength; // hose is taut
                transform.SetPositionAndRotation(target, h.rotation);
            }
            else
            {
                transform.SetLocalPositionAndRotation(_restLocalPos, _restLocalRot);
            }
            DrawHose();
        }

        private void DrawHose()
        {
            if (_hose == null || hoseRoot == null) return;
            Vector3 a = hoseRoot.position, b = transform.position - transform.forward * 0.12f;
            Vector3 sag = Vector3.down * Mathf.Max(0.05f, hoseLength - Vector3.Distance(a, b)) * 0.6f;
            for (int i = 0; i < _hose.positionCount; i++)
            {
                float t = i / (float)(_hose.positionCount - 1);
                Vector3 p = Vector3.Lerp(a, b, t) + sag * (4f * t * (1f - t));
                _hose.SetPosition(i, p);
            }
        }
    }
}
