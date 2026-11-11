using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Lesson
{
    /// <summary>
    /// Station 5 (stay low): walk through a smoke-filled corridor keeping your head under the smoke layer the whole way.
    /// </summary>
    public class SmokeCorridorCheck : MonoBehaviour, IPracticeCheck
    {
        [SerializeField] private Vector3 entry;
        [SerializeField] private Vector3 exit;
        [SerializeField] private float halfWidth = 1.2f;
        [SerializeField] private float smokeHeight = 1.2f;

        private Transform _head;
        private bool _inside;
        private bool _clean;

        public void Configure(Vector3 entryPoint, Vector3 exitPoint, float width, float smokeY)
        {
            entry = entryPoint; exit = exitPoint; halfWidth = width * 0.5f; smokeHeight = smokeY;
        }

        public bool Done { get; private set; }
        public string Instruction => $"Đi hết hành lang khói: luôn giữ đầu thấp hơn lớp khói ({smokeHeight:F1} m).";
        public string Feedback { get; private set; } = "";
        public float SmokeHeight => smokeHeight;

        private void Update()
        {
            if (Done) return;
            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
                if (_head == null) return;
            }
            Vector3 axis = exit - entry;
            float len = axis.magnitude;
            Vector3 dir = axis / Mathf.Max(1e-4f, len);
            Vector3 rel = _head.position - entry;
            float along = Vector3.Dot(new Vector3(rel.x, 0f, rel.z), new Vector3(dir.x, 0f, dir.z).normalized);
            float side = Vector3.Cross(new Vector3(dir.x, 0f, dir.z).normalized, new Vector3(rel.x, 0f, rel.z)).y;
            bool inCorridor = along >= 0f && along <= len && Mathf.Abs(side) <= halfWidth;
            float headAboveFloor = _head.position.y - transform.position.y;
            if (inCorridor && !_inside)
            {
                _inside = true;
                _clean = along < 0.6f; // must enter from the entry end
            }
            if (_inside && inCorridor && headAboveFloor > smokeHeight)
            {
                _clean = false;
                Feedback = "Đầu bạn đã vào lớp khói! Ra lại đầu hành lang và đi tiếp thật thấp.";
            }
            if (_inside && !inCorridor)
            {
                if (_clean && along > len) Done = true;
                _inside = false;
            }
        }
    }
}
