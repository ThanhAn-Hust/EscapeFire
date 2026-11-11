using NUnit.Framework;
using EscapeFire.Hazards;

namespace EscapeFire.Tests.EditMode
{
    public class FireSuppressionModelTests
    {
        [Test] public void BestAtBase_Sweeping_FullSqueeze() => Assert.AreEqual(1f, FireSuppressionModel.Efficiency(1.8f, 3f, 0.1f, 25f, 1f), 1e-4f);

        [Test] public void SweepingBeatsHoldingStill() =>
            Assert.Greater(FireSuppressionModel.Efficiency(1.8f, 3f, 0.1f, 25f, 1f), FireSuppressionModel.Efficiency(1.8f, 3f, 0.1f, 0f, 1f));

        [Test] public void AimingAtFlamesIsPoor() =>
            Assert.Less(FireSuppressionModel.Efficiency(1.8f, 3f, 1.0f, 25f, 1f), 0.25f);

        [Test] public void OutOfRangeDoesNothing() => Assert.AreEqual(0f, FireSuppressionModel.Efficiency(4.5f, 0f, 0f, 25f, 1f));

        [Test] public void MissingTheFireDoesNothing() => Assert.AreEqual(0f, FireSuppressionModel.Efficiency(1.8f, 40f, 0f, 25f, 1f));

        [Test] public void LightSqueezeDoesNotOpenValve() => Assert.AreEqual(0f, FireSuppressionModel.Efficiency(1.8f, 0f, 0f, 25f, 0.4f));

        [Test] public void TooCloseIsWorseThanBestRange() =>
            Assert.Less(FireSuppressionModel.Range(0.3f), FireSuppressionModel.Range(1.5f));
    }
}
