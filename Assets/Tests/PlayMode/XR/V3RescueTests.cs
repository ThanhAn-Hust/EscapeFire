using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Scenario;
using Object = UnityEngine.Object;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V3 — rescue skills done with simulated VR input only: wet towel at the tap, the 114 radio call (push-to-talk at
    /// the mouth), the Rautek drag of an unconscious victim into the stairwell, stopping Minh at the lift.
    /// Speech-to-text is the one sensor replaced: a fake STT returns the sentence the learner "says" (as the real
    /// STT adapter would), everything else goes through the XR rig and XRI.
    /// </summary>
    public class V3RescueTests : BuildingSceneTest
    {
        private static Transform Rescue(string path) => GameObject.Find("[RESCUE_V3]").transform.Find(path);

        [UnityTest]
        public IEnumerator Towel_WetUnderTap_ThenHeldToFace()
        {
            yield return LoadLab(TrainingMode.Guided);
            var tap = Rescue("Sink").GetComponent<WaterTap>();
            var towel = Rescue("Sink/Towel").GetComponent<WetTowel>();
            Vector3 handle = tap.Handle.transform.position;
            yield return StandAt(new Vector3(handle.x - 0.1f, 1.6f, -6.25f), handle);
            Shot("towel_01_sink");

            // turn the tap a quarter turn with the left hand
            yield return GripAt(Hand.Left, tap.Handle.transform.Find("GrabPoint").position, 0.4f);
            yield return ArcMove(Hand.Left, handle, Vector3.up, 95f, 0.8f);
            yield return Release(Hand.Left);
            Assert.IsTrue(tap.IsRunning, $"tap turned on (angle {tap.Handle.Angle:F0})");

            // pick up the towel, hold it under the water
            yield return GripAt(Hand.Right, towel.transform.position, 0.4f);
            Assert.IsTrue(towel.IsHeld, "towel picked up");
            yield return Xr.MoveHand(Hand.Right, tap.SpoutPosition + Vector3.down * 0.12f, Xr.HandRotation(Hand.Right), 0.5f);
            yield return Xr.Wait(1.0f);
            Shot("towel_02_under_the_tap");
            Assert.IsFalse(towel.IsWet, "not soaked after a second");
            yield return Xr.Wait(1.5f);
            Assert.IsTrue(towel.IsWet, "soaked after ~2 s under the water");
            Assert.AreEqual(1f, WetTowel.BreathingFactor(), "no protection while not on the face");

            // hold it over nose and mouth
            Transform head = Xr.Camera.transform;
            yield return Xr.MoveHand(Hand.Right, WetTowel.Mouth(head) + head.forward * 0.03f, Quaternion.LookRotation(-head.forward), 0.5f);
            yield return Xr.Wait(0.3f);
            Shot("towel_03_over_nose_and_mouth");
            Assert.IsTrue(towel.OnFace, "towel held to the face");
            Assert.Less(WetTowel.BreathingFactor(), 0.5f, "wet towel slows the oxygen loss");
        }

        [UnityTest]
        public IEnumerator Radio_PTTAtMouth_Call114_AsksBackForFloor()
        {
            yield return LoadLab(TrainingMode.Guided);
            var stt = new FakeStt();
            SpeechServices.Register(stt);
            try
            {
                var radio = Rescue("RadioDock/Radio").GetComponent<Radio>();
                var call = Call114Dialogue.Instance;
                Vector3 r = radio.transform.position;
                yield return StandAt(new Vector3(r.x, 1.6f, -6.3f), r);
                yield return GripAt(Hand.Right, r, 0.4f);
                Transform head = Xr.Camera.transform;

                // PTT pressed with the radio at the chest: the microphone does not pick it up
                yield return Xr.MoveHand(Hand.Right, head.position + head.forward * 0.3f + Vector3.down * 0.5f, Xr.HandRotation(Hand.Right), 0.4f);
                Xr.Trigger(Hand.Right, 1f);
                yield return Xr.Wait(0.3f);
                Assert.IsFalse(radio.IsTransmitting || stt.Listening, "not transmitting far from the mouth");
                stt.Say("Cháy phòng server tầng 3");
                yield return Xr.Wait(0.1f);
                Xr.Trigger(Hand.Right, 0f);
                Assert.IsFalse(call.Started, "nobody heard it");
                yield return Xr.Wait(4.5f);

                // radio at the mouth, hold PTT, report (forgetting the floor)
                yield return Xr.MoveHand(Hand.Right, WetTowel.Mouth(head) + head.forward * 0.04f, Quaternion.LookRotation(-head.forward), 0.5f);
                Xr.Trigger(Hand.Right, 1f);
                yield return Xr.Wait(0.3f);
                Shot("radio_01_talking_to_114");
                Assert.IsTrue(radio.IsTransmitting && stt.Listening, "PTT held at the mouth opens the microphone");
                stt.Say("Alo 114, cháy tủ server ở tòa B1 Đại học Bách Khoa, có một người bị ngất");
                yield return Xr.Wait(0.2f);
                Assert.IsFalse(call.IsComplete);
                Assert.AreEqual(1, call.AskBacks);
                StringAssert.Contains("tầng", call.LastReply, "114 asks which floor");
                stt.Say("Tầng ba");
                yield return Xr.Wait(0.2f);
                Xr.Trigger(Hand.Right, 0f);
                Assert.IsTrue(call.IsComplete, "address, floor, what is burning, people trapped all given");
            }
            finally
            {
                SpeechServices.Unregister(stt);
            }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Rautek_TwoHandsRequired_DragIntoStairwell_HeadKeptLow()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.6f, 0f, -12.35f), 90f);
            var v = Rescue("Victim_Nam").GetComponent<VictimBody>();
            Shot("rautek_01_victim_in_the_corridor");

            // one hand is not enough
            yield return KneelAndGrip(v, false);
            yield return Xr.Wait(0.8f);
            Assert.IsFalse(v.IsDragged, "one hand cannot lift an unconscious adult");
            Xr.Grip(Hand.Right, false);
            yield return Xr.Wait(0.2f);

            yield return RautekIntoStairwellA(v, "rautek_");
        }

        [UnityTest]
        public IEnumerator Minh_TakenByTheArm_BeforeTheLiftOpens_GoesToTheStairs()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -8.3f), 180f);
            var minh = Object.FindFirstObjectByType<PanicRunner>();
            yield return Xr.Wait(0.3f);
            Shot("minh_01_at_the_lift");
            yield return StopMinh("minh_02_stopped");
            yield return Xr.Wait(6f);
            Assert.IsFalse(minh.Trapped);
            Assert.IsFalse(minh.InBuilding, "he went down the stairs");
            Assert.IsFalse(ElevatorTrap.ElevatorUsedThisSession, "the learner never called the lift");
        }

        [UnityTest]
        public IEnumerator Minh_NotStopped_IsTrappedInTheLift()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -8.3f), 180f);
            var minh = Object.FindFirstObjectByType<PanicRunner>();
            yield return Xr.Wait(13f);
            Assert.IsTrue(minh.Trapped, "nobody stopped him: he took the lift and is stuck");
        }
    }
}
