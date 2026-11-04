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
)
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


class TelemetryMessage(BaseModel):
    type: Literal["telemetry"] = "telemetry"
    session_id: str = ""
    time_elapsed: float = Field(ge=0)
    cognitive_friction_cf: float = Field(ge=0, le=1)
    is_power_cut: bool = False
    held_item: str = "None"
    current_zone: str = "Unknown"
    phase: str = ""
    prior_challenges: list[str] = Field(default_factory=list)


class DirectiveMessage(BaseModel):
    type: Literal["directive"] = "directive"
    directive: str = MAINTAIN
    target_object: str = ""
    pccc_rule_reference: str = ""
    pedagogical_reason: str = ""
    source: Literal["llm", "rule", "fallback"] = "fallback"
    latency_ms: float = 0.0


class DirectiveResultMessage(BaseModel):
    type: Literal["directive_result"] = "directive_result"
    directive: str
    result: str
