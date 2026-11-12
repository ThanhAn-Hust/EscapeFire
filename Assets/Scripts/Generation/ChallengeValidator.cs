using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace EscapeFire.Generation
{
    /// <summary>One validated piece of a composed challenge.</summary>
    public sealed class ChallengePiece
    {
        public string Kind;
        public readonly Dictionary<string, string> Options = new Dictionary<string, string>();
        public readonly Dictionary<string, float> Numbers = new Dictionary<string, float>();
        public float DelaySeconds;
        public string Code;
        public int Tier;
        public readonly List<string> Skills = new List<string>();

        public string Option(string name) => Options.TryGetValue(name, out var v) ? v : "";
        public float Number(string name, float fallback = 0f) => Numbers.TryGetValue(name, out var v) ? v : fallback;
    }

    public sealed class ChallengeContext
    {
        public int ChallengesGiven;
        public float Cf;
        public List<string> Prior = new List<string>();
        public List<string> BlockedRoutes = new List<string>();
        /// <summary>VL.3: the learner's weakest skills (preference, never a rejection reason).</summary>
        public List<string> WeakSkills = new List<string>();
    }

    public sealed class ChallengeCheck
    {
        public bool Ok;
        public readonly List<string> Errors = new List<string>();
        public readonly List<ChallengePiece> Pieces = new List<ChallengePiece>();
        public int Tier;
        public string Rule = "";
        public string Reason = "";
        public string Error => Errors.Count > 0 ? Errors[0] : "";
        public IEnumerable<string> Codes => Pieces.Select(p => p.Code);
        public List<string> Skills => Pieces.SelectMany(p => p.Skills).Distinct().ToList();
        public bool Targets(IEnumerable<string> weak) => weak != null && Skills.Intersect(weak).Any();
    }

    /// <summary>
    /// Tier-1 rules for a composed challenge (plan V4.5b), identical to Server/escapefire_edge/challenge.py — the same
    /// error codes, checked against the same fixtures (Server/tests/fixtures/challenge_cases.json). Pure logic: the
    /// headset re-runs it on every challenge the server sends, then <see cref="EscapeFire.Scenario.ChallengeRunner"/>
    /// adds the scene checks (route exists, an exit stays reachable, FPS budget, device state).
    /// </summary>
    public static class ChallengeValidator
    {
        public const int MaxReasonChars = 300;

        public static ChallengeCheck Validate(JToken spec, ChallengeContext ctx)
        {
            var r = new ChallengeCheck();
            if (!(spec is JObject data)) return Fail(r, "not_object");
            if (!(data["pieces"] is JArray rawPieces) || rawPieces.Count == 0) return Fail(r, "no_pieces");

            var cfg = V4Catalog.Challenge;
            int limit = (int)cfg["max_pieces"];
            if (ctx.Cf > (float)cfg["max_pieces_when_cf_above"]["cf"]) limit = System.Math.Min(limit, (int)cfg["max_pieces_when_cf_above"]["max"]);
            if (rawPieces.Count > limit) r.Errors.Add($"too_many_pieces:{rawPieces.Count}>{limit}");

            var known = V4Catalog.Pieces;
            float maxDelay = (float)cfg["max_delay_s"];
            var clean = new List<ChallengePiece>();
            foreach (var token in rawPieces)
            {
                if (!(token is JObject item)) { r.Errors.Add("bad_piece"); continue; }
                string kind = V4Catalog.Text(item["piece"] ?? "").Trim().ToUpperInvariant();
                if (item["piece"] == null || item["piece"].Type == JTokenType.Null) kind = "";
                if (!(known[kind] is JObject spec2))
                {
                    r.Errors.Add($"unknown_piece:{(kind.Length > 0 ? kind : "<missing>")}");
                    continue;
                }
                var paramsIn = item["params"];
                if (paramsIn == null || paramsIn.Type == JTokenType.Null) paramsIn = new JObject();
                if (!(paramsIn is JObject pin)) { r.Errors.Add($"bad_param:{kind}"); continue; }
                var piece = new ChallengePiece { Kind = kind };
                foreach (var prop in ((JObject)spec2["params"]).Properties())
                {
                    string name = prop.Name;
                    var rng = (JObject)prop.Value;
                    if (!pin.ContainsKey(name)) { r.Errors.Add($"missing_param:{kind}.{name}"); continue; }
                    if (rng["options"] is JArray options)
                    {
                        string value = V4Catalog.Text(pin[name]).Trim().ToLowerInvariant();
                        if (!options.Values<string>().Contains(value)) { r.Errors.Add($"bad_option:{kind}.{name}"); continue; }
                        piece.Options[name] = value;
                    }
                    else
                    {
                        if (!V4Catalog.TryNumber(pin[name], out float v)) { r.Errors.Add($"bad_param:{kind}.{name}"); continue; }
                        if (v < (float)rng["min"] || v > (float)rng["max"]) { r.Errors.Add($"param_out_of_range:{kind}.{name}"); continue; }
                        piece.Numbers[name] = v;
                    }
                }
                foreach (var prop in pin.Properties())
                    if (spec2["params"][prop.Name] == null) r.Errors.Add($"unknown_param:{kind}.{prop.Name}");
                var delayToken = item["delay_s"];
                float delay;
                if (delayToken == null || delayToken.Type == JTokenType.Null) delay = 0f;
                else if (!V4Catalog.TryNumber(delayToken, out delay)) delay = -1f;
                if (delay < 0f || delay > maxDelay)
                {
                    r.Errors.Add($"bad_delay:{kind}");
                    delay = 0f;
                }
                piece.DelaySeconds = delay;
                piece.Code = CodeOf(piece, (JObject)spec2);
                piece.Tier = TierOf(piece, (JObject)spec2);
                piece.Skills.AddRange(SkillsOf(piece, (JObject)spec2));
                clean.Add(piece);
            }
            if (r.Errors.Count > 0) return r;

            foreach (var p in clean)
                if ((bool?)known[p.Kind]["alone"] == true && clean.Count > 1) r.Errors.Add($"alone:{p.Kind}");
            for (int i = 0; i < clean.Count; i++)
            {
                string code = clean[i].Code;
                for (int j = 0; j < i; j++)
                    if (clean[j].Code == code) { r.Errors.Add($"duplicate:{code}"); break; }
                if (ctx.Prior.Contains(code)) r.Errors.Add($"repeated:{code}");
            }
            int tier = clean.Max(p => p.Tier);
            int cap = V4Catalog.AllowedTier(ctx.ChallengesGiven);
            if (tier > cap) r.Errors.Add($"tier_too_high:{tier}>{cap}");

            var blocked = new HashSet<string>(ctx.BlockedRoutes);
            foreach (var p in clean)
            {
                if (p.Kind != "BLOCK_ROUTE") continue;
                string route = p.Option("route");
                if (blocked.Contains(route)) r.Errors.Add($"route_already_blocked:{route}");
                blocked.Add(route);
            }
            if (blocked.Count > 0 && blocked.Count >= ((JArray)cfg["routes"]).Count) r.Errors.Add("no_exit_left");

            string reference = V4Catalog.Text(data["pccc_rule_reference"] ?? "").Trim();
            if (data["pccc_rule_reference"] == null) reference = "";
            if (!V4Catalog.KnownRule(reference)) r.Errors.Add("unknown_rule");
            string reason = data["pedagogical_reason"] == null ? "" : V4Catalog.Text(data["pedagogical_reason"]).Trim();
            if (reason.Length == 0) r.Errors.Add("missing_reason");

            r.Ok = r.Errors.Count == 0;
            if (r.Ok)
            {
                r.Pieces.AddRange(clean);
                r.Tier = tier;
                r.Rule = reference.Length > 80 ? reference.Substring(0, 80) : reference;
                r.Reason = reason.Length > MaxReasonChars ? reason.Substring(0, MaxReasonChars) : reason;
            }
            return r;
        }

        /// <summary>KIND, or KIND:option for pieces with an option parameter (each counts as its own challenge).</summary>
        private static string CodeOf(ChallengePiece p, JObject spec)
        {
            foreach (var prop in ((JObject)spec["params"]).Properties())
                if (prop.Value["options"] != null) return $"{p.Kind}:{p.Option(prop.Name)}";
            return p.Kind;
        }

        private static IEnumerable<string> SkillsOf(ChallengePiece p, JObject spec)
        {
            if (spec["option_skills"] is JObject bySkill)
                foreach (var prop in bySkill.Properties())
                    if (prop.Value[p.Option(prop.Name)] is JArray s) return s.Values<string>();
            return spec["skills"] is JArray list ? list.Values<string>() : Enumerable.Empty<string>();
        }

        /// <summary>Skills an enum directive trains (catalog "directive_skills"), for the local governor's choice.</summary>
        public static List<string> DirectiveSkills(string code) =>
            V4Catalog.Challenge["directive_skills"]?[code] is JArray s ? s.Values<string>().ToList() : new List<string>();

        private static int TierOf(ChallengePiece p, JObject spec)
        {
            if (spec["option_tiers"] is JObject optionTiers)
                foreach (var prop in optionTiers.Properties())
                    if (prop.Value[p.Option(prop.Name)] != null) return (int)prop.Value[p.Option(prop.Name)];
            return (int)spec["tier"];
        }

        private static ChallengeCheck Fail(ChallengeCheck r, string error)
        {
            r.Errors.Add(error);
            return r;
        }
    }
}
