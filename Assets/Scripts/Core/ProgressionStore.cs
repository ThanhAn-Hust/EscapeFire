using UnityEngine;

namespace EscapeFire.Core
{
    /// <summary>
    /// Learning ladder (plan V1.5, advisor: "bài giảng trước, sau đó thực hành, từ dễ đến khó"):
    /// Lesson → Guided drill → Exam. Stored per device in PlayerPrefs.
    /// </summary>
    public static class ProgressionStore
    {
        /// <summary>Tests use their own prefix so they never touch a learner's real progress.</summary>
        public static string KeyPrefix = "EF_progress_";

        public static bool LessonDone
        {
            get => PlayerPrefs.GetInt(KeyPrefix + "lesson", 0) == 1;
            set { PlayerPrefs.SetInt(KeyPrefix + "lesson", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool GuidedPassed
        {
            get => PlayerPrefs.GetInt(KeyPrefix + "guided", 0) == 1;
            set { PlayerPrefs.SetInt(KeyPrefix + "guided", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool GuidedUnlocked => LessonDone;
        public static bool ExamUnlocked => LessonDone && GuidedPassed;

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(KeyPrefix + "lesson");
            PlayerPrefs.DeleteKey(KeyPrefix + "guided");
            PlayerPrefs.Save();
        }
    }
}
