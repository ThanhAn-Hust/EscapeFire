"""Static audit of a Unity scene YAML: hierarchy, world positions (approx), components, prefab instances."""
import re
import sys
import io
from collections import defaultdict

path = sys.argv[1]
text = io.open(path, encoding="utf-8").read()
docs = re.split(r"^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$", text, flags=re.M)

objs = {}
for i in range(1, len(docs), 3):
    cls, fid, body = int(docs[i]), docs[i + 1], docs[i + 2]
    objs[fid] = (cls, body)

CLASS = {1: "GameObject", 4: "Transform", 224: "RectTransform", 33: "MeshFilter", 23: "MeshRenderer", 137: "SkinnedMeshRenderer",
         65: "BoxCollider", 64: "MeshCollider", 136: "CapsuleCollider", 135: "SphereCollider", 54: "Rigidbody",
         114: "MonoBehaviour", 95: "Animator", 1001: "PrefabInstance", 108: "Light", 20: "Camera", 82: "AudioSource",
         143: "CharacterController", 198: "ParticleSystem", 199: "ParticleSystemRenderer"}

def f(body, key):
    m = re.search(rf"^\s*{key}: (.*)$", body, re.M)
    return m.group(1).strip() if m else None

def vec(s):
    if not s: return (0.0, 0.0, 0.0)
    m = re.findall(r"[xyz]: (-?[\d.e+-]+)", s)
    return tuple(float(v) for v in m[:3]) if len(m) >= 3 else (0.0, 0.0, 0.0)

gos = {}
for fid, (cls, body) in objs.items():
    if cls == 1:
        name = f(body, "m_Name") or "?"
        comps = re.findall(r"component: \{fileID: (-?\d+)\}", body)
        gos[fid] = {"name": name, "comps": comps, "active": f(body, "m_IsActive")}

transforms = {}
for fid, (cls, body) in objs.items():
    if cls in (4, 224):
        go = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
        father = re.search(r"m_Father: \{fileID: (-?\d+)\}", body)
        transforms[fid] = {
            "go": go.group(1) if go else None,
            "pos": vec(f(body, "m_LocalPosition")),
            "scale": vec(f(body, "m_LocalScale")),
            "father": father.group(1) if father else "0",
        }

go_to_tf = {t["go"]: k for k, t in transforms.items()}

def world_pos(tf_id, depth=0):
    t = transforms.get(tf_id)
    if not t or depth > 50: return (0, 0, 0)
    if t["father"] == "0" or t["father"] not in transforms:
        return t["pos"]
    p = world_pos(t["father"], depth + 1)
    ps = parent_scale(t["father"])
    return (p[0] + t["pos"][0] * ps[0], p[1] + t["pos"][1] * ps[1], p[2] + t["pos"][2] * ps[2])  # ignores rotation

def parent_scale(tf_id, depth=0):
    t = transforms.get(tf_id)
    if not t or depth > 50: return (1, 1, 1)
    s = t["scale"]
    if t["father"] == "0" or t["father"] not in transforms: return s
    ps = parent_scale(t["father"], depth + 1)
    return (s[0] * ps[0], s[1] * ps[1], s[2] * ps[2])

def path_of(tf_id):
    names = []
    cur = tf_id
    for _ in range(50):
        t = transforms.get(cur)
        if not t: break
        names.append(gos.get(t["go"], {}).get("name", "?"))
        if t["father"] == "0": break
        cur = t["father"]
    return "/".join(reversed(names))

def comp_kinds(go):
    kinds = []
    for c in go["comps"]:
        cls, body = objs.get(c, (None, ""))
        k = CLASS.get(cls, str(cls))
        if cls == 114:
            g = re.search(r"m_Script: \{fileID: \d+, guid: ([0-9a-f]+)", body)
            k = "MB:" + (g.group(1)[:8] if g else "MISSING")
            if g is None or "guid: 0000000000000000" in body: k = "MB:MISSING_SCRIPT"
        if cls == 33:
            m = re.search(r"m_Mesh: \{fileID: (-?\d+)(?:, guid: ([0-9a-f]+))?", body)
            k = f"MeshFilter({'builtin' if m and m.group(2) in (None,'0000000000000000e000000000000000') else 'asset'}:{m.group(1) if m else '?'})"
        if cls == 54:
            k = "Rigidbody(kin)" if f(body, "m_IsKinematic") == "1" else "Rigidbody(dyn)"
        kinds.append(k)
    return kinds

mode = sys.argv[2] if len(sys.argv) > 2 else "all"
pattern = re.compile(sys.argv[3], re.I) if len(sys.argv) > 3 else None

print(f"GameObjects: {len(gos)}, PrefabInstances: {sum(1 for c,_ in objs.values() if c==1001)}")
for gid, go in gos.items():
    tf = go_to_tf.get(gid)
    p = path_of(tf) if tf else go["name"]
    if pattern and not pattern.search(p): continue
    wp = world_pos(tf) if tf else (0, 0, 0)
    kinds = comp_kinds(go)
    if mode == "roots" and transforms.get(tf, {}).get("father") not in ("0", None): continue
    print(f"{p} | pos~({wp[0]:.2f},{wp[1]:.2f},{wp[2]:.2f}) | active={go['active']} | {', '.join(k for k in kinds if k not in ('Transform',))}")

# prefab instances
for fid, (cls, body) in objs.items():
    if cls == 1001:
        src = re.search(r"m_SourcePrefab: \{fileID: \d+, guid: ([0-9a-f]+)", body)
        name = re.search(r"propertyPath: m_Name\s*\n\s*value: (.*)", body)
        print(f"PREFAB {name.group(1) if name else '?'} <- {src.group(1) if src else '?'}")
