using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Player;
using EscapeFire.Scenario;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace EscapeFire.Tests.PlayMode
{
    /// <summary>
    /// Automated play-throughs of complete drills in the real lab scene, one per game branch.
    /// Each run records a timeline (phases, AI mentor lines, AI-added challenges, score, outcome) to
    /// Logs/Playthroughs/&lt;run&gt;.md so the adaptive behaviour can be reviewed after the fact.
    /// Long in-game timers are shortened through reflection; game logic is untouched.
    /// </summary>
    public class PlaythroughTests
    {
        private static readonly string OUT_DIR = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Playthroughs");

        // ------------------------------------------------------------------ branch 1: perfect drill (Guided)

        [UnityTest]
        public IEnumerator Run01_Guided_PerfectProcedure_Pass()
        {
            yield return Load(TrainingMode.Guided);
            using (var rec = new Recorder("Run01_Guided_PerfectProcedure_Pass"))
            {
                var c = LabProgressiveScenarioController.Instance;
                yield return Wait(rec, "Phase 0 peaceful practice", 1f);
                c.SetPhase(ScenarioPhase.Phase1_PowerFlickerWarning);
                yield return Wait(rec, "Phase 1 spark warning", 0.5f);
                c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                yield return Wait(rec, "Fire erupts", 0.5f);

                rec.Action("Press fire alarm"); Object.FindFirstObjectByType<FireAlarmButton>().PushAlarmButton();
                rec.Action("Cut main breaker (Aptomat)"); Object.FindFirstObjectByType<CircuitBreakerSwitch>().SetPowerState(false);
                var co2 = Extinguisher(ExtinguisherType.CO2);
                rec.Action("Pull CO2 safety pin"); co2.PullSafetyPin();
                rec.Action("Spray CO2 at the base of the fire"); co2.TrySpray(isElectricalPowerLive: false);
                c.ServerFire.Extinguish(1e6f); co2.StopSpray();
                yield return Wait(rec, "Fire out", 0.5f);

                var hoa = Npc(NPCRole.HoaFaint);
                hoa.TriggerEmergencyReaction();
                rec.Action("Rescue Hoa"); hoa.RescueNPC();
                rec.Action("Check door handle with back of hand"); Object.FindFirstObjectByType<DoorHeatChecker>().CheckDoorHeatWithHand();
                rec.Action("Walk into the hallway"); c.TriggerHallwayEvacuationWin();
                yield return Wait(rec, "Debrief", 0.5f);

                rec.Finish();
                Assert.IsTrue(SessionResult.Passed);
                Assert.AreEqual(100, AfterActionReport.Total(SessionResult.Scorecard), rec.Summary());
            }
        }

        // ------------------------------------------------------------------ branch 2: electrocution

        [UnityTest]
        public IEnumerator Run02_WaterOnLivePanel_ElectrocutionFail()
        {
            yield return Load(TrainingMode.Guided);
            using (var rec = new Recorder("Run02_WaterOnLivePanel_ElectrocutionFail"))
            {
                LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                yield return Wait(rec, "Fire erupts", 0.5f);
                var water = Extinguisher(ExtinguisherType.WaterFoam);
                rec.Action("Grab the WATER extinguisher, pull pin, spray with power still ON");
                water.PullSafetyPin();
                water.TrySpray(isElectricalPowerLive: true);
                yield return Wait(rec, "Debrief", 0.5f);
                rec.Finish();
                Assert.IsFalse(SessionResult.Passed);
                StringAssert.Contains("Điện giật", SessionResult.FailCause);
            }
        }

        // ------------------------------------------------------------------ branch 3: suffocation (standing in smoke)

        [UnityTest]
        public IEnumerator Run03_StandingInSmoke_SuffocationFail()
        {
            yield return Load(TrainingMode.Guided);
            using (var rec = new Recorder("Run03_StandingInSmoke_SuffocationFail"))
            {
                var c = LabProgressiveScenarioController.Instance;
                var head = PinHead(1.65f); // learner stands upright, never crouches
                var smoke = Object.FindFirstObjectByType<SmokeDensityController>();
                SetField(Object.FindFirstObjectByType<OxygenSystem>(), "depletionRateInSmoke", 22f);
                c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                yield return Wait(rec, "Fire erupts", 0.3f);
                rec.Action("Smoke layer drops to 1.2 m; learner keeps standing (head 1.65 m)");
                SetAutoProperty(smoke, "CurrentSmokeHeight", 1.2f);
                yield return Wait(rec, "Breathing smoke", 6f);
                rec.Finish();
                Object.Destroy(head);
                Assert.IsFalse(SessionResult.Passed, rec.Summary());
                StringAssert.Contains("Ngạt khói", SessionResult.FailCause);
                Assert.That(rec.MentorLines, Has.Some.Contains("khom người"), "Socratic smoke warning expected");
            }
        }

        // ------------------------------------------------------------------ branch 4: FM-200 auto discharge

        [UnityTest]
        public IEnumerator Run04_FireIgnored_FM200DischargeFail()
        {
            yield return Load(TrainingMode.Guided);
            using (var rec = new Recorder("Run04_FireIgnored_FM200DischargeFail"))
            {
                var fm200 = Object.FindFirstObjectByType<FM200System>();
                SetField(fm200, "autoTriggerAfterBurnSeconds", 1.5f); // real: 90 s
                SetField(fm200, "countdownSeconds", 2f);              // real: 30 s
                LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                rec.Action("Learner hesitates and never extinguishes or evacuates");
                yield return Wait(rec, "Fire burns, FM-200 countdown, discharge", 5f);
                rec.Finish();
                Assert.IsFalse(SessionResult.Passed, rec.Summary());
                StringAssert.Contains("FM-200", SessionResult.FailCause);
            }
        }

        // ------------------------------------------------------------------ branch 5: FM-200 countdown but evacuates in time

        [UnityTest]
        public IEnumerator Run05_FM200Countdown_EvacuatesInTime_Pass()
        {
            yield return Load(TrainingMode.Guided);
            using (var rec = new Recorder("Run05_FM200Countdown_EvacuatesInTime_Pass"))
            {
                var fm200 = Object.FindFirstObjectByType<FM200System>();
                SetField(fm200, "autoTriggerAfterBurnSeconds", 1f);
                SetField(fm200, "countdownSeconds", 3f);
                var c = LabProgressiveScenarioController.Instance;
                c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                yield return Wait(rec, "Fire burns until the FM-200 siren", 1.8f);
                rec.Action("Stops fighting the fire and runs to the hallway during the countdown");
                c.TriggerHallwayEvacuationWin();
                yield return Wait(rec, "Gas discharges behind the closed door", 3f);
                rec.Finish();
                Assert.IsTrue(SessionResult.Passed, rec.Summary());
            }
        }

        // ------------------------------------------------------------------ branch 6: Exam mode, calm learner -> local AI escalates

        [UnityTest]
        public IEnumerator Run06_Exam_CalmLearner_LocalAIEscalates()
        {
            yield return Load(TrainingMode.Exam);
            using (var rec = new Recorder("Run06_Exam_CalmLearner_LocalAIEscalates"))
            {
                var head = PinHead(1.0f); // still and crouched: low cognitive friction
                var governor = MicroAIGovernor.Instance;
                SetField(governor, "evaluationIntervalSeconds", 0.5f);
                SetField(governor, "escalationCooldownSeconds", 1.5f); // real: 45 s
                SetField(governor, "minObservationSeconds", 2f);       // real: 15 s
                SetField(Object.FindFirstObjectByType<FM200System>(), "countdownSeconds", 60f);
                LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                rec.Action("Learner stays calm and composed (CF low)");
                yield return Wait(rec, "AI Director observes and escalates", 9f);

                rec.Action("Tries the alarm button");
                Object.FindFirstObjectByType<FireAlarmButton>().PushAlarmButton();
                rec.Action("Pulls the CO2 pin twice");
                var co2 = Extinguisher(ExtinguisherType.CO2);
                co2.PullSafetyPin(); co2.PullSafetyPin();
                yield return Wait(rec, "Handling anomalies", 0.5f);
                rec.Action("Shouts into the walkie-talkie: 'Cháy! Có cháy!'");
                Object.FindFirstObjectByType<VoiceCommandRouter>().HandleTranscript("Cháy! Có cháy! Báo cháy tầng này!");
                yield return Wait(rec, "Alarm raised by voice", 0.5f);
                LabProgressiveScenarioController.Instance.TriggerHallwayEvacuationWin();
                yield return Wait(rec, "Debrief", 0.5f);
                rec.Finish();
                Object.Destroy(head);

                Assert.That(rec.AcceptedDirectives.Count, Is.GreaterThanOrEqualTo(3), rec.Summary());
                Assert.IsTrue(co2.IsPinPulled, "Jammed pin must come out after the second pull");
                Assert.IsTrue(Object.FindFirstObjectByType<FireAlarmButton>().IsActivated, "Voice alarm must work when the cable is cut");
            }
        }

        // ------------------------------------------------------------------ branch 7: Exam mode, panicking learner -> AI calms, never escalates

        [UnityTest]
        public IEnumerator Run07_Exam_PanickingLearner_AICalmsDown()
        {
            yield return Load(TrainingMode.Exam);
            using (var rec = new Recorder("Run07_Exam_PanickingLearner_AICalmsDown"))
            {
                var head = PinHead(1.65f);
                var governor = MicroAIGovernor.Instance;
                SetField(governor, "evaluationIntervalSeconds", 0.5f);
                SetField(governor, "escalationCooldownSeconds", 1f);
                rec.Action("Learner looks around frantically (head sweeps ±80° several times per second) and makes 4 safety mistakes");
                for (int i = 0; i < 4; i++) ScoringManager.Instance.DeductScore(5, "Lỗi an toàn (mô phỏng)");
                float t = 0f;
                bool fireStarted = false;
                while (t < 9f)
                {
                    head.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(t * 12f) * 80f, 0f);
                    if (!fireStarted && t > 2.2f)
                    {
                        fireStarted = true;
                        LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                        rec.Note("Fire erupts while the learner is already agitated; hesitation keeps growing");
                    }
                    t += Time.deltaTime;
                    yield return null;
                }
                rec.Finish();
                Object.Destroy(head);
                Assert.That(rec.MentorLines, Has.Some.Contains("Bình tĩnh"), rec.Summary());
                Assert.AreEqual(0, rec.AcceptedDirectives.Count, "A panicking learner must never get extra challenges:\n" + rec.Summary());
            }
        }

        // ------------------------------------------------------------------ branch 8: Exam mode with the LLM edge server

        [UnityTest]
        public IEnumerator Run08_Exam_EdgeLLMServer_Directives()
        {
            int port = 8800 + UnityEngine.Random.Range(0, 150);
            var server = StartEdgeServer(port);
            try
            {
                bool up = false;
                for (int i = 0; i < 40 && !up; i++)
                {
                    using (var req = UnityWebRequest.Get($"http://127.0.0.1:{port}/health"))
                    {
                        yield return req.SendWebRequest();
                        up = req.result == UnityWebRequest.Result.Success;
                    }
                    if (!up) yield return new WaitForSecondsRealtime(0.5f);
                }
                if (!up) Assert.Inconclusive("Edge server did not start (uv / Python missing?)");

                yield return Load(TrainingMode.Exam);
                using (var rec = new Recorder("Run08_Exam_EdgeLLMServer_Directives"))
                {
                    var head = PinHead(1.0f);
                    var client = Object.FindFirstObjectByType<EdgeDirectorClient>();
                    SetField(client, "telemetryIntervalSeconds", 1f); // real: 10 s
                    SetField(Object.FindFirstObjectByType<FM200System>(), "countdownSeconds", 60f);
                    client.Connect($"ws://127.0.0.1:{port}/session");
                    LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                    rec.Action("Headset streams telemetry to the laptop AI Director (scripted LLM)");
                    yield return Wait(rec, "LLM decides macro directives", 8f);
                    rec.Note($"Edge connected: {client.IsConnected}; local escalation paused: {!MicroAIGovernor.Instance.LocalEscalationEnabled}");
                    LabProgressiveScenarioController.Instance.TriggerHallwayEvacuationWin();
                    yield return Wait(rec, "Debrief", 0.5f);
                    rec.Finish();
                    Object.Destroy(head);
                    Assert.That(rec.EdgeDirectives, Is.Not.Empty, rec.Summary());

                    // The session report must reach the mock LMS on the edge server (Server/logs/lms/<id>.json).
                    string lmsFile = Path.Combine(Directory.GetCurrentDirectory(), "Server", "logs", "lms", GameSession.SessionId + ".json");
                    for (int i = 0; i < 20 && !File.Exists(lmsFile); i++) yield return new WaitForSecondsRealtime(0.25f);
                    Assert.IsTrue(File.Exists(lmsFile), $"LMS report not delivered to {lmsFile}");
                }
            }
            finally
            {
                KillTree(server);
            }
        }

        // ------------------------------------------------------------------ branch 9: Socratic coach (alarm reminder + smoke posture)

        [UnityTest]
        public IEnumerator Run09_SocraticCoach_Reminders()
        {
            yield return Load(TrainingMode.Guided);
            using (var rec = new Recorder("Run09_SocraticCoach_Reminders"))
            {
                var head = PinHead(1.65f);
                SetField(Object.FindFirstObjectByType<OxygenSystem>(), "depletionRateInSmoke", 0.5f);
                SetField(Object.FindFirstObjectByType<FM200System>(), "autoTriggerAfterBurnSeconds", 999f);
                LabProgressiveScenarioController.Instance.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
                SetAutoProperty(Object.FindFirstObjectByType<SmokeDensityController>(), "CurrentSmokeHeight", 1.3f);
                rec.Action("Learner stands in the smoke and forgets the alarm (time x10)");
                Time.timeScale = 10f;
                yield return new WaitForSeconds(50f); // 5 s real
                Time.timeScale = 1f;
                rec.Finish();
                Object.Destroy(head);
                Assert.That(rec.MentorLines, Has.Some.Contains("khom người"), rec.Summary());
                Assert.That(rec.MentorLines, Has.Some.Contains("chuông báo động"), rec.Summary());
            }
        }

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        // ================================================================== helpers

        private static IEnumerator Load(TrainingMode mode)
        {
            GameSession.Reset();
            GameSession.Begin(mode, "PLAYTEST");
            SessionResult.Reset();
            LmsSync.OutboxFolder = "lms_outbox_tests";
            ProgressionStore.KeyPrefix = "EF_test_progress_"; // never touch the learner's real progress
            TelemetryLogger.FolderOverride = "TelemetryReports_tests";
            yield return SceneManager.LoadSceneAsync(SceneNames.LAB, LoadSceneMode.Single);
            yield return null;
            var reporter = Object.FindFirstObjectByType<SessionOutcomeReporter>();
            if (reporter != null) SetField(reporter, "loadAARScene", false);
            var edge = Object.FindFirstObjectByType<EdgeDirectorClient>();
            if (edge != null) SetField(edge, "reconnectDelaySeconds", 999f); // no LAN server during tests
        }

        private static IEnumerator Wait(Recorder rec, string label, float seconds)
        {
            rec.Note($"… {label} ({seconds:F1}s)");
            yield return new WaitForSeconds(seconds);
        }

        /// <summary>Replaces the tracked HMD with a fixed head so posture / CF are deterministic in batchmode.</summary>
        private static GameObject PinHead(float height)
        {
            var head = new GameObject("TestHead");
            head.transform.position = new Vector3(0f, height, -3f);
            foreach (var tracker in Object.FindObjectsByType<CognitiveFrictionTracker>(FindObjectsSortMode.None)) SetField(tracker, "headTransform", head.transform);
            foreach (var detector in Object.FindObjectsByType<VRHeightDetector>(FindObjectsSortMode.None))
            {
                SetField(detector, "headTransform", head.transform);
                detector.UpdateHeight(height);
            }
            foreach (var collector in Object.FindObjectsByType<SessionFactsCollector>(FindObjectsSortMode.None)) SetField(collector, "head", head.transform);
            foreach (var coach in Object.FindObjectsByType<DeterministicVoiceTriggerManager>(FindObjectsSortMode.None)) SetField(coach, "head", head.transform);
            head.AddComponent<HeadHeightPinner>().Init(height);
            return head;
        }

        private sealed class HeadHeightPinner : MonoBehaviour
        {
            private float _height;
            private VRHeightDetector[] _detectors;
            public void Init(float h) { _height = h; _detectors = FindObjectsByType<VRHeightDetector>(FindObjectsSortMode.None); }
            private void LateUpdate() { foreach (var d in _detectors) if (d != null) d.UpdateHeight(_height); }
        }

        private static FireExtinguisher Extinguisher(ExtinguisherType type)
        {
            foreach (var e in Object.FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None)) if (e.Type == type) return e;
            Assert.Fail($"No {type} extinguisher in the lab");
            return null;
        }

        private static NPCSimulator Npc(NPCRole role)
        {
            foreach (var n in Object.FindObjectsByType<NPCSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (n.Role == role) return n;
            Assert.Fail($"No NPC with role {role}");
            return null;
        }

        private static void SetField(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            f.SetValue(target, value);
        }

        private static void SetAutoProperty(object target, string property, object value)
        {
            SetField(target, $"<{property}>k__BackingField", value);
        }

        private static Process StartEdgeServer(int port)
        {
            string serverDir = Path.Combine(Directory.GetCurrentDirectory(), "Server");
            var psi = new ProcessStartInfo("uv", $"run python -m escapefire_edge.app --fake-llm --min-observation 2 --host 127.0.0.1 --port {port}")
            {
                WorkingDirectory = serverDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            var p = Process.Start(psi);
            p.OutputDataReceived += (_, __) => { };
            p.ErrorDataReceived += (_, __) => { };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            return p;
        }

        /// <summary>uv starts python as a child process: kill the whole tree so no server keeps the port.</summary>
        private static void KillTree(Process p)
        {
            try
            {
                if (p.HasExited) return;
                using (var kill = Process.Start(new ProcessStartInfo("taskkill", $"/PID {p.Id} /T /F") { CreateNoWindow = true, UseShellExecute = false }))
                {
                    kill?.WaitForExit(5000);
                }
            }
            catch (Exception) { }
        }

        // ================================================================== recorder

        private sealed class Recorder : IDisposable
        {
            private readonly string _name;
            private readonly float _start = Time.time;
            private readonly StringBuilder _timeline = new StringBuilder();
            public readonly List<string> MentorLines = new List<string>();
            public readonly List<string> AcceptedDirectives = new List<string>();
            public readonly List<string> EdgeDirectives = new List<string>();
            public readonly List<string> RejectedDirectives = new List<string>();

            public Recorder(string name)
            {
                _name = name;
                EventBus<PhaseChangedEvent>.Subscribe(OnPhase);
                EventBus<MentorMessageEvent>.Subscribe(OnMentor);
                EventBus<DirectiveResolvedEvent>.Subscribe(OnDirective);
                EventBus<ScoreChangedEvent>.Subscribe(OnScore);
                EventBus<CognitiveStateChangedEvent>.Subscribe(OnCf);
                EventBus<Fm200CountdownEvent>.Subscribe(OnFm200);
                EventBus<VoiceCommandEvent>.Subscribe(OnVoice);
                Line($"Mode: **{GameSession.Mode}**");
            }

            public void Dispose()
            {
                EventBus<PhaseChangedEvent>.Unsubscribe(OnPhase);
                EventBus<MentorMessageEvent>.Unsubscribe(OnMentor);
                EventBus<DirectiveResolvedEvent>.Unsubscribe(OnDirective);
                EventBus<ScoreChangedEvent>.Unsubscribe(OnScore);
                EventBus<CognitiveStateChangedEvent>.Unsubscribe(OnCf);
                EventBus<Fm200CountdownEvent>.Unsubscribe(OnFm200);
                EventBus<VoiceCommandEvent>.Unsubscribe(OnVoice);
            }

            private float T => Time.time - _start;
            private void Line(string s) => _timeline.AppendLine($"| {T,6:F1}s | {s} |");
            public void Action(string s) => Line($"🎮 **Người chơi:** {s}");
            public void Note(string s) => Line($"_{s}_");
            private void OnPhase(PhaseChangedEvent e) => Line($"📍 Phase → `{e.Phase}`");
            private void OnMentor(MentorMessageEvent e) { MentorLines.Add(e.Message); Line($"🗣️ **AI Mentor:** {e.Message}"); }
            private void OnScore(ScoreChangedEvent e) => Line($"{(e.Delta >= 0 ? "➕" : "➖")} {e.Delta:+#;-#;0} điểm — {e.Reason} (tổng {e.Total})");
            private void OnCf(CognitiveStateChangedEvent e) => Line($"🧠 CF = {e.CognitiveFriction:F2} → trạng thái `{e.State}`");
            private void OnVoice(VoiceCommandEvent e) => Line($"🎙️ Khẩu lệnh \"{e.Transcript}\" → `{e.Command}`");
            private void OnFm200(Fm200CountdownEvent e)
            {
                if (e.Stage != Fm200Stage.Tick) Line($"🚨 FM-200 `{e.Stage}`");
            }

            private void OnDirective(DirectiveResolvedEvent e)
            {
                string code = DirectiveCodes.ToCode(e.Directive);
                if (e.Result == DirectiveResult.Accepted)
                {
                    AcceptedDirectives.Add($"{code} ({e.Source})");
                    if (e.Source == "edge") EdgeDirectives.Add(code);
                    Line($"🤖 **AI thêm thử thách:** `{code}` (nguồn: {e.Source})");
                }
                else
                {
                    RejectedDirectives.Add($"{code}: {e.Result}");
                    Line($"🛡️ Validator chặn `{code}` → {e.Result} (nguồn: {e.Source})");
                }
            }

            public string Summary()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# {_name}");
                sb.AppendLine();
                sb.AppendLine($"- Kết quả: **{(SessionResult.HasResult ? (SessionResult.Passed ? "PASS" : "FAIL — " + SessionResult.FailCause) : "chưa kết thúc")}**");
                if (SessionResult.Scorecard.Count > 0)
                    sb.AppendLine($"- Điểm PCCC: **{AfterActionReport.Total(SessionResult.Scorecard)}/100** — {GradeRules.Classify(SessionResult.Passed, AfterActionReport.Total(SessionResult.Scorecard))}");
                sb.AppendLine($"- AI thêm thử thách: {(AcceptedDirectives.Count == 0 ? "không có" : string.Join(", ", AcceptedDirectives))}");
                sb.AppendLine($"- Validator chặn: {(RejectedDirectives.Count == 0 ? "không có" : string.Join(", ", RejectedDirectives))}");
                sb.AppendLine($"- Số câu AI Mentor: {MentorLines.Count}");
                if (SessionResult.Scorecard.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("| Tiêu chí | Điểm | Ghi chú |");
                    sb.AppendLine("| :--- | :-: | :--- |");
                    foreach (var it in SessionResult.Scorecard) sb.AppendLine($"| {it.Label} | {it.Points}/{it.MaxPoints} | {it.Note} |");
                }
                if (!string.IsNullOrEmpty(SessionResult.Debrief))
                {
                    sb.AppendLine();
                    sb.AppendLine($"**Nhận xét AAR:** {SessionResult.Debrief}");
                }
                sb.AppendLine();
                sb.AppendLine("| Thời điểm | Diễn biến |");
                sb.AppendLine("| ---: | :--- |");
                sb.Append(_timeline);
                return sb.ToString();
            }

            public void Finish()
            {
                Directory.CreateDirectory(OUT_DIR);
                File.WriteAllText(Path.Combine(OUT_DIR, _name + ".md"), Summary(), new UTF8Encoding(false));
                Debug.Log($"[Playthrough] {_name}: {(SessionResult.Passed ? "PASS" : "FAIL")} | AI added: {string.Join(", ", AcceptedDirectives)}");
            }
        }
    }
}
