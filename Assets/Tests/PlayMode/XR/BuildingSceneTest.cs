using System.Collections;
using NUnit.Framework;
using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// Shared helpers for tests in the V2 building: start in the corridor (rig placed = scenario setup), walk with the
    /// left thumbstick (real XRI locomotion + gravity), push a fire door's bar with the palm.
    /// </summary>
    public abstract class BuildingSceneTest : XRSceneTest
    {
        /// <summary>
        /// Where the learner stands in the real play area (tracking space x/z). Thumbstick locomotion moves the rig,
        /// the head stays at this spot. Zero = centre; a test that walked room-scale first sets it to where it ended.
        /// </summary>
        protected Vector3 PlayAreaSpot;

        protected IEnumerator Setup(TrainingMode mode, Vector3 originPos, float yaw)
        {
            PlayAreaSpot = Vector3.zero;
            yield return LoadLab(mode);
            Assert.IsNotNull(GameObject.Find("[BUILDING_V2]"), "Run EscapeFire > V3 > Build Rescue Scenario");
            LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            // Scenario setup: the learner starts this test already at this spot (not a learner action).
            PlaceRig(originPos);
            Body(yaw);
            yield return Xr.Wait(0.3f);
        }

        protected void PlaceRig(Vector3 originPos)
        {
            var cc = Xr.Origin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            Xr.Origin.transform.SetPositionAndRotation(originPos, Quaternion.identity);
            if (cc != null) cc.enabled = true;
        }

        /// <summary>Head height above the rig's floor, corrected for the camera offset object (Device mode).</summary>
        protected float TrackingHeadY(float headY) => headY - (Xr.TrackingSpace.position.y - Xr.Origin.transform.position.y);

        /// <summary>Head at the centre of the play area looking along <paramref name="yaw"/>, hands relaxed in front.</summary>
        protected void Body(float yaw, float headY = 1.6f, float pitch = 0f)
        {
            headY = TrackingHeadY(headY);
            Vector3 spot = new Vector3(PlayAreaSpot.x, headY, PlayAreaSpot.z);
            Xr.SetHeadTracking(spot, yaw, pitch);
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Vector3 tl = Xr.TrackingSpace.InverseTransformDirection(q * new Vector3(-0.2f, -0.5f, 0.25f));
            Vector3 tr = Xr.TrackingSpace.InverseTransformDirection(q * new Vector3(0.2f, -0.5f, 0.25f));
            Xr.SetHandTracking(Hand.Left, spot + tl, q);
            Xr.SetHandTracking(Hand.Right, spot + tr, q);
        }

        /// <summary>Walks with the left thumbstick (head-relative) to each waypoint.</summary>
        protected IEnumerator StickWalk(params Vector3[] waypoints)
        {
            foreach (var wp in waypoints)
            {
                float t = 0f;
                while (true)
                {
                    Vector3 head = Xr.Camera.transform.position;
                    Vector3 d = wp - head;
                    d.y = 0f;
                    if (d.magnitude < 0.25f) break;
                    t += Time.deltaTime;
                    Assert.Less(t, 20f, $"stuck walking to {wp} (head at {head})");
                    Body(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
                    Xr.Thumbstick(Hand.Left, new Vector2(0f, 1f));
                    yield return null;
                }
            }
            Xr.Thumbstick(Hand.Left, Vector2.zero);
            yield return Xr.Wait(0.2f);
        }

        /// <summary>Pushes a door's bar with the right hand (4 cm in) and holds a moment.</summary>
        protected IEnumerator PushBar(PushBarDoor door)
        {
            Collider bar = null;
            foreach (var c in door.GetComponentsInChildren<Collider>(true)) if (c.name == "PushBar") bar = c;
            Vector3 push = door.transform.forward;
            Vector3 face = bar.bounds.center - push * 0.03f;
            Quaternion rot = Quaternion.LookRotation(push);
            yield return Xr.MoveHand(Hand.Right, face - push * 0.15f, rot, 0.3f);
            yield return Xr.MoveHand(Hand.Right, face + push * 0.04f, rot, 0.3f);
            yield return Xr.Wait(0.6f);
        }

        protected static PushBarDoor Door(string stairwell, string name) =>
            GameObject.Find("[BUILDING_V2]").transform.Find(stairwell + "/" + name).GetComponent<PushBarDoor>();

        /// <summary>Both hands under the victim's armpits, rescuer low, hands about 45 cm above the floor.</summary>
        protected void HoldUnderArms(VictimBody v)
        {
            Xr.SetHand(Hand.Left, v.Armpit(true) + Vector3.up * 0.33f, Quaternion.LookRotation(Flat(v.ChestPoint - Xr.Camera.transform.position)));
            Xr.SetHand(Hand.Right, v.Armpit(false) + Vector3.up * 0.33f, Quaternion.LookRotation(Flat(v.ChestPoint - Xr.Camera.transform.position)));
        }

        /// <summary>Kneeling, facing the victim, backing away with the thumbstick while the hands keep hold under the arms.</summary>
        protected IEnumerator DragBackTo(VictimBody v, params Vector3[] waypoints)
        {
            foreach (var wp in waypoints)
            {
                for (float t = 0f; ; t += Time.deltaTime)
                {
                    Vector3 head = Xr.Camera.transform.position;
                    Vector3 d = wp - head; d.y = 0f;
                    if (d.magnitude < 0.2f) break;
                    Assert.Less(t, 25f, $"stuck dragging to {wp} (head {head})");
                    Assert.IsTrue(v.IsDragged, $"lost the grip at head {head}");
                    Vector3 look = Flat(v.ChestPoint - head);
                    float yaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg;
                    Body(yaw, 0.85f);
                    Vector3 hands = head + look * 0.42f;
                    hands.y = v.Heels.y + 0.45f;
                    Vector3 side = Vector3.Cross(Vector3.up, look);
                    Xr.SetHand(Hand.Left, hands - side * 0.2f, Quaternion.LookRotation(look));
                    Xr.SetHand(Hand.Right, hands + side * 0.2f, Quaternion.LookRotation(look));
                    Vector3 local = Quaternion.Inverse(Quaternion.LookRotation(look)) * d.normalized;
                    Xr.Thumbstick(Hand.Left, new Vector2(local.x, local.z));
                    yield return null;
                }
            }
            Xr.Thumbstick(Hand.Left, Vector2.zero);
            yield return Xr.Wait(0.2f);
        }

        protected IEnumerator KneelAndGrip(VictimBody v, bool bothHands)
        {
            Vector3 look = Flat(v.ChestPoint - Xr.Camera.transform.position);
            Body(Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg, 0.85f, 35f);
            yield return Xr.Wait(0.2f);
            yield return Xr.MoveHand(Hand.Right, v.Armpit(false), Quaternion.LookRotation(look), 0.4f);
            Xr.Grip(Hand.Right, true);
            if (bothHands)
            {
                yield return Xr.MoveHand(Hand.Left, v.Armpit(true), Quaternion.LookRotation(look), 0.4f);
                Xr.Grip(Hand.Left, true);
            }
            yield return Xr.Wait(0.3f);
        }

        /// <summary>
        /// Rautek rescue of the victim lying in the corridor (learner already kneeling at his head): both hands under the
        /// armpits, drag back to the stairwell door, put him down, push the fire door's bar, kneel again in the doorway
        /// and drag him through onto the landing and the first steps.
        /// </summary>
        protected IEnumerator RautekIntoStairwellA(VictimBody v, string shotPrefix)
        {
            // both hands under the armpits: Rautek hold, drag back to the stairwell door
            yield return KneelAndGrip(v, true);
            Assert.IsTrue(v.IsDragged, "Rautek hold with both hands");
            HoldUnderArms(v);
            yield return Xr.Wait(0.5f);
            Shot(shotPrefix + "02_two_hands_under_the_arms");
            yield return DragBackTo(v, new Vector3(0.25f, 0f, -12.75f));
            Shot(shotPrefix + "03_dragging_backwards");

            // put him down, stand, push the fire door's bar, kneel again in the doorway and drag through
            Xr.Grip(Hand.Left, false); Xr.Grip(Hand.Right, false);
            yield return Xr.Wait(0.3f);
            Assert.IsFalse(v.IsDragged, "let go: he is lowered to the floor");
            var door = Door("StairwellA", "FireDoor_F3");
            Body(180f);
            yield return Xr.Wait(0.3f);
            yield return PushBar(door);
            Assert.IsTrue(door.IsOpen, "fire door pushed open");
            yield return StickWalk(new Vector3(0.3f, 0f, -12.95f)); // stand in the doorway (keeps the door open)
            yield return KneelAndGrip(v, true);
            Assert.IsTrue(v.IsDragged, "hold again");
            yield return DragBackTo(v, new Vector3(0.3f, 0f, -14.0f), new Vector3(0.65f, 0f, -14.6f), new Vector3(0.65f, 0f, -15.5f));
            yield return Xr.Wait(0.3f);
            Shot(shotPrefix + "04_in_the_stairwell");
            Debug.Log($"[Rautek] heels {v.Heels} chest {v.ChestPoint} safe={v.IsSafe} maxHead={v.MaxHeadHeight:F2} dragged={v.DragDistance:F1}m");
            Assert.IsTrue(v.IsSafe, $"victim inside the protected stairwell (heels {v.Heels}, chest {v.ChestPoint})");
            Assert.Less(v.MaxHeadHeight, 1.0f, "his head stayed low while dragged");
        }

        /// <summary>Walks up to Minh at the lift and takes him by the shoulder; he goes to the stairs.</summary>
        protected IEnumerator StopMinh(string shot = null)
        {
            var minh = Object.FindFirstObjectByType<PanicRunner>();
            Assert.IsTrue(minh.Active, "Minh is at the lift");
            yield return StickWalk(new Vector3(-1.45f, 0f, -11.15f)); // an arm's length from him
            Vector3 look = Flat(minh.ShoulderPoint - Xr.Camera.transform.position);
            Body(Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg);
            yield return GripAt(Hand.Right, minh.ShoulderPoint, 0.4f);
            yield return Xr.Wait(0.3f);
            if (shot != null) Shot(shot);
            Assert.IsTrue(minh.Stopped, "taken by the arm, he listens");
            yield return Release(Hand.Right);
        }

        /// <summary>Presses a report button on the assembly-point board with the right fingertip.</summary>
        protected IEnumerator PressReport(string button)
        {
            var b = GameObject.Find("[RESCUE_V3]").transform.Find("RollCall/ReportBoard/" + button);
            Assert.IsNotNull(b, button);
            Vector3 face = b.GetComponent<Collider>().ClosestPoint(b.position + Vector3.forward);
            yield return Poke(Hand.Right, face, Vector3.back, 0.015f);
            yield return Xr.Wait(0.3f);
        }
    }
}
