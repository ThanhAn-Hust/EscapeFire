"""Challenge Composer (plan V4.5a/b): the LLM composes a challenge from parameterised pieces of the shared catalog.

Output of the LLM (untrusted):
  {"pieces": [{"piece": "SMOKE_SURGE", "params": {"drop_m": 0.6, "speed": 1.5}, "delay_s": 0},
              {"piece": "LIGHTS_OFF", "params": {"zone": "corridor"}, "delay_s": 10}],
   "pccc_rule_reference": "R07_STAY_LOW", "pedagogical_reason": "..."}

Tier-1 validation here; the headset repeats the same checks (ChallengeValidator.cs, same error codes, shared fixtures in
tests/fixtures/challenge_cases.json) and adds scene checks (route exists, exit reachable, FPS budget).
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass, field

from . import catalog
from .schema import PCCC_RULES, allowed_tier

_JSON_OBJECT = re.compile(r"\{.*\}", re.DOTALL)
MAX_REASON_CHARS = 300


@dataclass
class ChallengeContext:
    challenges_given: int = 0
    cf: float = 0.0
    prior: list[str] = field(default_factory=list)          # piece codes already used this drill
    blocked_routes: list[str] = field(default_factory=list)  # routes already blocked
    weak_skills: list[str] = field(default_factory=list)     # VL.3: skills the learner should practise (not a rule)


@dataclass
class ChallengeResult:
    ok: bool
    errors: list[str]
    pieces: list[dict] = field(default_factory=list)
    tier: int = 0
    pccc_rule_reference: str = ""
    pedagogical_reason: str = ""

    @property
    def error(self) -> str:
        return self.errors[0] if self.errors else ""

    @property
    def codes(self) -> list[str]:
        return [piece_code(p) for p in self.pieces]

    @property
    def skills(self) -> list[str]:
        """PCCC skills (rule ids) this challenge makes the learner practise."""
        out: list[str] = []
        for p in self.pieces:
            for s in piece_skills(p):
                if s not in out:
                    out.append(s)
        return out

    def targets(self, weak_skills: list[str]) -> bool:
        return any(s in weak_skills for s in self.skills)


def piece_code(p: dict) -> str:
    """KIND, or KIND:option for pieces with an option parameter (each counts as its own challenge)."""
    spec = catalog.pieces().get(p.get("piece", ""), {})
    for name, rng in spec.get("params", {}).items():
        if "options" in rng:
            return f"{p['piece']}:{p.get('params', {}).get(name, '')}"
    return str(p.get("piece", ""))


def piece_skills(p: dict) -> list[str]:
    spec = catalog.pieces()[p["piece"]]
    for name, by_option in spec.get("option_skills", {}).items():
        value = p.get("params", {}).get(name)
        if value in by_option:
            return list(by_option[value])
    return list(spec.get("skills", []))


def directive_skills(directive: str) -> list[str]:
    return list(catalog.catalog()["challenge"].get("directive_skills", {}).get(directive, []))


def piece_tier(p: dict) -> int:
    spec = catalog.pieces()[p["piece"]]
    for name, tiers in spec.get("option_tiers", {}).items():
        value = p.get("params", {}).get(name)
        if value in tiers:
            return int(tiers[value])
    return int(spec["tier"])


def known_rule(reference: str) -> bool:
    """An R-id of the shared rule book ("R07_STAY_LOW", "R07", or starting with the id), or a legacy NFPA/TCVN key."""
    ref = reference.strip().upper()
    if not ref:
        return False
    for rule in catalog.rules():
        if ref == rule or ref == rule.split("_")[0] or ref.startswith(rule):
            return True
    return len(ref) >= 6 and any(key.upper() in ref for key in PCCC_RULES)


def extract_json(raw: str) -> dict | None:
    if not raw or not raw.strip():
        return None
    match = _JSON_OBJECT.search(raw.strip())
    if not match:
        return None
    try:
        data = json.loads(match.group(0))
    except json.JSONDecodeError:
        return None
    return data if isinstance(data, dict) else None


def validate_challenge(data: dict | None, ctx: ChallengeContext) -> ChallengeResult:
    errors: list[str] = []
    if not isinstance(data, dict):
        return ChallengeResult(False, ["not_object"])
    raw_pieces = data.get("pieces")
    if not isinstance(raw_pieces, list) or not raw_pieces:
        return ChallengeResult(False, ["no_pieces"])

    cfg = catalog.catalog()["challenge"]
    limit = cfg["max_pieces"]
    if ctx.cf > cfg["max_pieces_when_cf_above"]["cf"]:
        limit = min(limit, cfg["max_pieces_when_cf_above"]["max"])
    if len(raw_pieces) > limit:
        errors.append(f"too_many_pieces:{len(raw_pieces)}>{limit}")

    known = catalog.pieces()
    clean: list[dict] = []
    for item in raw_pieces:
        if not isinstance(item, dict):
            errors.append("bad_piece")
            continue
        kind = str(item.get("piece", "")).strip().upper()
        if kind not in known:
            errors.append(f"unknown_piece:{kind or '<missing>'}")
            continue
        spec = known[kind]
        params_in = item.get("params") or {}
        if not isinstance(params_in, dict):
            errors.append(f"bad_param:{kind}")
            continue
        params: dict = {}
        for name, rng in spec["params"].items():
            if name not in params_in:
                errors.append(f"missing_param:{kind}.{name}")
                continue
            value = params_in[name]
            if "options" in rng:
                value = str(value).strip().lower()
                if value not in rng["options"]:
                    errors.append(f"bad_option:{kind}.{name}")
                    continue
            else:
                try:
                    value = float(value)
                except (TypeError, ValueError):
                    errors.append(f"bad_param:{kind}.{name}")
                    continue
                if not rng["min"] <= value <= rng["max"]:
                    errors.append(f"param_out_of_range:{kind}.{name}")
                    continue
            params[name] = value
        for name in params_in:
            if name not in spec["params"]:
                errors.append(f"unknown_param:{kind}.{name}")
        try:
            delay = float(item.get("delay_s", 0) or 0)
        except (TypeError, ValueError):
            delay = -1.0
        if not 0 <= delay <= cfg["max_delay_s"]:
            errors.append(f"bad_delay:{kind}")
            delay = 0.0
        clean.append({"piece": kind, "params": params, "delay_s": delay})

    if errors:
        return ChallengeResult(False, errors)

    for p in clean:
        if known[p["piece"]].get("alone") and len(clean) > 1:
            errors.append(f"alone:{p['piece']}")
    codes = [piece_code(p) for p in clean]
    for i, code in enumerate(codes):
        if code in codes[:i]:
            errors.append(f"duplicate:{code}")
        if code in ctx.prior:
            errors.append(f"repeated:{code}")
    tier = max(piece_tier(p) for p in clean)
    cap = allowed_tier(ctx.challenges_given)
    if tier > cap:
        errors.append(f"tier_too_high:{tier}>{cap}")

    blocked = set(ctx.blocked_routes)
    for p in clean:
        if p["piece"] == "BLOCK_ROUTE":
            route = p["params"]["route"]
            if route in blocked:
                errors.append(f"route_already_blocked:{route}")
            blocked.add(route)
    if blocked and len(blocked) >= len(cfg["routes"]):
        errors.append("no_exit_left")

    reference = str(data.get("pccc_rule_reference", "")).strip()
    if not known_rule(reference):
        errors.append("unknown_rule")
    reason = str(data.get("pedagogical_reason", "")).strip()
    if not reason:
        errors.append("missing_reason")

    return ChallengeResult(not errors, errors, clean if not errors else [], tier, reference[:80], reason[:MAX_REASON_CHARS])


# ---------------------------------------------------------------------------------------------------- prompt

def _piece_catalog_text() -> str:
    lines = []
    for kind, spec in catalog.pieces().items():
        params = []
        for name, rng in spec["params"].items():
            params.append(f"{name}: " + ("|".join(rng["options"]) if "options" in rng else f"{rng['min']}..{rng['max']}"))
        tiers = spec.get("option_tiers")
        tier = f"cấp {spec['tier']}" if not tiers else "cấp " + ", ".join(
            f"{v}={t}" for opts in tiers.values() for v, t in opts.items())
        alone = " (phải đứng một mình)" if spec.get("alone") else ""
        if spec.get("option_skills"):
            trains = "; ".join(f"{v}→{'/'.join(s)}" for opts in spec["option_skills"].values() for v, s in opts.items())
        else:
            trains = "/".join(spec.get("skills", []))
        lines.append(f"- {kind} [{tier}]{alone}: {spec['desc']}. Tham số: {', '.join(params)}. Luật: {spec['rule']}. Luyện kỹ năng: {trains}")
    return "\n".join(lines)


COMPOSER_SYSTEM = f"""Bạn là AI Giám đốc Huấn luyện PCCC. Bạn THIẾT KẾ thử thách cho người học đang diễn tập trong VR bằng cách
GHÉP các mảnh có tham số dưới đây (1-3 mảnh, có thể hẹn giờ delay_s 0-30 giây), dựa trên điểm yếu / điểm mạnh người học vừa thể hiện.

CÁC MẢNH (tham số phải nằm trong giới hạn):
{_piece_catalog_text()}

LUẬT AN TOÀN:
1. Luôn còn ít nhất một lối thoát (không chặn cả hai buồng thang).
2. Không lặp lại mảnh đã dùng; độ khó (cấp cao nhất trong các mảnh) không vượt cấp tối đa được ghi trong dữ liệu.
3. Người học càng căng thẳng thì càng ít mảnh.
4. Trích dẫn đúng một luật trong: {", ".join(catalog.rules())}.
5. ƯU TIÊN mảnh luyện đúng các KỸ NĂNG CÒN YẾU của người học (ghi trong dữ liệu); nêu rõ kỹ năng đó trong lý do sư phạm.

Đầu ra BẮT BUỘC là một JSON duy nhất:
{{"pieces": [{{"piece": "<MẢNH>", "params": {{...}}, "delay_s": <giây>}}], "pccc_rule_reference": "<Rxx_...>", "pedagogical_reason": "<vì sao tổ hợp này dạy được người học, ngắn gọn>"}}"""


def build_composer_prompt(telemetry, ctx: ChallengeContext) -> str:
    prior = ", ".join(ctx.prior) if ctx.prior else "Chưa có"
    return (
        f"Thời gian: {telemetry.time_elapsed:.0f}s, đã {telemetry.emergency_elapsed:.0f}s kể từ khi cháy\n"
        f"CF (ma sát nhận thức): {telemetry.cognitive_friction_cf:.2f}\n"
        f"Aptomat: {'đã ngắt' if telemetry.is_power_cut else 'còn điện'}; đang cầm: {telemetry.held_item}; vị trí: {telemetry.current_zone}\n"
        f"Giai đoạn: {telemetry.phase}\n"
        f"Mảnh đã dùng: {prior}\n"
        f"Buồng thang đã bị chặn: {', '.join(ctx.blocked_routes) or 'không'}\n"
        f"Kỹ năng còn yếu cần luyện (ưu tiên): {', '.join(ctx.weak_skills) or 'chưa có dữ liệu'}\n"
        f"Cấp tối đa cho thử thách này: {allowed_tier(ctx.challenges_given)}\n"
        "Hãy thiết kế thử thách tiếp theo!"
    )
