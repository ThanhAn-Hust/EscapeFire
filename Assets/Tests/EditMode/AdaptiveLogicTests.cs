using NUnit.Framework;
using EscapeFire.Analytics;
using EscapeFire.Hazards;
using EscapeFire.Scenario;

namespace EscapeFire.Tests
{
    public class CognitiveFrictionModelTests
    {
        [Test]
        public void Compute_AllZero_IsZero_AllMax_IsOne()
        {
            var m = new CognitiveFrictionModel();
            Assert.AreEqual(0f, m.Compute(0f, 0f, 0), 1e-5f);
            Assert.AreEqual(1f, m.Compute(500f, 100f, 10), 1e-5f);
        }

        [Test]
        public void Compute_UsesWeights()
        {
            var m = new CognitiveFrictionModel(0.4f, 0.35f, 0.25f, 15f, 4);
            // head max only -> 0.4 ; half tau hesitation -> 0.175 ; 2/4 mistakes -> 0.125
            Assert.AreEqual(0.4f, m.Compute(160f, 0f, 0), 1e-4f);
            Assert.AreEqual(0.175f, m.Compute(0f, 7.5f, 0), 1e-4f);
            Assert.AreEqual(0.125f, m.Compute(0f, 0f, 2), 1e-4f);
        }

        [Test]
        public void Sample_Hysteresis_FiresOncePerTransition()
        {
            var m = new CognitiveFrictionModel(1f, 0f, 0f);
            // velocity giving CF 0.8 -> Panic
            float panicVel = 10f + 0.8f * 150f;
            Assert.IsTrue(m.Sample(panicVel, 0f, 0));
            Assert.AreEqual(CognitiveState.Panic, m.State);
            Assert.IsFalse(m.Sample(panicVel, 0f, 0), "Staying in panic must not re-fire");

            // CF 0.70 is between exit (0.65) and enter (0.75) -> stays Panic
            Assert.IsFalse(m.Sample(10f + 0.70f * 150f, 0f, 0));
            Assert.AreEqual(CognitiveState.Panic, m.State);

            // CF 0.5 -> Nominal
            Assert.IsTrue(m.Sample(10f + 0.5f * 150f, 0f, 0));
            Assert.AreEqual(CognitiveState.Nominal, m.State);

            // CF 0.1 -> Calm
            Assert.IsTrue(m.Sample(10f + 0.1f * 150f, 0f, 0));
            Assert.AreEqual(CognitiveState.Calm, m.State);
        }

        [Test]
        public void Average_TracksSamples()
        {
            var m = new CognitiveFrictionModel(1f, 0f, 0f);
            m.Sample(160f, 0f, 0); // 1.0
            m.Sample(0f, 0f, 0);   // 0.0
            Assert.AreEqual(0.5f, m.Average, 1e-5f);
        }
    }

    public class EmitterBudgetTests
    {
        [Test]
        public void TryRegister_RespectsCapacity_AndIsIdempotent()
        {
            var b = new EmitterBudget(2);
            Assert.IsTrue(b.TryRegister(1));
            Assert.IsTrue(b.TryRegister(1));
            Assert.IsTrue(b.TryRegister(2));
            Assert.IsFalse(b.TryRegister(3));
            b.Unregister(1);
            Assert.IsTrue(b.TryRegister(3));
            Assert.AreEqual(2, b.Count);
        }
    }

    public class DirectiveTests
    {
        private static DirectiveContext Ok() => new DirectiveContext
        {
            EmergencyActive = true,
            ActiveFires = 1,
            MaxFires = 4,
            HasSafeSecondarySlot = true
        };

        [TestCase("ESCALATE_SECONDARY_FIRE", Directive.EscalateSecondaryFire)]
        [TestCase(" trigger_co2_pin_jam ", Directive.TriggerCo2PinJam)]
        [TestCase("ESCALATE_FM200_COUNTDOWN", Directive.EscalateFm200Countdown)]
        public void TryParse_KnownCodes(string code, Directive expected)
        {
            Assert.IsTrue(DirectiveCodes.TryParse(code, out var d));
            Assert.AreEqual(expected, d);
            Assert.AreEqual(code.Trim().ToUpperInvariant(), DirectiveCodes.ToCode(d));
        }

        [Test]
        public void TryParse_UnknownCode_False()
        {
            Assert.IsFalse(DirectiveCodes.TryParse("SET_PLAYER_ON_FIRE", out _));
            Assert.IsFalse(DirectiveCodes.TryParse(null, out _));
        }

        [Test]
        public void Maintain_AlwaysAccepted()
        {
            Assert.AreEqual(DirectiveResult.Accepted, DirectiveValidator.Validate(Directive.MaintainCurrentPace, default));
        }

        [Test]
        public void OutsideEmergency_Rejected()
        {
            var ctx = Ok(); ctx.EmergencyActive = false;
            Assert.AreEqual(DirectiveResult.RejectedWrongPhase, DirectiveValidator.Validate(Directive.EscalateSecondaryFire, ctx));
        }

        [Test]
        public void Panicking_NeverEscalates()
        {
            var ctx = Ok(); ctx.LearnerPanicking = true;
            Assert.AreEqual(DirectiveResult.RejectedLearnerPanicking, DirectiveValidator.Validate(Directive.EscalateFm200Countdown, ctx));
        }

        [Test]
        public void SecondaryFire_BudgetAndExitChecks()
        {
            var ctx = Ok();
            Assert.AreEqual(DirectiveResult.Accepted, DirectiveValidator.Validate(Directive.EscalateSecondaryFire, ctx));
            ctx.ActiveFires = 4;
            Assert.AreEqual(DirectiveResult.RejectedBudgetOverflow, DirectiveValidator.Validate(Directive.EscalateSecondaryFire, ctx));
            ctx.ActiveFires = 1; ctx.HasSafeSecondarySlot = false;
            Assert.AreEqual(DirectiveResult.RejectedTrappedPlayer, DirectiveValidator.Validate(Directive.EscalateSecondaryFire, ctx));
        }

        [Test]
        public void PinJam_RejectedWhenAlreadyUnpinnedOrApplied()
        {
            var ctx = Ok(); ctx.AnyExtinguisherUnpinned = true;
            Assert.AreEqual(DirectiveResult.RejectedAlreadyUnpinned, DirectiveValidator.Validate(Directive.TriggerCo2PinJam, ctx));
            ctx = Ok(); ctx.PinJamAlreadyApplied = true;
            Assert.AreEqual(DirectiveResult.RejectedAlreadyActive, DirectiveValidator.Validate(Directive.TriggerCo2PinJam, ctx));
        }

        [Test]
        public void AlarmCut_RejectedWhenAlarmRaised()
        {
            var ctx = Ok(); ctx.AlarmAlreadyRaised = true;
            Assert.AreEqual(DirectiveResult.RejectedAlarmAlreadyRaised, DirectiveValidator.Validate(Directive.TriggerAlarmCableCut, ctx));
        }

        [Test]
        public void Fm200_RejectedWhenActive()
        {
            var ctx = Ok(); ctx.Fm200Active = true;
            Assert.AreEqual(DirectiveResult.RejectedAlreadyActive, DirectiveValidator.Validate(Directive.EscalateFm200Countdown, ctx));
        }
    }
}
