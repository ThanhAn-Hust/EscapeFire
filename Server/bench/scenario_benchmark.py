"""V4.6 benchmark: teacher prompt -> ScenarioSpec with the real LLM.

For 20 Vietnamese prompts: share valid on the first answer, after the one repair round, template fallbacks, the
validator errors seen, latency and diversity (room types, furniture sets). Writes a table for the report.
Usage (Ollama running):  uv run python bench/scenario_benchmark.py --out ../docs/v4_scenario_benchmark.md
                         uv run python bench/scenario_benchmark.py --model gemma3:12b ...
Dry run (template-only fake, tool check):  --dry-run
"""

from __future__ import annotations

import argparse
import asyncio
import json
import statistics
import sys
import time
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from escapefire_edge.llm import OllamaClient  # noqa: E402
from escapefire_edge.scenario import TEMPLATES, ScenarioRequest, generate_scenario  # noqa: E402

PROMPTS = [
    "Phòng máy tính 30 chỗ, tủ server ở góc bị chập điện bốc cháy",
    "Thư viện tầng 2 nhiều giá sách, cháy từ thùng rác, có sinh viên hoảng loạn",
    "Căng-tin trường, chảo dầu trong bếp bén lửa giờ ăn trưa",
    "Xưởng thực hành có tủ hóa chất bị rò rỉ và bắt lửa, một người ngất",
    "Văn phòng khoa, máy photocopy chập điện cháy, hai cán bộ còn trong phòng",
    "Phòng thí nghiệm nhỏ, ổ cắm dưới bàn máy tính cháy âm ỉ",
    "Thư viện yên tĩnh, cháy giá sách gần lối ra chính",
    "Căng-tin đông người, cháy tủ lạnh và quầy bếp",
    "Phòng họp lớn với bàn hội nghị, máy in cạnh cửa sổ bốc khói",
    "Xưởng cơ khí nhiều thùng các-tông, cháy đống thùng giấy",
    "Phòng server nhỏ không cửa sổ, cháy tủ rack",
    "Văn phòng nhiều vách ngăn, thùng giấy vụn bắt lửa",
    "Thư viện có khu sofa đọc sách, cháy sofa",
    "Phòng lab dạy học, có một sinh viên yếu không tự đi được",
    "Căng-tin nhỏ một cửa, cháy bếp dầu",
    "Xưởng vẽ kỹ thuật với bàn vẽ, cháy tủ hóa chất pha sơn",
    "Văn phòng mở, cháy máy tính để bàn",
    "Phòng máy tầng cao, khói dày, có người bất tỉnh",
    "Thư viện rất rộng 16x14m, cháy giữa phòng",
    "Phòng làm việc giảng viên, cháy thùng rác cạnh bàn",
]


class TemplateLLM:
    """Dry run: answers with the matching template (always valid) — exercises the tool, measures nothing."""

    async def complete(self, system: str, user: str) -> str:
        from escapefire_edge.scenario import template_for
        return json.dumps(template_for(user), ensure_ascii=False)


async def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--model", default=None)
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--out", type=Path, default=None)
    args = ap.parse_args()
    llm = TemplateLLM() if args.dry_run else OllamaClient(model=args.model, timeout_s=60.0)
    rows, sources, errors, rooms, latencies, sets = [], Counter(), Counter(), Counter(), [], set()
    for prompt in PROMPTS:
        started = time.perf_counter()
        r = await generate_scenario(llm, ScenarioRequest(prompt=prompt), timeout_s=60.0)
        latencies.append((time.perf_counter() - started) * 1000.0)
        sources[r.source] += 1
        for e in r.errors:
            errors[e.split(":")[0]] += 1
        rooms[r.spec["room_type"]] += 1
        sets.add(tuple(sorted((f["category"], f["count"]) for f in r.spec.get("furniture", []))))
        rows.append((prompt, r.source, r.spec["room_type"], r.spec["title"], ";".join(r.errors[:3])))
    n = len(PROMPTS)
    model = "FAKE (dry run — không phải kết quả)" if args.dry_run else llm.model
    out = [f"### Prompt → ScenarioSpec — {n} prompt, LLM: {model}, {time.strftime('%Y-%m-%d %H:%M')}", "",
           f"- Hợp lệ ngay lần đầu: {sources['llm']}/{n}; sau 1 lần sửa: {sources['llm_repaired']}/{n}; rơi về mẫu: {sources['template']}/{n}",
           f"- Trễ TB {statistics.mean(latencies):.0f} ms, p95 {sorted(latencies)[int(0.95 * (n - 1))]:.0f} ms",
           f"- Loại phòng: {dict(rooms)}; bộ đồ đạc khác nhau: {len(sets)}/{n}",
           f"- Lỗi validator gặp nhiều nhất: {dict(errors.most_common(6))}", "",
           "| Prompt | Nguồn | Phòng | Tiêu đề | Lỗi lần trước |", "| :--- | :-: | :-: | :--- | :--- |"]
    out += [f"| {p} | {s} | {room} | {t} | {e} |" for p, s, room, t, e in rows]
    text = "\n".join(out) + "\n"
    print(text)
    if args.out:
        args.out.write_text(text, encoding="utf-8")


if __name__ == "__main__":
    assert len(PROMPTS) == 20 and TEMPLATES
    asyncio.run(main())
