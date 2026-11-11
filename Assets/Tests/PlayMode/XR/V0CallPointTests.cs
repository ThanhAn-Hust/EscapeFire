using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Interactions;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>V0.3 / B.1-1 — the manual call point is pressed with a fingertip, firmly, towards the wall.</summary>
    public class V0CallPointTests : XRSceneTest
    {
        private FireAlarmButton _alarm;
        private Vector3 _face, _into;

        private IEnumerator Setup()
        {
            yield return LoadLab();
            _alarm = Object.FindFirstObjectByType<FireAlarmButton>();
            Vector3 p = _alarm.transform.position;
            _into = Mathf.Abs(p.x) > Mathf.Abs(p.z) ? new Vector3(Mathf.Sign(p.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(p.z));
            var col = _alarm.GetComponent<Collider>();
            _face = col.ClosestPoint(p - _into * 1f);
            var eye = new Vector3(_face.x, 1.62f, _face.z) - _into * 0.55f;
            yield return StandAt(eye, _face);
        }

        [UnityTest]
        public IEnumerator Poke_Firmly_RaisesAlarm()
        {
            yield return Setup();
            yield return Poke(Hand.Right, _face, _into, 0.015f);
            Shot("poke_pressed");
            Assert.IsTrue(_alarm.IsActivated, "a firm fingertip press must raise the alarm");
        }

        [UnityTest]
        public IEnumerator Poke_LightTouch_DoesNothing()
        {
            yield return Setup();
            yield return Poke(Hand.Right, _face, _into, 0.002f);
            Assert.IsFalse(_alarm.IsActivated, "brushing the face (2 mm) is not a press");
        }

        [UnityTest]
        public IEnumerator Grip_Fist_DoesNothing()
        {
            yield return Setup();
            yield return Xr.MoveHand(Hand.Right, _face - _into * 0.03f, Quaternion.LookRotation(_into), 0.4f);
            Xr.Grip(Hand.Right, true);
            yield return Xr.Wait(0.3f);
            Xr.Grip(Hand.Right, false);
            yield return Xr.Wait(0.2f);
            Assert.IsFalse(_alarm.IsActivated, "a call point is pressed, not grabbed");
        }
    }
}
