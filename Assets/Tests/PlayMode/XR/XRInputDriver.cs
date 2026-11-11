using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Object = UnityEngine.Object;

namespace EscapeFire.Tests.PlayMode.XR
{
    public enum Hand { Left, Right }

    /// <summary>
    /// Headset-free VR driver for PlayMode tests and demo recordings. It replaces the keyboard simulator with an
    /// HMD and two controllers created as Input System devices (the same device types the XR Interaction
    /// Simulator uses), and feeds them world-space poses and grip / trigger values every frame. The XR rig and
    /// XRI interactors then react exactly as they do to a real headset: nothing in the game is called directly.
    /// </summary>
    public sealed class XRInputDriver : IDisposable
    {
        private readonly XRSimulatedHMD _hmd;
        private readonly XRSimulatedController _left, _right;
        private XRSimulatedHMDState _hs;
        private XRSimulatedControllerState _ls, _rs;
        private readonly Pump _pump;
        private readonly InputSettings.BackgroundBehavior _bgWas;
        private readonly InputSettings.EditorInputBehaviorInPlayMode _editorWas;

        public XROrigin Origin { get; }
        public Transform TrackingSpace => Origin.CameraFloorOffsetObject != null ? Origin.CameraFloorOffsetObject.transform : Origin.transform;
        public Camera Camera => Origin.Camera;

        private XRInputDriver(XROrigin origin)
        {
            Origin = origin;
            // Batchmode / an unfocused Game view never has focus: without this the Input System resets the
            // simulated devices every frame and the rig never moves.
            _bgWas = InputSystem.settings.backgroundBehavior;
            _editorWas = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _hmd = InputSystem.AddDevice<XRSimulatedHMD>("EF Test HMD");
            _left = InputSystem.AddDevice<XRSimulatedController>("EF Test Left");
            _right = InputSystem.AddDevice<XRSimulatedController>("EF Test Right");
            InputSystem.SetDeviceUsage(_left, UnityEngine.InputSystem.CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(_right, UnityEngine.InputSystem.CommonUsages.RightHand);
            _hs.Reset();
            _ls.Reset();
            _rs.Reset();
            _hs.isTracked = true; _hs.trackingState = 3;
            _ls.isTracked = true; _ls.trackingState = 3;
            _rs.isTracked = true; _rs.trackingState = 3;
            _hs.deviceRotation = _hs.centerEyeRotation = _ls.deviceRotation = _rs.deviceRotation = Quaternion.identity;
            _pump = new GameObject("_XRInputDriverPump").AddComponent<Pump>();
            _pump.Driver = this;
            Flush();
        }

        /// <summary>Turns off the keyboard/mouse simulators and takes over the rig in the active scene.</summary>
        public static XRInputDriver Create()
        {
            foreach (var sim in Object.FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) sim.gameObject.SetActive(false);
            foreach (var sim in Object.FindObjectsByType<XRDeviceSimulator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) sim.gameObject.SetActive(false);
            foreach (var d in InputSystem.devices.ToArray())
                if (d is XRSimulatedController || d is XRSimulatedHMD) InputSystem.RemoveDevice(d);
            var origin = Object.FindFirstObjectByType<XROrigin>();
            if (origin == null) throw new InvalidOperationException("No XR Origin in the scene");
            return new XRInputDriver(origin);
        }

        public void Dispose()
        {
            if (_pump != null) Object.Destroy(_pump.gameObject);
            foreach (var d in new InputDevice[] { _hmd, _left, _right }) if (d != null && d.added) InputSystem.RemoveDevice(d);
            InputSystem.settings.backgroundBehavior = _bgWas;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorWas;
        }

        // ------------------------------------------------------------------ poses (world space)

        public void SetHead(Vector3 worldPos, Quaternion worldRot)
        {
            Vector3 p = TrackingSpace.InverseTransformPoint(worldPos);
            Quaternion r = Quaternion.Inverse(TrackingSpace.rotation) * worldRot;
            _hs.devicePosition = _hs.centerEyePosition = _hs.leftEyePosition = _hs.rightEyePosition = p;
            _hs.deviceRotation = _hs.centerEyeRotation = _hs.leftEyeRotation = _hs.rightEyeRotation = r;
            Flush();
        }

        /// <summary>
        /// Head at a fixed spot of the play area (tracking space), looking along a world yaw — for thumbstick locomotion,
        /// where the rig (not the head) moves through the building.
        /// </summary>
        public void SetHeadTracking(Vector3 trackingPos, float worldYaw, float pitch = 0f)
        {
            Quaternion world = Quaternion.Euler(pitch, worldYaw, 0f);
            Quaternion r = Quaternion.Inverse(TrackingSpace.rotation) * world;
            _hs.devicePosition = _hs.centerEyePosition = _hs.leftEyePosition = _hs.rightEyePosition = trackingPos;
            _hs.deviceRotation = _hs.centerEyeRotation = _hs.leftEyeRotation = _hs.rightEyeRotation = r;
            Flush();
        }

        /// <summary>Hand at a tracking-space position (moves with the rig).</summary>
        public void SetHandTracking(Hand hand, Vector3 trackingPos, Quaternion worldRot)
        {
            Quaternion r = Quaternion.Inverse(TrackingSpace.rotation) * worldRot;
            if (hand == Hand.Left) { _ls.devicePosition = trackingPos; _ls.deviceRotation = r; }
            else { _rs.devicePosition = trackingPos; _rs.deviceRotation = r; }
            Flush();
        }

        public void LookAt(Vector3 eyeWorld, Vector3 target) => SetHead(eyeWorld, Quaternion.LookRotation(target - eyeWorld));

        public void SetHand(Hand hand, Vector3 worldPos, Quaternion worldRot)
        {
            Vector3 p = TrackingSpace.InverseTransformPoint(worldPos);
            Quaternion r = Quaternion.Inverse(TrackingSpace.rotation) * worldRot;
            if (hand == Hand.Left) { _ls.devicePosition = p; _ls.deviceRotation = r; }
            else { _rs.devicePosition = p; _rs.deviceRotation = r; }
            Flush();
        }

        public Vector3 HandPosition(Hand hand) => TrackingSpace.TransformPoint(hand == Hand.Left ? _ls.devicePosition : _rs.devicePosition);
        public Quaternion HandRotation(Hand hand) => TrackingSpace.rotation * (hand == Hand.Left ? _ls.deviceRotation : _rs.deviceRotation);
        public Vector3 HeadPosition => TrackingSpace.TransformPoint(_hs.devicePosition);

        // ------------------------------------------------------------------ buttons / axes

        public void Grip(Hand hand, bool held)
        {
            ref var s = ref State(hand);
            s.grip = held ? 1f : 0f;
            s = s.WithButton(ControllerButton.GripButton, held);
            Flush();
        }

        /// <summary>Analog trigger 0..1 (squeeze strength); the digital trigger button follows at 0.5.</summary>
        public void Trigger(Hand hand, float value)
        {
            ref var s = ref State(hand);
            s.trigger = Mathf.Clamp01(value);
            s = s.WithButton(ControllerButton.TriggerButton, value >= 0.5f);
            Flush();
        }

        public void Thumbstick(Hand hand, Vector2 value)
        {
            ref var s = ref State(hand);
            s.primary2DAxis = value;
            Flush();
        }

        private ref XRSimulatedControllerState State(Hand hand)
        {
            if (hand == Hand.Left) return ref _ls;
            return ref _rs;
        }

        internal void Flush()
        {
            // Queued events are processed in the Input System's own update, so XRI sees "pressed this frame"
            // exactly as with a real controller.
            if (_hmd != null && _hmd.added) InputSystem.QueueStateEvent(_hmd, _hs);
            if (_left != null && _left.added) InputSystem.QueueStateEvent(_left, _ls);
            if (_right != null && _right.added) InputSystem.QueueStateEvent(_right, _rs);
        }

        // ------------------------------------------------------------------ motions (coroutines)

        /// <summary>Moves a hand along a straight line at a human pace, keeping the given orientation.</summary>
        public IEnumerator MoveHand(Hand hand, Vector3 to, Quaternion rot, float seconds = 0.6f)
        {
            Vector3 from = HandPosition(hand);
            Quaternion fromRot = HandRotation(hand);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                SetHand(hand, Vector3.Lerp(from, to, k), Quaternion.Slerp(fromRot, rot, k));
                yield return null;
            }
            SetHand(hand, to, rot);
            yield return null;
        }

        /// <summary>Waits real game time, but always at least two frames: queued input reaches XRI one frame later.</summary>
        public IEnumerator Wait(float seconds)
        {
            int frames = 0;
            for (float t = 0f; t < seconds || frames < 2; t += Time.deltaTime, frames++) yield return null;
        }

        /// <summary>Re-applies the device state every frame so nothing (e.g. a late simulator) overrides it.</summary>
        private sealed class Pump : MonoBehaviour
        {
            public XRInputDriver Driver;
            private void Update() => Driver?.Flush();
            private void OnApplicationQuit() => Driver?.Dispose();
        }
    }
}
