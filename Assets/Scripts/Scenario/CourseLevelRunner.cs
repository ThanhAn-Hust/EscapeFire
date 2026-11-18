using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Course levels played in the lab may impose their situation (level 5: the lab lights fail as the fire breaks
    /// out, the smoke drops, stairwell B fills with smoke). The challenge goes through the same shared validator and
    /// scene checks as the AI director's (always one stairwell left, never where the learner stands).
    /// </summary>
    public class CourseLevelRunner : MonoBehaviour
    {
        [SerializeField] private float afterFireSeconds = 3f;
        private float _fireAt = -1f;
        private bool _done;

        public string Result { get; private set; } = "";

        private void Update()
        {
            if (_done) return;
            var level = Course.Current;
            var c = LabProgressiveScenarioController.Instance;
            if (level == null || !level.InLab || level.Forced == null || c == null) { _done = level != null && level.Forced == null; return; }
            if (c.CurrentPhase < ScenarioPhase.Phase2_ServerExplosionFire) return;
            if (_fireAt < 0f) _fireAt = Time.time;
            if (Time.time - _fireAt < afterFireSeconds) return;
            _done = true;
            var runner = ChallengeRunner.Instance != null ? ChallengeRunner.Instance : gameObject.AddComponent<ChallengeRunner>();
            var check = EscapeFire.Generation.ChallengeValidator.Validate(level.Forced, runner.Context(2, 0f));
            string why = check.Ok ? runner.SceneCheck(check) : check.Error;
            if (why == null) runner.Run(check);
            Result = why ?? "Accepted";
            Debug.Log($"[Course] {level.Id} situation [{string.Join("+", check.Codes)}] -> {Result}");
        }
    }
}
