using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Interactions;
using EscapeFire.Player;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>V0.1 — the rig behaves like a body: hands at the object, posture limits speed, human hands visible.</summary>
    public class V0RigTests : XRSceneTest
    {
        [UnityTest]
        public IEnumerator NearOnly_PointingFromAfar_CannotPressCallPoint()
        {
            yield return LoadLab();
            var alarm = Object.FindFirstObjectByType<FireAlarmButton>();
            Vector3 b = alarm.transform.position;
            var eye = new Vector3(b.x, 1.6f, b.z + 1.8f);
            yield return StandAt(eye, b);
            Quaternion aim = Quaternion.LookRotation(b - (eye + new Vector3(0.2f, -0.3f, -0.3f)));
            yield return Xr.MoveHand(Hand.Right, eye + new Vector3(0.2f, -0.3f, -0.3f), aim, 0.3f);
            Xr.Grip(Hand.Right, true);
            Xr.Trigger(Hand.Right, 1f);
            yield return Xr.Wait(0.4f);
            Xr.Grip(Hand.Right, false);
            Xr.Trigger(Hand.Right, 0f);
            yield return Xr.Wait(0.2f);
            Shot("far_point_no_press");
            Assert.IsFalse(alarm.IsActivated, "a call point is pressed by hand, not with a laser from 1.5 m");
        }

        [UnityTest]
        public IEnumerator Posture_CrouchingSlowsWalking()
        {
            yield return LoadLab();
            var posture = Object.FindFirstObjectByType<PostureLocomotion>();
            Assert.IsNotNull(posture, "Run EscapeFire > V0 > Setup Realistic Rig");
            var mover = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>();
            float floor = posture.transform.position.y;
            yield return StandAt(new Vector3(0f, floor + 1.65f, -4f), new Vector3(0f, floor + 1.6f, 0f));
            Assert.AreEqual(Posture.Standing, posture.Current);
            float standing = mover.moveSpeed;
            yield return StandAt(new Vector3(0f, floor + 1.05f, -4f), new Vector3(0f, floor + 1f, 0f));
            Assert.AreEqual(Posture.Crouching, posture.Current);
            Assert.Less(mover.moveSpeed, standing);
            yield return StandAt(new Vector3(0f, floor + 0.6f, -4f), new Vector3(0f, floor + 0.5f, 0f));
            Assert.AreEqual(Posture.Crawling, posture.Current);
            Assert.AreEqual(PostureRules.CrawlingSpeed, mover.moveSpeed, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Hands_VisibleAndFingersCurl()
        {
            yield return LoadLab();
            var eye = new Vector3(0f, 1.6f, -4f);
            yield return StandAt(eye, new Vector3(0f, 1.2f, -3f));
            Xr.SetHand(Hand.Right, eye + new Vector3(0.12f, -0.25f, 0.35f), Quaternion.LookRotation(Vector3.forward, Vector3.up));
            Xr.SetHand(Hand.Left, eye + new Vector3(-0.12f, -0.25f, 0.35f), Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return Xr.Wait(0.2f);
            var hand = GameObject.Find("EF_RightHand");
            Assert.IsNotNull(hand, "human hand model replaces the controller");
            Transform index = null;
            foreach (var t in hand.GetComponentsInChildren<Transform>()) if (t.name.Contains("IndexProximal")) index = t;
            Quaternion open = index.localRotation;
            Shot("hands_open");
            Xr.Grip(Hand.Right, true);
            Xr.Trigger(Hand.Right, 1f);
            Xr.Grip(Hand.Left, true);
            yield return Xr.Wait(0.2f);
            Shot("hands_closed");
            Xr.LookAt(eye, Xr.HandPosition(Hand.Right));
            yield return Xr.Wait(0.1f);
            Shot("right_fist_closeup");
            Assert.Greater(Quaternion.Angle(open, index.localRotation), 30f, "index finger curls with the trigger");
        }
    }
}
