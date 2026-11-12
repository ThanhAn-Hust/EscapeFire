"""Wire protocol between the headset (Unity) and the edge server.

Headset -> server : TelemetryMessage   {"type": "telemetry", ...}
Server  -> headset: DirectiveMessage   {"type": "directive", ...}
Headset -> server : DirectiveResultMessage {"type": "directive_result", ...}  (Tier-2 outcome, for logs)
"""

from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, Field

# Safe directive action space (spec 2026-10-08 §2.1). Must match Unity DirectiveCodes.
ALLOWED_DIRECTIVES: tuple[str, ...] = (
    "MAINTAIN_CURRENT_PACE",
    "ESCALATE_SECONDARY_FIRE",
    "TRIGGER_CO2_PIN_JAM",
    "TRIGGER_ALARM_CABLE_CUT",
    "ESCALATE_FM200_COUNTDOWN",
    "TRIGGER_SMOKE_SURGE",
    "TRIGGER_BLACKOUT",
    "TRIGGER_EXTINGUISHER_LOW_PRESSURE",
)

# Progressive difficulty (must match Unity DirectiveTiers): the n-th challenge may only use tiers 1..n.
DIRECTIVE_TIERS: dict[str, int] = {
    "TRIGGER_CO2_PIN_JAM": 1,
    "TRIGGER_SMOKE_SURGE": 1,
    "TRIGGER_ALARM_CABLE_CUT": 2,
    "TRIGGER_BLACKOUT": 2,
    "ESCALATE_SECONDARY_FIRE": 3,
    "TRIGGER_EXTINGUISHER_LOW_PRESSURE": 3,
    "ESCALATE_FM200_COUNTDOWN": 4,
}
MAX_TIER = 4

DIRECTIVE_DESCRIPTIONS: dict[str, str] = {
    "TRIGGER_CO2_PIN_JAM": "Chốt bình CO2 kẹt, phải giật 2 lần (phản xạ xử lý thiết bị)",
    "TRIGGER_SMOKE_SURGE": "Khói đen đặc đột ngột tràn xuống 0.8 m (phản xạ hạ thấp người)",
    "TRIGGER_ALARM_CABLE_CUT": "Cáp chuông báo cháy đứt, phải báo động bằng giọng nói",
    "TRIGGER_BLACKOUT": "Mất điện chiếu sáng, chỉ còn đèn khẩn cấp và biển EXIT (định hướng trong tối)",
    "ESCALATE_SECONDARY_FIRE": "Cháy lan sang bàn kỹ thuật / khay hóa chất (đánh giá lại tình huống)",
    "TRIGGER_EXTINGUISHER_LOW_PRESSURE": "Bình CO2 yếu áp, chỉ còn vài giây phun (quyết định dập tiếp hay sơ tán)",
    "ESCALATE_FM200_COUNTDOWN": "Còi xả khí FM-200 đếm ngược 30 s (bắt buộc sơ tán)",
}


def allowed_tier(challenges_so_far: int) -> int:
    return min(challenges_so_far + 1, MAX_TIER)
MAINTAIN = "MAINTAIN_CURRENT_PACE"

# Ground-truth PCCC rules the LLM must cite (spec §2.1 knowledge base).
PCCC_RULES: dict[str, str] = {
    "NFPA 75 8.4": "Cấm dùng chất chữa cháy dẫn điện (nước/bọt) trên thiết bị điện đang mang điện.",
    "NFPA 2001 / TCVN 7161": "Cháy phòng máy chủ quá 90s sẽ kích hoạt xả khí FM-200; con người có 30s để sơ tán.",
    "NFPA 101": "Kiểm tra nhiệt độ tay nắm cửa bằng mu bàn tay trước khi mở để tránh backdraft.",
    "LIFE SAFETY INVARIANT": "Luôn bảo đảm ít nhất một lối thoát hiểm an toàn có thể tiếp cận.",
    "TCVN 3890": "Trang bị, bố trí và sử dụng phương tiện PCCC cho nhà và công trình.",
}

PANIC_THRESHOLD = 0.75
CALM_THRESHOLD = 0.25
# A low CF right after the fire starts only means the learner has not acted yet: observe first.
MIN_OBSERVATION_SECONDS = 15.0


class TelemetryMessage(BaseModel):
    type: Literal["telemetry"] = "telemetry"
    session_id: str = ""
    time_elapsed: float = Field(ge=0)
    cognitive_friction_cf: float = Field(ge=0, le=1)
    is_power_cut: bool = False
    held_item: str = "None"
    current_zone: str = "Unknown"
    phase: str = ""
    emergency_elapsed: float = Field(default=0, ge=0)  # seconds since the fire started
    prior_challenges: list[str] = Field(default_factory=list)  # applied + permanently refused (never propose again)
    accepted_challenges: int | None = Field(default=None, ge=0)  # actually applied on the headset (drives the tier)
    prior_pieces: list[str] = Field(default_factory=list)    # composer piece codes already applied (V4)
    blocked_routes: list[str] = Field(default_factory=list)  # stairwells already blocked (V4)

    @property
    def challenges_given(self) -> int:
        """Difficulty tier counts challenges the learner really faced; older clients only send prior_challenges."""
        return self.accepted_challenges if self.accepted_challenges is not None else len(self.prior_challenges)


class DirectiveMessage(BaseModel):
    type: Literal["directive"] = "directive"
    directive: str = MAINTAIN
    target_object: str = ""
    pccc_rule_reference: str = ""
    pedagogical_reason: str = ""
    source: Literal["llm", "rule", "fallback"] = "fallback"
    latency_ms: float = 0.0


class ChallengeMessage(BaseModel):
    """V4 composer: a challenge built from catalog pieces (validated Tier-1; the headset re-validates)."""
    type: Literal["challenge"] = "challenge"
    pieces: list[dict] = Field(default_factory=list)
    tier: int = 0
    pccc_rule_reference: str = ""
    pedagogical_reason: str = ""
    source: Literal["llm"] = "llm"
    latency_ms: float = 0.0


class DirectiveResultMessage(BaseModel):
    type: Literal["directive_result"] = "directive_result"
    directive: str
    result: str
