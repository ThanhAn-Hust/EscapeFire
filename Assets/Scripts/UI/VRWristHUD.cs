using UnityEngine;
using TMPro;
using EscapeFire.Core;
using EscapeFire.Player;

namespace EscapeFire.UI
{
    /// <summary>
    /// Displays a 3D VR Wrist HUD on the player's left hand wrist controller.
    /// Shows Oxygen %, Health %, Crouch Status, and AI Mentor guidance.
    /// V5.2: laid out at runtime on a dark panel — oxygen | health on the top row, posture in the middle, the mentor line
    /// in its own box that auto-sizes and truncates, so long lines no longer spill over the other readouts.
    /// </summary>
    public class VRWristHUD : MonoBehaviour
    {
        public const float PanelWidth = 0.16f;
        public const float PanelHeight = 0.115f;

        [Header("UI Text References")]
        [SerializeField] private TextMeshPro oxygenText;
        [SerializeField] private TextMeshPro healthText;
        [SerializeField] private TextMeshPro crouchStatusText;
        [SerializeField] private TextMeshPro aiMentorText;

        [Header("Player System References")]
        [SerializeField] private OxygenSystem oxygenSystem;
        [SerializeField] private HealthSystem healthSystem;
        [SerializeField] private VRHeightDetector heightDetector;

        public TextMeshPro MentorText => aiMentorText;

        private void Awake() => Layout();

        private void Start()
        {
            if (oxygenSystem == null) oxygenSystem = FindFirstObjectByType<OxygenSystem>();
            if (healthSystem == null) healthSystem = FindFirstObjectByType<HealthSystem>();
            if (heightDetector == null) heightDetector = FindFirstObjectByType<VRHeightDetector>();
        }

        /// <summary>Positions every readout in its own non-overlapping box (local units = metres).</summary>
        public void Layout()
        {
            if (transform.Find("Panel") == null)
            {
                var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
                panel.name = "Panel";
                Destroy(panel.GetComponent<Collider>());
                panel.transform.SetParent(transform, false);
                panel.transform.localPosition = new Vector3(0f, 0f, 0.002f); // just behind the text
                panel.transform.localScale = new Vector3(PanelWidth, PanelHeight, 1f);
                var r = panel.GetComponent<Renderer>();
                r.sharedMaterial = RuntimeVisuals.ParticleMaterial(new Color(0.02f, 0.04f, 0.07f, 0.8f));
            }
            float top = PanelHeight / 2f, inner = PanelWidth - 0.01f;
            Box(oxygenText, new Vector2(-inner / 4f, top - 0.012f), new Vector2(inner / 2f, 0.018f), TextAlignmentOptions.Left, 0.12f, 0.2f);
            Box(healthText, new Vector2(inner / 4f, top - 0.012f), new Vector2(inner / 2f, 0.018f), TextAlignmentOptions.Right, 0.12f, 0.2f);
            Box(crouchStatusText, new Vector2(0f, top - 0.032f), new Vector2(inner, 0.016f), TextAlignmentOptions.Center, 0.08f, 0.14f);
            Box(aiMentorText, new Vector2(0f, -0.016f), new Vector2(inner, 0.07f), TextAlignmentOptions.TopLeft, 0.05f, 0.11f);
            if (aiMentorText != null) aiMentorText.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static void Box(TextMeshPro t, Vector2 centre, Vector2 size, TextAlignmentOptions align, float minSize, float maxSize)
        {
            if (t == null) return;
            var rt = t.rectTransform;
            rt.localPosition = new Vector3(centre.x, centre.y, 0f);
            rt.localRotation = Quaternion.identity;
            rt.sizeDelta = size;
            t.alignment = align;
            t.enableAutoSizing = true;
            t.fontSizeMin = minSize;
            t.fontSizeMax = maxSize;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Truncate;
            t.margin = Vector4.zero;
        }

        private void OnEnable() => EventBus<MentorMessageEvent>.Subscribe(HandleMentorMessage);

        private void OnDisable() => EventBus<MentorMessageEvent>.Unsubscribe(HandleMentorMessage);

        private void HandleMentorMessage(MentorMessageEvent e) => UpdateAIMentorText(e.Message);

        private void Update()
        {
            if (oxygenSystem != null && oxygenText != null)
            {
                oxygenText.text = $"OXY {oxygenSystem.CurrentOxygen:F0}%";
                oxygenText.color = oxygenSystem.CurrentOxygen < 30f ? Color.red : Color.cyan;
            }

            if (healthSystem != null && healthText != null)
            {
                healthText.text = $"MÁU {healthSystem.CurrentHealth:F0}%";
                healthText.color = healthSystem.CurrentHealth < 30f ? Color.red : Color.green;
            }

            if (heightDetector != null && crouchStatusText != null)
            {
                if (heightDetector.IsCrouching)
                {
                    crouchStatusText.text = "TƯ THẾ: KHOM — AN TOÀN";
                    crouchStatusText.color = Color.green;
                }
                else
                {
                    crouchStatusText.text = "TƯ THẾ: ĐỨNG — NGUY HIỂM KHÓI";
                    crouchStatusText.color = Color.yellow;
                }
            }
        }

        private void UpdateAIMentorText(string message)
        {
            if (aiMentorText != null)
            {
                aiMentorText.text = message;
            }
        }
    }
}
