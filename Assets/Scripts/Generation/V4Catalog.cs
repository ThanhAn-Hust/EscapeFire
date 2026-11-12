using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EscapeFire.Generation
{
    /// <summary>
    /// Shared V4 catalog (challenge pieces, room types, furniture categories, fire classes, rule ids). The edge server
    /// loads an identical copy (Server/escapefire_edge/v4_catalog.json); a server test checks both files match.
    /// </summary>
    public static class V4Catalog
    {
        private static JObject _root;

        public static JObject Root
        {
            get
            {
                if (_root == null)
                {
                    var asset = Resources.Load<TextAsset>("V4Catalog");
                    _root = JObject.Parse(asset != null ? asset.text : "{}");
                }
                return _root;
            }
        }

        /// <summary>Loads the catalog from text (tests outside Unity, or a catalog pushed by the server).</summary>
        public static void Load(string json) => _root = JObject.Parse(json);

        public static JObject Challenge => (JObject)Root["challenge"];
        public static JObject Pieces => (JObject)Challenge["pieces"];
        public static JObject Scenario => (JObject)Root["scenario"];
        public static List<string> Rules => Root["rules"].Values<string>().ToList();

        /// <summary>Same rule as the server: the n-th challenge may only use tiers 1..n.</summary>
        public static int AllowedTier(int challengesSoFar) => System.Math.Min(challengesSoFar + 1, (int)Challenge["max_tier"]);

        /// <summary>Legacy rule keys the enum director still cites.</summary>
        public static readonly string[] LegacyRuleKeys = { "NFPA 75 8.4", "NFPA 2001 / TCVN 7161", "NFPA 101", "LIFE SAFETY INVARIANT", "TCVN 3890" };

        public static bool KnownRule(string reference)
        {
            string r = (reference ?? "").Trim().ToUpperInvariant();
            if (r.Length == 0) return false;
            foreach (var rule in Rules)
                if (r == rule || r == rule.Split('_')[0] || r.StartsWith(rule, System.StringComparison.Ordinal)) return true;
            if (r.Length < 6) return false;
            foreach (var key in LegacyRuleKeys)
                if (r.Contains(key.ToUpperInvariant())) return true;
            return false;
        }

        /// <summary>Lenient number like Python float(): JSON numbers, numeric strings, booleans.</summary>
        public static bool TryNumber(JToken t, out float value)
        {
            value = 0f;
            if (t == null) return false;
            switch (t.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    value = t.Value<float>();
                    return true;
                case JTokenType.Boolean:
                    value = t.Value<bool>() ? 1f : 0f;
                    return true;
                case JTokenType.String:
                    return float.TryParse(t.Value<string>().Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out value);
                default:
                    return false;
            }
        }

        /// <summary>Python-style str() of a JSON scalar (for option parameters).</summary>
        public static string Text(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) return "None";
            if (t.Type == JTokenType.String) return t.Value<string>();
            if (t.Type == JTokenType.Boolean) return t.Value<bool>() ? "True" : "False";
            return t.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
