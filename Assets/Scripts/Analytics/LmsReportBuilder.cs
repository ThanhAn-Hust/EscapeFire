using System.Collections.Generic;
using Newtonsoft.Json;

namespace EscapeFire.Analytics
{
    /// <summary>Builds the LMS payload (5-axis radar + 6-item scorecard + raw facts + compliance log).</summary>
    public static class LmsReportBuilder
    {
        public static string Build(string sessionId, string studentId, string mode, in SessionFacts facts,
            List<ScorecardItem> scorecard, SkillRadar radar, string debrief, IReadOnlyList<string> complianceLog,
            int adaptiveEscalations)
        {
            var items = new List<Dictionary<string, object>>(scorecard.Count);
            foreach (var it in scorecard)
            {
                items.Add(new Dictionary<string, object>
                {
                    { "key", it.Key }, { "label", it.Label }, { "points", it.Points },
                    { "max_points", it.MaxPoints }, { "note", it.Note }
                });
            }

            var payload = new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "student_id", studentId },
                { "mode", mode },
                { "passed", facts.Passed },
                { "fail_cause", facts.FailCause },
                { "duration_seconds", facts.DurationSeconds },
                { "total_score", AfterActionReport.Total(scorecard) },
                { "scorecard", items },
                { "radar", new Dictionary<string, float>
                    {
                        { "risk_detection_score", radar.RiskDetection },
                        { "extinguisher_selection_score", radar.ExtinguisherSelection },
                        { "crouch_compliance_score", radar.CrouchCompliance },
                        { "anomaly_handling_score", radar.AnomalyHandling },
                        { "stress_resilience_index", radar.StressResilience }
                    }
                },
                { "facts", facts },
                { "adaptive_escalations", adaptiveEscalations },
                { "debrief", debrief },
                { "compliance_log", complianceLog }
            };
            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }
    }
}
