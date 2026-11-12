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
        [Tooltip("Upper bound of AI-added challenges per drill (local + edge) so the learner is never overwhelmed.")]
        [SerializeField] private int maxEscalationsPerSession = 4;
        [Tooltip("Low CF right after the fire starts only means the learner has not acted yet: observe before escalating.")]
        [SerializeField] private float minObservationSeconds = 15f;

        /// <summary>Candidate challenges, easiest first. Selection is tier-gated (see DirectiveTiers).</summary>
        private static readonly Directive[] ESCALATION_ORDER =
        {
            Directive.TriggerCo2PinJam,                 // tier 1
            Directive.TriggerSmokeSurge,                // tier 1
            Directive.TriggerAlarmCableCut,             // tier 2
            Directive.TriggerBlackout,                  // tier 2
            Directive.EscalateSecondaryFire,            // tier 3
            Directive.TriggerExtinguisherLowPressure,   // tier 3
            Directive.EscalateFm200Countdown            // tier 4
        };

        private FireExtinguisher[] _extinguishers;
        private FireAlarmButton[] _alarms;
        private float _nextEvaluation;
        private float _lastEscalation = float.NegativeInfinity;
        private float _lastCalming = float.NegativeInfinity;
        private bool _pinJamApplied;
        private int _escalationCount;
        private readonly System.Collections.Generic.HashSet<Directive> _applied = new System.Collections.Generic.HashSet<Directive>();

        public int EscalationCount => _escalationCount;

        /// <summary>Paused by EdgeDirectorClient while the LLM edge server is connected (it decides escalations then).</summary>
        public bool LocalEscalationEnabled { get; set; } = true;

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

        private SmokeDensityController _smoke;
        private BlackoutController _blackout;
        private CircuitBreakerSwitch _breaker;

        private void Start()
        {
            _smoke = FindFirstObjectByType<SmokeDensityController>();
            _breaker = FindFirstObjectByType<CircuitBreakerSwitch>();
            _blackout = FindFirstObjectByType<BlackoutController>();
            if (_blackout == null) _blackout = gameObject.AddComponent<BlackoutController>();
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

        private float _emergencyStart = -1f;

        private void OnEnable()
        {
            EventBus<CognitiveStateChangedEvent>.Subscribe(OnCognitiveStateChanged);
            EventBus<PhaseChangedEvent>.Subscribe(OnPhaseChanged);
        }

        private void OnDisable()
        {
            EventBus<CognitiveStateChangedEvent>.Unsubscribe(OnCognitiveStateChanged);
            EventBus<PhaseChangedEvent>.Unsubscribe(OnPhaseChanged);
        }

        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.Phase == ScenarioPhase.Phase2_ServerExplosionFire && _emergencyStart < 0f) _emergencyStart = Time.time;
        }

        private void Update()
        {
            if (!AdaptiveEnabled || !LocalEscalationEnabled || Time.time < _nextEvaluation) return;
            _nextEvaluation = Time.time + evaluationIntervalSeconds;

            var tracker = CognitiveFrictionTracker.Instance;
            if (tracker == null || tracker.State != CognitiveState.Calm) return;
            if (Time.time - _lastEscalation < escalationCooldownSeconds) return;

            if (_escalationCount >= maxEscalationsPerSession) return;
            if (_emergencyStart < 0f || Time.time - _emergencyStart < minObservationSeconds) return;

            // Prefer the hardest tier currently allowed, then fall back to easier unused ones.
            // Within a tier the starting candidate is random, so every drill plays out differently.
            int allowed = DirectiveTiers.AllowedTier(_escalationCount);
            int offset = Random.Range(0, ESCALATION_ORDER.Length);
            for (int tier = allowed; tier >= 1; tier--)
            {
                for (int k = 0; k < ESCALATION_ORDER.Length; k++)
                {
                    var d = ESCALATION_ORDER[(k + offset) % ESCALATION_ORDER.Length];
                    if (DirectiveTiers.TierOf(d) != tier || _applied.Contains(d)) continue; // each type once per drill
                    if (TryExecute(d, "local") == DirectiveResult.Accepted)
                    {
                        _lastEscalation = Time.time;
                        return;
                    }
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
                else
                {
                    _escalationCount++;
                    _applied.Add(directive);
                }
            }

            var cf = CognitiveFrictionTracker.Instance;
            Debug.Log($"[MicroAIGovernor] {source}:{DirectiveCodes.ToCode(directive)} -> {result} (CF={(cf != null ? cf.CurrentCF : 0f):F2})");
            EventBus<DirectiveResolvedEvent>.Publish(new DirectiveResolvedEvent(directive, result, source));
            return result;
        }

        /// <summary>
        /// V4 composed challenge from the edge server: the shared Tier-1 rules again on the headset, then the scene
        /// checks of <see cref="ChallengeRunner"/>. Returns null when it runs, else the rejection reason.
        /// </summary>
        public string TryExecuteChallenge(Newtonsoft.Json.Linq.JToken spec, string source)
        {
            var runner = ChallengeRunner.Instance != null ? ChallengeRunner.Instance : gameObject.AddComponent<ChallengeRunner>();
            var tracker = CognitiveFrictionTracker.Instance;
            string why = null;
            if (!IsEmergencyActive()) why = "wrong_phase";
            else if (tracker != null && tracker.State == CognitiveState.Panic) why = "learner_panicking";
            else if (_escalationCount >= maxEscalationsPerSession) why = "max_challenges";
            var check = EscapeFire.Generation.ChallengeValidator.Validate(spec, runner.Context(_escalationCount, tracker != null ? tracker.CurrentCF : 0f));
            if (why == null) why = runner.SceneCheck(check);
            if (why == null)
            {
                runner.Run(check);
                _escalationCount++;
                _lastEscalation = Time.time;
            }
            Debug.Log($"[MicroAIGovernor] {source}:challenge[{string.Join("+", check.Codes)}] -> {why ?? "Accepted"}");
            EventBus<ChallengeResolvedEvent>.Publish(new ChallengeResolvedEvent(string.Join("+", check.Codes), why ?? "Accepted", source));
            return why;
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
                Fm200Active = fm200 == null || fm200.IsCountdownActive || fm200.HasDischarged,
                ChallengesSoFar = _escalationCount,
                SmokeCanSurge = _smoke != null && _smoke.CanSurge,
                SmokeSurgeApplied = _smoke != null && _smoke.HasSurged,
                BlackoutActive = _blackout != null && _blackout.IsBlackout,
                PowerAlreadyCut = _breaker != null && !_breaker.IsPowerOn
            };

            var controller = LabProgressiveScenarioController.Instance;
            var fire = controller != null ? controller.ServerFire : null;
            ctx.FireBurning = fire != null && fire.isActiveAndEnabled && !fire.IsExtinguished;

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
                if (_extinguishers[i].Type == ExtinguisherType.CO2)
                {
                    anyCo2 = true;
                    if (_extinguishers[i].HasLowPressure) ctx.LowPressureApplied = true;
                }
            }
            ctx.HasCo2Extinguisher = anyCo2;
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

                case Directive.TriggerSmokeSurge:
                    if (_smoke == null || !_smoke.Surge()) return false;
                    Mentor("Khói đen đặc đột ngột tràn xuống! Tầm nhìn giảm mạnh — tư thế của bạn lúc này nên thế nào?");
                    return true;

                case Directive.TriggerBlackout:
                    if (_blackout == null || !_blackout.Trigger()) return false;
                    Mentor("Mất điện chiếu sáng! Chỉ còn đèn khẩn cấp. Hãy tìm biển EXIT màu xanh và lần theo mép tường.");
                    return true;

                case Directive.TriggerExtinguisherLowPressure:
                    bool applied = false;
                    for (int i = 0; i < _extinguishers.Length; i++)
                    {
                        if (_extinguishers[i] == null || _extinguishers[i].Type != ExtinguisherType.CO2) continue;
                        _extinguishers[i].SetLowPressure(4f);
                        applied = true;
                    }
                    if (applied) Mentor("Kim đồng hồ áp suất bình CO2 đang ở vạch đỏ — chỉ còn vài giây phun. Bạn sẽ dùng nó thế nào?");
                    return applied;

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
