using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Core;
using EscapeFire.Interactions;
using EscapeFire.Scenario;

namespace EscapeFire.NPC
{
    /// <summary>
    /// Minh in panic at the lift (plan V3.5, B.1-14): when the learner comes out into the corridor he is jabbing the
    /// lift call button. The learner has to reach him and take him by the arm / shoulder (real grip near his
    /// shoulder) before the doors open: then he follows the advice and goes down the stairs. Otherwise he steps into
    /// the car and is trapped when it stalls — he is then missing at the roll call.
    /// </summary>
    public class PanicRunner : MonoBehaviour
    {
        [SerializeField] private ElevatorTrap elevator;
        [SerializeField] private Transform stairsDoor;       // where he goes when stopped
        [SerializeField] private Bounds triggerSpace;         // learner's head entering this wakes the scene up
        [SerializeField] private float carArrivesAfter = 12f; // lift doors open 10 s after he starts pressing: a fair chance even while helping someone
        [SerializeField] private float reach = 0.3f;
        [SerializeField] private float walkSpeed = 1.3f;
        [SerializeField] private string displayName = "Minh";

        private enum State { Waiting, AtButton, Stopped, EnteringCar, Trapped, Gone }
        private State _state = State.Waiting;
        private Transform _head;
        private Transform _shoulder;
        private ProceduralWalk _walk;

        public bool Active => _state != State.Waiting;
        public bool Stopped => _state == State.Stopped || _state == State.Gone;
        public bool Trapped => _state == State.Trapped;
        public bool InBuilding => _state != State.Gone;
        public Vector3 ShoulderPoint => _shoulder != null ? _shoulder.position : transform.position + Vector3.up * 1.35f;

        private bool _armed;
        private NpcBodyPose _pose;
        private NPCSimulator _hoa;
        private bool _withHoa;

        /// <summary>V4 NPC_BEHAVIOUR piece: Minh rushes to the lift now, wherever the learner is.</summary>
        public void Arm() => _armed = true;

        public void Configure(ElevatorTrap lift, Transform stairs, Bounds trigger)
        {
            elevator = lift; stairsDoor = stairs; triggerSpace = trigger;
        }

        private void Awake()
        {
            foreach (var t in GetComponentsInChildren<Transform>()) // the shown model
                if (t.name == "Shoulder_R") { _shoulder = t; break; }
            _walk = GetComponentInChildren<ProceduralWalk>();
            _pose = GetComponent<NpcBodyPose>();
            if (_pose == null) _pose = gameObject.AddComponent<NpcBodyPose>();
            SetVisible(false);
        }

        private void SetVisible(bool on)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = on;
        }

        private void Update()
        {
            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
                if (_head == null) return;
            }
            var c = LabProgressiveScenarioController.Instance;
            bool emergency = c == null || (c.CurrentPhase >= ScenarioPhase.Phase2_ServerExplosionFire && c.CurrentPhase < ScenarioPhase.Phase4_CompletedWin);

            switch (_state)
            {
                case State.Waiting:
                    if (!emergency || (!_armed && !triggerSpace.Contains(_head.position))) return;
                    _state = State.AtButton;
                    SetVisible(true);
                    _pose.Set(BodyPose.PressButton, 0.3f); // jabbing the call button
                    if (elevator != null) elevator.CallByNpc(carArrivesAfter - 2f);
                    Say($"[{displayName}] Thang máy! Thang máy nhanh hơn! Sao nó chưa lên?!");
                    if (GameSession.Mode == TrainingMode.Guided)
                        Say($"{displayName} đang định đi thang máy! Lại gần, nắm tay/vai {displayName} để ngăn lại.");
                    break;

                case State.AtButton:
                    _pose.Jab = Mathf.PingPong(Time.time * 3f, 1f);
                    if (Gripped())
                    {
                        Stop();
                        Say($"[{displayName}] ...Ừ, cháy thì không đi thang máy. Mình xuống thang bộ!");
                        return;
                    }
                    if (elevator != null && elevator.DoorsOpen) _state = State.EnteringCar;
                    break;

                case State.EnteringCar:
                    if (Gripped()) { Stop(); Say($"[{displayName}] Được rồi, mình đi thang bộ."); return; }
                    if (WalkTo(elevator.Car.center))
                    {
                        _state = State.Trapped;
                        SetVisible(false);
                        Say($"{displayName} đã vào thang máy — thang máy mất điện và kẹt giữa tầng! Phải báo người còn kẹt khi tới điểm tập kết.");
                    }
                    break;

                case State.Stopped:
                    // Hoa sits weak in the corridor: he helps her up and takes her down the stairs with him
                    if (!_withHoa && _hoa != null && _hoa.WaitingForHelp)
                    {
                        if (!WalkTo(_hoa.transform.position, 0.8f)) break;
                        _withHoa = true;
                        _hoa.HandOverTo(transform, stairsDoor);
                        Say($"[{displayName}] Hoa, bám vào mình! Mình dìu bạn xuống thang bộ.");
                        walkSpeed = 0.6f; // her pace
                        break;
                    }
                    if (stairsDoor == null || WalkTo(stairsDoor.position)) { _state = State.Gone; SetVisible(false); }
                    break;
            }
        }

        private void Stop()
        {
            _state = State.Stopped;
            _pose.Set(BodyPose.None, 0.3f);
            foreach (var n in FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None)) if (n.Role == NPCRole.HoaFaint) _hoa = n;
            Debug.Log($"[NPC] {displayName} stopped; Hoa {(_hoa == null ? "not found" : $"waiting={_hoa.WaitingForHelp} active={_hoa.gameObject.activeInHierarchy} rescued={_hoa.IsRescued} at {_hoa.transform.position}")}");
        }

        private bool WalkTo(Vector3 target, float arrive = 0.3f)
        {
            target.y = transform.position.y;
            Vector3 d = target - transform.position;
            if (d.magnitude < arrive) { if (_walk != null) _walk.enabled = false; return true; }
            if (_walk != null) _walk.enabled = true;
            transform.SetPositionAndRotation(Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime),
                Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), 8f * Time.deltaTime));
            return false;
        }

        private bool Gripped()
        {
            Vector3 s = ShoulderPoint;
            foreach (var i in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (!i.isActiveAndEnabled || i.hasSelection || !i.selectInput.ReadIsPerformed()) continue;
                if (Vector3.Distance(i.transform.position, s) <= reach) return true;
            }
            return false;
        }

        private static void Say(string m) => EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(m));
    }
}
