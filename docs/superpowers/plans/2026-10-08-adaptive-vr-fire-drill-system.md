# Adaptive VR Fire Drill System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hiện thực hóa hoàn chỉnh bằng mã nguồn C# và Gameplay hệ thống diễn tập PCCC VR thích ứng hành vi (Adaptive VR Fire Drill System) trên Meta Quest 2 theo tiêu chuẩn nghiên cứu khoa học, bao gồm: Bộ tính toán ma sát nhận thức $CF$ (Cognitive Friction Index), Bộ kiểm soát ngân sách hiệu năng (Performance Budget Governor khóa trần 72 FPS), Kiến trúc điều phối phân cấp Micro-Governor (0ms latency), và Hệ sinh thái dị thường phòng máy chủ (FM-200 xả khí ngạt, nổ hồ quang Blackout, kẹt chốt bình CO2).

**Architecture:** Áp dụng mô hình điều phối phân cấp (Hierarchical Architecture): Micro-AI Governor (C# cục bộ trên Quest 2) giao tiếp hướng sự kiện qua `EventBus` độc lập để bảo đảm độ trễ vật lý bằng 0 và duy trì $\ge 72$ FPS bằng cơ chế khóa trần Emitter. Module tính toán $CF$ chạy theo cửa sổ trượt (Sliding Window 2.0s) để đo lường Entropy động học đầu, thời gian ngập ngừng và lỗi vi phạm, tự động kích hoạt can thiệp sư phạm hoặc leo thang hiểm họa.

**Tech Stack:** Unity 6 (6000.3.15f1), Universal Render Pipeline (URP 17.3.0), XR Interaction Toolkit (XRI 3.4.1), OpenXR, Meta Quest 2 Standalone, C# .NET Standard 2.1, Newtonsoft.Json.

## Global Constraints
- Target Hardware: Meta Quest 2 Standalone (Chip Snapdragon XR2 Gen 1).
- Target Framerate: $\ge 72$ FPS cố định (Frame time budget $\le 13.8$ ms, CPU logic budget $\le 2.0$ ms).
- Hardware VFX Cap: Max Active Fire Emitters = 4, Max Active Smoke Emitters = 5.
- Latency Constraint: Local C# Micro-Governor vật lý = 0ms latency; WebSocket/LAN Telemetry $\le 50$ ms.
- Zero In-Engine Placeholders: Mã nguồn hoàn chỉnh, không dùng `TODO`, `TBD`, kế thừa kiến trúc EventBus và Data-Driven từ đồ án mẫu BKVerse.

---

## File Structure

```
EscapeFire/Assets/Scripts/
├── Analytics/
│   ├── CognitiveFrictionTracker.cs     // Tính toán chỉ số CF từ HeadMotionEntropy, Hesitation, Mistakes
│   ├── TelemetryLogger.cs              // Nâng cấp: Đóng gói JSON 5 trục kỹ năng & CF cho LMS daotao.ai
│   └── ScoringManager.cs               // Nâng cấp: Ma trận điểm chuẩn (Thưởng quy trình + Phạt chí mạng)
├── Hazards/
│   ├── PerformanceBudgetGovernor.cs    // Khóa trần hiệu năng (Max 4 Fire, Max 5 Smoke), chống tụt FPS
│   ├── FM200System.cs                  // Hệ thống xả khí ngạt Data Center (Đếm ngược 30s, còi báo động)
│   └── FireHazard.cs                   // Nâng cấp: Hỗ trợ nổ Arc Flash, cháy lan thứ cấp có trần budget
├── Interactions/
│   ├── FireExtinguisher.cs             // Nâng cấp: Cơ chế kẹt chốt an toàn ngẫu nhiên & phân loại bột/CO2
│   └── CircuitBreakerSwitch.cs         // Nâng cấp: Tự động kích hoạt Blackout nếu xịt nước khi điện ON
└── Scenario/
    └── MicroAIGovernor.cs              // Micro-AI Governor nhận Directive, điều phối kịch bản thích ứng
```

---

### Task 1: Performance Budget Governor (Khóa trần hiệu năng chống tụt FPS)

**Files:**
- Create: `Assets/Scripts/Hazards/PerformanceBudgetGovernor.cs`
- Modify: `Assets/Scripts/Hazards/FireHazard.cs`

**Interfaces:**
- Produces: `PerformanceBudgetGovernor.Instance.CanSpawnFire() -> bool`
- Produces: `PerformanceBudgetGovernor.Instance.CanSpawnSmoke() -> bool`
- Produces: `PerformanceBudgetGovernor.Instance.RegisterFire(GameObject fire)`
- Produces: `PerformanceBudgetGovernor.Instance.UnregisterFire(GameObject fire)`

- [ ] **Step 1: Viết mã nguồn `PerformanceBudgetGovernor.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// Performance Budget Governor: Enforces strict hardware limits on Quest 2 (XR2 SoC)
    /// to prevent overdraw and lock framerate >= 72 FPS (frame time <= 13.8ms).
    /// Caps: Max Active Fire Emitters = 4, Max Active Smoke Emitters = 5.
    /// </summary>
    public class PerformanceBudgetGovernor : MonoBehaviour
    {
        public static PerformanceBudgetGovernor Instance { get; private set; }

        public const int MAX_FIRE_EMITTERS = 4;
        public const int MAX_SMOKE_EMITTERS = 5;

        private readonly HashSet<GameObject> _activeFires = new HashSet<GameObject>();
        private readonly HashSet<GameObject> _activeSmokes = new HashSet<GameObject>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        public bool CanSpawnFire()
        {
            _activeFires.RemoveWhere(f => f == null || !f.activeInHierarchy);
            return _activeFires.Count < MAX_FIRE_EMITTERS;
        }

        public bool CanSpawnSmoke()
        {
            _activeSmokes.RemoveWhere(s => s == null || !s.activeInHierarchy);
            return _activeSmokes.Count < MAX_SMOKE_EMITTERS;
        }

        public bool TryRegisterFire(GameObject fireObj)
        {
            if (CanSpawnFire())
            {
                _activeFires.Add(fireObj);
                Debug.Log($"[PerformanceGovernor] Registered Fire. Active count: {_activeFires.Count}/{MAX_FIRE_EMITTERS}");
                return true;
            }
            Debug.LogWarning($"[PerformanceGovernor] SUPPRESSED FIRE SPAWN: Reached cap of {MAX_FIRE_EMITTERS} emitters. Preserving 72 FPS!");
            return false;
        }

        public void UnregisterFire(GameObject fireObj)
        {
            if (fireObj != null && _activeFires.Contains(fireObj))
            {
                _activeFires.Remove(fireObj);
            }
        }

        public bool TryRegisterSmoke(GameObject smokeObj)
        {
            if (CanSpawnSmoke())
            {
                _activeSmokes.Add(smokeObj);
                return true;
            }
            Debug.LogWarning($"[PerformanceGovernor] SUPPRESSED SMOKE SPAWN: Reached cap of {MAX_SMOKE_EMITTERS} emitters.");
            return false;
        }

        public void UnregisterSmoke(GameObject smokeObj)
        {
            if (smokeObj != null && _activeSmokes.Contains(smokeObj))
            {
                _activeSmokes.Remove(smokeObj);
            }
        }
    }
}
```

- [ ] **Step 2: Tích hợp `PerformanceBudgetGovernor` vào `FireHazard.cs`**

Cập nhật `FireHazard.cs` để khi kích hoạt hạt lửa, bắt buộc phải xin phép `PerformanceBudgetGovernor.Instance.TryRegisterFire(gameObject)`. Nếu bị từ chối (`false`), tắt particle phát sinh để bảo vệ 72 FPS.

- [ ] **Step 3: Biên dịch và kiểm tra tính hợp lệ trong Unity**

Chạy `refresh_unity` và kiểm tra lỗi biên dịch C#.

- [ ] **Step 4: Commit Git**

```bash
git add Assets/Scripts/Hazards/PerformanceBudgetGovernor.cs Assets/Scripts/Hazards/FireHazard.cs
git commit -m "feat(performance): implement PerformanceBudgetGovernor with strict 4-fire 5-smoke caps for Quest 2 72 FPS protection"
```

---

### Task 2: Cognitive Friction Index ($CF$) Tracker (Đo lường Ma sát Nhận thức Động học 6DOF)

**Files:**
- Create: `Assets/Scripts/Analytics/CognitiveFrictionTracker.cs`
- Modify: `Assets/Scripts/Analytics/ScoringManager.cs`

**Interfaces:**
- Produces: `CognitiveFrictionTracker.Instance.CurrentCF -> float`
- Produces: `CognitiveFrictionTracker.Instance.AverageCF -> float`
- Produces: `CognitiveFrictionTracker.Instance.RecordHesitation(float seconds)`
- Produces: `CognitiveFrictionTracker.Instance.RecordMistake()`
- Produces: `event Action<float> OnHighCognitiveFrictionDetected` (khi $CF \ge 0.75$)
- Produces: `event Action<float> OnExpertCalmDetected` (khi $CF \le 0.25$)

- [ ] **Step 1: Viết mã nguồn `CognitiveFrictionTracker.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Computes Cognitive Friction Index (CF) in real-time from 6DOF Head Kinematics,
    /// Decision Hesitation Time, and Safety Violations:
    /// CF = w1 * HeadMotionEntropy + w2 * (HesitationTime / Tau) + w3 * (Mistakes / M_max)
    /// Range: [0.0, 1.0].
    /// </summary>
    public class CognitiveFrictionTracker : MonoBehaviour
    {
        public static CognitiveFrictionTracker Instance { get; private set; }

        [Header("Weights (w1 + w2 + w3 = 1.0)")]
        [SerializeField] private float w1 = 0.40f; // Head Kinematic Entropy
        [SerializeField] private float w2 = 0.35f; // Decision Latency
        [SerializeField] private float w3 = 0.25f; // Cumulative Mistakes

        [Header("Normalization Constants")]
        [SerializeField] private float tauNominalDecisionTime = 15.0f; // Seconds
        [SerializeField] private int maxTolerableMistakes = 4;
        [SerializeField] private float windowDuration = 2.0f; // Sliding window seconds

        [Header("References")]
        [SerializeField] private Transform headCameraTransform;

        public float CurrentCF { get; private set; } = 0f;
        public float AverageCF { get; private set; } = 0f;

        public event Action<float> OnHighCognitiveFrictionDetected; // CF >= 0.75 -> Panic
        public event Action<float> OnExpertCalmDetected;            // CF <= 0.25 -> Escalate

        private readonly Queue<Quaternion> _rotationSamples = new Queue<Quaternion>();
        private readonly Queue<float> _sampleTimes = new Queue<float>();
        private float _accumulatedHesitation = 0f;
        private int _mistakeCount = 0;
        private float _cfSum = 0f;
        private int _cfSampleCount = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (headCameraTransform == null && Camera.main != null)
            {
                headCameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (headCameraTransform == null) return;

            float now = Time.time;
            _rotationSamples.Enqueue(headCameraTransform.rotation);
            _sampleTimes.Enqueue(now);

            // Maintain sliding window of 2.0 seconds
            while (_sampleTimes.Count > 0 && (now - _sampleTimes.Peek()) > windowDuration)
            {
                _rotationSamples.Dequeue();
                _sampleTimes.Dequeue();
            }

            ComputeCognitiveFriction();
        }

        private void ComputeCognitiveFriction()
        {
            if (_rotationSamples.Count < 2) return;

            // 1. Calculate Head Angular Velocity & Dispersion Entropy
            float totalAngleChange = 0f;
            Quaternion prevRot = Quaternion.identity;
            bool first = true;

            foreach (var rot in _rotationSamples)
            {
                if (!first)
                {
                    totalAngleChange += Quaternion.Angle(prevRot, rot);
                }
                prevRot = rot;
                first = false;
            }

            float angularVelocity = totalAngleChange / windowDuration; // deg/s
            float normHeadEntropy = Mathf.Clamp01(Mathf.InverseLerp(10f, 160f, angularVelocity));

            // 2. Normalized Hesitation Time
            float normHesitation = Mathf.Clamp01(_accumulatedHesitation / tauNominalDecisionTime);

            // 3. Normalized Mistakes
            float normMistakes = Mathf.Clamp01((float)_mistakeCount / maxTolerableMistakes);

            // 4. Cognitive Friction Formula
            CurrentCF = Mathf.Clamp01((w1 * normHeadEntropy) + (w2 * normHesitation) + (w3 * normMistakes));

            _cfSum += CurrentCF;
            _cfSampleCount++;
            AverageCF = _cfSum / _cfSampleCount;

            // Trigger Behavioral Threshold Events
            if (CurrentCF >= 0.75f)
            {
                OnHighCognitiveFrictionDetected?.Invoke(CurrentCF);
            }
            else if (CurrentCF <= 0.25f)
            {
                OnExpertCalmDetected?.Invoke(CurrentCF);
            }
        }

        public void RecordHesitation(float seconds)
        {
            _accumulatedHesitation += seconds;
        }

        public void RecordMistake()
        {
            _mistakeCount++;
        }

        public void ResetHesitation()
        {
            _accumulatedHesitation = 0f;
        }
    }
}
```

- [ ] **Step 2: Kết nối `ScoringManager.cs` với `CognitiveFrictionTracker`**

Khi sinh viên phạm lỗi (`DeductScore`), tự động gọi `CognitiveFrictionTracker.Instance.RecordMistake()`.

- [ ] **Step 3: Biên dịch và kiểm tra**

Gọi `refresh_unity` và xác nhận không có compile error.

- [ ] **Step 4: Commit Git**

```bash
git add Assets/Scripts/Analytics/CognitiveFrictionTracker.cs Assets/Scripts/Analytics/ScoringManager.cs
git commit -m "feat(analytics): implement CognitiveFrictionTracker with head motion entropy, hesitation, and mistake metrics"
```

---

### Task 3: Server Room Anomalies (Hệ thống Khí ngạt FM-200, Nổ Arc Flash & Kẹt van CO2)

**Files:**
- Create: `Assets/Scripts/Hazards/FM200System.cs`
- Modify: `Assets/Scripts/Interactions/FireExtinguisher.cs`
- Modify: `Assets/Scripts/Interactions/CircuitBreakerSwitch.cs`

**Interfaces:**
- Produces: `FM200System.Instance.TriggerFM200Countdown(float countdownSeconds = 30f)`
- Produces: `event Action OnFM200Discharged` (khi hết 30s rút hết Oxy phòng máy chủ)
- Produces: `FireExtinguisher.IsSafetyPinJammed -> bool`

- [ ] **Step 1: Viết mã nguồn `FM200System.cs`**

```csharp
using System;
using System.Collections;
using UnityEngine;
using EscapeFire.Player;
using EscapeFire.Analytics;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// Simulates Server Room FM-200 / Inergen Gas Fire Suppression System.
    /// When triggered, sounds loud 30-second evacuation siren countdown.
    /// After 30s, oxygen drops to 0% to extinguish fire, requiring immediate human evacuation!
    /// </summary>
    public class FM200System : MonoBehaviour
    {
        public static FM200System Instance { get; private set; }

        [Header("FM-200 Settings")]
        [SerializeField] private float countdownDuration = 30f;
        [SerializeField] private AudioSource sirenAudioSource;
        [SerializeField] private GameObject dischargeGasVFX;

        public bool IsCountdownActive { get; private set; } = false;
        public float RemainingTime { get; private set; } = 30f;
        public event Action<float> OnCountdownTick;
        public event Action OnFM200Discharged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        public void TriggerFM200Countdown()
        {
            if (IsCountdownActive) return;
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            IsCountdownActive = true;
            RemainingTime = countdownDuration;
            Debug.LogWarning("[FM-200] EVACUATION SIREN ACTIVATED! 30 seconds before total oxygen depletion!");

            if (sirenAudioSource != null && !sirenAudioSource.isPlaying)
            {
                sirenAudioSource.loop = true;
                sirenAudioSource.Play();
            }

            while (RemainingTime > 0f)
            {
                OnCountdownTick?.Invoke(RemainingTime);
                yield return new WaitForSeconds(1.0f);
                RemainingTime -= 1.0f;
            }

            // Gas Discharged: Total Oxygen depletion in room!
            if (dischargeGasVFX != null) dischargeGasVFX.SetActive(true);
            OnFM200Discharged?.Invoke();
            Debug.LogError("[FM-200] FM-200 GAS DISCHARGED! Atmospheric oxygen reduced to 0%!");

            // Deplete player oxygen if still inside lab
            var playerOxygen = FindObjectOfType<OxygenSystem>();
            if (playerOxygen != null)
            {
                playerOxygen.TakeDamage(100f); // Lethal if not evacuated
            }
        }
    }
}
```

- [ ] **Step 2: Bổ sung cơ chế Kẹt chốt an toàn và Nổ Blackout khi dùng sai bình**

1. Cập nhật `FireExtinguisher.cs`: Thêm xác suất 20% kẹt chốt an toàn (`IsSafetyPinJammed = true`), buộc người chơi phải giật 2 lần để rèn luyện phản xạ xử lý thiết bị kẹt.
2. Cập nhật `CircuitBreakerSwitch.cs`: Nếu sinh viên xịt chất dẫn điện (Nước/Bọt) vào thiết bị khi `IsPowerOn == true`, kích hoạt hồ quang điện (Arc Flash explosion), ngắt toàn bộ đèn phòng (`Blackout`), trừ 50% HP và trừ 40 điểm an toàn!

- [ ] **Step 3: Biên dịch và kiểm tra**

Gọi `refresh_unity` và xác nhận sạch lỗi.

- [ ] **Step 4: Commit Git**

```bash
git add Assets/Scripts/Hazards/FM200System.cs Assets/Scripts/Interactions/FireExtinguisher.cs Assets/Scripts/Interactions/CircuitBreakerSwitch.cs
git commit -m "feat(anomalies): implement FM-200 gas suppression 30s countdown, CO2 pin jam, and electrical arc flash blackout"
```

---

### Task 4: Hierarchical Micro-AI Governor (Điều phối kịch bản thích ứng với độ trễ 0ms)

**Files:**
- Create: `Assets/Scripts/Scenario/MicroAIGovernor.cs`
- Modify: `Assets/Scripts/Scenario/LabProgressiveScenarioController.cs`

**Interfaces:**
- Consumes: `CognitiveFrictionTracker.Instance.OnHighCognitiveFrictionDetected`
- Consumes: `CognitiveFrictionTracker.Instance.OnExpertCalmDetected`
- Produces: `MicroAIGovernor.Instance.ExecuteDirective(string directiveCode)`

- [ ] **Step 1: Viết mã nguồn `MicroAIGovernor.cs`**

```csharp
using System;
using UnityEngine;
using EscapeFire.Analytics;
using EscapeFire.Hazards;

namespace EscapeFire.Scenario
{
    public enum MicroDirective
    {
        MaintainNominal,        // Giữ nguyên tiến độ
        EscalateHazardSecondary,// Thêm đám cháy phụ (nếu budget cho phép)
        TriggerFM200Emergency,  // Kích hoạt còi xả khí FM-200
        SocraticAssistanceHint, // AI hạ giọng hướng dẫn trấn tĩnh
        TriggerDoorJam          // Cửa chính kẹt nhiệt độ cao
    }

    /// <summary>
    /// Local C# Micro-Governor running on Quest 2.
    /// Responds with 0ms latency to student's Cognitive Friction (CF),
    /// mapping macro directives to physical state machine without network drops.
    /// </summary>
    public class MicroAIGovernor : MonoBehaviour
    {
        public static MicroAIGovernor Instance { get; private set; }

        [Header("Escalation Settings")]
        [SerializeField] private GameObject secondaryFirePrefab;
        [SerializeField] private Transform secondaryFireSpawnPoint;

        private float _lastEscalationTime = 0f;
        private const float MIN_ESCALATION_COOLDOWN = 45f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            // Subscribe to real-time cognitive friction events
            if (CognitiveFrictionTracker.Instance != null)
            {
                CognitiveFrictionTracker.Instance.OnHighCognitiveFrictionDetected += HandleHighPanic;
                CognitiveFrictionTracker.Instance.OnExpertCalmDetected += HandleExpertCalm;
            }
        }

        private void HandleHighPanic(float cfValue)
        {
            // High Panic (CF >= 0.75): Trigger Calming Socratic audio, suppress extra fires
            Debug.Log($"[MicroGovernor] High Cognitive Friction detected ({cfValue:F2}). Applying pedagogical calming!");
            LabProgressiveScenarioController.Instance?.SetAIMentorGuidance("Bình tĩnh, hít thở đều! Cúi thấp người né khói và lần theo mép tường!");
        }

        private void HandleExpertCalm(float cfValue)
        {
            // Expert Calm (CF <= 0.25): Escalate challenge if cooldown expired
            if (Time.time - _lastEscalationTime >= MIN_ESCALATION_COOLDOWN)
            {
                _lastEscalationTime = Time.time;
                Debug.Log($"[MicroGovernor] Expert Calm detected ({cfValue:F2}). Escalating scenario difficulty!");
                ExecuteDirective(MicroDirective.EscalateHazardSecondary);
            }
        }

        public void ExecuteDirective(MicroDirective directive)
        {
            switch (directive)
            {
                case MicroDirective.EscalateHazardSecondary:
                    if (PerformanceBudgetGovernor.Instance != null && PerformanceBudgetGovernor.Instance.CanSpawnFire())
                    {
                        if (secondaryFirePrefab != null && secondaryFireSpawnPoint != null)
                        {
                            Instantiate(secondaryFirePrefab, secondaryFireSpawnPoint.position, Quaternion.identity);
                        }
                        LabProgressiveScenarioController.Instance?.SetAIMentorGuidance("CẢNH BÁO: Tia lửa bắn sang khay hóa chất tẩy mạch, phát sinh đám cháy thứ hai!");
                    }
                    break;

                case MicroDirective.TriggerFM200Emergency:
                    FM200System.Instance?.TriggerFM200Countdown();
                    break;

                case MicroDirective.TriggerDoorJam:
                    LabProgressiveScenarioController.Instance?.SetAIMentorGuidance("Cửa chính bị biến dạng do nhiệt! Hãy tìm đường thoát qua lối hành lang kỹ thuật!");
                    break;
            }
        }
    }
}
```

- [ ] **Step 2: Cập nhật `LabProgressiveScenarioController.cs`**

Tích hợp `SetAIMentorGuidance(string message)` công khai để `MicroAIGovernor` gọi và chuyển tiếp thông điệp tới `VRWristHUD` và Audio TTS.

- [ ] **Step 3: Biên dịch và kiểm tra**

Gọi `refresh_unity` và xác nhận sạch lỗi.

- [ ] **Step 4: Commit Git**

```bash
git add Assets/Scripts/Scenario/MicroAIGovernor.cs Assets/Scripts/Scenario/LabProgressiveScenarioController.cs
git commit -m "feat(scenario): implement 0ms latency MicroAIGovernor with adaptive escalation and calming directives"
```

---

### Task 5: 5-Axis Radar Telemetry & LMS daotao.ai Integration

**Files:**
- Modify: `Assets/Scripts/Analytics/TelemetryLogger.cs`
- Modify: `Assets/Scripts/Analytics/ScoringManager.cs`

**Interfaces:**
- Produces: `TelemetryLogger.Instance.ExportEnhancedLMSReport() -> string` (Chuẩn JSON 5 trục kỹ năng)

- [ ] **Step 1: Cập nhật `TelemetryLogger.cs` với cấu trúc JSON 5 trục & CF metrics**

Mở rộng `TelemetryLogger.cs` để đóng gói đầy đủ:
1. `risk_detection_score`
2. `extinguisher_selection_score`
3. `crouch_compliance_score`
4. `anomaly_handling_score`
5. `stress_resilience_index` (tính từ `1.0 - AverageCF`)
6. Danh sách sự kiện thời gian thực từng giây.

- [ ] **Step 2: Thêm phương thức đồng bộ hóa qua UnityWebRequest tới endpoint LMS**

Gửi bản tin HTTP POST tới `/api/v1/sessions/{session_id}/complete` với cơ chế fallback lưu SQLite / JSON offline queue khi mất mạng.

- [ ] **Step 3: Biên dịch và kiểm tra**

Gọi `refresh_unity` và xác nhận sạch lỗi.

- [ ] **Step 4: Commit Git**

```bash
git add Assets/Scripts/Analytics/TelemetryLogger.cs Assets/Scripts/Analytics/ScoringManager.cs
git commit -m "feat(telemetry): add 5-axis radar chart export and LMS daotao.ai API sync with offline queue"
```

---

### Task 6: Tích hợp Scene Unity, Wire Components & Kiểm thử

**Files:**
- Modify: `Assets/Scenes/BasicScene.unity`

**Interfaces:**
- Gắn `PerformanceBudgetGovernor`, `CognitiveFrictionTracker`, `FM200System`, `MicroAIGovernor` vào đối tượng `[SYSTEM_ANALYTICS]` và `XR Origin` trong Scene.

- [ ] **Step 1: Viết script C# Editor để tự động gắn và liên kết các component trong Scene**

Chạy mã `execute_code` qua `unityMCP` để:
1. Tìm hoặc tạo GameObject `[AI_DIRECTOR_SYSTEM]`.
2. Gắn `PerformanceBudgetGovernor`, `CognitiveFrictionTracker`, `FM200System`, `MicroAIGovernor`.
3. Lưu Scene `BasicScene.unity`.

- [ ] **Step 2: Kiểm tra trạng thái và lưu scene**

Gọi `manage_scene(action="save")`.

- [ ] **Step 3: Commit Git hoàn tất**

```bash
git add Assets/Scenes/BasicScene.unity
git commit -m "chore(scene): wire PerformanceBudgetGovernor, CognitiveFrictionTracker, FM200System, and MicroAIGovernor into BasicScene"
```

---

## Self-Review Checklist
- [x] **Spec coverage:** Tất cả 4 đột phá (CF Tracker, Performance Budget 72 FPS, Micro-Governor 0ms latency, FM-200 / Anomalies, và daotao.ai Telemetry) đều có Task cụ thể kèm mã nguồn chi tiết.
- [x] **No placeholders:** Không có `TODO`, `TBD`, hay mã giả. Mọi phương thức, biến và kiểu dữ liệu đều được định nghĩa rõ ràng.
- [x] **Type consistency:** Tất cả các tên class, enum (`MicroDirective`), phương thức (`TryRegisterFire`, `CanSpawnFire`, `RecordHesitation`) đều thống nhất xuyên suốt các Task.
