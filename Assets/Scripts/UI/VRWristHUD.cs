using UnityEngine;
using TMPro;
using EscapeFire.Player;
using EscapeFire.Scenario;

namespace EscapeFire.UI
{
    /// <summary>
    /// Displays a 3D VR Wrist HUD on the player's left hand wrist controller.
    /// Shows Oxygen %, Health %, Crouch Status, and AI Mentor guidance.
    /// </summary>
    public class VRWristHUD : MonoBehaviour
    {
        [Header("UI Text References")]
        [SerializeField] private TextMeshPro oxygenText;
        [SerializeField] private TextMeshPro healthText;
        [SerializeField] private TextMeshPro crouchStatusText;
        [SerializeField] private TextMeshPro aiMentorText;

        [Header("Player System References")]
        [SerializeField] private OxygenSystem oxygenSystem;
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private VRHeightDetector heightDetector;

        private void Start()
        {
            if (oxygenSystem == null) oxygenSystem = FindObjectOfType<OxygenSystem>();
            if (healthSystem == null) healthSystem = FindObjectOfType<HealthSystem>();
            if (heightDetector == null) heightDetector = FindObjectOfType<VRHeightDetector>();

            if (LabProgressiveScenarioController.Instance != null)
            {
                LabProgressiveScenarioController.Instance.OnAIMentorMessage += UpdateAIMentorText;
            }
        }

        private void Update()
        {
            if (oxygenSystem != null && oxygenText != null)
            {
                oxygenText.text = $"OXY: {oxygenSystem.CurrentOxygen:F0}%";
                oxygenText.color = oxygenSystem.CurrentOxygen < 30f ? Color.red : Color.cyan;
            }

            if (healthSystem != null && healthText != null)
            {
                healthText.text = $"MÁU: {healthSystem.CurrentHealth:F0}%";
                healthText.color = healthSystem.CurrentHealth < 30f ? Color.red : Color.green;
            }

            if (heightDetector != null && crouchStatusText != null)
            {
                if (heightDetector.IsCrouching)
                {
                    crouchStatusText.text = "TƯ THẾ: KHOM NGƯỜI (AN TOÀN)";
                    crouchStatusText.color = Color.green;
                }
                else
                {
                    crouchStatusText.text = "TƯ THẾ: ĐỨNG THẲNG (NGUY HIỂM KHÓI)";
                    crouchStatusText.color = Color.yellow;
                }
            }
        }

        private void UpdateAIMentorText(string message)
        {
            if (aiMentorText != null)
            {
                aiMentorText.text = $"[AI MENTOR]: {message}";
            }
        }
    }
}
