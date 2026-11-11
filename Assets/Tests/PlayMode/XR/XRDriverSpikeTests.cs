using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Core;
using EscapeFire.Interactions;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>Proves the headset-free driver moves the real XR rig and XRI interactors (no game calls).</summary>
    public class XRDriverSpikeTests
    {
        private XRInputDriver _xr;

        [TearDown]
        public void TearDown() { _xr?.Dispose(); _xr = null; }

        [UnityTest]
        public IEnumerator Driver_MovesCameraAndControllers()
        {
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "XRTEST");
            yield return SceneManager.LoadSceneAsync(SceneNames.LAB, LoadSceneMode.Single);
            yield return null;
            _xr = XRInputDriver.Create();
            var eye = new Vector3(1.2f, 1.6f, -5.6f);
            _xr.LookAt(eye, eye + Vector3.back);
            _xr.SetHand(Hand.Right, eye + new Vector3(0.25f, -0.3f, -0.35f), Quaternion.LookRotation(Vector3.back));
            yield return _xr.Wait(0.3f);

            foreach (var d in UnityEngine.InputSystem.InputSystem.devices) Debug.Log($"[XRSpike] device {d.name} layout={d.layout} usages={string.Join(",", d.usages)} enabled={d.enabled}");
            foreach (var tpd in Object.FindObjectsByType<UnityEngine.InputSystem.XR.TrackedPoseDriver>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var a = tpd.positionInput.action;
                Debug.Log($"[XRSpike] TPD {tpd.name} active={tpd.isActiveAndEnabled} posAction={(a != null ? a.name + " enabled=" + a.enabled + " val=" + a.ReadValue<Vector3>() + " ctrl=" + (a.activeControl != null ? a.activeControl.path : "none") + " bindings=" + string.Join("|", System.Linq.Enumerable.Select(a.bindings, b => b.effectivePath)) : "null")} local={tpd.transform.localPosition}");
            }
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.name.Contains("Controller") && go.parent != null && go.parent.name.Contains("Camera Offset")) Debug.Log($"[XRSpike] {go.name} active={go.gameObject.activeInHierarchy}");
            Debug.Log($"[XRSpike] runInBackground={Application.runInBackground} editorInput={UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode} bg={UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior} update={UnityEngine.InputSystem.InputSystem.settings.updateMode}");
            Assert.Less(Vector3.Distance(_xr.Camera.transform.position, eye), 0.05f, "HMD pose must drive the rig camera");
            var near = FindInteractor(Hand.Right);
            Assert.IsNotNull(near, "right-hand interactor");
            Debug.Log($"[XRSpike] right interactor '{near.name}' at {near.transform.position}, expected near {_xr.HandPosition(Hand.Right)}");
            Assert.Less(Vector3.Distance(near.transform.position, _xr.HandPosition(Hand.Right)), 0.25f, "controller pose must drive the hand");
        }

        [UnityTest]
        public IEnumerator Grip_OnAlarmButton_RaisesAlarm_ThroughXRI()
        {
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "XRTEST");
            yield return SceneManager.LoadSceneAsync(SceneNames.LAB, LoadSceneMode.Single);
            yield return null;
            _xr = XRInputDriver.Create();
            var alarm = Object.FindFirstObjectByType<FireAlarmButton>();
            Vector3 b = alarm.transform.position;
            var eye = new Vector3(b.x, 1.6f, b.z + 0.7f);
            _xr.LookAt(eye, b);
            Quaternion palm = Quaternion.LookRotation(b - eye);
            _xr.SetHand(Hand.Right, eye + new Vector3(0.2f, -0.35f, -0.2f), palm);
            yield return _xr.Wait(0.2f);
            yield return _xr.MoveHand(Hand.Right, b + new Vector3(0f, 0f, 0.05f), palm, 0.6f);
            _xr.Grip(Hand.Right, true);
            yield return _xr.Wait(0.3f);
            _xr.Grip(Hand.Right, false);
            yield return _xr.Wait(0.2f);
            foreach (var i in Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
                Debug.Log($"[XRSpike] interactor {i.name} ({i.GetType().Name}) at {i.transform.position} hover={i.hasHover}");
            Assert.IsTrue(alarm.IsActivated, "grabbing the call point with the right controller must raise the alarm");
        }

        private XRBaseInteractor FindInteractor(Hand hand)
        {
            string side = hand == Hand.Left ? "Left" : "Right";
            foreach (var i in Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
            {
                for (var t = i.transform; t != null; t = t.parent)
                    if (t.name.Contains(side)) return i;
            }
            return null;
        }
    }
}
