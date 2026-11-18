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
    /// <summary>V1.5 / V6 — the learning path in the menu: Lesson → Course (6 levels, in order) → Quiz.</summary>
    public class V1ProgressionTests
    {
        [SetUp] public void Prefix() { ProgressionStore.KeyPrefix = "EF_test_progress_"; ProgressionStore.ResetAll(); Course.ResetAll(); Course.Current = null; }
        [TearDown] public void Restore() { Course.ResetAll(); Course.Current = null; ProgressionStore.ResetAll(); ProgressionStore.KeyPrefix = "EF_progress_"; }

        private static string Status() => GameObject.Find("Status").GetComponent<TMP_Text>().text;

        private static void Pass(int level, int score)
        {
            Course.Current = Course.Levels[level];
            Course.Record(score, true);
            Course.Current = null;
        }

        [UnityTest]
        public IEnumerator Course_LockedUntilLesson_LevelsOpenInOrder_Stars()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.MAIN_MENU, LoadSceneMode.Single);
            yield return null;
            var course = Object.FindFirstObjectByType<CourseMenuController>();
            Assert.AreEqual(6, Course.Levels.Count, "six levels");
            course.Show();
            Assert.IsFalse(course.Open, "the course is locked before the lesson");
            StringAssert.Contains("đang khoá", Status());

            ProgressionStore.LessonDone = true;
            course.Show();
            Assert.IsTrue(course.Open);
            Assert.AreEqual(0, course.Selected, "starts on level 1");
            Assert.IsTrue(Course.Unlocked(0) && !Course.Unlocked(1), "only level 1 is open");

            Pass(0, 95);
            Assert.AreEqual(3, Course.Stars(Course.Levels[0].Id), "95 points = 3 stars");
            Assert.IsTrue(Course.Unlocked(1) && !Course.Unlocked(2), "passing level 1 opens level 2");
            Pass(1, 72);
            Assert.AreEqual(2, Course.Stars(Course.Levels[1].Id));
            course.Back();
            StringAssert.Contains("2/6 màn", Status());
            StringAssert.Contains("màn 3", Status(), "the menu says what to do next");
        }
    }
}
