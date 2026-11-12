using System.Linq;
using NUnit.Framework;
using EscapeFire.Analytics;
using EscapeFire.Scenario;

namespace EscapeFire.Tests
{
    /// <summary>VL: skill model (BKT), evidence from a drill, closed scoring loopholes, recall-first coaching.</summary>
    public class VLSkillTests
    {
        // ---------------- BKT skill model

        [Test]
        public void CorrectAnswers_RaiseMastery_WrongOnesLowerIt()
        {
            var m = new SkillModel();
            m.BeginSession();
            float p0 = m.Known("R07_STAY_LOW");
            m.Observe("R07_STAY_LOW", true);
            float p1 = m.Known("R07_STAY_LOW");
            m.Observe("R07_STAY_LOW", false);
            Assert.Greater(p1, p0);
            Assert.Less(m.Known("R07_STAY_LOW"), p1);
        }

        [Test]
        public void OneLuckyGuess_IsNotMastery_ButAFewSessionsAre()
        {
            var m = new SkillModel();
            m.BeginSession();
            m.Observe("R10_NO_ELEVATOR", true, guess: 1f / 3f);
            Assert.IsFalse(m.IsMastered("R10_NO_ELEVATOR"), "a single right answer out of 3 could be a guess");
            for (int s = 0; s < 4; s++) { m.BeginSession(); m.Observe("R10_NO_ELEVATOR", true); }
            Assert.IsTrue(m.IsMastered("R10_NO_ELEVATOR"));
        }

        [Test]
        public void WeakSkills_LowestFirst_ThenSpacedReviewsWhenDue()
        {
            var m = new SkillModel();
            for (int s = 0; s < 5; s++) { m.BeginSession(); m.Observe("R07_STAY_LOW", true); m.Observe("R08_DOOR_BACK_OF_HAND", false); }
            var weak = m.WeakSkills(new[] { "R07_STAY_LOW", "R08_DOOR_BACK_OF_HAND" }, 2);
            Assert.AreEqual("R08_DOOR_BACK_OF_HAND", weak[0], "the failed skill comes first");
            Assert.IsFalse(weak.Contains("R07_STAY_LOW"), "a mastered skill just practised is not due yet");
            for (int s = 0; s < 8; s++) m.BeginSession();
            Assert.IsTrue(m.WeakSkills(new[] { "R07_STAY_LOW", "R08_DOOR_BACK_OF_HAND" }, 2).Contains("R07_STAY_LOW"), "spaced review comes back");
        }

        [Test]
        public void Model_SurvivesJson()
        {
            var m = new SkillModel();
            m.BeginSession();
            m.Observe("R03_RIGHT_AGENT", true);
            var back = SkillModel.FromJson(m.ToJson());
            Assert.AreEqual(m.Known("R03_RIGHT_AGENT"), back.Known("R03_RIGHT_AGENT"), 1e-6f);
            Assert.AreEqual(1, back.Session);
            Assert.AreEqual(0, SkillModel.FromJson("garbage").Session);
        }

        // ---------------- evidence from a drill

        private static SessionFacts Good()
        {
            var f = SessionFacts.Empty;
            f.Passed = true;
            f.FireStartedAt = 10f; f.AlarmRaisedAt = 15f; f.PowerCutAt = 20f; f.FirstSprayAt = 30f;
            f.PinPulled = true; f.FireExtinguished = true; f.PassBaseRatio = 0.8f; f.PassSweepRatio = 0.7f;
            f.DoorHeatChecked = true; f.NpcRescued = true; f.HoaTracked = true; f.HoaOut = true;
            return f;
        }

        [Test]
        public void GoodDrill_ShowsTheTestedSkills_AndOnlyThose()
        {
            var obs = SkillEvidence.FromDrill(Good());
            var skills = obs.Select(o => o.Skill).ToList();
            CollectionAssert.IsSubsetOf(new[] { "R01_ALARM_EARLY", "R02_POWER_OFF_FIRST", "R03_RIGHT_AGENT", "R04_PASS", "R07_STAY_LOW", "R08_DOOR_BACK_OF_HAND", "R09_RESCUE_KEEP_LOW", "R10_NO_ELEVATOR" }, skills);
            Assert.IsTrue(obs.All(o => o.Correct), string.Join(", ", obs.Where(o => !o.Correct).Select(o => o.Skill + ":" + o.Why)));
            Assert.IsFalse(skills.Contains("R11_FM200_EVACUATE"), "no FM-200 countdown, no evidence about it");
            Assert.IsFalse(skills.Contains("R12_FIRE_TRIANGLE"), "knowledge item: lesson / quiz only");
        }

        [Test]
        public void Mistakes_AreObservedAsWrong()
        {
            var f = Good();
            f.FirstSprayWithConductiveAgent = true;
            f.StandingInSmokeSeconds = 9f;
            f.PassBaseRatio = 0.2f;
            var obs = SkillEvidence.FromDrill(f).ToDictionary(o => o.Skill);
            Assert.IsFalse(obs["R03_RIGHT_AGENT"].Correct);
            Assert.IsFalse(obs["R07_STAY_LOW"].Correct);
            Assert.IsFalse(obs["R04_PASS"].Correct);
            Assert.IsTrue(obs["R08_DOOR_BACK_OF_HAND"].Correct);
        }

        [Test]
        public void NoEmergency_NoEvidence()
        {
            Assert.IsEmpty(SkillEvidence.FromDrill(SessionFacts.Empty));
        }

        // ---------------- VL.2 loopholes

        private static int Pts(SessionFacts f, string key) => AfterActionReport.BuildScorecard(f).Find(i => i.Key == key).Points;

        [Test]
        public void HoaPickedUpButLeftBehind_IsNotARescue()
        {
            var f = Good();
            f.HoaOut = false;
            Assert.Less(Pts(f, "rescue"), Pts(Good(), "rescue"));
            Assert.IsFalse(SkillEvidence.FromDrill(f).First(o => o.Skill == "R09_RESCUE_KEEP_LOW").Correct);
        }

        [Test]
        public void LateAlarm_LosesPoints()
        {
            var f = Good();
            f.AlarmRaisedAt = f.FireStartedAt + 200f;
            Assert.AreEqual(3, Pts(f, "alarm"));
            Assert.AreEqual(10, Pts(Good(), "alarm"));
            Assert.IsFalse(SkillEvidence.FromDrill(f).First(o => o.Skill == "R01_ALARM_EARLY").Correct);
        }

        [Test]
        public void SayingTheWordAddress_IsNotAnAddress()
        {
            var c = new Call114Checker();
            c.Hear("địa chỉ là ở đây, cháy phòng server tầng 3, có người bị ngất");
            Assert.IsFalse(c.Has(Call114Checker.Item.Address));
            c.Hear("tòa B1 Bách Khoa");
            Assert.IsTrue(c.IsComplete);
        }

        // ---------------- VL.5 recall before telling, fading with mastery

        [Test]
        public void Mentor_AsksFirst_TellsOnRepeat_OnlyAsksOnceMastered()
        {
            var t = SocraticTrigger.StandingInSmoke;
            Assert.AreEqual(SocraticRules.QuestionFor(t), SocraticRules.LineFor(t, 0, false));
            Assert.AreEqual(SocraticRules.LineFor(t), SocraticRules.LineFor(t, 1, false));
            Assert.AreEqual(SocraticRules.QuestionFor(t), SocraticRules.LineFor(t, 3, true));
            StringAssert.EndsWith("?", SocraticRules.QuestionFor(SocraticTrigger.HotHandleUnchecked));
            Assert.AreEqual("R08_DOOR_BACK_OF_HAND", SocraticRules.SkillOf(SocraticTrigger.HotHandleUnchecked));
        }
    }
}
