using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Interactions;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>V0.7 / B.1-8, B.1-9 — back of the hand on the door, then turn the lever handle and push the leaf.</summary>
    public class V0DoorTests : XRSceneTest
    {
        private DoorHeatChecker _checker;
        private ExitDoor _door;
        private Transform _leftHinge, _handlePivot;
        private Vector3 _surface, _into;

        private IEnumerator Setup()
        {
            yield return LoadLab();
            _checker = Object.FindFirstObjectByType<DoorHeatChecker>();
            _door = _checker.GetComponent<ExitDoor>();
            _leftHinge = _door.transform.parent.Find("ExitDoor_HingeLeft");
            _handlePivot = _leftHinge.Find("EF_HandlePivotIn");
            Vector3 p = _door.transform.position;
            _into = new Vector3(0f, 0f, Mathf.Sign(p.z));
            _surface = _checker.GetComponent<Collider>().ClosestPoint(p - _into + Vector3.up * 0.1f + Vector3.right * 0.3f);
            Vector3 eye = new Vector3(p.x + 0.2f, 1.6f, p.z) - _into * 0.7f;
            yield return StandAt(eye, p + Vector3.up * 0.3f);
        }

        /// <summary>Right hand flat against the door; backOfHand = true turns the back of the hand to the door.</summary>
        private IEnumerator HandOnDoor(bool backOfHand, float seconds)
        {
            // Right palm normal is -controller.right: choose the rotation so that it points away from / at the door.
            Vector3 wantRight = backOfHand ? _into : -_into;
            Quaternion rot = Quaternion.LookRotation(Vector3.Cross(wantRight, Vector3.up), Vector3.up);
            yield return Xr.MoveHand(Hand.Right, _surface - _into * 0.03f, rot, 0.4f);
            yield return Xr.Wait(seconds);
            yield return Xr.MoveHand(Hand.Right, _surface - _into * 0.25f, rot, 0.2f);
        }

        [UnityTest]
        public IEnumerator BackOfHand_OneSecond_Counts()
        {
            yield return Setup();
            yield return HandOnDoor(true, 1.3f);
            Shot("back_of_hand_on_door");
            Assert.IsTrue(_checker.HasCheckedHeat, "back of the hand on the door for > 1 s is a heat check");
        }

        [UnityTest]
        public IEnumerator Palm_DoesNotCount()
        {
            yield return Setup();
            yield return HandOnDoor(false, 1.5f);
            Assert.IsFalse(_checker.HasCheckedHeat, "the palm is not the safe way to feel for heat");
        }

        [UnityTest]
        public IEnumerator BriefTouch_DoesNotCount()
        {
            yield return Setup();
            yield return HandOnDoor(true, 0.3f);
            Assert.IsFalse(_checker.HasCheckedHeat, "a brief tap does not tell you the temperature");
        }

        [UnityTest]
        public IEnumerator TurnHandle_ThenPush_Opens()
        {
            yield return Setup();
            yield return GripAt(Hand.Right, _handlePivot.Find("GrabPoint").position);
            yield return ArcMove(Hand.Right, _handlePivot.position, _handlePivot.forward, 40f, 0.5f);
            Shot("handle_turned");
            Assert.Greater(Mathf.Abs(_handlePivot.GetComponent<HingeGrab>().Angle), 25f, "lever handle turned down");
            yield return ArcMove(Hand.Right, _leftHinge.position, Vector3.up, 95f, 1.0f);
            Shot("door_pushed_open");
            yield return Release(Hand.Right);
            Assert.IsTrue(_door.IsOpen, "turning the handle and pushing opens the leaf");
            Assert.IsFalse(_checker.GetComponent<Collider>().enabled, "an open door no longer blocks the way");
        }

        [UnityTest]
        public IEnumerator Push_WithoutTurningHandle_StaysShut()
        {
            yield return Setup();
            yield return GripAt(Hand.Right, _handlePivot.Find("GrabPoint").position);
            yield return ArcMove(Hand.Right, _leftHinge.position, Vector3.up, 95f, 1.0f);
            yield return Release(Hand.Right);
            Assert.IsFalse(_door.IsOpen, "a latched door does not open without turning the handle");
        }
    }
}
