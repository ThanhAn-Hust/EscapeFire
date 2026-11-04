namespace EscapeFire.Player
{
    public static class CrouchRules
    {
        /// <summary>Spec SCENE_FLOW §2.1: crouching means head height &lt; 0.7 × H₀.</summary>
        public const float CROUCH_RATIO = 0.7f;

        public static float ComputeThreshold(float calibratedHeight, float fallbackThreshold)
        {
            return calibratedHeight > 0f ? calibratedHeight * CROUCH_RATIO : fallbackThreshold;
        }
    }

    public static class BreathingRules
    {
        /// <summary>Player inhales smoke when standing and the head is at/above the lowering smoke layer.</summary>
        public static bool IsBreathingSmoke(float headWorldY, float smokeLayerY, bool isCrouching)
        {
            return !isCrouching && headWorldY >= smokeLayerY;
        }
    }
}
