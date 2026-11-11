using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.UI;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>V1.5 — Lesson → Guided → Exam ladder in the main menu.</summary>
    public class V1ProgressionTests
    {
        [SetUp] public void Prefix() { ProgressionStore.KeyPrefix = "EF_test_progress_"; ProgressionStore.ResetAll(); }
        [TearDown] public void Restore() { ProgressionStore.ResetAll(); ProgressionStore.KeyPrefix = "EF_progress_"; }

        private static string Status() => GameObject.Find("Status").GetComponent<TMP_Text>().text;

        [UnityTest]
        public IEnumerator Exam_LockedUntilLessonAndGuidedPassed()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.MAIN_MENU, LoadSceneMode.Single);
            yield return null;
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            menu.SelectExamMode();
            StringAssert.Contains("SÁT HẠCH mở khi", Status());
            menu.SelectGuidedMode();
            StringAssert.Contains("BÀI GIẢNG trước", Status());

            ProgressionStore.LessonDone = true;
            menu.SelectGuidedMode();
            StringAssert.Contains("Huấn luyện có Hướng dẫn", Status());
            menu.SelectExamMode();
            StringAssert.Contains("SÁT HẠCH mở khi", Status(), "the exam needs a passed guided drill too");

            ProgressionStore.GuidedPassed = true;
            menu.SelectExamMode();
            StringAssert.Contains("Sát hạch Thực chiến", Status());
        }
    }
}
