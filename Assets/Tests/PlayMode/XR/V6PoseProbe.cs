using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.NPC;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>Screenshots of each NPC body pose (review aid, Demo category).</summary>
    [Category("Demo")]
    public class V6PoseProbe : BuildingSceneTest
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator EachPose_Screenshot()
        {
            yield return LoadLab(TrainingMode.Guided);
            NPCSimulator hoa = null;
            foreach (var n in Object.FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None)) if (n.Role == NPCRole.HoaFaint) hoa = n;
            var pose = hoa.GetComponent<NpcBodyPose>() ?? hoa.gameObject.AddComponent<NpcBodyPose>();
            Vector3 p = hoa.transform.position;
            Vector3 eye = p + hoa.transform.forward * 2.2f + Vector3.right * 0.6f;
            eye.y = 1.4f;
            yield return StandAt(eye, p + Vector3.up * 0.8f);
            foreach (BodyPose b in System.Enum.GetValues(typeof(BodyPose)))
            {
                pose.Set(b, 0.3f);
                yield return Xr.Wait(0.6f);
                Shot("pose_" + b);
            }
        }
    }
}
