import asyncio
import json

from fastapi.testclient import TestClient

from escapefire_edge.aar import DebriefRequest, build_debrief_prompt, generate_debrief
from escapefire_edge.app import create_app
from escapefire_edge.llm import ScriptedLLM

GOOD_TEXT = ("Bạn ngắt Aptomat rất nhanh và chọn đúng bình CO2. Tuy nhiên bạn đứng thẳng trong khói 8 giây ở khu bàn 4, "
             "khí nóng có thể gây bỏng đường hô hấp. Lần tới, điều gì giúp bạn nhớ hạ thấp người ngay khi khói xuống?")


def req(**kw) -> DebriefRequest:
    base = dict(passed=True, total_score=90, template_debrief="TEMPLATE",
                scorecard=[{"label": "Khom lưng", "points": 12, "max_points": 20, "note": "Đứng thẳng 8 giây"}],
                standing_in_smoke_seconds=8)
    base.update(kw)
    return DebriefRequest(**base)


def test_prompt_contains_scorecard_facts():
    p = build_debrief_prompt(req())
    assert "Khom lưng: 12/20" in p and "90/100" in p


def test_llm_text_is_used_when_acceptable():
    r = asyncio.run(generate_debrief(ScriptedLLM([GOOD_TEXT]), req()))
    assert r.source == "llm" and r.debrief.startswith("Bạn ngắt")


def test_json_or_garbage_falls_back_to_template():
    for bad in ['{"directive": "X"}', "ok", "```code```" + "x" * 50, "x" * 2000]:
        r = asyncio.run(generate_debrief(ScriptedLLM([bad]), req()))
        assert r.source == "template" and r.debrief == "TEMPLATE"


class Broken:
    async def complete(self, system, user):
        raise ConnectionError()


def test_llm_down_falls_back():
    assert asyncio.run(generate_debrief(Broken(), req())).source == "template"


def test_http_endpoints(tmp_path):
    client = TestClient(create_app(ScriptedLLM([GOOD_TEXT]), log_dir=tmp_path))

    r = client.post("/aar/debrief", json=req().model_dump())
    assert r.status_code == 200 and r.json()["source"] == "llm"

    report = {"session_id": "abc123", "total_score": 90}
    r = client.post("/api/v1/sessions/abc123/complete", json=report)
    assert r.json()["status"] == "stored"
    assert json.loads((tmp_path / "lms" / "abc123.json").read_text(encoding="utf-8"))["total_score"] == 90

    assert client.post("/api/v1/sessions/..%2Fevil/complete", json=report).status_code in (400, 404)
    assert client.post("/aar/debrief", json={"passed": True, "total_score": 500}).status_code == 422
