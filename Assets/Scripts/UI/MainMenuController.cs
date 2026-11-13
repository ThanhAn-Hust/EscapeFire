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

        private void RefreshStatus()
        {
            string mode = _selectedMode == TrainingMode.Guided ? "Huấn luyện có Hướng dẫn" : "Sát hạch Thực chiến";
            string height = GameSession.IsCalibrated
                ? $"H₀ = {GameSession.CalibratedHeight:F2}m (khom khi < {GameSession.CalibratedHeight * 0.7f:F2}m)"
                : "Chưa hiệu chuẩn chiều cao";
            // plain words: the Vietnamese font has no check-mark / circle glyphs (they showed as blanks)
            string ladder = $"Bài giảng: {(ProgressionStore.LessonDone ? "xong" : "chưa")}  ·  Có hướng dẫn: {(ProgressionStore.GuidedPassed ? "đạt" : "chưa")}  ·  Sát hạch: {(ProgressionStore.ExamUnlocked ? "mở" : "khoá")}";
            SetStatus($"Chế độ: {mode}\n{height}\n{ladder}");
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            else Debug.Log($"[MainMenu] {message}");
        }
    }
}
