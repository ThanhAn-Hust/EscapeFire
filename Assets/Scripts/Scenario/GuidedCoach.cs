using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using EscapeFire.Core;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.UI;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Guided mode, step by step (playtest 18/11: one long instruction at the start of the fire, then silence while a
    /// first-timer stood still). Once the fire breaks out the coach follows the drill one action at a time and, the
    /// longer the current action is not done, the more it helps (hint ladder):
    /// 0 s ask (retrieval: "what must you do first?"), 8 s tell the action, 16 s light up the object to use (bobbing
    /// yellow arrow + glow), 24 s an arrow in front of the learner pointing the way and how to do it.
    /// The current step is pinned as a subtitle in front of the eyes. Steps that no longer matter are skipped (no
    /// "cut the power" once the fire is out, no fire-fighting once the learner is out in the corridor).
    /// Not active in Exam mode. Hints used per step are kept for the debrief.
    /// </summary>
    public class GuidedCoach : MonoBehaviour
    {
        public const float TellAfter = 8f, ShowAfter = 16f, LeadAfter = 24f;

        private sealed class Step
        {
            public string Id, Ask, Tell, How;
            public Func<bool> Done, Relevant;
            public Func<Transform> Target;
        }

        private readonly List<Step> _steps = new List<Step>();
        private Step _current;
        private float _since;
        private int _level = -1;
        private Transform _beacon, _guide;
        private Light _glow;
        private Transform _head;
        private bool _built;

        public string CurrentStep => _current?.Id;
        public int HintLevel => _level;
        /// <summary>Highest hint level each step needed (0 ask … 3 lead), for the debrief.</summary>
        public readonly Dictionary<string, int> HintsUsed = new Dictionary<string, int>();

        private static bool Guided => GameSession.Mode == TrainingMode.Guided;

        private void Update()
        {
            var c = LabProgressiveScenarioController.Instance;
            bool emergency = c != null && c.CurrentPhase >= ScenarioPhase.Phase2_ServerExplosionFire && c.CurrentPhase < ScenarioPhase.Phase4_CompletedWin;
            if (!Guided || !emergency) { Clear(); return; }
            if (!_built) Build(c);
            if (_head == null && Camera.main != null) _head = Camera.main.transform;

            Step next = null;
            foreach (var s in _steps) if (Safe(s.Relevant) && !Safe(s.Done)) { next = s; break; }
            if (next != _current)
            {
                _current = next;
                _since = 0f;
                _level = -1;
                HideCues();
                if (_current == null) { ViewSubtitle.Instance?.Pin(null); return; }
            }
            if (_current == null) return;
            _since += Time.deltaTime;
            int level = _since >= LeadAfter ? 3 : _since >= ShowAfter ? 2 : _since >= TellAfter ? 1 : 0;
            if (level != _level)
            {
                _level = level;
                HintsUsed[_current.Id] = Math.Max(level, HintsUsed.TryGetValue(_current.Id, out var h) ? h : 0);
                string line = level == 0 ? _current.Ask : level == 1 ? _current.Tell : level == 2 ? _current.Tell + " (vật cần dùng đang sáng mũi tên vàng)" : _current.How;
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(line));
                ViewSubtitle.Instance?.Pin(level == 0 ? _current.Ask : _current.Tell);
            }
            UpdateCues();
        }

        private static bool Safe(Func<bool> f)
        {
            try { return f == null || f(); } catch { return false; }
        }

        // ------------------------------------------------------------------ the lab drill, step by step

        private void Build(LabProgressiveScenarioController c)
        {
            _built = true;
            var alarm = FindFirstObjectByType<FireAlarmButton>();
            var breaker = FindFirstObjectByType<CircuitBreakerSwitch>();
            var checker = FindFirstObjectByType<DoorHeatChecker>();
            var exit = checker != null ? checker.GetComponent<ExitDoor>() : FindFirstObjectByType<ExitDoor>();
            FireExtinguisher co2 = null;
            foreach (var e in FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None)) if (e.Type == ExtinguisherType.CO2) co2 = e;
            NPCSimulator hoa = null;
            foreach (var n in FindObjectsByType<NPCSimulator>(FindObjectsSortMode.None)) if (n.Role == NPCRole.HoaFaint) hoa = n;
            var minh = FindFirstObjectByType<PanicRunner>();
            var rescue = GameObject.Find("[RESCUE_V3]");
            Transform radio = rescue != null ? rescue.transform.Find("RadioDock/Radio") : null;
            var building = GameObject.Find("[BUILDING_V2]");
            Transform stairsA = building != null ? building.transform.Find("StairwellA/FireDoor_F3") : null;
            var fire = c.ServerFire;
            var grab = co2 != null ? co2.GetComponent<XRGrabInteractable>() : null;

            bool InLab() => _head == null || _head.position.z > -7f;
            bool FireBurning() => fire != null && fire.gameObject.activeInHierarchy && !fire.IsExtinguished;
            bool FightingFire() => InLab() && FireBurning() && c.CurrentPhase == ScenarioPhase.Phase2_ServerExplosionFire;

            Add("alarm", alarm != null ? alarm.transform : null,
                "Có cháy! Việc ĐẦU TIÊN bạn phải làm là gì?",
                "Báo động cho mọi người: ấn nút báo cháy màu đỏ cạnh cửa ra vào.",
                "Đi theo mũi tên tới nút đỏ trên tường, ấn mạnh đầu ngón trỏ vào nút.",
                () => alarm == null || alarm.IsActivated, () => true);
            Add("call114", radio,
                "Đã báo động. Ai cần biết tin có cháy ngay bây giờ?",
                "Gọi 114: cầm bộ đàm trên giá cạnh cửa, đưa sát miệng, giữ nút nói.",
                "Nói rõ 4 ý: địa chỉ (đọc trên bảng cạnh giá), tầng, cái gì đang cháy, có người mắc kẹt không.",
                () => radio == null || Call114Dialogue.Instance == null || Call114Dialogue.Instance.IsComplete, FightingFire);
            Add("power", breaker != null ? breaker.transform : null,
                "Tủ server đang cháy — cháy thiết bị điện. Trước khi phun, phải làm gì?",
                "Ngắt điện: mở cửa tủ điện trên tường, gạt cầu dao xuống hết.",
                "Nắm tay cửa tủ điện kéo mở, rồi nắm cần gạt kéo xuống đến hết hành trình.",
                () => breaker == null || !breaker.IsPowerOn, FightingFire);
            Add("take", co2 != null ? co2.transform : null,
                "Điện đã ngắt. Dùng bình nào cho cháy điện?",
                "Lấy bình CO2 (bình đỏ có loa đen) khỏi giá treo.",
                "Khom người, nắm quai xách trên đầu bình, nhấc lên rồi kéo ra khỏi giá.",
                () => co2 == null || co2.IsPinPulled || (grab != null && grab.isSelected), FightingFire);
            Add("pin", co2 != null ? co2.transform.Find("SafetyPin") ?? co2.transform : null,
                "Bình đã trong tay. Bước P của PASS là gì?",
                "Rút chốt an toàn: tay kia nắm vòng chốt và kéo ra.",
                "Một tay giữ bình, tay còn lại móc vào vòng chốt kim loại rồi kéo mạnh sang bên.",
                () => co2 == null || co2.IsPinPulled, FightingFire);
            Add("spray", fire != null ? fire.transform : null,
                "Chốt đã rút. Chĩa loa vào đâu của đám cháy?",
                "PASS: cầm loa, tiến lại khoảng 2 m, khom thấp, chĩa vào GỐC lửa, bóp cò và quét qua lại.",
                "Tay kia cầm loa đen, hướng xuống chân ngọn lửa, bóp cò hết cỡ, quét trái phải đến khi tắt hẳn.",
                () => !FireBurning(), FightingFire);
            Add("hoa", hoa != null ? hoa.transform : null,
                "Hoa đang choáng vì khói. Bạn ấy còn tỉnh — giúp thế nào?",
                "Dìu Hoa: khom xuống ngang tầm, nắm dưới nách bạn ấy, đỡ dậy rồi đi chậm ra cửa.",
                "Hạ người thấp, đưa tay vào nách Hoa, bóp nút cầm và giữ; đi chậm, Hoa sẽ đi theo tay bạn.",
                () => hoa == null || !hoa.gameObject.activeInHierarchy || !hoa.IsFainted || hoa.IsRescued, InLab);
            Add("door", checker != null ? checker.transform : null,
                "Trước khi mở một cánh cửa trong đám cháy, cần kiểm tra gì?",
                "Thử nóng cửa: áp MU bàn tay lên cánh cửa khoảng 1 giây.",
                "Xoay mu bàn tay (không phải lòng bàn tay) áp lên cửa, giữ yên một giây.",
                () => checker == null || checker.HasCheckedHeat, InLab);
            Add("open", exit != null ? exit.transform : null,
                "Cửa không nóng. Ra ngoài thế nào?",
                "Mở cửa: xoay tay nắm rồi đẩy cửa ra hành lang.",
                "Nắm tay nắm, xoay xuống, rồi đẩy cánh cửa và bước ra theo nó.",
                () => exit == null || exit.IsOpen, InLab);
            Add("minh", minh != null ? minh.transform : null,
                "Minh đang bấm thang máy! Đi thang máy khi cháy có an toàn không?",
                "Ngăn Minh: lại gần, nắm tay hoặc vai Minh trước khi cửa thang máy mở.",
                "Đi nhanh tới cửa thang máy, bóp nút cầm vào vai Minh.",
                () => minh == null || !minh.Active || minh.Stopped || minh.Trapped, () => !InLab());
            Add("nam", VictimBody.All.Count > 0 ? VictimBody.All[0].transform : null,
                "Có người nằm bất tỉnh trước cửa cầu thang A. Đưa bạn ấy đi thế nào?",
                "Kéo Rautek vào buồng thang: mở cửa, quỳ phía đầu, luồn CẢ HAI tay dưới nách, kéo lùi.",
                "Đẩy thanh cửa buồng thang, quỳ ở phía đầu Nam, hai tay bóp cầm dưới nách, lùi chậm vào trong.",
                () => VictimBody.All.Count == 0 || VictimBody.All[0].IsSafe, () => !InLab());
            Add("stairs", stairsA,
                "Thoát xuống bằng gì: thang máy hay thang bộ?",
                "Đi thang BỘ xuống tầng 1, bám lan can, theo biển LỐI THOÁT.",
                "Vào buồng thang A, đi xuống 2 tầng, đẩy thanh cửa thoát hiểm ở tầng 1 ra sân.",
                () => _head != null && _head.position.y < -5f, () => !InLab());
            Add("report", AssemblyPoint.Instance != null ? AssemblyPoint.Instance.transform : null,
                "Đã tới điểm tập kết. Người điều phối cần biết điều gì?",
                "Báo cáo điểm danh: còn ai trong tòa nhà và ở đâu.",
                "Ấn vào dòng đúng trên bảng báo cáo cạnh người điều phối.",
                () => AssemblyPoint.Instance == null || AssemblyPoint.Instance.AnswerCorrect, () => _head != null && _head.position.y < -5f);
        }

        private void Add(string id, Transform target, string ask, string tell, string how, Func<bool> done, Func<bool> relevant) =>
            _steps.Add(new Step { Id = id, Ask = ask, Tell = tell, How = how, Done = done, Relevant = relevant, Target = () => target });

        // ------------------------------------------------------------------ cues: beacon over the object, guide arrow

        private void UpdateCues()
        {
            Transform target = _current.Target();
            if (target == null || !target.gameObject.activeInHierarchy) { HideCues(); return; }
            Vector3 top = Top(target);
            if (_level >= 2)
            {
                if (_beacon == null) _beacon = MakeArrowQuad("CoachBeacon", 0.32f);
                _beacon.gameObject.SetActive(true);
                _beacon.position = top + Vector3.up * (0.35f + 0.08f * Mathf.Sin(Time.time * 4f));
                if (_head != null) // billboard around the vertical axis, arrow pointing down
                {
                    Vector3 to = _head.position - _beacon.position; to.y = 0f;
                    if (to.sqrMagnitude > 1e-4f) _beacon.rotation = Quaternion.LookRotation(-to.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, 180f);
                }
                if (_glow == null)
                {
                    _glow = new GameObject("CoachGlow").AddComponent<Light>();
                    _glow.type = LightType.Point;
                    _glow.color = new Color(1f, 0.8f, 0.2f);
                    _glow.range = 1.6f;
                    _glow.shadows = LightShadows.None;
                }
                _glow.gameObject.SetActive(true);
                _glow.transform.position = top + Vector3.up * 0.2f;
                _glow.intensity = 1.2f + 0.8f * Mathf.Sin(Time.time * 5f);
            }
            if (_level >= 3 && _head != null)
            {
                if (_guide == null) _guide = MakeArrowQuad("CoachGuideArrow", 0.25f);
                _guide.gameObject.SetActive(true);
                Vector3 fwd = _head.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
                Vector3 to = top - _head.position; to.y = 0f;
                to = to.sqrMagnitude > 1e-4f ? to.normalized : fwd;
                // lying flat 0.7 m ahead at chest height, its tip turned towards the target
                _guide.position = _head.position + fwd * 0.7f + Vector3.down * 0.45f;
                _guide.rotation = Quaternion.LookRotation(Vector3.down, to);
            }
        }

        private static Vector3 Top(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return t.position + Vector3.up * 0.3f;
            var b = rs[0].bounds;
            foreach (var r in rs) if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        private static Transform MakeArrowQuad(string name, float size)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Destroy(q.GetComponent<Collider>());
            q.transform.localScale = Vector3.one * size;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetTexture("_BaseMap", Resources.Load<Texture2D>("UI/EF_GuideArrow"));
            // transparent (the arrow's surroundings are see-through), drawn from both sides
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_Cull", 0f);
            mat.renderQueue = 3100;
            q.GetComponent<Renderer>().sharedMaterial = mat;
            return q.transform;
        }

        private void HideCues()
        {
            if (_beacon != null) _beacon.gameObject.SetActive(false);
            if (_guide != null) _guide.gameObject.SetActive(false);
            if (_glow != null) _glow.gameObject.SetActive(false);
        }

        private void Clear()
        {
            if (_current != null) { _current = null; ViewSubtitle.Instance?.Pin(null); }
            HideCues();
        }

        private void OnDestroy()
        {
            if (_beacon != null) Destroy(_beacon.gameObject);
            if (_guide != null) Destroy(_guide.gameObject);
            if (_glow != null) Destroy(_glow.gameObject);
        }
    }
}
