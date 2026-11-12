using NUnit.Framework;
using EscapeFire.Analytics;
using EscapeFire.Scenario;

namespace EscapeFire.Tests
{
    /// <summary>V3: the 114 call content check and the new Cứu nạn / Thoát nạn scorecard items.</summary>
    public class V3RescueScoringTests
    {
        [Test]
        public void Call114_AllFourItems_InOneSentence_Complete()
        {
            var c = new Call114Checker();
            c.Hear("Alo 114, cháy phòng server ở tầng 3 tòa B1 Đại học Bách Khoa, số 1 Đại Cồ Việt, có một người bất tỉnh mắc kẹt");
            Assert.IsTrue(c.IsComplete);
            Assert.IsNull(c.NextQuestion());
        }

        [Test]
        public void Call114_MissingFloor_AsksBackForFloor()
        {
            var c = new Call114Checker();
            c.Hear("Cháy tủ server ở tòa B1 Bách Khoa, có người bị ngất");
            Assert.IsFalse(c.IsComplete);
            Assert.IsFalse(c.Has(Call114Checker.Item.Floor));
            StringAssert.Contains("tầng", c.NextQuestion());
            c.Hear("Tầng ba");
            Assert.IsTrue(c.IsComplete);
        }

        [Test]
        public void Call114_WithoutDiacritics_StillUnderstood()
        {
            var c = new Call114Checker();
            c.Hear("chay phong lab tang 3 toa B1 dai hoc bach khoa, khong co ai mac ket");
            Assert.IsTrue(c.IsComplete);
        }

        [Test]
        public void Call114_SmallTalk_CoversNothing()
        {
            var c = new Call114Checker();
            Assert.IsEmpty(c.Hear("alo alo, nghe rõ không"));
            Assert.AreEqual(Call114Checker.Question(Call114Checker.Item.Address), c.NextQuestion());
        }

        private static SessionFacts FullV3()
        {
            var f = SessionFacts.Empty;
            f.Passed = true;
            f.FireStartedAt = 10f; f.AlarmRaisedAt = 15f; f.PowerCutAt = 20f; f.FirstSprayAt = 30f;
            f.PinPulled = true; f.FireExtinguished = true; f.NpcRescued = true;
            f.Call114Present = true; f.Call114Complete = true;
            f.VictimPresent = true; f.VictimSafe = true;
            f.PanicNpcPresent = true; f.PanicNpcStopped = true;
            f.WallFollowRatio = 0.5f;
            f.RollCallPresent = true; f.RollCallCorrect = true;
            return f;
        }

        private static int Pts(SessionFacts f, string key) => AfterActionReport.BuildScorecard(f).Find(i => i.Key == key).Points;

        [Test]
        public void FullV3Run_Scores100()
        {
            Assert.AreEqual(100, AfterActionReport.Total(AfterActionReport.BuildScorecard(FullV3())));
        }

        [Test]
        public void VictimLeftBehind_AndMinhInLift_LoseRescuePoints()
        {
            var f = FullV3();
            f.VictimSafe = false;
            f.PanicNpcStopped = false;
            Assert.AreEqual(8, Pts(f, "rescue"));
        }

        [Test]
        public void ElevatorUsed_WrongRollCall_NoWallFollow_LoseEvacuationPoints()
        {
            var f = FullV3();
            f.ElevatorUsed = true;
            f.RollCallCorrect = false;
            f.WallFollowRatio = 0f;
            Assert.AreEqual(0, Pts(f, "evacuation"));
        }

        [Test]
        public void Call114_NotMade_OnlyCallPointPoints()
        {
            var f = FullV3();
            f.Call114Complete = false;
            Assert.AreEqual(6, Pts(f, "alarm"));
            f.Call114Complete = true; f.Call114AskBacks = 2;
            Assert.AreEqual(9, Pts(f, "alarm"));
        }

        [Test]
        public void NotEvacuated_NoEvacuationPoints()
        {
            var f = FullV3();
            f.Passed = false;
            Assert.AreEqual(0, Pts(f, "evacuation"));
        }
    }
}
