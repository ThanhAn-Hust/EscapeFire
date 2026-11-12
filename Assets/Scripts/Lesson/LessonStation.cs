using TMPro;
using UnityEngine;
using EscapeFire.Analytics;

namespace EscapeFire.Lesson
{
    /// <summary>Hands-on part of a lesson station: done when the learner has physically done the thing.</summary>
    public interface IPracticeCheck
    {
        bool Done { get; }
        /// <summary>What to do, shown on the board.</summary>
        string Instruction { get; }
        /// <summary>Latest corrective feedback (e.g. "wrong extinguisher"), empty if none.</summary>
        string Feedback { get; }
    }

    /// <summary>
    /// One lesson station (plan V1.4): a board explaining one PCCC rule with its source, a hands-on practice done with
    /// the VR hands, then a quick question answered by pressing a button with a fingertip. Practice comes first.
    /// </summary>
    public class LessonStation : MonoBehaviour
    {
        [SerializeField] private string title;
        [TextArea] [SerializeField] private string explanation;
        [SerializeField] private string ruleId;
        [SerializeField] private string question;
        [SerializeField] private string[] options = new string[0];
        [SerializeField] private int correctOption;
        [TextArea] [SerializeField] private string whyCorrect;
        [SerializeField] private MonoBehaviour practice;
        [SerializeField] private TMP_Text board;
        [SerializeField] private TMP_Text feedback;
        [SerializeField] private Renderer lamp;

        private string _feedback = "";
        private Color _feedbackColor = Color.white;

        public bool PracticeDone { get; private set; }
        public bool QuizDone { get; private set; }
        public bool IsComplete => PracticeDone && QuizDone;
        public bool IsActive { get; set; }
        public int WrongAnswers { get; private set; }
        public string Title => title;
        public string RuleId => ruleId;
        public int OptionCount => options != null ? options.Length : 0;
        public IPracticeCheck Practice => practice as IPracticeCheck;

        public void Configure(string stationTitle, string text, string rule, string q, string[] opts, int correct, string why,
            MonoBehaviour practiceCheck, TMP_Text boardText, TMP_Text feedbackText, Renderer statusLamp)
        {
            title = stationTitle; explanation = text; ruleId = rule; question = q; options = opts; correctOption = correct;
            whyCorrect = why; practice = practiceCheck; board = boardText; feedback = feedbackText; lamp = statusLamp;
        }

        private void Start() => Refresh();

        private void Update()
        {
            var p = Practice;
            if (p != null && !PracticeDone)
            {
                if (p.Done)
                {
                    PracticeDone = true;
                    Say("Đã làm đúng! Giờ trả lời câu hỏi bên dưới bằng cách ấn nút.", new Color(0.5f, 1f, 0.6f));
                    Refresh();
                }
                else if (!string.IsNullOrEmpty(p.Feedback) && p.Feedback != _feedback) Say(p.Feedback, new Color(1f, 0.6f, 0.4f));
            }
            if (lamp != null)
            {
                Color c = IsComplete ? new Color(0.2f, 0.9f, 0.3f) : IsActive ? new Color(1f, 0.8f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);
                if (lamp.material.color != c) lamp.material.color = c;
            }
        }

        /// <summary>Pressed answer button <paramref name="index"/>.</summary>
        public void Answer(int index)
        {
            if (QuizDone) return;
            if (!PracticeDone)
            {
                Say("Hãy thực hành trước, rồi mới trả lời.", new Color(1f, 0.8f, 0.4f));
                return;
            }
            if (index == correctOption)
            {
                QuizDone = true;
                Say("Chính xác! " + whyCorrect, new Color(0.5f, 1f, 0.6f));
            }
            else
            {
                WrongAnswers++;
                Say("Chưa đúng. " + whyCorrect, new Color(1f, 0.55f, 0.45f));
            }
            Refresh();
        }

        private void Say(string text, Color color)
        {
            _feedback = text;
            _feedbackColor = color;
            if (feedback != null) { feedback.text = text; feedback.color = color; }
        }

        private void Refresh()
        {
            if (board == null) return;
            var rule = string.IsNullOrEmpty(ruleId) ? null : PcccRuleBook.Get(ruleId);
            var sb = new System.Text.StringBuilder();
            sb.Append("<b><size=130%>").Append(title).Append("</size></b>\n\n");
            sb.Append(explanation).Append('\n');
            if (rule != null) sb.Append("<size=70%><color=#9fd3ff>Nguồn: ").Append(rule.Sources.Count > 0 ? rule.Sources[0].Name : rule.Title).Append("</color></size>\n");
            sb.Append('\n');
            var p = Practice;
            if (!PracticeDone) sb.Append("<color=#ffd27f><b>Thực hành:</b></color> ").Append(p != null ? p.Instruction : "").Append('\n');
            else if (!QuizDone)
            {
                sb.Append("<color=#ffd27f><b>Câu hỏi:</b></color> ").Append(question).Append('\n');
                for (int i = 0; i < options.Length; i++) sb.Append("<b>").Append((char)('A' + i)).Append(".</b> ").Append(options[i]).Append('\n');
            }
            else sb.Append("<color=#7CFC9A><b>Hoàn thành trạm này ✔</b></color>");
            board.text = sb.ToString();
        }
    }
}
