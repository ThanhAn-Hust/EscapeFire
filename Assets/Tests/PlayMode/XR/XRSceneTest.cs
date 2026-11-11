using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Scenario;
using Object = UnityEngine.Object;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// Base for VR interaction tests: loads a scene, takes over the rig with <see cref="XRInputDriver"/>, and can
    /// save screenshots from the rig camera (what the learner sees) to Logs/Demo/&lt;date&gt;_xr/.
    /// Tests built on this must act only through the driver (poses, grip, trigger) — never call game code.
    /// </summary>
    public abstract class XRSceneTest
    {
        protected XRInputDriver Xr;
        private static string _shotDir;

        [TearDown]
        public void TearDownXr()
        {
            Xr?.Dispose();
            Xr = null;
            Time.timeScale = 1f;
        }

        protected IEnumerator LoadLab(TrainingMode mode = TrainingMode.Guided, bool stayInLab = true)
        {
            GameSession.Reset();
            GameSession.Begin(mode, "XRTEST");
            SessionResult.Reset();
            LmsSync.OutboxFolder = "lms_outbox_tests";
            ProgressionStore.KeyPrefix = "EF_test_progress_";
            TelemetryLogger.FolderOverride = "TelemetryReports_tests";
            yield return SceneManager.LoadSceneAsync(SceneNames.LAB, LoadSceneMode.Single);
            yield return null;
            if (stayInLab)
            {
                var reporter = Object.FindFirstObjectByType<SessionOutcomeReporter>();
                if (reporter != null) typeof(SessionOutcomeReporter).GetField("loadAARScene", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(reporter, false);
            }
            var edge = Object.FindFirstObjectByType<EdgeDirectorClient>();
            if (edge != null) typeof(EdgeDirectorClient).GetField("reconnectDelaySeconds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(edge, 999f);
            Xr = XRInputDriver.Create();
            yield return null;
        }

        /// <summary>Stands the learner at <paramref name="eye"/> looking at <paramref name="target"/>, hands relaxed.</summary>
        protected IEnumerator StandAt(Vector3 eye, Vector3 target, float settle = 0.2f)
        {
            Xr.LookAt(eye, target);
            Quaternion look = Quaternion.LookRotation(Flat(target - eye));
            Xr.SetHand(Hand.Left, eye + look * new Vector3(-0.2f, -0.45f, 0.25f), look);
            Xr.SetHand(Hand.Right, eye + look * new Vector3(0.2f, -0.45f, 0.25f), look);
            yield return Xr.Wait(settle);
        }

        /// <summary>Fingertip (poke point) of a hand, as XRI uses it.</summary>
        protected Transform PokeTip(Hand hand)
        {
            string side = hand == Hand.Left ? "Left" : "Right";
            foreach (var p in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor>(FindObjectsSortMode.None))
                for (var t = p.transform; t != null; t = t.parent)
                    if (t.name.StartsWith(side)) return p.GetAttachTransform(null);
            return null;
        }

        /// <summary>
        /// Presses a surface with the index fingertip: approach 6 cm in front, push <paramref name="depth"/> past the
        /// surface along <paramref name="intoSurface"/>, hold, pull back — like pressing a real button.
        /// </summary>
        protected IEnumerator Poke(Hand hand, Vector3 surfacePoint, Vector3 intoSurface, float depth, float hold = 0.3f)
        {
            Quaternion rot = Quaternion.LookRotation(intoSurface, Vector3.up);
            Xr.SetHand(hand, surfacePoint - intoSurface * 0.25f, rot);
            yield return Xr.Wait(0.05f);
            var tip = PokeTip(hand);
            Vector3 tipOffset = tip != null ? tip.position - Xr.HandPosition(hand) : Vector3.zero;
            Vector3 start = surfacePoint - intoSurface * 0.06f - tipOffset;
            yield return Xr.MoveHand(hand, start, rot, 0.3f);
            yield return Xr.MoveHand(hand, surfacePoint + intoSurface * depth - tipOffset, rot, 0.25f);
            yield return Xr.Wait(hold);
            yield return Xr.MoveHand(hand, start, rot, 0.2f);
        }

        /// <summary>Moves a gripping hand along an arc around a hinge (opening a door, pulling a lever).</summary>
        protected IEnumerator ArcMove(Hand hand, Vector3 pivot, Vector3 axis, float degrees, float seconds)
        {
            Vector3 start = Xr.HandPosition(hand);
            Quaternion rot = Xr.HandRotation(hand);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Quaternion q = Quaternion.AngleAxis(degrees * Mathf.SmoothStep(0f, 1f, t / seconds), axis);
                Xr.SetHand(hand, pivot + q * (start - pivot), q * rot);
                yield return null;
            }
            Quaternion end = Quaternion.AngleAxis(degrees, axis);
            Xr.SetHand(hand, pivot + end * (start - pivot), end * rot);
            yield return Xr.Wait(0.1f);
        }

        protected IEnumerator GripAt(Hand hand, Vector3 point, float seconds = 0.4f)
        {
            yield return Xr.MoveHand(hand, point, Xr.HandRotation(hand), seconds);
            Xr.Grip(hand, true);
            yield return Xr.Wait(0.15f);
        }

        protected IEnumerator Release(Hand hand)
        {
            Xr.Grip(hand, false);
            yield return Xr.Wait(0.15f);
        }

        protected static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude < 1e-6f ? Vector3.forward : v.normalized; }

        /// <summary>Saves what the learner sees right now (rig camera) as a PNG.</summary>
        protected void Shot(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Xr == null) return;
            if (_shotDir == null)
            {
                _shotDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Demo", DateTime.Now.ToString("yyyyMMdd_HHmm") + "_xr");
                Directory.CreateDirectory(_shotDir);
            }
            var cam = Xr.Camera;
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(_shotDir, GetType().Name + "_" + name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }
    }
}
