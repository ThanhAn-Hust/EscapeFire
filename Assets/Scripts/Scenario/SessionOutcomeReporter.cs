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
            var scoring = ScoringManager.Instance;
            string json = scoring != null ? scoring.ExportJSONReport() : string.Empty;
            string failCause = LabProgressiveScenarioController.Instance != null
                ? LabProgressiveScenarioController.Instance.FailCause
                : string.Empty;

            SessionResult.Record(
                passed: won,
                score: scoring != null ? scoring.TotalScore : 0,
                durationSeconds: scoring != null ? scoring.GetElapsedSeconds() : 0f,
                failCause: failCause,
                logs: scoring != null ? scoring.GetFormattedLogs() : null,
                reportJson: json);

            if (telemetryLogger != null && !string.IsNullOrEmpty(json))
            {
                telemetryLogger.SaveTelemetryReport(json, GameSession.StudentId);
            }

            if (loadAARScene) StartCoroutine(LoadAARAfterDelay());
        }

        private IEnumerator LoadAARAfterDelay()
        {
            yield return new WaitForSecondsRealtime(delayBeforeAAR);
            SceneFlowManager.Load(SceneNames.AAR);
        }
    }
}
