using UnityEngine;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// How much of the agent actually reaches the fuel (plan Phần B.1-5, PASS: Aim at the base, Squeeze, Sweep).
    /// Pure logic so it can be unit-tested and tuned. Distances/angles are initial proposals:
    /// TODO(V1): verify effective range per extinguisher type (CO2 ≈ 1–2.5 m) against NFPA 10 / TCVN sources.
    /// </summary>
    public static class FireSuppressionModel
    {
        public const float BestMinRange = 1.0f;
        public const float BestMaxRange = 2.5f;
        public const float MaxRange = 4.0f;
        public const float TooCloseFactor = 0.6f;     // < 0.5 m: agent blows burning material around
        public const float ConeHalfAngle = 15f;       // jet spread around the aim line
        public const float MissAngle = 30f;           // farther than this from the base: misses
        public const float FlamesAimFactor = 0.2f;    // hitting the flames above the base does little
        public const float BaseBand = 0.45f;          // aim point within this height above the base counts as "base"
        public const float SweepRateNeeded = 10f;     // deg/s of side-to-side motion
        public const float StaticFactor = 0.5f;       // holding still at one point covers only part of the base
        public const float MinSqueeze = 0.6f;         // trigger below this: valve not open

        public static float Range(float distance)
        {
            if (distance > MaxRange) return 0f;
            if (distance > BestMaxRange) return Mathf.InverseLerp(MaxRange, BestMaxRange, distance);
            if (distance < 0.5f) return TooCloseFactor;
            if (distance < BestMinRange) return Mathf.Lerp(TooCloseFactor, 1f, Mathf.InverseLerp(0.5f, BestMinRange, distance));
            return 1f;
        }

        /// <param name="angleToBase">Angle between the jet and the line nozzle → fire base (deg).</param>
        /// <param name="aimHeightAboveBase">Height above the base where the aim line passes the fire (m).</param>
        public static float Aim(float angleToBase, float aimHeightAboveBase)
        {
            if (angleToBase > MissAngle) return 0f;
            float cone = angleToBase <= ConeHalfAngle ? 1f : Mathf.InverseLerp(MissAngle, ConeHalfAngle, angleToBase);
            return aimHeightAboveBase > BaseBand ? cone * FlamesAimFactor : cone;
        }

        public static float Sweep(float sweepDegPerSecond) =>
            sweepDegPerSecond >= SweepRateNeeded ? 1f : Mathf.Lerp(StaticFactor, 1f, sweepDegPerSecond / SweepRateNeeded);

        public static float Squeeze(float trigger) => trigger < MinSqueeze ? 0f : Mathf.InverseLerp(MinSqueeze, 1f, trigger) * 0.5f + 0.5f;

        public static float Efficiency(float distance, float angleToBase, float aimHeightAboveBase, float sweepDegPerSecond, float trigger) =>
            Range(distance) * Aim(angleToBase, aimHeightAboveBase) * Sweep(sweepDegPerSecond) * Squeeze(trigger);
    }
}
