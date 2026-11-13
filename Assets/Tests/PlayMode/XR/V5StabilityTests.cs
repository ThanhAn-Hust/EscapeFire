using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V5.3 — playing several drills in a row must not leak: event handlers, singletons and persistent objects stay the
    /// same after every reload, and the console stays free of errors. (Drill content is ended by a scenario shortcut.)
    /// </summary>
    public class V5StabilityTests : XRSceneTest
    {
        private static Dictionary<string, int> Handlers() => new Dictionary<string, int>
        {
            { "Mentor", EventBus<MentorMessageEvent>.HandlerCount },
            { "Phase", EventBus<PhaseChangedEvent>.HandlerCount },
            { "FireOut", EventBus<FireExtinguishedEvent>.HandlerCount },
            { "Alarm", EventBus<AlarmActivatedEvent>.HandlerCount },
            { "Spray", EventBus<ExtinguisherSprayEvent>.HandlerCount },
            { "Power", EventBus<PowerStateChangedEvent>.HandlerCount },
        };

        private readonly List<string> _errors = new List<string>();

        private void OnLog(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception) && !message.Contains("haptic")) _errors.Add(message);
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator ThreeDrillsInARow_NoLeaks_NoErrors()
        {
            _errors.Clear();
            Application.logMessageReceived += OnLog;
            Dictionary<string, int> first = null;
            int persistentFirst = -1;
            try
            {
                for (int run = 0; run < 3; run++)
                {
                    Xr?.Dispose();
                    Xr = null;
                    yield return LoadLab(TrainingMode.Guided);
                    yield return Xr.Wait(1f);
                    var c = LabProgressiveScenarioController.Instance;
                    Assert.AreEqual(1, Object.FindObjectsByType<LabProgressiveScenarioController>(FindObjectsSortMode.None).Length, "one scenario controller");
                    Assert.AreEqual(1, Object.FindObjectsByType<MicroAIGovernor>(FindObjectsSortMode.None).Length, "one governor");
                    Assert.AreEqual(1, Object.FindObjectsByType<ScoringManager>(FindObjectsSortMode.None).Length, "one scoring manager");
                    Assert.LessOrEqual(Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>(FindObjectsSortMode.None).Length, 1, "one interaction manager");
                    c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                    yield return Xr.Wait(1.5f);
                    c.Fail("Kiểm tra ổn định: kết thúc ván."); // scenario shortcut
                    yield return Xr.Wait(0.5f);

                    var now = Handlers();
                    int persistent = PersistentObjects();
                    Debug.Log($"[Stability] run {run}: handlers {string.Join(", ", now)} persistent={persistent}");
                    if (first == null) { first = now; persistentFirst = persistent; }
                    else
                    {
                        foreach (var kv in now) Assert.AreEqual(first[kv.Key], kv.Value, $"{kv.Key} handlers grew on reload {run}");
                        Assert.AreEqual(persistentFirst, persistent, "objects surviving scene loads grew");
                    }
                }
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
            }
            Assert.IsEmpty(_errors, "console errors:\n" + string.Join("\n", _errors));
        }

        /// <summary>Root objects in DontDestroyOnLoad (scene flow runner, LMS sync, XR managers…).</summary>
        private static int PersistentObjects()
        {
            var probe = new GameObject("probe");
            Object.DontDestroyOnLoad(probe);
            int n = probe.scene.GetRootGameObjects().Length - 1;
            Object.Destroy(probe);
            return n;
        }
    }
}
