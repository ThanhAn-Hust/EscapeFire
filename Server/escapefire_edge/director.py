"""Macro AI Director: deterministic guards first, then LLM + Tier-1 validation, always with a safe fallback."""

from __future__ import annotations

import asyncio
import json
import time
from pathlib import Path

from .llm import LLMClient
from .prompt import SYSTEM_PROMPT, build_user_prompt
from .schema import (CALM_THRESHOLD, MAINTAIN, MIN_OBSERVATION_SECONDS, PANIC_THRESHOLD, DirectiveMessage,
                     TelemetryMessage)
from .validator import validate_llm_output

EMERGENCY_PHASES = ("Phase2_ServerExplosionFire", "Phase3_SmokeEvacuation")


class Director:
    def __init__(self, llm: LLMClient, timeout_s: float = 6.0, log_dir: Path | None = None,
                 min_observation_s: float = MIN_OBSERVATION_SECONDS) -> None:
        self.llm = llm
        self.min_observation_s = min_observation_s
        self.timeout_s = timeout_s
        self.log_dir = log_dir
        if log_dir is not None:
            log_dir.mkdir(parents=True, exist_ok=True)

    async def decide(self, telemetry: TelemetryMessage) -> DirectiveMessage:
        started = time.perf_counter()

        # Deterministic guards (no LLM call): never escalate panicking learners or outside the emergency.
        if telemetry.cognitive_friction_cf >= PANIC_THRESHOLD:
            return self._finish(telemetry, started, DirectiveMessage(
                directive=MAINTAIN, source="rule",
                pedagogical_reason="Người học đang hoảng loạn (CF cao): giữ nguyên nhịp độ."), raw=None, error="")
        if telemetry.phase not in EMERGENCY_PHASES:
            return self._finish(telemetry, started, DirectiveMessage(
                directive=MAINTAIN, source="rule",
                pedagogical_reason="Chưa ở giai đoạn sự cố."), raw=None, error="")
        if telemetry.emergency_elapsed < self.min_observation_s:
            return self._finish(telemetry, started, DirectiveMessage(
                directive=MAINTAIN, source="rule",
                pedagogical_reason="Đang quan sát phản ứng ban đầu của người học."), raw=None, error="")
        if telemetry.cognitive_friction_cf > CALM_THRESHOLD:
            return self._finish(telemetry, started, DirectiveMessage(
                directive=MAINTAIN, source="rule",
                pedagogical_reason="Người học đang ở vùng phát triển gần (CF trung bình)."), raw=None, error="")

        raw: str | None = None
        try:
            raw = await asyncio.wait_for(
                self.llm.complete(SYSTEM_PROMPT, build_user_prompt(telemetry)), timeout=self.timeout_s)
        except asyncio.TimeoutError:
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=None, error="llm_timeout")
        except Exception as exc:  # network down, Ollama not running, ...
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=None,
                                error=f"llm_error:{type(exc).__name__}")

        tier1 = validate_llm_output(raw)
        if not tier1.ok:
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=raw, error=tier1.error)

        if tier1.directive in telemetry.prior_challenges:
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=raw,
                                error="repeated_challenge")

        return self._finish(telemetry, started, DirectiveMessage(
            directive=tier1.directive,
            target_object=tier1.target_object,
            pccc_rule_reference=tier1.pccc_rule_reference,
            pedagogical_reason=tier1.pedagogical_reason,
            source="llm"), raw=raw, error="")

    def _finish(self, telemetry: TelemetryMessage, started: float, message: DirectiveMessage,
                raw: str | None, error: str) -> DirectiveMessage:
        message.latency_ms = round((time.perf_counter() - started) * 1000.0, 1)
        self._log({
            "ts": time.time(),
            "session_id": telemetry.session_id,
            "telemetry": telemetry.model_dump(),
            "raw_llm_output": raw,
            "tier1_error": error,
            "decision": message.model_dump(),
        })
        return message

    def log_result(self, session_id: str, directive: str, result: str) -> None:
        self._log({"ts": time.time(), "session_id": session_id, "tier2": {"directive": directive, "result": result}})

    def _log(self, record: dict) -> None:
        if self.log_dir is None:
            return
        day = time.strftime("%Y%m%d")
        with open(self.log_dir / f"director_{day}.jsonl", "a", encoding="utf-8") as fh:
            fh.write(json.dumps(record, ensure_ascii=False) + "\n")
