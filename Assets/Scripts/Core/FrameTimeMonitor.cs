using UnityEngine;

namespace EscapeFire.Core
{
    /// <summary>
    /// Measures frame time (avg, 95th percentile, % frames over the 13.8 ms budget) over the session for the
    /// thesis performance chapter. Allocation-free ring buffer; values exposed for the report / debug HUD.
    /// </summary>
    public class FrameTimeMonitor : MonoBehaviour
    {
        public const float BUDGET_MS = 1000f / 72f;
        private const int WINDOW = 720; // 10 s at 72 Hz

        private readonly float[] _samples = new float[WINDOW];
        private readonly float[] _sorted = new float[WINDOW];
        private int _count;
        private int _index;
        private long _totalFrames;
        private long _overBudgetFrames;
        private double _sumMs;

        public static FrameTimeMonitor Instance { get; private set; }

        public float AverageMs => _totalFrames == 0 ? 0f : (float)(_sumMs / _totalFrames);
        public float OverBudgetPercent => _totalFrames == 0 ? 0f : 100f * _overBudgetFrames / _totalFrames;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms <= 0f || ms > 1000f) return; // skip pauses / scene loads
            _samples[_index] = ms;
            _index = (_index + 1) % WINDOW;
            if (_count < WINDOW) _count++;
            _totalFrames++;
            _sumMs += ms;
            if (ms > BUDGET_MS) _overBudgetFrames++;
        }

        /// <summary>95th percentile of the last 10 s. Sorts a copy — call occasionally (e.g. once per second).</summary>
        public float Percentile95Ms()
        {
            if (_count == 0) return 0f;
            System.Array.Copy(_samples, _sorted, _count);
            System.Array.Sort(_sorted, 0, _count);
            return _sorted[Mathf.Clamp(Mathf.CeilToInt(_count * 0.95f) - 1, 0, _count - 1)];
        }
    }
}
