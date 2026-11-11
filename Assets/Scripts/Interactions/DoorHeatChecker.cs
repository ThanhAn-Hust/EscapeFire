using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Analytics;
using EscapeFire.Scenario;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Checks door handle temperature before opening exit door.
    /// Prevents Backdraft explosions and awards procedure points for checking with back of hand.
    /// </summary>
    public class DoorHeatChecker : MonoBehaviour
    {
        [Header("Door Heat Settings")]
        [Tooltip("The lab exit leads to a safe hallway, so its handle is cool. Set true for a door with fire behind it.")]
        [SerializeField] private bool isDoorHot = false;
        [SerializeField] private float heatCheckDistance = 0.25f;

        [Header("Back-of-hand check (plan Phần B.1-8)")]
        [Tooltip("Max gap between the hand and the door / handle surface (m).")]
        [SerializeField] private float touchDistance = 0.06f;
        [Tooltip("Back of the hand must rest on the door this long (s).")]
        [SerializeField] private float holdSeconds = 1.0f;
        [Tooltip("Door / handle surface collider. Defaults to this object's collider.")]
        [SerializeField] private Collider surface;

        private float _backHeld, _palmHeld;
        private bool _palmHintShown, _burned;

        [Header("Visuals & Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hotWarningSound;

        public bool HasCheckedHeat { get; private set; } = false;
        public bool IsDoorHot => isDoorHot;

        public void CheckDoorHeatWithHand()
        {
            if (HasCheckedHeat) return;

            HasCheckedHeat = true;
            Debug.Log("[DoorHeatChecker] Player checked door handle heat with back of hand!");

            // Checking is the correct procedure whatever the result (NFPA 101), so it always earns the points.
            if (ScoringManager.Instance != null)
            {
                ScoringManager.Instance.AddScore(10, "Kiểm tra nhiệt độ tay nắm cửa bằng mu bàn tay (+10đ quy trình)");
            }
            if (isDoorHot)
            {
                Debug.LogWarning("[DoorHeatChecker] WARNING: Door handle is extremely HOT!");
                if (audioSource != null && hotWarningSound != null)
                {
                    audioSource.PlayOneShot(hotWarningSound);
                }
            }

            EventBus<DoorHeatCheckedEvent>.Publish(new DoorHeatCheckedEvent(isDoorHot));
        }

        /// <summary>Opens the exit door (after the heat check; an unchecked hot door costs points first).</summary>
        public void OpenDoor()
        {
            CanOpenDoorSafely();
            var door = GetComponentInParent<ExitDoor>();
            if (door == null) door = GetComponentInChildren<ExitDoor>();
            if (door != null) door.Open();
        }

        public bool CanOpenDoorSafely()
        {
            if (isDoorHot && !HasCheckedHeat)
            {
                Debug.LogWarning("[DoorHeatChecker] Unsafe door opening without heat check!");
                if (ScoringManager.Instance != null)
                {
                    ScoringManager.Instance.DeductScore(15, "Mở cửa vội vàng không kiểm tra nhiệt độ tay nắm (-15đ)");
                }
            }
            return true;
        }

        /// <summary>
        /// Palm direction of a controller hand: palms face each other when the controllers are held naturally
        /// (right palm towards -X of the right controller, left palm towards +X of the left one).
        /// </summary>
        public static Vector3 PalmNormal(Transform controller, bool isLeft) => (isLeft ? 1f : -1f) * controller.right;

        private void Update()
        {
            if (HasCheckedHeat && (!isDoorHot || _burned)) return;
            if (surface == null) surface = GetComponent<Collider>();
            if (surface == null) return;
            bool back = false, palm = false;
            foreach (var i in FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (!i.isActiveAndEnabled) continue;
                Transform hand = i.transform;
                Vector3 closest = surface.ClosestPoint(hand.position);
                Vector3 toDoor = closest - hand.position;
                if (toDoor.magnitude > touchDistance) continue;
                Vector3 dir = toDoor.sqrMagnitude > 1e-6f ? toDoor.normalized : -surface.transform.forward;
                bool isLeft = hand.parent != null && hand.parent.name.StartsWith("Left") || hand.name.StartsWith("Left");
                for (var t = hand; t != null && !isLeft; t = t.parent) if (t.name.StartsWith("Left")) isLeft = true;
                Vector3 palmN = PalmNormal(hand, isLeft);
                float d = Vector3.Dot(palmN, dir);
                if (d < -0.5f) back = true;      // back of the hand faces the door
                else if (d > 0.5f) palm = true;  // palm on the door
            }
            _backHeld = back ? _backHeld + Time.deltaTime : 0f;
            _palmHeld = palm ? _palmHeld + Time.deltaTime : 0f;
            if (!HasCheckedHeat && _backHeld >= holdSeconds) CheckDoorHeatWithHand();
            if (_palmHeld >= 0.5f)
            {
                if (isDoorHot && !_burned)
                {
                    _burned = true;
                    if (ScoringManager.Instance != null) ScoringManager.Instance.DeductScore(10, "Áp lòng bàn tay vào cửa nóng: bỏng tay (-10đ)");
                    EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Cửa nóng! Lòng bàn tay vừa bị bỏng. Vì sao nên kiểm tra bằng mu bàn tay?"));
                }
                else if (!_palmHintShown && GameSession.Mode == TrainingMode.Guided && !HasCheckedHeat)
                {
                    _palmHintShown = true;
                    EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Hãy áp MU bàn tay (không phải lòng bàn tay) lên cửa vài giây để cảm nhận nhiệt."));
                }
            }
        }
    }
}
