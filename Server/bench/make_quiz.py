"""VL.4: paper / online knowledge quiz from the rule book (Assets/Resources/PcccRules.json, field "quiz").

Three forms with the same 12 items in different orders and option orders (pre-test, post-test, 1-week retention), plus
an answer key per form. Usage:  uv run python bench/make_quiz.py --out ../docs/vl_quiz_forms.md
"""

from __future__ import annotations

import argparse
import json
import random
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FORMS = {"A — trước khi học": 11, "B — ngay sau buổi học": 22, "C — sau 1 tuần": 33}
LETTERS = "ABC"


def load_items() -> list[dict]:
    book = json.loads((ROOT / "Assets" / "Resources" / "PcccRules.json").read_text(encoding="utf-8"))
    return [dict(r["quiz"], rule=r["id"], title=r["title"]) for r in book["rules"]]


def form(items: list[dict], seed: int) -> tuple[list[str], list[str]]:
    rng = random.Random(seed)
    order = items[:]
    rng.shuffle(order)
    lines, key = [], []
    for n, it in enumerate(order, 1):
        idx = list(range(len(it["options"])))
        rng.shuffle(idx)
        lines.append(f"**{n}.** {it['question']}  ")
        for k, i in enumerate(idx):
            lines.append(f"&nbsp;&nbsp;{LETTERS[k]}. {it['options'][i]}  ")
        lines.append("")
        key.append(f"{n}. {LETTERS[idx.index(it['answer'])]} ({it['rule']})")
    return lines, key


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path, default=None)
    args = ap.parse_args()
    items = load_items()
    out = ["# Bài trắc nghiệm kiến thức PCCC (12 câu, mỗi câu 1 luật R01–R12)", "",
           "Dùng cho đánh giá VL.4: cùng 12 câu, ba đề xáo thứ tự câu và phương án. Chấm 1 điểm / câu; "
           "điểm từng câu được ghi theo mã luật để so với mức thành thạo đo trong VR.", ""]
    keys = []
    for name, seed in FORMS.items():
        lines, key = form(items, seed)
        out += [f"## Đề {name}", "", "Họ tên / mã học viên: ________________   Ngày: ___________", ""] + lines
        keys.append((name, key))
    out += ["## Đáp án (giữ riêng)", ""]
    for name, key in keys:
        out += [f"**Đề {name}:** " + "; ".join(key), ""]
    out += ["## Giải thích", ""] + [f"- **{it['rule']}** {it['title']}: {it['why']}" for it in items]
    text = "\n".join(out) + "\n"
    if args.out:
        args.out.write_text(text, encoding="utf-8")
    else:
        print(text)


if __name__ == "__main__":
    main()
