using System;
using NUnit.Framework;
using EscapeFire.Core;

namespace EscapeFire.Tests
{
    public class GameSessionTests
    {
        [SetUp]
        public void SetUp() => GameSession.Reset();

        [TearDown]
        public void TearDown() => GameSession.Reset();

        [Test]
        public void Begin_SetsModeStudentAndNewSessionId()
        {
            GameSession.Begin(TrainingMode.Exam, "SV_001");
            string first = GameSession.SessionId;
            GameSession.Begin(TrainingMode.Exam, "SV_001");

            Assert.AreEqual(TrainingMode.Exam, GameSession.Mode);
            Assert.AreEqual("SV_001", GameSession.StudentId);
            Assert.IsFalse(string.IsNullOrEmpty(first));
            Assert.AreNotEqual(first, GameSession.SessionId);
        }

        [Test]
        public void Begin_BlankStudentId_FallsBackToDefault()
        {
            GameSession.Begin(TrainingMode.Guided, "  ");

            Assert.AreEqual(GameSession.DEFAULT_STUDENT_ID, GameSession.StudentId);
        }

        [Test]
        public void SetCalibratedHeight_OutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GameSession.SetCalibratedHeight(0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameSession.SetCalibratedHeight(2.6f));
            Assert.IsFalse(GameSession.IsCalibrated);
        }

        [Test]
        public void SetCalibratedHeight_Valid_MarksCalibrated()
        {
            GameSession.SetCalibratedHeight(1.65f);

            Assert.IsTrue(GameSession.IsCalibrated);
            Assert.AreEqual(1.65f, GameSession.CalibratedHeight, 1e-5f);
        }
    }
}
