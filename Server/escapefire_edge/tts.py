"""Vietnamese text-to-speech on the edge laptop (Piper, voice vi_VN-vais1000-medium).

Gemma writes text; this turns the Mentor's lines into speech on the same laptop, offline. The voice model lives
outside the repo (EscapeFire_AI/models/piper, or ESCAPEFIRE_TTS_MODEL). Dataset of the voice: VAIS-1000 (CC BY 4.0).

The same key (first 16 hex digits of the MD5 of the UTF-8 text, trimmed) names the files of the headset's voice pack,
so a line rendered once is never synthesised again.
"""

from __future__ import annotations

import hashlib
import io
import os
import threading
import wave
from pathlib import Path

DEFAULT_MODEL = Path(__file__).resolve().parents[3] / "EscapeFire_AI" / "models" / "piper" / "vi_VN-vais1000-medium.onnx"


def line_key(text: str) -> str:
    return hashlib.md5(text.strip().encode("utf-8")).hexdigest()[:16]


class Speaker:
    """Interface: text -> WAV bytes."""

    available = True

    def wav(self, text: str) -> bytes:  # pragma: no cover - interface
        raise NotImplementedError


class PiperSpeaker(Speaker):
    def __init__(self, model: Path | None = None, max_cache: int = 512):
        self.model = Path(os.getenv("ESCAPEFIRE_TTS_MODEL", model or DEFAULT_MODEL))
        self._voice = None
        self._lock = threading.Lock()
        self._cache: dict[str, bytes] = {}
        self._max = max_cache

    @property
    def available(self) -> bool:  # type: ignore[override]
        return self.model.exists()

    def _load(self):
        if self._voice is None:
            from piper import PiperVoice  # imported lazily: the server runs without the voice too
            self._voice = PiperVoice.load(str(self.model))
        return self._voice

    def wav(self, text: str) -> bytes:
        key = line_key(text)
        if key in self._cache:
            return self._cache[key]
        with self._lock:  # onnxruntime session: one synthesis at a time
            voice = self._load()
            buf = io.BytesIO()
            with wave.open(buf, "wb") as w:
                voice.synthesize_wav(text.strip(), w)
        data = buf.getvalue()
        if len(self._cache) >= self._max:
            self._cache.pop(next(iter(self._cache)))
        self._cache[key] = data
        return data


class SilentSpeaker(Speaker):
    """Tests / no voice installed: a short silent WAV (or unavailable)."""

    def __init__(self, available: bool = True):
        self.available = available
        self.calls: list[str] = []

    def wav(self, text: str) -> bytes:
        self.calls.append(text)
        buf = io.BytesIO()
        with wave.open(buf, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(22050)
            w.writeframes(b"\x00\x00" * 2205)
        return buf.getvalue()
