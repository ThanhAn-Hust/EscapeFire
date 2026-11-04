using UnityEngine;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Local (0 ms) adaptive director on the headset.
    /// - Panic (high CF): calming Socratic guidance, never escalates.
    /// - Calm (low CF) during the emergency: escalates one challenge from the safe action space,
    ///   each validated by the Tier-2 <see cref="DirectiveValidator"/> (budget, exit, state).
    /// <see cref="TryExecute"/> is also the entry point for directives coming from the edge LLM server (M3).
    /// </summary>
    public class MicroAIGovernor : MonoBehaviour
    {
        public static MicroAIGovernor Instance { get; private set; }

        [SerializeField] private bool adaptiveInGuidedMode = false;
        [SerializeField] private float evaluationIntervalSeconds = 5f;
        [SerializeField] private float escalationCooldownSeconds = 45f;
        [SerializeField] private float calmingCooldownSeconds = 20f;
        [SerializeField] private SecondaryFireSlot[] secondaryFireSlots;

        private static readonly Directive[] ESCALATION_ORDER =
        {
            Directive.TriggerCo2PinJam,
            Directive.TriggerAlarmCableCut,
            Directive.EscalateSecondaryFire,
            Directive.EscalateFm200Countdown
        };

        private FireExtinguisher[] _extinguishers;
        private FireAlarmButton[] _alarms;
        private float _nextEvaluation;
        private float _lastEscalation = float.NegativeInfinity;
        private float _lastCalming = float.NegativeInfinity;
        private bool _pinJamApplied;
        private int _escalationCount;

        public int EscalationCount => _escalationCount;

        private bool AdaptiveEnabled => adaptiveInGuidedMode || GameSession.Mode == TrainingMode.Exam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _extinguishers = FindObjectsByType<FireExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _alarms = FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (secondaryFireSlots == null || secondaryFireSlots.Length == 0)
            {
                secondaryFireSlots = FindObjectsByType<SecondaryFireSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => EventBus<CognitiveStateChangedEvent>.Subscribe(OnCognitiveStateChanged);

        private void OnDisable() => EventBus<CognitiveStateChangedEvent>.Unsubscribe(OnCognitiveStateChanged);

        private void Update()
        {
            if (!AdaptiveEnabled || Time.time < _nextEvaluation) return;
            _nextEvaluation = Time.time + evaluationIntervalSeconds;

            var tracker = CognitiveFrictionTracker.Instance;
            if (tracker == null || tracker.State != CognitiveState.Calm) return;
            if (Time.time - _lastEscalation < escalationCooldownSeconds) return;

            for (int i = 0; i < ESCALATION_ORDER.Length; i++)
            {
                if (TryExecute(ESCALATION_ORDER[i], "local") == DirectiveResult.Accepted)
                {
                    _lastEscalation = Time.time;
                    return;
                }
            }
        }

        private void OnCognitiveStateChanged(CognitiveStateChangedEvent e)
        {
            if (e.State != CognitiveState.Panic) return;
            if (!IsEmergencyActive()) return;
            if (Time.time - _lastCalming < calmingCooldownSeconds) return;
            _lastCalming = Time.time;
            Mentor("Bình tĩnh, hít thở đều! Cúi thấp người né khói và lần theo mép tường. Việc đầu tiên cần làm là gì?");
        }

        /// <summary>Validates then applies a directive. Source is "local" or "edge" (for telemetry).</summary>
        public DirectiveResult TryExecute(Directive directive, string source)
        {
            var ctx = BuildContext();
            var result = DirectiveValidator.Validate(directive, ctx);
            if (result == DirectiveResult.Accepted && directive != Directive.MaintainCurrentPace)
            {
                if (!Apply(directive)) result = DirectiveResult.RejectedBudgetOverflow;
                else _escalationCount++;
            }

            var cf = CognitiveFrictionTracker.Instance;
            Debug.Log($"[MicroAIGovernor] {source}:{DirectiveCodes.ToCode(directive)} -> {result} (CF={(cf != null ? cf.CurrentCF : 0f):F2})");
            EventBus<DirectiveResolvedEvent>.Publish(new DirectiveResolvedEvent(directive, result, source));
            return result;
        }

        /// <summary>Wire-format overload for the edge server: unknown codes are rejected, never guessed.</summary>
        public DirectiveResult TryExecute(string directiveCode, string source)
        {
            if (!DirectiveCodes.TryParse(directiveCode, out var directive))
            {
                Debug.LogWarning($"[MicroAIGovernor] Rejected unknown directive '{directiveCode}' from {source}.");
                return DirectiveResult.RejectedUnknownDirective;
            }
            return TryExecute(directive, source);
        }

        private DirectiveContext BuildContext()
        {
            var tracker = CognitiveFrictionTracker.Instance;
            var budget = PerformanceBudgetGovernor.Instance;
            var fm200 = FM200System.Instance;

            var ctx = new DirectiveContext
            {
                EmergencyActive = IsEmergencyActive(),
                LearnerPanicking = tracker != null && tracker.State == CognitiveState.Panic,
                ActiveFires = budget != null ? budget.ActiveFires : 0,
                MaxFires = budget != null ? PerformanceBudgetGovernor.MAX_FIRE_EMITTERS : int.MaxValue,
                PinJamAlreadyApplied = _pinJamApplied,
                Fm200Active = fm200 == null || fm200.IsCountdownActive || fm200.HasDischarged
            };

            for (int i = 0; secondaryFireSlots != null && i < secondaryFireSlots.Length; i++)
            {
                if (secondaryFireSlots[i] != null && secondaryFireSlots[i].IsAvailable)
                {
                    ctx.HasSafeSecondarySlot = true;
                    break;
                }
            }

            bool anyCo2 = false;
            for (int i = 0; _extinguishers != null && i < _extinguishers.Length; i++)
            {
                if (_extinguishers[i] == null) continue;
                if (_extinguishers[i].IsPinPulled) ctx.AnyExtinguisherUnpinned = true;
                if (_extinguishers[i].Type == ExtinguisherType.CO2) anyCo2 = true;
            }
            if (!anyCo2) ctx.PinJamAlreadyApplied = true; // nothing to jam

            bool anyAlarm = false;
            for (int i = 0; _alarms != null && i < _alarms.Length; i++)
            {
                if (_alarms[i] == null) continue;
                anyAlarm = true;
                if (_alarms[i].IsActivated) ctx.AlarmAlreadyRaised = true;
                if (_alarms[i].IsCableCut) ctx.AlarmCableAlreadyCut = true;
            }
            if (!anyAlarm) ctx.AlarmCableAlreadyCut = true; // nothing to cut

            return ctx;
        }

        private bool Apply(Directive directive)
        {
            switch (directive)
            {
                case Directive.EscalateSecondaryFire:
                    var controller = LabProgressiveScenarioController.Instance;
                    var template = controller != null ? controller.ServerFire : null;
                    for (int i = 0; i < secondaryFireSlots.Length; i++)
                    {
                        var slot = secondaryFireSlots[i];
                        if (slot == null || !slot.IsAvailable) continue;
                        if (!slot.Ignite(template)) return false;
                        Mentor($"CẢNH BÁO: Lửa đã lan sang {slot.Description}! Đánh giá lại: dập tiếp hay sơ tán?");
                        return true;
                    }
                    return false;

                case Directive.TriggerCo2PinJam:
                    for (int i = 0; i < _extinguishers.Length; i++)
                    {
                        if (_extinguishers[i] != null && _extinguishers[i].Type == ExtinguisherType.CO2)
                            _extinguishers[i].JamSafetyPin(2);
                    }
                    _pinJamApplied = true;
                    return true;

                case Directive.TriggerAlarmCableCut:
                    for (int i = 0; i < _alarms.Length; i++)
                    {
                        if (_alarms[i] != null) _alarms[i].CutCable();
                    }
                    return true;

                case Directive.EscalateFm200Countdown:
                    if (FM200System.Instance == null) return false;
                    FM200System.Instance.TriggerCountdown();
                    return true;

                default:
                    return true;
            }
        }

        private static bool IsEmergencyActive()
        {
            var controller = LabProgressiveScenarioController.Instance;
            if (controller == null) return false;
            var p = controller.CurrentPhase;
            return p == ScenarioPhase.Phase2_ServerExplosionFire || p == ScenarioPhase.Phase3_SmokeEvacuation;
        }

        private static void Mentor(string message)
        {
            var controller = LabProgressiveScenarioController.Instance;
            if (controller != null) controller.SetAIMentorGuidance(message);
            else EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(message));
        }
    }
}
