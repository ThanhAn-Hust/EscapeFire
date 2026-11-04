"""Generative After-Action Review: the LLM rewrites the scorecard into personalised Socratic feedback.

Safety: the LLM only gets the scorecard facts; output must be plain text, non-empty, bounded in length and must not
introduce directives/JSON. Anything else falls back to the deterministic template written by the headset.
"""

from __future__ import annotations

import asyncio

from pydantic import BaseModel, Field

from .llm import LLMClient
from .schema import PCCC_RULES

MAX_DEBRIEF_CHARS = 900


class ScorecardLine(BaseModel):
    label: str
    points: int = Field(ge=0)
    max_points: int = Field(ge=0)
    note: str = ""


class DebriefRequest(BaseModel):
    passed: bool
    fail_cause: str = ""
    total_score: int = Field(ge=0, le=100)
    scorecard: list[ScorecardLine] = Field(default_factory=list)
    standing_in_smoke_seconds: float = Field(default=0, ge=0)
    average_cognitive_friction: float = Field(default=0.5, ge=0, le=1)
    template_debrief: str = ""


class DebriefResponse(BaseModel):
    debrief: str
    source: str  # "llm" | "template"


_RULES = "\n".join(f"- {k}: {v}" for k, v in PCCC_RULES.items())

DEBRIEF_SYSTEM_PROMPT = f"""Bạn là AI Mentor PCCC thân thiện, nhận xét sau buổi diễn tập thực tế ảo cho sinh viên.
Chỉ dựa trên bảng điểm được cung cấp, không bịa thêm sự kiện. Chỉ trích dẫn các luật sau nếu cần:
{_RULES}
Viết tiếng Việt, 3-5 câu, giọng gợi mở (Socratic): khen 1 điểm mạnh cụ thể, chỉ ra 1-2 lỗi quan trọng nhất kèm lý do an toàn,
kết thúc bằng một câu hỏi tự vấn. Không dùng JSON, không markdown, không liệt kê điểm số."""


def build_debrief_prompt(req: DebriefRequest) -> str:
    lines = [f"Kết quả: {'Thoát hiểm an toàn' if req.passed else 'Thất bại - ' + req.fail_cause}",
             f"Tổng điểm: {req.total_score}/100",
             f"Thời gian đứng thẳng trong khói: {req.standing_in_smoke_seconds:.0f} giây",
             f"Chỉ số ma sát nhận thức trung bình: {req.average_cognitive_friction:.2f}",
             "Bảng điểm:"]
    lines += [f"- {s.label}: {s.points}/{s.max_points} ({s.note})" for s in req.scorecard]
    return "\n".join(lines)


def _is_acceptable(text: str) -> bool:
    t = text.strip()
    return 20 <= len(t) <= MAX_DEBRIEF_CHARS and not t.startswith("{") and "```" not in t


async def generate_debrief(llm: LLMClient, req: DebriefRequest, timeout_s: float = 8.0) -> DebriefResponse:
    fallback = DebriefResponse(debrief=req.template_debrief or "Hoàn thành buổi diễn tập.", source="template")
    try:
        text = await asyncio.wait_for(llm.complete(DEBRIEF_SYSTEM_PROMPT, build_debrief_prompt(req)), timeout=timeout_s)
    except Exception:  # timeout, connection error, ...
        return fallback
    if not text or not _is_acceptable(text):
        return fallback
    return DebriefResponse(debrief=text.strip(), source="llm")
