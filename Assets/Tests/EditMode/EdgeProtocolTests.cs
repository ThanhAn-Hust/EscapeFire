using NUnit.Framework;
using Newtonsoft.Json.Linq;
using EscapeFire.Scenario;

namespace EscapeFire.Tests
{
    public class EdgeProtocolTests
    {
        [Test]
        public void SerializeTelemetry_UsesServerFieldNamesAndClampsCf()
        {
            var payload = new TelemetryPayload { SessionId = "abc", TimeElapsed = 12f, CognitiveFriction = 1.7f, IsPowerCut = true };
            payload.PriorChallenges.Add("TRIGGER_CO2_PIN_JAM");
            payload.AcceptedChallenges = 1;

            var obj = JObject.Parse(EdgeProtocol.SerializeTelemetry(payload));

            Assert.AreEqual("telemetry", (string)obj["type"]);
            Assert.AreEqual("abc", (string)obj["session_id"]);
            Assert.AreEqual(1f, (float)obj["cognitive_friction_cf"], 1e-5f);
            Assert.IsTrue((bool)obj["is_power_cut"]);
            Assert.AreEqual("TRIGGER_CO2_PIN_JAM", (string)obj["prior_challenges"][0]);
            Assert.AreEqual(1, (int)obj["accepted_challenges"]);
        }

        [Test]
        public void TryParseDirective_ReadsServerReply()
        {
            const string json = "{\"type\":\"directive\",\"directive\":\"ESCALATE_FM200_COUNTDOWN\"," +
                                "\"pccc_rule_reference\":\"NFPA 2001\",\"source\":\"llm\",\"latency_ms\":420.5}";

            Assert.IsTrue(EdgeProtocol.TryParseDirective(json, out var reply));
            Assert.AreEqual("ESCALATE_FM200_COUNTDOWN", reply.Directive);
            Assert.AreEqual("llm", reply.Source);
            Assert.AreEqual(420.5f, reply.LatencyMs, 1e-3f);
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"type\":\"error\",\"error\":\"schema\"}")]
        [TestCase("{\"type\":\"directive\"}")]
        public void TryParseDirective_RejectsBadMessages(string json)
        {
            Assert.IsFalse(EdgeProtocol.TryParseDirective(json, out _));
        }

        [Test]
        public void SerializeResult_HasTypeAndResult()
        {
            var obj = JObject.Parse(EdgeProtocol.SerializeResult("TRIGGER_CO2_PIN_JAM", DirectiveResult.RejectedAlreadyUnpinned));
            Assert.AreEqual("directive_result", (string)obj["type"]);
            Assert.AreEqual("RejectedAlreadyUnpinned", (string)obj["result"]);
        }
    }
}
