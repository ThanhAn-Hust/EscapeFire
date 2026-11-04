using System.Text;
using TMPro;
using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>
    /// Scene 02_AAR_Evaluation: shows the scorecard of the last drill and offers Retry / Main Menu.
    /// </summary>
    public class AARController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI headlineText;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private int maxLogLines = 12;

        private void Start()
        {
            Render();
        }

        public void Render()
        {
            if (!SessionResult.HasResult)
            {
                SetText(headlineText, "CHƯA CÓ KẾT QUẢ");
                SetText(summaryText, "Hãy hoàn thành một buổi diễn tập trước.");
                SetText(logText, string.Empty);
                return;
            }

            SetText(headlineText, SessionResult.Passed ? "THOÁT HIỂM AN TOÀN" : "DIỄN TẬP THẤT BẠI");

            var summary = new StringBuilder();
            summary.AppendLine($"Tổng điểm: {SessionResult.Score}");
            summary.AppendLine($"Xếp loại: {GradeRules.Classify(SessionResult.Passed, SessionResult.Score)}");
            summary.AppendLine($"Thời gian: {SessionResult.DurationSeconds:F0} giây");
            summary.AppendLine($"Chế độ: {(GameSession.Mode == TrainingMode.Guided ? "Có hướng dẫn" : "Sát hạch")}");
            if (!SessionResult.Passed && !string.IsNullOrEmpty(SessionResult.FailCause))
            {
                summary.AppendLine($"Nguyên nhân: {SessionResult.FailCause}");
            }
            SetText(summaryText, summary.ToString());

            var logs = new StringBuilder();
            int start = Mathf.Max(0, SessionResult.Logs.Count - maxLogLines);
            for (int i = start; i < SessionResult.Logs.Count; i++)
            {
                logs.AppendLine(SessionResult.Logs[i]);
            }
            SetText(logText, logs.ToString());
        }

        public void Retry()
        {
            GameSession.Begin(GameSession.Mode, GameSession.StudentId);
            SessionResult.Reset();
            SceneFlowManager.Load(SceneNames.LAB);
        }

        public void BackToMenu()
        {
            SceneFlowManager.Load(SceneNames.MAIN_MENU);
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}
