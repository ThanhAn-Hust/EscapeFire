using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using EscapeFire.Generation;
using EscapeFire.Scenario;

namespace EscapeFire.Tests
{
    /// <summary>
    /// V4: the headset's Tier-1 validators must agree with the edge server's on the shared fixtures
    /// (Server/tests/fixtures/*.json, also run by pytest), so the two tiers never disagree about what is safe.
    /// </summary>
    public class V4ParityTests
    {
        /// <summary>Finds a repo file from the Unity project root, the current directory or the test assembly folder.</summary>
        private static string RepoFile(string relative)
        {
            var starts = new List<string> { Directory.GetCurrentDirectory(), System.AppContext.BaseDirectory };
            foreach (var start in starts)
            {
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    string p = Path.Combine(dir.FullName, relative);
                    if (File.Exists(p)) return p;
                }
            }
            Assert.Fail("not found: " + relative);
            return null;
        }

        private static JArray Cases(string file)
        {
            V4Catalog.Load(File.ReadAllText(RepoFile("Assets/Resources/V4Catalog.json")));
            return (JArray)JObject.Parse(File.ReadAllText(RepoFile("Server/tests/fixtures/" + file)))["cases"];
        }

        [Test]
        public void ChallengeValidator_MatchesServerOnSharedCases()
        {
            var failures = new List<string>();
            foreach (JObject c in Cases("challenge_cases.json"))
            {
                var ctxJ = (JObject)c["ctx"];
                var ctx = new ChallengeContext
                {
                    ChallengesGiven = (int?)ctxJ["challenges_given"] ?? 0,
                    Cf = (float?)ctxJ["cf"] ?? 0f,
                    Prior = ctxJ["prior"] != null ? ctxJ["prior"].ToObject<List<string>>() : new List<string>(),
                    BlockedRoutes = ctxJ["blocked_routes"] != null ? ctxJ["blocked_routes"].ToObject<List<string>>() : new List<string>()
                };
                var r = ChallengeValidator.Validate(c["spec"], ctx);
                bool ok = (bool)c["ok"];
                if (r.Ok != ok || (!ok && !r.Error.StartsWith((string)c["error"])) || (ok && r.Tier != (int)c["tier"]))
                    failures.Add($"{c["name"]}: ok={r.Ok} tier={r.Tier} errors={string.Join(",", r.Errors)}");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void ScenarioValidator_MatchesServerOnSharedCases()
        {
            var failures = new List<string>();
            foreach (JObject c in Cases("scenario_cases.json"))
            {
                var errors = ScenarioValidator.Validate(c["spec"]);
                bool ok = (bool)c["ok"];
                if ((errors.Count == 0) != ok || (!ok && !errors[0].StartsWith((string)c["error"])))
                    failures.Add($"{c["name"]}: {string.Join(",", errors)}");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void ChallengeMessage_IsParsed_AndDirectivesAreNot()
        {
            Assert.IsTrue(EdgeProtocol.TryParseChallenge("{\"type\":\"challenge\",\"pieces\":[]}", out var c));
            Assert.AreEqual("challenge", (string)c["type"]);
            Assert.IsFalse(EdgeProtocol.TryParseChallenge("{\"type\":\"directive\",\"directive\":\"TRIGGER_BLACKOUT\"}", out _));
            Assert.IsFalse(EdgeProtocol.TryParseChallenge("not json", out _));
        }
    }
}
