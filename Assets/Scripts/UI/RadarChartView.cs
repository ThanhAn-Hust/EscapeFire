using TMPro;
using UnityEngine;
using EscapeFire.Analytics;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>5-axis skill radar drawn with LineRenderers (world space, sits next to the AAR scorecard).</summary>
    public class RadarChartView : MonoBehaviour
    {
        private static readonly string[] LABELS =
        {
            "Phát hiện rủi ro", "Chọn bình", "Khom lưng", "Xử lý dị thường", "Chịu áp lực"
        };

        [SerializeField] private float radius = 0.35f;
        [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color valueColor = new Color(0.2f, 0.75f, 1f);

        private void Start()
        {
            Draw(SessionResult.Radar);
        }

        public void Draw(SkillRadar radar)
        {
            float[] values =
            {
                radar.RiskDetection, radar.ExtinguisherSelection, radar.CrouchCompliance,
                radar.AnomalyHandling, radar.StressResilience
            };

            for (int ring = 1; ring <= 4; ring++) DrawPolygon($"Grid_{ring}", null, ring / 4f, gridColor, 0.002f);
            DrawPolygon("Values", values, 1f, valueColor, 0.006f);

            for (int i = 0; i < LABELS.Length; i++)
            {
                var go = new GameObject($"Label_{i}", typeof(TextMeshPro));
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Axis(i) * (radius * 1.25f);
                var tmp = go.GetComponent<TextMeshPro>();
                tmp.text = $"{LABELS[i]}\n{values[i] * 100f:F0}%";
                tmp.fontSize = 0.35f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.rectTransform.sizeDelta = new Vector2(0.4f, 0.12f);
            }
        }

        private void DrawPolygon(string name, float[] values, float scale, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = LABELS.Length;
            lr.widthMultiplier = width;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = color;
            for (int i = 0; i < LABELS.Length; i++)
            {
                float v = values != null ? Mathf.Clamp01(values[i]) : 1f;
                lr.SetPosition(i, Axis(i) * (radius * scale * Mathf.Max(v, 0.02f)));
            }
        }

        private static Vector3 Axis(int i)
        {
            float angle = Mathf.PI / 2f - i * 2f * Mathf.PI / LABELS.Length;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }
    }
}
