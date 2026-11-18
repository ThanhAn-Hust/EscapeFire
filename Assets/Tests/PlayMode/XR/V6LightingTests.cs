using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Hazards;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V6 graphics: the ceiling light panels are drawn unlit, so they must go dark with the room lights in a blackout
    /// (only the battery emergency lights stay on) and come back when power returns. The blackout itself is the AI
    /// director's TRIGGER_BLACKOUT; here it is triggered directly (scenario setup).
    /// </summary>
    public class V6LightingTests : BuildingSceneTest
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator Blackout_TurnsOffCeilingPanels_AndRestores()
        {
            yield return LoadLab(TrainingMode.Guided);
            var lamps = Object.FindObjectsByType<LampFixture>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(lamps.Length, 8, "lab and corridor light panels");
            Assert.That(lamps.All(l => l.IsOn), "lit before the blackout");
            var blackout = Object.FindFirstObjectByType<BlackoutController>();
            if (blackout == null) blackout = new GameObject("Blackout").AddComponent<BlackoutController>();
            PlaceRig(new Vector3(-6f, 0f, -5.5f));
            Body(45f, 1.65f, -12f);
            yield return Xr.Wait(0.3f);
            Shot("lab_ceiling_lit");
            Assert.IsTrue(blackout.Trigger(), "blackout started");
            yield return Xr.Wait(0.3f);
            Shot("lab_ceiling_blackout");
            Assert.That(lamps.All(l => !l.IsOn), "panels dark in the blackout");
            blackout.Restore();
            yield return null;
            Assert.That(lamps.All(l => l.IsOn), "panels back on with the power");
        }
    }
}
