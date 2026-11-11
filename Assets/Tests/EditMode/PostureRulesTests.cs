using NUnit.Framework;
using EscapeFire.Player;

namespace EscapeFire.Tests.EditMode
{
    public class PostureRulesTests
    {
        [TestCase(1.70f, 1.70f, Posture.Standing)]
        [TestCase(1.30f, 1.70f, Posture.Crouching)]
        [TestCase(0.80f, 1.70f, Posture.Crawling)]
        [TestCase(1.20f, 0f, Posture.Crouching)] // uncalibrated → default H0 1.65
        public void Classify_FromHeadHeight(float head, float h0, Posture expected) => Assert.AreEqual(expected, PostureRules.Classify(head, h0));

        [Test]
        public void Speed_DropsWithPosture()
        {
            Assert.Greater(PostureRules.MaxSpeed(Posture.Standing), PostureRules.MaxSpeed(Posture.Crouching));
            Assert.Greater(PostureRules.MaxSpeed(Posture.Crouching), PostureRules.MaxSpeed(Posture.Crawling));
        }
    }
}
