using System.Collections;
using UnityEngine;
using EscapeFire.Analytics;
using EscapeFire.Core;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// When the drill ends (win or fail), records the result for the AAR scene, saves the telemetry report,
    /// and loads 02_AAR_Evaluation after a short delay.
    /// </summary>
    public class SessionOutcomeReporter : MonoBehaviour
    {
        [SerializeField] private float delayBeforeAAR = 4f;
        [SerializeField] private TelemetryLogger telemetryLogger;
        [Tooltip("Disable to stay in the lab after the drill ends (debugging).")]
        [SerializeField] private bool loadAARScene = true;

        private bool _reported;

        private void OnEnable() => EventBus<PhaseChangedEvent>.Subscribe(HandlePhaseChanged);

        private void OnDisable() => EventBus<PhaseChangedEvent>.Unsubscribe(HandlePhaseChanged);

        private void Awake()
        {
            if (telemetryLogger == null) telemetryLogger = FindFirstObjectByType<TelemetryLogger>();
        }

        private void HandlePhaseChanged(PhaseChangedEvent e)
        {
            if (_reported) return;
            bool won = e.Phase == ScenarioPhase.Phase4_CompletedWin;
            bool failed = e.Phase == ScenarioPhase.Phase5_GameOverFail;
            if (!won && !failed) return;

            _reported = true;
            if (won && GameSession.Mode == TrainingMode.Guided) ProgressionStore.GuidedPassed = true; // unlocks the exam
            var scoring = ScoringManager.Instance;
            string json = scoring != null ? scoring.ExportJSONReport() : string.Empty;
            string failCause = LabProgressiveScenarioController.Instance != null
                ? LabProgressiveScenarioController.Instance.FailCause
                : string.Empty;

            var collector = SessionFactsCollector.Instance;
            var facts = collector != null ? collector.BuildFacts(won, failCause) : SessionFacts.Empty;
            if (collector == null) { facts.Passed = won; facts.FailCause = failCause; }
            var scorecard = AfterActionReport.BuildScorecard(facts);
            // VL.1: what this drill showed about each PCCC skill, folded into the learner's skill model
            var observations = SkillEvidence.FromDrill(facts);
            var skills = SkillStore.Record(observations);
            var radar = AfterActionReport.BuildRadar(facts, scorecard);
            string debrief = AfterActionReport.TemplateDebrief(facts, scorecard);

            SessionResult.Record(
                passed: won,
                score: scoring != null ? scoring.TotalScore : 0,
                durationSeconds: scoring != null ? scoring.GetElapsedSeconds() : 0f,
                failCause: failCause,
                logs: scoring != null ? scoring.GetFormattedLogs() : null,
                reportJson: json);

            SessionResult.RecordSkills(observations, skills);
            SessionResult.RecordReview(facts, scorecard, radar, debrief,
                collector != null ? collector.Positions : null,
                collector != null ? collector.StandingInSmokeFlags : null);

            if (telemetryLogger != null && !string.IsNullOrEmpty(json))
            {
                telemetryLogger.SaveTelemetryReport(json, GameSession.StudentId);
            }

            string lmsJson = LmsReportBuilder.Build(GameSession.SessionId, GameSession.StudentId, GameSession.Mode.ToString(),
                facts, scorecard, radar, debrief, scoring != null ? scoring.GetFormattedLogs() : new System.Collections.Generic.List<string>(),
                MicroAIGovernor.Instance != null ? MicroAIGovernor.Instance.EscalationCount : 0, observations, skills);
            LmsSync.Submit(GameSession.SessionId, lmsJson);

            if (loadAARScene) StartCoroutine(LoadAARAfterDelay());
        }

        private IEnumerator LoadAARAfterDelay()
        {
            yield return new WaitForSecondsRealtime(delayBeforeAAR);
            SceneFlowManager.Load(SceneNames.AAR);
        }
    }
}
