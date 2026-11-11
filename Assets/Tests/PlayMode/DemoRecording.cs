#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Player;
using EscapeFire.Scenario;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace EscapeFire.Tests.PlayMode
{
    /// <summary>
    /// Records a demo video + screenshots of one complete Exam drill against the REAL edge AI Director
    /// (Ollama/Gemma on this laptop, server started separately on 127.0.0.1:8765).
    /// A first-person "autopilot" walks the learner through the drill at walking speed and reacts to whatever
    /// challenges the LLM really adds; the AAR scene with the LLM debrief is recorded at the end.
    /// Output: Logs/Demo/&lt;timestamp&gt;/demo.mp4, shots/*.png, timeline.md.
    /// Explicit: never runs with the normal suite. Run:
    ///   Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter DemoRecording
    /// </summary>
    [Explicit("Needs the real edge server + Ollama; records video. Run on demand.")]
    [Category("Demo")]
    public class DemoRecording
    {
        private const int W = 1280, H = 720, FPS = 30;
        private const string EDGE_URL = "ws://127.0.0.1:8765/session";
        private const float WALK_SPEED = 1.3f;
        private const float FRONT_ROW_Z = -5.6f;  // free strip between the desks and the exit wall
        private const float EAST_AISLE_X = 7.0f;  // free aisle between the east desk column and the wall

        private string _dir;
        private Camera _cam;
        private RenderTexture _rt;
        private Texture2D _tex;
        private MediaEncoder _encoder;
        private float _recStart;
        private long _frames;
        private Transform _head;
        private float _eye = 1.62f;
        private float _yaw, _pitch;

        private TextMeshProUGUI _title, _status, _feed, _subtitle;
        private GameObject _statusBox, _feedBox;
        private readonly List<string> _feedLines = new List<string>();
        private readonly StringBuilder _timeline = new StringBuilder();
        private float _t0;
        private string _action = "";
        private string _mentor = "";
        private float _mentorUntil;
        private string _pendingShot;
        private float _pendingShotAt;
        private int _shotIndex;
        private float _cf;
        private CognitiveState _cfState = CognitiveState.Nominal;
        private readonly List<string> _accepted = new List<string>();
        private readonly List<string> _rejected = new List<string>();
        private readonly HashSet<Directive> _active = new HashSet<Directive>();
        private float _fireStart = -1f;

        private static readonly Regex EDGE_LOG = new Regex(@"\[EdgeDirector\] (\S+) \((.*), (\d+)ms\) -> (\w+)");

        /// <summary>Quick visual probe of the fire / smoke rendering from the demo camera (no LLM needed).</summary>
        private static void RequireGpu()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Demo recording needs a GPU (run Unity batchmode without -nographics).");
        }

        /// <summary>The demo camera / overlay survive scene loads; remove them so later tests start clean.</summary>
        [TearDown]
        public void Cleanup()
        {
            foreach (var name in new[] { "_DemoCamera", "_DemoOverlay", "_ProbeCamera", "DemoHead" })
            {
                var go = GameObject.Find(name);
                if (go != null) Object.Destroy(go);
            }
            if (_rt != null) { _rt.Release(); Object.Destroy(_rt); _rt = null; }
            if (_tex != null) { Object.Destroy(_tex); _tex = null; }
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator DemoProbe_FireAndSmoke()
        {
            RequireGpu();
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Guided, "PROBE");
            SessionResult.Reset();
            yield return SceneManager.LoadSceneAsync(SceneNames.LAB, LoadSceneMode.Single);
            yield return null;
            var c = LabProgressiveScenarioController.Instance;
            c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            var smoke = Object.FindFirstObjectByType<SmokeDensityController>();
            yield return new WaitForSeconds(4f);
            if (smoke != null) typeof(SmokeDensityController).GetField("<CurrentSmokeHeight>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(smoke, 1.7f);
            yield return new WaitForSeconds(1f);

            var sb = new StringBuilder();
            var fire = c.ServerFire;
            sb.AppendLine($"fire '{fire.name}' active={fire.gameObject.activeInHierarchy} pos={fire.transform.position} scale={fire.transform.lossyScale} intensity={fire.FireIntensity} suppressed={fire.IsSuppressed}");
            foreach (var ps in fire.GetComponentsInChildren<ParticleSystem>(true))
            {
                var r = ps.GetComponent<ParticleSystemRenderer>();
                sb.AppendLine($"  ps '{ps.name}' active={ps.gameObject.activeInHierarchy} playing={ps.isPlaying} count={ps.particleCount} renderer={(r != null && r.enabled)} mat={(r != null && r.sharedMaterial != null ? r.sharedMaterial.name + " / " + r.sharedMaterial.shader.name + " supported=" + r.sharedMaterial.shader.isSupported : "none")} layer={LayerMask.LayerToName(ps.gameObject.layer)}");
            }
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (mr.name.Contains("Smoke")) sb.AppendLine($"smoke '{mr.name}' enabled={mr.enabled} pos={mr.transform.position} scale={mr.transform.lossyScale} bounds={mr.bounds} mat={(mr.sharedMaterial != null ? mr.sharedMaterial.shader.name : "none")}");
            sb.AppendLine($"smoke height={(smoke != null ? smoke.CurrentSmokeHeight : -1f)}");

            _dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Demo", "probe");
            Directory.CreateDirectory(Path.Combine(_dir, "shots"));
            var camGo = new GameObject("_ProbeCamera");
            _cam = camGo.AddComponent<Camera>();
            _cam.fieldOfView = 68f; _cam.nearClipPlane = 0.05f; _cam.enabled = false;
            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            _cam.targetTexture = _rt;
            EnableUrpPost(_cam);
            _tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            Vector3 fp = fire.transform.position;
            var views = new[] { (new Vector3(fp.x + 0.6f, 1.1f, fp.z - 2.4f), fp + Vector3.up * 0.6f), (new Vector3(0f, 1.6f, -5.5f), fp), (new Vector3(2f, 1.0f, -1f), new Vector3(2f, 2.2f, 3f)) };
            for (int i = 0; i < views.Length; i++)
            {
                _cam.transform.SetPositionAndRotation(views[i].Item1, Quaternion.LookRotation(views[i].Item2 - views[i].Item1));
                yield return null;
                _cam.Render();
                RenderTexture.active = _rt;
                _tex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false);
                _tex.Apply(false);
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(_dir, "shots", $"probe_{i}.png"), _tex.EncodeToPNG());
            }
            File.WriteAllText(Path.Combine(_dir, "probe.txt"), sb.ToString());
            Debug.Log("[DemoProbe]\n" + sb);
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator DemoRecording_ExamDrill_RealLLM()
        {
            RequireGpu();
            yield return CheckServer();
            yield return Load();
            SetupRig();
            Application.logMessageReceived += OnLog;
            EventBus<MentorMessageEvent>.Subscribe(OnMentor);
            EventBus<DirectiveResolvedEvent>.Subscribe(OnDirective);
            EventBus<CognitiveStateChangedEvent>.Subscribe(OnCf);
            EventBus<PhaseChangedEvent>.Subscribe(OnPhase);
            try
            {
                yield return Drill();
                yield return Aar();
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
                EventBus<MentorMessageEvent>.Unsubscribe(OnMentor);
                EventBus<DirectiveResolvedEvent>.Unsubscribe(OnDirective);
                EventBus<CognitiveStateChangedEvent>.Unsubscribe(OnCf);
                EventBus<PhaseChangedEvent>.Unsubscribe(OnPhase);
                _encoder?.Dispose();
                _encoder = null;
                WriteTimeline();
                Debug.Log($"[Demo] Video + shots written to {_dir}");
            }
        }

        // ================================================================== the drill (autopilot)

        private IEnumerator Drill()
        {
            var c = LabProgressiveScenarioController.Instance;
            var alarm = Object.FindFirstObjectByType<FireAlarmButton>();
            var breaker = Object.FindFirstObjectByType<CircuitBreakerSwitch>();
            var door = Object.FindFirstObjectByType<DoorHeatChecker>();
            var router = Object.FindFirstObjectByType<VoiceCommandRouter>();
            var co2 = Extinguisher(ExtinguisherType.CO2);
            var hoa = Npc(NPCRole.HoaFaint);
            var fire = c.ServerFire;
            Vector3 fireLook = new Vector3(6f, 1.0f, 4.3f);
            Vector3 breakerPos = breaker.transform.position;
            Vector3 alarmPos = alarm.transform.position;
            Vector3 co2Pos = co2.transform.position;

            // --- intro: peaceful lab
            Place(new Vector3(-2.2f, _eye, FRONT_ROW_Z), new Vector3(3f, 1.1f, 3f));
            Act("Buổi sát hạch PCCC bắt đầu: người học đứng trong phòng Lab máy tính");
            yield return Hold(2.5f, new Vector3(3f, 1.1f, 3f));
            Shot("intro_lab");
            yield return Hold(3f, new Vector3(6f, 1.2f, 4.5f));

            // --- phase 1: electrical warning
            c.SetPhase(ScenarioPhase.Phase1_PowerFlickerWarning);
            Act("Cảnh báo: chập điện, đèn nhấp nháy");
            yield return Hold(4f, breakerPos);
            Shot("phase1_spark_warning");

            // --- phase 2: fire
            c.SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            Act("Tủ server bốc cháy!");
            yield return Hold(4f, fireLook);
            Shot("phase2_server_fire");

            Act("Giữ bình tĩnh, quan sát đám cháy và lối thoát");
            yield return Hold(3f, new Vector3(0f, 1.2f, -6.9f));
            Act("Đi tới nút báo cháy cạnh cửa");
            yield return Walk(fireLook, new Vector2(0.9f, FRONT_ROW_Z), new Vector2(1.35f, -6.05f));
            yield return Hold(1f, alarmPos);

            // let the AI Director observe the calm learner for the mandated 15 s before acting on its anomalies
            Act("Đánh giá tình huống trước khi hành động");
            while (Time.time - _fireStart < 19f) yield return Hold(1f, Time.time - _fireStart < 16f ? fireLook : alarmPos);

            if (!Busy(alarm, co2, fire))
            {
                Act("Nhấn nút báo cháy");
                alarm.PushAlarmButton();
                yield return Hold(1.8f, alarmPos);
                Shot("alarm_button");
                if (!alarm.IsActivated)
                {
                    Act("Chuông không kêu! Hô vào bộ đàm: \"Cháy! Có cháy!\"");
                    router.HandleTranscript("Cháy! Có cháy! Báo cháy phòng Lab!");
                    yield return Hold(2.5f, alarmPos);
                    Shot("voice_alarm");
                }
            }

            // --- take the CO2 extinguisher (correct agent for electrical fires)
            Act("Lấy bình CO2 cạnh cửa (đúng loại cho cháy điện)");
            yield return Walk(co2Pos, new Vector2(-1.4f, -6.05f));
            yield return Hold(1.2f, co2Pos);
            Carry(co2);
            Act("Rút chốt an toàn");
            co2.PullSafetyPin();
            yield return Hold(1.8f, null);
            if (!co2.IsPinPulled)
            {
                Shot("co2_pin_jammed");
                Act("Chốt bị kẹt: giữ chắc thân bình, xoay nhẹ và giật mạnh lần nữa");
                yield return Hold(1.5f, null);
                co2.PullSafetyPin();
                yield return Hold(1.5f, null);
            }

            // --- cut the power before fighting an electrical fire
            if (!_active.Contains(Directive.EscalateFm200Countdown))
            {
                Act("Đi tới tủ điện, ngắt Aptomat trước khi dập lửa");
                yield return Walk(breakerPos, new Vector2(1.5f, FRONT_ROW_Z), new Vector2(EAST_AISLE_X - 0.2f, FRONT_ROW_Z),
                    new Vector2(EAST_AISLE_X, -3.2f), new Vector2(EAST_AISLE_X + 0.4f, 1.3f));
                yield return Hold(1f, breakerPos);
                breaker.SetPowerState(false);
                yield return Hold(1.8f, breakerPos);
                Shot("breaker_off");
            }

            // --- fight the fire
            if (!_active.Contains(Directive.EscalateFm200Countdown) && !fire.IsExtinguished)
            {
                Act("Tiến lại gần, khom người, xịt CO2 vào gốc lửa");
                _eye = 1.05f;
                yield return Walk(new Vector3(6f, 0.6f, 4.5f), new Vector2(6.7f, 2.4f));
                yield return Spray(co2, fire, new Vector3(6f, 0.6f, 4.5f), 45f, "spraying_server_fire");
                if (fire.IsExtinguished)
                {
                    Act("Đám cháy tủ server đã tắt");
                    yield return Hold(2.5f, fireLook);
                    Shot("fire_out");
                }
                else if (co2.IsEmpty)
                {
                    Act("Bình CO2 hết áp, lửa vẫn cháy: quyết định ngừng dập, ưu tiên sơ tán");
                    yield return Hold(2.5f, fireLook);
                }
            }

            // --- secondary fire (only if the AI started one)
            var secondary = BurningSecondaryFire();
            if (secondary != null && !_active.Contains(Directive.EscalateFm200Countdown))
            {
                Vector3 sp = secondary.transform.position;
                if (!co2.IsEmpty)
                {
                    Act("Lửa lan sang bàn khác: đánh giá lại và dập tiếp bằng CO2");
                    yield return Walk(sp, new Vector2(EAST_AISLE_X, 1.9f), new Vector2(sp.x + (sp.x > 0 ? 1.4f : 1.6f), sp.z - 1.4f));
                    yield return Spray(co2, secondary, sp, 20f, "spraying_secondary_fire");
                }
                else
                {
                    Act("Lửa lan nhưng bình đã hết: không liều, sơ tán ngay");
                    yield return Hold(2f, sp);
                }
            }
            co2.StopSpray();

            if (_active.Contains(Directive.EscalateFm200Countdown))
            {
                Act("Còi FM-200 hú! Ngừng dập lửa, sơ tán trong 30 giây");
                yield return Hold(1.5f, null);
            }

            // --- rescue Hoa
            float waitHoa = 0f;
            while (!hoa.IsFainted && waitHoa < 6f) { waitHoa += Time.deltaTime; yield return Hold(0f, null); }
            if (hoa.IsFainted && !hoa.IsRescued)
            {
                Act("Bạn Hoa ngất vì khói: tới đỡ Hoa dậy");
                Vector3 hp = hoa.transform.position;
                yield return GoToEastAisle(hp.z, new Vector3(hp.x, 0.3f, hp.z));
                yield return Hold(1f, new Vector3(hp.x, 0.3f, hp.z));
                hoa.RescueNPC();
                Act("Đã cứu Hoa, Hoa tự đi ra lối thoát");
                yield return Hold(2.5f, new Vector3(hp.x, 0.8f, hp.z));
                Shot("hoa_rescued");
            }

            // --- door check and evacuation
            Act("Khom thấp, di chuyển tới cửa thoát hiểm");
            _eye = 1.05f;
            yield return GoToFrontRow(new Vector3(0f, 1.0f, -6.9f));
            yield return Walk(new Vector3(0f, 1.0f, -6.9f), new Vector2(0.2f, -6.0f));
            Act("Kiểm tra tay nắm cửa bằng mu bàn tay trước khi mở");
            door.CheckDoorHeatWithHand();
            yield return Hold(2.2f, new Vector3(0f, 1.0f, -6.9f));
            Shot("door_check");
            Uncarry(co2);
            Act("Cửa không nóng: mở cửa, sơ tán ra hành lang");
            yield return Walk(new Vector3(0f, 1.0f, -13f), new Vector2(0f, -7.6f), new Vector2(0f, -9.2f));
            c.TriggerHallwayEvacuationWin();
            yield return Hold(2.5f, new Vector3(0f, 1.0f, -13f));
            Shot("hallway_safe");
        }

        /// <summary>True when a challenge already forces the learner to skip the alarm step (nothing today).</summary>
        private bool Busy(FireAlarmButton alarm, FireExtinguisher co2, FireHazard fire) => alarm.IsActivated;

        private IEnumerator Spray(FireExtinguisher ext, FireHazard target, Vector3 aim, float maxSeconds, string shot)
        {
            float t = 0f;
            bool shotTaken = false;
            while (!target.IsExtinguished && t < maxSeconds && !ext.IsEmpty && !_active.Contains(Directive.EscalateFm200Countdown))
            {
                if (!ext.IsSpraying) ext.TrySpray(isElectricalPowerLive: false);
                if (ext.IsSpraying) target.Extinguish(ext.ExtinguishPowerPerSecond * Time.deltaTime);
                t += Time.deltaTime;
                if (!shotTaken && t > 1.5f) { Shot(shot); shotTaken = true; }
                yield return Hold(0f, aim);
            }
            ext.StopSpray();
        }

        private IEnumerator GoToEastAisle(float z, Vector3 look)
        {
            Vector3 p = _cam.transform.position;
            if (p.x < EAST_AISLE_X - 1f)
                yield return Walk(look, new Vector2(p.x, FRONT_ROW_Z), new Vector2(EAST_AISLE_X, FRONT_ROW_Z), new Vector2(EAST_AISLE_X, z));
            else
                yield return Walk(look, new Vector2(EAST_AISLE_X, p.z), new Vector2(EAST_AISLE_X, z));
        }

        private IEnumerator GoToFrontRow(Vector3 look)
        {
            Vector3 p = _cam.transform.position;
            if (p.z > FRONT_ROW_Z + 0.5f)
            {
                float x = p.x > 3f ? EAST_AISLE_X : p.x;
                yield return Walk(look, new Vector2(x, p.z), new Vector2(x, FRONT_ROW_Z));
            }
        }

        private SecondaryFireSlot BurningSecondaryFire()
        {
            foreach (var s in Object.FindObjectsByType<SecondaryFireSlot>(FindObjectsSortMode.None)) if (s.IsBurning) return s;
            return null;
        }

        private IEnumerator Spray(FireExtinguisher ext, SecondaryFireSlot slot, Vector3 aim, float maxSeconds, string shot)
        {
            var fire = slot.GetComponentInChildren<FireHazard>();
            if (fire == null) yield break;
            yield return Spray(ext, fire, aim, maxSeconds, shot);
        }

        // ================================================================== AAR

        private IEnumerator Aar()
        {
            Act("Kết thúc diễn tập → bảng đánh giá AAR");
            float wait = 0f;
            while (SceneManager.GetActiveScene().name != SceneNames.AAR && wait < 15f) { wait += Time.unscaledDeltaTime; yield return Hold(0f, null); }
            yield return null;

            Canvas best = null;
            float bestArea = 0f;
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (cv.renderMode != RenderMode.WorldSpace || !cv.isRootCanvas) continue;
                var r = (RectTransform)cv.transform;
                float area = r.rect.width * r.lossyScale.x * r.rect.height * r.lossyScale.y;
                if (area > bestArea) { bestArea = area; best = cv; }
            }
            if (best != null)
            {
                var r = (RectTransform)best.transform;
                float hgt = r.rect.height * r.lossyScale.y, wid = r.rect.width * r.lossyScale.x;
                float dist = Mathf.Max(hgt / 2f / Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad),
                                       wid / 2f / Mathf.Tan(Camera.VerticalToHorizontalFieldOfView(_cam.fieldOfView, _cam.aspect) * 0.5f * Mathf.Deg2Rad)) * 1.08f;
                Vector3 center = r.TransformPoint(r.rect.center);
                _cam.transform.SetPositionAndRotation(center - best.transform.forward * dist, Quaternion.LookRotation(best.transform.forward, best.transform.up));
            }
            else Note("AAR: no world-space canvas found");

            _statusBox.SetActive(false);
            _feedBox.SetActive(false);
            Act("Bảng điểm 100 đ theo 6 tiêu chí; nhận xét do Gemma (chạy local) viết");
            string template = SessionResult.Debrief;
            float t = 0f;
            bool llm = false;
            while (t < 40f)
            {
                if (SessionResult.Debrief != template) { llm = true; break; }
                t += Time.unscaledDeltaTime;
                yield return Frame();
            }
            Note(llm ? $"AAR debrief from the LLM after {t:F1}s: {SessionResult.Debrief}" : "AAR debrief: LLM did not answer, template kept");
            _action = llm ? "Nhận xét AI Mentor (Gemma) đã hiển thị" : "Nhận xét mẫu (LLM không phản hồi)";
            for (float h = 0f; h < 2f; h += Time.unscaledDeltaTime) yield return Frame();
            Shot("aar_scorecard");
            _action = "";
            _subtitle.transform.parent.gameObject.SetActive(false);
            for (float h = 0f; h < 10f; h += Time.unscaledDeltaTime) yield return Frame();
            Shot("aar_clean");
            yield return Frame();
        }

        // ================================================================== camera + capture

        private void Place(Vector3 pos, Vector3 look)
        {
            Vector3 d = (look - pos).normalized;
            _yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            _pitch = -Mathf.Asin(d.y) * Mathf.Rad2Deg;
            _cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(_pitch, _yaw, 0f));
        }

        private void LookToward(Vector3 target, float rate)
        {
            Vector3 d = target - _cam.transform.position;
            if (d.sqrMagnitude < 0.01f) return;
            d.Normalize();
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Clamp(-Mathf.Asin(d.y) * Mathf.Rad2Deg, -45f, 60f);
            float k = 1f - Mathf.Exp(-rate * Time.deltaTime);
            _yaw = Mathf.LerpAngle(_yaw, yaw, k);
            _pitch = Mathf.Lerp(_pitch, pitch, k);
        }

        private void Settle()
        {
            Vector3 p = _cam.transform.position;
            p.y = Mathf.Lerp(p.y, _eye, 1f - Mathf.Exp(-3f * Time.deltaTime));
            _cam.transform.SetPositionAndRotation(p, Quaternion.Euler(_pitch, _yaw, 0f));
            _head.position = p;
            foreach (var d in Object.FindObjectsByType<VRHeightDetector>(FindObjectsSortMode.None)) d.UpdateHeight(p.y);
        }

        private IEnumerator Hold(float seconds, Vector3? look)
        {
            float t = 0f;
            do
            {
                if (look.HasValue) LookToward(look.Value, 2.2f);
                Settle();
                t += Time.deltaTime;
                yield return Frame();
            } while (t < seconds);
        }

        private IEnumerator Walk(Vector3 finalLook, params Vector2[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 target = new Vector3(points[i].x, 0f, points[i].y);
                bool last = i == points.Length - 1;
                while (true)
                {
                    Vector3 p = _cam.transform.position;
                    Vector3 flat = new Vector3(target.x - p.x, 0f, target.z - p.z);
                    float dist = flat.magnitude;
                    if (dist < 0.05f) break;
                    Vector3 step = flat / dist * Mathf.Min(dist, WALK_SPEED * Time.deltaTime);
                    _cam.transform.position = p + step;
                    Vector3 look = last && dist < 2.2f ? finalLook : new Vector3(target.x, _eye - 0.15f, target.z) + flat.normalized * 2f;
                    LookToward(look, last && dist < 2.2f ? 2.5f : 3.5f);
                    Settle();
                    yield return Frame();
                }
            }
        }

        private IEnumerator Frame()
        {
            yield return null;
            Capture();
        }

        private void Capture()
        {
            if (_encoder == null) return;
            UpdateOverlay();
            _cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            _tex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false);
            _tex.Apply(false);
            RenderTexture.active = prev;

            long due = (long)((Time.unscaledTime - _recStart) * FPS) + 1;
            int added = 0;
            while (_frames < due && added < FPS) { _encoder.AddFrame(_tex); _frames++; added++; }
            if (_frames < due) _frames = due; // long hitch (scene load): drop it instead of a frozen second

            if (_pendingShot != null && Time.time >= _pendingShotAt)
            {
                string name = $"{++_shotIndex:00}_{_pendingShot}.png";
                File.WriteAllBytes(Path.Combine(_dir, "shots", name), _tex.EncodeToPNG());
                Note($"screenshot {name}");
                _pendingShot = null;
            }
        }

        private void Shot(string name, float delay = 0f)
        {
            if (_pendingShot != null) // flush the previous one right away
            {
                string prevName = $"{++_shotIndex:00}_{_pendingShot}.png";
                File.WriteAllBytes(Path.Combine(_dir, "shots", prevName), _tex.EncodeToPNG());
            }
            _pendingShot = name;
            _pendingShotAt = Time.time + delay;
        }

        private void Carry(FireExtinguisher ext)
        {
            foreach (var rb in ext.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
            foreach (var col in ext.GetComponentsInChildren<Collider>()) col.enabled = false;
            ext.transform.SetParent(_cam.transform, true);
            ext.transform.localPosition = new Vector3(0.34f, -0.5f, 0.78f);
            ext.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
        }

        private void Uncarry(FireExtinguisher ext)
        {
            ext.transform.SetParent(null, true);
            ext.gameObject.SetActive(false); // left behind in the lab
        }

        // ================================================================== setup

        private IEnumerator CheckServer()
        {
            using (var req = UnityWebRequest.Get("http://127.0.0.1:8765/health"))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success || !req.downloadHandler.text.Contains("OllamaClient"))
                    Assert.Inconclusive("Start the real edge server first: cd Server && uv run python -m escapefire_edge.app --llm-timeout 25 --debrief-timeout 45");
            }
        }

        private IEnumerator Load()
        {
            GameSession.Reset();
            GameSession.Begin(TrainingMode.Exam, "DEMO");
            SessionResult.Reset();
            LmsSync.OutboxFolder = "lms_outbox_demo";
            TelemetryLogger.FolderOverride = "TelemetryReports_demo";
            SceneManager.sceneLoaded += OnSceneLoaded;
            yield return SceneManager.LoadSceneAsync(SceneNames.LAB, LoadSceneMode.Single);
            yield return null;

            var edge = Object.FindFirstObjectByType<EdgeDirectorClient>();
            SetField(edge, "telemetryIntervalSeconds", 5f); // real: 10 s, halved so the 3-minute video shows more decisions
            SetField(edge, "reconnectDelaySeconds", 2f);
            edge.Connect(EDGE_URL);
            for (int i = 0; i < 40 && !edge.IsConnected; i++) yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsTrue(edge.IsConnected, "Edge server not reachable at " + EDGE_URL);

            // Keep the XR rig camera from rendering in parallel (the demo camera is the only view).
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) cam.enabled = false;

            // Logic "head": CF / posture / trajectory follow the demo camera position, with a steady gaze (calm learner).
            _head = new GameObject("DemoHead").transform;
            foreach (var tracker in Object.FindObjectsByType<CognitiveFrictionTracker>(FindObjectsSortMode.None)) SetField(tracker, "headTransform", _head);
            foreach (var detector in Object.FindObjectsByType<VRHeightDetector>(FindObjectsSortMode.None)) SetField(detector, "headTransform", _head);
            foreach (var collector in Object.FindObjectsByType<SessionFactsCollector>(FindObjectsSortMode.None)) SetField(collector, "head", _head);
            foreach (var coach in Object.FindObjectsByType<DeterministicVoiceTriggerManager>(FindObjectsSortMode.None)) SetField(coach, "head", _head);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneNames.AAR) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            var ctrl = Object.FindFirstObjectByType<EscapeFire.UI.AARController>();
            if (ctrl != null) SetField(ctrl, "debriefTimeoutSeconds", 45);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (cam != _cam) cam.enabled = false;
        }

        private void SetupRig()
        {
            _dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Demo", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(Path.Combine(_dir, "shots"));

            var camGo = new GameObject("_DemoCamera");
            Object.DontDestroyOnLoad(camGo);
            _cam = camGo.AddComponent<Camera>();
            _cam.fieldOfView = 68f;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 200f;
            _cam.enabled = false;
            _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            _cam.targetTexture = _rt;
            EnableUrpPost(_cam);
            _tex = new Texture2D(W, H, TextureFormat.RGBA32, false);

            BuildOverlay();

            var attrs = new VideoTrackAttributes
            {
                frameRate = new MediaRational(FPS),
                width = W,
                height = H,
                includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.High
            };
            _encoder = new MediaEncoder(Path.Combine(_dir, "demo.mp4"), attrs);
            _recStart = Time.unscaledTime;
            _t0 = Time.time;
            _timeline.AppendLine("| Thời điểm | Diễn biến |");
            _timeline.AppendLine("| ---: | :--- |");
        }

        private static void EnableUrpPost(Camera cam)
        {
            var type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) return;
            var data = cam.GetComponent(type);
            if (data == null) data = cam.gameObject.AddComponent(type);
            type.GetProperty("renderPostProcessing")?.SetValue(data, true);
            var aa = type.GetProperty("antialiasing");
            if (aa != null) aa.SetValue(data, Enum.Parse(aa.PropertyType, "SubpixelMorphologicalAntiAliasing"));
        }

        private void BuildOverlay()
        {
            var go = new GameObject("_DemoOverlay");
            Object.DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _cam;
            canvas.planeDistance = 0.2f;
            canvas.sortingOrder = 100;

            _title = Box(go.transform, new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(560f, 64f), 17, TextAlignmentOptions.TopLeft, out _);
            _title.text = "<b>EscapeFire</b> · Diễn tập PCCC phòng Lab · chế độ <b>Sát hạch</b>\n" +
                          "<size=14>AI Director: Gemma 3 4B qua Ollama, chạy cục bộ trên laptop</size>";
            _status = Box(go.transform, new Vector2(1f, 1f), new Vector2(-16f, -14f), new Vector2(330f, 128f), 15, TextAlignmentOptions.TopLeft, out _statusBox);
            _feed = Box(go.transform, new Vector2(1f, 1f), new Vector2(-16f, -150f), new Vector2(430f, 168f), 14, TextAlignmentOptions.TopLeft, out _feedBox);
            _subtitle = Box(go.transform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1100f, 92f), 20, TextAlignmentOptions.Center, out _);
        }

        private static TextMeshProUGUI Box(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, out GameObject box)
        {
            box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            var rt = (RectTransform)box.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            box.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(box.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 8f);
            trt.offsetMax = new Vector2(-12f, -8f);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            return tmp;
        }

        private void UpdateOverlay()
        {
            var c = LabProgressiveScenarioController.Instance;
            if (c != null && _statusBox.activeSelf)
            {
                var breaker = Object.FindFirstObjectByType<CircuitBreakerSwitch>();
                var alarm = Object.FindFirstObjectByType<FireAlarmButton>();
                var smoke = Object.FindFirstObjectByType<SmokeDensityController>();
                var o2 = Object.FindFirstObjectByType<EscapeFire.Player.OxygenSystem>();
                var sb = new StringBuilder();
                sb.AppendLine($"<b>{PhaseVn(c.CurrentPhase)}</b>");
                if (breaker != null) sb.AppendLine($"Aptomat: {(breaker.IsPowerOn ? "<color=#ffcc66>đang có điện</color>" : "<color=#7CFC9A>đã ngắt</color>")}");
                if (alarm != null) sb.AppendLine($"Báo cháy: {(alarm.IsActivated ? "<color=#7CFC9A>đã báo</color>" : "chưa")}");
                if (smoke != null && c.CurrentPhase >= ScenarioPhase.Phase2_ServerExplosionFire) sb.AppendLine($"Lớp khói: cách sàn {smoke.CurrentSmokeHeight:F1} m · tư thế {(_cam.transform.position.y < 1.3f ? "khom" : "đứng")}");
                if (o2 != null) sb.AppendLine($"Oxy: {o2.CurrentOxygen:F0}%");
                sb.Append($"Ma sát nhận thức CF: {_cf:F2} ({CfVn(_cfState)})");
                _status.text = sb.ToString();
            }

            var feed = new StringBuilder("<b>AI Director (laptop) → kính VR</b>\n");
            if (_feedLines.Count == 0) feed.Append("<color=#bbbbbb>Đang quan sát người học (≥15 s sau khi cháy)…</color>");
            for (int i = Mathf.Max(0, _feedLines.Count - 3); i < _feedLines.Count; i++) feed.AppendLine(_feedLines[i]);
            _feed.text = feed.ToString();

            string sub = string.IsNullOrEmpty(_action) ? "" : $"<color=#ffd27f>Người chơi:</color> {_action}";
            if (Time.time < _mentorUntil) sub += (sub.Length > 0 ? "\n" : "") + $"<color=#8fd3ff>AI Mentor:</color> {_mentor}";
            _subtitle.text = sub;
            _subtitle.transform.parent.gameObject.SetActive(sub.Length > 0);
        }

        // ================================================================== events + log

        private void OnLog(string message, string stack, LogType type)
        {
            var m = EDGE_LOG.Match(message);
            if (!m.Success) return;
            string code = m.Groups[1].Value, rule = m.Groups[2].Value, result = m.Groups[4].Value;
            float sec = int.Parse(m.Groups[3].Value) / 1000f;
            bool ok = result == "Accepted";
            string name = DirectiveVn(code);
            _feedLines.Add(ok
                ? $"<color=#ff9f6e>+ {name}</color>\n   <size=12>{code} · luật {rule} · Gemma {sec:F1} s</size>"
                : $"<color=#bbbbbb>× {name}: lớp 2 trên kính chặn ({result})</color>");
            Note($"🤖 Gemma → `{code}` ({rule}, {sec:F1} s) → **{result}**");
        }

        private void OnDirective(DirectiveResolvedEvent e)
        {
            if (e.Result == DirectiveResult.Accepted)
            {
                _active.Add(e.Directive);
                _accepted.Add($"{DirectiveCodes.ToCode(e.Directive)} ({e.Source})");
                Shot("ai_" + DirectiveCodes.ToCode(e.Directive).ToLowerInvariant(), 1.2f);
                if (e.Directive == Directive.TriggerSmokeSurge) _eye = 1.0f; // react: get low under the smoke
            }
            else _rejected.Add($"{DirectiveCodes.ToCode(e.Directive)}: {e.Result}");
        }

        private void OnMentor(MentorMessageEvent e)
        {
            _mentor = e.Message;
            _mentorUntil = Time.time + 6f;
            Note($"🗣️ AI Mentor: {e.Message}");
        }

        private void OnCf(CognitiveStateChangedEvent e) { _cf = e.CognitiveFriction; _cfState = e.State; }

        private void OnPhase(PhaseChangedEvent e)
        {
            if (e.Phase == ScenarioPhase.Phase2_ServerExplosionFire && _fireStart < 0f) _fireStart = Time.time;
            Note($"📍 {PhaseVn(e.Phase)}");
        }

        private void Act(string s) { _action = s; Note($"🎮 {s}"); }

        private void Note(string s) => _timeline.AppendLine($"| {Time.time - _t0,6:F1}s | {s.Replace("|", "/")} |");

        private void WriteTimeline()
        {
            if (string.IsNullOrEmpty(_dir)) return;
            var sb = new StringBuilder();
            sb.AppendLine("# Demo: ván Sát hạch với AI Director Gemma 3 4B (Ollama, local)");
            sb.AppendLine();
            sb.AppendLine($"- Ngày: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"- Kết quả: **{(SessionResult.HasResult ? (SessionResult.Passed ? "THOÁT HIỂM AN TOÀN" : "THẤT BẠI — " + SessionResult.FailCause) : "chưa kết thúc")}**");
            if (SessionResult.Scorecard.Count > 0) sb.AppendLine($"- Điểm: **{AfterActionReport.Total(SessionResult.Scorecard)}/100**");
            sb.AppendLine($"- AI thêm thử thách: {(_accepted.Count == 0 ? "không có" : string.Join(", ", _accepted))}");
            sb.AppendLine($"- Bị chặn: {(_rejected.Count == 0 ? "không có" : string.Join(", ", _rejected))}");
            sb.AppendLine($"- Video: {_frames} khung hình = {_frames / (float)FPS:F0} s @ {FPS} fps, {W}x{H}");
            sb.AppendLine("- Ghi chú: telemetry gửi 5 s/lần (cấu hình thật 10 s) để video ngắn; mọi quyết định thử thách và nhận xét AAR là output thật của Gemma.");
            if (!string.IsNullOrEmpty(SessionResult.Debrief)) { sb.AppendLine(); sb.AppendLine($"**Nhận xét AAR:** {SessionResult.Debrief}"); }
            sb.AppendLine();
            sb.Append(_timeline);
            File.WriteAllText(Path.Combine(_dir, "timeline.md"), sb.ToString(), new UTF8Encoding(false));
        }

        // ================================================================== text helpers

        private static string PhaseVn(ScenarioPhase p)
        {
            switch (p)
            {
                case ScenarioPhase.Phase0_PeacefulPractice: return "Giai đoạn 0 · Yên bình";
                case ScenarioPhase.Phase1_PowerFlickerWarning: return "Giai đoạn 1 · Cảnh báo chập điện";
                case ScenarioPhase.Phase2_ServerExplosionFire: return "Giai đoạn 2 · Cháy tủ server";
                case ScenarioPhase.Phase3_SmokeEvacuation: return "Giai đoạn 3 · Khói lan, sơ tán";
                case ScenarioPhase.Phase4_CompletedWin: return "Thoát hiểm thành công";
                case ScenarioPhase.Phase5_GameOverFail: return "Diễn tập thất bại";
                default: return p.ToString();
            }
        }

        private static string CfVn(CognitiveState s) => s == CognitiveState.Calm ? "bình tĩnh" : s == CognitiveState.Nominal ? "bình thường" : "căng thẳng";

        private static string DirectiveVn(string code)
        {
            switch (code)
            {
                case "TRIGGER_CO2_PIN_JAM": return "Chốt bình CO2 bị kẹt";
                case "TRIGGER_SMOKE_SURGE": return "Khói đặc tràn xuống thấp";
                case "TRIGGER_ALARM_CABLE_CUT": return "Đứt cáp chuông báo cháy";
                case "TRIGGER_BLACKOUT": return "Mất điện chiếu sáng";
                case "ESCALATE_SECONDARY_FIRE": return "Cháy lan sang bàn khác";
                case "TRIGGER_EXTINGUISHER_LOW_PRESSURE": return "Bình CO2 yếu áp";
                case "ESCALATE_FM200_COUNTDOWN": return "Còi FM-200 đếm ngược 30 s";
                default: return code;
            }
        }

        // ================================================================== reflection helpers

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
    }
}
#endif
