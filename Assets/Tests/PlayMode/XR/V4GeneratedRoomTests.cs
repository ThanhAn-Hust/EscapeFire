using System.Collections;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Generation;
using EscapeFire.Interactions;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V4 — rooms built from a ScenarioSpec (what /scenario/generate returns for a teacher prompt) are playable through
    /// simulated VR input only: pick the extinguisher off the wall, pull the pin, aim at the base, squeeze, sweep, leave
    /// by the EXIT; the wrong agent on an electrical fire fails the drill. Loading the spec = scenario setup.
    /// </summary>
    public class V4GeneratedRoomTests : BuildingSceneTest
    {
        private ScenarioBuilder _builder;

        private IEnumerator LoadRoom(JObject spec, int seed = 1)
        {
            PlayAreaSpot = Vector3.zero;
            Application.targetFrameRate = 72;
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "XRTEST");
            ProgressionStore.KeyPrefix = "EF_test_progress_";
            EscapeFire.Analytics.SkillStore.Clear();
            SessionResult.Reset();
            GeneratedScenario.Pending = spec;
            GeneratedScenario.Seed = seed;
            yield return SceneManager.LoadSceneAsync(SceneNames.GENERATED, LoadSceneMode.Single);
            yield return null;
            Xr = XRInputDriver.Create();
            yield return null;
            _builder = Object.FindFirstObjectByType<ScenarioBuilder>();
            // these tests stay in the room after the drill ends (no automatic AAR scene)
            typeof(GeneratedDrill).GetField("loadAARScene", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_builder.GetComponent<GeneratedDrill>(), false);
            Assert.IsTrue(_builder.Built, "room built: " + string.Join(",", _builder.Errors));
            Body(_builder.Layout.Start.Yaw);
            yield return Xr.Wait(0.5f);
        }

        private static JObject Sample(string room) => JObject.Parse(Resources.Load<TextAsset>("Scenarios/" + room).text);

        [UnityTest, Timeout(300000)]
        public IEnumerator FiveSampleRooms_AreBuilt_AndPhotographed()
        {
            foreach (var room in new[] { "library", "lab", "office", "canteen", "workshop" })
            {
                yield return LoadRoom(Sample(room));
                var L = _builder.Layout;
                Assert.IsTrue(L.Ok && L.Items.Count >= 3, room);
                // overview from the corner opposite the first exit, eye 1.7 m (scenario camera, not a learner action)
                var e = L.Exits[0];
                Vector3 corner = new Vector3(e.X > 0 ? -L.Width / 2 + 1.3f : L.Width / 2 - 1.3f, 0f, e.Z > 0 ? -L.Depth / 2 + 1.3f : L.Depth / 2 - 1.3f);
                PlaceRig(corner);
                Vector3 look = -corner;
                Body(Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg, 2.4f, 24f);
                yield return Xr.Wait(0.6f);
                Shot($"room_{room}");
                Xr.Dispose();
                Xr = null;
            }
        }

        /// <summary>Bottle off the wall with the right hand, pin out with the left, horn in the left hand.</summary>
        private IEnumerator TakeAndArm(FireExtinguisher ext)
        {
            Transform handle = ext.transform.Find("CarryHandle");
            Vector3 d = Flat(handle.position - Xr.Camera.transform.position);
            Body(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 1.35f, 30f);
            yield return Xr.MoveHand(Hand.Right, handle.position, handle.rotation, 0.5f);
            Xr.Grip(Hand.Right, true);
            yield return Xr.Wait(0.2f);
            yield return Xr.MoveHand(Hand.Right, handle.position + Vector3.up * 0.07f, handle.rotation, 0.4f);
            yield return Xr.MoveHand(Hand.Right, handle.position + Vector3.up * 0.07f - d * 0.35f, handle.rotation, 0.5f);
            yield return Xr.Wait(0.3f);
            Transform ring = ext.transform.Find("SafetyPin/Ring"), rod = ext.transform.Find("SafetyPin/Rod");
            Vector3 pull = (ring.position - rod.position).normalized;
            Vector3 r0 = ring.position;
            yield return GripAt(Hand.Left, r0, 0.5f);
            for (float s = 0f; s <= 0.09f; s += 0.01f) { Xr.SetHand(Hand.Left, r0 + pull * s, Xr.HandRotation(Hand.Left)); yield return Xr.Wait(0.03f); }
            yield return Release(Hand.Left);
            Assert.IsTrue(ext.IsPinPulled, "pin pulled with the other hand");
            Transform horn = ext.transform.Find("Horn");
            yield return GripAt(Hand.Left, horn.position - horn.forward * 0.05f, 0.4f);
        }

        /// <summary>Crouched about 1.8 m away: aim the horn at the base, squeeze, sweep; returns when out or after 15 s.</summary>
        private IEnumerator Spray(Hazards.FireHazard fire, string shot)
        {
            Vector3 basePt = fire.BasePoint;
            Vector3 toFire = Flat(basePt - Xr.Camera.transform.position);
            Body(Mathf.Atan2(toFire.x, toFire.z) * Mathf.Rad2Deg, 1.2f, 25f);
            yield return Xr.Wait(0.3f);
            Vector3 eye = Xr.Camera.transform.position;
            Quaternion q = Quaternion.LookRotation(toFire);
            Vector3 hornHand = eye + q * new Vector3(-0.15f, -0.35f, 0.4f);
            yield return Xr.MoveHand(Hand.Right, eye + q * new Vector3(0.12f, -0.5f, 0.25f), q, 0.4f);
            yield return Xr.MoveHand(Hand.Left, hornHand, Quaternion.LookRotation(basePt - hornHand), 0.4f);
            Xr.Trigger(Hand.Right, 1f);
            Quaternion aim = Quaternion.LookRotation(basePt - hornHand);
            bool shotTaken = false;
            for (float t = 0f; t < 15f && !fire.IsExtinguished; t += Time.deltaTime)
            {
                Xr.SetHand(Hand.Left, hornHand, Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI) * 18f, Vector3.up) * aim);
                if (!shotTaken && t > 1.2f) { Shot(shot); shotTaken = true; }
                if (_builder.GetComponent<GeneratedDrill>().Current != GeneratedDrill.State.Running) break;
                yield return null;
            }
            Xr.Trigger(Hand.Right, 0f);
        }

        private static FireExtinguisher Extinguisher(ExtinguisherType type)
        {
            foreach (var e in Object.FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None)) if (e.Type == type) return e;
            return null;
        }

        /// <summary>A walking point 1.8 m from the fire on the learner's side.</summary>
        private Vector3 Approach(Vector3 firePoint)
        {
            Vector3 from = Xr.Camera.transform.position;
            Vector3 dir = Flat(from - firePoint);
            Vector3 p = firePoint + dir * 1.8f;
            return new Vector3(p.x, 0f, p.z);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Library_ClassA_WaterExtinguisher_PutOut_ThenLeaveByTheExit()
        {
            yield return LoadRoom(Sample("library"));
            var drill = _builder.GetComponent<GeneratedDrill>();
            Shot("library_01_start");
            var water = Extinguisher(ExtinguisherType.WaterFoam);
            Assert.IsNotNull(water, "water/foam extinguisher on the wall by the exit");
            yield return TakeAndArm(water);
            Shot("library_02_extinguisher_ready");
            yield return StickWalk(Approach(drill.Fire.BasePoint));
            yield return Spray(drill.Fire, "library_03_spraying");
            Assert.IsTrue(drill.FireOut, "class A fire put out with water/foam");
            Assert.IsFalse(drill.Reignited);
            yield return Release(Hand.Left);
            yield return Release(Hand.Right);
            var exit = _builder.Layout.Exits[0];
            Vector3 o = ScenarioBuilder.ExitBounds(exit).center;
            yield return StickWalk(new Vector3(exit.X, 0f, exit.Z), new Vector3(o.x, 0f, o.z));
            yield return Xr.Wait(0.3f);
            Assert.AreEqual(GeneratedDrill.State.Won, drill.Current, drill.Outcome);
            StringAssert.Contains("Dập tắt", drill.Outcome);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Lab_ElectricalFire_WaterOnIt_Electrocutes()
        {
            var spec = Sample("lab");
            spec["extinguishers"] = JArray.Parse("[{\"type\":\"CO2\",\"count\":1},{\"type\":\"WaterFoam\",\"count\":1}]");
            yield return LoadRoom(spec);
            var drill = _builder.GetComponent<GeneratedDrill>();
            var water = Extinguisher(ExtinguisherType.WaterFoam);
            Assert.IsNotNull(Extinguisher(ExtinguisherType.CO2), "the right one is there too");
            yield return TakeAndArm(water);
            yield return StickWalk(Approach(drill.Fire.BasePoint));
            yield return Spray(drill.Fire, "lab_wrong_agent");
            Assert.AreEqual(GeneratedDrill.State.Failed, drill.Current, "water on a live electrical fire");
            StringAssert.Contains("Điện giật", drill.Outcome);
        }
    }
}
