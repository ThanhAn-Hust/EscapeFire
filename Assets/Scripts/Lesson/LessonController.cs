using TMPro;
using UnityEngine;
using UnityEngine.Events;
using EscapeFire.Core;

namespace EscapeFire.Lesson
{
    /// <summary>
    /// The lesson room (plan V1.4): stations in order, the current one highlighted; when all are done the learner can
    /// go on to the guided drill (V1.5 ladder: Lesson → Guided → Exam).
    /// </summary>
    public class LessonController : MonoBehaviour
    {
        [SerializeField] private LessonStation[] stations = new LessonStation[0];
        [SerializeField] private TMP_Text progressBoard;
        [SerializeField] private GameObject continueButton;

        public LessonStation[] Stations => stations;
        private readonly System.Collections.Generic.HashSet<LessonStation> _recorded = new System.Collections.Generic.HashSet<LessonStation>();
        private bool _sessionStarted;
        public bool AllDone { get; private set; }

        public void Configure(LessonStation[] s, TMP_Text board, GameObject next) { stations = s; progressBoard = board; continueButton = next; }

        private void Start()
        {
            if (continueButton != null) continueButton.SetActive(false);
        }

        private void Update()
        {
            int done = 0;
            bool activeSet = false;
            foreach (var s in stations)
            {
                if (s == null) continue;
                if (s.IsComplete)
                {
                    done++;
                    s.IsActive = false;
                    if (_recorded.Add(s)) RecordEvidence(s);
                    continue;
                }
                s.IsActive = !activeSet;
                activeSet = true;
            }
            if (!AllDone && done == stations.Length && stations.Length > 0)
            {
                AllDone = true;
                ProgressionStore.LessonDone = true;
                if (continueButton != null) continueButton.SetActive(true);
            }
            if (progressBoard != null)
            {
                progressBoard.text = AllDone
                    ? "<b>HOÀN THÀNH BÀI GIẢNG</b>\nẤn nút bên dưới để sang <b>Thực hành có hướng dẫn</b> trong phòng Lab."
                    : $"<b>BÀI GIẢNG PCCC</b> — đã xong {done}/{stations.Length} trạm\nĐi lần lượt theo các trạm có đèn vàng.";
            }
        }

        /// <summary>
        /// VL.1: a finished station is evidence for its rule — right on the first answer = correct (multiple choice,
        /// so the guess rate is 1/options), the hands-on practice was done either way.
        /// </summary>
        private void RecordEvidence(LessonStation s)
        {
            if (string.IsNullOrEmpty(s.RuleId)) return;
            var model = EscapeFire.Analytics.SkillStore.Load();
            if (!_sessionStarted) { model.BeginSession(); _sessionStarted = true; }
            model.Observe(s.RuleId, s.WrongAnswers == 0, 1f / System.Math.Max(2, s.OptionCount));
            EscapeFire.Analytics.SkillStore.Save(model);
        }

        /// <summary>V5.1: the lesson room always has a way back to the menu.</summary>
        public void BackToMenu()
        {
            SceneFlowManager.Load(SceneNames.MAIN_MENU);
        }

        /// <summary>Continue button: go to the guided drill.</summary>
        public void GoToGuidedDrill()
        {
            if (!AllDone) return;
            GameSession.Begin(TrainingMode.Guided, GameSession.StudentId);
            SessionResult.Reset();
            SceneFlowManager.Load(SceneNames.LAB);
        }
    }
}
