using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Player;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode
{
    /// <summary>
    /// Runs the real lab scene with physics and checks what a VR player would notice:
    /// nothing falls / flies away, NPCs stand on the floor, and the drill logic works end to end.
    /// Run: Window > General > Test Runner > PlayMode (or Unity MCP run_tests mode=PlayMode).
    /// </summary>
    public class LabScenePlayModeTests
    {
        private const float SETTLE_SECONDS = 3f;
        private const float MAX_DRIFT = 0.05f;

        private static readonly WaitForSeconds SETTLE = new WaitForSeconds(SETTLE_SECONDS);
        private static readonly WaitForSeconds HALF_SECOND = new WaitForSeconds(0.5f);
        private static readonly WaitForSeconds ONE_SECOND = new WaitForSeconds(1f);
        private static readonly WaitForSeconds ONE_AND_HALF = new WaitForSeconds(1.5f);
        private static readonly WaitForSeconds MINH_RUN = new WaitForSeconds(3.5f);

        private float _floorY;
        private Bounds _room;

        [UnitySetUp]
        public IEnumerator LoadLab()
        {
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "PLAYMODE_TEST"); // no adaptive escalations during tests
            SessionResult.Reset();
            yield return SceneManager.LoadSceneAsync(SceneNames.LAB, LoadSceneMode.Single);
            yield return null;

            var floor = GameObject.Find("Floor_HighRise");
            Assert.IsNotNull(floor, "Floor_HighRise missing from the lab");
            _room = floor.GetComponent<Renderer>().bounds;
            _floorY = _room.max.y;

            // Keep the result screen from loading in the middle of a test.
            var reporter = Object.FindFirstObjectByType<SessionOutcomeReporter>();
            if (reporter != null)
            {
                typeof(SessionOutcomeReporter).GetField("loadAARScene", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(reporter, false);
            }
        }

        // ------------------------------------------------------------------ placement / physics

        [UnityTest]
        public IEnumerator NothingFallsOrDriftsDuringPeacefulPhase()
        {
            var tracked = new List<(Transform t, Vector3 start)>();
            foreach (var rb in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)) tracked.Add((rb.transform, rb.transform.position));
            foreach (var npc in Object.FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None)) tracked.Add((npc.transform, npc.transform.position));

            yield return SETTLE;

            var problems = new List<string>();
            foreach (var (t, start) in tracked)
            {
                if (t == null) { problems.Add("object destroyed while idle"); continue; }
                float drift = Vector3.Distance(t.position, start);
                if (drift > MAX_DRIFT) problems.Add($"'{t.name}' moved {drift:F2} m (from {start} to {t.position})");
            }
            Assert.IsEmpty(problems, "Objects must stay put before the fire:\n" + string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator EveryRendererInsideTheLabIsBetweenFloorAndCeiling()
        {
            yield return HALF_SECOND;
            var ceiling = GameObject.Find("Ceiling_HighRise");
            float ceilingY = ceiling != null ? ceiling.GetComponent<Renderer>().bounds.max.y : 5f;

            var problems = new List<string>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer) continue;
                Bounds b = r.bounds;
                bool inside = b.center.x > _room.min.x && b.center.x < _room.max.x && b.center.z > _room.min.z && b.center.z < _room.max.z;
                if (!inside || r.name.Contains("Smoke")) continue;
                if (b.max.y < _room.min.y - 0.05f || b.min.y > ceilingY + 0.05f)
                    problems.Add($"'{r.name}' at y {b.min.y:F2}..{b.max.y:F2}");
            }
            Assert.IsEmpty(problems, "Renderers below the floor / above the ceiling:\n" + string.Join("\n", problems));
        }

        [UnityTest]
        public IEnumerator NpcsStandOnTheFloor()
        {
            yield return null;
            var npcs = Object.FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None);
            Assert.That(npcs.Length, Is.GreaterThanOrEqualTo(2), "Expected NPC Minh and NPC Hoa");
            var roles = new HashSet<NPCRole>();
            foreach (var npc in npcs)
            {
                Assert.IsTrue(roles.Add(npc.Role), $"Two NPCs share role {npc.Role}");
                Assert.AreEqual(_floorY, npc.transform.position.y, 0.1f, $"NPC '{npc.name}' feet are not on the floor");
                Assert.AreEqual(0f, Vector3.Angle(npc.transform.up, Vector3.up), 1f, $"NPC '{npc.name}' is not upright");
            }
        }

        [UnityTest]
        public IEnumerator ExtinguishersStayOnTheirWallBracket()
        {
            var ext = Object.FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None);
            Assert.IsNotEmpty(ext, "No extinguishers in the lab");
            var start = new Vector3[ext.Length];
            for (int i = 0; i < ext.Length; i++) start[i] = ext[i].transform.position;
            yield return SETTLE;
            for (int i = 0; i < ext.Length; i++)
            {
                Assert.Less(Vector3.Distance(start[i], ext[i].transform.position), MAX_DRIFT, $"'{ext[i].name}' fell off its bracket");
                Assert.Greater(ext[i].transform.position.y, _floorY + 0.3f, $"'{ext[i].name}' is lying on the floor");
            }
        }

        // ------------------------------------------------------------------ gameplay logic

        [UnityTest]
        public IEnumerator OxygenStaysFullWhileStandingBeforeTheFire()
        {
            var oxygen = Object.FindFirstObjectByType<OxygenSystem>();
            Assert.IsNotNull(oxygen);
            yield return SETTLE;
            Assert.AreEqual(oxygen.MaxOxygen, oxygen.CurrentOxygen, 0.01f, "Oxygen must not drop when there is no smoke");
        }

        [UnityTest]
        public IEnumerator FullDrill_CutPowerExtinguishEvacuate_IsAPass()
        {
            var controller = LabProgressiveScenarioController.Instance;
            Assert.IsNotNull(controller);
            controller.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return null;

            Assert.IsNotNull(controller.ServerFire, "Server fire missing");
            Assert.IsTrue(controller.ServerFire.isActiveAndEnabled, "Fire must be visible in phase 2");

            Object.FindFirstObjectByType<FireAlarmButton>().PushAlarmButton();
            Object.FindFirstObjectByType<CircuitBreakerSwitch>().SetPowerState(false);
            yield return HALF_SECOND;

            controller.ServerFire.Extinguish(1e6f);
            yield return null;
            Assert.AreEqual(ScenarioPhase.Phase3_SmokeEvacuation, controller.CurrentPhase);

            controller.TriggerHallwayEvacuationWin();
            yield return null;
            Assert.AreEqual(ScenarioPhase.Phase4_CompletedWin, controller.CurrentPhase);
            Assert.IsTrue(SessionResult.HasResult, "Result must be recorded for the AAR");
            Assert.IsTrue(SessionResult.Passed);
            Assert.GreaterOrEqual(EscapeFire.Analytics.AfterActionReport.Total(SessionResult.Scorecard), 30);
        }

        [UnityTest]
        public IEnumerator WaterOnLivePanel_IsElectrocutionFail()
        {
            var controller = LabProgressiveScenarioController.Instance;
            controller.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return null;

            FireExtinguisher water = null;
            foreach (var e in Object.FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None))
                if (e.Type == ExtinguisherType.WaterFoam) water = e;
            Assert.IsNotNull(water, "Scenario needs a water/foam extinguisher (the wrong choice)");

            water.PullSafetyPin();
            water.TrySpray(isElectricalPowerLive: true);
            yield return null;

            Assert.AreEqual(ScenarioPhase.Phase5_GameOverFail, controller.CurrentPhase);
            StringAssert.Contains("Điện giật", controller.FailCause);
        }

        [UnityTest]
        public IEnumerator Minh_RunsAlongTheFloorWhenFireStarts()
        {
            NPCSimulator minh = null;
            foreach (var n in Object.FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None))
                if (n.Role == NPCRole.MinhPanic) minh = n;
            Assert.IsNotNull(minh);
            Vector3 start = minh.transform.position;

            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return MINH_RUN;

            if (minh == null || !minh.gameObject.activeInHierarchy) yield break; // already left through the door
            Vector3 now = minh.transform.position;
            Assert.Greater(Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(now.x, 0, now.z)), 0.3f, "Minh did not run");
            Assert.AreEqual(start.y, now.y, 0.02f, "Minh must stay on the floor while running");
        }

        [UnityTest]
        public IEnumerator Hoa_FaintsAndCanBeRescued()
        {
            NPCSimulator hoa = null;
            foreach (var n in Object.FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None))
                if (n.Role == NPCRole.HoaFaint) hoa = n;
            Assert.IsNotNull(hoa, "No NPC with role HoaFaint");

            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            hoa.TriggerEmergencyReaction();
            yield return null;
            Assert.IsTrue(hoa.IsFainted);

            hoa.RescueNPC();
            yield return null;
            Assert.IsTrue(hoa.IsRescued);
        }

        [UnityTest]
        public IEnumerator SmokeLowersOnlyWhileTheFireBurns()
        {
            var smoke = Object.FindFirstObjectByType<SmokeDensityController>();
            Assert.IsNotNull(smoke, "No smoke layer in the lab");
            float start = smoke.CurrentSmokeHeight;
            yield return ONE_SECOND;
            Assert.AreEqual(start, smoke.CurrentSmokeHeight, 0.001f, "Smoke must not lower before the fire");

            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return ONE_AND_HALF;
            Assert.Less(smoke.CurrentSmokeHeight, start, "Smoke must lower while the server fire burns");
        }

        [UnityTest]
        public IEnumerator ExitZoneIsBeyondTheExitDoor()
        {
            yield return null;
            var zone = Object.FindFirstObjectByType<EvacuationExitZone>();
            var door = GameObject.Find("Main_Exit_Door_Double");
            Assert.IsNotNull(zone, "No EvacuationExitZone");
            Assert.IsNotNull(door, "No Main_Exit_Door_Double");
            var col = zone.GetComponent<Collider>();
            Assert.IsTrue(col.isTrigger);
            Assert.IsFalse(_room.Contains(new Vector3(col.bounds.center.x, _room.center.y, col.bounds.center.z)),
                "The win zone must be outside the lab (in the hallway)");
            Assert.LessOrEqual(col.bounds.min.y, _floorY + 0.1f, "Win zone must reach the floor so the player triggers it");
        }
    }
}
