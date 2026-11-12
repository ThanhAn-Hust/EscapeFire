using System.Collections;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Interactions;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V4 Challenge Composer on the headset: composed challenges (as the edge server sends them) go through the shared
    /// Tier-1 rules and the scene checks, then play out; the learner reacts only through simulated VR input.
    /// Handing the challenge to the governor is the AI director's move, not a learner action.
    /// </summary>
    public class V4ChallengeTests : BuildingSceneTest
    {
        private static JObject Spec(string piecesJson, string rule = "R10_NO_ELEVATOR") =>
            JObject.Parse("{\"type\":\"challenge\",\"pieces\":" + piecesJson + ",\"pccc_rule_reference\":\"" + rule + "\",\"pedagogical_reason\":\"test\"}");

        private static string Give(string piecesJson, string rule = "R10_NO_ELEVATOR") =>
            MicroAIGovernor.Instance.TryExecuteChallenge(Spec(piecesJson, rule), "edge");

        [UnityTest]
        public IEnumerator Tiers_GrowOneByOne_AndPiecesAreNeverRepeated()
        {
            yield return Setup(TrainingMode.Exam, new Vector3(0.2f, 0f, -9.5f), 180f);
            StringAssert.StartsWith("tier_too_high", Give("[{\"piece\":\"BLOCK_ROUTE\",\"params\":{\"route\":\"stairwell_a\"}}]"), "no jumping to tier 3");
            Assert.IsNull(Give("[{\"piece\":\"DEVICE_FAULT\",\"params\":{\"device\":\"co2_pin_jam\"}}]", "R04_PASS"));
            StringAssert.StartsWith("repeated", Give("[{\"piece\":\"DEVICE_FAULT\",\"params\":{\"device\":\"co2_pin_jam\"}}]", "R04_PASS"));
            Assert.IsNull(Give("[{\"piece\":\"LIGHTS_OFF\",\"params\":{\"zone\":\"corridor\"}},{\"piece\":\"SMOKE_SURGE\",\"params\":{\"drop_m\":0.5,\"speed\":1.5},\"delay_s\":2}]", "R07_STAY_LOW"));
            Assert.AreEqual(3, ChallengeRunner.Instance.AppliedCodes.Count, "three pieces applied over two challenges");
        }

        [UnityTest]
        public IEnumerator Tier2_NeverBlocksTheLastWayDown()
        {
            yield return Setup(TrainingMode.Exam, new Vector3(0.2f, 0f, -9.5f), 180f);
            Assert.IsNull(Give("[{\"piece\":\"DEVICE_FAULT\",\"params\":{\"device\":\"co2_pin_jam\"}}]", "R04_PASS"));
            Assert.IsNull(Give("[{\"piece\":\"LIGHTS_OFF\",\"params\":{\"zone\":\"corridor\"}}]"));
            Assert.IsNull(Give("[{\"piece\":\"BLOCK_ROUTE\",\"params\":{\"route\":\"stairwell_b\"}}]"));
            Assert.AreEqual("no_exit_left", Give("[{\"piece\":\"BLOCK_ROUTE\",\"params\":{\"route\":\"stairwell_a\"}}]"), "one way down always stays open");
        }

        [UnityTest]
        public IEnumerator Tier2_NeverBlocksTheStairwellTheLearnerIsIn()
        {
            // the learner is already on the landing of stairwell A — blocking A would trap them
            yield return Setup(TrainingMode.Exam, new Vector3(0.6f, 0f, -14.3f), 180f);
            Give("[{\"piece\":\"DEVICE_FAULT\",\"params\":{\"device\":\"co2_pin_jam\"}}]", "R04_PASS");
            Give("[{\"piece\":\"LIGHTS_OFF\",\"params\":{\"zone\":\"stairwell_a\"}}]");
            Assert.AreEqual("learner_inside_route", Give("[{\"piece\":\"BLOCK_ROUTE\",\"params\":{\"route\":\"stairwell_a\"}}]"));
        }

        [UnityTest, Timeout(400000)]
        public IEnumerator StairwellABlocked_LearnerFindsTheBarDead_TakesStairwellB_ToTheAssemblyPoint()
        {
            yield return Setup(TrainingMode.Guided, new Vector3(0.2f, 0f, -11.6f), 180f);
            Assert.IsNull(Give("[{\"piece\":\"DEVICE_FAULT\",\"params\":{\"device\":\"co2_pin_jam\"}}]", "R04_PASS"));
            Assert.IsNull(Give("[{\"piece\":\"LIGHTS_OFF\",\"params\":{\"zone\":\"corridor\"}}]"));
            Assert.IsNull(Give("[{\"piece\":\"BLOCK_ROUTE\",\"params\":{\"route\":\"stairwell_a\"}}]"));
            yield return Xr.Wait(0.5f);
            Shot("v4_01_corridor_dark_stairwell_a_blocked");

            string lastMentor = "";
            System.Action<MentorMessageEvent> listen = e => lastMentor = e.Message;
            EventBus<MentorMessageEvent>.Subscribe(listen);
            try
            {
                var doorA = Door("StairwellA", "FireDoor_F3");
                yield return StickWalk(new Vector3(0.2f, 0f, -12.4f));
                Body(180f);
                yield return PushBar(doorA);
                yield return Xr.Wait(0.5f);
                Assert.IsFalse(doorA.IsOpen, "the blocked fire door does not open");
                StringAssert.Contains("thang B", lastMentor, "the learner is told why and where to go");

                // stairwell B: door on the east wall of the corridor, flights go east
                var doorB = Door("StairwellB", "FireDoor_F3");
                yield return StickWalk(new Vector3(1.6f, 0f, -11.2f), new Vector3(2.2f, 0f, -10.5f));
                Body(90f);
                yield return PushBar(doorB);
                Shot("v4_02_stairwell_b_open");
                Assert.IsTrue(doorB.IsOpen, "the other stairwell opens");
                Vector3 B(float x, float z) => new Vector3(3f + z, 0f, -10.5f - x); // stairwell B local -> world
                yield return StickWalk(B(0f, 0.7f), B(-0.65f, 1.2f), B(-0.65f, 4.2f), B(0f, 4.75f), B(0.65f, 4.2f), B(0.65f, 1.2f), B(0f, 0.6f),
                                       B(-0.65f, 1.2f), B(-0.65f, 4.2f), B(0f, 4.75f), B(0.65f, 4.2f), B(0.65f, 1.2f), B(0.6f, 0.7f));
                Assert.AreEqual(-8f, Xr.Origin.transform.position.y, 0.35f, "walked down stairwell B");
                var exit = Door("StairwellB", "ExitDoor_Ground");
                yield return PushBar(exit);
                Assert.IsTrue(exit.IsOpen, "ground exit of B");
                yield return StickWalk(B(2.4f, 0.7f), new Vector3(3.7f, 0f, -20f), new Vector3(-7f, 0f, -22.5f));
                Shot("v4_03_out_by_stairwell_b");
                var ap = AssemblyPoint.Instance;
                Assert.IsTrue(ap.Arrived, "at the assembly point");
                yield return StickWalk(new Vector3(-7f, 0f, -23.5f));
                Body(180f);
                yield return PressReport(ap.Expected == AssemblyPoint.Report.StillInsideStairwell ? "Report_Stairwell" : "Report_CorridorLift");
                Assert.AreEqual(ScenarioPhase.Phase4_CompletedWin, LabProgressiveScenarioController.Instance.CurrentPhase);
            }
            finally
            {
                EventBus<MentorMessageEvent>.Unsubscribe(listen);
            }
        }
    }
}
