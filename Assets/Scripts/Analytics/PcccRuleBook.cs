using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EscapeFire.Analytics
{
    [Serializable]
    public sealed class PcccSource
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("url")] public string Url;
    }

    [Serializable]
    public sealed class PcccRule
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("title")] public string Title;
        [JsonProperty("statement")] public string Statement;
        [JsonProperty("usedBy")] public List<string> UsedBy = new List<string>();
        [JsonProperty("sources")] public List<PcccSource> Sources = new List<PcccSource>();
        [JsonProperty("values")] public Dictionary<string, float> Values = new Dictionary<string, float>();

        /// <summary>Short citation for the AAR: "R04 Kỹ thuật PASS — OSHA eTool …".</summary>
        public string Citation => Sources.Count > 0 ? $"{Title} — {Sources[0].Name}" : Title;
    }

    /// <summary>
    /// Ground truth for every procedure step, score and Mentor line (plan V1.1–V1.2). Loaded from
    /// Resources/PcccRules.json; see docs/pccc_ground_truth.md for the human-readable version.
    /// </summary>
    public static class PcccRuleBook
    {
        private sealed class Root { [JsonProperty("rules")] public List<PcccRule> Rules = new List<PcccRule>(); }

        private static List<PcccRule> _rules;

        public static IReadOnlyList<PcccRule> Rules
        {
            get
            {
                if (_rules == null)
                {
                    var asset = Resources.Load<TextAsset>("PcccRules");
                    Load(asset != null ? asset.text : null);
                }
                return _rules;
            }
        }

        public static void Load(string json)
        {
            _rules = string.IsNullOrEmpty(json) ? new List<PcccRule>() : JsonConvert.DeserializeObject<Root>(json).Rules;
        }

        public static PcccRule Get(string id)
        {
            foreach (var r in Rules) if (r.Id == id) return r;
            return null;
        }

        /// <summary>The rule backing a usage key such as "score:alarm" or "socratic:StandingInSmoke".</summary>
        public static PcccRule For(string usage)
        {
            foreach (var r in Rules) if (r.UsedBy.Contains(usage)) return r;
            return null;
        }
    }
}
