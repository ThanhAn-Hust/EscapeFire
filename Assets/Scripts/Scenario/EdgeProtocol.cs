using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EscapeFire.Scenario
{
    /// <summary>Headset → edge server telemetry (must match Server/escapefire_edge/schema.py TelemetryMessage).</summary>
    public sealed class TelemetryPayload
    {
        [JsonProperty("type")] public string Type = "telemetry";
        [JsonProperty("session_id")] public string SessionId = "";
        [JsonProperty("time_elapsed")] public float TimeElapsed;
        [JsonProperty("cognitive_friction_cf")] public float CognitiveFriction;
        [JsonProperty("is_power_cut")] public bool IsPowerCut;
        [JsonProperty("held_item")] public string HeldItem = "None";
        [JsonProperty("current_zone")] public string CurrentZone = "Unknown";
        [JsonProperty("phase")] public string Phase = "";
        [JsonProperty("emergency_elapsed")] public float EmergencyElapsed;
        [JsonProperty("prior_challenges")] public List<string> PriorChallenges = new List<string>();
        /// <summary>Challenges actually applied (prior_challenges also lists permanently refused ones). Drives the tier.</summary>
        [JsonProperty("accepted_challenges")] public int AcceptedChallenges;
        [JsonProperty("prior_pieces")] public List<string> PriorPieces = new List<string>();
        [JsonProperty("blocked_routes")] public List<string> BlockedRoutes = new List<string>();
        [JsonProperty("weak_skills")] public List<string> WeakSkills = new List<string>();
    }

    /// <summary>Edge server → headset decision.</summary>
    public sealed class DirectiveReply
    {
        public string Directive = "MAINTAIN_CURRENT_PACE";
        public string PcccRuleReference = "";
        public string PedagogicalReason = "";
        public string Source = "";
        public float LatencyMs;
    }

    public static class EdgeProtocol
    {
        public static string SerializeTelemetry(TelemetryPayload payload)
        {
            if (payload.CognitiveFriction < 0f) payload.CognitiveFriction = 0f;
            if (payload.CognitiveFriction > 1f) payload.CognitiveFriction = 1f;
            if (payload.TimeElapsed < 0f) payload.TimeElapsed = 0f;
            return JsonConvert.SerializeObject(payload);
        }

        public static string SerializeResult(string directiveCode, DirectiveResult result)
        {
            return JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                { "type", "directive_result" },
                { "directive", directiveCode },
                { "result", result.ToString() }
            });
        }

        /// <summary>V4: a composed challenge ({"type": "challenge", "pieces": [...], ...}); returns the whole object.</summary>
        public static bool TryParseChallenge(string json, out JObject challenge)
        {
            challenge = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var obj = JObject.Parse(json);
                if ((string)obj["type"] != "challenge") return false;
                challenge = obj;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>Parses a server message. Returns false for errors, other message types or malformed JSON.</summary>
        public static bool TryParseDirective(string json, out DirectiveReply reply)
        {
            reply = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            JObject obj;
            try
            {
                obj = JObject.Parse(json);
            }
            catch (JsonException)
            {
                return false;
            }

            if ((string)obj["type"] != "directive") return false;
            string directive = (string)obj["directive"];
            if (string.IsNullOrWhiteSpace(directive)) return false;

            reply = new DirectiveReply
            {
                Directive = directive,
                PcccRuleReference = (string)obj["pccc_rule_reference"] ?? "",
                PedagogicalReason = (string)obj["pedagogical_reason"] ?? "",
                Source = (string)obj["source"] ?? "",
                LatencyMs = obj["latency_ms"] != null ? (float)obj["latency_ms"] : 0f
            };
            return true;
        }
    }
}
