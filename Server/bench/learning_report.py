"""VL.4: one CSV row per drill session from the LMS reports the headset sends (Server/logs/lms/*.json).

Columns: session, student, mode, passed, total score, skill session index, and for every PCCC rule R01–R12 the
learner's mastery after the session and whether this drill showed it (1 / 0 / empty = not tested).
Usage:  uv run python bench/learning_report.py --out ../docs/vl_learning_data.csv [--logs logs/lms]
"""

from __future__ import annotations

import argparse
import csv
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from escapefire_edge import catalog  # noqa: E402


def rows(log_dir: Path) -> list[dict]:
    out = []
    for path in sorted(log_dir.glob("*.json"), key=lambda p: p.stat().st_mtime):
        try:
            r = json.loads(path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, UnicodeDecodeError):
            continue
        row = {"session": r.get("session_id", path.stem), "student": r.get("student_id", ""), "mode": r.get("mode", ""),
               "passed": int(bool(r.get("passed"))), "total_score": r.get("total_score", ""),
               "skill_session": r.get("skill_session", "")}
        mastery = r.get("skill_mastery") or {}
        shown = {o.get("Skill"): int(bool(o.get("Correct"))) for o in r.get("skill_observations") or []}
        for rule in catalog.rules():
            m = mastery.get(rule)
            row[f"{rule}_p"] = round(m.get("PKnown", 0.0), 3) if isinstance(m, dict) else ""
            row[f"{rule}_shown"] = shown.get(rule, "")
        out.append(row)
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--logs", type=Path, default=Path(__file__).resolve().parents[1] / "logs" / "lms")
    ap.add_argument("--out", type=Path, required=True)
    args = ap.parse_args()
    data = rows(args.logs)
    if not data:
        print(f"no LMS reports in {args.logs}")
        return
    with open(args.out, "w", newline="", encoding="utf-8-sig") as fh:
        w = csv.DictWriter(fh, fieldnames=list(data[0].keys()))
        w.writeheader()
        w.writerows(data)
    print(f"{len(data)} sessions -> {args.out}")


if __name__ == "__main__":
    main()
