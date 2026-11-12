"""FastAPI app: GET /health and WebSocket /session (one connection per headset session).

Run:  uv run python -m escapefire_edge.app            (Ollama on localhost:11434)
      uv run python -m escapefire_edge.app --fake-llm (scripted LLM, no GPU needed)
"""

from __future__ import annotations

import argparse
import json
import logging
from pathlib import Path

import re

from fastapi import FastAPI, HTTPException, Request, WebSocket, WebSocketDisconnect
from pydantic import ValidationError

from .aar import DebriefRequest, DebriefResponse, generate_debrief
from .director import MODES, Director
from .scenario import ScenarioRequest, ScenarioResponse, generate_scenario
from .llm import LLMClient, OllamaClient, ScriptedLLM
from .schema import DirectiveResultMessage, TelemetryMessage

log = logging.getLogger("escapefire.edge")
_SAFE_ID = re.compile(r"[A-Za-z0-9-]{1,64}")

DEMO_OUTPUTS = [  # one challenge per tier, in order (scripted stand-in for the LLM)
    '{"directive": "TRIGGER_CO2_PIN_JAM", "target_object": "CO2_Extinguisher", '
    '"pccc_rule_reference": "TCVN 3890", "pedagogical_reason": "Rèn phản xạ xử lý thiết bị kẹt."}',
    '{"directive": "TRIGGER_BLACKOUT", "target_object": "LabLighting", '
    '"pccc_rule_reference": "NFPA 101", "pedagogical_reason": "Định hướng theo đèn khẩn cấp và biển EXIT."}',
    '{"directive": "ESCALATE_SECONDARY_FIRE", "target_object": "ChemicalTray", '
    '"pccc_rule_reference": "LIFE SAFETY INVARIANT", "pedagogical_reason": "Kiểm tra khả năng đánh giá lại tình huống."}',
    '{"directive": "ESCALATE_FM200_COUNTDOWN", "target_object": "ServerRoom", '
    '"pccc_rule_reference": "NFPA 2001 / TCVN 7161", "pedagogical_reason": "Buộc chuyển từ dập lửa sang sơ tán."}',
]


def create_app(llm: LLMClient | None = None, log_dir: Path | None = Path("logs"),
               min_observation_s: float | None = None, llm_timeout_s: float = 6.0,
               debrief_timeout_s: float = 8.0, director_mode: str = "enum",
               scenario_timeout_s: float = 30.0) -> FastAPI:
    app = FastAPI(title="EscapeFire Edge AI Director")
    director = Director(llm or OllamaClient(), log_dir=log_dir, timeout_s=llm_timeout_s, mode=director_mode,
                        **({} if min_observation_s is None else {"min_observation_s": min_observation_s}))
    app.state.director = director

    @app.get("/health")
    async def health() -> dict:
        return {"status": "ok", "llm": type(director.llm).__name__, "director_mode": director.mode}

    @app.post("/scenario/generate", response_model=ScenarioResponse)
    async def scenario_generate(req: ScenarioRequest) -> ScenarioResponse:
        """V4: teacher prompt -> validated ScenarioSpec (LLM, one repair round, template fallback)."""
        return await generate_scenario(director.llm, req, timeout_s=scenario_timeout_s)

    @app.post("/aar/debrief", response_model=DebriefResponse)
    async def aar_debrief(req: DebriefRequest) -> DebriefResponse:
        return await generate_debrief(director.llm, req, timeout_s=debrief_timeout_s)

    # Local mock of the LMS (daotao.ai) contract so demos work without the real LMS.
    @app.post("/api/v1/sessions/{session_id}/complete")
    async def lms_complete(session_id: str, request: Request) -> dict:
        if not _SAFE_ID.fullmatch(session_id):
            raise HTTPException(status_code=400, detail="invalid session id")
        try:
            report = await request.json()
        except json.JSONDecodeError:
            raise HTTPException(status_code=400, detail="invalid json")
        if log_dir is not None:
            lms_dir = log_dir / "lms"
            lms_dir.mkdir(parents=True, exist_ok=True)
            (lms_dir / f"{session_id}.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
        return {"status": "stored", "session_id": session_id}

    @app.websocket("/session")
    async def session(ws: WebSocket) -> None:
        await ws.accept()
        session_id = ""
        try:
            while True:
                text = await ws.receive_text()
                try:
                    data = json.loads(text)
                except json.JSONDecodeError:
                    await ws.send_json({"type": "error", "error": "invalid_json"})
                    continue

                kind = data.get("type")
                try:
                    if kind == "telemetry":
                        telemetry = TelemetryMessage.model_validate(data)
                        session_id = telemetry.session_id or session_id
                        decision = await director.decide(telemetry)
                        await ws.send_text(decision.model_dump_json())
                    elif kind == "directive_result":
                        result = DirectiveResultMessage.model_validate(data)
                        director.log_result(session_id, result.directive, result.result)
                    else:
                        await ws.send_json({"type": "error", "error": f"unknown_type:{kind}"})
                except ValidationError as exc:
                    await ws.send_json({"type": "error", "error": "schema", "detail": exc.errors()[:3]})
        except WebSocketDisconnect:
            log.info("headset disconnected (session %s)", session_id)

    return app


def main() -> None:
    import uvicorn

    parser = argparse.ArgumentParser(description="EscapeFire edge AI Director")
    parser.add_argument("--host", default="0.0.0.0", help="0.0.0.0 so the Quest 2 can reach it over LAN")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--fake-llm", action="store_true", help="use scripted directives instead of Ollama")
    parser.add_argument("--min-observation", type=float, default=None,
                        help="seconds of emergency to observe before any escalation (default 15)")
    parser.add_argument("--llm-timeout", type=float, default=6.0,
                        help="seconds the director waits for the LLM before falling back (default 6)")
    parser.add_argument("--director-mode", choices=MODES, default="enum",
                        help="rules = fixed if-else, enum = LLM picks one of 7 (baseline), composer = LLM composes pieces (V4)")
    parser.add_argument("--debrief-timeout", type=float, default=8.0,
                        help="seconds the AAR debrief waits for the LLM before using the template (default 8)")
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO)
    http_timeout = max(args.llm_timeout, args.debrief_timeout) + 2.0
    llm: LLMClient = ScriptedLLM(DEMO_OUTPUTS) if args.fake_llm else OllamaClient(timeout_s=http_timeout)
    uvicorn.run(create_app(llm, min_observation_s=args.min_observation, llm_timeout_s=args.llm_timeout,
                           debrief_timeout_s=args.debrief_timeout, director_mode=args.director_mode),
                host=args.host, port=args.port)


if __name__ == "__main__":
    main()
