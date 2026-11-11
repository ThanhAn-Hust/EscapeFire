using System;
using System.IO;
using NUnit.Framework;
using EscapeFire.Analytics;
using EscapeFire.Scenario;

namespace EscapeFire.Tests.EditMode
{
    /// <summary>V1.2 — every score, Socratic line and AI-cited rule maps to a sourced PCCC rule.</summary>
    public class PcccRuleBookTests
    {
        private static void Load() => PcccRuleBook.Load(File.ReadAllText(Path.Combine("Assets", "Resources", "PcccRules.json")));

        [TestCase("score:alarm")]
        [TestCase("score:power_cut")]
        [TestCase("score:agent")]
        [TestCase("score:technique")]
        [TestCase("score:crouch")]
        [TestCase("score:rescue")]
        [TestCase("directive:NFPA 75 8.4")]
        [TestCase("directive:NFPA 2001 / TCVN 7161")]
        [TestCase("directive:NFPA 101")]
        public void EveryScoreAndCitedRule_HasASource(string usage)
        {
            Load();
            Assert.IsNotNull(PcccRuleBook.For(usage), $"no PCCC rule backs '{usage}'");
        }

        [Test]
        public void EverySocraticTrigger_HasASource()
        {
            Load();
            foreach (SocraticTrigger t in Enum.GetValues(typeof(SocraticTrigger)))
            {
                if (t == SocraticTrigger.None) continue;
                Assert.IsNotNull(PcccRuleBook.For("socratic:" + t), $"Socratic line {t} has no rule");
            }
        }

        [Test]
        public void EveryRule_HasAWebSource_AndUniqueId()
        {
            Load();
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var r in PcccRuleBook.Rules)
            {
                Assert.IsTrue(ids.Add(r.Id), $"duplicate rule id {r.Id}");
                Assert.IsNotEmpty(r.Sources, $"{r.Id} has no source");
                foreach (var s in r.Sources) StringAssert.StartsWith("http", s.Url, $"{r.Id}: source without URL");
            }
            Assert.GreaterOrEqual(ids.Count, 12);
        }
    }
}
