"""Edge TTS endpoint (Piper on the laptop). The real voice is exercised only if installed."""
import pytest
from fastapi.testclient import TestClient

from escapefire_edge.app import create_app
from escapefire_edge.llm import ScriptedLLM
from escapefire_edge.tts import PiperSpeaker, SilentSpeaker, line_key


def client(speaker):
    return TestClient(create_app(ScriptedLLM(["{}"]), log_dir=None, speaker=speaker))


def test_tts_returns_wav_and_key():
    sp = SilentSpeaker()
    r = client(sp).post("/tts", json={"text": "Có cháy! Ấn nút báo cháy."})
    assert r.status_code == 200
    assert r.headers["content-type"] == "audio/wav"
    assert r.content[:4] == b"RIFF"
    assert r.headers["x-line-key"] == line_key("Có cháy! Ấn nút báo cháy.")
    assert sp.calls == ["Có cháy! Ấn nút báo cháy."]


def test_tts_unavailable_is_503_and_health_says_so():
    c = client(SilentSpeaker(available=False))
    assert c.post("/tts", json={"text": "xin chào"}).status_code == 503
    assert c.get("/health").json()["tts"] is False


def test_tts_rejects_empty_text():
    assert client(SilentSpeaker()).post("/tts", json={"text": ""}).status_code == 422


def test_line_key_matches_the_headset_rule():
    # C#: MD5(UTF-8(text.Trim())) hex, first 16 chars
    assert line_key("  Có cháy!  ") == line_key("Có cháy!")
    assert len(line_key("x")) == 16


@pytest.mark.skipif(not PiperSpeaker().available, reason="Piper voice not installed")
def test_real_piper_voice_speaks_vietnamese():
    data = PiperSpeaker().wav("Có cháy! Việc đầu tiên bạn phải làm là gì?")
    assert data[:4] == b"RIFF" and len(data) > 22050  # more than half a second of 22 kHz audio
