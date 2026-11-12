using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using EscapeFire.Analytics;
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
        [SerializeField] private TextMeshProUGUI debriefText;
        [SerializeField] private int maxLogLines = 12;
        [SerializeField] private int debriefTimeoutSeconds = 10;

        private void Start()
        {
            Render();
            if (SessionResult.HasResult && !string.IsNullOrEmpty(GameSession.EdgeHttpBase))
            {
                StartCoroutine(FetchGenerativeDebrief());
            }
        }

        /// <summary>Asks the edge server for an LLM-written debrief; keeps the template text on any failure.</summary>
        private IEnumerator FetchGenerativeDebrief()
        {
            var body = JsonConvert.SerializeObject(new
            {
                passed = SessionResult.Passed,
                fail_cause = SessionResult.FailCause,
                total_score = AfterActionReport.Total(SessionResult.Scorecard),
                scorecard = SessionResult.Scorecard.ConvertAll(i => new { label = i.Label, points = i.Points, max_points = i.MaxPoints, note = i.Note }),
                standing_in_smoke_seconds = SessionResult.Facts.StandingInSmokeSeconds,
                average_cognitive_friction = SessionResult.Facts.AverageCognitiveFriction,
                template_debrief = SessionResult.Debrief
            });

            using (var request = new UnityWebRequest(GameSession.EdgeHttpBase.TrimEnd('/') + "/aar/debrief", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = debriefTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success) yield break;
                string text;
                try
                {
                    text = (string)JObject.Parse(request.downloadHandler.text)["debrief"];
                }
                catch (JsonException)
                {
                    yield break;
                }
                if (string.IsNullOrWhiteSpace(text)) yield break;
                SessionResult.Debrief = text;
                SetText(debriefText, "[AI MENTOR] " + text);
            }
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
            // V5.1: the 5-axis radar is measured only in the lab drill; a generated room would show a misleading 0 %
            bool labDrill = SessionResult.DrillScene != SceneNames.GENERATED;
            foreach (var radar in FindObjectsByType<RadarChartView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                radar.gameObject.SetActive(labDrill);

            bool hasScorecard = SessionResult.Scorecard.Count > 0;
            int score = hasScorecard ? AfterActionReport.Total(SessionResult.Scorecard) : SessionResult.Score;

            var summary = new StringBuilder();
            summary.AppendLine($"TỔNG ĐIỂM: {score}{(hasScorecard ? " / 100" : "")}");
            summary.AppendLine($"Xếp loại: {GradeRules.Classify(SessionResult.Passed, score)}");
            summary.AppendLine($"Thời gian: {SessionResult.DurationSeconds:F0} giây");
            summary.AppendLine($"Chế độ: {(GameSession.Mode == TrainingMode.Guided ? "Có hướng dẫn" : "Sát hạch")}");
            if (!SessionResult.Passed && !string.IsNullOrEmpty(SessionResult.FailCause))
            {
                summary.AppendLine($"Nguyên nhân: {SessionResult.FailCause}");
            }
            if (hasScorecard)
            {
                summary.AppendLine();
                foreach (var item in SessionResult.Scorecard)
                {
                    summary.AppendLine($"{item.Label}: {item.Points}/{item.MaxPoints}  <size=70%>({item.Note})</size>");
                    // V1.6: where points were lost, say which PCCC rule and source it comes from.
                    if (item.Points < item.MaxPoints)
                    {
                        var rule = PcccRuleBook.For("score:" + item.Key);
                        if (rule != null) summary.AppendLine($"<size=60%><color=#9fd3ff>Theo: {rule.Citation}</color></size>");
                    }
                }
            }
            summary.Append(SkillSummary(SessionResult.SkillObservations, SessionResult.SkillMastery));
            SetText(summaryText, summary.ToString());
            SetText(debriefText, string.IsNullOrEmpty(SessionResult.Debrief) ? string.Empty : "[AI MENTOR] " + SessionResult.Debrief);

            var logs = new StringBuilder();
            int start = Mathf.Max(0, SessionResult.Logs.Count - maxLogLines);
            for (int i = start; i < SessionResult.Logs.Count; i++)
            {
                logs.AppendLine(SessionResult.Logs[i]);
            }
            SetText(logText, logs.ToString());
        }

        /// <summary>
        /// VL.2: the AAR shows each PCCC skill this drill tested (shown / not yet) and the learner's mastery across
        /// sessions, so the debrief is about skills, not only a total score that can be gamed.
        /// </summary>
        public static string SkillSummary(System.Collections.Generic.IReadOnlyList<SkillObservation> observations,
                                          System.Collections.Generic.IReadOnlyDictionary<string, float> mastery)
        {
            if (observations == null || observations.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("<b>KỸ NĂNG PCCC</b> <size=70%>(mức thành thạo qua các buổi)</size>");
            foreach (var o in observations)
            {
                var rule = PcccRuleBook.Get(o.Skill);
                float p = mastery != null && mastery.TryGetValue(o.Skill, out var v) ? v : 0f;
                string title = rule != null ? rule.Title : o.Skill;
                sb.AppendLine($"{(o.Correct ? "<color=#7CFC9A>ĐẠT</color>" : "<color=#FF8A80>CHƯA</color>")} {title}: {p * 100f:F0}%  <size=70%>({o.Why})</size>");
            }
            return sb.ToString();
        }

        public void Retry()
        {
            string scene = string.IsNullOrEmpty(SessionResult.DrillScene) ? SceneNames.LAB : SessionResult.DrillScene;
            GameSession.Begin(GameSession.Mode, GameSession.StudentId);
            SessionResult.Reset();
            SessionResult.DrillScene = scene;
            SceneFlowManager.Load(scene);
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
