"""V4.7 ablation: hard-coded rules vs LLM picking one enum (baseline) vs LLM composing pieces.

Simulates N calm learners (each a drill of several director turns with varied telemetry: zone, held item, power,
CF) and feeds the same telemetry to the three director modes. The "headset" accepts what passes Tier-1 and updates
prior challenges / pieces / blocked routes exactly like EdgeDirectorClient does.

Metrics per mode:
  - validity: share of LLM answers that pass Tier-1 (rules mode: no LLM, always valid)
  - challenges per drill, distinct challenges (piece codes incl. parameters), distinct drill sequences / drills
  - entropy (bits) of the first challenge across drills — how predictable the director is
  - latency mean / p95
Usage (Ollama with gemma3:4b running):
  uv run python bench/director_ablation.py --drills 20 --out ../docs/v4_director_ablation.md
  uv run python bench/director_ablation.py --dry-run   # scripted fake LLM: checks the tool only, NOT results
"""

from __future__ import annotations

import argparse
import asyncio
import json
import math
import random
import statistics
import sys
import time
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from escapefire_edge import catalog  # noqa: E402
from escapefire_edge.director import Director  # noqa: E402
from escapefire_edge.llm import OllamaClient  # noqa: E402
from escapefire_edge.schema import MAINTAIN, ChallengeMessage, TelemetryMessage  # noqa: E402

ZONES = ["ServerZone", "WorkstationZone", "Corridor", "StairwellA"]
HELD = ["None", "CO2_Extinguisher", "Water_Extinguisher"]


class FakeLLM:
    """Dry run only: random answers in the right shape (some invalid) so the pipeline can be exercised offline."""

    def __init__(self, seed: int) -> None:
        self.rng = random.Random(seed)

    async def complete(self, system: str, user: str) -> str:
        await asyncio.sleep(0)
        if "GHÉP" in system:
            kinds = list(catalog.pieces())
            pieces = []
            for kind in self.rng.sample(kinds, self.rng.randint(1, 2)):
                params = {}
                for name, rng in catalog.pieces()[kind]["params"].items():
                    params[name] = self.rng.choice(rng["options"]) if "options" in rng else round(self.rng.uniform(rng["min"], rng["max"]), 2)
                pieces.append({"piece": kind, "params": params, "delay_s": self.rng.choice([0, 5, 10])})
            return json.dumps({"pieces": pieces, "pccc_rule_reference": catalog.pieces()[pieces[0]["piece"]]["rule"],
                               "pedagogical_reason": "dry run"})
        choices = [c.strip() for c in user.split("CHỈ được chọn một trong:")[-1].split("\n")[0].split(",")]
        return json.dumps({"directive": self.rng.choice(choices), "pccc_rule_reference": "LIFE SAFETY INVARIANT",
                           "pedagogical_reason": "dry run"})


def telemetry_for(drill: int, turn: int, state: dict, rng: random.Random) -> TelemetryMessage:
    return TelemetryMessage(
        session_id=f"ablation-{drill}", time_elapsed=40 + turn * 30, cognitive_friction_cf=round(rng.uniform(0.0, 0.24), 2),
        is_power_cut=rng.random() < 0.5, held_item=rng.choice(HELD), current_zone=rng.choice(ZONES),
        phase="Phase2_ServerExplosionFire", emergency_elapsed=30 + turn * 30,
        prior_challenges=list(state["prior"]), accepted_challenges=state["accepted"],
        prior_pieces=list(state["pieces"]), blocked_routes=list(state["blocked"]))


async def run_mode(mode: str, llm, drills: int, turns: int, seed: int) -> dict:
    director = Director(llm, mode=mode, timeout_s=30.0, log_dir=None, min_observation_s=0)
    latencies, valid, asked = [], 0, 0
    sequences, firsts, distinct = [], [], set()
    for d in range(drills):
        rng = random.Random(seed * 1000 + d)  # same telemetry for every mode
        state = {"prior": [], "accepted": 0, "pieces": [], "blocked": []}
        seq = []
        for t in range(turns):
            started = time.perf_counter()
            msg = await director.decide(telemetry_for(d, t, state, rng))
            latencies.append((time.perf_counter() - started) * 1000.0)
            if mode != "rules":
                asked += 1
                valid += msg.source == "llm"
            if isinstance(msg, ChallengeMessage):
                codes = []
                for p in msg.pieces:
                    from escapefire_edge.challenge import piece_code
                    code = piece_code(p)
                    codes.append(code)
                    state["pieces"].append(code)
                    if p["piece"] == "BLOCK_ROUTE":
                        state["blocked"].append(p["params"]["route"])
                    nums = {k: v for k, v in p["params"].items() if isinstance(v, float)}
                    distinct.add(code + (json.dumps(nums, sort_keys=True) if nums else ""))
                state["accepted"] += 1
                seq.append("+".join(codes))
            elif msg.directive != MAINTAIN:
                state["prior"].append(msg.directive)
                state["accepted"] += 1
                seq.append(msg.directive)
                distinct.add(msg.directive)
        sequences.append(tuple(seq))
        if seq:
            firsts.append(seq[0])
    counts = Counter(firsts)
    total = sum(counts.values()) or 1
    entropy = abs(-sum(c / total * math.log2(c / total) for c in counts.values()))
    return {
        "mode": mode,
        "validity": 1.0 if mode == "rules" else (valid / asked if asked else 0.0),
        "challenges_per_drill": statistics.mean(len(s) for s in sequences) if sequences else 0.0,
        "distinct_challenges": len(distinct),
        "distinct_sequences": len(set(sequences)),
        "drills": drills,
        "first_challenge_entropy_bits": round(entropy, 2),
        "latency_ms_mean": round(statistics.mean(latencies), 1),
        "latency_ms_p95": round(sorted(latencies)[int(0.95 * (len(latencies) - 1))], 1),
    }


def table(results: list[dict], header: str) -> str:
    lines = [header, "",
             "| Chế độ | Hợp lệ Tier-1 | Thử thách / ván | Thử thách khác nhau | Chuỗi khác nhau / ván | Entropy thử thách đầu (bit) | Trễ TB (ms) | Trễ p95 (ms) |",
             "| :--- | :-: | :-: | :-: | :-: | :-: | :-: | :-: |"]
    for r in results:
        lines.append(f"| {r['mode']} | {r['validity'] * 100:.0f}% | {r['challenges_per_drill']:.1f} | {r['distinct_challenges']} | "
                     f"{r['distinct_sequences']}/{r['drills']} | {r['first_challenge_entropy_bits']} | {r['latency_ms_mean']} | {r['latency_ms_p95']} |")
    return "\n".join(lines) + "\n"


async def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--drills", type=int, default=20)
    ap.add_argument("--turns", type=int, default=4)
    ap.add_argument("--seed", type=int, default=7)
    ap.add_argument("--dry-run", action="store_true", help="scripted fake LLM (tool check only, not results)")
    ap.add_argument("--out", type=Path, default=None)
    args = ap.parse_args()
    llm = FakeLLM(args.seed) if args.dry_run else OllamaClient(timeout_s=40.0)
    results = [await run_mode(m, llm, args.drills, args.turns, args.seed) for m in ("rules", "enum", "composer")]
    model = "FAKE (dry run — không phải kết quả)" if args.dry_run else getattr(llm, "model", "?")
    text = table(results, f"### Ablation AI Director — {args.drills} ván × {args.turns} lượt, LLM: {model}, {time.strftime('%Y-%m-%d %H:%M')}")
    print(text)
    print(json.dumps(results, ensure_ascii=False))
    if args.out:
        args.out.write_text(text + "\n```json\n" + json.dumps(results, ensure_ascii=False, indent=2) + "\n```\n", encoding="utf-8")


if __name__ == "__main__":
    asyncio.run(main())
