using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EscapeFire.Core
{
    /// <summary>One level of the course (Resources/Course.json).</summary>
    public sealed class CourseLevel
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("title")] public string Title;
        [JsonProperty("place")] public string Place;
        [JsonProperty("goal")] public string Goal;
        [JsonProperty("skills")] public List<string> Skills = new List<string>();
        [JsonProperty("scene")] public string Scene;          // "lab" | "generated"
        [JsonProperty("scenario")] public string Scenario;    // Resources/Scenarios/<name>.json for generated rooms
        [JsonProperty("mode")] public string Mode = "Guided"; // Guided | Exam
        [JsonProperty("forced")] public JObject Forced;        // a challenge applied when the fire breaks out (lab)

        public TrainingMode TrainingMode => Mode == "Exam" ? TrainingMode.Exam : TrainingMode.Guided;
        public bool InLab => Scene == "lab";
    }

    /// <summary>
    /// The course (playtest 18/11: one 2-minute scenario was not enough depth): six levels from a small fire to the
    /// full exam, each with its goal and the PCCC skills it trains, unlocked in order, scored in stars from the drill's
    /// score (≥ 90 → 3, ≥ 70 → 2, passed → 1). Progress is kept per device like the rest of the ladder.
    /// </summary>
    public static class Course
    {
        private sealed class Root { [JsonProperty("levels")] public List<CourseLevel> Levels = new List<CourseLevel>(); }

        private static List<CourseLevel> _levels;

        public static IReadOnlyList<CourseLevel> Levels
        {
            get
            {
                if (_levels == null)
                {
                    var asset = Resources.Load<TextAsset>("Course");
                    _levels = asset != null ? JsonConvert.DeserializeObject<Root>(asset.text).Levels : new List<CourseLevel>();
                }
                return _levels;
            }
        }

        /// <summary>The level being played (null for drills started outside the course).</summary>
        public static CourseLevel Current { get; set; }

        public static int IndexOf(CourseLevel l) => _levels == null ? -1 : _levels.IndexOf(l);

        private static string Key(string id, string what) => ProgressionStore.KeyPrefix + "course_" + id + "_" + what;

        public static int BestScore(string id) => PlayerPrefs.GetInt(Key(id, "best"), -1);
        public static bool Passed(string id) => PlayerPrefs.GetInt(Key(id, "passed"), 0) == 1;

        public static int Stars(string id)
        {
            if (!Passed(id)) return 0;
            int s = BestScore(id);
            return s >= 90 ? 3 : s >= 70 ? 2 : 1;
        }

        /// <summary>Level 1 opens with the lesson; each next level once the previous one is passed.</summary>
        public static bool Unlocked(int index, bool requireProgression = true)
        {
            if (!requireProgression) return true;
            if (index <= 0) return ProgressionStore.LessonDone;
            return index < Levels.Count && Passed(Levels[index - 1].Id);
        }

        public static int PassedCount()
        {
            int n = 0;
            foreach (var l in Levels) if (Passed(l.Id)) n++;
            return n;
        }

        /// <summary>The result of the drill just played (called by the debrief scene).</summary>
        public static void Record(int score, bool passed)
        {
            if (Current == null) return;
            if (score > BestScore(Current.Id)) PlayerPrefs.SetInt(Key(Current.Id, "best"), score);
            if (passed) PlayerPrefs.SetInt(Key(Current.Id, "passed"), 1);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            foreach (var l in Levels)
            {
                PlayerPrefs.DeleteKey(Key(l.Id, "best"));
                PlayerPrefs.DeleteKey(Key(l.Id, "passed"));
            }
            PlayerPrefs.Save();
        }

        /// <summary>For tests: reload Course.json.</summary>
        public static void Reload() => _levels = null;
    }
}
