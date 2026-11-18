using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EscapeFire.Analytics;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>
    /// Final knowledge quiz (one question per PCCC rule R01–R12, each from Resources/PcccRules.json with its source).
    /// Questions and options are shuffled every time (retrieval, not recognising the station's layout). After each
    /// answer: right / wrong, why, and the source. At the end: the score, the rules still wrong with where to practise
    /// them, and every answer is recorded in the learner's skill model, so the next drill targets what is still weak.
    /// Passing = at least <see cref="PassMark"/> correct.
    /// </summary>
    public class FinalQuizController : MonoBehaviour
    {
        public const int PassMark = 10;

        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button[] optionButtons = new Button[3];
        [SerializeField] private GameObject nextButton;
        [SerializeField] private MainMenuController menu;

        private readonly List<PcccRule> _items = new List<PcccRule>();
        private readonly List<SkillObservation> _answers = new List<SkillObservation>();
        private int[] _order = new int[0]; // displayed option -> original option index
        private int _index = -1;
        private bool _answered;
        private int _correct;

        public bool Running => _index >= 0 && _index < _items.Count;
        public bool Finished { get; private set; }
        public int Correct => _correct;
        public int Count => _items.Count;
        /// <summary>Which displayed option (0..2) is right for the current question.</summary>
        public int CurrentCorrectOption => Running ? System.Array.IndexOf(_order, _items[_index].Quiz.Answer) : -1;

        public void Begin()
        {
            if (menu != null && !menu.CanTakeQuiz) return;
            _items.Clear();
            foreach (var r in PcccRuleBook.Rules) if (r.Quiz != null && r.Quiz.Options.Count >= 2) _items.Add(r);
            Shuffle(_items);
            _answers.Clear();
            _correct = 0;
            _index = -1;
            Finished = false;
            menuPanel.SetActive(false);
            quizPanel.SetActive(true);
            Next();
        }

        public void Choose0() => Choose(0);
        public void Choose1() => Choose(1);
        public void Choose2() => Choose(2);

        public void Choose(int displayed)
        {
            if (!Running || _answered || displayed >= _order.Length) return;
            _answered = true;
            var rule = _items[_index];
            bool right = _order[displayed] == rule.Quiz.Answer;
            if (right) _correct++;
            _answers.Add(new SkillObservation { Skill = rule.Id, Correct = right, Why = "trắc nghiệm tổng" });
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null || !optionButtons[i].gameObject.activeSelf) continue;
                Color c = i == CurrentCorrectOption ? new Color(0.12f, 0.55f, 0.25f) : i == displayed ? new Color(0.7f, 0.15f, 0.12f) : new Color(0.25f, 0.27f, 0.3f);
                optionButtons[i].GetComponent<Image>().color = c;
            }
            string source = rule.Sources.Count > 0 ? rule.Sources[0].Name : "";
            feedbackText.text = (right ? "<color=#7CFC9A><b>ĐÚNG.</b></color> " : "<color=#FF8A7A><b>CHƯA ĐÚNG.</b></color> ") + rule.Quiz.Why +
                                (source.Length > 0 ? $"\n<size=80%><color=#B8C4D0>Nguồn: {source}</color></size>" : "");
            nextButton.SetActive(true);
        }

        public void Next()
        {
            if (_index >= 0 && !_answered) return; // answer first
            _index++;
            _answered = false;
            nextButton.SetActive(false);
            if (_index >= _items.Count) { Finish(); return; }
            var q = _items[_index].Quiz;
            progressText.text = $"TRẮC NGHIỆM TỔNG · Câu {_index + 1}/{_items.Count} · Đúng {_correct}";
            questionText.text = q.Question;
            feedbackText.text = "Chạm đầu ngón tay vào phương án bạn chọn.";
            _order = new int[q.Options.Count];
            for (int i = 0; i < _order.Length; i++) _order[i] = i;
            Shuffle(_order);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool shown = i < _order.Length;
                optionButtons[i].gameObject.SetActive(shown);
                if (!shown) continue;
                optionButtons[i].GetComponent<Image>().color = new Color(0.16f, 0.42f, 0.70f);
                var label = optionButtons[i].GetComponentInChildren<TMP_Text>();
                label.enableAutoSizing = true;
                label.fontSizeMax = 30f;
                label.fontSizeMin = 18f;
                label.text = $"{(char)('A' + i)}. {q.Options[_order[i]]}";
            }
        }

        private void Finish()
        {
            Finished = true;
            foreach (var b in optionButtons) b.gameObject.SetActive(false);
            SkillStore.Record(_answers); // the drill director now knows which rules are still weak
            if (_correct > ProgressionStore.QuizBest) ProgressionStore.QuizBest = _correct;
            bool passed = _correct >= PassMark;
            progressText.text = $"KẾT QUẢ TRẮC NGHIỆM TỔNG: {_correct}/{_items.Count}";
            questionText.text = passed
                ? "<color=#7CFC9A><b>ĐẠT</b></color> — bạn nắm được các quy tắc PCCC cơ bản."
                : $"<color=#FF8A7A><b>CHƯA ĐẠT</b></color> — cần ít nhất {PassMark}/{_items.Count}. Ôn lại rồi làm lại.";
            var sb = new StringBuilder();
            var wrong = _answers.FindAll(a => !a.Correct);
            if (wrong.Count == 0) sb.Append("Không sai câu nào. Hãy thử sức ở chế độ SÁT HẠCH.");
            else
            {
                sb.AppendLine("<b>Cần ôn:</b>");
                foreach (var a in wrong)
                {
                    var r = PcccRuleBook.Get(a.Skill);
                    sb.AppendLine($"• {r?.Title ?? a.Skill} — {WhereToPractise(a.Skill)}");
                }
                sb.Append("<size=85%>Lần diễn tập tới, AI sẽ ưu tiên tình huống luyện các mục này.</size>");
            }
            feedbackText.text = sb.ToString();
            nextButton.SetActive(false);
        }

        /// <summary>Where the rule is practised in the game.</summary>
        private static string WhereToPractise(string ruleId)
        {
            switch (ruleId)
            {
                case "R12_FIRE_TRIANGLE": return "Bài giảng, trạm 1";
                case "R03_RIGHT_AGENT": return "Bài giảng, trạm 2";
                case "R04_PASS": case "R05_CO2_CHARGE": return "Bài giảng, trạm 3 + diễn tập";
                case "R01_ALARM_EARLY": return "Bài giảng, trạm 4 + diễn tập";
                case "R07_STAY_LOW": return "Bài giảng, trạm 5 + diễn tập";
                default: return "diễn tập phòng Lab";
            }
        }

        public void BackToMenu()
        {
            _index = -1;
            quizPanel.SetActive(false);
            menuPanel.SetActive(true);
            if (menu != null) menu.Refresh();
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
