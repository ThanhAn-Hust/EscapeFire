using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Lesson;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>V1.4 — the five lesson stations, each practised with the VR hands and answered with a fingertip.</summary>
    public class V1LessonTests : XRSceneTest
    {
        private LessonController _lesson;

        private IEnumerator LoadLesson()
        {
            ProgressionStore.KeyPrefix = "EF_test_progress_";
            ProgressionStore.ResetAll();
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "XRTEST");
            yield return SceneManager.LoadSceneAsync(SceneNames.LESSON, LoadSceneMode.Single);
            yield return null;
            Xr = XRInputDriver.Create();
            _lesson = Object.FindFirstObjectByType<LessonController>();
            Assert.IsNotNull(_lesson, "Run EscapeFire > V1 > Build Lesson Room");
            yield return null;
        }

        [TearDown]
        public void ResetProgressKeys()
        {
            ProgressionStore.ResetAll();
            ProgressionStore.KeyPrefix = "EF_progress_";
        }

        private LessonStation St(int i) => _lesson.Stations[i];

        private IEnumerator Answer(LessonStation s, char letter)
        {
            var btn = s.transform.Find("Answer_" + letter);
            Vector3 face = btn.GetComponent<Collider>().ClosestPoint(btn.position + Vector3.up);
            yield return Poke(Hand.Right, face, Vector3.down, 0.015f);
            yield return Xr.Wait(0.2f);
        }

        private IEnumerator Visit(Vector3 tableCentre, float eyeY = 1.6f)
        {
            Vector3 eye = new Vector3(tableCentre.x, eyeY, tableCentre.z - 0.75f);
            yield return StandAt(eye, tableCentre + Vector3.up * 0.9f);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator AllStations_ThroughVRInputOnly()
        {
            yield return LoadLesson();
            Shot("00_lesson_room");

            // ---- Station 1: put the lid on the burning tray, then answer B (oxygen)
            var s1 = St(0);
            Transform lid = s1.transform.Find("Lid"), tray = s1.transform.Find("Tray");
            yield return Visit(new Vector3(tray.position.x + 0.15f, 0f, tray.position.z));
            Shot("01_tray_fire");
            yield return GripAt(Hand.Right, lid.position, 0.5f);
            yield return Xr.MoveHand(Hand.Right, tray.position + Vector3.up * 0.15f, Quaternion.identity, 0.5f);
            yield return Xr.MoveHand(Hand.Right, tray.position + Vector3.up * 0.04f, Quaternion.identity, 0.3f);
            yield return Xr.Wait(0.3f);
            Shot("02_lid_on_tray");
            yield return Release(Hand.Right);
            yield return Xr.Wait(0.2f);
            Assert.IsTrue(s1.PracticeDone, "lid smothered the tray fire");
            yield return Answer(s1, 'A');
            Assert.IsFalse(s1.QuizDone, "wrong answer does not pass");
            yield return Answer(s1, 'B');
            Assert.IsTrue(s1.IsComplete, "station 1 complete");

            // ---- Station 2: put the CO2 extinguisher on the electrical-fire stand, answer B
            var s2 = St(1);
            Transform co2Mini = s2.transform.Find("Mini_CO2"), water = s2.transform.Find("Mini_WaterFoam"), socket = s2.transform.Find("Socket");
            yield return Visit(new Vector3(s2.transform.Find("Table").position.x, 0f, socket.position.z));
            yield return GripAt(Hand.Right, water.GetComponent<Collider>().bounds.center, 0.5f);
            yield return Xr.MoveHand(Hand.Right, socket.position, Quaternion.identity, 0.6f);
            yield return Release(Hand.Right);
            yield return Xr.Wait(0.4f);
            Shot("03_water_on_electrical_stand");
            Assert.IsFalse(s2.PracticeDone, "water is wrong for a live electrical fire");
            yield return GripAt(Hand.Right, water.GetComponent<Collider>().bounds.center, 0.4f);
            yield return Xr.MoveHand(Hand.Right, socket.position + new Vector3(-0.5f, 0.1f, 0f), Quaternion.identity, 0.5f);
            yield return Release(Hand.Right);
            yield return GripAt(Hand.Right, co2Mini.GetComponent<Collider>().bounds.center, 0.5f);
            yield return Xr.MoveHand(Hand.Right, socket.position, Quaternion.identity, 0.6f);
            yield return Release(Hand.Right);
            yield return Xr.Wait(0.4f);
            Shot("04_co2_on_electrical_stand");
            Assert.IsTrue(s2.PracticeDone, "CO2 is the right agent");
            yield return Answer(s2, 'B');
            Assert.IsTrue(s2.IsComplete);

            // ---- Station 3: PASS on the training fire
            var s3 = St(2);
            var ext = s3.GetComponentInChildren<FireExtinguisher>();
            var fire = s3.transform.Find("TrainingFire").GetComponent<FireHazard>();
            Transform handle = ext.transform.Find("CarryHandle"), pin = ext.transform.Find("SafetyPin"), horn = ext.transform.Find("Horn");
            yield return StandAt(new Vector3(handle.position.x - 0.1f, 1.4f, handle.position.z - 0.55f), handle.position);
            yield return Xr.MoveHand(Hand.Right, handle.position, handle.rotation, 0.5f);
            Xr.Grip(Hand.Right, true);
            yield return Xr.Wait(0.2f);
            yield return Xr.MoveHand(Hand.Right, handle.position + Vector3.up * 0.07f, handle.rotation, 0.4f);
            Vector3 basePt = fire.BasePoint;
            Vector3 eye = new Vector3(basePt.x, 1.25f, basePt.z + 2.0f);
            Quaternion toFire = Quaternion.LookRotation(Flat(basePt - eye));
            Xr.LookAt(eye, basePt);
            yield return Xr.MoveHand(Hand.Right, eye + toFire * new Vector3(0.12f, -0.5f, 0.3f), toFire, 0.6f);
            yield return Xr.Wait(0.4f);
            Vector3 ring = pin.position + ext.transform.right * 0.035f;
            yield return GripAt(Hand.Left, ring, 0.4f);
            for (float d = 0f; d <= 0.09f; d += 0.01f) { Xr.SetHand(Hand.Left, ring + ext.transform.right * d, Xr.HandRotation(Hand.Left)); yield return Xr.Wait(0.03f); }
            yield return Release(Hand.Left);
            Assert.IsTrue(ext.IsPinPulled, "pin pulled");
            yield return GripAt(Hand.Left, horn.position - horn.forward * 0.05f, 0.4f);
            Vector3 hornHand = eye + toFire * new Vector3(-0.15f, -0.35f, 0.4f);
            Quaternion aim = Quaternion.LookRotation(basePt - hornHand);
            yield return Xr.MoveHand(Hand.Left, hornHand, aim, 0.4f);
            Xr.Trigger(Hand.Right, 1f);
            bool shot = false;
            for (float t = 0f; t < 10f && !fire.IsExtinguished; t += Time.deltaTime)
            {
                Xr.SetHand(Hand.Left, hornHand, Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI) * 18f, Vector3.up) * aim);
                if (!shot && t > 1f) { Shot("05_pass_training_fire"); shot = true; }
                yield return null;
            }
            Xr.Trigger(Hand.Right, 0f);
            yield return Release(Hand.Left);
            yield return Xr.Wait(0.3f);
            Assert.IsTrue(s3.PracticeDone, "training fire out with PASS");
            yield return Xr.MoveHand(Hand.Right, eye + Vector3.down * 0.8f, toFire, 0.3f);
            yield return Release(Hand.Right); // put the extinguisher down
            yield return Visit(s3.transform.Find("Table").position);
            yield return Answer(s3, 'B');
            Assert.IsTrue(s3.IsComplete);

            // ---- Station 4: press the call point, answer B
            var s4 = St(3);
            var cp = s4.transform.Find("CallPoint");
            yield return StandAt(new Vector3(cp.position.x, 1.6f, cp.position.z - 0.6f), cp.position);
            Vector3 cpFace = cp.GetComponent<Collider>().ClosestPoint(cp.position + Vector3.back);
            yield return Poke(Hand.Right, cpFace, Vector3.forward, 0.015f);
            Shot("06_call_point");
            Assert.IsTrue(s4.PracticeDone, "call point pressed");
            yield return Visit(s4.transform.Find("Table").position);
            yield return Answer(s4, 'B');
            Assert.IsTrue(s4.IsComplete);

            // ---- Station 5: through the smoke corridor keeping the head under 1.2 m, answer B
            var s5 = St(4);
            Vector3 start = new Vector3(-4.5f, 1.0f, -0.4f);
            yield return StandAt(start, start + Vector3.back);
            Shot("07_smoke_corridor_crouched");
            for (float z = -0.4f; z >= -4.6f; z -= 0.02f)
            {
                Xr.LookAt(new Vector3(-4.5f, 1.0f, z), new Vector3(-4.5f, 1.0f, z - 1f));
                yield return null;
            }
            yield return Xr.Wait(0.2f);
            Assert.IsTrue(s5.PracticeDone, "walked through the smoke staying low");
            Transform t5 = s5.transform.Find("Table");
            yield return StandAt(new Vector3(t5.position.x - 0.75f, 1.6f, t5.position.z), t5.position + Vector3.up * 0.9f);
            var btnB = s5.transform.Find("Answer_B");
            yield return Poke(Hand.Right, btnB.GetComponent<Collider>().ClosestPoint(btnB.position + Vector3.up), Vector3.down, 0.015f);
            yield return Xr.Wait(0.3f);
            Assert.IsTrue(s5.IsComplete);

            yield return Xr.Wait(0.3f);
            Assert.IsTrue(_lesson.AllDone && ProgressionStore.LessonDone, "lesson finished and recorded");
            yield return StandAt(new Vector3(0f, 1.6f, -3.2f), new Vector3(0f, 1.5f, -5f));
            Shot("08_lesson_complete");
        }

        [UnityTest]
        public IEnumerator SmokeCorridor_StandingUp_DoesNotCount()
        {
            yield return LoadLesson();
            var s5 = St(4);
            for (float z = -0.4f; z >= -4.6f; z -= 0.02f)
            {
                Xr.LookAt(new Vector3(-4.5f, 1.65f, z), new Vector3(-4.5f, 1.65f, z - 1f));
                yield return null;
            }
            yield return Xr.Wait(0.2f);
            Shot("standing_in_smoke_corridor");
            Assert.IsFalse(s5.PracticeDone, "walking upright through the smoke does not pass");
        }

        [UnityTest]
        public IEnumerator Quiz_BeforePractice_IsRefused()
        {
            yield return LoadLesson();
            var s1 = St(0);
            yield return Visit(s1.transform.Find("Table").position);
            yield return Answer(s1, 'B');
            Assert.IsFalse(s1.QuizDone, "practice first, then the question");
        }
    }
}
