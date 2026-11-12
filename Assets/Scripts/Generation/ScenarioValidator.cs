using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace EscapeFire.Generation
{
    /// <summary>
    /// Tier-1 rules for a generated room (plan V4.3), identical to Server/escapefire_edge/scenario.py (same error codes,
    /// same fixtures: Server/tests/fixtures/scenario_cases.json). The headset re-checks every spec it receives; the
    /// <see cref="LayoutSolver"/> then checks the real layout (every cell, fire, NPC and extinguisher reaches an exit).
    /// </summary>
    public static class ScenarioValidator
    {
        public const int TitleMax = 60;

        public static List<string> Validate(JToken token)
        {
            var errors = new List<string>();
            if (!(token is JObject spec)) { errors.Add("not_object"); return errors; }
            var cfg = V4Catalog.Scenario;

            string room = Str(spec["room_type"]).ToLowerInvariant();
            if (!(cfg["room_types"][room] is JObject roomCfg))
            {
                errors.Add($"unknown_room_type:{(room.Length > 0 ? room : "<missing>")}");
                return errors;
            }

            bool hasW = V4Catalog.TryNumber(spec["width_m"], out float w);
            bool hasD = V4Catalog.TryNumber(spec["depth_m"], out float d);
            if (!hasW || w < (float)cfg["width_m"]["min"] || w > (float)cfg["width_m"]["max"]) errors.Add("bad_width");
            if (!hasD || d < (float)cfg["depth_m"]["min"] || d > (float)cfg["depth_m"]["max"]) errors.Add("bad_depth");
            float area = (hasW ? w : 0f) * (hasD ? d : 0f);

            var walls = cfg["walls"].Values<string>().ToList();
            var exits = spec["exits"] as JArray;
            int exitCount = 0;
            if (exits == null || exits.Count == 0) errors.Add("no_exit");
            else
            {
                exitCount = exits.Count;
                if (exits.Count > (int)cfg["max_exits"]) errors.Add("too_many_exits");
                var seen = new HashSet<string>();
                foreach (var e in exits)
                {
                    string wall = e is JObject eo ? Str(eo["wall"]).ToLowerInvariant() : "";
                    bool hasPos = e is JObject eo2 && V4Catalog.TryNumber(eo2["position"], out _);
                    float pos = 0f;
                    if (hasPos) V4Catalog.TryNumber(((JObject)e)["position"], out pos);
                    if (!walls.Contains(wall) || !hasPos || pos < 0.1f || pos > 0.9f) { errors.Add("bad_exit"); continue; }
                    string key = wall + ":" + Math.Round(pos, 1, MidpointRounding.ToEven).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (!seen.Add(key)) errors.Add("duplicate_exit");
                }
            }
            if (area > (float)cfg["two_exits_above_m2"] && exitCount < 2) errors.Add("needs_two_exits");

            var allowed = roomCfg["categories"].Values<string>().ToList();
            var categories = (JObject)cfg["categories"];
            float cover = 0f;
            var furniture = spec["furniture"];
            if (furniture != null && furniture.Type != JTokenType.Null && !(furniture is JArray)) { errors.Add("bad_furniture"); furniture = null; }
            if (furniture is JArray fa)
            {
                foreach (var f in fa)
                {
                    string cat = f is JObject fo ? Str(fo["category"]).ToLowerInvariant() : "";
                    if (categories[cat] == null) { errors.Add($"unknown_category:{(cat.Length > 0 ? cat : "<missing>")}"); continue; }
                    if (!allowed.Contains(cat)) { errors.Add($"category_not_in_room:{cat}"); continue; }
                    bool hasCount = V4Catalog.TryNumber(((JObject)f)["count"], out float count);
                    if (!hasCount || count < 1 || count > 20 || count != (float)Math.Floor(count)) { errors.Add($"bad_count:{cat}"); continue; }
                    cover += (float)categories[cat]["w"] * (float)categories[cat]["d"] * count;
                }
            }
            if (area > 0 && cover > (float)cfg["max_furniture_cover"] * area) errors.Add("too_crowded");

            string fireClass = "";
            var sources = (JObject)cfg["fire_sources"];
            if (!(spec["fire"] is JObject fire)) errors.Add("no_fire");
            else
            {
                string source = Str(fire["source"]).ToLowerInvariant();
                fireClass = Str(fire["class"]).ToUpperInvariant();
                if (sources[source] == null) errors.Add($"unknown_fire_source:{(source.Length > 0 ? source : "<missing>")}");
                else if (!allowed.Contains(source)) errors.Add($"fire_source_not_in_room:{source}");
                else if (fireClass != (string)sources[source]) errors.Add($"wrong_fire_class:{source}={(string)sources[source]}");
            }

            var extCfg = (JObject)cfg["extinguishers"];
            var ext = spec["extinguishers"] as JArray;
            bool suitable = false;
            if (ext == null || ext.Count == 0) errors.Add("no_extinguisher");
            else
            {
                foreach (var x in ext)
                {
                    string kind = x is JObject xo ? Str(xo["type"]) : "";
                    if (extCfg[kind] == null) { errors.Add($"unknown_extinguisher:{(kind.Length > 0 ? kind : "<missing>")}"); continue; }
                    var countToken = ((JObject)x)["count"] ?? new JValue(1);
                    if (!V4Catalog.TryNumber(countToken, out float count) || count < 1 || count > 3) { errors.Add($"bad_count:{kind}"); continue; }
                    if (extCfg[kind]["classes"].Values<string>().Contains(fireClass)) suitable = true;
                }
                if (fireClass.Length > 0 && !suitable) errors.Add("no_suitable_extinguisher");
            }

            var npcs = spec["npcs"];
            var roles = cfg["npc_roles"].Values<string>().ToList();
            if (npcs != null && npcs.Type != JTokenType.Null)
            {
                if (!(npcs is JArray na) || na.Count > (int)cfg["max_npcs"]) errors.Add("too_many_npcs");
                else
                    foreach (var n in na)
                    {
                        string role = n is JObject no ? Str(no["role"]).ToLowerInvariant() : "";
                        if (!roles.Contains(role)) errors.Add($"unknown_npc_role:{(role.Length > 0 ? role : "<missing>")}");
                    }
            }

            var goals = spec["learning_goals"] as JArray;
            if (goals == null || goals.Count == 0) errors.Add("no_learning_goal");
            else
            {
                var rules = V4Catalog.Rules;
                foreach (var g in goals)
                    if (!rules.Contains(V4Catalog.Text(g).ToUpperInvariant())) errors.Add($"unknown_rule:{V4Catalog.Text(g)}");
            }

            var levelToken = spec["level"] ?? new JValue(1);
            if (!V4Catalog.TryNumber(levelToken, out float level) || (level != 1f && level != 2f && level != 3f)) errors.Add("bad_level");
            string title = Str(spec["title"]).Trim();
            if (title.Length == 0 || title.Length > TitleMax) errors.Add("bad_title");
            return errors;
        }

        private static string Str(JToken t) => t == null || t.Type == JTokenType.Null ? "" : V4Catalog.Text(t).Trim();
    }
}
