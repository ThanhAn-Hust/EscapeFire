import asyncio
import json

import pytest
from fastapi.testclient import TestClient

from escapefire_edge.app import create_app
from escapefire_edge.director import Director
from escapefire_edge.llm import ScriptedLLM
from escapefire_edge.prompt import SYSTEM_PROMPT, build_user_prompt
from escapefire_edge.schema import ALLOWED_DIRECTIVES, MAINTAIN, TelemetryMessage
from escapefire_edge.validator import validate_llm_output

GOOD = ('{"directive": "ESCALATE_SECONDARY_FIRE", "target_object": "ChemicalTray", '
        '"pccc_rule_reference": "LIFE SAFETY INVARIANT", "pedagogical_reason": "test"}')


def calm(**overrides) -> TelemetryMessage:
    base = dict(session_id="s1", time_elapsed=60, cognitive_friction_cf=0.1,
                phase="Phase2_ServerExplosionFire", emergency_elapsed=30, prior_challenges=[])
    base.update(overrides)
    return TelemetryMessage(**base)


# ---------------- Tier-1 validator

def test_valid_output_passes():
    r = validate_llm_output(GOOD)
    assert r.ok and r.directive == "ESCALATE_SECONDARY_FIRE"


def test_json_inside_fences_and_chatter_is_extracted():
    r = validate_llm_output("Sure!\n```json\n" + GOOD + "\n```")
    assert r.ok


@pytest.mark.parametrize("raw,error", [
    ("", "empty_output"),
    ("not json at all", "invalid_json"),
    ('{"directive": "SET_PLAYER_ON_FIRE", "pccc_rule_reference": "NFPA 101"}', "directive_not_whitelisted"),
    ('{"directive": "ESCALATE_FM200_COUNTDOWN", "pccc_rule_reference": "made up rule"}', "missing_or_unknown_pccc_rule"),
    ('{"directive": "ESCALATE_FM200_COUNTDOWN", "pccc_rule_reference": "A"}', "missing_or_unknown_pccc_rule"),
])
def test_invalid_outputs_fall_back_to_maintain(raw, error):
    r = validate_llm_output(raw)
    assert not r.ok
    assert r.directive == MAINTAIN
    assert r.error.startswith(error)


def test_maintain_does_not_need_rule():
    assert validate_llm_output('{"directive": "maintain_current_pace"}').ok


# ---------------- prompt

def test_prompt_lists_whitelist_and_telemetry():
    for d in ALLOWED_DIRECTIVES:
        assert d in SYSTEM_PROMPT
    user = build_user_prompt(calm(prior_challenges=["TRIGGER_CO2_PIN_JAM"]))
    assert "0.10" in user and "TRIGGER_CO2_PIN_JAM" in user


# ---------------- director

def run(coro):
    return asyncio.run(coro)


def test_panic_never_calls_llm():
    llm = ScriptedLLM([GOOD])
    d = run(Director(llm, log_dir=None).decide(calm(cognitive_friction_cf=0.9)))
    assert d.directive == MAINTAIN and d.source == "rule" and llm.calls == 0


def test_outside_emergency_never_calls_llm():
    llm = ScriptedLLM([GOOD])
    d = run(Director(llm, log_dir=None).decide(calm(phase="Phase0_PeacefulPractice")))
    assert d.directive == MAINTAIN and llm.calls == 0


def test_no_escalation_during_initial_observation_window():
    llm = ScriptedLLM([GOOD])
    d = run(Director(llm, log_dir=None).decide(calm(emergency_elapsed=3)))
    assert d.directive == MAINTAIN and d.source == "rule" and llm.calls == 0


def test_calm_learner_gets_validated_llm_directive():
    d = run(Director(ScriptedLLM([GOOD]), log_dir=None).decide(calm()))
    assert d.directive == "ESCALATE_SECONDARY_FIRE" and d.source == "llm"


def test_repeated_challenge_rejected():
    d = run(Director(ScriptedLLM([GOOD]), log_dir=None).decide(calm(prior_challenges=["ESCALATE_SECONDARY_FIRE"])))
    assert d.directive == MAINTAIN and d.source == "fallback"


class SlowLLM:
    async def complete(self, system, user):
        await asyncio.sleep(5)
        return GOOD


class BrokenLLM:
    async def complete(self, system, user):
        raise ConnectionError("ollama down")


def test_timeout_falls_back():
    d = run(Director(SlowLLM(), timeout_s=0.05, log_dir=None).decide(calm()))
    assert d.directive == MAINTAIN and d.source == "fallback"


def test_llm_error_falls_back():
    d = run(Director(BrokenLLM(), log_dir=None).decide(calm()))
    assert d.directive == MAINTAIN and d.source == "fallback"


def test_decisions_are_logged(tmp_path):
    run(Director(ScriptedLLM([GOOD]), log_dir=tmp_path).decide(calm()))
    lines = next(tmp_path.glob("director_*.jsonl")).read_text(encoding="utf-8").splitlines()
    record = json.loads(lines[0])
    assert record["decision"]["directive"] == "ESCALATE_SECONDARY_FIRE"
    assert record["raw_llm_output"] == GOOD


# ---------------- websocket

def test_websocket_roundtrip(tmp_path):
    client = TestClient(create_app(ScriptedLLM([GOOD]), log_dir=tmp_path))
    assert client.get("/health").json()["status"] == "ok"
    with client.websocket_connect("/session") as ws:
        ws.send_text(calm().model_dump_json())
        reply = ws.receive_json()
        assert reply["type"] == "directive" and reply["directive"] == "ESCALATE_SECONDARY_FIRE"

        ws.send_text(json.dumps({"type": "directive_result", "directive": "ESCALATE_SECONDARY_FIRE",
                                 "result": "Accepted"}))
        ws.send_text("garbage")
        assert ws.receive_json()["error"] == "invalid_json"

        ws.send_text(json.dumps({"type": "telemetry", "time_elapsed": 1, "cognitive_friction_cf": 7}))
        assert ws.receive_json()["error"] == "schema"
