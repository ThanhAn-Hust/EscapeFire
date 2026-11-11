using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V0.9 — a complete drill done ONLY through simulated VR input (head + two controllers, grip / trigger / poke).
    /// The only direct calls set the scenario up (start the fire, Hoa faints); every learner action goes through the
    /// XR rig and XRI interactors exactly as on the headset. Screenshots of each step: Logs/Demo/&lt;time&gt;_xr.
    /// </summary>
    public class V0FullDrillTests : XRSceneTest
    {
        private const float FrontRowZ = -5.6f;
        private const float EastAisleX = 7.0f;

        private Vector3 _eye;
        private float _eyeHeight = 1.6f;

        /// <summary>Walks the learner's body (head + both hands keep their offsets) through waypoints at walking pace.</summary>
        private IEnumerator WalkTo(float speed, params Vector3[] points)
        {
            foreach (var target in points)
            {
                Vector3 goal = new Vector3(target.x, _eyeHeight, target.z);
                while (Vector3.Distance(_eye, goal) > 0.02f)
                {
                    Vector3 step = Vector3.MoveTowards(_eye, goal, speed * Time.deltaTime) - _eye;
                    Vector3 l = Xr.HandPosition(Hand.Left) + step, r = Xr.HandPosition(Hand.Right) + step;
                    _eye += step;
                    Vector3 fwd = Flat(goal - _eye);
                    Xr.SetHead(_eye, Quaternion.LookRotation(fwd));
                    Xr.SetHand(Hand.Left, l, Xr.HandRotation(Hand.Left));
                    Xr.SetHand(Hand.Right, r, Xr.HandRotation(Hand.Right));
                    yield return null;
                }
            }
        }

        private void Look(Vector3 target) => Xr.LookAt(_eye, target);

        private IEnumerator SetEyeHeight(float h)
        {
            float from = _eyeHeight;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                _eyeHeight = Mathf.Lerp(from, h, t / 0.5f);
                _eye.y = _eyeHeight;
                Xr.SetHead(_eye, Xr.Camera.transform.rotation);
                yield return null;
            }
            _eyeHeight = h;
            _eye.y = h;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator FullDrill_ThroughVRInputOnly()
        {
            yield return LoadLab(TrainingMode.Guided);
            var c = LabProgressiveScenarioController.Instance;
            var alarm = Object.FindFirstObjectByType<FireAlarmButton>();
            var breaker = Object.FindFirstObjectByType<CircuitBreakerSwitch>();
            var checker = Object.FindFirstObjectByType<DoorHeatChecker>();
            var exitDoor = checker.GetComponent<ExitDoor>();
            FireExtinguisher co2 = null;
            foreach (var e in Object.FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None)) if (e.Type == ExtinguisherType.CO2) co2 = e;
            NPCSimulator hoa = null;
            foreach (var n in Object.FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None)) if (n.Role == NPCRole.HoaFaint) hoa = n;
            var fire = c.ServerFire;

            // ---- start: standing in the lab, fire breaks out (scenario)
            _eye = new Vector3(-0.5f, _eyeHeight, -4.5f);
            yield return StandAt(_eye, new Vector3(6f, 1.2f, 4f));
            c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return Xr.Wait(1.5f);
            Shot("01_fire_breaks_out");

            // ---- 1. raise the alarm: walk to the call point, press it with the fingertip
            Vector3 a = alarm.transform.position;
            yield return WalkTo(1.3f, new Vector3(a.x, 0f, a.z + 0.6f));
            Look(a);
            Vector3 face = alarm.GetComponent<Collider>().ClosestPoint(a + Vector3.forward);
            yield return Poke(Hand.Right, face, Vector3.back, 0.015f);
            Shot("02_call_point_pressed");
            Assert.IsTrue(alarm.IsActivated, "alarm raised with a fingertip press");

            // ---- 2. take the CO2 extinguisher off its bracket (crouch, grip the carry handle, lift, pull out)
            Transform handle = co2.transform.Find("CarryHandle");
            yield return WalkTo(1.3f, new Vector3(handle.position.x, 0f, handle.position.z + 0.55f));
            yield return SetEyeHeight(1.35f);
            Look(handle.position);
            yield return Xr.MoveHand(Hand.Right, handle.position, handle.rotation, 0.5f);
            Xr.Grip(Hand.Right, true);
            yield return Xr.Wait(0.2f);
            yield return Xr.MoveHand(Hand.Right, handle.position + Vector3.up * 0.07f, handle.rotation, 0.4f);
            Quaternion body = Quaternion.LookRotation(Vector3.forward);
            yield return Xr.MoveHand(Hand.Right, _eye + body * new Vector3(0.15f, -0.5f, 0.3f), body, 0.6f);
            yield return Xr.Wait(0.4f);
            Shot("03_extinguisher_in_hand");

            // ---- 3. pull the pin with the other hand while holding the bottle
            Transform pin = co2.transform.Find("SafetyPin");
            Vector3 ring = pin.position + co2.transform.right * 0.035f;
            yield return GripAt(Hand.Left, ring, 0.5f);
            for (float d = 0f; d <= 0.09f; d += 0.01f) { Xr.SetHand(Hand.Left, ring + co2.transform.right * d, Xr.HandRotation(Hand.Left)); yield return Xr.Wait(0.03f); }
            yield return Release(Hand.Left);
            Shot("04_pin_pulled");
            Assert.IsTrue(co2.IsPinPulled, "pin pulled by the second hand");
            yield return SetEyeHeight(1.6f);

            // ---- 4. cut the power: walk to the panel, open its door, pull the lever down (left hand; right keeps the bottle)
            Vector3 bp = breaker.transform.position;
            yield return WalkTo(1.3f, new Vector3(1.5f, 0f, FrontRowZ), new Vector3(EastAisleX, 0f, FrontRowZ), new Vector3(EastAisleX, 0f, -1.0f), new Vector3(bp.x - 0.7f, 0f, bp.z - 0.3f));
            Look(bp);
            var door = breaker.transform.Find("EF_PanelDoorHinge").GetComponent<HingeGrab>();
            var lever = breaker.transform.Find("EF_BreakerLever").GetComponent<HingeGrab>();
            yield return GripAt(Hand.Left, door.transform.Find("GrabPoint").position, 0.5f);
            yield return ArcMove(Hand.Left, door.transform.position, Vector3.up, 100f * Mathf.Sign(DoorMax(door)), 1.0f);
            yield return Release(Hand.Left);
            yield return GripAt(Hand.Left, lever.transform.Find("Arm/GrabPoint").position, 0.5f);
            yield return ArcMove(Hand.Left, lever.transform.position, lever.transform.right, 75f, 0.8f);
            yield return Release(Hand.Left);
            Shot("05_power_cut");
            Assert.IsFalse(breaker.IsPowerOn, "power cut at the breaker with a full lever pull");

            // ---- 5. PASS: take the horn, approach to ~1.8 m crouched, aim at the base, squeeze, sweep
            Transform horn = co2.transform.Find("Horn");
            yield return GripAt(Hand.Left, horn.position - horn.forward * 0.05f, 0.4f);
            Vector3 basePt = fire.BasePoint;
            yield return WalkTo(1.0f, new Vector3(EastAisleX, 0f, 1.6f));
            yield return SetEyeHeight(1.2f);
            Look(basePt);
            Quaternion toFire = Quaternion.LookRotation(Flat(basePt - _eye));
            Vector3 hornHand = _eye + toFire * new Vector3(-0.15f, -0.35f, 0.4f);
            yield return Xr.MoveHand(Hand.Right, _eye + toFire * new Vector3(0.12f, -0.5f, 0.25f), toFire, 0.4f);
            yield return Xr.MoveHand(Hand.Left, hornHand, Quaternion.LookRotation(basePt - hornHand), 0.4f);
            Xr.Trigger(Hand.Right, 1f);
            Quaternion aim = Quaternion.LookRotation(basePt - hornHand);
            bool shot = false;
            for (float t = 0f; t < 12f && !fire.IsExtinguished; t += Time.deltaTime)
            {
                Xr.SetHand(Hand.Left, hornHand, Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI) * 18f, Vector3.up) * aim);
                if (!shot && t > 1.2f) { Shot("06_spraying_at_the_base"); shot = true; }
                yield return null;
            }
            Xr.Trigger(Hand.Right, 0f);
            Assert.IsTrue(fire.IsExtinguished, "fire put out with PASS");
            yield return Xr.Wait(0.5f);
            Shot("07_fire_out");
            yield return Release(Hand.Left);

            // ---- 6. put the extinguisher down, help Hoa up and support her out
            yield return Xr.MoveHand(Hand.Right, new Vector3(_eye.x, 0.5f, _eye.z) + toFire * new Vector3(0.3f, 0f, 0.3f), Xr.HandRotation(Hand.Right), 0.5f);
            yield return Release(Hand.Right);
            if (!hoa.IsFainted) hoa.TriggerEmergencyReaction(); // scenario: she has collapsed in the smoke
            var grip = hoa.GetComponent<EscortGrip>();
            yield return Xr.Wait(0.3f);
            Vector3 gp = grip.GripPoint;
            yield return SetEyeHeight(1.6f);
            yield return WalkTo(1.2f, new Vector3(EastAisleX, 0f, gp.z - 0.3f));
            yield return SetEyeHeight(0.95f);
            Look(gp);
            yield return GripAt(Hand.Right, grip.GripPoint, 0.6f);
            yield return Xr.Wait(0.4f);
            Shot("08_helping_hoa_up");
            Assert.IsTrue(hoa.IsRescued && grip.IsSupported, "Hoa helped up by the arm");
            yield return SetEyeHeight(1.15f);
            Vector3 doorPt = exitDoor.transform.position;
            yield return WalkTo(0.6f, new Vector3(EastAisleX, 0f, FrontRowZ), new Vector3(doorPt.x + 0.45f, 0f, doorPt.z + 0.75f));
            Shot("09_leading_hoa_to_the_door");
            Debug.Log($"[FullDrill] at door: hoa {hoa.transform.position} supported={grip.IsSupported} eye={_eye}");
            Assert.IsTrue(hoa.gameObject.activeInHierarchy, "Hoa does not walk through a closed door");

            // ---- 7. back of the (left) hand on the door for a second, then turn the handle and push
            Look(doorPt + Vector3.up * 0.2f);
            Vector3 surf = checker.GetComponent<Collider>().ClosestPoint(doorPt + Vector3.forward + Vector3.up * 0.1f - Vector3.right * 0.3f);
            Quaternion backToDoor = Quaternion.LookRotation(Vector3.Cross(Vector3.forward, Vector3.up), Vector3.up); // left palm = +right, back = -right = into door
            yield return Xr.MoveHand(Hand.Left, surf + Vector3.forward * 0.03f, backToDoor, 0.5f);
            yield return Xr.Wait(1.3f);
            Shot("10_back_of_hand_on_door");
            Assert.IsTrue(checker.HasCheckedHeat, "door checked with the back of the hand");
            var leftHinge = exitDoor.transform.parent.Find("ExitDoor_HingeLeft");
            var hp = leftHinge.Find("EF_HandlePivotIn");
            yield return GripAt(Hand.Left, hp.Find("GrabPoint").position, 0.4f);
            yield return ArcMove(Hand.Left, hp.position, hp.forward, 40f, 0.5f);
            yield return ArcMove(Hand.Left, leftHinge.position, Vector3.up, 95f, 1.0f);
            yield return Release(Hand.Left);
            Shot("11_door_open");
            Assert.IsTrue(exitDoor.IsOpen, "door opened: handle turned, leaf pushed");

            // ---- 8. through the door into the floor-3 corridor (Hoa follows out of the lab), walk up to stairwell A
            yield return WalkTo(0.7f, new Vector3(0.3f, 0f, doorPt.z + 0.4f), new Vector3(0.3f, 0f, -8.2f), new Vector3(0.2f, 0f, -10f));
            yield return SetEyeHeight(1.6f);
            yield return Xr.Wait(0.5f);
            Look(new Vector3(0f, 1f, -5f));
            yield return Xr.Wait(0.2f);
            Shot("12_out_in_the_corridor");
            Assert.AreNotEqual(ScenarioPhase.Phase4_CompletedWin, c.CurrentPhase, "the corridor is not safe yet: the drill ends at the assembly point");
            Assert.IsTrue(!hoa.gameObject.activeInHierarchy || hoa.transform.position.z < exitDoor.transform.position.z,
                $"Hoa came out through the open door with the rescuer (at {hoa.transform.position})");

            // ---- 9. stairs, not the lift: push the fire door's bar, then thumbstick down two storeys (gravity on the flights)
            yield return Release(Hand.Right); // let go of Hoa's arm (she walks on with the others)
            var building = GameObject.Find("[BUILDING_V2]").transform;
            var fireDoor = building.Find("StairwellA/FireDoor_F3").GetComponent<PushBarDoor>();
            yield return WalkTo(1.0f, new Vector3(0.2f, 0f, -12.4f));
            yield return PushBar(fireDoor);
            Shot("13_fire_door_pushed");
            Assert.IsTrue(fireDoor.IsOpen, "fire door opened with the push bar");
            Vector3 W(float x, float z) => new Vector3(-x, 0f, -13.2f - z); // stairwell A local -> world
            yield return StickWalk(W(0f, 0.7f), W(-0.65f, 1.2f), W(-0.65f, 2.2f));
            yield return TurnHead(W(-0.65f, 4.2f) + Vector3.down * 2.5f);
            Shot("14_going_down_the_stairs");
            yield return StickWalk(W(-0.65f, 4.2f), W(0f, 4.75f), W(0.65f, 4.2f), W(0.65f, 1.2f), W(0f, 0.6f), W(-0.65f, 1.2f), W(-0.65f, 4.2f), W(0f, 4.75f), W(0.65f, 4.2f), W(0.65f, 1.2f), W(0.6f, 0.7f));
            Assert.AreEqual(-8f, Xr.Origin.transform.position.y, 0.35f, "walked down two storeys");
            Assert.IsFalse(ElevatorTrap.ElevatorUsedThisSession, "never touched the lift");

            // ---- 10. out through the ground-floor exit to the assembly point
            var groundExit = building.Find("StairwellA/ExitDoor_Ground").GetComponent<PushBarDoor>();
            yield return PushBar(groundExit);
            Assert.IsTrue(groundExit.IsOpen, "ground exit pushed open");
            yield return StickWalk(W(2.4f, 0.7f), new Vector3(-6f, 0f, -18f), new Vector3(-7f, 0f, -22.5f));
            yield return TurnHead(new Vector3(-2f, -5f, -14f));
            Shot("15_safe_at_the_assembly_point");
            Assert.AreEqual(ScenarioPhase.Phase4_CompletedWin, c.CurrentPhase, "reaching the assembly point wins the drill");
            Debug.Log($"[FullDrill] hoa at {hoa.transform.position} active={hoa.gameObject.activeInHierarchy} supported={grip.IsSupported} rescued={hoa.IsRescued}");
            Debug.Log($"[FullDrill] passed={SessionResult.Passed} score={(SessionResult.Scorecard.Count > 0 ? AfterActionReport.Total(SessionResult.Scorecard) : -1)} handrail={Handrail.SecondsTouched:F1}s");
        }

        /// <summary>
        /// Thumbstick locomotion (left stick, head-relative): the head stays where it is in the play area and turns
        /// towards each waypoint; the rig moves (XRI move provider + gravity). Hands stay in front of the body.
        /// </summary>
        private IEnumerator StickWalk(params Vector3[] waypoints)
        {
            Vector3 track = Xr.TrackingSpace.InverseTransformPoint(Xr.HeadPosition);
            foreach (var wp in waypoints)
            {
                float t = 0f;
                while (true)
                {
                    Vector3 head = Xr.Camera.transform.position;
                    Vector3 d = wp - head; d.y = 0f;
                    if (d.magnitude < 0.25f) break;
                    t += Time.deltaTime;
                    Assert.Less(t, 20f, $"stuck walking to {wp} (head at {head})");
                    float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    Quaternion q = Quaternion.Euler(0f, yaw, 0f);
                    Xr.SetHeadTracking(track, yaw);
                    Xr.SetHandTracking(Hand.Left, track + Xr.TrackingSpace.InverseTransformDirection(q * new Vector3(-0.2f, -0.5f, 0.25f)), q);
                    Xr.SetHandTracking(Hand.Right, track + Xr.TrackingSpace.InverseTransformDirection(q * new Vector3(0.2f, -0.5f, 0.25f)), q);
                    Xr.Thumbstick(Hand.Left, new Vector2(0f, 1f));
                    yield return null;
                }
            }
            Xr.Thumbstick(Hand.Left, Vector2.zero);
            yield return Xr.Wait(0.2f);
            _eye = Xr.HeadPosition;
            _eyeHeight = _eye.y - Xr.Origin.transform.position.y;
        }

        /// <summary>Turns the head (yaw + pitch) towards a point without moving it in the play area.</summary>
        private IEnumerator TurnHead(Vector3 target)
        {
            Vector3 d = target - Xr.HeadPosition;
            Xr.SetHeadTracking(Xr.TrackingSpace.InverseTransformPoint(Xr.HeadPosition), Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg,
                -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg);
            yield return Xr.Wait(0.4f);
        }

        /// <summary>Right palm pushes a fire door's bar 4 cm in and holds a moment.</summary>
        private IEnumerator PushBar(PushBarDoor door)
        {
            Collider bar = null;
            foreach (var col in door.GetComponentsInChildren<Collider>(true)) if (col.name == "PushBar") bar = col;
            Vector3 push = door.transform.forward;
            Vector3 face = bar.bounds.center - push * 0.03f;
            Quaternion rot = Quaternion.LookRotation(push);
            Xr.LookAt(Xr.HeadPosition, bar.bounds.center);
            yield return Xr.MoveHand(Hand.Right, face - push * 0.15f, rot, 0.3f);
            yield return Xr.MoveHand(Hand.Right, face + push * 0.04f, rot, 0.3f);
            yield return Xr.Wait(0.6f);
        }

        private static float DoorMax(HingeGrab h) =>
            (float)typeof(HingeGrab).GetField("maxAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(h);
    }
}
