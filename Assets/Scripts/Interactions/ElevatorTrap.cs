using Unity.XR.CoreUtils;
using UnityEngine;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Scenario;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Elevator decoy (plan Phần B.1-14, rule R10 "không dùng thang máy khi cháy"). Pressing the call button costs
    /// points; in Guided mode the Mentor asks a Socratic question; in Exam mode stepping into the car is a failure
    /// (lift loses power and gets stuck between floors).
    /// </summary>
    public class ElevatorTrap : MonoBehaviour
    {
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private Vector3 slide = new Vector3(0f, 0f, 0.6f);
        [SerializeField] private Bounds car = new Bounds(Vector3.zero, Vector3.one);
        [SerializeField] private float doorDelay = 2f;

        private Vector3 _l0, _r0;
        private float _calledAt = -1f;
        private Transform _head;
        private bool _failed;
        private bool _learnerCalled;

        public bool Called => _calledAt >= 0f;
        public bool DoorsOpen { get; private set; }
        public static bool ElevatorUsedThisSession { get; private set; }

        public void Configure(Transform l, Transform r, Vector3 slideDir, Bounds carBounds)
        {
            leftDoor = l; rightDoor = r; slide = slideDir; car = carBounds;
        }

        private void Awake()
        {
            ElevatorUsedThisSession = false;
            if (leftDoor != null) _l0 = leftDoor.position;
            if (rightDoor != null) _r0 = rightDoor.position;
        }

        public Bounds Car => car;
        /// <summary>Someone else (a panicking NPC) called the lift: no penalty for the learner; the car takes longer.</summary>
        public void CallByNpc(float extraDelay)
        {
            if (Called) return;
            _calledAt = Time.time + extraDelay;
        }

        /// <summary>Call button pressed (fingertip poke).</summary>
        public void CallPressed()
        {
            if (_learnerCalled) return;
            _learnerCalled = true;
            _calledAt = Called ? Mathf.Min(_calledAt, Time.time) : Time.time;
            ElevatorUsedThisSession = true;
            if (ScoringManager.Instance != null) ScoringManager.Instance.DeductScore(20, "Gọi thang máy khi đang có cháy (-20đ)");
            if (GameSession.Mode == TrainingMode.Guided)
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(
                    "Khi cháy, thang máy có thể mất điện và kẹt, hoặc mở cửa ra đúng tầng đầy khói. Còn lối nào an toàn hơn?"));
        }

        private void Update()
        {
            if (!Called) return;
            bool open = Time.time - _calledAt >= doorDelay && Time.time - _calledAt < doorDelay + 8f;
            DoorsOpen = open;
            float k = open ? 1f : 0f;
            if (leftDoor != null) leftDoor.position = Vector3.MoveTowards(leftDoor.position, _l0 - slide * k, Time.deltaTime);
            if (rightDoor != null) rightDoor.position = Vector3.MoveTowards(rightDoor.position, _r0 + slide * k, Time.deltaTime);
            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
            }
            if (_failed || !open || _head == null || !car.Contains(_head.position)) return;
            if (GameSession.Mode == TrainingMode.Exam)
            {
                _failed = true;
                var c = LabProgressiveScenarioController.Instance;
                if (c != null) c.Fail("Mắc kẹt trong thang máy khi đang có cháy (không được dùng thang máy).");
            }
            else
            {
                _calledAt = Time.time - doorDelay - 8f; // doors close; Guided: just the lesson, no failure
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Dừng lại! Không vào thang máy khi có cháy — hãy đi cầu thang bộ theo biển EXIT."));
            }
        }
    }
}
