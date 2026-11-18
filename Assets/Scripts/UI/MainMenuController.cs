using System;
using TMPro;
using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>
    /// Scene 00_MainMenu: height calibration (H₀), training mode choice, start drill.
    /// Public methods are meant to be wired to world-space UI Button.onClick.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Transform headTransform;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private string studentId = GameSession.DEFAULT_STUDENT_ID;
        [Tooltip("Lesson -> Guided -> Exam ladder (plan V1.5). Turn off for instructor demos.")]
        [SerializeField] private bool requireProgression = true;

        private TrainingMode _selectedMode = TrainingMode.Guided;

        private void Start()
        {
            if (headTransform == null && Camera.main != null) headTransform = Camera.main.transform;
            RefreshStatus();
        }

        public void CalibrateHeight()
        {
            if (headTransform == null)
            {
                SetStatus("Không tìm thấy kính VR (Main Camera).");
                return;
            }

            // head above the rig's floor: correct whether the camera offset carries a height (Device mode) or not (Floor)
            var origin = headTransform.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            float height = origin != null ? headTransform.position.y - origin.transform.position.y : headTransform.localPosition.y;
            try
            {
                GameSession.SetCalibratedHeight(height);
                RefreshStatus();
            }
            catch (ArgumentOutOfRangeException)
            {
                SetStatus($"Chiều cao {height:F2}m không hợp lệ. Hãy đứng thẳng tự nhiên rồi bấm lại.");
            }
        }

        public void SelectGuidedMode()
        {
            if (requireProgression && !ProgressionStore.GuidedUnlocked)
            {
                SetStatus("Hãy học BÀI GIẢNG trước (5 trạm), rồi mới thực hành có hướng dẫn.");
                return;
            }
            _selectedMode = TrainingMode.Guided;
            RefreshStatus();
        }

        public void SelectExamMode()
        {
            if (requireProgression && !ProgressionStore.ExamUnlocked)
            {
                SetStatus("SÁT HẠCH mở khi bạn đã xong Bài giảng và vượt qua 1 lần Có hướng dẫn.");
                return;
            }
            _selectedMode = TrainingMode.Exam;
            RefreshStatus();
        }

        /// <summary>Lesson room first (advisor: "bài giảng trước, sau đó thực hành").</summary>
        public void StartLesson()
        {
            GameSession.Begin(TrainingMode.Guided, studentId);
            SessionResult.Reset();
            SceneFlowManager.Load(SceneNames.LESSON);
        }

        private static readonly string[] GeneratedRooms = { "library", "canteen", "office", "workshop", "lab" };

        /// <summary>
        /// V5.1: a room built from a ScenarioSpec (V4). Each press opens the next sample room with a new arrangement
        /// (seed); an instructor with the edge server can hand in a spec from a prompt instead (GeneratedScenario.Request).
        /// </summary>
        public void StartGeneratedRoom()
        {
            if (requireProgression && !ProgressionStore.GuidedUnlocked)
            {
                SetStatus("Hãy học BÀI GIẢNG trước khi vào phòng mới.");
                return;
            }
            int index = PlayerPrefs.GetInt(ProgressionStore.KeyPrefix + "generated_index", 0);
            PlayerPrefs.SetInt(ProgressionStore.KeyPrefix + "generated_index", index + 1);
            string room = GeneratedRooms[index % GeneratedRooms.Length];
            var sample = Resources.Load<TextAsset>("Scenarios/" + room);
            if (sample == null)
            {
                SetStatus("Không tìm thấy phòng mẫu " + room + ".");
                return;
            }
            EscapeFire.Generation.GeneratedScenario.Pending = Newtonsoft.Json.Linq.JObject.Parse(sample.text);
            EscapeFire.Generation.GeneratedScenario.Seed = UnityEngine.Random.Range(1, 10000);
            GameSession.Begin(TrainingMode.Guided, studentId);
            SessionResult.Reset();
            SceneFlowManager.Load(SceneNames.GENERATED);
        }

        public void StartDrill()
        {
            if (requireProgression && !ProgressionStore.GuidedUnlocked)
            {
                SetStatus("Hãy học BÀI GIẢNG trước khi vào phòng Lab.");
                return;
            }
            if (requireProgression && _selectedMode == TrainingMode.Exam && !ProgressionStore.ExamUnlocked) _selectedMode = TrainingMode.Guided;
            GameSession.Begin(_selectedMode, studentId);
            SessionResult.Reset();
            SceneFlowManager.Load(SceneNames.LAB);
        }

        /// <summary>The final quiz opens once the lesson is done (it tests what the lesson taught).</summary>
        public bool CanTakeQuiz
        {
            get
            {
                if (!requireProgression || ProgressionStore.LessonDone) return true;
                SetStatus("Mục này đang khoá: hãy học <b>1. BÀI GIẢNG</b> trước khi làm trắc nghiệm tổng.");
                return false;
            }
        }

        public void Refresh() => RefreshStatus();

        /// <summary>The course opens once the lesson is done.</summary>
        public bool CanOpenCourse
        {
            get
            {
                if (!requireProgression || ProgressionStore.LessonDone) return true;
                SetStatus("Mục này đang khoá: hãy học <b>1. BÀI GIẢNG</b> trước khi vào khoá học.");
                return false;
            }
        }

        private void RefreshStatus()
        {
            // two short lines a first-time learner can act on: where they are on the path, and what to do next
            int quiz = ProgressionStore.QuizBest, passed = Course.PassedCount(), levels = Course.Levels.Count;
            string lesson = ProgressionStore.LessonDone ? "<color=#7CFC9A>xong</color>" : "<color=#FFD27F>mở</color>";
            string course = !ProgressionStore.LessonDone ? "<color=#8C96A0>khoá</color>" : passed == levels ? "<color=#7CFC9A>xong</color>" : $"<color=#FFD27F>{passed}/{levels} màn</color>";
            string quizS = quiz >= 0 ? $"<color=#7CFC9A>{quiz}/12</color>" : ProgressionStore.LessonDone ? "<color=#FFD27F>mở</color>" : "<color=#8C96A0>khoá</color>";
            string path = $"1. Bài giảng {lesson} → 2. Khoá học {course} → 3. Trắc nghiệm {quizS}";
            string next;
            if (!ProgressionStore.LessonDone) next = "Lần đầu? Bấm <b>1. BÀI GIẢNG</b>: học từng trạm, làm thử bằng tay, rồi trả lời câu hỏi.";
            else if (passed < levels) next = $"Tiếp theo: <b>2. KHOÁ HỌC</b> — màn {passed + 1}: {Course.Levels[passed].Title}.";
            else if (quiz < FinalQuizController.PassMark) next = "Tiếp theo: <b>3. TRẮC NGHIỆM TỔNG</b>.";
            else next = "Bạn đã hoàn thành khoá học. Thử <b>4. THỬ THÁCH PHÒNG LẠ</b> để luyện trong căn phòng chưa gặp.";
            if (!GameSession.IsCalibrated) next += " <color=#B8C4D0>(Đứng thẳng, bấm XÁC NHẬN CHIỀU CAO trước.)</color>";
            SetStatus($"<size=88%>{path}</size>\n{next}");
            UpdateLocks();
        }

        /// <summary>Locked buttons look locked (grey); pressing one still explains why it is locked.</summary>
        private void UpdateLocks()
        {
            foreach (var b in GetComponentsInParentScene<UnityEngine.UI.Button>())
            {
                bool? open = null;
                string n = b.name;
                if (n.Contains("2. KHOÁ HỌC") || n.Contains("3. TRẮC NGHIỆM") || n.Contains("4. THỬ THÁCH")) open = !requireProgression || ProgressionStore.LessonDone;
                if (open == null) continue;
                var img = b.GetComponent<UnityEngine.UI.Image>();
                if (!_colors.ContainsKey(b)) _colors[b] = img.color;
                img.color = open.Value ? _colors[b] : new Color(0.32f, 0.34f, 0.37f);
                var label = b.GetComponentInChildren<TMP_Text>();
                if (label != null) label.alpha = open.Value ? 1f : 0.55f;
            }
        }

        private readonly System.Collections.Generic.Dictionary<UnityEngine.UI.Button, Color> _colors = new System.Collections.Generic.Dictionary<UnityEngine.UI.Button, Color>();

        private static T[] GetComponentsInParentScene<T>() where T : Component => FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            else Debug.Log($"[MainMenu] {message}");
        }
    }
}
