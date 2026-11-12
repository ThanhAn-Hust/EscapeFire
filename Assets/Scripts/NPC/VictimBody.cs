using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Core;
using EscapeFire.Interactions;

namespace EscapeFire.NPC
{
    /// <summary>
    /// An unconscious victim moved with the Rautek drag (plan Phần B.1-11): get down at the head end, slide BOTH hands
    /// under the armpits and grip; the upper body comes up only as high as the hands lift it, the heels drag on the
    /// floor, and the body trails behind the rescuer like a trailer as they walk backwards. One hand is not enough
    /// (too heavy); letting go lowers the body; pulling away too fast loses the grip. Lifting the head high (carrying
    /// upright in smoke) gets a reminder. Safe once heels and chest are inside the protected stairwell.
    /// Rig: this transform = heels on the floor, yaw = heels → head; <see cref="tiltPivot"/> rotates about its local X.
    /// </summary>
    public class VictimBody : MonoBehaviour
    {
        [SerializeField] private Transform tiltPivot;
        [SerializeField] private Transform chest;            // child of tiltPivot at the armpits
        [SerializeField] private float armpitLength = 1.38f;  // heels → armpits along the body
        [SerializeField] private float headLength = 1.62f;
        [SerializeField] private float lyingLift = 0.12f;     // body half-thickness above the floor
        [SerializeField] private float reach = 0.2f;
        [SerializeField] private float maxRescuerHeadHeight = 1.4f;
        [SerializeField] private float maxTilt = 55f;
        [SerializeField] private float maxDragSpeed = 0.8f;
        [SerializeField] private float loseGripDistance = 0.5f;
        [SerializeField] private Bounds safeZone = new Bounds(Vector3.zero, Vector3.zero);
        [SerializeField] private string victimName = "Nam";

        private static readonly List<VictimBody> _all = new List<VictimBody>();
        public static IReadOnlyList<VictimBody> All => _all;

        private readonly List<NearFarInteractor> _hands = new List<NearFarInteractor>();
        private XRBaseInputInteractor _a, _b;
        private Transform _head;
        private float _tilt;
        private float _oneHandSince = -1f;
        private bool _oneHandHint, _headHint, _tooFastHint;

        public bool IsDragged => _a != null;
        public bool IsSafe { get; private set; }
        public float DragDistance { get; private set; }
        public float MaxHeadHeight { get; private set; }
        public float Tilt => _tilt;
        public Vector3 Heels => transform.position;
        public Vector3 ChestPoint => tiltPivot.TransformPoint(new Vector3(0f, lyingLift, armpitLength));
        public Vector3 HeadPoint => tiltPivot.TransformPoint(new Vector3(0f, lyingLift, headLength));
        /// <summary>Where a rescuer's hands go: under the left and right armpit.</summary>
        public Vector3 Armpit(bool left) => tiltPivot.TransformPoint(new Vector3(left ? 0.2f : -0.2f, lyingLift, armpitLength));
        public Bounds SafeZone => safeZone;

        public void Configure(Transform pivot, Transform chestMarker, Bounds safe, string displayName)
        {
            tiltPivot = pivot; chest = chestMarker; safeZone = safe; victimName = displayName;
        }

        private void OnEnable()
        {
            _all.Add(this);
            PushBarDoor.DoorwayOccupants.Add(transform);
            if (chest != null) PushBarDoor.DoorwayOccupants.Add(chest);
        }

        private void OnDisable()
        {
            _all.Remove(this);
            PushBarDoor.DoorwayOccupants.Remove(transform);
            if (chest != null) PushBarDoor.DoorwayOccupants.Remove(chest);
        }

        private void Update()
        {
            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
            }
            _hands.Clear();
            foreach (var i in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
                if (i.isActiveAndEnabled) _hands.Add(i);

            if (IsDragged) Drag();
            else TryTakeHold();

            if (!IsDragged) _tilt = Mathf.MoveTowards(_tilt, 0f, 60f * Time.deltaTime);
            tiltPivot.localRotation = Quaternion.Euler(-_tilt, 0f, 0f);

            CheckSafe();
        }

        private void TryTakeHold()
        {
            XRBaseInputInteractor first = null, second = null;
            foreach (var h in _hands)
            {
                if (h.hasSelection || !h.selectInput.ReadIsPerformed()) continue;
                Vector3 p = h.transform.position;
                if (Vector3.Distance(p, Armpit(true)) > reach && Vector3.Distance(p, Armpit(false)) > reach) continue;
                if (first == null) first = h; else if (second == null) second = h;
            }
            if (first == null) { _oneHandSince = -1f; return; }
            bool low = _head == null || _head.position.y - transform.position.y <= maxRescuerHeadHeight;
            if (second == null || !low)
            {
                if (_oneHandSince < 0f) _oneHandSince = Time.time;
                if (!_oneHandHint && Time.time - _oneHandSince > 0.5f)
                {
                    _oneHandHint = true;
                    Say(!low
                        ? $"Ngồi thấp xuống phía đầu {victimName} rồi mới luồn tay dưới nách."
                        : $"{victimName} bất tỉnh và rất nặng: luồn CẢ HAI tay dưới nách, nắm cẳng tay rồi kéo lùi (Rautek).");
                }
                return;
            }
            _a = first; _b = second;
            _oneHandSince = -1f;
            Say($"Đã đỡ {victimName} (Rautek). Kéo lùi chậm, giữ đầu bạn ấy thấp, đi vào buồng thang bộ.");
        }

        private void Drag()
        {
            if (!_a.selectInput.ReadIsPerformed() || !_b.selectInput.ReadIsPerformed())
            {
                _a = _b = null; // let go: the body is lowered where it is
                return;
            }
            Vector3 m = (_a.transform.position + _b.transform.position) * 0.5f;
            float floor = transform.position.y;
            _tilt = Mathf.MoveTowards(_tilt, Mathf.Clamp(Mathf.Asin(Mathf.Clamp01((m.y - floor - lyingLift) / armpitLength)) * Mathf.Rad2Deg, 0f, maxTilt), 90f * Time.deltaTime);

            // trailer: the heels follow the hands at the body's horizontal length
            Vector3 heels = transform.position;
            Vector3 d = m - heels; d.y = 0f;
            float reachH = armpitLength * Mathf.Cos(_tilt * Mathf.Deg2Rad);
            float gap = d.magnitude - reachH;
            if (gap > loseGripDistance)
            {
                _a = _b = null;
                if (!_tooFastHint) { _tooFastHint = true; Say($"Tuột tay! Lùi chậm lại và giữ chặt {victimName}."); }
                return;
            }
            if (d.sqrMagnitude > 1e-4f)
            {
                Vector3 dir = d.normalized;
                if (gap > 0f)
                {
                    Vector3 step = dir * Mathf.Min(gap, maxDragSpeed * Time.deltaTime);
                    transform.position = heels + step;
                    DragDistance += step.magnitude;
                }
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
            SnapToGround();

            float headH = HeadPoint.y - transform.position.y;
            MaxHeadHeight = Mathf.Max(MaxHeadHeight, headH);
            if (headH > 1.25f && !_headHint)
            {
                _headHint = true;
                Say($"Giữ đầu {victimName} thấp — khói và khí nóng ở trên cao. Kéo lê sát sàn, đừng bế đứng.");
            }
        }

        private void SnapToGround()
        {
            Vector3 p = transform.position;
            float best = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(p + Vector3.up * 0.6f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<XROrigin>() != null) continue;
                if (hit.point.y > best) best = hit.point.y;
            }
            if (!float.IsNegativeInfinity(best)) transform.position = new Vector3(p.x, best, p.z);
        }

        private void CheckSafe()
        {
            if (IsSafe || safeZone.size == Vector3.zero) return;
            if (!safeZone.Contains(Heels + Vector3.up * 0.1f) || !safeZone.Contains(new Vector3(ChestPoint.x, Heels.y + 0.1f, ChestPoint.z))) return;
            IsSafe = true;
            EventBus<VictimSafeEvent>.Publish(new VictimSafeEvent());
            Say($"{victimName} đã ở trong buồng thang bộ (khu vực được bảo vệ). Nhớ báo vị trí cho đội trưởng ở điểm tập kết.");
        }

        private static void Say(string m) => EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(m));
    }
}
