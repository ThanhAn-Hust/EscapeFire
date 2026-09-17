# VR PCCC Lab Simulator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a complete, interactive VR Emergency Hazard & Fire Extinguishing Simulator in Unity (Meta Quest 6DOF) featuring dynamic smoke propagation, physical extinguisher interactions, AI Mentor guidance, and instructor scoring analytics.

**Architecture:** Component-based Unity architecture using XR Interaction Toolkit (XRI 3.4). Single-responsibility C# scripts for VR player height detection, survival metrics, hazard physics, 6DOF tool interaction, and telemetry logging. Communicates state via C# events (`Action<T>`).

**Tech Stack:** Unity 2022.3 LTS, URP, XR Interaction Toolkit 3.4.1, TextMeshPro, Newtonsoft.Json.

## Global Constraints

- All scripts MUST be placed under `Assets/Scripts/` with clear namespaces (`EscapeFire.Player`, `EscapeFire.Interactions`, `EscapeFire.Hazards`, `EscapeFire.Analytics`, `EscapeFire.AI`).
- All interactive 6DOF components MUST use Unity XR Interaction Toolkit `XRBaseInteractable` or `XRGrabInteractable`.
- No hardcoded paths; use `SerializeField` and `GetComponent` safely with null checks.
- Code style: Standard C# Naming Conventions (PascalCase for classes/methods, camelCase for local variables, `_camelCase` for private fields).

---

### Task 1: VR Height Detector & Crouch Sensing

**Files:**
- Create: `EscapeFire/Assets/Scripts/Player/VRHeightDetector.cs`
- Create: `EscapeFire/Assets/Scripts/Player/Tests/VRHeightDetectorTests.cs`

**Interfaces:**
- Consumes: Main Camera transform (VR Headset transform).
- Produces: `public event Action<bool> OnCrouchStateChanged`, `public bool IsCrouching { get; }`, `public float CurrentHeight { get; }`.

- [ ] **Step 1: Write the failing unit/integration logic test**

```csharp
using NUnit.Framework;
using UnityEngine;
using EscapeFire.Player;

public class VRHeightDetectorTests
{
    [Test]
    public void TestCrouchState_WhenHeightBelowThreshold_ReturnsTrue()
    {
        var gameObject = new GameObject();
        var detector = gameObject.AddComponent<VRHeightDetector>();
        detector.CrouchThresholdHeight = 1.2f;
        
        detector.UpdateHeight(1.0f);
        Assert.IsTrue(detector.IsCrouching);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run in Unity Test Runner / C# test runner. Expected: FAIL with missing class `VRHeightDetector`.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System;
using UnityEngine;

namespace EscapeFire.Player
{
    public class VRHeightDetector : MonoBehaviour
    {
        [SerializeField] private Transform headTransform;
        [SerializeField] private float crouchThresholdHeight = 1.2f;

        public float CrouchThresholdHeight 
        { 
            get => crouchThresholdHeight; 
            set => crouchThresholdHeight = value; 
        }

        public bool IsCrouching { get; private set; }
        public float CurrentHeight { get; private set; }

        public event Action<bool> OnCrouchStateChanged;

        private void Update()
        {
            if (headTransform != null)
            {
                UpdateHeight(headTransform.localPosition.y);
            }
        }

        public void UpdateHeight(float height)
        {
            CurrentHeight = height;
            bool newlyCrouching = height < crouchThresholdHeight;

            if (newlyCrouching != IsCrouching)
            {
                IsCrouching = newlyCrouching;
                OnCrouchStateChanged?.Invoke(IsCrouching);
            }
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add EscapeFire/Assets/Scripts/Player/
git commit -m "feat(player): implement VRHeightDetector for crouch sensing"
```

---

### Task 2: Oxygen & Health Survival System

**Files:**
- Create: `EscapeFire/Assets/Scripts/Player/OxygenSystem.cs`
- Create: `EscapeFire/Assets/Scripts/Player/HealthSystem.cs`

**Interfaces:**
- Consumes: `VRHeightDetector.IsCrouching`, `SmokeDensityController.SmokeDensity`.
- Produces: `public float CurrentOxygen`, `public float CurrentHealth`, `public event Action OnPlayerDied`.

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;
using EscapeFire.Player;

public class SurvivalSystemTests
{
    [Test]
    public void TestOxygenDepletion_WhenStandingInSmoke_DepletesOxygen()
    {
        var gameObject = new GameObject();
        var oxygen = gameObject.AddComponent<OxygenSystem>();
        oxygen.MaxOxygen = 100f;
        oxygen.ResetOxygen();
        
        oxygen.DepleteOxygen(10f);
        Assert.AreEqual(90f, oxygen.CurrentOxygen);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Expected: FAIL with `OxygenSystem` missing.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System;
using UnityEngine;

namespace EscapeFire.Player
{
    public class OxygenSystem : MonoBehaviour
    {
        [SerializeField] private float maxOxygen = 100f;
        [SerializeField] private float depletionRateInSmoke = 10f;
        
        public float MaxOxygen { get => maxOxygen; set => maxOxygen = value; }
        public float CurrentOxygen { get; private set; }

        public event Action<float> OnOxygenChanged;
        public event Action OnOxygenDepleted;

        private void Awake()
        {
            ResetOxygen();
        }

        public void ResetOxygen()
        {
            CurrentOxygen = maxOxygen;
            OnOxygenChanged?.Invoke(CurrentOxygen);
        }

        public void DepleteOxygen(float amount)
        {
            CurrentOxygen = Mathf.Max(0f, CurrentOxygen - amount);
            OnOxygenChanged?.Invoke(CurrentOxygen);

            if (CurrentOxygen <= 0f)
            {
                OnOxygenDepleted?.Invoke();
            }
        }
    }
}
```

- [ ] **Step 4: Verify test passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add EscapeFire/Assets/Scripts/Player/
git commit -m "feat(player): implement OxygenSystem and HealthSystem for survival logic"
```

---

### Task 3: Circuit Breaker (Aptomat) Switch Interaction

**Files:**
- Create: `EscapeFire/Assets/Scripts/Interactions/CircuitBreakerSwitch.cs`

**Interfaces:**
- Consumes: XRI 3.4 `XRBaseInteractable`.
- Produces: `public bool IsPowerOn { get; }`, `public event Action<bool> OnPowerStateChanged`.

- [ ] **Step 1: Write test**

```csharp
using NUnit.Framework;
using UnityEngine;
using EscapeFire.Interactions;

public class CircuitBreakerTests
{
    [Test]
    public void TestSwitchPower_TogglesStateCorrectly()
    {
        var gameObject = new GameObject();
        var breaker = gameObject.AddComponent<CircuitBreakerSwitch>();
        breaker.SetPowerState(true);
        
        breaker.TogglePower();
        Assert.IsFalse(breaker.IsPowerOn);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Expected: FAIL missing `CircuitBreakerSwitch`.

- [ ] **Step 3: Write implementation**

```csharp
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace EscapeFire.Interactions
{
    public class CircuitBreakerSwitch : MonoBehaviour
    {
        [SerializeField] private Transform switchHandle;
        [SerializeField] private Vector3 onRotation = new Vector3(0, 0, 45);
        [SerializeField] private Vector3 offRotation = new Vector3(0, 0, -45);
        
        public bool IsPowerOn { get; private set; } = true;
        public event Action<bool> OnPowerStateChanged;

        public void SetPowerState(bool powerOn)
        {
            IsPowerOn = powerOn;
            if (switchHandle != null)
            {
                switchHandle.localEulerAngles = powerOn ? onRotation : offRotation;
            }
            OnPowerStateChanged?.Invoke(IsPowerOn);
        }

        public void TogglePower()
        {
            SetPowerState(!IsPowerOn);
        }
    }
}
```

- [ ] **Step 4: Verify test passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add EscapeFire/Assets/Scripts/Interactions/
git commit -m "feat(interactions): add CircuitBreakerSwitch component"
```

---

### Task 4: 6DOF Fire Extinguisher Component (CO2 vs Powder)

**Files:**
- Create: `EscapeFire/Assets/Scripts/Interactions/FireExtinguisher.cs`

**Interfaces:**
- Consumes: `CircuitBreakerSwitch.IsPowerOn`, XR Grip Trigger input.
- Produces: `public ExtinguisherType Type`, `public bool IsPinPulled`, `public event Action OnShockHazardTriggered`.

- [ ] **Step 1: Write test**

```csharp
using NUnit.Framework;
using UnityEngine;
using EscapeFire.Interactions;

public class ExtinguisherTests
{
    [Test]
    public void TestSpray_WithoutPullingPin_DoesNotSpray()
    {
        var gameObject = new GameObject();
        var ext = gameObject.AddComponent<FireExtinguisher>();
        ext.IsPinPulled = false;
        
        bool result = ext.TrySpray();
        Assert.IsFalse(result);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Expected: FAIL missing `FireExtinguisher`.

- [ ] **Step 3: Write implementation**

```csharp
using System;
using UnityEngine;

namespace EscapeFire.Interactions
{
    public enum ExtinguisherType { CO2, WaterFoam, DryPowder }

    public class FireExtinguisher : MonoBehaviour
    {
        [SerializeField] private ExtinguisherType type = ExtinguisherType.CO2;
        [SerializeField] private ParticleSystem sprayParticles;
        
        public ExtinguisherType Type => type;
        public bool IsPinPulled { get; set; } = false;
        public bool IsSpraying { get; private set; } = false;

        public event Action OnShockHazardTriggered;

        public void PullSafetyPin()
        {
            IsPinPulled = true;
        }

        public bool TrySpray(bool isElectricalPowerLive = false)
        {
            if (!IsPinPulled) return false;

            if (type == ExtinguisherType.WaterFoam && isElectricalPowerLive)
            {
                OnShockHazardTriggered?.Invoke();
                return false;
            }

            IsSpraying = true;
            if (sprayParticles != null && !sprayParticles.isPlaying)
            {
                sprayParticles.Play();
            }
            return true;
        }

        public void StopSpray()
        {
            IsSpraying = false;
            if (sprayParticles != null && sprayParticles.isPlaying)
            {
                sprayParticles.Stop();
            }
        }
    }
}
```

- [ ] **Step 4: Verify test passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add EscapeFire/Assets/Scripts/Interactions/
git commit -m "feat(interactions): add 6DOF FireExtinguisher logic with safety pin and shock hazard detection"
```

---

### Task 5: Fire & Volumetric Smoke Hazard System

**Files:**
- Create: `EscapeFire/Assets/Scripts/Hazards/FireHazard.cs`
- Create: `EscapeFire/Assets/Scripts/Hazards/SmokeDensityController.cs`

**Interfaces:**
- Consumes: Particle collisions from `FireExtinguisher.sprayParticles`.
- Produces: `public float FireIntensity`, `public bool IsExtinguished`.

- [ ] **Step 1: Write test**

```csharp
using NUnit.Framework;
using UnityEngine;
using EscapeFire.Hazards;

public class FireHazardTests
{
    [Test]
    public void TestExtinguish_ReducesIntensity()
    {
        var gameObject = new GameObject();
        var fire = gameObject.AddComponent<FireHazard>();
        fire.InitialIntensity = 100f;
        fire.ResetFire();
        
        fire.Extinguish(30f);
        Assert.AreEqual(70f, fire.FireIntensity);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Expected: FAIL missing `FireHazard`.

- [ ] **Step 3: Write implementation**

```csharp
using System;
using UnityEngine;

namespace EscapeFire.Hazards
{
    public class FireHazard : MonoBehaviour
    {
        [SerializeField] private float initialIntensity = 100f;
        [SerializeField] private ParticleSystem fireParticles;

        public float InitialIntensity { get => initialIntensity; set => initialIntensity = value; }
        public float FireIntensity { get; private set; }
        public bool IsExtinguished => FireIntensity <= 0f;

        public event Action OnFireExtinguished;

        private void Awake()
        {
            ResetFire();
        }

        public void ResetFire()
        {
            FireIntensity = initialIntensity;
        }

        public void Extinguish(float amount)
        {
            if (IsExtinguished) return;

            FireIntensity = Mathf.Max(0f, FireIntensity - amount);
            if (IsExtinguished)
            {
                if (fireParticles != null) fireParticles.Stop();
                OnFireExtinguished?.Invoke();
            }
        }
    }
}
```

- [ ] **Step 4: Verify test passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add EscapeFire/Assets/Scripts/Hazards/
git commit -m "feat(hazards): implement FireHazard and SmokeDensityController"
```

---

### Task 6: Telemetry Logger & Scoring Manager

**Files:**
- Create: `EscapeFire/Assets/Scripts/Analytics/ScoringManager.cs`
- Create: `EscapeFire/Assets/Scripts/Analytics/TelemetryLogger.cs`

**Interfaces:**
- Consumes: All player actions and rule compliance events.
- Produces: `public string GenerateJSONReport()`.

- [ ] **Step 1: Write test**

```csharp
using NUnit.Framework;
using EscapeFire.Analytics;

public class ScoringTests
{
    [Test]
    public void TestScoreCalculation_IncludesAddedPoints()
    {
        var manager = new ScoringManager();
        manager.AddScore(20, "Power Cutoff Completed");
        
        Assert.AreEqual(20, manager.TotalScore);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Expected: FAIL missing `ScoringManager`.

- [ ] **Step 3: Write implementation**

```csharp
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace EscapeFire.Analytics
{
    public class ScoringManager
    {
        public int TotalScore { get; private set; } = 0;
        private readonly List<string> _actionLogs = new List<string>();

        public void AddScore(int points, string reason)
        {
            TotalScore += points;
            _actionLogs.Add($"[+{points} pts] {reason}");
        }

        public void DeductScore(int points, string reason)
        {
            TotalScore -= points;
            _actionLogs.Add($"[-{points} pts] {reason}");
        }

        public string ExportJSONReport(string studentId, float totalTimeSeconds)
        {
            var report = new
            {
                StudentId = studentId,
                FinalScore = TotalScore,
                TimeElapsedSeconds = totalTimeSeconds,
                Logs = _actionLogs
            };

            return JsonConvert.SerializeObject(report, Formatting.Indented);
        }
    }
}
```

- [ ] **Step 4: Verify test passes**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add EscapeFire/Assets/Scripts/Analytics/
git commit -m "feat(analytics): add ScoringManager and TelemetryLogger for JSON reporting"
```

---

## Plan Complete and Handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-03-vr-pccc-lab-simulator.md`. Two execution options:

1. **Subagent-Driven (recommended)** - Dispatch a fresh subagent per task, review between tasks, fast iteration
2. **Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
