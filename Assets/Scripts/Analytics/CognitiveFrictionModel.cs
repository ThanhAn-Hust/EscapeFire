using System;

namespace EscapeFire.Analytics
{
    public enum CognitiveState
    {
        Nominal,
        Calm,   // CF low  -> learner copes easily -> may escalate
        Panic   // CF high -> learner overwhelmed -> calm down, never escalate
    }

    /// <summary>
    /// Pure Cognitive Friction Index:
    /// CF = w1 * HeadMotion + w2 * (Hesitation / Tau) + w3 * (Mistakes / M_max), clamped to [0, 1].
    /// State changes use hysteresis so events fire once per transition (not every frame).
    /// </summary>
    public sealed class CognitiveFrictionModel
    {
        public const float PANIC_ENTER = 0.75f;
        public const float PANIC_EXIT = 0.65f;
        public const float CALM_ENTER = 0.25f;
        public const float CALM_EXIT = 0.35f;
        public const float HEAD_VELOCITY_MIN = 10f;  // deg/s considered still
        public const float HEAD_VELOCITY_MAX = 160f; // deg/s considered frantic scanning

        private readonly float _w1, _w2, _w3, _tau;
        private readonly int _maxMistakes;
        private double _cfSum;
        private int _cfSamples;

        public float Current { get; private set; }
        public float Average => _cfSamples == 0 ? 0f : (float)(_cfSum / _cfSamples);
        public CognitiveState State { get; private set; } = CognitiveState.Nominal;

        public CognitiveFrictionModel(float w1 = 0.40f, float w2 = 0.35f, float w3 = 0.25f,
            float tauSeconds = 15f, int maxMistakes = 4)
        {
            if (tauSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(tauSeconds));
            if (maxMistakes <= 0) throw new ArgumentOutOfRangeException(nameof(maxMistakes));
            float sum = w1 + w2 + w3;
            if (sum <= 0f) throw new ArgumentException("Weights must sum to a positive value.");
            _w1 = w1 / sum; _w2 = w2 / sum; _w3 = w3 / sum;
            _tau = tauSeconds;
            _maxMistakes = maxMistakes;
        }

        public static float NormalizeHeadVelocity(float degreesPerSecond)
        {
            return Clamp01((degreesPerSecond - HEAD_VELOCITY_MIN) / (HEAD_VELOCITY_MAX - HEAD_VELOCITY_MIN));
        }

        public float Compute(float headAngularVelocity, float hesitationSeconds, int mistakes)
        {
            float head = NormalizeHeadVelocity(headAngularVelocity);
            float hes = Clamp01(hesitationSeconds / _tau);
            float mis = Clamp01((float)mistakes / _maxMistakes);
            return Clamp01(_w1 * head + _w2 * hes + _w3 * mis);
        }

        /// <summary>Feeds one sample. Returns true when <see cref="State"/> changed.</summary>
        public bool Sample(float headAngularVelocity, float hesitationSeconds, int mistakes)
        {
            Current = Compute(headAngularVelocity, hesitationSeconds, mistakes);
            _cfSum += Current;
            _cfSamples++;
            return UpdateState(Current);
        }

        private bool UpdateState(float cf)
        {
            CognitiveState next = State;
            switch (State)
            {
                case CognitiveState.Panic:
                    if (cf < PANIC_EXIT) next = cf <= CALM_ENTER ? CognitiveState.Calm : CognitiveState.Nominal;
                    break;
                case CognitiveState.Calm:
                    if (cf > CALM_EXIT) next = cf >= PANIC_ENTER ? CognitiveState.Panic : CognitiveState.Nominal;
                    break;
                default:
                    if (cf >= PANIC_ENTER) next = CognitiveState.Panic;
                    else if (cf <= CALM_ENTER) next = CognitiveState.Calm;
                    break;
            }
            if (next == State) return false;
            State = next;
            return true;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
