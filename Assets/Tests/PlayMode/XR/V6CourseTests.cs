using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Generation;
using EscapeFire.Interactions;
using EscapeFire.Scenario;
using EscapeFire.UI;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V6 course levels: level 1 (classroom bin fire) ends at the exit and opens level 2; level 3 (oil fire) punishes
    /// water with a flare-up; level 5 imposes its night situation (lights off, smoke, stairwell B blocked) through the
    /// shared challenge checks. Levels are started the way the menu's START does (CourseLauncher).
    /// </summary>
    public class V6CourseTests : BuildingSceneTest
    {
        [SetUp] public void Prefix() { ProgressionStore.KeyPrefix = "EF_test_progress_"; ProgressionStore.ResetAll(); Course.ResetAll(); Course.Current = null; }
        [TearDown] public void Restore() { Course.ResetAll(); Course.Current = null; ProgressionStore.ResetAll(); ProgressionStore.KeyPrefix = "EF_progress_"; }

        private ScenarioBuilder _builder;

        private IEnumerator StartLevel(int i)
        {
            Application.targetFrameRate = 72;
            GameSession.Reset();
            ProgressionStore.LessonDone = true;
            bool loaded = false;
            UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode> onLoaded = (sc, m) => loaded = true;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += onLoaded;
            Assert.IsTrue(CourseLauncher.Start(Course.Levels[i], "XRTEST"));
            var level = Course.Levels[i];
            for (float t = 0f; !loaded && t < 30f; t += Time.unscaledDeltaTime) yield return null; // a fresh load, even if already in that scene
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= onLoaded;
            yield return WaitForScene(level.InLab ? SceneNames.LAB : SceneNames.GENERATED, 30f);
            if (!level.InLab)
            {
                _builder = Object.FindFirstObjectByType<ScenarioBuilder>();
                Assert.IsTrue(_builder.Built, string.Join(",", _builder.Errors));
                typeof(GeneratedDrill).GetField("loadAARScene", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(_builder.GetComponent<GeneratedDrill>(), false); // stay in the room to check it
            }
            Assert.AreSame(level, Course.Current);
            Assert.AreEqual(level.TrainingMode, GameSession.Mode);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator Level1_Classroom_LeaveByTheExit_PassesAndOpensLevel2()
        {
            yield return StartLevel(0);
            Assert.IsNotNull(_builder.GetComponent<GeneratedDrill>().Fire, "the bin is burning");
            Shot("course_L1_classroom");
            var exit = ScenarioBuilder.ExitBounds(_builder.Layout.Exits[0]).center;
            yield return StickWalk(new Vector3(_builder.Layout.Exits[0].X, 0f, _builder.Layout.Exits[0].Z), new Vector3(exit.x, 0f, exit.z));
            Assert.AreEqual(GeneratedDrill.State.Won, _builder.GetComponent<GeneratedDrill>().Current);
            Assert.IsTrue(Course.Passed(Course.Levels[0].Id), "level 1 passed");
            Assert.AreEqual(1, Course.Stars(Course.Levels[0].Id), "only left (no fire fought): 1 star");
            Assert.IsTrue(Course.Unlocked(1), "level 2 open");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator Level3_OilFire_Water_FlaresUp_Fails()
        {
            yield return StartLevel(2);
            var drill = _builder.GetComponent<GeneratedDrill>();
            FireExtinguisher water = null;
            foreach (var e in Object.FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None)) if (e.Type == ExtinguisherType.WaterFoam) water = e;
            Assert.IsNotNull(water, "a water extinguisher is there to tempt the learner");
            Vector3 f = drill.Fire.BasePoint;
            PlaceRig(new Vector3(f.x, 0f, f.z - 2f)); // scenario setup: standing 2 m from the burning pan
            yield return Xr.Wait(0.3f);
            water.PullSafetyPin();
            water.TrySpray(); // the extinguisher handling itself is VR-tested in V0ExtinguisherTests
            yield return Xr.Wait(0.3f);
            Shot("course_L3_flare_up");
            Assert.AreEqual(GeneratedDrill.State.Failed, drill.Current, "water on burning oil");
            StringAssert.Contains("Dầu bùng", drill.Outcome);
            Assert.IsFalse(Course.Passed(Course.Levels[2].Id));
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator Level5_Night_SituationImposed_WhenTheFireBreaksOut()
        {
            yield return StartLevel(4);
            var runner = Object.FindFirstObjectByType<CourseLevelRunner>();
            Assert.IsNotNull(runner);
            PlaceRig(new Vector3(-0.5f, 0f, -4.5f));
            Body(40f);
            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return Xr.Wait(4f);
            Assert.AreEqual("Accepted", runner.Result, "lights off + smoke surge + stairwell B blocked, through the shared checks");
            yield return Xr.Wait(8f);
            Shot("course_L5_night");
        }
    }
}
