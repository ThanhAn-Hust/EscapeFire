using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using EscapeFire.Core;
using EscapeFire.Player;
using EscapeFire.Scenario;
using EscapeFire.UI;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V5.5 — VR comfort: every scene's rig has the tunnelling vignette while moving and snap (not smooth) turning;
    /// walking down the stairs does not jolt the head; the right stick turns in fixed steps; the menu's "confirm height"
    /// button, pressed by the fingertip while standing, calibrates the crouch threshold to this learner.
    /// </summary>
    public class V5ComfortTests : BuildingSceneTest
    {
        private static readonly string[] Scenes =
            { SceneNames.MAIN_MENU, SceneNames.LESSON, SceneNames.LAB, SceneNames.AAR, SceneNames.GENERATED };

        [UnityTest, Timeout(300000)]
        public IEnumerator EveryScene_Rig_VignetteAndSnapTurn()
        {
            Application.targetFrameRate = 72;
            var report = new List<string>();
            foreach (var scene in Scenes)
            {
                if (scene == SceneNames.GENERATED) // scenario setup: the room the menu would pick first
                    EscapeFire.Generation.GeneratedScenario.Pending =
                        Newtonsoft.Json.Linq.JObject.Parse(Resources.Load<TextAsset>("Scenarios/library").text);
                yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
                yield return null;
                yield return null;
                var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                Assert.IsNotNull(origin, scene + ": XR rig");
                var vignette = origin.GetComponentInChildren<TunnelingVignetteController>(true);
                Assert.IsNotNull(vignette, scene + ": tunnelling vignette");
                Assert.That(vignette.locomotionVignetteProviders.Any(p => p.enabled && p.locomotionProvider != null),
                    scene + ": the vignette follows the movement providers");
                var snap = origin.GetComponentInChildren<SnapTurnProvider>(true);
                Assert.IsNotNull(snap, scene + ": snap turn");
                Assert.IsTrue(snap.isActiveAndEnabled, scene + ": snap turn on");
                Assert.That(snap.turnAmount, Is.InRange(30f, 45f), scene + ": snap step");
                // the starter rig keeps both turn providers; its input manager decides which stick action is live
                foreach (var m in origin.GetComponentsInChildren<MonoBehaviour>(true).Where(m => m.GetType().Name == "ControllerInputActionManager"))
                {
                    var prop = m.GetType().GetProperty("smoothTurnEnabled");
                    Assert.IsFalse((bool)prop.GetValue(m), scene + ": smooth turning off (motion sickness) on " + m.name);
                }
                report.Add($"{scene}: vignette providers {vignette.locomotionVignetteProviders.Count}, snap {snap.turnAmount}°");
            }
            Debug.Log("[Comfort] " + string.Join("; ", report));
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Stairs_DownOneFlight_NoHeadJolts()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -12.4f), 180f);
            Application.targetFrameRate = 72;
            yield return PushBar(Door("StairwellA", "FireDoor_F3"));
            Body(180f);
            Vector3 W(float x, float z) => new Vector3(-x, 0f, -13.2f - z);
            yield return StickWalk(W(0f, 0.7f), W(-0.65f, 1.2f));

            // down the first flight, sampling the head height every frame
            var dy = new List<float>();
            float startY = Xr.Camera.transform.position.y, last = startY;
            Vector3 target = W(-0.65f, 4.2f);
            for (float t = 0f; ; t += Time.deltaTime)
            {
                Vector3 d = target - Xr.Camera.transform.position; d.y = 0f;
                if (d.magnitude < 0.25f) break;
                Assert.Less(t, 20f, "stuck on the stairs");
                Body(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
                Xr.Thumbstick(Hand.Left, new Vector2(0f, 1f));
                yield return null;
                float y = Xr.Camera.transform.position.y;
                dy.Add(Mathf.Abs(y - last));
                last = y;
            }
            Xr.Thumbstick(Hand.Left, Vector2.zero);
            float drop = startY - last;
            float max = dy.Max();
            int jolts = dy.Count(v => v > 0.06f);
            Debug.Log($"[Comfort] stairs: dropped {drop:F2} m over {dy.Count} frames, max per-frame {max * 100f:F1} cm, jolts>6cm {jolts}");
            Assert.Greater(drop, 1.2f, "went down a flight");
            Assert.Less(max, 0.08f, "no sudden head drop on a step");
            Assert.LessOrEqual(jolts, 2, "the descent is smooth, not step by step");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator RightStick_TurnsInSnapSteps()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -8.0f), 0f);
            var snap = Xr.Origin.GetComponentInChildren<SnapTurnProvider>();
            float before = Xr.Origin.transform.eulerAngles.y;
            Xr.Thumbstick(Hand.Right, new Vector2(1f, 0f));
            yield return Xr.Wait(0.15f);
            Xr.Thumbstick(Hand.Right, Vector2.zero);
            yield return Xr.Wait(0.3f);
            float turned = Mathf.DeltaAngle(before, Xr.Origin.transform.eulerAngles.y);
            Debug.Log($"[Comfort] snap turn: {turned:F1}° (step {snap.turnAmount}°)");
            Assert.AreEqual(snap.turnAmount, turned, 1f, "one snap step to the right, not a smooth spin");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Menu_ConfirmHeight_ByFingertip_CalibratesCrouch()
        {
            Application.targetFrameRate = 72;
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "XRTEST");
            yield return SceneManager.LoadSceneAsync(SceneNames.MAIN_MENU, LoadSceneMode.Single);
            yield return null;
            yield return null;
            Xr = XRInputDriver.Create();
            PlayAreaSpot = Vector3.zero;
            yield return Xr.Wait(0.3f);

            UIButtonPoke button = Object.FindObjectsByType<UIButtonPoke>(FindObjectsSortMode.None).First(b => b.name.Contains("XÁC NHẬN CHIỀU CAO"));
            Vector3 into = button.transform.forward;
            Vector3 stand = button.transform.position - Flat(into) * 0.45f;
            yield return StickWalk(new Vector3(stand.x, 0f, stand.z));
            // a tall learner, standing straight
            const float Tall = 1.82f;
            Body(Mathf.Atan2(into.x, into.z) * Mathf.Rad2Deg, Tall, 15f);
            yield return Xr.Wait(0.2f);
            Vector3 face = button.GetComponent<Collider>().ClosestPoint(button.transform.position - into * 0.2f);
            Quaternion rot = Quaternion.LookRotation(into, Vector3.up);
            Xr.SetHand(Hand.Right, face - into * 0.25f, rot);
            yield return Xr.Wait(0.05f);
            var tip = PokeTip(Hand.Right);
            Vector3 tipOffset = tip != null ? tip.position - Xr.HandPosition(Hand.Right) : Vector3.zero;
            int presses = button.Presses;
            Xr.SetHand(Hand.Right, face - into * 0.06f - tipOffset, rot);
            yield return Xr.Wait(0.1f);
            yield return Xr.MoveHand(Hand.Right, face + into * 0.015f - tipOffset, rot, 0.4f);
            yield return Xr.Wait(0.2f);
            Assert.Greater(button.Presses, presses, "fingertip pressed the button");
            Assert.IsTrue(GameSession.IsCalibrated, "height confirmed");
            Debug.Log($"[Comfort] calibrated H0 = {GameSession.CalibratedHeight:F2} m");
            Assert.AreEqual(Tall, GameSession.CalibratedHeight, 0.05f, "the learner's own standing height");
            // a tall learner at 1.40 m is already crouching; with the default threshold they would not be
            Assert.AreEqual(Posture.Standing, PostureRules.Classify(1.40f, 0f), "default threshold");
            Assert.AreNotEqual(Posture.Standing, PostureRules.Classify(1.40f, GameSession.CalibratedHeight), "calibrated threshold");
        }
    }
}
