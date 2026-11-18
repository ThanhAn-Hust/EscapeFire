using UnityEngine;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Scenario;

namespace EscapeFire.NPC
{
    public enum NPCRole { MinhPanic, HoaFaint }

    /// <summary>
    /// NPC behaviour during the drill (NPC_TIMELINE_STORYBOARD):
    /// - Before the fire both NPCs stay put (working at their desks).
    /// - Minh: when the fire erupts he panics, knocks his chair over and runs along the floor to the exit, then leaves.
    /// - Both flinch the moment the server explodes (forearms over the head, knees bent) instead of standing still.
    /// - Hoa: overcome by smoke (conscious but weak, NOT unconscious: the unconscious victim is Nam, Rautek drag):
    ///   she coughs with a hand over her mouth, then sinks down and sits slumped on the floor. Helped up under the arm
    ///   she gets up slowly and walks bent low, slowly, only where the supporting hand leads. Let go and she sits down
    ///   again and waits. Outside the lab another person can take her over (<see cref="HandOverTo"/>: Minh leads her
    ///   down the stairs), so she is never just left behind.
    /// Legs/arms move with <see cref="ProceduralWalk"/> (no walk clips in the project).
    /// Movement is purely horizontal so NPCs never float or sink; the root pivot must be at the feet.
    /// </summary>
    public class NPCSimulator : MonoBehaviour
    {
        private static readonly int PANIC_RUN = Animator.StringToHash("PanicRun");
        private static readonly int FAINT = Animator.StringToHash("Faint");
        private static readonly int RESCUED = Animator.StringToHash("Rescued");

        [Header("NPC Settings")]
        [SerializeField] private NPCRole role = NPCRole.MinhPanic;
        [SerializeField] private float runSpeed = 3.5f;
        [SerializeField] private float walkSpeed = 1.2f;
        [SerializeField] private float reactionDelaySeconds = 2f;
        [SerializeField] private float arriveDistance = 0.6f;

        [Header("References")]
        [SerializeField] private Transform exitDoorTarget;
        [SerializeField] private GameObject chairToKnockOver;
        [SerializeField] private Animator animator;
        [Tooltip("Visual root rotated when fainting. Defaults to this transform.")]
        [SerializeField] private Transform visualRoot;

        [Tooltip("Who Hoa follows after the rescue. Defaults to the main camera (the learner's head).")]
        [SerializeField] private Transform escortTarget;
        [SerializeField] private float escortDistance = 1.1f;

        private enum State { Idle, Waiting, Moving, Fainted, StandingUp, Following, Gone }
        private ProceduralWalk _walk;
        private NpcBodyPose _pose;
        private Transform _handoverDoor;
        [SerializeField] private float weakWalkSpeed = 0.6f;

        private State _state = State.Idle;
        private float _timer;
        private float _speed;
        private Quaternion _standingRotation;
        private Vector3 _standingLocalPosition;

        public NPCRole Role => role;
        public bool IsRescued { get; private set; }
        public bool IsFainted { get; private set; }

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            _standingRotation = visualRoot.localRotation;
            _standingLocalPosition = visualRoot.localPosition;
            _walk = GetComponent<ProceduralWalk>();
            if (_walk == null) _walk = gameObject.AddComponent<ProceduralWalk>();
            _pose = GetComponent<NpcBodyPose>();
            if (_pose == null) _pose = gameObject.AddComponent<NpcBodyPose>();
        }

        private void OnEnable() => EventBus<PhaseChangedEvent>.Subscribe(OnPhaseChanged);

        private void OnDisable() => EventBus<PhaseChangedEvent>.Unsubscribe(OnPhaseChanged);

        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.Phase == ScenarioPhase.Phase2_ServerExplosionFire && _state == State.Idle)
            {
                _state = State.Waiting;
                _timer = reactionDelaySeconds;
                // startled by the explosion: forearms over the head, crouch; Hoa then starts coughing in the smoke
                _pose.Set(BodyPose.CoverHeadCrouch, 0.25f);
                _coughAt = role == NPCRole.HoaFaint ? Time.time + 1.0f : -1f;
            }
        }

        public void TriggerEmergencyReaction()
        {
            if (role == NPCRole.MinhPanic)
            {
                if (chairToKnockOver != null) chairToKnockOver.transform.Rotate(Vector3.right, 75f, Space.Self);
                if (animator != null) animator.SetTrigger(PANIC_RUN);
                _pose.Set(BodyPose.None, 0.3f);
                _walk.Hunch = 0.45f; // runs bent low under the smoke
                StartMoving(runSpeed);
                Debug.Log("[NPC] Minh panicked and ran towards the exit door!");
            }
            else
            {
                IsFainted = true;
                _state = State.Fainted;
                if (animator != null) animator.SetTrigger(FAINT);
                _pose.Set(BodyPose.SitSlumped, 1.6f); // sinks down and sits slumped, head down
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Hoa bị choáng vì khói, ngồi gục xuống sàn: bạn ấy còn tỉnh nhưng không tự đi được."));
                Debug.Log("[NPC] Hoa is overcome by smoke and sits slumped on the floor.");
            }
        }

        public void RescueNPC()
        {
            if (role != NPCRole.HoaFaint || !IsFainted || IsRescued) return;

            IsRescued = true;
            EventBus<NpcRescuedEvent>.Publish(new NpcRescuedEvent(role));
            if (ScoringManager.Instance != null)
            {
                ScoringManager.Instance.AddScore(15, "Cứu hộ thành công bạn học Hoa khỏi khói độc");
            }
            visualRoot.SetLocalPositionAndRotation(_standingLocalPosition, _standingRotation);
            if (animator != null) animator.SetTrigger(RESCUED);
            if (escortTarget == null && Camera.main != null) escortTarget = Camera.main.transform;
            if (escortTarget != null)
            {
                StandUp();
                Debug.Log("[NPC] Hoa is helped up and is led out, supported.");
            }
            else
            {
                StartMoving(walkSpeed);
                Debug.Log("[NPC] Hoa was rescued and is walking to the exit.");
            }
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Waiting:
                    _timer -= Time.deltaTime;
                    if (_coughAt > 0f && Time.time >= _coughAt) { _coughAt = -1f; _pose.Set(BodyPose.CoughHunch, 0.5f); }
                    if (_timer <= 0f) TriggerEmergencyReaction();
                    break;

                case State.StandingUp:
                    if (!_pose.Blending) { _state = State.Following; _walk.Hunch = 0.85f; }
                    break;

                case State.Moving:
                    MoveTowardsExit();
                    break;

                case State.Following:
                    FollowEscort();
                    break;
            }
        }

        private void StartMoving(float speed)
        {
            _speed = speed;
            _state = exitDoorTarget != null ? State.Moving : State.Idle;
            if (exitDoorTarget == null) Debug.LogWarning($"[NPC] {name}: exitDoorTarget not set, cannot leave the room.");
        }

        /// <summary>Who to follow after being rescued (e.g. the learner's head or a demo camera).</summary>
        public void SetEscortTarget(Transform target) => escortTarget = target;

        /// <summary>Supporting hand (EscortGrip): null = let go, she stops where she is.</summary>
        public void SetEscortTarget(Transform target, float distance)
        {
            escortTarget = target;
            escortDistance = distance;
            _heldByHand = true;
        }

        private bool _heldByHand;
        private EscapeFire.Interactions.ExitDoor _exitDoor;

        private float _coughAt = -1f;
        private bool _seated;

        private void StandUp()
        {
            _seated = false;
            _state = State.StandingUp;
            _pose.Set(BodyPose.None, 1.3f); // gets up slowly
        }

        /// <summary>
        /// Someone else (Minh) takes over: Hoa gets up and goes with <paramref name="helper"/> to
        /// <paramref name="stairsDoor"/>, then down the stairs (leaves the floor).
        /// </summary>
        public void HandOverTo(Transform helper, Transform stairsDoor)
        {
            if (role != NPCRole.HoaFaint || !IsRescued || _state == State.Gone) return;
            escortTarget = helper;
            escortDistance = 0.7f;
            _heldByHand = false;
            _handoverDoor = stairsDoor;
            StandUp();
        }

        /// <summary>True while Hoa, already helped, sits waiting for someone to lead her on.</summary>
        public bool WaitingForHelp => role == NPCRole.HoaFaint && IsRescued && _seated && gameObject.activeInHierarchy;

        private void FollowEscort()
        {
            if (escortTarget == null)
            {
                if (!_heldByHand) { StartMoving(walkSpeed); return; }
                // let go: too weak to go on alone, she sits down again and waits for someone to lead her
                if (!_seated) { _seated = true; _pose.Set(BodyPose.SitSlumped, 1.0f); }
                return;
            }
            if (_seated) { StandUp(); return; } // taken by the arm again
            if (_handoverDoor != null && HorizontalDistance(transform.position, _handoverDoor.position) < 1.3f) // right behind the helper at the door
            {
                _state = State.Gone;
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Minh đang dìu Hoa xuống thang bộ B."));
                gameObject.SetActive(false);
                return;
            }
            // Led by a non-hand target (logic tests): walk out on her own once at the open exit.
            if (!_heldByHand && _handoverDoor == null)
            {
                if (_exitDoor == null) _exitDoor = FindFirstObjectByType<EscapeFire.Interactions.ExitDoor>();
                bool doorOpen = _exitDoor == null || _exitDoor.IsOpen;
                if (doorOpen && exitDoorTarget != null && HorizontalDistance(escortTarget.position, exitDoorTarget.position) < 1.5f)
                {
                    StartMoving(walkSpeed);
                    return;
                }
            }
            Vector3 target = escortTarget.position;
            target.y = transform.position.y;
            Vector3 toTarget = target - transform.position;
            float dist = toTarget.magnitude;
            if (dist > escortDistance)
            {
                // weak: slow, bent low, never faster than a supported shuffle
                float speed = _heldByHand || _handoverDoor != null ? weakWalkSpeed : Mathf.Lerp(walkSpeed, walkSpeed * 1.6f, Mathf.Clamp01((dist - escortDistance) / 2f));
                transform.position = Vector3.MoveTowards(transform.position, target - toTarget / dist * escortDistance, speed * Time.deltaTime);
                SnapToGround();
            }
            if (dist > 0.05f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget / dist, Vector3.up), 8f * Time.deltaTime);
        }

        /// <summary>Keeps the feet on the floor / stairs below (corridor, landings, ramps) instead of walking on air.</summary>
        private void SnapToGround()
        {
            Vector3 p = transform.position;
            float best = float.NegativeInfinity;
            foreach (var hit in Physics.RaycastAll(p + Vector3.up * 1.2f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.point.y > best) best = hit.point.y;
            }
            if (!float.IsNegativeInfinity(best)) transform.position = new Vector3(p.x, best, p.z);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void MoveTowardsExit()
        {
            Vector3 target = exitDoorTarget.position;
            target.y = transform.position.y; // stay on the floor
            Vector3 toTarget = target - transform.position;
            if (toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
            {
                _state = State.Gone;
                // V5.2: say where she went (she reappears with the group at the assembly point)
                if (role == NPCRole.HoaFaint && IsRescued)
                    EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Hoa đã ra khỏi phòng và tự đi tiếp xuống cầu thang bộ cùng mọi người."));
                gameObject.SetActive(false); // left the room through the door
                return;
            }
            transform.SetPositionAndRotation(
                Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime),
                Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget.normalized, Vector3.up), 10f * Time.deltaTime));
        }

    }
}
