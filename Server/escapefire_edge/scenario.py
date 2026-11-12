"""Prompt -> ScenarioSpec (plan V4.1-V4.3): a teacher types a sentence, the LLM writes a room spec from the shared
catalog, the Tier-1 validator checks it against PCCC / layout rules, one repair round, then a safe template fallback.

ScenarioSpec:
  {"title": "...", "room_type": "library", "width_m": 10, "depth_m": 8,
   "exits": [{"wall": "south", "position": 0.5}],
   "furniture": [{"category": "bookshelf", "count": 6}, ...],
   "fire": {"source": "bookshelf", "class": "A"},
   "extinguishers": [{"type": "WaterFoam", "count": 1}],
   "npcs": [{"role": "weak"}],
   "learning_goals": ["R03_RIGHT_AGENT"], "level": 1}

The headset re-checks the spec (ScenarioValidator.cs, same error codes, fixtures in tests/fixtures/scenario_cases.json)
and then the real layout: every free cell, the fire, the NPCs and the extinguishers must reach an exit (grid path).
"""

from __future__ import annotations

import asyncio
import copy
import time
import unicodedata

from pydantic import BaseModel, Field

from . import catalog
from .challenge import extract_json
from .llm import LLMClient

TITLE_MAX = 60


def _num(value) -> float | None:
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def validate_scenario(spec: dict | None) -> list[str]:
    """Returns error codes (empty list = valid). Order matters: the first error is compared with the headset."""
    if not isinstance(spec, dict):
        return ["not_object"]
    cfg = catalog.scenario()
    errors: list[str] = []

    room = str(spec.get("room_type", "")).strip().lower()
    if room not in cfg["room_types"]:
        return [f"unknown_room_type:{room or '<missing>'}"]

    w, d = _num(spec.get("width_m")), _num(spec.get("depth_m"))
    if w is None or not cfg["width_m"]["min"] <= w <= cfg["width_m"]["max"]:
        errors.append("bad_width")
    if d is None or not cfg["depth_m"]["min"] <= d <= cfg["depth_m"]["max"]:
        errors.append("bad_depth")
    area = (w or 0) * (d or 0)

    exits = spec.get("exits")
    if not isinstance(exits, list) or not exits:
        errors.append("no_exit")
        exits = []
    elif len(exits) > cfg["max_exits"]:
        errors.append("too_many_exits")
    seen = set()
    for e in exits:
        wall = str((e or {}).get("wall", "")).lower() if isinstance(e, dict) else ""
        pos = _num(e.get("position")) if isinstance(e, dict) else None
        if wall not in cfg["walls"] or pos is None or not 0.1 <= pos <= 0.9:
            errors.append("bad_exit")
            continue
        if (wall, round(pos, 1)) in seen:
            errors.append("duplicate_exit")
        seen.add((wall, round(pos, 1)))
    if area > cfg["two_exits_above_m2"] and len(exits) < 2:
        errors.append("needs_two_exits")

    allowed = cfg["room_types"][room]["categories"]
    cover = 0.0
    furniture = spec.get("furniture") or []
    if not isinstance(furniture, list):
        errors.append("bad_furniture")
        furniture = []
    for f in furniture:
        cat = str((f or {}).get("category", "")).lower() if isinstance(f, dict) else ""
        count = _num(f.get("count")) if isinstance(f, dict) else None
        if cat not in cfg["categories"]:
            errors.append(f"unknown_category:{cat or '<missing>'}")
            continue
        if cat not in allowed:
            errors.append(f"category_not_in_room:{cat}")
            continue
        if count is None or count < 1 or count > 20 or count != int(count):
            errors.append(f"bad_count:{cat}")
            continue
        c = cfg["categories"][cat]
        cover += c["w"] * c["d"] * count
    if area > 0 and cover > cfg["max_furniture_cover"] * area:
        errors.append("too_crowded")

    fire = spec.get("fire")
    fire_class = ""
    if not isinstance(fire, dict):
        errors.append("no_fire")
    else:
        source = str(fire.get("source", "")).lower()
        fire_class = str(fire.get("class", "")).upper()
        if source not in cfg["fire_sources"]:
            errors.append(f"unknown_fire_source:{source or '<missing>'}")
        elif source not in allowed:
            errors.append(f"fire_source_not_in_room:{source}")
        elif fire_class != cfg["fire_sources"][source]:
            errors.append(f"wrong_fire_class:{source}={cfg['fire_sources'][source]}")

    ext = spec.get("extinguishers")
    if not isinstance(ext, list) or not ext:
        errors.append("no_extinguisher")
        ext = []
    suitable = False
    for x in ext:
        kind = str((x or {}).get("type", "")) if isinstance(x, dict) else ""
        count = _num(x.get("count", 1)) if isinstance(x, dict) else None
        if kind not in cfg["extinguishers"]:
            errors.append(f"unknown_extinguisher:{kind or '<missing>'}")
            continue
        if count is None or not 1 <= count <= 3:
            errors.append(f"bad_count:{kind}")
            continue
        if fire_class in cfg["extinguishers"][kind]["classes"]:
            suitable = True
    if ext and fire_class and not suitable:
        errors.append("no_suitable_extinguisher")

    npcs = spec.get("npcs") or []
    if not isinstance(npcs, list) or len(npcs) > cfg["max_npcs"]:
        errors.append("too_many_npcs")
        npcs = []
    for n in npcs:
        role = str((n or {}).get("role", "")).lower() if isinstance(n, dict) else ""
        if role not in cfg["npc_roles"]:
            errors.append(f"unknown_npc_role:{role or '<missing>'}")

    goals = spec.get("learning_goals") or []
    if not isinstance(goals, list) or not goals:
        errors.append("no_learning_goal")
    else:
        for g in goals:
            if str(g).upper() not in catalog.rules():
                errors.append(f"unknown_rule:{g}")

    level = _num(spec.get("level", 1))
    if level is None or level not in (1, 2, 3):
        errors.append("bad_level")
    title = str(spec.get("title", "")).strip()
    if not title or len(title) > TITLE_MAX:
        errors.append("bad_title")
    return errors


def normalize(spec: dict) -> dict:
    """Canonical casing so the headset gets exactly what the validator accepted."""
    s = copy.deepcopy(spec)
    s["room_type"] = str(s["room_type"]).lower()
    s["fire"]["source"] = str(s["fire"]["source"]).lower()
    s["fire"]["class"] = str(s["fire"]["class"]).upper()
    for f in s.get("furniture", []):
        f["category"] = str(f["category"]).lower()
        f["count"] = int(f["count"])
    for x in s["extinguishers"]:
        x["count"] = int(x.get("count", 1))
    s["npcs"] = [{"role": str(n["role"]).lower()} for n in s.get("npcs") or []]
    s["learning_goals"] = [str(g).upper() for g in s["learning_goals"]]
    s["level"] = int(s.get("level", 1))
    return s


# ---------------------------------------------------------------------------------------------------- templates

TEMPLATES: dict[str, dict] = {
    "lab": {"title": "Phòng máy tính — cháy tủ server", "room_type": "lab", "width_m": 10, "depth_m": 8,
            "exits": [{"wall": "south", "position": 0.5}, {"wall": "north", "position": 0.8}],
            "furniture": [{"category": "computer_desk", "count": 6}, {"category": "chair", "count": 6},
                          {"category": "server_rack", "count": 2}, {"category": "cabinet", "count": 1}],
            "fire": {"source": "server_rack", "class": "E"}, "extinguishers": [{"type": "CO2", "count": 1}],
            "npcs": [{"role": "weak"}], "learning_goals": ["R02_POWER_OFF_FIRST", "R03_RIGHT_AGENT"], "level": 1},
    "library": {"title": "Thư viện — cháy giá sách", "room_type": "library", "width_m": 12, "depth_m": 9,
                "exits": [{"wall": "south", "position": 0.3}, {"wall": "east", "position": 0.6}],
                "furniture": [{"category": "bookshelf", "count": 6}, {"category": "table", "count": 3},
                              {"category": "chair", "count": 8}],
                "fire": {"source": "bookshelf", "class": "A"}, "extinguishers": [{"type": "WaterFoam", "count": 1}],
                "npcs": [{"role": "panic"}], "learning_goals": ["R03_RIGHT_AGENT", "R07_STAY_LOW"], "level": 1},
    "office": {"title": "Văn phòng — cháy máy in", "room_type": "office", "width_m": 9, "depth_m": 7,
               "exits": [{"wall": "west", "position": 0.5}, {"wall": "east", "position": 0.5}],
               "furniture": [{"category": "desk", "count": 4}, {"category": "chair", "count": 4},
                             {"category": "printer", "count": 1}, {"category": "cabinet", "count": 2}],
               "fire": {"source": "printer", "class": "E"}, "extinguishers": [{"type": "CO2", "count": 1}],
               "npcs": [], "learning_goals": ["R02_POWER_OFF_FIRST"], "level": 1},
    "canteen": {"title": "Căng-tin — cháy bếp", "room_type": "canteen", "width_m": 12, "depth_m": 10,
                "exits": [{"wall": "south", "position": 0.5}, {"wall": "north", "position": 0.5}],
                "furniture": [{"category": "table", "count": 4}, {"category": "chair", "count": 10},
                              {"category": "kitchen_counter", "count": 2}, {"category": "fridge", "count": 1}],
                "fire": {"source": "kitchen_counter", "class": "B"}, "extinguishers": [{"type": "CO2", "count": 1}],
                "npcs": [{"role": "weak"}], "learning_goals": ["R03_RIGHT_AGENT"], "level": 2},
    "workshop": {"title": "Xưởng — cháy tủ hóa chất", "room_type": "workshop", "width_m": 11, "depth_m": 8,
                 "exits": [{"wall": "south", "position": 0.5}, {"wall": "west", "position": 0.4}],
                 "furniture": [{"category": "drafting_table", "count": 3}, {"category": "chemical_cabinet", "count": 2},
                               {"category": "cardboard_boxes", "count": 3}, {"category": "chair", "count": 3}],
                 "fire": {"source": "chemical_cabinet", "class": "B"}, "extinguishers": [{"type": "CO2", "count": 1}],
                 "npcs": [{"role": "unconscious"}], "learning_goals": ["R03_RIGHT_AGENT", "R09_RESCUE_KEEP_LOW"], "level": 2},
}

_KEYWORDS = {
    "library": ("thu vien", "sach", "library"),
    "canteen": ("can tin", "cang tin", "bep", "nha an", "canteen", "kitchen"),
    "workshop": ("xuong", "hoa chat", "workshop", "co khi"),
    "office": ("van phong", "office", "may in"),
    "lab": ("phong may", "lab", "server", "thi nghiem", "may tinh"),
}


def _plain(text: str) -> str:
    t = unicodedata.normalize("NFD", text.lower().replace("đ", "d"))
    return "".join(c for c in t if unicodedata.category(c) != "Mn")


def template_for(prompt: str) -> dict:
    p = _plain(prompt)
    for room, words in _KEYWORDS.items():
        if any(w in p for w in words):
            return copy.deepcopy(TEMPLATES[room])
    return copy.deepcopy(TEMPLATES["lab"])


# ---------------------------------------------------------------------------------------------------- LLM generation

def _system_prompt() -> str:
    cfg = catalog.scenario()
    rooms = "\n".join(f"- {k} ({v['name']}): đồ đạc được dùng: {', '.join(v['categories'])}" for k, v in cfg["room_types"].items())
    sources = ", ".join(f"{k}={v}" for k, v in cfg["fire_sources"].items())
    ext = ", ".join(f"{k} dập lớp {'/'.join(v['classes'])}" for k, v in cfg["extinguishers"].items())
    classes = ", ".join(f"{k}: {v}" for k, v in cfg["fire_classes"].items())
    example = TEMPLATES["library"]
    import json as _json
    return f"""Bạn thiết kế PHÒNG DIỄN TẬP PCCC cho thực tế ảo theo mô tả của giảng viên. Chỉ dùng danh mục dưới đây.

LOẠI PHÒNG VÀ ĐỒ ĐẠC:
{rooms}
NGUỒN CHÁY (loại đồ = lớp cháy): {sources}
LỚP CHÁY: {classes}
BÌNH CHỮA CHÁY: {ext}
NPC: role weak (còn tỉnh, yếu), unconscious (bất tỉnh), panic (hoảng loạn); tối đa {cfg['max_npcs']}.
MỤC TIÊU HỌC (mã luật): {', '.join(catalog.rules())}

RÀNG BUỘC:
- Rộng {cfg['width_m']['min']}-{cfg['width_m']['max']} m, sâu {cfg['depth_m']['min']}-{cfg['depth_m']['max']} m; phòng > {cfg['two_exits_above_m2']} m² phải có >= 2 lối ra; lối ra: wall north|south|east|west, position 0.1-0.9.
- Đồ đạc chiếm tối đa {int(cfg['max_furniture_cover'] * 100)}% diện tích sàn; mỗi loại 1-20 cái; nguồn cháy phải là đồ có trong phòng.
- Phải có ít nhất 1 bình dập được đúng lớp cháy (cháy điện E: chỉ CO2). level 1-3. title <= {TITLE_MAX} ký tự.

Đầu ra là MỘT JSON đúng dạng ví dụ:
{_json.dumps(example, ensure_ascii=False)}"""


SCENARIO_SYSTEM = _system_prompt()


class ScenarioRequest(BaseModel):
    prompt: str = Field(min_length=3, max_length=500)
    level: int = Field(default=1, ge=1, le=3)


class ScenarioResponse(BaseModel):
    ok: bool
    spec: dict
    source: str  # "llm" | "llm_repaired" | "template"
    errors: list[str] = Field(default_factory=list)  # errors of the last rejected LLM attempt
    attempts: int = 0
    latency_ms: float = 0.0


async def generate_scenario(llm: LLMClient, req: ScenarioRequest, timeout_s: float = 30.0) -> ScenarioResponse:
    started = time.perf_counter()
    user = f"Mô tả của giảng viên: {req.prompt}\nCấp độ mong muốn: {req.level}\nHãy viết ScenarioSpec JSON."
    errors: list[str] = []
    attempts = 0
    for attempt in range(2):  # first try + one repair round with the validator's errors
        attempts += 1
        prompt = user if attempt == 0 else (
            user + f"\nBản trước bị từ chối vì: {', '.join(errors[:6])}. Sửa đúng các lỗi đó và trả lại JSON đầy đủ.")
        try:
            raw = await asyncio.wait_for(llm.complete(SCENARIO_SYSTEM, prompt), timeout=timeout_s)
        except asyncio.TimeoutError:
            errors = ["llm_timeout"]
            break
        except Exception as exc:  # Ollama down, ...
            errors = [f"llm_error:{type(exc).__name__}"]
            break
        spec = extract_json(raw)
        errors = validate_scenario(spec) if spec is not None else ["invalid_json"]
        if not errors:
            return ScenarioResponse(ok=True, spec=normalize(spec), source="llm" if attempt == 0 else "llm_repaired",
                                    attempts=attempts, latency_ms=_ms(started))
    spec = template_for(req.prompt)
    spec["level"] = req.level
    return ScenarioResponse(ok=True, spec=spec, source="template", errors=errors, attempts=attempts, latency_ms=_ms(started))


def _ms(started: float) -> float:
    return round((time.perf_counter() - started) * 1000.0, 1)
