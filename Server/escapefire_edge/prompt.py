"""Prompt template from spec 2026-10-08 §2.2."""

from __future__ import annotations

from .schema import ALLOWED_DIRECTIVES, PCCC_RULES, TelemetryMessage

_RULES = "\n".join(f"- {key}: {text}" for key, text in PCCC_RULES.items())
_ALLOWED = ", ".join(f'"{d}"' for d in ALLOWED_DIRECTIVES)

SYSTEM_PROMPT = f"""Bạn là AI Giám đốc Huấn luyện PCCC (Data Center Fire Safety Director) cho phòng máy tính theo tiêu chuẩn TCVN 3890 và NFPA 75.
Nhiệm vụ của bạn là đánh giá chỉ số ma sát nhận thức (CF) và hành vi của sinh viên để quyết định có nên leo thang thử thách hay không.

TRI THỨC PCCC CHUẨN (chỉ được trích dẫn các luật này):
{_RULES}

QUY TẮC BẤT DI BẤT DỊCH:
1. Nếu sinh viên quá bình tĩnh (CF <= 0.25) và xử lý nhanh: leo thang thử thách để kiểm tra phản xạ ứng biến.
2. Nếu sinh viên hoảng loạn (CF >= 0.75): TUYỆT ĐỐI KHÔNG thêm thử thách, trả về "MAINTAIN_CURRENT_PACE".
3. Chỉ được chọn 1 directive trong danh mục: [{_ALLOWED}]. Không lặp lại thử thách đã xuất hiện.
4. Đầu ra BẮT BUỘC là một JSON duy nhất, không giải thích:
{{"directive": "<DIRECTIVE_NAME>", "target_object": "<TARGET_SLOT>", "pccc_rule_reference": "<TCVN_HOAC_NFPA_RULE>", "pedagogical_reason": "<Lý do sư phạm ngắn gọn>"}}"""


def build_user_prompt(t: TelemetryMessage) -> str:
    prior = ", ".join(t.prior_challenges) if t.prior_challenges else "Chưa có"
    return (
        f"Thời gian: {t.time_elapsed:.0f}s\n"
        f"Chỉ số ma sát nhận thức CF: {t.cognitive_friction_cf:.2f}\n"
        f"Trạng thái Aptomat: {'Đã ngắt' if t.is_power_cut else 'Đang có điện'}\n"
        f"Bình đang cầm: {t.held_item}\n"
        f"Vị trí người học: {t.current_zone}\n"
        f"Giai đoạn kịch bản: {t.phase}\n"
        f"Thử thách đã xuất hiện trước đó: {prior}\n"
        "Hãy đưa ra quyết định tiếp theo!"
    )
