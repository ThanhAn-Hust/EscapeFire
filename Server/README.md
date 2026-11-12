# EscapeFire Edge AI Director (M3)

Laptop-side server that receives telemetry from the Quest 2 every 10 s and returns one macro directive
from the safe action space. Spec: `docs/superpowers/specs/2026-10-08-llm-adaptive-level-generation-design.md`.

```
Quest 2 (Unity)                         Laptop (this server)
EdgeDirectorClient ── telemetry ──────► Director
                                         ├─ rule guards (CF ≥ 0.75, not in emergency, CF mid) → MAINTAIN, no LLM call
                                         ├─ LLM (Ollama / Gemma) with 6 s timeout
                                         └─ Tier-1: JSON + whitelist + known PCCC rule + no repeats → else MAINTAIN
MicroAIGovernor ◄──── directive ────────┘
 └─ Tier-2 (C#): budget ≤ 4 fires, exit kept free, state prerequisites
EdgeDirectorClient ── directive_result ─► logs/director_YYYYMMDD.jsonl
```

If the server is down or slow, the headset keeps the local Micro-AI governor running (offline fallback).

## Run

```bash
cd Server
uv sync
uv run python -m escapefire_edge.app --fake-llm     # scripted directives, no GPU needed
uv run python -m escapefire_edge.app                # real LLM via Ollama
```

Real LLM: install Ollama, then `ollama pull gemma3:4b`. Override the model / host with the
`ESCAPEFIRE_LLM_MODEL` and `OLLAMA_HOST` environment variables.

Server listens on `0.0.0.0:8765`. In Unity set `EdgeDirectorClient.serverUrl` to
`ws://<laptop LAN IP>:8765/session`; laptop and headset must be on the same Wi-Fi, and Windows Firewall must
allow inbound TCP 8765. Android build: Player Settings → Internet Access = Require.

## HTTP endpoints (M5)

- `POST /aar/debrief` — LLM rewrites the AAR scorecard into Socratic feedback (plain text, ≤ 900 chars);
  JSON/garbage/timeouts fall back to the headset's template debrief.
- `POST /api/v1/sessions/{session_id}/complete` — local mock of the LMS (daotao.ai) contract; stores the report in
  `logs/lms/{session_id}.json`. The headset queues reports offline (`persistentDataPath/lms_outbox`) and retries.

## Test

```bash
uv run pytest -q
```

Every decision (telemetry, raw LLM output, Tier-1 error, final directive, Tier-2 result) is appended to
`logs/director_*.jsonl` for the thesis evaluation.

## V4: director modes, Challenge Composer, prompt -> room

```bash
uv run python -m escapefire_edge.app --director-mode composer   # rules | enum (default, baseline) | composer
```

- `rules`: fixed if-else (easiest challenge left), no LLM — the ablation baseline.
- `enum`: the LLM picks one of the 7 directives (previous behaviour, kept as fallback / baseline).
- `composer`: the LLM composes 1-3 parameterised pieces from `escapefire_edge/v4_catalog.json`
  (smoke, lights per zone, device faults, fire spot, blocked stairwell, NPC behaviour, FM-200, delays) and sends
  `{"type": "challenge", ...}`; Tier-1 = `challenge.validate_challenge`, the headset re-runs the same rules
  (`ChallengeValidator.cs`, shared fixtures in `tests/fixtures/`) plus scene checks (`ChallengeRunner.cs`).
- `POST /scenario/generate {"prompt": "...", "level": 1}`: teacher prompt -> ScenarioSpec (LLM, one repair round with
  the validator's errors, template fallback). The headset builds it in scene `03_Generated`.

Catalog: `Assets/Resources/V4Catalog.json` is the master; copy it to `escapefire_edge/v4_catalog.json`
(`tests/test_v4.py` fails if they differ).

Benchmarks for the report (need Ollama running; `--dry-run` only checks the tools):

```bash
uv run python bench/director_ablation.py --drills 20 --out ../docs/v4_director_ablation.md
uv run python bench/scenario_benchmark.py --out ../docs/v4_scenario_benchmark.md
```
