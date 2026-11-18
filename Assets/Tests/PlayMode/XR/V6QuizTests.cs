using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.UI;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V6 final quiz, fingertip only: locked until the lesson is done; 12 questions (one per PCCC rule), each answer
    /// shows right / wrong with the reason and source; the result lists the rules still wrong and they enter the
    /// learner's skill model (so the drill director targets them). The learner here knows 10 answers out of 12.
    /// </summary>
    public class V6QuizTests : BuildingSceneTest
    {
        [UnityTest, Timeout(400000)]
        public IEnumerator FinalQuiz_Fingertip_TenOfTwelve_Passes_AndFeedsTheSkillModel()
        {
            Application.targetFrameRate = 72;
            ProgressionStore.KeyPrefix = "EF_test_progress_";
            ProgressionStore.ResetAll();
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "XRTEST");
            SkillStore.Clear();
            yield return SceneManager.LoadSceneAsync(SceneNames.MAIN_MENU, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MAIN_MENU);
            var quiz = Object.FindFirstObjectByType<FinalQuizController>();
            Assert.IsNotNull(quiz, "the menu has the final quiz (run EscapeFire > V5 > Rebuild Menu Flow Scenes)");

            yield return PressUi("4. TRẮC NGHIỆM TỔNG");
            Assert.IsFalse(quiz.Running, "locked before the lesson");

            ProgressionStore.LessonDone = true; // scenario setup: lesson finished (V1LessonTests)
            yield return PressUi("4. TRẮC NGHIỆM TỔNG");
            Assert.IsTrue(quiz.Running, "quiz started");
            Assert.AreEqual(12, quiz.Count, "one question per PCCC rule");
            yield return FaceBoard();
            Shot("quiz_01_question");

            for (int q = 0; q < quiz.Count; q++)
            {
                int right = quiz.CurrentCorrectOption;
                int pick = q < 2 ? (right + 1) % 3 : right; // gets the first two wrong
                yield return PressUi("PHƯƠNG ÁN " + (char)('A' + pick));
                if (q == 0) { yield return FaceBoard(); Shot("quiz_02_feedback_wrong"); }
                if (q == 2) { yield return FaceBoard(); Shot("quiz_03_feedback_right"); }
                yield return PressUi("CÂU TIẾP THEO");
            }
            Assert.IsTrue(quiz.Finished);
            Assert.AreEqual(10, quiz.Correct);
            yield return FaceBoard();
            Shot("quiz_04_result");
            Assert.AreEqual(10, ProgressionStore.QuizBest, "best score kept");
            Assert.GreaterOrEqual(SkillStore.WeakSkills(2).Count, 1, "the wrong answers are weak skills now");

            yield return PressUi("VỀ MENU (thoát trắc nghiệm)");
            Assert.IsFalse(quiz.Running);
            yield return FaceBoard();
            Shot("quiz_05_back_in_the_menu");
        }
    }
}
