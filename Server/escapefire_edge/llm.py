"""LLM backends. Default: local Ollama (Gemma). A scripted fake is provided for tests and offline demos."""

from __future__ import annotations

import os
from typing import Protocol

import httpx


class LLMClient(Protocol):
    async def complete(self, system: str, user: str) -> str: ...


class OllamaClient:
    def __init__(
        self,
        model: str | None = None,
        base_url: str | None = None,
        timeout_s: float = 8.0,
    ) -> None:
        self.model = model or os.getenv("ESCAPEFIRE_LLM_MODEL", "gemma3:4b")
        self.base_url = (base_url or os.getenv("OLLAMA_HOST", "http://127.0.0.1:11434")).rstrip("/")
        self.timeout_s = timeout_s

    async def complete(self, system: str, user: str) -> str:
        payload = {
            "model": self.model,
            "stream": False,
            "format": "json",
            "options": {"temperature": 0.2},
            "messages": [
                {"role": "system", "content": system},
                {"role": "user", "content": user},
            ],
        }
        async with httpx.AsyncClient(timeout=self.timeout_s) as client:
            response = await client.post(f"{self.base_url}/api/chat", json=payload)
            response.raise_for_status()
            return response.json().get("message", {}).get("content", "")


class ScriptedLLM:
    """Returns canned outputs in order (last one repeats). Used by tests and `--fake-llm` demos."""

    def __init__(self, outputs: list[str]) -> None:
        if not outputs:
            raise ValueError("ScriptedLLM needs at least one output")
        self._outputs = outputs
        self._index = 0
        self.calls = 0

    async def complete(self, system: str, user: str) -> str:
        self.calls += 1
        output = self._outputs[min(self._index, len(self._outputs) - 1)]
        self._index += 1
        return output
