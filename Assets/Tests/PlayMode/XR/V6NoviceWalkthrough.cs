using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Lesson;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// Review aid (Demo): what a first-time learner sees and hears. Records every text on the boards they meet and
    /// every Mentor line, with screenshots at reading distance: the menu, the lesson room and its five stations, then
    /// the guided lab drill while the learner does NOTHING for a while (do they get told what to do?).
    /// Output: Logs/Demo/&lt;time&gt;_xr/V6NoviceWalkthrough_*.png and Logs/novice_walkthrough.md.
    /// </summary>
    [Category("Demo")]
    public class V6NoviceWalkthrough : BuildingSceneTest
    {
        private readonly StringBuilder _md = new StringBuilder();
        private float _t0;

        private void OnMentor(MentorMessageEvent e) => _md.AppendLine($"- `{Time.time - _t0,5:0.0}s` **Mentor:** {e.Message}");

        private void Boards(string where)
        {
            _md.AppendLine($"\n### Chữ trên các bảng — {where}\n");
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)
                         .Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text))
                         .OrderBy(t => t.transform.position.x))
                _md.AppendLine($"- [{t.name}] {t.text.Replace("\n", " / ")}");
        }

        private IEnumerator LookFrom(Vector3 eye, Vector3 target, string shot)
        {
            PlaceRig(new Vector3(eye.x, 0f, eye.z));
            Vector3 d = target - eye;
            Body(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, eye.y, -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
            yield return Xr.Wait(0.4f);
            Shot(shot);
            _md.AppendLine($"\n![{shot}]({shot}.png)");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator FirstTimeLearner_SeesAndHears()
        {
            Application.targetFrameRate = 72;
            ProgressionStore.KeyPrefix = "EF_test_progress_";
            ProgressionStore.ResetAll();
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "NOVICE");
            EventBus<MentorMessageEvent>.Subscribe(OnMentor);
            _t0 = Time.time;
            try
            {
                // ---- the menu, as the headset starts
                yield return SceneManager.LoadSceneAsync(SceneNames.MAIN_MENU, LoadSceneMode.Single);
                yield return WaitForScene(SceneNames.MAIN_MENU);
                _md.AppendLine("## 1. Menu chính (lần đầu mở game)");
                yield return LookFrom(new Vector3(0f, 1.6f, -0.4f), new Vector3(0f, 1.45f, 1.2f), "novice_01_menu");
                Boards("menu");

                // ---- the lesson room
                yield return PressUi("1. BÀI GIẢNG");
                yield return WaitForScene(SceneNames.LESSON);
                _t0 = Time.time;
                _md.AppendLine("\n## 2. Phòng bài giảng — vừa bước vào");
                yield return Xr.Wait(3f);
                Shot("novice_02_lesson_entry");
                _md.AppendLine("\n![novice_02_lesson_entry](novice_02_lesson_entry.png)");
                Boards("phòng bài giảng");
                int i = 3;
                foreach (var st in Object.FindObjectsByType<LessonStation>(FindObjectsSortMode.None).OrderBy(s => s.name))
                {
                    var p = st.transform.position;
                    Vector3 toward = (Vector3.zero - p); toward.y = 0f;
                    toward = toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.back;
                    _md.AppendLine($"\n### Trạm {st.name}");
                    yield return LookFrom(p + toward * 1.6f + Vector3.up * 1.6f, p + Vector3.up * 1.3f, $"novice_{i++:00}_{st.name}");
                }

                // ---- the guided drill, learner stands still and does nothing
                yield return LoadLab(TrainingMode.Guided);
                _t0 = Time.time;
                _md.AppendLine("\n## 3. Diễn tập có hướng dẫn — người mới đứng yên, không làm gì");
                yield return LookFrom(new Vector3(-0.5f, 1.6f, -4.5f), new Vector3(3f, 1.4f, 2f), "novice_10_lab_start");
                Boards("phòng lab");
                for (int k = 1; k <= 5; k++)
                {
                    yield return Xr.Wait(15f);
                    var c = LabProgressiveScenarioController.Instance;
                    _md.AppendLine($"- `{Time.time - _t0,5:0.0}s` (giai đoạn: {c.CurrentPhase})");
                    Shot($"novice_{10 + k:00}_idle_{k * 15}s");
                    _md.AppendLine($"\n![novice_{10 + k:00}_idle_{k * 15}s](novice_{10 + k:00}_idle_{k * 15}s.png)");
                }
            }
            finally
            {
                EventBus<MentorMessageEvent>.Unsubscribe(OnMentor);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/novice_walkthrough.md", "# Người mới chơi lần đầu — những gì nhìn thấy và nghe thấy\n\n" + _md);
            }
        }
    }
}
