using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V0.4–V0.5 / B.1-4, B.1-5 — taking the extinguisher off its bracket and PASS with two hands, only through
    /// simulated controllers. (Starting the fire is scenario setup, not a learner action.)
    /// </summary>
    public class V0ExtinguisherTests : XRSceneTest
    {
        private FireExtinguisher _co2;
        private Transform _handle, _pin, _horn;
        private ExtinguisherMount _mount;
        private FireHazard _fire;

        private IEnumerator Setup(bool startFire = true)
        {
            yield return LoadLab();
            foreach (var e in Object.FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None)) if (e.Type == ExtinguisherType.CO2) _co2 = e;
            _handle = _co2.transform.Find("CarryHandle");
            _pin = _co2.transform.Find("SafetyPin");
            _horn = _co2.transform.Find("Horn");
            _mount = _co2.GetComponent<ExtinguisherMount>();
            Assert.IsNotNull(_handle, "Run EscapeFire > V0 > Setup Realistic Rig");
            var c = LabProgressiveScenarioController.Instance;
            _fire = c.ServerFire;
            if (startFire) c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            Vector3 into = new Vector3(0f, 0f, Mathf.Sign(_co2.transform.position.z));
            Vector3 eye = new Vector3(_handle.position.x, 1.45f, _handle.position.z) - into * 0.55f;
            yield return StandAt(eye, _handle.position);
        }

        private IEnumerator GrabHandle()
        {
            yield return Xr.MoveHand(Hand.Right, _handle.position, _handle.rotation, 0.5f);
            Xr.Grip(Hand.Right, true);
            yield return Xr.Wait(0.2f);
        }

        private IEnumerator LiftOffBracket()
        {
            yield return GrabHandle();
            yield return Xr.MoveHand(Hand.Right, Xr.HandPosition(Hand.Right) + Vector3.up * 0.07f, Xr.HandRotation(Hand.Right), 0.4f);
            Assert.IsFalse(_mount.IsMounted, "lifting 7 cm takes the bottle off its hook");
        }

        private IEnumerator PullPin()
        {
            // Bring the bottle in front of the body first, like a real person does.
            Vector3 eye = Xr.HeadPosition;
            Quaternion bodyRot = Quaternion.LookRotation(Flat(Xr.Camera.transform.forward));
            yield return Xr.MoveHand(Hand.Right, eye + bodyRot * new Vector3(0.1f, -0.45f, 0.35f), bodyRot, 0.6f);
            yield return Xr.Wait(0.4f); // bottle eases into the hand (heavy)
            Vector3 axis = _co2.transform.right;
            Vector3 ring = _pin.position + axis * 0.035f;
            yield return Xr.MoveHand(Hand.Left, ring, Xr.HandRotation(Hand.Left), 0.5f);
            Xr.Grip(Hand.Left, true);
            yield return Xr.Wait(0.15f);
            for (float d = 0f; d <= 0.09f; d += 0.01f) { Xr.SetHand(Hand.Left, ring + _co2.transform.right * d, Xr.HandRotation(Hand.Left)); yield return Xr.Wait(0.03f); }
            Xr.Grip(Hand.Left, false);
            yield return Xr.Wait(0.2f);
        }

        /// <summary>Walks up to ~1.8 m from the fire base with the bottle, takes the horn and aims at the base.</summary>
        private IEnumerator TakeHornAndApproach(float distance, float aimHeightOffset)
        {
            Vector3 basePt = _fire.BasePoint;
            Vector3 toFire = Flat(basePt - Xr.HeadPosition);
            Vector3 stand = basePt - toFire * distance;
            Vector3 eye = new Vector3(stand.x, 1.35f, stand.z);
            Quaternion body = Quaternion.LookRotation(toFire);
            Xr.LookAt(eye, basePt);
            Xr.SetHand(Hand.Right, eye + body * new Vector3(0.12f, -0.5f, 0.25f), body);
            yield return Xr.Wait(0.5f);
            Vector3 hornGrip = _horn.position - _horn.forward * 0.05f;
            yield return Xr.MoveHand(Hand.Left, hornGrip, _horn.rotation, 0.4f);
            Xr.Grip(Hand.Left, true);
            yield return Xr.Wait(0.15f);
            Vector3 aimPt = basePt + Vector3.up * aimHeightOffset;
            Vector3 hand = eye + body * new Vector3(-0.15f, -0.4f, 0.4f);
            yield return Xr.MoveHand(Hand.Left, hand, Quaternion.LookRotation(aimPt - hand), 0.5f);
            Assert.IsTrue(_horn.GetComponent<ExtinguisherHorn>().IsHeld, "the second hand holds the horn");
        }

        private IEnumerator SprayFor(float seconds, bool sweep, float trigger = 1f)
        {
            Vector3 hand = Xr.HandPosition(Hand.Left);
            Quaternion aim = Xr.HandRotation(Hand.Left);
            Xr.Trigger(Hand.Right, trigger);
            for (float t = 0f; t < seconds && !_fire.IsExtinguished; t += Time.deltaTime)
            {
                float yaw = sweep ? Mathf.Sin(t * Mathf.PI) * 18f : 0f; // ±18° at 0.5 Hz ≈ 57°/s peak
                Xr.SetHand(Hand.Left, hand, Quaternion.AngleAxis(yaw, Vector3.up) * aim);
                yield return null;
            }
            Xr.Trigger(Hand.Right, 0f);
            yield return Xr.Wait(0.1f);
        }

        [UnityTest]
        public IEnumerator Bracket_PullStraightOut_StaysOnHook()
        {
            yield return Setup(false);
            Vector3 start = _co2.transform.position;
            yield return GrabHandle();
            Vector3 away = new Vector3(0f, 0f, -Mathf.Sign(start.z)) * 0.25f;
            yield return Xr.MoveHand(Hand.Right, Xr.HandPosition(Hand.Right) + away, Xr.HandRotation(Hand.Right), 0.4f);
            Shot("pull_without_lift");
            Assert.IsTrue(_mount.IsMounted);
            Assert.Less(Vector3.Distance(start, _co2.transform.position), 0.02f, "a hooked bottle does not come straight out");
        }

        [UnityTest]
        public IEnumerator Bracket_LiftThenPull_Detaches()
        {
            yield return Setup(false);
            yield return LiftOffBracket();
            Vector3 lifted = _co2.transform.position;
            yield return Xr.MoveHand(Hand.Right, Xr.HandPosition(Hand.Right) + new Vector3(0f, 0f, -Mathf.Sign(lifted.z)) * 0.3f, Xr.HandRotation(Hand.Right), 0.4f);
            yield return Xr.Wait(0.4f);
            Shot("bottle_in_hand");
            Assert.Greater(Vector3.Distance(lifted, _co2.transform.position), 0.2f, "after lifting it follows the hand");
        }

        [UnityTest]
        public IEnumerator Pin_BottleStillOnWall_CannotBePulled()
        {
            yield return Setup(false);
            Vector3 ring = _pin.position + _co2.transform.right * 0.035f;
            yield return Xr.MoveHand(Hand.Left, ring, Quaternion.identity, 0.5f);
            Xr.Grip(Hand.Left, true);
            for (float d = 0f; d <= 0.1f; d += 0.01f) { Xr.SetHand(Hand.Left, ring + _co2.transform.right * d, Quaternion.identity); yield return Xr.Wait(0.03f); }
            Assert.IsFalse(_co2.IsPinPulled, "the pin is pulled with the bottle held in the other hand");
        }

        [UnityTest]
        public IEnumerator Squeeze_Light_NoDischarge()
        {
            yield return Setup();
            yield return LiftOffBracket();
            yield return PullPin();
            Assert.IsTrue(_co2.IsPinPulled);
            Xr.Trigger(Hand.Right, 0.4f);
            yield return Xr.Wait(0.3f);
            Assert.IsFalse(_co2.IsSpraying, "a light touch on the lever does not open the valve");
            Xr.Trigger(Hand.Right, 0.9f);
            yield return Xr.Wait(0.3f);
            Assert.IsTrue(_co2.IsSpraying, "squeezing the lever discharges");
            Xr.Trigger(Hand.Right, 0f);
            yield return Xr.Wait(0.2f);
            Assert.IsFalse(_co2.IsSpraying, "releasing stops at once");
        }

        [UnityTest]
        public IEnumerator PASS_TwoHands_SweepAtBase_Extinguishes()
        {
            yield return Setup();
            yield return LiftOffBracket();
            Shot("lifted_off_bracket");
            yield return PullPin();
            Shot("pin_pulled");
            Assert.IsTrue(_co2.IsPinPulled, "second hand pulled the pin");
            yield return TakeHornAndApproach(1.8f, 0.05f);
            Shot("aim_at_base");
            yield return SprayFor(1.2f, true);
            Shot("spraying_sweep");
            yield return SprayFor(10f, true);
            Assert.IsTrue(_fire.IsExtinguished, $"PASS at 1.8 m, aimed at the base and sweeping, puts the fire out (intensity {_fire.FireIntensity:F1})");
        }

        [UnityTest]
        public IEnumerator PASS_AimAtFlames_DoesNotPutFireOut()
        {
            yield return Setup();
            yield return LiftOffBracket();
            yield return PullPin();
            yield return TakeHornAndApproach(1.8f, 1.2f);
            float before = _fire.FireIntensity;
            yield return SprayFor(6f, true);
            Shot("aim_at_flames");
            Assert.IsFalse(_fire.IsExtinguished, "spraying into the flames above the base is not enough");
            Assert.Greater(_fire.FireIntensity, before * 0.4f);
        }
    }
}
