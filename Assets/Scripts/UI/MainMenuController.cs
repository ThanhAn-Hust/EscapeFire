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

            float height = headTransform.localPosition.y;
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
            _selectedMode = TrainingMode.Guided;
            RefreshStatus();
        }

        public void SelectExamMode()
        {
            _selectedMode = TrainingMode.Exam;
            RefreshStatus();
        }

        public void StartDrill()
        {
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
            SetStatus($"Chế độ: {mode}\n{height}");
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            else Debug.Log($"[MainMenu] {message}");
        }
    }
}
