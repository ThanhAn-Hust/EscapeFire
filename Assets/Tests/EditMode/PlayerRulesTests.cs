using NUnit.Framework;
using EscapeFire.Player;

namespace EscapeFire.Tests
{
    public class PlayerRulesTests
    {
        [Test]
        public void CrouchThreshold_UsesSeventyPercentOfCalibratedHeight()
        {
            Assert.AreEqual(1.19f, CrouchRules.ComputeThreshold(1.70f, 1.2f), 1e-4f);
        }

        [Test]
        public void CrouchThreshold_NotCalibrated_UsesFallback()
        {
            Assert.AreEqual(1.2f, CrouchRules.ComputeThreshold(0f, 1.2f), 1e-4f);
        }

        [Test]
        public void BreathingSmoke_StandingWithHeadInsideSmokeLayer()
        {
            Assert.IsTrue(BreathingRules.IsBreathingSmoke(headWorldY: 1.6f, smokeLayerY: 1.5f, isCrouching: false));
        }

        [Test]
        public void NotBreathingSmoke_HeadBelowLayer()
        {
            Assert.IsFalse(BreathingRules.IsBreathingSmoke(1.6f, 3.2f, false));
        }

        [Test]
        public void NotBreathingSmoke_WhenCrouching()
        {
            Assert.IsFalse(BreathingRules.IsBreathingSmoke(1.1f, 1.0f, true));
        }
    }
}
