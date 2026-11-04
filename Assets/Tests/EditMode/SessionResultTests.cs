using NUnit.Framework;
using EscapeFire.Core;

namespace EscapeFire.Tests
{
    public class SessionResultTests
    {
        [SetUp]
        public void SetUp() => SessionResult.Reset();

        [TearDown]
        public void TearDown() => SessionResult.Reset();

        [Test]
        public void Record_StoresOutcomeAndCopiesLogs()
        {
            var logs = new System.Collections.Generic.List<string> { "a", "b" };
            SessionResult.Record(true, 95, 120f, "ignored", logs, "{}");
            logs.Add("c");

            Assert.IsTrue(SessionResult.HasResult);
            Assert.IsTrue(SessionResult.Passed);
            Assert.AreEqual(95, SessionResult.Score);
            Assert.AreEqual(2, SessionResult.Logs.Count, "Logs must be copied, not aliased");
            Assert.AreEqual(string.Empty, SessionResult.FailCause, "A passed drill has no fail cause");
        }

        [Test]
        public void Record_Failed_KeepsCause()
        {
            SessionResult.Record(false, 10, 30f, "Điện giật", null, null);

            Assert.IsFalse(SessionResult.Passed);
            Assert.AreEqual("Điện giật", SessionResult.FailCause);
            Assert.AreEqual(0, SessionResult.Logs.Count);
        }

        [Test]
        public void Reset_ClearsResult()
        {
            SessionResult.Record(true, 50, 1f, null, null, null);
            SessionResult.Reset();
            Assert.IsFalse(SessionResult.HasResult);
        }

        [TestCase(false, 200, "KHÔNG ĐẠT - CHƯA AN TOÀN")]
        [TestCase(true, 130, "XUẤT SẮC - ĐẠT CHUẨN AN TOÀN")]
        [TestCase(true, 80, "KHÁ - ĐẠT CHUẨN AN TOÀN")]
        [TestCase(true, 50, "ĐẠT")]
        [TestCase(true, 20, "CHƯA ĐẠT - CẦN LUYỆN TẬP THÊM")]
        public void Classify_MapsScoreToGrade(bool passed, int score, string expected)
        {
            Assert.AreEqual(expected, GradeRules.Classify(passed, score));
        }
    }
}
