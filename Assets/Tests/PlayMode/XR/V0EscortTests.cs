using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.NPC;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>V0.8 / B.1-10 — get down, take Hoa under the arm, lift her and support her out.</summary>
    public class V0EscortTests : XRSceneTest
    {
        private NPCSimulator _hoa;
        private EscortGrip _grip;

        private IEnumerator Setup(float eyeHeight)
        {
            yield return LoadLab();
            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            foreach (var n in Object.FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None)) if (n.Role == NPCRole.HoaFaint) _hoa = n;
            _hoa.TriggerEmergencyReaction(); // scenario: she has fainted (not a learner action)
            _grip = _hoa.GetComponent<EscortGrip>();
            Assert.IsNotNull(_grip, "Run EscapeFire > V0 > Setup Realistic Rig");
            yield return Xr.Wait(0.3f);
            Vector3 g = _grip.GripPoint;
            Vector3 eye = new Vector3(g.x + 0.55f, eyeHeight, g.z - 0.35f);
            yield return StandAt(eye, g);
        }

        private IEnumerator TakeArm()
        {
            yield return GripAt(Hand.Right, _grip.GripPoint, 0.5f);
            yield return Xr.Wait(0.3f);
        }

        /// <summary>Walks the supporting hand (and head) along a path at a given speed.</summary>
        private IEnumerator WalkHand(Vector3 delta, float speed)
        {
            Vector3 hand0 = Xr.HandPosition(Hand.Right), head0 = Xr.HeadPosition;
            float seconds = delta.magnitude / speed;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Vector3 d = delta * (t / seconds);
                Xr.SetHand(Hand.Right, hand0 + d + Vector3.up * Mathf.Min(0.6f, t * 2f), Xr.HandRotation(Hand.Right));
                Xr.LookAt(head0 + d, head0 + d + delta.normalized);
                yield return null;
            }
            yield return Xr.Wait(0.3f);
        }

        [UnityTest]
        public IEnumerator StandingUpright_CannotLiftHer()
        {
            yield return Setup(1.65f);
            yield return TakeArm();
            Assert.IsFalse(_hoa.IsRescued, "you get down to someone lying in smoke before lifting her");
        }

        [UnityTest]
        public IEnumerator Crouched_TakeArm_SheStandsAndFollows()
        {
            yield return Setup(0.95f);
            Vector3 start = _hoa.transform.position;
            yield return TakeArm();
            Shot("taking_her_arm");
            Assert.IsTrue(_hoa.IsRescued, "taking her under the arm while crouched helps her up");
            yield return WalkHand(new Vector3(0f, 0f, -2f), 0.6f);
            Vector3 side = Xr.HeadPosition + new Vector3(1.4f, 0.4f, 0.6f);
            Xr.LookAt(side, _hoa.transform.position + Vector3.up * 0.8f);
            yield return Xr.Wait(0.1f);
            Shot("supporting_her");
            Assert.Greater(Vector3.Distance(start, _hoa.transform.position), 1.2f, "she walks where the supporting hand goes");
            Assert.IsTrue(_grip.IsSupported);
        }

        [UnityTest]
        public IEnumerator LetGo_SheStops()
        {
            yield return Setup(0.95f);
            yield return TakeArm();
            yield return Release(Hand.Right);
            Vector3 at = _hoa.transform.position;
            yield return WalkHand(new Vector3(0f, 0f, -2f), 0.6f);
            Assert.Less(Vector3.Distance(at, _hoa.transform.position), 0.3f, "without support she does not walk off on her own");
        }

        [UnityTest]
        public IEnumerator TooFast_LosesGrip()
        {
            yield return Setup(0.95f);
            yield return TakeArm();
            yield return WalkHand(new Vector3(0f, 0f, -3f), 6f);
            Assert.IsFalse(_grip.IsSupported, "dragging her at a run, you lose her");
        }
    }
}
