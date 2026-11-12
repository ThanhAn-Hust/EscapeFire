using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using EscapeFire.Core;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Wet towel over nose and mouth (plan Phần B.1-7): pick it up (grab), hold it under a running tap for ~2 s until
    /// it is wet (darker colour), then hold it against the face (≤ 10 cm from the mouth, below the eyes). While it is
    /// held to the face, smoke takes oxygen much more slowly (dry: a little slower). The hand holding it is busy:
    /// XRI lets one hand hold one thing, so the learner must choose between the towel and the extinguisher.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class WetTowel : MonoBehaviour
    {
        [SerializeField] private float secondsToSoak = 2f;
        [SerializeField] private float faceDistance = 0.10f;
        [SerializeField] private float wetFactor = 0.3f;   // oxygen loss multiplier with a wet towel on the face
        [SerializeField] private float dryFactor = 0.75f;
        [SerializeField] private Color dryColor = new Color(0.55f, 0.78f, 0.95f);
        [SerializeField] private Color wetColor = new Color(0.18f, 0.36f, 0.55f);

        private static readonly List<WetTowel> _all = new List<WetTowel>();
        private XRGrabInteractable _grab;
        private Transform _head;
        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private bool _wetAnnounced;

        /// <summary>0 = dry, 1 = soaked.</summary>
        public float Wetness { get; private set; }
        public bool IsWet => Wetness >= 1f;
        public bool IsHeld => _grab != null && _grab.isSelected;
        public bool OnFace { get; private set; }
        public float SecondsOnFace { get; private set; }

        /// <summary>Oxygen loss multiplier from towels held to the face this frame (1 = no protection).</summary>
        public static float BreathingFactor()
        {
            float f = 1f;
            foreach (var t in _all)
                if (t.OnFace) f = Mathf.Min(f, t.IsWet ? t.wetFactor : t.dryFactor);
            return f;
        }

        public static void ResetAll() { foreach (var t in _all) { t.Wetness = 0f; t.SecondsOnFace = 0f; } }

        /// <summary>Mouth position: below the eyes, a little in front (where a towel covers nose and mouth).</summary>
        public static Vector3 Mouth(Transform head) => head.position - head.up * 0.09f + head.forward * 0.05f;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _renderer = GetComponentInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        private void Update()
        {
            if (Wetness < 1f)
            {
                foreach (var tap in WaterTap.All)
                {
                    if (!tap.InStream(transform.position, 0.06f)) continue;
                    Wetness = Mathf.Min(1f, Wetness + Time.deltaTime / Mathf.Max(0.1f, secondsToSoak));
                    break;
                }
                if (Wetness >= 1f && !_wetAnnounced)
                {
                    _wetAnnounced = true;
                    EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Khăn đã ướt: che kín mũi miệng và giữ bằng một tay khi đi qua khói."));
                }
            }

            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
            }
            OnFace = IsHeld && _head != null && Vector3.Distance(transform.position, Mouth(_head)) <= faceDistance;
            if (OnFace) SecondsOnFace += Time.deltaTime;

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", Color.Lerp(dryColor, wetColor, Wetness));
                _renderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
