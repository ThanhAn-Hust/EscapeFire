using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.UI;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>Review screenshots of the training-centre backdrop: menu, quiz, debrief (AAR), lesson room (Demo).</summary>
    [Category("Demo")]
    public class V6CentreShots : BuildingSceneTest
    {
        private IEnumerator Look(float yaw, float pitch, string shot)
        {
            Body(yaw, 1.65f, pitch);
            yield return Xr.Wait(0.4f);
            Shot(shot);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator TrainingCentre_Screenshots()
        {
            Application.targetFrameRate = 72;
            ProgressionStore.KeyPrefix = "EF_test_progress_";
            ProgressionStore.ResetAll();
            ProgressionStore.LessonDone = true; // so the quiz opens (scenario setup)
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "SHOTS");
            yield return SceneManager.LoadSceneAsync(SceneNames.MAIN_MENU, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MAIN_MENU);
            yield return Look(0f, -5f, "centre_01_menu");
            yield return Look(-60f, -5f, "centre_02_menu_left");
            yield return Look(60f, -5f, "centre_03_menu_right");
            yield return Look(180f, 0f, "centre_04_menu_behind");
            Object.FindFirstObjectByType<FinalQuizController>().Begin();
            yield return Look(0f, -5f, "centre_05_quiz");

            yield return SceneManager.LoadSceneAsync(SceneNames.AAR, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.AAR);
            yield return Look(0f, -5f, "centre_06_aar");
            yield return Look(-70f, -5f, "centre_07_aar_left");

            yield return SceneManager.LoadSceneAsync(SceneNames.LESSON, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.LESSON);
            yield return Look(0f, -3f, "centre_08_lesson");
            yield return Look(90f, -3f, "centre_09_lesson_right");
            yield return Look(180f, -3f, "centre_10_lesson_behind");
        }
    }
}
