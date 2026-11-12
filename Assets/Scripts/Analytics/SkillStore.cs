using System.Collections.Generic;
using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Per-learner skill model on this device (plan VL.1): PlayerPrefs under the progression prefix + student id, so
    /// tests never touch a learner's data. Every source of evidence (drill, lesson, quiz, generated room) goes here.
    /// </summary>
    public static class SkillStore
    {
        private static string Key(string student) =>
            ProgressionStore.KeyPrefix + "skills_" + (string.IsNullOrEmpty(student) ? "anon" : student);

        public static SkillModel Load(string student = null) =>
            SkillModel.FromJson(PlayerPrefs.GetString(Key(student ?? GameSession.StudentId), ""));

        public static void Save(SkillModel model, string student = null)
        {
            PlayerPrefs.SetString(Key(student ?? GameSession.StudentId), model.ToJson());
            PlayerPrefs.Save();
        }

        public static void Clear(string student = null)
        {
            PlayerPrefs.DeleteKey(Key(student ?? GameSession.StudentId));
            PlayerPrefs.Save();
        }

        /// <summary>Adds one session's observations (a new session starts) and saves.</summary>
        public static SkillModel Record(IReadOnlyList<SkillObservation> observations, float guess = SkillModel.PGuess)
        {
            var model = Load();
            model.BeginSession();
            foreach (var o in observations) model.Observe(o.Skill, o.Correct, guess);
            Save(model);
            return model;
        }

        /// <summary>The skills to practise next for the current learner.</summary>
        public static List<string> WeakSkills(int count = 3) => Load().WeakSkills(SkillEvidence.AllSkills, count);
    }
}
