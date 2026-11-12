using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace EscapeFire.Generation
{
    /// <summary>A validated room spec (plan V4.1), from the edge server's /scenario/generate or a stored sample.</summary>
    public sealed class ScenarioSpec
    {
        public struct Exit { public string Wall; public float Position; }
        public struct Furniture { public string Category; public int Count; }
        public struct Extinguisher { public string Type; public int Count; }

        public string Title = "";
        public string RoomType = "";
        public float Width;
        public float Depth;
        public readonly List<Exit> Exits = new List<Exit>();
        public readonly List<Furniture> Items = new List<Furniture>();
        public string FireSource = "";
        public string FireClass = "";
        public readonly List<Extinguisher> Extinguishers = new List<Extinguisher>();
        public readonly List<string> Npcs = new List<string>();
        public readonly List<string> Goals = new List<string>();
        public int Level = 1;

        /// <summary>Validates (shared Tier-1 rules) then parses. Returns null and the errors when invalid.</summary>
        public static ScenarioSpec Parse(JToken token, out List<string> errors)
        {
            errors = ScenarioValidator.Validate(token);
            if (errors.Count > 0) return null;
            var o = (JObject)token;
            var s = new ScenarioSpec
            {
                Title = V4Catalog.Text(o["title"]).Trim(),
                RoomType = V4Catalog.Text(o["room_type"]).Trim().ToLowerInvariant(),
                FireSource = V4Catalog.Text(o["fire"]["source"]).Trim().ToLowerInvariant(),
                FireClass = V4Catalog.Text(o["fire"]["class"]).Trim().ToUpperInvariant()
            };
            V4Catalog.TryNumber(o["width_m"], out s.Width);
            V4Catalog.TryNumber(o["depth_m"], out s.Depth);
            if (o["level"] != null && V4Catalog.TryNumber(o["level"], out float level)) s.Level = (int)level;
            foreach (JObject e in o["exits"])
            {
                V4Catalog.TryNumber(e["position"], out float p);
                s.Exits.Add(new Exit { Wall = V4Catalog.Text(e["wall"]).Trim().ToLowerInvariant(), Position = p });
            }
            if (o["furniture"] is JArray fa)
                foreach (JObject f in fa)
                {
                    V4Catalog.TryNumber(f["count"], out float c);
                    s.Items.Add(new Furniture { Category = V4Catalog.Text(f["category"]).Trim().ToLowerInvariant(), Count = (int)c });
                }
            foreach (JObject x in o["extinguishers"])
            {
                float c = 1f;
                if (x["count"] != null) V4Catalog.TryNumber(x["count"], out c);
                s.Extinguishers.Add(new Extinguisher { Type = V4Catalog.Text(x["type"]).Trim(), Count = (int)c });
            }
            if (o["npcs"] is JArray na)
                foreach (JObject n in na) s.Npcs.Add(V4Catalog.Text(n["role"]).Trim().ToLowerInvariant());
            foreach (var g in o["learning_goals"]) s.Goals.Add(V4Catalog.Text(g).ToUpperInvariant());
            return s;
        }
    }
}
