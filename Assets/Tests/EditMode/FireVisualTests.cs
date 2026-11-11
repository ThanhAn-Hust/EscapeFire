using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using EscapeFire.Hazards;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.EditMode
{
    /// <summary>Regressions found while recording the real-LLM demo (2026-11-11).</summary>
    public class FireVisualTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private FireHazard MakeRackWithFlames(out GameObject flames, out Renderer rackMesh)
        {
            _root = GameObject.CreatePrimitive(PrimitiveType.Cube); // stands in for the server rack mesh
            _root.name = "Rack";
            rackMesh = _root.GetComponent<Renderer>();
            flames = new GameObject("Flames");
            flames.transform.SetParent(_root.transform, false);
            flames.transform.localPosition = new Vector3(0.2f, 1.4f, -0.3f);
            var ps = flames.AddComponent<ParticleSystem>();
            flames.SetActive(false); // how the lab scene stores it before the fire starts
            var fire = _root.AddComponent<FireHazard>();
            typeof(FireHazard).GetField("fireParticles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(fire, ps);
            return fire;
        }

        [Test]
        public void ResetFire_ActivatesInactiveFlameObject()
        {
            var fire = MakeRackWithFlames(out var flames, out _);
            fire.ResetFire();
            Assert.IsTrue(flames.activeSelf, "Flames must become visible when the fire starts");
        }

        [Test]
        public void ResetFire_FindsUnassignedFlamesInChildren()
        {
            var fire = MakeRackWithFlames(out var flames, out _);
            typeof(FireHazard).GetField("fireParticles", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(fire, null);
            fire.ResolveParticles();
            fire.ResetFire();
            Assert.AreSame(flames.GetComponent<ParticleSystem>(), fire.Particles);
            Assert.IsTrue(flames.activeSelf);
        }

        [Test]
        public void SecondaryFireClone_ShowsOnlyFlames()
        {
            var fire = MakeRackWithFlames(out var flames, out var rackMesh);
            SecondaryFireSlot.HideEverythingButFlames(fire);
            Assert.IsFalse(rackMesh.enabled, "The cloned rack mesh must not appear on the desk");
            Assert.IsFalse(_root.GetComponent<Collider>().enabled);
            Assert.AreEqual(0f, flames.transform.localPosition.x);
            Assert.AreEqual(0f, flames.transform.localPosition.z);
        }

        [Test]
        public void PermanentRejections_CountAsUsedChallenges()
        {
            Assert.IsTrue(EdgeDirectorClient.CountsAsUsed(DirectiveResult.Accepted));
            Assert.IsTrue(EdgeDirectorClient.CountsAsUsed(DirectiveResult.RejectedAlarmAlreadyRaised));
            Assert.IsTrue(EdgeDirectorClient.CountsAsUsed(DirectiveResult.RejectedAlreadyUnpinned));
            Assert.IsFalse(EdgeDirectorClient.CountsAsUsed(DirectiveResult.RejectedLearnerPanicking));
            Assert.IsFalse(EdgeDirectorClient.CountsAsUsed(DirectiveResult.RejectedBudgetOverflow));
        }
    }
}
