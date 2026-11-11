using System.Collections;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V2 — floor 3 corridor, push-bar fire doors, elevator decoy, two storeys of stairs walked down with the
    /// thumbstick (real XRI locomotion + gravity on the ramps), out to the assembly point.
    /// </summary>
    public class V2BuildingTests : XRSceneTest
    {
        private static readonly Vector3 HeadLocal = new Vector3(0f, 1.6f, 0f);
        private XROrigin _origin;

        private IEnumerator Setup(TrainingMode mode, Vector3 originPos, float yaw)
        {
            yield return LoadLab(mode);
            Assert.IsNotNull(GameObject.Find("[BUILDING_V2]"), "Run EscapeFire > V2 > Build Building");
            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            _origin = Xr.Origin;
            // Scenario setup: the learner starts this test already in the corridor (not a learner action).
            var cc = _origin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            _origin.transform.SetPositionAndRotation(originPos, Quaternion.identity);
            if (cc != null) cc.enabled = true;
            Body(yaw);
            yield return Xr.Wait(0.3f);
        }

        /// <summary>Head at the centre of the play area looking along <paramref name="yaw"/>, hands relaxed in front.</summary>
        private void Body(float yaw, float headY = 1.6f)
        {
            // Head height above the rig's floor: the camera offset object may sit above the origin (Device mode).
            headY -= Xr.TrackingSpace.position.y - Xr.Origin.transform.position.y;
            Xr.SetHeadTracking(new Vector3(0f, headY, 0f), yaw);
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Vector3 tl = Xr.TrackingSpace.InverseTransformDirection(q * new Vector3(-0.2f, -0.5f, 0.25f));
            Vector3 tr = Xr.TrackingSpace.InverseTransformDirection(q * new Vector3(0.2f, -0.5f, 0.25f));
            Xr.SetHandTracking(Hand.Left, new Vector3(0f, headY, 0f) + tl, q);
            Xr.SetHandTracking(Hand.Right, new Vector3(0f, headY, 0f) + tr, q);
        }

        /// <summary>Walks with the left thumbstick (head-relative) to each waypoint; returns false if stuck.</summary>
        private IEnumerator StickWalk(params Vector3[] waypoints)
        {
            foreach (var wp in waypoints)
            {
                float t = 0f;
                while (true)
                {
                    Vector3 head = Xr.Camera.transform.position;
                    Vector3 d = wp - head;
                    d.y = 0f;
                    if (d.magnitude < 0.25f) break;
                    t += Time.deltaTime;
                    Assert.Less(t, 20f, $"stuck walking to {wp} (head at {head})");
                    Body(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
                    Xr.Thumbstick(Hand.Left, new Vector2(0f, 1f));
                    yield return null;
                }
            }
            Xr.Thumbstick(Hand.Left, Vector2.zero);
            yield return Xr.Wait(0.2f);
        }

        /// <summary>Pushes a door's bar with the right hand (4 cm in) and holds a moment.</summary>
        private IEnumerator PushBar(PushBarDoor door)
        {
            var bar = door.GetComponentInChildren<Collider>(true);
            foreach (var c in door.GetComponentsInChildren<Collider>(true)) if (c.name == "PushBar") bar = c;
            Vector3 push = door.transform.forward;
            Vector3 face = bar.bounds.center - push * 0.03f;
            Quaternion rot = Quaternion.LookRotation(push);
            yield return Xr.MoveHand(Hand.Right, face - push * 0.15f, rot, 0.3f);
            yield return Xr.MoveHand(Hand.Right, face + push * 0.04f, rot, 0.3f);
            yield return Xr.Wait(0.6f);
        }

        private static PushBarDoor Door(string stairwell, string name) =>
            GameObject.Find("[BUILDING_V2]").transform.Find(stairwell + "/" + name).GetComponent<PushBarDoor>();

        /// <summary>Screenshots of the new spaces from good viewpoints (rig placed directly: setup, not a learner action).</summary>
        [UnityTest]
        public IEnumerator Tour_Screenshots()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.6f, 0f, -7.6f), 180f);
            yield return Look(new Vector3(0.6f, 0f, -7.6f), new Vector3(0f, 1.3f, -13f));
            Shot("t1_corridor_exits_and_elevator");
            yield return Look(new Vector3(-1.5f, 0f, -9.6f), new Vector3(-3f, 1.4f, -10.6f));
            Shot("t2_elevator_sign");
            yield return Look(new Vector3(0.0f, 0f, -13.75f), new Vector3(0.65f, -1.2f, -16.0f));
            Shot("t3_top_landing_looking_down");
            yield return Look(new Vector3(0.0f, -2f, -18.0f), new Vector3(-0.65f, -3.5f, -15.0f));
            Shot("t4_mid_landing");
            yield return Look(new Vector3(0.0f, -8f, -14.3f), new Vector3(-1.4f, -7f, -13.9f));
            Shot("t5_ground_exit_door");
            yield return Look(new Vector3(-12f, -8f, -30f), new Vector3(-2f, -5f, -14f));
            Shot("t6_building_and_assembly_point");
        }

        private IEnumerator Look(Vector3 floorPos, Vector3 target)
        {
            var cc = Xr.Origin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            Xr.Origin.transform.position = floorPos;
            if (cc != null) cc.enabled = true;
            Vector3 eye = floorPos + Vector3.up * 1.6f;
            Vector3 d = target - eye;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            float headY = 1.6f - (Xr.TrackingSpace.position.y - Xr.Origin.transform.position.y);
            Xr.SetHeadTracking(new Vector3(0f, headY, 0f), yaw, pitch);
            Xr.SetHandTracking(Hand.Left, new Vector3(-0.3f, headY - 1.0f, 0f), Quaternion.identity);
            Xr.SetHandTracking(Hand.Right, new Vector3(0.3f, headY - 1.0f, 0f), Quaternion.identity);
            yield return Xr.Wait(0.4f);
        }

        [UnityTest]
        public IEnumerator FireDoor_PushBarOpens_ThenClosesItself()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -12.4f), 180f);
            var door = Door("StairwellA", "FireDoor_F3");
            Assert.IsFalse(door.IsOpen);
            yield return PushBar(door);
            Shot("fire_door_pushed_open");
            Assert.IsTrue(door.IsOpen, "pushing the bar opens the fire door");
            yield return StickWalk(new Vector3(0.2f, 0f, -11.2f)); // step back out of the doorway
            Body(0f);
            yield return Xr.Wait(7f);
            Assert.IsFalse(door.IsOpen, "the door closer shuts it again (keeps smoke out of the stairs)");
        }

        [UnityTest]
        public IEnumerator Elevator_CallButton_Penalised()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(-2.3f, 0f, -11.4f), -90f);
            var trap = Object.FindFirstObjectByType<ElevatorTrap>();
            var btn = trap.transform.Find("CallButton");
            Vector3 face = btn.GetComponent<Collider>().ClosestPoint(btn.position + Vector3.right);
            yield return Poke(Hand.Right, face, Vector3.left, 0.015f);
            yield return Xr.Wait(2.5f);
            Shot("elevator_called");
            Assert.IsTrue(trap.Called && ElevatorTrap.ElevatorUsedThisSession, "elevator call recorded");
            Assert.That(EscapeFire.Analytics.ScoringManager.Instance.GetFormattedLogs(), Has.Some.Contains("thang máy"));
        }

        [UnityTest]
        public IEnumerator Elevator_Exam_SteppingIn_Fails()
        {
            yield return Setup(TrainingMode.Exam, new Vector3(-2.3f, 0f, -11.4f), -90f);
            var trap = Object.FindFirstObjectByType<ElevatorTrap>();
            var btn = trap.transform.Find("CallButton");
            Vector3 face = btn.GetComponent<Collider>().ClosestPoint(btn.position + Vector3.right);
            yield return Poke(Hand.Right, face, Vector3.left, 0.015f);
            yield return Xr.Wait(3f);
            Assert.IsTrue(trap.DoorsOpen);
            yield return StickWalk(new Vector3(-2.6f, 0f, -10.5f), new Vector3(-4.0f, 0f, -10.5f));
            yield return Xr.Wait(0.3f);
            Assert.AreEqual(ScenarioPhase.Phase5_GameOverFail, LabProgressiveScenarioController.Instance.CurrentPhase, "stuck in the lift");
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Stairs_DownTwoStoreys_ToAssemblyPoint()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -12.4f), 180f);
            Shot("corridor_to_stairwell_A");
            var door = Door("StairwellA", "FireDoor_F3");
            yield return PushBar(door);
            Body(180f);
            // Stairwell A: door (0,0,-13.2), going -z; local x maps to world -x.
            Vector3 W(float x, float z) => new Vector3(-x, 0f, -13.2f - z);
            yield return StickWalk(W(0f, 0.7f), W(-0.65f, 1.2f));
            Shot("top_of_the_stairs");
            yield return StickWalk(W(-0.65f, 4.2f), W(0f, 4.75f), W(0.65f, 4.2f));
            Shot("mid_landing");
            yield return StickWalk(W(0.65f, 1.2f), W(0f, 0.6f), W(-0.65f, 1.2f), W(-0.65f, 4.2f), W(0f, 4.75f), W(0.65f, 4.2f), W(0.65f, 1.2f), W(0.6f, 0.7f));
            float y = Xr.Origin.transform.position.y;
            Shot("ground_floor_landing");
            Assert.AreEqual(-8f, y, 0.35f, "walked down two storeys on the stairs");
            var exit = Door("StairwellA", "ExitDoor_Ground");
            yield return PushBar(exit);
            Assert.IsTrue(exit.IsOpen, "ground exit pushed open");
            yield return StickWalk(W(2.4f, 0.7f), new Vector3(-6f, 0f, -18f), new Vector3(-7f, 0f, -22.5f));
            yield return Xr.Wait(0.3f);
            Shot("assembly_point");
            Assert.AreEqual(ScenarioPhase.Phase4_CompletedWin, LabProgressiveScenarioController.Instance.CurrentPhase, "reaching the assembly point completes the evacuation");
        }

        [UnityTest]
        public IEnumerator Handrail_HandOnRail_Counts()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -12.4f), 180f);
            var rail = GameObject.Find("[BUILDING_V2]").transform.Find("StairwellA/Handrail_0");
            Handrail.ResetStats();
            yield return Xr.MoveHand(Hand.Left, rail.position, Quaternion.identity, 0.4f);
            yield return Xr.Wait(1.0f);
            Assert.Greater(Handrail.SecondsTouched, 0.5f, "hand resting on the rail is counted");
        }

        [UnityTest]
        public IEnumerator Smoke_LeaksIntoCorridor_OnlyWhileLabDoorOpen()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -9f), 0f);
            var lab = Object.FindFirstObjectByType<SmokeDensityController>();
            typeof(SmokeDensityController).GetProperty("CurrentSmokeHeight").SetValue(lab, 1.2f); // thick smoke in the lab (scenario)
            var corridor = GameObject.Find("[BUILDING_V2]").transform.Find("Smoke_Corridor").GetComponent<SmokeZone>();
            Time.timeScale = 4f;
            yield return new WaitForSeconds(12f);
            float shut = corridor.Thickness;
            Object.FindFirstObjectByType<ExitDoor>().Open(); // scenario: the lab door stands open
            yield return new WaitForSeconds(20f);
            Time.timeScale = 1f;
            float open = corridor.Thickness;
            Assert.Less(shut, 0.05f, "closed lab door keeps the corridor clear");
            Assert.Greater(open, 0.3f, "smoke pours into the corridor through the open door");
        }
    }
}
