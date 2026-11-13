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
        private IEnumerator WaitForScene(string name, float timeout = 20f)
        {
            for (float t = 0f; SceneManager.GetActiveScene().name != name; t += Time.unscaledDeltaTime)
            {
                Assert.Less(t, timeout, $"scene {name} never loaded (still in {SceneManager.GetActiveScene().name})");
                yield return null;
            }
            yield return null;
            yield return null;
            Xr?.Dispose(); // the rig of the previous scene is gone
            Xr = XRInputDriver.Create();
            PlayAreaSpot = Vector3.zero;
            yield return Xr.Wait(0.5f);
            Debug.Log($"[Flow] {name}: origin {Xr.Origin.transform.position} yaw {Xr.Origin.transform.eulerAngles.y:F0}, camera {Xr.Camera.transform.position} fwd {Xr.Camera.transform.forward}");
        }

        /// <summary>Learner turns to face the menu / result board (for the screenshot).</summary>
        private IEnumerator FaceBoard()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) yield break;
            Vector3 d = canvas.transform.position - Xr.Camera.transform.position;
            Body(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 1.6f, 5f);
            yield return Xr.Wait(0.3f);
        }

        /// <summary>Walks up to a canvas button and presses it with the right fingertip.</summary>
        private IEnumerator PressUi(string label)
        {
            UIButtonPoke target = null;
            foreach (var b in Object.FindObjectsByType<UIButtonPoke>(FindObjectsSortMode.None))
                if (b.name.Contains(label)) target = b;
            Assert.IsNotNull(target, "button " + label);
            Vector3 into = target.transform.forward; // into the canvas
            Vector3 stand = target.transform.position - Flat(into) * 0.45f;
            yield return StickWalk(new Vector3(stand.x, 0f, stand.z));
            Body(Mathf.Atan2(into.x, into.z) * Mathf.Rad2Deg, 1.6f, 20f);
            yield return Xr.Wait(0.2f);
            int before = target.Presses;
            Vector3 face = target.GetComponent<Collider>().ClosestPoint(target.transform.position - into * 0.2f);
            yield return PressAndHold(Hand.Right, face, into);
            Assert.Greater(target.Presses, before, "fingertip pressed " + label);
        }

        /// <summary>
        /// Fingertip press that stays on the button: these buttons change the scene, so the hand must not move again
        /// afterwards (the rig is destroyed with the old scene).
        /// </summary>
        private IEnumerator PressAndHold(Hand hand, Vector3 surfacePoint, Vector3 into)
        {
            Quaternion rot = Quaternion.LookRotation(into, Vector3.up);
            Xr.SetHand(hand, surfacePoint - into * 0.25f, rot);
            yield return Xr.Wait(0.05f);
            var tip = PokeTip(hand);
            Vector3 tipOffset = tip != null ? tip.position - Xr.HandPosition(hand) : Vector3.zero;
            Vector3 from = surfacePoint - into * 0.06f - tipOffset, to = surfacePoint + into * 0.015f - tipOffset;
            Xr.SetHand(hand, from, rot);
            yield return Xr.Wait(0.1f);
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                if (Xr.Origin == null) yield break; // the press already changed the scene
                Xr.SetHand(hand, Vector3.Lerp(from, to, t / 0.4f), rot);
                yield return null;
            }
            for (int i = 0; i < 3 && Xr.Origin != null; i++) yield return null;
        }

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

            // Menu -> lab drill -> AAR
            yield return PressUi("BẮT ĐẦU BÀI TẬP PHÒNG LAB");
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
            yield return PressUi("4. PHÒNG MỚI");
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
