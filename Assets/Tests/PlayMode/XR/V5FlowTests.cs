using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Generation;
using EscapeFire.Scenario;
using EscapeFire.UI;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V5.1 — the whole game flow with simulated VR input only: every button is pressed with the fingertip and every
    /// scene change is real (Menu → Lesson → Menu → Lab → AAR → Menu → generated room → AAR → retry). The content of each
    /// screen has its own tests (V1 lesson, V0–V3 full drill, V4 rooms); here the shortcuts are marked as scenario setup.
    /// </summary>
    public class V5FlowTests : BuildingSceneTest
    {
        private readonly System.Collections.Generic.List<string> _problems = new System.Collections.Generic.List<string>();

        /// <summary>V5.3: errors, exceptions and the warnings that point at real problems, seen during the whole flow.</summary>
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                if (message.Contains("haptic")) return; // simulated controllers have no haptics
                _problems.Add(type + ": " + message);
            }
            else if (type == LogType.Warning && (message.Contains("multiple active and enabled XR Interaction Manager") || message.Contains("was not found in the")))
                _problems.Add("Warning: " + message.Split('\n')[0]);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator WholeFlow_FingertipOnly_NoDeadEnds()
        {
            _problems.Clear();
            Application.logMessageReceived += OnLog;
            try
            {
            yield return WholeFlow();
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
                Course.ResetAll();
                Course.Current = null;
            }
            Assert.IsEmpty(_problems, "console during the flow:\n" + string.Join("\n", _problems));
        }

        private IEnumerator WholeFlow()
        {
            Application.targetFrameRate = 72;
            ProgressionStore.KeyPrefix = "EF_test_progress_";
            ProgressionStore.ResetAll();
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "XRTEST");
            SkillStore.Clear();
            yield return SceneManager.LoadSceneAsync(SceneNames.MAIN_MENU, LoadSceneMode.Single);
            yield return WaitForScene(SceneNames.MAIN_MENU);
            yield return FaceBoard();
            Shot("flow_01_menu");

            // Menu -> lesson room
            yield return PressUi("1. BÀI GIẢNG");
            yield return WaitForScene(SceneNames.LESSON);
            Shot("flow_02_lesson");

            // lesson -> back to the menu (the lesson stations themselves: V1LessonTests)
            var back = GameObject.Find("BackToMenuButton");
            Assert.IsNotNull(back, "the lesson room has a way back");
            Vector3 top = back.GetComponent<Collider>().bounds.center + Vector3.up * back.GetComponent<Collider>().bounds.extents.y;
            yield return StickWalk(new Vector3(top.x, 0f, top.z + 0.45f));
            Body(180f, 1.5f, 45f);
            yield return PressAndHold(Hand.Right, top, Vector3.down);
            yield return WaitForScene(SceneNames.MAIN_MENU);
            ProgressionStore.LessonDone = true; // scenario setup: lesson finished (covered by V1LessonTests)
            Course.ResetAll();
            for (int i = 0; i < 3; i++) { Course.Current = Course.Levels[i]; Course.Record(100, true); } // scenario setup: levels 1-3 passed (V6CourseTests)
            Course.Current = null;

            // Menu -> lab drill -> AAR
            yield return PressUi("2. KHOÁ HỌC");
            yield return PressUi("MÀN 4");
            yield return PressUi("BẮT ĐẦU MÀN NÀY");
            yield return WaitForScene(SceneNames.LAB);
            Shot("flow_03_lab");
            var c = LabProgressiveScenarioController.Instance;
            c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return Xr.Wait(0.5f);
            c.Fail("Kiểm tra luồng: kết thúc ván."); // scenario shortcut: the drill itself is V0FullDrillTests
            yield return WaitForScene(SceneNames.AAR);
            yield return FaceBoard();
            Shot("flow_04_aar_after_lab");
            Assert.AreEqual(SceneNames.LAB, SessionResult.DrillScene);

            // AAR -> menu
            yield return PressUi("VỀ MENU CHÍNH");
            yield return WaitForScene(SceneNames.MAIN_MENU);

            // Menu -> a generated room -> leave by the EXIT -> AAR
            yield return PressUi("4. THỬ THÁCH PHÒNG LẠ");
            yield return WaitForScene(SceneNames.GENERATED);
            var builder = Object.FindFirstObjectByType<ScenarioBuilder>();
            Assert.IsTrue(builder.Built, string.Join(",", builder.Errors));
            Shot("flow_05_generated_room");
            var exit = ScenarioBuilder.ExitBounds(builder.Layout.Exits[0]).center;
            yield return StickWalk(new Vector3(builder.Layout.Exits[0].X, 0f, builder.Layout.Exits[0].Z), new Vector3(exit.x, 0f, exit.z));
            Assert.AreEqual(GeneratedDrill.State.Won, builder.GetComponent<GeneratedDrill>().Current);
            yield return WaitForScene(SceneNames.AAR);
            yield return FaceBoard();
            Shot("flow_06_aar_after_generated_room");
            Assert.AreEqual(SceneNames.GENERATED, SessionResult.DrillScene);
            Assert.That(SessionResult.Scorecard.Exists(i => i.Key == "evacuation" && i.Points == i.MaxPoints), "left by the exit");

            // AAR -> retry goes back to the generated room, not the lab
            yield return PressUi("LÀM LẠI");
            yield return WaitForScene(SceneNames.GENERATED);
            Assert.IsTrue(Object.FindFirstObjectByType<ScenarioBuilder>().Built);
        }
    }
}
