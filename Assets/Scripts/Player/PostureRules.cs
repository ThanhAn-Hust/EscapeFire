namespace EscapeFire.Player
{
    public enum Posture { Standing, Crouching, Crawling }

    /// <summary>
    /// Body posture from the real head height relative to the learner's calibrated standing head height H₀
    /// (plan Phần B.1-6). No "crouch button": the learner must physically get low.
    /// Thresholds and speeds are initial proposals — TODO(V1): verify against sources / tune on the headset.
    /// </summary>
    public static class PostureRules
    {
        public const float CrouchFraction = 0.80f;   // below 80 % of H₀ → crouching
        public const float CrawlFraction = 0.55f;    // below 55 % of H₀ → crawling / kneeling
        public const float DefaultStandingHeadHeight = 1.65f;

        public const float StandingSpeed = 1.4f;     // m/s, normal walking
        public const float CrouchingSpeed = 0.8f;
        public const float CrawlingSpeed = 0.4f;

        public static Posture Classify(float headHeight, float calibratedStandingHeight)
        {
            float h0 = calibratedStandingHeight > 0.5f ? calibratedStandingHeight : DefaultStandingHeadHeight;
            float k = headHeight / h0;
            if (k < CrawlFraction) return Posture.Crawling;
            if (k < CrouchFraction) return Posture.Crouching;
            return Posture.Standing;
        }

        public static float MaxSpeed(Posture posture) =>
            posture == Posture.Crawling ? CrawlingSpeed : posture == Posture.Crouching ? CrouchingSpeed : StandingSpeed;
    }
}
