using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Generation;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.NPC;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Runs a composed challenge on the headset (plan V4.5a/b). Tier 2 = scene checks on top of the shared
    /// <see cref="ChallengeValidator"/>: the route / zone / device / NPC exists and is in a usable state, the FPS budget
    /// has room for another fire, the learner is not inside a stairwell that would be blocked, and at least one way
    /// down stays open. Accepted pieces start after their delay; the runner remembers used piece codes and blocked
    /// routes (sent back to the server in the telemetry).
    /// </summary>
    public class ChallengeRunner : MonoBehaviour
    {
        private static readonly Dictionary<string, string> RouteDoor = new Dictionary<string, string>
        {
            { "stairwell_a", "StairwellA/FireDoor_F3" },
            { "stairwell_b", "StairwellB/FireDoor_F3" }
        };
        private static readonly Dictionary<string, string> RouteZone = new Dictionary<string, string>
        {
            { "stairwell_a", "StairwellA/StairwellA_Smoke" },
            { "stairwell_b", "StairwellB/StairwellB_Smoke" },
            { "corridor", "Smoke_Corridor" }
        };

        private readonly List<string> _applied = new List<string>();
        private readonly List<string> _blocked = new List<string>();
        private readonly HashSet<string> _darkZones = new HashSet<string>();

        public IReadOnlyList<string> AppliedCodes => _applied;
        public IReadOnlyList<string> BlockedRoutes => _blocked;

        public static ChallengeRunner Instance { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Context for the shared validator, from what this drill has already seen.</summary>
        public ChallengeContext Context(int challengesGiven, float cf) => new ChallengeContext
        {
            ChallengesGiven = challengesGiven,
            Cf = cf,
            Prior = new List<string>(_applied),
            BlockedRoutes = new List<string>(_blocked)
        };

        /// <summary>Tier-2 scene checks. Returns null when every piece can run now, else the first reason.</summary>
        public string SceneCheck(ChallengeCheck check)
        {
            if (!check.Ok) return check.Error;
            foreach (var p in check.Pieces)
            {
                string why = CheckPiece(p);
                if (why != null) return why;
            }
            return null;
        }

        private static Transform Building()
        {
            var b = GameObject.Find("[BUILDING_V2]");
            return b != null ? b.transform : null;
        }

        private string CheckPiece(ChallengePiece p)
        {
            switch (p.Kind)
            {
                case "SMOKE_SURGE":
                {
                    var smoke = FindFirstObjectByType<SmokeDensityController>();
                    return smoke != null && smoke.CanSurge ? null : "smoke_cannot_surge";
                }
                case "LIGHTS_OFF":
                {
                    string zone = p.Option("zone");
                    if (_darkZones.Contains(zone)) return "already_active:" + zone;
                    if (zone == "lab")
                    {
                        var blackout = FindFirstObjectByType<BlackoutController>();
                        return blackout != null && !blackout.IsBlackout ? null : "already_active:lab";
                    }
                    return Zone(zone) != null ? null : "zone_missing:" + zone;
                }
                case "DEVICE_FAULT":
                {
                    var co2 = Co2s();
                    switch (p.Option("device"))
                    {
                        case "co2_pin_jam":
                            if (co2.Count == 0) return "device_missing:co2";
                            foreach (var e in co2) if (e.IsPinPulled) return "already_unpinned";
                            return null;
                        case "co2_low_pressure":
                            if (co2.Count == 0) return "device_missing:co2";
                            foreach (var e in co2) if (e.HasLowPressure) return "already_active:co2_low_pressure";
                            return null;
                        default:
                            var alarms = FindObjectsByType<FireAlarmButton>(FindObjectsSortMode.None);
                            if (alarms.Length == 0) return "device_missing:alarm";
                            foreach (var a in alarms) if (a.IsActivated || a.IsCableCut) return "alarm_already_raised";
                            return null;
                    }
                }
                case "FIRE_SPOT":
                {
                    var slot = Slot(p.Option("slot"));
                    if (slot == null) return "slot_missing:" + p.Option("slot");
                    if (!slot.IsAvailable) return "slot_unavailable:" + p.Option("slot");
                    var budget = PerformanceBudgetGovernor.Instance;
                    return budget == null || budget.CanSpawnFire ? null : "fps_budget";
                }
                case "BLOCK_ROUTE":
                {
                    string route = p.Option("route");
                    if (Door(route) == null) return "route_missing:" + route;
                    // life safety invariant: another way down must exist and be open
                    bool otherOpen = false;
                    foreach (var other in RouteDoor.Keys)
                        if (other != route && !_blocked.Contains(other) && Door(other) != null) otherOpen = true;
                    if (!otherOpen) return "no_exit_left";
                    var zone = Zone(route);
                    var head = Head();
                    if (zone != null && head != null && zone.Space.Contains(head.position)) return "learner_inside_route";
                    return null;
                }
                case "NPC_BEHAVIOUR":
                {
                    var minh = FindFirstObjectByType<PanicRunner>();
                    return minh != null && !minh.Active ? null : "npc_unavailable";
                }
                case "FM200":
                {
                    var fm = FM200System.Instance;
                    return fm != null && !fm.IsCountdownActive && !fm.HasDischarged ? null : "already_active:fm200";
                }
                default:
                    return "unknown_piece:" + p.Kind;
            }
        }

        /// <summary>Starts every piece after its delay (call only after <see cref="SceneCheck"/> returned null).</summary>
        public void Run(ChallengeCheck check)
        {
            foreach (var p in check.Pieces)
            {
                _applied.Add(p.Code);
                if (p.Kind == "BLOCK_ROUTE") _blocked.Add(p.Option("route"));
                if (p.Kind == "LIGHTS_OFF") _darkZones.Add(p.Option("zone"));
                StartCoroutine(After(p));
            }
            if (!string.IsNullOrEmpty(check.Reason)) Debug.Log($"[ChallengeRunner] {string.Join(" + ", check.Codes)} — {check.Reason} ({check.Rule})");
        }

        private IEnumerator After(ChallengePiece p)
        {
            if (p.DelaySeconds > 0f) yield return new WaitForSeconds(p.DelaySeconds);
            Apply(p);
        }

        private void Apply(ChallengePiece p)
        {
            switch (p.Kind)
            {
                case "SMOKE_SURGE":
                {
                    var smoke = FindFirstObjectByType<SmokeDensityController>();
                    if (smoke != null && smoke.Surge(p.Number("drop_m", 0.8f), p.Number("speed", 2f)))
                        Mentor("Khói đen đặc đột ngột tràn xuống thấp! Tư thế của bạn lúc này nên thế nào?");
                    break;
                }
                case "LIGHTS_OFF":
                    LightsOff(p.Option("zone"));
                    break;
                case "DEVICE_FAULT":
                    switch (p.Option("device"))
                    {
                        case "co2_pin_jam": foreach (var e in Co2s()) e.JamSafetyPin(2); break;
                        case "co2_low_pressure":
                            foreach (var e in Co2s()) e.SetLowPressure(4f);
                            Mentor("Kim đồng hồ áp suất bình CO2 đang ở vạch đỏ — chỉ còn vài giây phun. Bạn sẽ dùng nó thế nào?");
                            break;
                        default: foreach (var a in FindObjectsByType<FireAlarmButton>(FindObjectsSortMode.None)) a.CutCable(); break;
                    }
                    break;
                case "FIRE_SPOT":
                {
                    var slot = Slot(p.Option("slot"));
                    var c = LabProgressiveScenarioController.Instance;
                    if (slot != null && slot.Ignite(c != null ? c.ServerFire : null, p.Number("intensity", 1f)))
                        Mentor($"CẢNH BÁO: Lửa đã lan sang {slot.Description}! Đánh giá lại: dập tiếp hay sơ tán?");
                    break;
                }
                case "BLOCK_ROUTE":
                {
                    string route = p.Option("route");
                    var door = Door(route);
                    if (door != null) door.Lock(route == "stairwell_a"
                        ? "Cửa thang A nóng và khói dày phía sau — không mở được. Dùng cầu thang B!"
                        : "Cửa thang B nóng và khói dày phía sau — không mở được. Dùng cầu thang A!");
                    var zone = Zone(route);
                    if (zone != null) zone.Flood(0.95f);
                    break;
                }
                case "NPC_BEHAVIOUR":
                {
                    var minh = FindFirstObjectByType<PanicRunner>();
                    if (minh != null) minh.Arm();
                    break;
                }
                case "FM200":
                    if (FM200System.Instance != null) FM200System.Instance.TriggerCountdown(p.Number("countdown_s", 30f));
                    break;
            }
        }

        /// <summary>Power to the normal lights in a zone fails: only battery emergency lights (dim) and EXIT signs stay.</summary>
        private void LightsOff(string zone)
        {
            if (zone == "lab")
            {
                var blackout = FindFirstObjectByType<BlackoutController>();
                if (blackout != null) blackout.Trigger();
                Mentor("Mất điện chiếu sáng! Chỉ còn đèn khẩn cấp. Hãy tìm biển EXIT màu xanh và lần theo mép tường.");
                return;
            }
            var z = Zone(zone);
            if (z == null) return;
            z.SetDark(true);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional || !z.Space.Contains(l.transform.position)) continue;
                if (l.name.Contains("ExitGlow")) continue;
                l.intensity *= l.name.StartsWith("Emergency") ? 0.3f : 0f;
            }
            Mentor(zone == "corridor" ? "Hành lang mất điện, chỉ còn biển EXIT phát sáng. Một tay lần theo tường!" : "Buồng thang mất điện — vịn lan can, đi chậm từng bậc.");
        }

        private static SmokeZone Zone(string key)
        {
            var b = Building();
            if (b == null || !RouteZone.TryGetValue(key, out var path)) return null;
            var t = b.Find(path);
            return t != null ? t.GetComponent<SmokeZone>() : null;
        }

        private static PushBarDoor Door(string route)
        {
            var b = Building();
            if (b == null || !RouteDoor.TryGetValue(route, out var path)) return null;
            var t = b.Find(path);
            return t != null ? t.GetComponent<PushBarDoor>() : null;
        }

        private static SecondaryFireSlot Slot(string key)
        {
            string name = key == "chemical_tray" ? "SecondaryFireSlot_ChemicalTray" : "SecondaryFireSlot_CableDesk";
            foreach (var s in FindObjectsByType<SecondaryFireSlot>(FindObjectsSortMode.None)) if (s.name == name) return s;
            return null;
        }

        private static List<FireExtinguisher> Co2s()
        {
            var list = new List<FireExtinguisher>();
            foreach (var e in FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None))
                if (e.Type == ExtinguisherType.CO2) list.Add(e);
            return list;
        }

        private static Transform Head()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            return origin != null && origin.Camera != null ? origin.Camera.transform : null;
        }

        private static void Mentor(string message) => EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(message));
    }
}
