using System;
using System.Collections.Generic;

namespace EscapeFire.Core
{
    /// <summary>
    /// Outcome of the last drill, handed from the Lab scene to the AAR scene. Plain static (survives scene loads).
    /// </summary>
    public static class SessionResult
    {
        private static readonly List<string> _logs = new List<string>(32);

        public static bool HasResult { get; private set; }
        public static bool Passed { get; private set; }
        public static int Score { get; private set; }
        public static float DurationSeconds { get; private set; }
        public static string FailCause { get; private set; } = string.Empty;
        public static string ReportJson { get; private set; } = string.Empty;
        public static IReadOnlyList<string> Logs => _logs;

        public static void Record(bool passed, int score, float durationSeconds, string failCause,
            IReadOnlyList<string> logs, string reportJson)
        {
            HasResult = true;
            Passed = passed;
            Score = score;
            DurationSeconds = Math.Max(0f, durationSeconds);
            FailCause = passed ? string.Empty : (failCause ?? string.Empty);
            ReportJson = reportJson ?? string.Empty;
            _logs.Clear();
            if (logs != null)
            {
                for (int i = 0; i < logs.Count; i++) _logs.Add(logs[i]);
            }
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            HasResult = false;
            Passed = false;
            Score = 0;
            DurationSeconds = 0f;
            FailCause = string.Empty;
            ReportJson = string.Empty;
            _logs.Clear();
        }
    }

    public static class GradeRules
    {
        public const int EXCELLENT_MIN = 100;
        public const int GOOD_MIN = 75;
        public const int PASS_MIN = 50;

        /// <summary>Vietnamese grade label shown on the AAR scorecard. A failed drill never passes.</summary>
        public static string Classify(bool passed, int score)
        {
            if (!passed) return "KHÔNG ĐẠT - CHƯA AN TOÀN";
            if (score >= EXCELLENT_MIN) return "XUẤT SẮC - ĐẠT CHUẨN AN TOÀN";
            if (score >= GOOD_MIN) return "KHÁ - ĐẠT CHUẨN AN TOÀN";
            if (score >= PASS_MIN) return "ĐẠT";
            return "CHƯA ĐẠT - CẦN LUYỆN TẬP THÊM";
        }
    }
}
