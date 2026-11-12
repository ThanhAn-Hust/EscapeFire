"""Shared V4 catalog (challenge pieces, room types, furniture categories, fire classes, rules).

The headset loads an identical copy from Assets/Resources/V4Catalog.json; tests/test_v4_catalog.py checks they match.
"""

from __future__ import annotations

import json
from functools import lru_cache
from pathlib import Path

CATALOG_PATH = Path(__file__).with_name("v4_catalog.json")


@lru_cache(maxsize=1)
def catalog() -> dict:
    return json.loads(CATALOG_PATH.read_text(encoding="utf-8"))


def rules() -> list[str]:
    return list(catalog()["rules"])


def pieces() -> dict:
    return catalog()["challenge"]["pieces"]


def scenario() -> dict:
    return catalog()["scenario"]
