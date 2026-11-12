using System.IO;
using NUnit.Framework;
using EscapeFire.Analytics;

namespace EscapeFire.Tests
{
    public class AfterActionReportTests
    {
        private static SessionFacts Perfect()
        {
            var f = SessionFacts.Empty;
            f.Passed = true;
            f.FireStartedAt = 25f;
            f.AlarmRaisedAt = 30f;
            f.PowerCutAt = 35f;
            f.FirstSprayAt = 40f;
            f.PinPulled = true;
            f.FireExtinguished = true;
            f.NpcRescued = true;
            f.DoorHeatChecked = true;
            f.AverageCognitiveFriction = 0.3f;
            return f;
        }

        [Test]
        public void PerfectRun_Scores100_AndFullRadar()
        {
            var f = Perfect();
            var items = AfterActionReport.BuildScorecard(f);
            Assert.AreEqual(7, items.Count);
            Assert.AreEqual(100, AfterActionReport.Total(items));

            var radar = AfterActionReport.BuildRadar(f, items);
            Assert.AreEqual(1f, radar.RiskDetection, 1e-4f);
            Assert.AreEqual(1f, radar.ExtinguisherSelection, 1e-4f);
            Assert.AreEqual(0.7f, radar.StressResilience, 1e-4f);
        }

        [Test]
        public void StandingInSmoke_DeductsOnePointPerSecond()
        {
            var f = Perfect();
            f.StandingInSmokeSeconds = 8f;
            var items = AfterActionReport.BuildScorecard(f);
            Assert.AreEqual(7, items.Find(i => i.Key == "crouch").Points);
            Assert.AreEqual(92, AfterActionReport.Total(items)); // crouch 15 - 8
        }

        [Test]
        public void PowerCutAfterSpray_PartialCredit()
        {
            var f = Perfect();
            f.PowerCutAt = 50f;
            Assert.AreEqual(6, AfterActionReport.BuildScorecard(f).Find(i => i.Key == "power_cut").Points);
        }

        [Test]
        public void WaterOnLivePower_ZeroAgentScore_AndDebriefWarns()
        {
            var f = Perfect();
            f.Passed = false;
            f.FailCause = "Điện giật.";
            f.FirstSprayWithConductiveAgent = true;
            f.UsedConductiveAgentOnLivePower = true;
            var items = AfterActionReport.BuildScorecard(f);
            Assert.AreEqual(0, items.Find(i => i.Key == "agent").Points);

            string debrief = AfterActionReport.TemplateDebrief(f, items);
            StringAssert.Contains("Điện giật", debrief);
            StringAssert.Contains("ngắt Aptomat trước", debrief);
        }

        [Test]
        public void NothingDone_ScoresMinimal()
        {
            var f = SessionFacts.Empty;
            f.StandingInSmokeSeconds = 60f;
            Assert.AreEqual(0, AfterActionReport.Total(AfterActionReport.BuildScorecard(f)));
        }
    }

    public class LmsOutboxTests
    {
        private string _dir;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "ef_outbox_" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void EnqueueThenDeliver_RemovesFile()
        {
            var outbox = new LmsOutbox(_dir);
            outbox.Enqueue("abc123", "{\"a\":1}");
            Assert.AreEqual(1, outbox.PendingCount);

            var pending = outbox.Pending();
            Assert.AreEqual("abc123", pending[0].sessionId);
            Assert.AreEqual("{\"a\":1}", pending[0].json);

            outbox.MarkDelivered(pending[0].path);
            Assert.AreEqual(0, outbox.PendingCount);
        }

        [Test]
        public void SessionIdIsSanitizedForFileName()
        {
            var outbox = new LmsOutbox(_dir);
            outbox.Enqueue("../../evil", "{}");
            Assert.AreEqual(1, Directory.GetFiles(_dir).Length, "File must stay inside the outbox folder");
        }
    }
}

namespace EscapeFire.Tests
{
    public class PassTechniqueScoringTests
    {
        private static EscapeFire.Analytics.SessionFacts Facts(float aim, float sweep)
        {
            var f = EscapeFire.Analytics.SessionFacts.Empty;
            f.FireStartedAt = 1f; f.FirstSprayAt = 5f; f.PinPulled = true; f.FireExtinguished = true;
            f.PassBaseRatio = aim; f.PassSweepRatio = sweep;
            return f;
        }

        private static int Technique(EscapeFire.Analytics.SessionFacts f)
        {
            foreach (var i in EscapeFire.Analytics.AfterActionReport.BuildScorecard(f)) if (i.Key == "technique") return i.Points;
            return -1;
        }

        [NUnit.Framework.Test] public void GoodPASS_Full15() => NUnit.Framework.Assert.AreEqual(15, Technique(Facts(0.8f, 0.7f)));
        [NUnit.Framework.Test] public void AimedAtFlames_NoSweep_LosesPoints() => NUnit.Framework.Assert.AreEqual(8, Technique(Facts(0f, 0f)));
        [NUnit.Framework.Test] public void Unmeasured_NotPenalised() => NUnit.Framework.Assert.AreEqual(15, Technique(Facts(-1f, -1f)));
    }
}
