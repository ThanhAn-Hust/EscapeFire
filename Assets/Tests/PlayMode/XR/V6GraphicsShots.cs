using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// V6 graphics review: screenshots of the dressed building before the fire (lab, corridor, stairs, yard, facade),
    /// for a human to judge. No assertions on looks; category Demo keeps it out of the regular suite.
    /// The rig is placed at each viewpoint (scenario setup, not a learner action).
    /// </summary>
    [Category("Demo")]
    public class V6GraphicsShots : BuildingSceneTest
    {
        private struct View { public string Name; public Vector3 Origin; public float Yaw, Pitch, Head; }

        private static readonly View[] Views =
        {
            new View { Name = "lab", Origin = new Vector3(-6f, 0f, -5.5f), Yaw = 45f, Pitch = -12f, Head = 1.65f },
            new View { Name = "corridor_ceiling", Origin = new Vector3(0.3f, 0f, -7.6f), Yaw = 180f, Pitch = -18f, Head = 1.65f },
            new View { Name = "corridor_lift", Origin = new Vector3(1.6f, 0f, -9.6f), Yaw = 250f, Pitch = 0f, Head = 1.65f },
            new View { Name = "corridor_plan", Origin = new Vector3(1.4f, 0f, -8.3f), Yaw = 90f, Pitch = 5f, Head = 1.65f },
            new View { Name = "stairs_down", Origin = new Vector3(0.45f, 0f, -14.0f), Yaw = 180f, Pitch = 35f, Head = 1.65f },
            new View { Name = "stairs_mid_landing", Origin = new Vector3(0f, -2f, -18.0f), Yaw = 0f, Pitch = -20f, Head = 1.65f },
            new View { Name = "yard_building", Origin = new Vector3(-8f, -8f, -32f), Yaw = 20f, Pitch = -12f, Head = 1.65f },
            new View { Name = "yard_side", Origin = new Vector3(-17f, -8f, -14f), Yaw = 70f, Pitch = -10f, Head = 1.65f },
        };

        [UnityTest, Timeout(300000)]
        public IEnumerator DressedBuilding_Screenshots()
        {
            yield return LoadLab(TrainingMode.Guided);
            Assert.IsNotNull(GameObject.Find("[DRESSING]"), "Run EscapeFire > V3 > Build Rescue Scenario");
            foreach (var v in Views)
            {
                PlaceRig(v.Origin);
                Body(v.Yaw, v.Head, v.Pitch);
                yield return Xr.Wait(0.6f);
                Shot("v6_" + v.Name);
            }
        }
    }
}
