using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EscapeFire.Analytics;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>
    /// The course board in the menu: six level cards (stars or lock), a briefing for the chosen level (where, what to
    /// do, which PCCC skills it trains) and START. Shares the board with the menu and the quiz (one panel at a time).
    /// </summary>
    public class CourseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject coursePanel;
        [SerializeField] private Button[] levelButtons = new Button[6];
        [SerializeField] private TMP_Text briefText;
        [SerializeField] private GameObject startButton;
        [SerializeField] private MainMenuController menu;
        [SerializeField] private string studentId = GameSession.DEFAULT_STUDENT_ID;
        [SerializeField] private bool requireProgression = true;

        private int _selected = -1;

        public int Selected => _selected;
        public bool Open => coursePanel != null && coursePanel.activeSelf;

        public void Show()
        {
            if (menu != null && !menu.CanOpenCourse) return;
            menuPanel.SetActive(false);
            coursePanel.SetActive(true);
            int first = 0;
            for (int i = 0; i < Course.Levels.Count; i++) if (Course.Unlocked(i, requireProgression) && !Course.Passed(Course.Levels[i].Id)) { first = i; break; }
            Refresh();
            Select(first);
        }

        public void Select0() => Select(0);
        public void Select1() => Select(1);
        public void Select2() => Select(2);
        public void Select3() => Select(3);
        public void Select4() => Select(4);
        public void Select5() => Select(5);

        public void Select(int i)
        {
            if (i < 0 || i >= Course.Levels.Count) return;
            _selected = i;
            var l = Course.Levels[i];
            bool open = Course.Unlocked(i, requireProgression);
            var sb = new StringBuilder();
            sb.Append($"<b><size=120%>MÀN {i + 1}: {l.Title.ToUpperInvariant()}</size></b>\n");
            sb.Append($"<color=#B8C4D0>{l.Place} · {(l.TrainingMode == TrainingMode.Exam ? "Sát hạch, không gợi ý" : "Có hướng dẫn")}</color>\n\n");
            sb.Append(l.Goal).Append("\n\n<color=#FFD27F><b>Luyện kỹ năng:</b></color> ");
            for (int k = 0; k < l.Skills.Count; k++)
            {
                var r = PcccRuleBook.Get(l.Skills[k]);
                sb.Append(k > 0 ? " · " : "").Append(r != null ? r.Title : l.Skills[k]);
            }
            int best = Course.BestScore(l.Id);
            if (best >= 0) sb.Append($"\n\n<color=#7CFC9A>Điểm cao nhất: {best}/100 · {Course.Stars(l.Id)}/3 sao</color>");
            if (!open) sb.Append($"\n\n<color=#FF8A7A>Đang khoá: {(i == 0 ? "học BÀI GIẢNG trước" : $"qua màn {i} trước")}.</color>");
            briefText.text = sb.ToString();
            startButton.SetActive(open);
            for (int k = 0; k < levelButtons.Length; k++)
                if (levelButtons[k] != null) levelButtons[k].transform.localScale = Vector3.one * (k == i ? 1.04f : 1f);
        }

        public void StartSelected()
        {
            if (_selected < 0 || !Course.Unlocked(_selected, requireProgression)) return;
            CourseLauncher.Start(Course.Levels[_selected], studentId);
        }

        public void Back()
        {
            coursePanel.SetActive(false);
            menuPanel.SetActive(true);
            if (menu != null) menu.Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < levelButtons.Length && i < Course.Levels.Count; i++)
            {
                var l = Course.Levels[i];
                bool open = Course.Unlocked(i, requireProgression);
                int stars = Course.Stars(l.Id);
                var label = levelButtons[i].GetComponentInChildren<TMP_Text>();
                label.enableAutoSizing = true;
                label.fontSizeMax = 26f;
                label.fontSizeMin = 16f;
                label.text = $"<b>{i + 1}. {l.Title}</b>\n<size=80%>{(!open ? "<color=#AAB2BC>khoá</color>" : stars > 0 ? $"<color=#FFD27F>{stars}/3 sao</color>" : "<color=#7CFC9A>mở — chưa chơi</color>")}</size>";
                levelButtons[i].GetComponent<Image>().color = !open ? new Color(0.3f, 0.32f, 0.35f) : stars > 0 ? new Color(0.12f, 0.45f, 0.3f) : new Color(0.16f, 0.42f, 0.7f);
            }
        }
    }
}
