"""Renders the headset's voice pack: every Mentor line listed by the game (Logs/voice_lines.txt, written in the editor
whenever a line had no voice) becomes Assets/Resources/Voice/<key>.wav with the Piper Vietnamese voice, so the
headset speaks without the server. Lines already rendered are skipped.

    cd Server
    uv run python -m escapefire_edge.voice_pack --lines ../Logs/voice_lines.txt --out ../Assets/Resources/Voice
"""

from __future__ import annotations

import argparse
from pathlib import Path

from .tts import PiperSpeaker, line_key


def render(lines_file: Path, out: Path, speaker: PiperSpeaker | None = None) -> tuple[int, int]:
    speaker = speaker or PiperSpeaker()
    if not speaker.available:
        raise SystemExit(f"Piper voice not found: {speaker.model}")
    out.mkdir(parents=True, exist_ok=True)
    made = kept = 0
    seen = set()
    for raw in lines_file.read_text(encoding="utf-8").splitlines():
        text = raw.strip()
        if not text or text.startswith("[") or text in seen:
            continue
        seen.add(text)
        target = out / f"{line_key(text)}.wav"
        if target.exists():
            kept += 1
            continue
        target.write_bytes(speaker.wav(text))
        made += 1
    return made, kept


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--lines", type=Path, default=Path("../Logs/voice_lines.txt"))
    ap.add_argument("--out", type=Path, default=Path("../Assets/Resources/Voice"))
    args = ap.parse_args()
    made, kept = render(args.lines, args.out)
    print(f"voice pack: {made} new lines, {kept} already there -> {args.out}")


if __name__ == "__main__":
    main()
