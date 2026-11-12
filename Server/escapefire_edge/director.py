"""Macro AI Director: deterministic guards first, then LLM + Tier-1 validation, always with a safe fallback."""

from __future__ import annotations

import asyncio
import json
import time
from pathlib import Path

from .challenge import COMPOSER_SYSTEM, ChallengeContext, build_composer_prompt, extract_json, validate_challenge
from .llm import LLMClient
from .prompt import SYSTEM_PROMPT, build_user_prompt, remaining_choices
from .schema import (CALM_THRESHOLD, MAINTAIN, MIN_OBSERVATION_SECONDS, PANIC_THRESHOLD, ChallengeMessage,
                     DirectiveMessage, TelemetryMessage)
from .validator import validate_llm_output

EMERGENCY_PHASES = ("Phase2_ServerExplosionFire", "Phase3_SmokeEvacuation")
# Director modes (plan V4.5c / V4.7 ablation): hard-coded rules, LLM picks one enum (baseline), LLM composes pieces.
MODES = ("rules", "enum", "composer")


class Director:
    def __init__(self, llm: LLMClient, timeout_s: float = 6.0, log_dir: Path | None = None,
                 min_observation_s: float = MIN_OBSERVATION_SECONDS, mode: str = "enum") -> None:
        if mode not in MODES:
            raise ValueError(f"mode must be one of {MODES}")
        self.mode = mode
        self.llm = llm
        self.min_observation_s = min_observation_s
        self.timeout_s = timeout_s
        self.log_dir = log_dir
        if log_dir is not None:
            log_dir.mkdir(parents=True, exist_ok=True)

    async def decide(self, telemetry: TelemetryMessage) -> DirectiveMessage | ChallengeMessage:
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

        if self.mode == "rules":
            # Baseline: the easiest challenge still allowed, no LLM (what an if-else director would do).
            choices = remaining_choices(telemetry.prior_challenges, telemetry.challenges_given)
            if choices:
                message = DirectiveMessage(directive=choices[0], source="rule",
                                           pedagogical_reason="Luật cố định: thử thách dễ nhất còn lại.")
            else:
                message = DirectiveMessage(source="rule", pedagogical_reason="Đã dùng hết thử thách.")
            return self._finish(telemetry, started, message, raw=None, error="")
        if self.mode == "composer":
            return await self._compose(telemetry, started)

        raw: str | None = None
        try:
            raw = await asyncio.wait_for(
                self.llm.complete(SYSTEM_PROMPT, build_user_prompt(telemetry)), timeout=self.timeout_s)
        except asyncio.TimeoutError:
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=None, error="llm_timeout")
        except Exception as exc:  # network down, Ollama not running, ...
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=None,
                                error=f"llm_error:{type(exc).__name__}")

        tier1 = validate_llm_output(raw, telemetry.prior_challenges, telemetry.challenges_given)
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

    async def _compose(self, telemetry: TelemetryMessage, started: float) -> DirectiveMessage | ChallengeMessage:
        ctx = ChallengeContext(challenges_given=telemetry.challenges_given, cf=telemetry.cognitive_friction_cf,
                               prior=list(telemetry.prior_pieces), blocked_routes=list(telemetry.blocked_routes),
                               weak_skills=list(telemetry.weak_skills))
        try:
            raw = await asyncio.wait_for(
                self.llm.complete(COMPOSER_SYSTEM, build_composer_prompt(telemetry, ctx)), timeout=self.timeout_s)
        except asyncio.TimeoutError:
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=None, error="llm_timeout")
        except Exception as exc:
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=None,
                                error=f"llm_error:{type(exc).__name__}")
        result = validate_challenge(extract_json(raw), ctx)
        if not result.ok:
            return self._finish(telemetry, started, DirectiveMessage(source="fallback"), raw=raw,
                                error="composer:" + ";".join(result.errors[:4]))
        return self._finish(telemetry, started, ChallengeMessage(
            pieces=result.pieces, tier=result.tier, skills=result.skills, targets_weak_skill=result.targets(ctx.weak_skills),
            pccc_rule_reference=result.pccc_rule_reference,
            pedagogical_reason=result.pedagogical_reason), raw=raw, error="")

    def _finish(self, telemetry: TelemetryMessage, started: float, message: DirectiveMessage | ChallengeMessage,
                raw: str | None, error: str) -> DirectiveMessage:
        message.latency_ms = round((time.perf_counter() - started) * 1000.0, 1)
        self._log({
            "ts": time.time(),
            "mode": self.mode,
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
