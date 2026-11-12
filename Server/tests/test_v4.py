"""V4: shared catalog, Challenge Composer (Tier-1), Prompt -> ScenarioSpec, director modes."""

import asyncio
import json
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from escapefire_edge import catalog
from escapefire_edge.app import create_app
from escapefire_edge.challenge import ChallengeContext, piece_code, validate_challenge
from escapefire_edge.director import Director
from escapefire_edge.llm import ScriptedLLM
from escapefire_edge.scenario import (TEMPLATES, ScenarioRequest, generate_scenario, template_for,
                                      validate_scenario)
from escapefire_edge.schema import ChallengeMessage, DirectiveMessage, TelemetryMessage

FIX = Path(__file__).parent / "fixtures"
ROOT = Path(__file__).resolve().parents[2]


def _cases(name: str) -> list[dict]:
    return json.loads((FIX / name).read_text(encoding="utf-8"))["cases"]


# ---------------- shared catalog

def test_server_and_headset_catalogs_are_identical():
    unity = json.loads((ROOT / "Assets" / "Resources" / "V4Catalog.json").read_text(encoding="utf-8"))
    assert unity == catalog.catalog(), "copy Assets/Resources/V4Catalog.json to Server/escapefire_edge/v4_catalog.json"


def test_catalog_rules_match_the_rule_book():
    book = json.loads((ROOT / "Assets" / "Resources" / "PcccRules.json").read_text(encoding="utf-8"))
    assert [r["id"] for r in book["rules"]] == catalog.rules()
    for spec in catalog.pieces().values():
        assert spec["rule"] in catalog.rules()


def test_every_room_category_and_fire_source_is_described():
    sc = catalog.scenario()
    for room in sc["room_types"].values():
        for cat in room["categories"]:
            assert cat in sc["categories"]
    for source, cls in sc["fire_sources"].items():
        assert source in sc["categories"] and cls in sc["fire_classes"]


# ---------------- challenge composer, Tier-1 (same cases as the headset)

@pytest.mark.parametrize("case", _cases("challenge_cases.json"), ids=lambda c: c["name"])
def test_challenge_cases(case):
    ctx = ChallengeContext(**case["ctx"])
    r = validate_challenge(case["spec"] if isinstance(case["spec"], dict) else None, ctx)
    assert r.ok == case["ok"], r.errors
    if case["ok"]:
        assert r.tier == case["tier"]
    else:
        assert r.error.startswith(case["error"]), r.errors


def test_piece_codes_separate_options():
    assert piece_code({"piece": "DEVICE_FAULT", "params": {"device": "co2_pin_jam"}}) == "DEVICE_FAULT:co2_pin_jam"
    assert piece_code({"piece": "SMOKE_SURGE", "params": {"drop_m": 0.5, "speed": 1}}) == "SMOKE_SURGE"


# ---------------- scenario spec (same cases as the headset)

@pytest.mark.parametrize("case", _cases("scenario_cases.json"), ids=lambda c: c["name"])
def test_scenario_cases(case):
    errors = validate_scenario(case["spec"])
    assert (not errors) == case["ok"], errors
    if not case["ok"]:
        assert errors[0].startswith(case["error"]), errors


def test_all_templates_are_valid():
    for room, spec in TEMPLATES.items():
        assert validate_scenario(spec) == [], room


@pytest.mark.parametrize("prompt,room", [
    ("Cháy ở thư viện tầng 2, có sinh viên hoảng loạn", "library"),
    ("căng tin bị cháy bếp dầu", "canteen"),
    ("xưởng có tủ hóa chất", "workshop"),
    ("văn phòng khoa, máy in chập điện", "office"),
    ("something unrelated", "lab"),
])
def test_template_fallback_matches_the_prompt(prompt, room):
    assert template_for(prompt)["room_type"] == room


def _run(coro):
    return asyncio.run(coro)


def test_generate_uses_valid_llm_output():
    spec = dict(TEMPLATES["office"], title="Phòng giáo vụ")
    r = _run(generate_scenario(ScriptedLLM([json.dumps(spec, ensure_ascii=False)]), ScenarioRequest(prompt="phòng giáo vụ")))
    assert r.ok and r.source == "llm" and r.spec["title"] == "Phòng giáo vụ" and r.attempts == 1


def test_generate_repairs_once_with_the_validator_errors():
    bad = dict(TEMPLATES["lab"], extinguishers=[{"type": "WaterFoam", "count": 1}])  # water on electrical
    good = TEMPLATES["lab"]
    llm = ScriptedLLM([json.dumps(bad), json.dumps(good)])
    r = _run(generate_scenario(llm, ScenarioRequest(prompt="phòng máy")))
    assert r.ok and r.source == "llm_repaired" and r.attempts == 2


def test_generate_falls_back_to_template_after_two_bad_answers():
    llm = ScriptedLLM(["not json", '{"room_type": "spaceship"}'])
    r = _run(generate_scenario(llm, ScenarioRequest(prompt="thư viện cháy", level=2)))
    assert r.ok and r.source == "template" and r.spec["room_type"] == "library" and r.spec["level"] == 2
    assert r.errors and r.errors[0].startswith("unknown_room_type")


def test_scenario_endpoint():
    client = TestClient(create_app(ScriptedLLM([json.dumps(TEMPLATES["canteen"], ensure_ascii=False)]), log_dir=None))
    r = client.post("/scenario/generate", json={"prompt": "căng tin cháy bếp"})
    assert r.status_code == 200 and r.json()["spec"]["room_type"] == "canteen"


# ---------------- director modes

def calm(**overrides) -> TelemetryMessage:
    base = dict(session_id="s1", time_elapsed=60, cognitive_friction_cf=0.1,
                phase="Phase2_ServerExplosionFire", emergency_elapsed=30)
    base.update(overrides)
    return TelemetryMessage(**base)


COMPOSED = json.dumps({
    "pieces": [{"piece": "LIGHTS_OFF", "params": {"zone": "corridor"}, "delay_s": 0},
               {"piece": "SMOKE_SURGE", "params": {"drop_m": 0.5, "speed": 1.5}, "delay_s": 6}],
    "pccc_rule_reference": "R07_STAY_LOW", "pedagogical_reason": "Tối và khói thấp: lần tường, giữ thấp."})


def test_rules_mode_needs_no_llm_and_picks_the_easiest():
    llm = ScriptedLLM(["{}"])
    d = _run(Director(llm, mode="rules").decide(calm()))
    assert isinstance(d, DirectiveMessage) and d.source == "rule" and d.directive == "TRIGGER_CO2_PIN_JAM"
    assert llm.calls == 0


def test_composer_mode_returns_a_challenge():
    d = _run(Director(ScriptedLLM([COMPOSED]), mode="composer").decide(calm(accepted_challenges=1)))
    assert isinstance(d, ChallengeMessage) and len(d.pieces) == 2 and d.tier == 2


def test_composer_invalid_output_falls_back_to_maintain():
    d = _run(Director(ScriptedLLM([COMPOSED]), mode="composer").decide(calm(accepted_challenges=0)))  # tier 2 > 1
    assert isinstance(d, DirectiveMessage) and d.source == "fallback" and d.directive == "MAINTAIN_CURRENT_PACE"


def test_composer_still_obeys_the_panic_guard():
    llm = ScriptedLLM([COMPOSED])
    d = _run(Director(llm, mode="composer").decide(calm(cognitive_friction_cf=0.9)))
    assert d.source == "rule" and llm.calls == 0


def test_composer_over_websocket():
    app = create_app(ScriptedLLM([COMPOSED]), log_dir=None, director_mode="composer", min_observation_s=0)
    with TestClient(app).websocket_connect("/session") as ws:
        ws.send_text(calm(accepted_challenges=1).model_dump_json())
        msg = ws.receive_json()
    assert msg["type"] == "challenge" and msg["pieces"][0]["piece"] == "LIGHTS_OFF"


# ---------------- VL.3: challenges know which PCCC skills they train

from escapefire_edge.challenge import build_composer_prompt, directive_skills, piece_skills  # noqa: E402


def test_every_piece_and_directive_trains_known_skills():
    for kind, spec in catalog.pieces().items():
        assert spec.get("skills"), kind
        for s in spec["skills"]:
            assert s in catalog.rules()
    for d in ("TRIGGER_CO2_PIN_JAM", "TRIGGER_SMOKE_SURGE", "ESCALATE_FM200_COUNTDOWN"):
        assert directive_skills(d) and all(s in catalog.rules() for s in directive_skills(d))


def test_option_specific_skills():
    assert piece_skills({"piece": "DEVICE_FAULT", "params": {"device": "alarm_cable_cut"}}) == ["R01_ALARM_EARLY"]
    assert "R06_EVACUATE_IF_EMPTY" in piece_skills({"piece": "DEVICE_FAULT", "params": {"device": "co2_low_pressure"}})


def test_composer_reports_whether_it_targets_a_weak_skill():
    hit = _run(Director(ScriptedLLM([COMPOSED]), mode="composer").decide(calm(accepted_challenges=1, weak_skills=["R07_STAY_LOW"])))
    miss = _run(Director(ScriptedLLM([COMPOSED]), mode="composer").decide(calm(accepted_challenges=1, weak_skills=["R08_DOOR_BACK_OF_HAND"])))
    assert hit.targets_weak_skill and "R07_STAY_LOW" in hit.skills
    assert not miss.targets_weak_skill


def test_weak_skills_reach_the_prompt_and_cf_rules_still_apply():
    t = calm(weak_skills=["R08_DOOR_BACK_OF_HAND"])
    prompt = build_composer_prompt(t, ChallengeContext(weak_skills=t.weak_skills))
    assert "R08_DOOR_BACK_OF_HAND" in prompt
    llm = ScriptedLLM([COMPOSED])
    d = _run(Director(llm, mode="composer").decide(calm(cognitive_friction_cf=0.5, weak_skills=["R07_STAY_LOW"])))
    assert d.source == "rule" and llm.calls == 0, "CF still decides when to add a challenge"


# ---------------- VL.4: one quiz item per rule (paper / online pre-post-retention test)

def test_every_rule_has_a_quiz_item():
    book = json.loads((ROOT / "Assets" / "Resources" / "PcccRules.json").read_text(encoding="utf-8"))
    for r in book["rules"]:
        q = r["quiz"]
        assert q["question"].strip() and len(q["options"]) == 3 and 0 <= q["answer"] < 3 and q["why"].strip(), r["id"]
        assert len(set(q["options"])) == 3, r["id"]
