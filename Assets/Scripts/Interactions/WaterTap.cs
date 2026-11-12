using System.Collections.Generic;
using UnityEngine;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Sink tap (plan Phần B.1-7): grip the wing handle and turn it a quarter turn (≥ 80°) to run water. The stream is a
    /// vertical column under the spout; anything held in it gets wet (<see cref="WetTowel"/>). Turning is a real
    /// hand rotation around the tap axis via <see cref="HingeGrab"/>, the same on the headset and in tests.
    /// </summary>
    public class WaterTap : MonoBehaviour
    {
        [SerializeField] private HingeGrab handle;
        [SerializeField] private Transform spout;           // stream starts here and falls straight down
        [SerializeField] private float streamLength = 0.32f;
        [SerializeField] private float streamRadius = 0.07f;
        [SerializeField] private float openAngle = 80f;
        [SerializeField] private Renderer streamVisual;

        private static readonly List<WaterTap> _all = new List<WaterTap>();
        public static IReadOnlyList<WaterTap> All => _all;

        public bool IsRunning => handle != null && Mathf.Abs(handle.Angle) >= openAngle;
        public HingeGrab Handle => handle;
        public Vector3 SpoutPosition => spout != null ? spout.position : transform.position;

        public void Configure(HingeGrab h, Transform spoutPoint, Renderer stream)
        {
            handle = h; spout = spoutPoint; streamVisual = stream;
        }

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>True if the point is inside the running water column.</summary>
        public bool InStream(Vector3 p, float extra = 0f)
        {
            if (!IsRunning) return false;
            Vector3 top = SpoutPosition;
            float down = top.y - p.y;
            if (down < -0.03f || down > streamLength) return false;
            Vector2 d = new Vector2(p.x - top.x, p.z - top.z);
            return d.magnitude <= streamRadius + extra;
        }

        private void Update()
        {
            if (streamVisual != null) streamVisual.enabled = IsRunning;
        }
    }
}
