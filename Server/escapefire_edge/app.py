"""FastAPI app: GET /health and WebSocket /session (one connection per headset session).

Run:  uv run python -m escapefire_edge.app            (Ollama on localhost:11434)
      uv run python -m escapefire_edge.app --fake-llm (scripted LLM, no GPU needed)
"""

from __future__ import annotations

import argparse
import json
import logging
from pathlib import Path

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from pydantic import ValidationError

from .director import Director
from .llm import LLMClient, OllamaClient, ScriptedLLM
from .schema import DirectiveResultMessage, TelemetryMessage

log = logging.getLogger("escapefire.edge")

DEMO_OUTPUTS = [
    '{"directive": "TRIGGER_CO2_PIN_JAM", "target_object": "CO2_Extinguisher", '
    '"pccc_rule_reference": "TCVN 3890", "pedagogical_reason": "Rèn phản xạ xử lý thiết bị kẹt."}',
    '{"directive": "ESCALATE_SECONDARY_FIRE", "target_object": "ChemicalTray", '
    '"pccc_rule_reference": "LIFE SAFETY INVARIANT", "pedagogical_reason": "Kiểm tra khả năng đánh giá lại tình huống."}',
    '{"directive": "ESCALATE_FM200_COUNTDOWN", "target_object": "ServerRoom", '
    '"pccc_rule_reference": "NFPA 2001 / TCVN 7161", "pedagogical_reason": "Buộc chuyển từ dập lửa sang sơ tán."}',
]


def create_app(llm: LLMClient | None = None, log_dir: Path | None = Path("logs")) -> FastAPI:
    app = FastAPI(title="EscapeFire Edge AI Director")
    director = Director(llm or OllamaClient(), log_dir=log_dir)
    app.state.director = director

    @app.get("/health")
    async def health() -> dict:
        return {"status": "ok", "llm": type(director.llm).__name__}

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
    args = parser.parse_args()

    logging.basicConfig(level=logging.INFO)
    llm: LLMClient = ScriptedLLM(DEMO_OUTPUTS) if args.fake_llm else OllamaClient()
    uvicorn.run(create_app(llm), host=args.host, port=args.port)


if __name__ == "__main__":
    main()
