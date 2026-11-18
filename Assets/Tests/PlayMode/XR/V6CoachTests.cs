using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Interactions;
using EscapeFire.Scenario;
using EscapeFire.UI;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// A first-timer who freezes when the fire breaks out (playtest 18/11) gets more and more help on ONE action at a
    /// time: asked, then told, then the object lights up, then an arrow leads the way; the current step stays as a
    /// subtitle in front of the eyes. Done → the next step. Exam mode gives no such help.
    /// </summary>
    public class V6CoachTests : BuildingSceneTest
    {
        private static GuidedCoach Coach => Object.FindFirstObjectByType<GuidedCoach>();

        [UnityTest, Timeout(180000)]
        public IEnumerator Guided_LearnerFreezes_HelpEscalates_OneStepAtATime()
        {
            yield return LoadLab(TrainingMode.Guided);
            PlaceRig(new Vector3(-0.5f, 0f, -4.5f));
            Body(40f);
            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return Xr.Wait(1f);
            Assert.IsNotNull(Coach, "the guided coach runs in the lab");
            Assert.AreEqual("alarm", Coach.CurrentStep, "first: raise the alarm");
            Assert.AreEqual(0, Coach.HintLevel, "first it asks (retrieval)");
            StringAssert.Contains("ĐẦU TIÊN", ViewSubtitle.Instance.Shown, "the question is in front of the eyes");
            var voice = Object.FindFirstObjectByType<MentorVoice>();
            Assert.IsNotNull(voice, "the Mentor has a voice");
            StringAssert.Contains("ĐẦU TIÊN", voice.LastSpoken, "and says it aloud");
            Assert.AreEqual("pack", voice.LastSource, "from the Vietnamese voice pack built into the game (no server needed)");

            yield return Xr.Wait(GuidedCoach.TellAfter);
            Assert.AreEqual(1, Coach.HintLevel, "then it tells the action");
            yield return Xr.Wait(GuidedCoach.ShowAfter - GuidedCoach.TellAfter);
            Assert.AreEqual(2, Coach.HintLevel);
            var beacon = GameObject.Find("CoachBeacon");
            Assert.IsNotNull(beacon, "the call point lights up");
            var alarm = Object.FindFirstObjectByType<FireAlarmButton>();
            Assert.Less(Vector3.Distance(new Vector3(beacon.transform.position.x, 0f, beacon.transform.position.z),
                new Vector3(alarm.transform.position.x, 0f, alarm.transform.position.z)), 0.5f, "over the call point");
            Shot("coach_01_beacon_on_call_point");
            yield return Xr.Wait(GuidedCoach.LeadAfter - GuidedCoach.ShowAfter + 0.5f);
            Assert.AreEqual(3, Coach.HintLevel);
            Assert.IsNotNull(GameObject.Find("CoachGuideArrow"), "an arrow in front of the learner leads the way");
            Body(0f, 1.6f, 40f);
            yield return Xr.Wait(0.3f);
            Shot("coach_02_guide_arrow");

            alarm.PushAlarmButton(); // the action itself is VR-tested in V0CallPointTests
            yield return Xr.Wait(0.5f);
            Assert.AreNotEqual("alarm", Coach.CurrentStep, "done: on to the next action");
            Assert.AreEqual(0, Coach.HintLevel, "and it starts by asking again");
            Assert.IsFalse(GameObject.Find("CoachBeacon") != null && GameObject.Find("CoachBeacon").activeInHierarchy, "the light moved off");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Exam_NoCoaching()
        {
            yield return LoadLab(TrainingMode.Exam);
            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            yield return Xr.Wait(GuidedCoach.ShowAfter + 1f);
            Assert.IsNull(Coach.CurrentStep, "no step-by-step help in the exam");
            Assert.IsNull(GameObject.Find("CoachBeacon"), "nothing lights up in the exam");
        }
    }
}
