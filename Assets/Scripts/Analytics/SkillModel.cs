using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace EscapeFire.Analytics
{
    /// <summary>What we know about one PCCC skill (one rule R01–R12) for one learner.</summary>
    public sealed class SkillState
    {
        public float PKnown;
        public int Observed;
        public int Correct;
        public int LastSession = -1;
        /// <summary>Correct sessions in a row: the spacing interval doubles with it (1, 2, 4… sessions).</summary>
        public int Streak;
        public bool LastCorrect;
    }

    /// <summary>
    /// Plan VL.1 (advisor 12/11: know which knowledge is still missing, not only how hard the game is).
    /// Bayesian Knowledge Tracing per skill (Corbett &amp; Anderson 1995) with the textbook default parameters — an
    /// assumption to calibrate with pilot data. Every drill / lesson / quiz adds observations (correct / wrong); the
    /// model gives P(known) per skill and the skills to practise next: unmastered ones first (lowest P), then
    /// mastered ones whose spaced review is due. Pure logic, serialisable, testable outside Unity.
    /// </summary>
    public sealed class SkillModel
    {
        public const float PInit = 0.2f;
        public const float PLearn = 0.15f;
        public const float PGuess = 0.2f;
        public const float PSlip = 0.1f;
        public const float Mastery = 0.85f;

        [JsonProperty] public int Session { get; private set; }
        [JsonProperty] public Dictionary<string, SkillState> Skills { get; private set; } = new Dictionary<string, SkillState>();

        public void BeginSession() => Session++;

        public SkillState Get(string skill)
        {
            if (!Skills.TryGetValue(skill, out var s))
            {
                s = new SkillState { PKnown = PInit };
                Skills[skill] = s;
            }
            return s;
        }

        public float Known(string skill) => Skills.TryGetValue(skill, out var s) ? s.PKnown : PInit;
        public bool IsMastered(string skill) => Known(skill) >= Mastery;

        /// <summary>BKT update for one observation; <paramref name="guess"/> is 1/options for multiple choice.</summary>
        public void Observe(string skill, bool correct, float guess = PGuess, float slip = PSlip)
        {
            var s = Get(skill);
            float p = s.PKnown;
            float posterior = correct
                ? p * (1f - slip) / (p * (1f - slip) + (1f - p) * guess)
                : p * slip / (p * slip + (1f - p) * (1f - guess));
            s.PKnown = Clamp(posterior + (1f - posterior) * PLearn);
            s.Observed++;
            if (correct) s.Correct++;
            // the streak counts sessions, not observations
            if (s.LastSession != Session) s.Streak = correct ? s.Streak + 1 : 0;
            else if (!correct) s.Streak = 0;
            s.LastSession = Session;
            s.LastCorrect = correct;
        }

        /// <summary>Spaced review: a mastered skill is due again after 2^(streak-1) sessions (1, 2, 4, 8, cap 8).</summary>
        public bool ReviewDue(string skill)
        {
            if (!Skills.TryGetValue(skill, out var s) || s.LastSession < 0) return false;
            int interval = Math.Min(8, 1 << Math.Max(0, s.Streak - 1));
            return Session - s.LastSession >= interval;
        }

        /// <summary>
        /// Skills to practise next, most needed first: never-seen and unmastered skills by ascending P(known), then
        /// mastered skills whose review is due (oldest first). <paramref name="universe"/> = all skill ids.
        /// </summary>
        public List<string> WeakSkills(IEnumerable<string> universe, int count)
        {
            var all = universe.ToList();
            var unmastered = all.Where(k => !IsMastered(k)).OrderBy(Known).ThenBy(k => k, StringComparer.Ordinal);
            var due = all.Where(k => IsMastered(k) && ReviewDue(k)).OrderBy(k => Skills[k].LastSession).ThenBy(k => k, StringComparer.Ordinal);
            return unmastered.Concat(due).Take(count).ToList();
        }

        public string ToJson() => JsonConvert.SerializeObject(this);

        public static SkillModel FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new SkillModel();
            try { return JsonConvert.DeserializeObject<SkillModel>(json) ?? new SkillModel(); }
            catch (JsonException) { return new SkillModel(); }
        }

        private static float Clamp(float v) => v < 0.001f ? 0.001f : (v > 0.999f ? 0.999f : v);
    }
}
