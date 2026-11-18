using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Interactions;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V6 sound: the drill is no longer silent. The server cabinet bursts with a bang and the fire roars; the fire bell
    /// rings only once someone raises the alarm (it is the learner's action that rings it); the extinguisher hisses
    /// while spraying. The actions themselves are VR-tested elsewhere; here they are called directly (scenario setup).
    /// </summary>
    public class V6SoundTests : BuildingSceneTest
    {
        private static AudioSource Child(Component c, string name)
        {
            var t = c.transform.Find(name);
            if (t == null) foreach (var x in c.GetComponentsInChildren<Transform>(true)) if (x.name == name) t = x;
            Assert.IsNotNull(t, $"{name} under {c.name} (run EscapeFire > V3 > Build Rescue Scenario)");
            var s = t.GetComponent<AudioSource>();
            Assert.IsNotNull(s.clip, name + " has a clip");
            return s;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Fire_Bell_Spray_SoundAtTheRightMoments()
        {
            if (AudioSettings.GetConfiguration().sampleRate <= 0) Assert.Ignore("no audio device");
            yield return LoadLab(TrainingMode.Guided);
            var c = LabProgressiveScenarioController.Instance;
            var alarm = Object.FindFirstObjectByType<FireAlarmButton>();
            var bell = Child(alarm, "FireAlarmBell");
            FireExtinguisher co2 = null;
            foreach (var e in Object.FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None)) if (e.Type == ExtinguisherType.CO2) co2 = e;
            var spray = Child(co2, "SprayAudio");

            c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return Xr.Wait(0.3f);
            Assert.IsTrue(Child(c.ServerFire, "FireAudio").isPlaying, "the fire roars");
            Assert.IsFalse(bell.isPlaying, "the bell does not ring by itself: someone must raise the alarm");

            alarm.PushAlarmButton();
            yield return Xr.Wait(0.2f);
            Assert.IsTrue(bell.isPlaying, "the call point rings the fire bell");

            co2.PullSafetyPin();
            Assert.IsTrue(co2.TrySpray(isElectricalPowerLive: false), "spraying");
            yield return Xr.Wait(0.2f);
            Assert.IsTrue(spray.isPlaying, "the CO2 jet hisses");
            co2.StopSpray();
            yield return Xr.Wait(0.1f);
            Assert.IsFalse(spray.isPlaying, "silent when the lever is released");
        }
    }
}
