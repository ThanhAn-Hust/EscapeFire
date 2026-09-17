# Expanded High-Rise University Computer Lab Level Design Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this high-rise lab level plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the small lab room into a modern, spacious 18m x 14m x 4.5m High-Rise University Server & Computer Lab with floor-to-ceiling city skyline windows, 12+ student workstations, a glass-partitioned Server Hub, and a progressive emergency event system.

**Architecture:** Enclosed 3D spatial grid built with URP Lit shaders and Synty POLYGON Office assets. Features a glass-panoramic West Wall overlooking a City Skyline Skybox, 3 workstation clusters, dedicated Server Hub zone, and automated C# progressive scenario controller (`LabProgressiveScenarioManager.cs`).

**Tech Stack:** Unity 2022.3 LTS, URP 14, XR Interaction Toolkit 3.4.1, TextMeshPro, Newtonsoft.Json.

## Global Constraints

- Room Dimensions: 18m (Width - X: -9.0m to +9.0m) x 14m (Depth - Z: -7.0m to +7.0m) x 4.5m (Height - Y: 0 to 4.5m).
- All static wall/floor colliders MUST be on `Layer: Environment`.
- West Wall MUST be constructed with transparent glass material (`Universal Render Pipeline/Lit` in Transparent/Glass mode) showing the City Horizon Skybox outside.
- All interactive items MUST have `Layer: GrabInteractable` or `Layer: UI`.
- XR Origin Start Position: `(0, 0, -4)` facing North `(0, 0, 1)`.

---

### High-Rise Lab 3D Spatial Layout Blueprint

```
+---------------------------------------------------------------------------------------------------+
|  [ZONE A: Server Hub (Glass Enclosure)]                              [NORTH WALL: Whiteboard]     |
|  Location: (X: +6.0m to +8.5m, Z: +4.0m to +6.5m)                   Location: (X: 0.0m, Z: +6.9m)|
|  * 3 Server Racks, Electrical Aptomat Panel (East Wall)                                           |
|  * Primary Fire Hazard Origin                                                                     |
|                                                                                                   |
| [WEST WALL: FULL GLASS WINDOWS - CITY SKYLINE PANORAMA]                                           |
| Location: (X: -8.9m, Y: 0 to 4.5m)                                                                |
| * Floor-to-ceiling glass wall looking out over city high-rise skyline                              |
|                                                                                                   |
|             [ZONE E: 3 STUDENT WORKSTATION CLUSTERS (12-16 PCs)]                                  |
|             Cluster 1 (Left):   (X: -4.5m, Z: -3m to +3m) - 4 Workstations                         |
|             Cluster 2 (Center): (X:  0.0m, Z: -3m to +3m) - 4 Workstations                         |
|             Cluster 3 (Right):  (X: +4.5m, Z: -3m to +1m) - 4 Workstations                         |
|                                                                                                   |
| [PLAYER SPAWN: XR Origin]                        [ZONE D: Double Exit Doors & Fire Alarm]         |
| Location: (X: 0.0m, Y: 0.0m, Z: -4.0m)            Location: (X: 0.0m, Z: -6.9m)                    |
| Facing: North (0, 0, 1)                          CO2 Extinguisher: (X: -2.0m, Z: -6.8m)          |
+---------------------------------------------------------------------------------------------------+
```

---

### Task 1: High-Rise Room Shell Construction (18m x 14m x 4.5m)

**Files:**
- Modify Scene: `EscapeFire/Assets/Scenes/LabEmergencyScene.unity`
- Create Script: `EscapeFire/Assets/Scripts/Level/HighRiseRoomBuilder.cs`

**Interfaces:**
- Consumes: Unity URP `Universal Render Pipeline/Lit` materials.
- Produces: `[ROOM_HIGHRISE_SHELL]` GameObject containing Floor (18x14m), 3 Solid Walls, 1 Glass Window Wall (West), Ceiling (4.5m).

- [ ] **Step 1: Write HighRiseRoomBuilder C# script**

```csharp
using UnityEngine;

namespace EscapeFire.Level
{
    public class HighRiseRoomBuilder : MonoBehaviour
    {
        public static void BuildHighRiseShell(GameObject parent)
        {
            // Floor (18m x 14m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_HighRise";
            floor.transform.parent = parent.transform;
            floor.transform.position = new Vector3(0, -0.1f, 0);
            floor.transform.localScale = new Vector3(18f, 0.2f, 14f);

            // North Wall (Z = +7m)
            GameObject wallN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallN.name = "Wall_North";
            wallN.transform.parent = parent.transform;
            wallN.transform.position = new Vector3(0, 2.25f, 7.0f);
            wallN.transform.localScale = new Vector3(18f, 4.5f, 0.2f);

            // South Wall (Z = -7m)
            GameObject wallS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallS.name = "Wall_South";
            wallS.transform.parent = parent.transform;
            wallS.transform.position = new Vector3(0, 2.25f, -7.0f);
            wallS.transform.localScale = new Vector3(18f, 4.5f, 0.2f);

            // East Wall (X = +9m)
            GameObject wallE = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallE.name = "Wall_East";
            wallE.transform.parent = parent.transform;
            wallE.transform.position = new Vector3(9.0f, 2.25f, 0);
            wallE.transform.localScale = new Vector3(0.2f, 4.5f, 14f);

            // West Wall - Panoramic Glass (X = -9m)
            GameObject wallW = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallW.name = "Wall_West_GlassPanorama";
            wallW.transform.parent = parent.transform;
            wallW.transform.position = new Vector3(-9.0f, 2.25f, 0);
            wallW.transform.localScale = new Vector3(0.1f, 4.5f, 14f);

            // Ceiling (Y = 4.5m)
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling_HighRise";
            ceiling.transform.parent = parent.transform;
            ceiling.transform.position = new Vector3(0, 4.6f, 0);
            ceiling.transform.localScale = new Vector3(18f, 0.2f, 14f);
        }
    }
}
```

- [ ] **Step 2: Execute script via Unity MCP to replace old room shell**

Run C# execution via `execute_code` to instantiate `[ROOM_HIGHRISE_SHELL]`.

- [ ] **Step 3: Save and Commit**

```bash
git add EscapeFire/Assets/Scripts/Level/ EscapeFire/Assets/Scenes/LabEmergencyScene.unity
git commit -m "feat(level): construct 18m x 14m x 4.5m high-rise room shell with West glass wall"
```

---

### Task 2: City Skyline Backdrop & Glass Window Material Setup

**Files:**
- Create Material: `EscapeFire/Assets/Materials/GlassPanoramaMaterial.mat`
- Create Material: `EscapeFire/Assets/Materials/CitySkylineBackdrop.mat`
- Modify Scene: `EscapeFire/Assets/Scenes/LabEmergencyScene.unity`

**Interfaces:**
- Consumes: URP Lit Transparent Glass Shader.
- Produces: Panoramic Glass Window Wall with outdoor city view.

- [ ] **Step 1: Write C# code to create Glass & City Skybox materials**

```csharp
using UnityEngine;
using UnityEditor;

public class CreateCitySkylineMaterials
{
    public static void CreateGlassAndSkyline()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        // 1. Glass Material (Transparent, Smooth, Refractive)
        Material glassMat = new Material(urpLit);
        glassMat.name = "GlassPanoramaMaterial";
        glassMat.SetOverrideTag("RenderType", "Transparent");
        glassMat.SetFloat("_Surface", 1); // 1 = Transparent
        glassMat.SetFloat("_Blend", 0);   // Alpha blend
        glassMat.color = new Color(0.8f, 0.9f, 1.0f, 0.25f); // Light blue tint transparent
        glassMat.SetFloat("_Smoothness", 0.95f);
        AssetDatabase.CreateAsset(glassMat, "Assets/Materials/GlassPanoramaMaterial.mat");

        // 2. City Skyline Backdrop Panel behind Glass Wall (X = -12m)
        GameObject skylineBackdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        skylineBackdrop.name = "City_Skyline_Backdrop";
        skylineBackdrop.transform.position = new Vector3(-12.0f, 5.0f, 0);
        skylineBackdrop.transform.localScale = new Vector3(25f, 15f, 1f);
        skylineBackdrop.transform.eulerAngles = new Vector3(0, 90, 0);

        Material skylineMat = new Material(urpLit);
        skylineMat.color = new Color(0.4f, 0.6f, 0.8f); // High-rise sky horizon
        skylineBackdrop.GetComponent<MeshRenderer>().material = skylineMat;
    }
}
```

- [ ] **Step 2: Execute material creation via Unity MCP**

- [ ] **Step 3: Save and Commit**

```bash
git add EscapeFire/Assets/Materials/ EscapeFire/Assets/Scenes/LabEmergencyScene.unity
git commit -m "feat(level): add panoramic glass material and city skyline backdrop"
```

---

### Task 3: 3 Workstation Clusters (12-16 Student PC Desks)

**Files:**
- Modify Scene: `EscapeFire/Assets/Scenes/LabEmergencyScene.unity`

**Interfaces:**
- Consumes: `POLYGON Office` Desk/Chair/Monitor prefabs.
- Produces: `[LAB_WORKSTATIONS_CLUSTER_1]`, `[LAB_WORKSTATIONS_CLUSTER_2]`, `[LAB_WORKSTATIONS_CLUSTER_3]`.

- [ ] **Step 1: Write C# code to populate 12 student workstations in 3 clusters**

```csharp
using UnityEngine;

public class PopulateExpandedWorkstations
{
    public static void Create12Workstations(GameObject parent)
    {
        float[] xPositions = new float[] { -4.5f, 0.0f, 4.5f };
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        
        Material deskMat = new Material(urpLit);
        deskMat.color = new Color(0.25f, 0.25f, 0.28f);

        Material screenMat = new Material(urpLit);
        screenMat.color = new Color(0.1f, 0.35f, 0.55f);

        int count = 0;
        foreach (float xPos in xPositions)
        {
            GameObject cluster = new GameObject($"Workstation_Cluster_X{xPos}");
            cluster.transform.parent = parent.transform;

            for (int row = 0; row < 4; row++)
            {
                float zPos = -3.0f + (row * 1.8f);
                count++;

                GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                desk.name = $"Desk_{count}";
                desk.transform.parent = cluster.transform;
                desk.transform.position = new Vector3(xPos, 0.4f, zPos);
                desk.transform.localScale = new Vector3(1.3f, 0.8f, 0.75f);
                desk.GetComponent<MeshRenderer>().material = deskMat;

                GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                monitor.name = "PC_Monitor";
                monitor.transform.parent = desk.transform;
                monitor.transform.localPosition = new Vector3(0, 0.65f, 0.1f);
                monitor.transform.localScale = new Vector3(0.55f, 0.45f, 0.08f);
                monitor.GetComponent<MeshRenderer>().material = screenMat;
            }
        }
    }
}
```

- [ ] **Step 2: Execute script via Unity MCP to populate 12 workstations**

- [ ] **Step 3: Save and Commit**

```bash
git add EscapeFire/Assets/Scenes/LabEmergencyScene.unity
git commit -m "feat(level): populate 12 student PC workstations across 3 high-rise lab clusters"
```

---

### Task 4: Lab Progressive Scenario Controller (Phase 0 -> Phase 3)

**Files:**
- Create Script: `EscapeFire/Assets/Scripts/Scenario/LabProgressiveScenarioController.cs`

**Interfaces:**
- Consumes: Player trigger / timer input.
- Produces: `public enum ScenarioPhase { Phase0_Peaceful, Phase1_FlickerWarning, Phase2_FireSparks, Phase3_Evacuation }`.

- [ ] **Step 1: Write LabProgressiveScenarioController C# code**

```csharp
using System;
using UnityEngine;
using EscapeFire.Hazards;

namespace EscapeFire.Scenario
{
    public enum ScenarioPhase
    {
        Phase0_PeacefulPractice,
        Phase1_PowerFlickerWarning,
        Phase2_ServerExplosionFire,
        Phase3_SmokeEvacuation
    }

    public class LabProgressiveScenarioController : MonoBehaviour
    {
        [Header("Phase Timings")]
        [SerializeField] private float peacefulDurationSeconds = 30f;
        [SerializeField] private float warningDurationSeconds = 10f;

        [Header("References")]
        [SerializeField] private FireHazard serverFire;
        [SerializeField] private GameObject redEmergencyLight;
        [SerializeField] private AudioSource alarmAudioSource;

        public ScenarioPhase CurrentPhase { get; private set; } = ScenarioPhase.Phase0_PeacefulPractice;
        public event Action<ScenarioPhase> OnPhaseChanged;

        private float _timer = 0f;

        private void Start()
        {
            SetPhase(ScenarioPhase.Phase0_PeacefulPractice);
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (CurrentPhase == ScenarioPhase.Phase0_PeacefulPractice && _timer >= peacefulDurationSeconds)
            {
                SetPhase(ScenarioPhase.Phase1_PowerFlickerWarning);
            }
            else if (CurrentPhase == ScenarioPhase.Phase1_PowerFlickerWarning && _timer >= (peacefulDurationSeconds + warningDurationSeconds))
            {
                SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            }
        }

        public void SetPhase(ScenarioPhase newPhase)
        {
            CurrentPhase = newPhase;
            Debug.Log($"[ProgressiveScenario] Advanced to phase: {newPhase}");

            switch (newPhase)
            {
                case ScenarioPhase.Phase0_PeacefulPractice:
                    if (serverFire != null) serverFire.gameObject.SetActive(false);
                    if (redEmergencyLight != null) redEmergencyLight.SetActive(false);
                    break;

                case ScenarioPhase.Phase1_PowerFlickerWarning:
                    if (redEmergencyLight != null) redEmergencyLight.SetActive(true);
                    break;

                case ScenarioPhase.Phase2_ServerExplosionFire:
                    if (serverFire != null)
                    {
                        serverFire.gameObject.SetActive(true);
                        serverFire.ResetFire();
                    }
                    if (alarmAudioSource != null && !alarmAudioSource.isPlaying)
                    {
                        alarmAudioSource.Play();
                    }
                    break;
            }

            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}
```

- [ ] **Step 2: Commit Scenario Controller**

```bash
git add EscapeFire/Assets/Scripts/Scenario/
git commit -m "feat(scenario): implement LabProgressiveScenarioController for phase 0 to phase 3 event flow"
```

---

## Plan Complete and Handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-03-expanded-highrise-lab-level-design.md`. Two execution options:

1. **Subagent-Driven (recommended)** - Dispatch a fresh subagent per task, review between tasks, fast iteration
2. **Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
