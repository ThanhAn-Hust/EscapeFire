"""Tier-1 validator (runs on the laptop): JSON syntax + whitelist + mandatory PCCC rule citation.

The LLM output is untrusted text. Anything that does not pass is replaced by MAINTAIN_CURRENT_PACE;
the headset still applies its own Tier-2 validator (budget / exit / state) afterwards.
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass

from .schema import ALLOWED_DIRECTIVES, MAINTAIN, PCCC_RULES

_JSON_OBJECT = re.compile(r"\{.*\}", re.DOTALL)
MAX_REASON_CHARS = 300


@dataclass(frozen=True)
class Tier1Result:
    ok: bool
    directive: str
    target_object: str = ""
    pccc_rule_reference: str = ""
    pedagogical_reason: str = ""
    error: str = ""


def _reject(error: str) -> Tier1Result:
    return Tier1Result(ok=False, directive=MAINTAIN, error=error)


def _extract_json(raw: str) -> dict | None:
    text = raw.strip()
    # Tolerate ```json fences and leading chatter: take the outermost {...} block.
    match = _JSON_OBJECT.search(text)
    if not match:
        return None
    try:
        data = json.loads(match.group(0))
    except json.JSONDecodeError:
        return None
    return data if isinstance(data, dict) else None


def _cites_known_rule(reference: str) -> bool:
    ref = reference.upper().strip()
    if len(ref) < 6:
        return False
    return any(key.upper() in ref or ref in key.upper() for key in PCCC_RULES)


def validate_llm_output(raw: str) -> Tier1Result:
    if not raw or not raw.strip():
        return _reject("empty_output")

    data = _extract_json(raw)
    if data is None:
        return _reject("invalid_json")

    directive = str(data.get("directive", "")).strip().upper()
    if directive not in ALLOWED_DIRECTIVES:
        return _reject(f"directive_not_whitelisted:{directive or '<missing>'}")

    reference = str(data.get("pccc_rule_reference", "")).strip()
    if directive != MAINTAIN and not _cites_known_rule(reference):
        return _reject("missing_or_unknown_pccc_rule")

    return Tier1Result(
        ok=True,
        directive=directive,
        target_object=str(data.get("target_object", ""))[:80],
        pccc_rule_reference=reference[:80],
        pedagogical_reason=str(data.get("pedagogical_reason", ""))[:MAX_REASON_CHARS],
    )
