using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using EscapeFire.Core;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Tracks PCCC safety rule compliance, score additions/deductions, and generates telemetry JSON reports.
    /// </summary>
    public class ScoringManager : MonoBehaviour
    {
        public static ScoringManager Instance { get; private set; }

        private readonly ScoreLedger _ledger = new ScoreLedger();
        public int TotalScore => _ledger.Total;
        private float _sessionStartTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _sessionStartTime = Time.time;
        }

        public void AddScore(int points, string reason)
        {
            var entry = _ledger.Add(points, reason, GetElapsedSeconds());
            Debug.Log($"[Scoring] {ScoreLedger.Format(entry)}");
            EventBus<ScoreChangedEvent>.Publish(new ScoreChangedEvent(entry.Delta, _ledger.Total, entry.Reason));
        }

        public void DeductScore(int points, string reason)
        {
            var entry = _ledger.Deduct(points, reason, GetElapsedSeconds());
            Debug.LogWarning($"[Scoring] {ScoreLedger.Format(entry)}");
            EventBus<ScoreChangedEvent>.Publish(new ScoreChangedEvent(entry.Delta, _ledger.Total, entry.Reason));
        }

        public float GetElapsedSeconds()
        {
            return Time.time - _sessionStartTime;
        }

        public List<string> GetFormattedLogs()
        {
            var logs = new List<string>(_ledger.Entries.Count);
            for (int i = 0; i < _ledger.Entries.Count; i++)
            {
                logs.Add(ScoreLedger.Format(_ledger.Entries[i]));
            }
            return logs;
        }

        public string ExportJSONReport(string studentId = null)
        {
            var logs = GetFormattedLogs();

            var report = new
            {
                StudentId = studentId ?? GameSession.StudentId,
                SessionId = GameSession.SessionId,
                Mode = GameSession.Mode.ToString(),
                FinalScore = TotalScore,
                AverageCognitiveFriction = CognitiveFrictionTracker.Instance != null ? CognitiveFrictionTracker.Instance.AverageCF : 0f,
                StressResilienceIndex = CognitiveFrictionTracker.Instance != null ? 1f - CognitiveFrictionTracker.Instance.AverageCF : 1f,
                SafetyMistakes = CognitiveFrictionTracker.Instance != null ? CognitiveFrictionTracker.Instance.Mistakes : 0,
                AdaptiveEscalations = Scenario.MicroAIGovernor.Instance != null ? Scenario.MicroAIGovernor.Instance.EscalationCount : 0,
                TimeElapsedSeconds = GetElapsedSeconds(),
                ComplianceLogs = logs
            };

            return JsonConvert.SerializeObject(report, Formatting.Indented);
        }
    }
}
