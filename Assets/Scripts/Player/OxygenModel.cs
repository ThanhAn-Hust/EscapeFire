using UnityEngine;

namespace EscapeFire.Player
{
    /// <summary>Pure oxygen state. Tick/Deplete return true only on the call where oxygen reaches zero.</summary>
    public sealed class OxygenModel
    {
        public float Max { get; }
        public float Current { get; private set; }
        public bool IsDepleted => Current <= 0f;

        public OxygenModel(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
        }

        public void Reset() => Current = Max;

        public bool Tick(float deltaTime, bool breathingSmoke, float depletionRate, float recoveryRate)
        {
            if (IsDepleted) return false;
            if (breathingSmoke) return Deplete(depletionRate * deltaTime);
            Recover(recoveryRate * deltaTime);
            return false;
        }

        public bool Deplete(float amount)
        {
            if (IsDepleted || amount <= 0f) return false;
            Current = Mathf.Max(0f, Current - amount);
            return IsDepleted;
        }

        public void Recover(float amount)
        {
            if (IsDepleted || amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
        }
    }
}
