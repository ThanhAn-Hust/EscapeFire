using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Tracks PCCC safety rule compliance, score additions/deductions, and generates telemetry JSON reports.
    /// </summary>
    public class ScoringManager : MonoBehaviour
    {
        public static ScoringManager Instance { get; private set; }

        public int TotalScore { get; private set; } = 0;
        private readonly List<string> _actionLogs = new List<string>();
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
            TotalScore += points;
            string log = $"[+{points} pts] {reason} (Time: {GetElapsedSeconds():F1}s)";
            _actionLogs.Add(log);
            Debug.Log($"[Scoring] {log}");
        }

        public void DeductScore(int points, string reason)
        {
            TotalScore -= points;
            string log = $"[-{points} pts] {reason} (Time: {GetElapsedSeconds():F1}s)";
            _actionLogs.Add(log);
            Debug.LogWarning($"[Scoring] {log}");
        }

        public float GetElapsedSeconds()
        {
            return Time.time - _sessionStartTime;
        }

        public string ExportJSONReport(string studentId = "SV_2026_TEST")
        {
            var report = new
            {
                StudentId = studentId,
                FinalScore = TotalScore,
                TimeElapsedSeconds = GetElapsedSeconds(),
                ComplianceLogs = _actionLogs
            };

            return JsonConvert.SerializeObject(report, Formatting.Indented);
        }
    }
}
