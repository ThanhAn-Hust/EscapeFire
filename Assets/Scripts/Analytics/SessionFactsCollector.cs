using System.Collections.Generic;
using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Player;
using EscapeFire.Scenario;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Listens to gameplay events and records the facts needed for the AAR scorecard, plus the learner's
    /// head trajectory every 0.2 s (position + "standing in smoke" flag) for the 3D heatmap path.
    /// </summary>
    public class SessionFactsCollector : MonoBehaviour
    {
        public const int MAX_SAMPLES = 3000; // 10 minutes at 5 Hz

        public static SessionFactsCollector Instance { get; private set; }

        [SerializeField] private float sampleInterval = 0.2f;
        [SerializeField] private Transform head;
        [SerializeField] private VRHeightDetector heightDetector;
        [SerializeField] private SmokeDensityController smoke;

        private readonly List<Vector3> _positions = new List<Vector3>(MAX_SAMPLES);
        private readonly List<bool> _inSmokeStanding = new List<bool>(MAX_SAMPLES);
        private SessionFacts _facts = SessionFacts.Empty;
        private float _startTime;
        private float _sampleTimer;
        private bool _emergency;

        public IReadOnlyList<Vector3> Positions => _positions;
        public IReadOnlyList<bool> StandingInSmokeFlags => _inSmokeStanding;

        private float Now => Time.time - _startTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _startTime = Time.time;
        }

        private void Start()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (heightDetector == null) heightDetector = FindFirstObjectByType<VRHeightDetector>();
            if (smoke == null) smoke = FindFirstObjectByType<SmokeDensityController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EventBus<PhaseChangedEvent>.Subscribe(OnPhase);
            EventBus<AlarmActivatedEvent>.Subscribe(OnAlarm);
            EventBus<PowerStateChangedEvent>.Subscribe(OnPower);
            EventBus<ExtinguisherSprayEvent>.Subscribe(OnSpray);
            EventBus<ExtinguisherPinPulledEvent>.Subscribe(OnPin);
            EventBus<FireExtinguishedEvent>.Subscribe(OnExtinguished);
            EventBus<NpcRescuedEvent>.Subscribe(OnRescue);
            EventBus<DoorHeatCheckedEvent>.Subscribe(OnDoor);
            EventBus<DirectiveResolvedEvent>.Subscribe(OnDirective);
        }

        private void OnDisable()
        {
            EventBus<PhaseChangedEvent>.Unsubscribe(OnPhase);
            EventBus<AlarmActivatedEvent>.Unsubscribe(OnAlarm);
            EventBus<PowerStateChangedEvent>.Unsubscribe(OnPower);
            EventBus<ExtinguisherSprayEvent>.Unsubscribe(OnSpray);
            EventBus<ExtinguisherPinPulledEvent>.Unsubscribe(OnPin);
            EventBus<FireExtinguishedEvent>.Unsubscribe(OnExtinguished);
            EventBus<NpcRescuedEvent>.Unsubscribe(OnRescue);
            EventBus<DoorHeatCheckedEvent>.Unsubscribe(OnDoor);
            EventBus<DirectiveResolvedEvent>.Unsubscribe(OnDirective);
        }

        private void Update()
        {
            if (head == null) return;

            bool standingInSmoke = _emergency && smoke != null &&
                                   BreathingRules.IsBreathingSmoke(head.position.y, smoke.CurrentSmokeHeight,
                                       heightDetector != null && heightDetector.IsCrouching);
            if (standingInSmoke) _facts.StandingInSmokeSeconds += Time.deltaTime;

            _sampleTimer += Time.deltaTime;
            if (_sampleTimer < sampleInterval || _positions.Count >= MAX_SAMPLES) return;
            _sampleTimer = 0f;
            _positions.Add(head.position);
            _inSmokeStanding.Add(standingInSmoke);
        }

        public SessionFacts BuildFacts(bool passed, string failCause)
        {
            var f = _facts;
            f.Passed = passed;
            f.FailCause = failCause ?? string.Empty;
            f.DurationSeconds = Now;
            var tracker = CognitiveFrictionTracker.Instance;
            f.AverageCognitiveFriction = tracker != null ? tracker.AverageCF : 0.5f;
            float spraying = 0f, atBase = 0f, sweeping = 0f;
            foreach (var fx in FindObjectsByType<EscapeFire.Interactions.ExtinguisherSprayEffect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                spraying += fx.SecondsSpraying;
                atBase += fx.SecondsAtBase;
                sweeping += fx.SecondsSweeping;
            }
            if (spraying >= 0.5f)
            {
                f.PassBaseRatio = atBase / spraying;
                f.PassSweepRatio = sweeping / spraying;
            }
            CollectV3(ref f);
            return f;
        }

        /// <summary>114 call, rescue and evacuation facts (V3), read from the scene's components at the end.</summary>
        private static void CollectV3(ref SessionFacts f)
        {
            var call = EscapeFire.Scenario.Call114Dialogue.Instance;
            f.Call114Present = call != null;
            f.Call114Complete = call != null && call.IsComplete;
            f.Call114AskBacks = call != null ? call.AskBacks : 0;
            var victims = EscapeFire.NPC.VictimBody.All;
            f.VictimPresent = victims.Count > 0;
            f.VictimSafe = f.VictimPresent;
            foreach (var v in victims) if (!v.IsSafe) f.VictimSafe = false;
            var minh = FindFirstObjectByType<EscapeFire.NPC.PanicRunner>();
            f.PanicNpcPresent = minh != null && minh.Active;
            f.PanicNpcStopped = minh != null && minh.Stopped;
            f.ElevatorUsed = EscapeFire.Interactions.ElevatorTrap.ElevatorUsedThisSession;
            var wall = EscapeFire.Player.WallFollowTracker.Instance;
            f.WallFollowRatio = wall != null ? wall.Ratio : -1f;
            var ap = EscapeFire.Scenario.AssemblyPoint.Instance;
            f.RollCallPresent = ap != null && ap.Arrived;
            f.RollCallCorrect = ap != null && ap.AnswerCorrect;
        }

        private void OnPhase(PhaseChangedEvent e)
        {
            _emergency = e.Phase == ScenarioPhase.Phase2_ServerExplosionFire || e.Phase == ScenarioPhase.Phase3_SmokeEvacuation;
            if (e.Phase == ScenarioPhase.Phase2_ServerExplosionFire && _facts.FireStartedAt < 0f) _facts.FireStartedAt = Now;
        }

        private void OnAlarm(AlarmActivatedEvent _)
        {
            if (_facts.AlarmRaisedAt < 0f) _facts.AlarmRaisedAt = Now;
        }

        private void OnPower(PowerStateChangedEvent e)
        {
            if (!e.IsPowerOn && _facts.PowerCutAt < 0f) _facts.PowerCutAt = Now;
        }

        private void OnSpray(ExtinguisherSprayEvent e)
        {
            bool conductive = e.Type == ExtinguisherType.WaterFoam;
            if (_facts.FirstSprayAt < 0f)
            {
                _facts.FirstSprayAt = Now;
                _facts.FirstSprayWithConductiveAgent = conductive;
            }
            if (conductive && e.PowerLive) _facts.UsedConductiveAgentOnLivePower = true;
        }

        private void OnPin(ExtinguisherPinPulledEvent _) => _facts.PinPulled = true;

        private void OnExtinguished(FireExtinguishedEvent e)
        {
            var controller = LabProgressiveScenarioController.Instance;
            if (controller == null || controller.ServerFire == null || controller.ServerFire.GetInstanceID() == e.FireInstanceId)
                _facts.FireExtinguished = true;
        }

        private void OnRescue(NpcRescuedEvent _) => _facts.NpcRescued = true;

        private void OnDoor(DoorHeatCheckedEvent _) => _facts.DoorHeatChecked = true;

        private void OnDirective(DirectiveResolvedEvent e)
        {
            if (e.Result == DirectiveResult.Accepted && e.Directive != Directive.MaintainCurrentPace) _facts.AnomaliesTriggered++;
        }
    }
}
