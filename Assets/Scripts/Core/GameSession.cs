using System;
using UnityEngine;

namespace EscapeFire.Core
{
    public enum TrainingMode
    {
        Guided, // Highlights + floor arrows (beginners)
        Exam    // No hints, full adaptive pressure
    }

    /// <summary>
    /// Cross-scene session state (MainMenu -> Lab -> AAR). Plain static, reset each play session.
    /// </summary>
    public static class GameSession
    {
        public const string DEFAULT_STUDENT_ID = "SV_2026_TEST";
        public const float MIN_CALIBRATED_HEIGHT = 1.0f;
        public const float MAX_CALIBRATED_HEIGHT = 2.3f;

        public static TrainingMode Mode { get; private set; } = TrainingMode.Guided;
        public static string StudentId { get; private set; } = DEFAULT_STUDENT_ID;
        public static string SessionId { get; private set; } = string.Empty;
        public static float CalibratedHeight { get; private set; }
        public static bool IsCalibrated => CalibratedHeight > 0f;

        /// <summary>HTTP base of the edge server (e.g. http://192.168.1.100:8765), empty when unknown. Set by EdgeDirectorClient.</summary>
        public static string EdgeHttpBase { get; set; } = string.Empty;

        public static void Begin(TrainingMode mode, string studentId)
        {
            Mode = mode;
            StudentId = string.IsNullOrWhiteSpace(studentId) ? DEFAULT_STUDENT_ID : studentId.Trim();
            SessionId = Guid.NewGuid().ToString("N");
        }

        public static void SetCalibratedHeight(float standingHeadHeight)
        {
            if (standingHeadHeight < MIN_CALIBRATED_HEIGHT || standingHeadHeight > MAX_CALIBRATED_HEIGHT)
            {
                throw new ArgumentOutOfRangeException(nameof(standingHeadHeight), standingHeadHeight,
                    $"Calibrated height must be within [{MIN_CALIBRATED_HEIGHT}, {MAX_CALIBRATED_HEIGHT}] m.");
            }
            CalibratedHeight = standingHeadHeight;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Mode = TrainingMode.Guided;
            StudentId = DEFAULT_STUDENT_ID;
            SessionId = string.Empty;
            CalibratedHeight = 0f;
            EdgeHttpBase = string.Empty;
        }
    }
}
