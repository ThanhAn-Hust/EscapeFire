using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Interactions;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>V0.6 / B.1-3 — open the panel door, then pull the breaker lever down through its full travel.</summary>
    public class V0BreakerTests : XRSceneTest
    {
        private CircuitBreakerSwitch _breaker;
        private HingeGrab _door, _lever;

        private IEnumerator Setup()
        {
            yield return LoadLab();
            _breaker = Object.FindFirstObjectByType<CircuitBreakerSwitch>();
            _door = _breaker.transform.Find("EF_PanelDoorHinge").GetComponent<HingeGrab>();
            _lever = _breaker.transform.Find("EF_BreakerLever").GetComponent<HingeGrab>();
            Vector3 p = _breaker.transform.position;
            Vector3 into = Mathf.Abs(p.x) > Mathf.Abs(p.z) ? new Vector3(Mathf.Sign(p.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(p.z));
            Vector3 eye = new Vector3(p.x, 1.65f, p.z) - into * 0.65f;
            yield return StandAt(eye, p);
        }

        private Vector3 Grab(HingeGrab h) => h.transform.Find("GrabPoint") != null ? h.transform.Find("GrabPoint").position : h.transform.GetComponentInChildren<Transform>().Find("GrabPoint").position;

        private Vector3 LeverTip => _lever.transform.Find("Arm/GrabPoint").position;

        private IEnumerator OpenDoor()
        {
            yield return GripAt(Hand.Left, _door.transform.Find("GrabPoint").position);
            float sign = Mathf.Sign(_door.GetType().GetField("maxAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_door) is float f ? f : 1f);
            yield return ArcMove(Hand.Left, _door.transform.position, Vector3.up, 100f * sign, 1.0f);
            yield return Release(Hand.Left);
        }

        private IEnumerator PullLever(float degrees)
        {
            yield return GripAt(Hand.Right, LeverTip);
            Vector3 axis = _lever.transform.right;
            yield return ArcMove(Hand.Right, _lever.transform.position, axis, degrees, 0.8f);
        }

        [UnityTest]
        public IEnumerator Lever_PanelClosed_CannotBeReached()
        {
            yield return Setup();
            yield return PullLever(75f);
            yield return Release(Hand.Right);
            Shot("panel_closed");
            Assert.IsTrue(_breaker.IsPowerOn, "the lever is behind the closed panel door");
        }

        [UnityTest]
        public IEnumerator OpenDoor_ThenFullTravel_CutsPower()
        {
            yield return Setup();
            yield return OpenDoor();
            Shot("panel_open");
            Assert.Greater(Mathf.Abs(_door.Angle), 80f, "panel door swung open");
            yield return PullLever(75f);
            Shot("lever_off");
            yield return Release(Hand.Right);
            Vector3 p = _breaker.transform.position;
            Vector3 back = new Vector3(p.x, 1.7f, p.z) + new Vector3(-Mathf.Sign(p.x) * 1.6f, 0f, -0.9f);
            Xr.LookAt(back, p + Vector3.down * 0.2f);
            yield return Xr.Wait(0.2f);
            Shot("panel_overview");
            Assert.IsFalse(_breaker.IsPowerOn, "lever pulled through its full travel switches the power off");
            Assert.IsTrue(_lever.IsLatched);
        }

        [UnityTest]
        public IEnumerator Lever_HalfTravel_SpringsBack()
        {
            yield return Setup();
            yield return OpenDoor();
            yield return PullLever(35f);
            yield return Release(Hand.Right);
            yield return Xr.Wait(0.4f);
            Assert.IsTrue(_breaker.IsPowerOn, "half way is not OFF");
            Assert.Less(_lever.Angle, 5f, "a spring-loaded lever returns when released early");
        }
    }
}
