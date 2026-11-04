using NUnit.Framework;
using EscapeFire.Player;

namespace EscapeFire.Tests
{
    public class OxygenModelTests
    {
        [Test]
        public void Tick_NotBreathingSmoke_DoesNotDeplete()
        {
            var model = new OxygenModel(100f);
            model.Tick(5f, breathingSmoke: false, depletionRate: 10f, recoveryRate: 5f);
            Assert.AreEqual(100f, model.Current, 1e-4f);
        }

        [Test]
        public void Tick_BreathingSmoke_DepletesAndReportsOnceWhenEmpty()
        {
            var model = new OxygenModel(100f);
            Assert.IsFalse(model.Tick(5f, true, 10f, 5f));
            Assert.AreEqual(50f, model.Current, 1e-4f);

            Assert.IsTrue(model.Tick(6f, true, 10f, 5f));
            Assert.AreEqual(0f, model.Current, 1e-4f);

            Assert.IsFalse(model.Tick(1f, true, 10f, 5f), "Depletion must only be reported once");
        }

        [Test]
        public void Tick_RecoversButNeverExceedsMax()
        {
            var model = new OxygenModel(100f);
            model.Deplete(30f);
            model.Tick(100f, false, 10f, 5f);
            Assert.AreEqual(100f, model.Current, 1e-4f);
        }

        [Test]
        public void Depleted_DoesNotRecover()
        {
            var model = new OxygenModel(100f);
            model.Deplete(100f);
            model.Tick(10f, false, 10f, 5f);
            Assert.IsTrue(model.IsDepleted);
        }
    }
}
