using NUnit.Framework;
using EscapeFire.Scenario;

namespace EscapeFire.Tests
{
    public class SocraticRulesTests
    {
        [Test]
        public void WaterNearLivePanel_FiresThenCoolsDown()
        {
            var rules = new SocraticRules();
            var i = new SocraticInputs { HoldingConductiveAgent = true, PanelPowerOn = true, DistanceToPanel = 2f, HandToHandleDistance = 99f };

            Assert.AreEqual(SocraticTrigger.WaterNearLivePanel, rules.Evaluate(i, 10f, 0.1f));
            Assert.AreEqual(SocraticTrigger.None, rules.Evaluate(i, 12f, 0.1f), "Within cooldown");
            Assert.AreEqual(SocraticTrigger.WaterNearLivePanel, rules.Evaluate(i, 10f + SocraticRules.REPEAT_COOLDOWN_SECONDS, 0.1f));
        }

        [Test]
        public void WaterNearPanel_PowerCut_NoTrigger()
        {
            var rules = new SocraticRules();
            var i = new SocraticInputs { HoldingConductiveAgent = true, PanelPowerOn = false, DistanceToPanel = 1f, HandToHandleDistance = 99f };
            Assert.AreEqual(SocraticTrigger.None, rules.Evaluate(i, 1f, 0.1f));
        }

        [Test]
        public void StandingInSmoke_NeedsThreeContinuousSeconds()
        {
            var rules = new SocraticRules();
            var standing = new SocraticInputs { HeadInSmoke = true, HandToHandleDistance = 99f };
            var crouched = new SocraticInputs { HeadInSmoke = true, Crouching = true, HandToHandleDistance = 99f };

            Assert.AreEqual(SocraticTrigger.None, rules.Evaluate(standing, 1f, 2f));
            Assert.AreEqual(SocraticTrigger.None, rules.Evaluate(crouched, 2f, 0.5f), "Crouching resets the timer");
            Assert.AreEqual(SocraticTrigger.None, rules.Evaluate(standing, 3f, 2f));
            Assert.AreEqual(SocraticTrigger.StandingInSmoke, rules.Evaluate(standing, 4f, 1.5f));
        }

        [Test]
        public void HotHandle_OnlyWhenNotChecked()
        {
            var rules = new SocraticRules();
            var i = new SocraticInputs { DoorHot = true, HandToHandleDistance = 0.3f };
            Assert.AreEqual(SocraticTrigger.HotHandleUnchecked, rules.Evaluate(i, 1f, 0.1f));

            var checkedRules = new SocraticRules();
            i.DoorHeatChecked = true;
            Assert.AreEqual(SocraticTrigger.None, checkedRules.Evaluate(i, 1f, 0.1f));
        }

        [Test]
        public void AlarmReminder_FiresOnceAfter45s()
        {
            var rules = new SocraticRules();
            var i = new SocraticInputs { FireBurning = true, FireBurnSeconds = 40f, HandToHandleDistance = 99f };
            Assert.AreEqual(SocraticTrigger.None, rules.Evaluate(i, 40f, 0.1f));
            i.FireBurnSeconds = 46f;
            Assert.AreEqual(SocraticTrigger.AlarmNotRaised, rules.Evaluate(i, 46f, 0.1f));
            i.FireBurnSeconds = 200f;
            Assert.AreEqual(SocraticTrigger.None, rules.Evaluate(i, 200f, 0.1f));
        }

        [Test]
        public void EveryTriggerHasALine()
        {
            foreach (SocraticTrigger t in new[] { SocraticTrigger.WaterNearLivePanel, SocraticTrigger.StandingInSmoke,
                         SocraticTrigger.HotHandleUnchecked, SocraticTrigger.AlarmNotRaised })
            {
                Assert.IsNotEmpty(SocraticRules.LineFor(t));
            }
        }
    }

    public class VoiceCommandParserTests
    {
        [TestCase("Cháy! Có cháy!", VoiceCommand.RaiseFireAlarm)]
        [TestCase("BÁO CHÁY tầng 5", VoiceCommand.RaiseFireAlarm)]
        [TestCase("co chay", VoiceCommand.RaiseFireAlarm)]
        [TestCase("Báo động khẩn cấp", VoiceCommand.RaiseFireAlarm)]
        [TestCase("Fire! Fire!", VoiceCommand.RaiseFireAlarm)]
        [TestCase("Cứu với", VoiceCommand.CallForHelp)]
        [TestCase("gọi 114", VoiceCommand.CallForHelp)]
        [TestCase("chạy đi", VoiceCommand.None)]
        [TestCase("chảy nước", VoiceCommand.None)]
        [TestCase("", VoiceCommand.None)]
        public void Parse(string transcript, VoiceCommand expected)
        {
            Assert.AreEqual(expected, VoiceCommandParser.Parse(transcript));
        }

        [Test]
        public void Normalize_StripsDiacriticsAndDd()
        {
            Assert.AreEqual("bao dong do", VoiceCommandParser.Normalize("Báo động đỏ!!"));
        }
    }
}
