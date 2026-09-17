# VR PCCC Computer Lab Scene Layout & Level Design Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this scene layout task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide an exact 3D spatial layout, coordinate map, prefab mapping, lighting setup, and collision zone blueprint for constructing the VR PCCC Computer Lab Scene in Unity (`Assets/Scenes/LabEmergencyScene.unity`).

**Architecture:** Room grid centered at World Origin `(0, 0, 0)`. Prefab placement mapped strictly to imported asset paths (`PolygonOffice`, `Assets_ServerRacks`, `OccaSoftware`, `Real Fire & Smoke`). Uses URP Lighting, Unity Physics Colliders, and XR Interaction Toolkit Interactables.

**Tech Stack:** Unity 2022.3 URP, XR Interaction Toolkit 3.4.1, ProBuilder / Synty Modular Mesh.

## Global Constraints

- Room Dimensions: 10m (Width - X) x 8m (Depth - Z) x 3.5m (Height - Y).
- All static wall/floor colliders MUST have `Static` flags set and `Layer: Environment`.
- All interactive items MUST have `Layer: GrabInteractable` or `Layer: UI`.
- XR Origin Start Position: `(0, 0, -2)` facing North `(0, 0, 1)`.

---

### Room Spatial Coordinates & Prefab Mapping

```
+-----------------------------------------------------------------------+
|  [ZONE A: Server Rack Cluster]                  [ZONE B: Electrical] |
|  Location: (X: +3.5m, Z: +3.0m)                 Panel (Aptomat)]     |
|  * Fire Origin Point                            Location: (+4.8m, +1m)|
|                                                                       |
|             [ZONE E: Student Workstation Rows]                        |
|             Row 1: (X: -1.5m, Z: +1.0m)                               |
|             Row 2: (X: +1.5m, Z: -0.5m)                               |
|                                                                       |
|  [ZONE C2: Water Extinguisher]                                        |
|  (Decoy - West Wall X: -4.5m)                                         |
|                                                                       |
| [PLAYER SPAWN: XR Origin]         [ZONE D: Exit Door & CO2 Ext.]     |
| Location: (0.0m, 0.0m, -2.0m)     Door: (0.0m, -3.9m)                 |
|                                   CO2 Ext: (-1.2m, -3.8m)              |
+-----------------------------------------------------------------------+
```

---

### Task 1: Environment Shell & Room Architecture Construction

**Files:**
- Create Scene: `EscapeFire/Assets/Scenes/LabEmergencyScene.unity`
- Prefabs Used:
  - Floor: `Assets/PolygonOffice/Prefabs/Environments/SM_Env_Floor_01.prefab`
  - Walls: `Assets/PolygonOffice/Prefabs/Environments/SM_Env_Wall_01.prefab`
  - Door Frame: `Assets/PolygonOffice/Prefabs/Environments/SM_Env_Door_Exit_01.prefab`

**Interfaces:**
- Consumes: Unity URP Default Materials.
- Produces: Base Room Shell GameObject `[ENVIRONMENT_SHELL]`.

- [ ] **Step 1: Create new URP Scene `LabEmergencyScene.unity`**

Open Unity Editor, File -> New Scene -> Select URP Basic (Built-in Lighting), Save to `Assets/Scenes/LabEmergencyScene.unity`.

- [ ] **Step 2: Assemble Floor & Outer Walls**

Instantiate Floor at `(0, 0, 0)` scaled to 10m x 8m. Position North Wall at `Z = +4m`, South Wall at `Z = -4m`, East Wall at `X = +5m`, West Wall at `X = -5m`. Add BoxColliders to all walls.

- [ ] **Step 3: Commit Scene Setup**

```bash
git add EscapeFire/Assets/Scenes/LabEmergencyScene.unity
git commit -m "feat(level): construct base environment shell for lab scene"
```

---

### Task 2: Zone A & B Object Placement (Server Racks & Electrical Panel)

**Files:**
- Modify Scene: `EscapeFire/Assets/Scenes/LabEmergencyScene.unity`
- Prefabs Used:
  - Server Rack: `Assets/Assets_ServerRacks/Prefabs/` or `Assets/Technical laboratory designer-V2_Server/Prefabs/ServerRack.prefab`
  - Circuit Breaker Panel: `Assets/Technical laboratory designer-V2_Server/Prefabs/ElectricalPanel.prefab`
  - Fire Particle: `Assets/Real Fire & Smoke/Prefabs/Fire_Medium.prefab`

**Interfaces:**
- Consumes: `CircuitBreakerSwitch.cs`, `FireHazard.cs`.
- Produces: `[ZONE_A_SERVER_RACK]`, `[ZONE_B_CIRCUIT_BREAKER]`.

- [ ] **Step 1: Place Server Rack Cluster at Zone A**

Place Server Rack 1 at `Position: (X: 3.5, Y: 0.0, Z: 3.0)`, Rotation: `(0, 180, 0)`. Attach `FireHazard.cs` script to the top rack slot. Place inactive `Fire_Medium` particle prefab under it.

- [ ] **Step 2: Mount Circuit Breaker Panel on East Wall (Zone B)**

Place `ElectricalPanel.prefab` at `Position: (X: 4.8, Y: 1.5, Z: 1.0)`, Rotation: `(0, -90, 0)`. Attach `CircuitBreakerSwitch.cs` script to the switch handle.

- [ ] **Step 3: Commit Object Placement**

```bash
git add EscapeFire/Assets/Scenes/LabEmergencyScene.unity
git commit -m "feat(level): place server rack cluster and circuit breaker panel"
```

---

### Task 3: Zone C & D Placement (Fire Extinguishers & Exit Door)

**Files:**
- Modify Scene: `EscapeFire/Assets/Scenes/LabEmergencyScene.unity`
- Prefabs Used:
  - CO2 Extinguisher: `Assets/PolygonOffice/Prefabs/Props/SM_Prop_FireExtinguisher_01.prefab`
  - Fire Alarm Button: `Assets/PolygonOffice/Prefabs/Props/SM_Prop_FireAlarm_01.prefab`

**Interfaces:**
- Consumes: `FireExtinguisher.cs`, `XRGrabInteractable`.
- Produces: `[ZONE_C_CO2_EXTINGUISHER]`, `[ZONE_D_EXIT_DOOR]`.

- [ ] **Step 1: Place CO2 Extinguisher near Exit Door (Zone C1)**

Mount CO2 Extinguisher bracket on South Wall at `Position: (X: -1.2, Y: 1.2, Z: -3.8)`. Attach `FireExtinguisher.cs` (`Type = CO2`) and `XRGrabInteractable` to the bottle.

- [ ] **Step 2: Place Decoy Water Extinguisher on West Wall (Zone C2)**

Mount Water Extinguisher at `Position: (X: -4.5, Y: 1.2, Z: 0.0)`. Attach `FireExtinguisher.cs` (`Type = WaterFoam`).

- [ ] **Step 3: Mount Fire Alarm Button & Exit Door (Zone D)**

Place Exit Door at `Position: (X: 0.0, Y: 0.0, Z: -3.9)`. Place Fire Alarm Button on right wall of door at `Position: (X: 0.8, Y: 1.4, Z: -3.8)`.

- [ ] **Step 4: Commit Safety Props**

```bash
git add EscapeFire/Assets/Scenes/LabEmergencyScene.unity
git commit -m "feat(level): add fire extinguishers and exit door safety props"
```

---

### Task 4: Player Spawn & Atmospheric Lighting Setup

**Files:**
- Modify Scene: `EscapeFire/Assets/Scenes/LabEmergencyScene.unity`
- Prefabs Used:
  - XR Origin Rig: `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab`
  - Volumetric Smoke: `Assets/OccaSoftware/ResponsiveSmokes/Prefabs/SmokeVolume.prefab`

**Interfaces:**
- Consumes: `VRHeightDetector.cs`, `OxygenSystem.cs`.
- Produces: `[XR_ORIGIN_PLAYER]`, `[LIGHTING_AND_VFX]`.

- [ ] **Step 1: Instantiate XR Origin (Player Rig)**

Place `XR Origin` prefab at `Position: (0.0, 0.0, -2.0)`, Rotation: `(0, 0, 0)`. Attach `VRHeightDetector.cs` and `OxygenSystem.cs` to the Camera Offset / Head.

- [ ] **Step 2: Setup Emergency Lighting & Volumetric Smoke Layer**

Add Ceiling Main Light (Warm White, Intensity 1.2) and Emergency Red Spotlights (Intensity 0.0 initial, turns ON during fire). Place `SmokeVolume.prefab` at ceiling height `(0.0, 3.2, 0.0)`.

- [ ] **Step 3: Commit Player Rig & Atmosphere**

```bash
git add EscapeFire/Assets/Scenes/LabEmergencyScene.unity
git commit -m "feat(level): setup XR origin player rig and emergency volumetric lighting"
```

---

## Plan Complete and Handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-03-scene-layout-and-level-design.md`. Two execution options:

1. **Subagent-Driven (recommended)** - Dispatch a fresh subagent per task, review between tasks, fast iteration
2. **Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
