using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>Starts a course level: the lab drill (guided / exam) or a generated room from its scenario.</summary>
    public static class CourseLauncher
    {
        public static bool Start(CourseLevel level, string studentId)
        {
            if (level == null) return false;
            Course.Current = level;
            GameSession.Begin(level.TrainingMode, studentId);
            SessionResult.Reset();
            if (level.InLab)
            {
                SessionResult.DrillScene = SceneNames.LAB;
                SceneFlowManager.Load(SceneNames.LAB);
                return true;
            }
            var sample = Resources.Load<TextAsset>("Scenarios/" + level.Scenario);
            if (sample == null) { Debug.LogWarning("[Course] missing scenario " + level.Scenario); return false; }
            EscapeFire.Generation.GeneratedScenario.Pending = Newtonsoft.Json.Linq.JObject.Parse(sample.text);
            EscapeFire.Generation.GeneratedScenario.Seed = Random.Range(1, 10000);
            SessionResult.DrillScene = SceneNames.GENERATED;
            SceneFlowManager.Load(SceneNames.GENERATED);
            return true;
        }
    }
}
