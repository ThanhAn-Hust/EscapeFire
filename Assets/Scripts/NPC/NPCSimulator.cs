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
    /// - Hoa: faints from smoke a few seconds into the fire (lies on the floor) — optional rescue objective.
    ///   When rescued (select / poke her) she gets up, bent low, and follows the rescuer out (escort), instead of
    ///   leaving on her own. Without an escort target she walks to the exit by herself.
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

        private enum State { Idle, Waiting, Moving, Fainted, Following, Gone }
        private ProceduralWalk _walk;

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
        }

        private void OnEnable() => EventBus<PhaseChangedEvent>.Subscribe(OnPhaseChanged);

        private void OnDisable() => EventBus<PhaseChangedEvent>.Unsubscribe(OnPhaseChanged);

        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.Phase == ScenarioPhase.Phase2_ServerExplosionFire && _state == State.Idle)
            {
                _state = State.Waiting;
                _timer = reactionDelaySeconds;
            }
        }

        public void TriggerEmergencyReaction()
        {
            if (role == NPCRole.MinhPanic)
            {
                if (chairToKnockOver != null) chairToKnockOver.transform.Rotate(Vector3.right, 75f, Space.Self);
                if (animator != null) animator.SetTrigger(PANIC_RUN);
                StartMoving(runSpeed);
                Debug.Log("[NPC] Minh panicked and ran towards the exit door!");
            }
            else
            {
                IsFainted = true;
                _state = State.Fainted;
                if (animator != null) animator.SetTrigger(FAINT);
                else LieDown();
                Debug.Log("[NPC] Hoa fainted from smoke inhalation!");
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
                _state = State.Following;
                _walk.Hunch = 0.6f; // stays low under the smoke while being led out
                Debug.Log("[NPC] Hoa was rescued and follows the learner out.");
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
                    if (_timer <= 0f) TriggerEmergencyReaction();
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

        private void FollowEscort()
        {
            if (escortTarget == null)
            {
                if (_heldByHand) return; // let go: she waits, bent low, for the rescuer to take her arm again
                StartMoving(walkSpeed);
                return;
            }
            // Once the rescuer is through the exit, walk out on her own the last metres.
            if (_exitDoor == null) _exitDoor = FindFirstObjectByType<EscapeFire.Interactions.ExitDoor>();
            bool doorOpen = _exitDoor == null || _exitDoor.IsOpen;
            if (doorOpen && exitDoorTarget != null && HorizontalDistance(escortTarget.position, exitDoorTarget.position) < 1.5f)
            {
                StartMoving(walkSpeed);
                return;
            }
            Vector3 target = escortTarget.position;
            target.y = transform.position.y;
            Vector3 toTarget = target - transform.position;
            float dist = toTarget.magnitude;
            if (dist > escortDistance)
            {
                float speed = Mathf.Lerp(walkSpeed, walkSpeed * 1.6f, Mathf.Clamp01((dist - escortDistance) / 2f));
                transform.position = Vector3.MoveTowards(transform.position, target - toTarget / dist * escortDistance, speed * Time.deltaTime);
            }
            if (dist > 0.05f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget / dist, Vector3.up), 8f * Time.deltaTime);
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
                gameObject.SetActive(false); // left the room through the door
                return;
            }
            transform.SetPositionAndRotation(
                Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime),
                Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget.normalized, Vector3.up), 10f * Time.deltaTime));
        }

        /// <summary>Procedural faint (no animation clip needed): tip the body over onto the floor around the feet.</summary>
        private void LieDown()
        {
            visualRoot.SetLocalPositionAndRotation(
                visualRoot.localPosition + new Vector3(0f, 0.15f, 0f),
                _standingRotation * Quaternion.Euler(-90f, 0f, 0f));
        }
    }
}
