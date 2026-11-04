# M0 — Nền móng Kỹ thuật (Technical Foundation) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đưa codebase hiện có (14 script) về trạng thái có kiến trúc chung (EventBus, GameSession), có assembly definition, có EditMode test, sửa các lỗi logic nền tảng, và dọn sample thừa — **không làm vỡ `BasicScene`**.

**Architecture:** Thêm `EscapeFire.Core` (EventBus generic tĩnh, không cấp phát khi Publish; `GameSession` lưu trạng thái xuyên scene). Tách logic thuần (điểm, oxy, khom người, hít khói) ra class C# thường để test được trong EditMode; MonoBehaviour chỉ còn là lớp vỏ gọi model + publish event. Giữ nguyên các C# `event` cũ để không vỡ tham chiếu Inspector/scene.

**Tech Stack:** Unity 6000.3.15f1, URP 17.3, XRI 3.4.1, Unity Test Framework 1.6.0 (đã có qua `com.unity.feature.development`), NUnit, Newtonsoft.Json 3.2.2, Coplay Unity MCP.

**Roadmap gốc:** `docs/superpowers/plans/2026-11-04-development-roadmap.md` (M0).

## Global Constraints
- Không đổi vị trí/tên file `.cs` hiện có → giữ GUID trong `.meta`, scene không mất script.
- Zero GC trong `Update()` (CODING_STANDARDS §1.2): không `new`, không LINQ, không `Find*` trong vòng lặp.
- Không dùng `FindObjectOfType` (obsolete trong Unity 6) → `FindFirstObjectByType`.
- Commit theo Conventional Commits, tiêu đề ≤ 72 ký tự, chữ thường. Trailer commit theo CODING_STANDARDS §3.3 (do chủ repo quyết định giữ hay bỏ).
- Mỗi task kết thúc: Unity compile sạch (0 error) + test xanh, rồi mới commit.

## Cách chạy test (dùng cho mọi task)
- **Ưu tiên:** Unity MCP — `refresh_unity` → `read_console` (lọc error) → `run_tests` mode `EditMode`.
- **Thay thế:** Unity Editor → `Window > General > Test Runner > EditMode > Run All`.
- **CLI (phải đóng Editor trước):**
  ```bash
  "C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml
  ```
  (đường dẫn Unity.exe tùy máy — kiểm tra trong Unity Hub.)

---

## Các lỗi hiện có được sửa trong M0

| # | File | Lỗi | Hậu quả |
| :-: | :--- | :--- | :--- |
| B1 | `OxygenSystem.cs` | Trừ oxy **mọi lúc khi đứng thẳng**, không quan tâm có khói hay không | Đứng thẳng ở Phase 0 (chưa cháy) → chết ngạt sau ~10s |
| B2 | `SmokeDensityController.cs` | Hạ khói khi `!targetFire.IsExtinguished`, kể cả khi GameObject lửa đang **inactive** (Phase 0/1) | Khói tụt xuống trước khi có cháy |
| B3 | `LabProgressiveScenarioController.cs`, `VRWristHUD.cs` | Đăng ký event nhưng không bao giờ huỷ | Handler treo khi reload scene / tắt object |
| B4 | Nhiều file | `FindObjectOfType` obsolete | Warning compile, mặc định bỏ qua object inactive |
| B5 | `VRHeightDetector.cs` | Ngưỡng khom cố định 1.2m, bỏ qua `H₀` (spec: `< 0.7 × H₀`) | Người cao/thấp bị đánh giá sai |
| B6 | `LabProgressiveScenarioController.cs` | Thiếu `SetAIMentorGuidance(string)` mà plan 10-08 cần | Plan M2 không compile |

---

## File Structure

```
Assets/Scripts/
├── EscapeFire.Runtime.asmdef              (new)
├── Core/
│   ├── EventBus.cs                        (new)  EventBus<T> + EventBusRegistry
│   ├── GameEvents.cs                      (new)  struct event dùng chung
│   └── GameSession.cs                     (new)  Mode / StudentId / SessionId / H₀
├── Analytics/
│   ├── ScoreLedger.cs                     (new)  logic điểm thuần
│   └── ScoringManager.cs                  (mod)  dùng ScoreLedger + publish ScoreChangedEvent
├── Player/
│   ├── OxygenModel.cs                     (new)  logic oxy thuần
│   ├── PlayerRules.cs                     (new)  CrouchRules + BreathingRules
│   ├── OxygenSystem.cs                    (mod)  B1
│   ├── HealthSystem.cs                    (mod)  publish PlayerDiedEvent
│   └── VRHeightDetector.cs                (mod)  B5
├── Hazards/
│   ├── FireHazard.cs                      (mod)  publish FireExtinguishedEvent
│   └── SmokeDensityController.cs          (mod)  B2
├── Interactions/*.cs                      (mod)  publish event tương ứng
├── NPC/NPCSimulator.cs                    (mod)  publish NpcRescuedEvent
├── Scenario/LabProgressiveScenarioController.cs (mod) B3, B4, B6
└── UI/VRWristHUD.cs                       (mod)  B3, B4
Assets/Tests/EditMode/
├── EscapeFire.Tests.EditMode.asmdef       (new)
├── EventBusTests.cs                       (new)
├── GameSessionTests.cs                    (new)
├── ScoreLedgerTests.cs                    (new)
├── OxygenModelTests.cs                    (new)
└── PlayerRulesTests.cs                    (new)
```

---

### Task 1: Nhánh làm việc & baseline

**Files:**
- Modify: `docs/superpowers/plans/2026-11-04-development-roadmap.md`

- [ ] **Step 1: Tạo nhánh feature từ `develop/server`**

```bash
git checkout develop/server
git checkout -b feature/m0-foundation
```

- [ ] **Step 2: Sửa hiểu nhầm về branch trong roadmap**

Theo `CODING_STANDARDS_AND_RULES.md` §3.1, `develop/laptop` và `develop/server` là **nhánh làm việc theo máy**, không phải "nhánh code server". Trong roadmap:
- Bảng hiện trạng, dòng "Server (Laptop Edge LLM)": thay "**Chưa có gì** — branch `develop/server` hiện chỉ có docs" bằng "**Chưa có gì** — chưa có thư mục `Server/`".
- Tiêu đề M3: thay "(trọng tâm branch `develop/server`, ≈ 2 tuần)" bằng "(≈ 2 tuần, làm trên `feature/m3-edge-server`)".

- [ ] **Step 3: Ghi baseline console**

Mở project trong Unity 6000.3.15f1, mở `Assets/Scenes/BasicScene.unity`. Qua MCP: `refresh_unity` → `read_console`. Ghi lại số error/warning vào mô tả commit (kỳ vọng: 0 error, có warning `FindObjectOfType is obsolete`). Nếu có **error**, dừng và báo lại trước khi làm tiếp.

- [ ] **Step 4: Commit**

```bash
git add docs/superpowers/plans/2026-11-04-development-roadmap.md docs/superpowers/plans/2026-11-04-m0-foundation.md
git commit -m "docs(plan): add m0 foundation plan and fix branch naming in roadmap"
```

---

### Task 2: Assembly definitions

**Files:**
- Create: `Assets/Scripts/EscapeFire.Runtime.asmdef`
- Create: `Assets/Tests/EditMode/EscapeFire.Tests.EditMode.asmdef`

- [ ] **Step 1: Tạo runtime asmdef**

`Assets/Scripts/EscapeFire.Runtime.asmdef`:
```json
{
    "name": "EscapeFire.Runtime",
    "rootNamespace": "EscapeFire",
    "references": [
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```
`overrideReferences: false` → tự nhận các DLL auto-referenced, gồm `Newtonsoft.Json.dll` mà `ScoringManager` đang dùng. `UnityEngine.UI` bắt buộc vì `TextMeshPro` kế thừa `MaskableGraphic` và Unity không truyền reference bắc cầu (đã xác minh bằng compile).

- [ ] **Step 2: Tạo test asmdef**

`Assets/Tests/EditMode/EscapeFire.Tests.EditMode.asmdef`:
```json
{
    "name": "EscapeFire.Tests.EditMode",
    "rootNamespace": "EscapeFire.Tests",
    "references": [
        "EscapeFire.Runtime",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Compile & kiểm tra scene**

`refresh_unity` → `read_console`. Kỳ vọng 0 error.
- Nếu lỗi `The type or namespace name 'Newtonsoft' could not be found`: thêm vào runtime asmdef `"overrideReferences": true, "precompiledReferences": ["Newtonsoft.Json.dll"]`.
- Nếu lỗi `TMPro`: kiểm tra tên assembly trong `Library/ScriptAssemblies` (Unity 6 vẫn là `Unity.TextMeshPro`).

Mở `BasicScene`, chọn các object có script (`[SYSTEM_ANALYTICS]`, XR Origin, tủ điện…) → không có "Missing (Mono Script)". Qua MCP có thể dùng `manage_scene`/`find_gameobjects` để liệt kê component.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/EscapeFire.Runtime.asmdef Assets/Scripts/EscapeFire.Runtime.asmdef.meta Assets/Tests
git commit -m "chore(build): add runtime and editmode test assembly definitions"
```

---

### Task 3: EventBus (TDD)

**Files:**
- Create: `Assets/Tests/EditMode/EventBusTests.cs`
- Create: `Assets/Scripts/Core/EventBus.cs`

**Interfaces:**
- Produces: `EventBus<T>.Subscribe(Action<T>)`, `Unsubscribe(Action<T>)`, `Publish(T)`, `Clear()`, `HandlerCount`
- Produces: `EventBusRegistry.ResetAll()` (tự chạy ở `SubsystemRegistration` → an toàn khi tắt Domain Reload)

- [ ] **Step 1: Viết test (sẽ fail vì chưa có EventBus)**

```csharp
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Core;

namespace EscapeFire.Tests
{
    public class EventBusTests
    {
        private struct PingEvent
        {
            public int Value;
            public PingEvent(int value) { Value = value; }
        }

        [SetUp]
        public void SetUp() => EventBus<PingEvent>.Clear();

        [TearDown]
        public void TearDown() => EventBus<PingEvent>.Clear();

        [Test]
        public void Publish_DeliversPayloadToSubscriber()
        {
            int received = 0;
            EventBus<PingEvent>.Subscribe(e => received = e.Value);

            EventBus<PingEvent>.Publish(new PingEvent(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            void Handler(PingEvent e) => calls++;
            EventBus<PingEvent>.Subscribe(Handler);
            EventBus<PingEvent>.Unsubscribe(Handler);

            EventBus<PingEvent>.Publish(new PingEvent(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void Subscribe_SameHandlerTwice_DeliversOnce()
        {
            int calls = 0;
            void Handler(PingEvent e) => calls++;
            EventBus<PingEvent>.Subscribe(Handler);
            EventBus<PingEvent>.Subscribe(Handler);

            EventBus<PingEvent>.Publish(new PingEvent(1));

            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, EventBus<PingEvent>.HandlerCount);
        }

        [Test]
        public void Publish_HandlerThrows_OtherHandlersStillReceive()
        {
            int calls = 0;
            EventBus<PingEvent>.Subscribe(e => throw new System.InvalidOperationException("boom"));
            EventBus<PingEvent>.Subscribe(e => calls++);
            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            EventBus<PingEvent>.Publish(new PingEvent(1));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Publish_HandlerUnsubscribesItself_NoErrorAndOthersReceive()
        {
            int otherCalls = 0;
            System.Action<PingEvent> selfRemoving = null;
            selfRemoving = e => EventBus<PingEvent>.Unsubscribe(selfRemoving);
            EventBus<PingEvent>.Subscribe(e => otherCalls++);
            EventBus<PingEvent>.Subscribe(selfRemoving);

            EventBus<PingEvent>.Publish(new PingEvent(1));

            Assert.AreEqual(1, otherCalls);
            Assert.AreEqual(1, EventBus<PingEvent>.HandlerCount);
        }

        [Test]
        public void ResetAll_ClearsEveryTouchedBus()
        {
            EventBus<PingEvent>.Subscribe(e => { });

            EventBusRegistry.ResetAll();

            Assert.AreEqual(0, EventBus<PingEvent>.HandlerCount);
        }
    }
}
```

- [ ] **Step 2: Chạy test → kỳ vọng FAIL** (compile error: `EventBus` không tồn tại).

- [ ] **Step 3: Viết `Assets/Scripts/Core/EventBus.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFire.Core
{
    /// <summary>
    /// Typed, allocation-free (on Publish) static event bus.
    /// Handler order is NOT guaranteed. A throwing handler is logged and does not stop the others.
    /// Subscribe in OnEnable, Unsubscribe in OnDisable.
    /// </summary>
    public static class EventBus<T> where T : struct
    {
        private static readonly List<Action<T>> _handlers = new List<Action<T>>(8);

        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }

        public static int HandlerCount => _handlers.Count;

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null || _handlers.Contains(handler)) return;
            _handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null) return;
            _handlers.Remove(handler);
        }

        public static void Publish(T evt)
        {
            // Reverse iteration: a handler may unsubscribe itself (or others) safely without allocating a copy.
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (i >= _handlers.Count) continue;
                try
                {
                    _handlers[i](evt);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public static void Clear()
        {
            _handlers.Clear();
        }
    }

    /// <summary>
    /// Clears every EventBus that has been used, at the start of each play session
    /// (required when "Enter Play Mode Options" disables domain reload).
    /// </summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> _clearActions = new List<Action>();

        internal static void Register(Action clearAction)
        {
            _clearActions.Add(clearAction);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetAll()
        {
            for (int i = 0; i < _clearActions.Count; i++)
            {
                _clearActions[i]();
            }
        }
    }
}
```

- [ ] **Step 4: Chạy test → kỳ vọng 6/6 PASS.**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode/EventBusTests.cs Assets/Tests/EditMode/EventBusTests.cs.meta
git commit -m "feat(core): add allocation-free typed event bus with editmode tests"
```

---

### Task 4: GameEvents & GameSession (TDD)

**Files:**
- Create: `Assets/Scripts/Core/GameEvents.cs`
- Create: `Assets/Scripts/Core/GameSession.cs`
- Create: `Assets/Tests/EditMode/GameSessionTests.cs`

**Interfaces:**
- Produces các struct event (dùng ở Task 6):
  `PhaseChangedEvent`, `MentorMessageEvent`, `PowerStateChangedEvent`, `FireExtinguishedEvent`, `AlarmActivatedEvent`, `ExtinguisherPinPulledEvent`, `ShockHazardEvent`, `CrouchStateChangedEvent`, `OxygenDepletedEvent`, `PlayerDiedEvent`, `ScoreChangedEvent`, `NpcRescuedEvent`, `DoorHeatCheckedEvent`
- Produces: `GameSession.Begin(TrainingMode, string)`, `SetCalibratedHeight(float)`, `Reset()`, các property `Mode`, `StudentId`, `SessionId`, `CalibratedHeight`, `IsCalibrated`

- [ ] **Step 1: Viết `GameEvents.cs`** (chỉ là dữ liệu, không cần test riêng)

```csharp
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Scenario;

namespace EscapeFire.Core
{
    public readonly struct PhaseChangedEvent
    {
        public readonly ScenarioPhase Phase;
        public PhaseChangedEvent(ScenarioPhase phase) { Phase = phase; }
    }

    public readonly struct MentorMessageEvent
    {
        public readonly string Message;
        public MentorMessageEvent(string message) { Message = message; }
    }

    public readonly struct PowerStateChangedEvent
    {
        public readonly bool IsPowerOn;
        public PowerStateChangedEvent(bool isPowerOn) { IsPowerOn = isPowerOn; }
    }

    public readonly struct FireExtinguishedEvent
    {
        public readonly int FireInstanceId;
        public FireExtinguishedEvent(int fireInstanceId) { FireInstanceId = fireInstanceId; }
    }

    public readonly struct AlarmActivatedEvent { }

    public readonly struct ExtinguisherPinPulledEvent
    {
        public readonly ExtinguisherType Type;
        public ExtinguisherPinPulledEvent(ExtinguisherType type) { Type = type; }
    }

    public readonly struct ShockHazardEvent
    {
        public readonly ExtinguisherType Type;
        public ShockHazardEvent(ExtinguisherType type) { Type = type; }
    }

    public readonly struct CrouchStateChangedEvent
    {
        public readonly bool IsCrouching;
        public readonly float Height;
        public CrouchStateChangedEvent(bool isCrouching, float height) { IsCrouching = isCrouching; Height = height; }
    }

    public readonly struct OxygenDepletedEvent { }

    public readonly struct PlayerDiedEvent
    {
        public readonly string Cause;
        public PlayerDiedEvent(string cause) { Cause = cause; }
    }

    public readonly struct ScoreChangedEvent
    {
        public readonly int Delta;
        public readonly int Total;
        public readonly string Reason;
        public ScoreChangedEvent(int delta, int total, string reason) { Delta = delta; Total = total; Reason = reason; }
    }

    public readonly struct NpcRescuedEvent
    {
        public readonly NPCRole Role;
        public NpcRescuedEvent(NPCRole role) { Role = role; }
    }

    public readonly struct DoorHeatCheckedEvent
    {
        public readonly bool WasHot;
        public DoorHeatCheckedEvent(bool wasHot) { WasHot = wasHot; }
    }
}
```

- [ ] **Step 2: Viết test `GameSessionTests.cs` (FAIL)**

```csharp
using System;
using NUnit.Framework;
using EscapeFire.Core;

namespace EscapeFire.Tests
{
    public class GameSessionTests
    {
        [SetUp]
        public void SetUp() => GameSession.Reset();

        [Test]
        public void Begin_SetsModeStudentAndNewSessionId()
        {
            GameSession.Begin(TrainingMode.Exam, "SV_001");
            string first = GameSession.SessionId;
            GameSession.Begin(TrainingMode.Exam, "SV_001");

            Assert.AreEqual(TrainingMode.Exam, GameSession.Mode);
            Assert.AreEqual("SV_001", GameSession.StudentId);
            Assert.IsFalse(string.IsNullOrEmpty(first));
            Assert.AreNotEqual(first, GameSession.SessionId);
        }

        [Test]
        public void Begin_BlankStudentId_FallsBackToDefault()
        {
            GameSession.Begin(TrainingMode.Guided, "  ");

            Assert.AreEqual(GameSession.DEFAULT_STUDENT_ID, GameSession.StudentId);
        }

        [Test]
        public void SetCalibratedHeight_OutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GameSession.SetCalibratedHeight(0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameSession.SetCalibratedHeight(2.6f));
            Assert.IsFalse(GameSession.IsCalibrated);
        }

        [Test]
        public void SetCalibratedHeight_Valid_MarksCalibrated()
        {
            GameSession.SetCalibratedHeight(1.65f);

            Assert.IsTrue(GameSession.IsCalibrated);
            Assert.AreEqual(1.65f, GameSession.CalibratedHeight, 1e-5f);
        }
    }
}
```

- [ ] **Step 3: Chạy test → FAIL (compile).**

- [ ] **Step 4: Viết `GameSession.cs`**

```csharp
using System;
using UnityEngine;

namespace EscapeFire.Core
{
    public enum TrainingMode
    {
        Guided, // Highlights + floor arrows (beginners)
        Exam    // No hints, full adaptive pressure
    }

    /// <summary>
    /// Cross-scene session state (MainMenu -> Lab -> AAR). Plain static, reset each play session.
    /// </summary>
    public static class GameSession
    {
        public const string DEFAULT_STUDENT_ID = "SV_2026_TEST";
        public const float MIN_CALIBRATED_HEIGHT = 1.0f;
        public const float MAX_CALIBRATED_HEIGHT = 2.3f;

        public static TrainingMode Mode { get; private set; } = TrainingMode.Guided;
        public static string StudentId { get; private set; } = DEFAULT_STUDENT_ID;
        public static string SessionId { get; private set; } = string.Empty;
        public static float CalibratedHeight { get; private set; }
        public static bool IsCalibrated => CalibratedHeight > 0f;

        public static void Begin(TrainingMode mode, string studentId)
        {
            Mode = mode;
            StudentId = string.IsNullOrWhiteSpace(studentId) ? DEFAULT_STUDENT_ID : studentId.Trim();
            SessionId = Guid.NewGuid().ToString("N");
        }

        public static void SetCalibratedHeight(float standingHeadHeight)
        {
            if (standingHeadHeight < MIN_CALIBRATED_HEIGHT || standingHeadHeight > MAX_CALIBRATED_HEIGHT)
            {
                throw new ArgumentOutOfRangeException(nameof(standingHeadHeight), standingHeadHeight,
                    $"Calibrated height must be within [{MIN_CALIBRATED_HEIGHT}, {MAX_CALIBRATED_HEIGHT}] m.");
            }
            CalibratedHeight = standingHeadHeight;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Mode = TrainingMode.Guided;
            StudentId = DEFAULT_STUDENT_ID;
            SessionId = string.Empty;
            CalibratedHeight = 0f;
        }
    }
}
```

- [ ] **Step 5: Chạy test → 4/4 PASS (tổng 10).**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Core Assets/Tests/EditMode/GameSessionTests.cs Assets/Tests/EditMode/GameSessionTests.cs.meta
git commit -m "feat(core): add shared game events and cross-scene game session"
```

---

### Task 5: Logic thuần — ScoreLedger, OxygenModel, PlayerRules (TDD)

**Files:**
- Create: `Assets/Tests/EditMode/ScoreLedgerTests.cs`, `OxygenModelTests.cs`, `PlayerRulesTests.cs`
- Create: `Assets/Scripts/Analytics/ScoreLedger.cs`, `Assets/Scripts/Player/OxygenModel.cs`, `Assets/Scripts/Player/PlayerRules.cs`

**Interfaces:**
- `ScoreLedger.Add(int, string, float)`, `Deduct(int, string, float)`, `Total`, `Entries`, `static Format(Entry)`
- `OxygenModel(float max)`, `Tick(float dt, bool breathingSmoke, float depletionRate, float recoveryRate) -> bool justDepleted`, `Deplete(float) -> bool`, `Recover(float)`, `Reset()`
- `CrouchRules.ComputeThreshold(float calibratedHeight, float fallback) -> float` (0.7 × H₀ nếu đã hiệu chuẩn)
- `BreathingRules.IsBreathingSmoke(float headWorldY, float smokeLayerY, bool isCrouching) -> bool`

- [ ] **Step 1: Viết 3 file test (FAIL)**

`ScoreLedgerTests.cs`:
```csharp
using System;
using NUnit.Framework;
using EscapeFire.Analytics;

namespace EscapeFire.Tests
{
    public class ScoreLedgerTests
    {
        [Test]
        public void AddAndDeduct_UpdateTotalAndEntries()
        {
            var ledger = new ScoreLedger();
            ledger.Add(20, "Ngắt Aptomat", 5f);
            ledger.Deduct(15, "Mở cửa vội", 30f);

            Assert.AreEqual(5, ledger.Total);
            Assert.AreEqual(2, ledger.Entries.Count);
            Assert.AreEqual(-15, ledger.Entries[1].Delta);
        }

        [Test]
        public void NegativePoints_Throw()
        {
            var ledger = new ScoreLedger();
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Add(-1, "x", 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Deduct(-1, "x", 0f));
        }

        [Test]
        public void Format_MatchesLegacyLogFormat()
        {
            var ledger = new ScoreLedger();
            ledger.Add(10, "Báo cháy", 3.26f);
            ledger.Deduct(15, "Mở cửa", 12f);

            Assert.AreEqual("[+10 pts] Báo cháy (Time: 3.3s)", ScoreLedger.Format(ledger.Entries[0]));
            Assert.AreEqual("[-15 pts] Mở cửa (Time: 12.0s)", ScoreLedger.Format(ledger.Entries[1]));
        }
    }
}
```
> Dùng `3.26f` (không phải `3.25f`) để tránh phụ thuộc quy tắc làm tròn điểm giữa — test chỉ khóa **định dạng**.

`OxygenModelTests.cs`:
```csharp
using NUnit.Framework;
using EscapeFire.Player;

namespace EscapeFire.Tests
{
    public class OxygenModelTests
    {
        [Test]
        public void Tick_NotBreathingSmoke_DoesNotDeplete()
        {
            var model = new OxygenModel(100f);
            model.Tick(5f, breathingSmoke: false, depletionRate: 10f, recoveryRate: 5f);
            Assert.AreEqual(100f, model.Current, 1e-4f);
        }

        [Test]
        public void Tick_BreathingSmoke_DepletesAndReportsOnceWhenEmpty()
        {
            var model = new OxygenModel(100f);
            Assert.IsFalse(model.Tick(5f, true, 10f, 5f));
            Assert.AreEqual(50f, model.Current, 1e-4f);

            Assert.IsTrue(model.Tick(6f, true, 10f, 5f));
            Assert.AreEqual(0f, model.Current, 1e-4f);

            Assert.IsFalse(model.Tick(1f, true, 10f, 5f), "Depletion must only be reported once");
        }

        [Test]
        public void Tick_RecoversButNeverExceedsMax()
        {
            var model = new OxygenModel(100f);
            model.Deplete(30f);
            model.Tick(100f, false, 10f, 5f);
            Assert.AreEqual(100f, model.Current, 1e-4f);
        }

        [Test]
        public void Depleted_DoesNotRecover()
        {
            var model = new OxygenModel(100f);
            model.Deplete(100f);
            model.Tick(10f, false, 10f, 5f);
            Assert.IsTrue(model.IsDepleted);
        }
    }
}
```

`PlayerRulesTests.cs`:
```csharp
using NUnit.Framework;
using EscapeFire.Player;

namespace EscapeFire.Tests
{
    public class PlayerRulesTests
    {
        [Test]
        public void CrouchThreshold_UsesSeventyPercentOfCalibratedHeight()
        {
            Assert.AreEqual(1.19f, CrouchRules.ComputeThreshold(1.70f, 1.2f), 1e-4f);
        }

        [Test]
        public void CrouchThreshold_NotCalibrated_UsesFallback()
        {
            Assert.AreEqual(1.2f, CrouchRules.ComputeThreshold(0f, 1.2f), 1e-4f);
        }

        [Test]
        public void BreathingSmoke_StandingWithHeadInsideSmokeLayer()
        {
            Assert.IsTrue(BreathingRules.IsBreathingSmoke(headWorldY: 1.6f, smokeLayerY: 1.5f, isCrouching: false));
        }

        [Test]
        public void NotBreathingSmoke_HeadBelowLayer()
        {
            Assert.IsFalse(BreathingRules.IsBreathingSmoke(1.6f, 3.2f, false));
        }

        [Test]
        public void NotBreathingSmoke_WhenCrouching()
        {
            Assert.IsFalse(BreathingRules.IsBreathingSmoke(1.1f, 1.0f, true));
        }
    }
}
```

- [ ] **Step 2: Chạy test → FAIL (compile).**

- [ ] **Step 3: Viết `ScoreLedger.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EscapeFire.Analytics
{
    /// <summary>Pure score bookkeeping (no Unity dependency) used by ScoringManager.</summary>
    public sealed class ScoreLedger
    {
        public readonly struct Entry
        {
            public readonly int Delta;
            public readonly string Reason;
            public readonly float Time;
            public Entry(int delta, string reason, float time) { Delta = delta; Reason = reason; Time = time; }
        }

        private readonly List<Entry> _entries = new List<Entry>(32);

        public int Total { get; private set; }
        public IReadOnlyList<Entry> Entries => _entries;

        public Entry Add(int points, string reason, float time)
        {
            if (points < 0) throw new ArgumentOutOfRangeException(nameof(points), "Use Deduct for penalties.");
            return Record(points, reason, time);
        }

        public Entry Deduct(int points, string reason, float time)
        {
            if (points < 0) throw new ArgumentOutOfRangeException(nameof(points), "Pass a positive penalty.");
            return Record(-points, reason, time);
        }

        public static string Format(Entry entry)
        {
            string sign = entry.Delta >= 0 ? "+" : "-";
            return string.Format(CultureInfo.InvariantCulture, "[{0}{1} pts] {2} (Time: {3:F1}s)",
                sign, Math.Abs(entry.Delta), entry.Reason, entry.Time);
        }

        private Entry Record(int delta, string reason, float time)
        {
            var entry = new Entry(delta, reason ?? string.Empty, time);
            Total += delta;
            _entries.Add(entry);
            return entry;
        }
    }
}
```

- [ ] **Step 4: Viết `OxygenModel.cs`**

```csharp
using UnityEngine;

namespace EscapeFire.Player
{
    /// <summary>Pure oxygen state. Tick/Deplete return true only on the frame oxygen reaches zero.</summary>
    public sealed class OxygenModel
    {
        public float Max { get; }
        public float Current { get; private set; }
        public bool IsDepleted => Current <= 0f;

        public OxygenModel(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
        }

        public void Reset() => Current = Max;

        public bool Tick(float deltaTime, bool breathingSmoke, float depletionRate, float recoveryRate)
        {
            if (IsDepleted) return false;
            if (breathingSmoke) return Deplete(depletionRate * deltaTime);
            Recover(recoveryRate * deltaTime);
            return false;
        }

        public bool Deplete(float amount)
        {
            if (IsDepleted || amount <= 0f) return false;
            Current = Mathf.Max(0f, Current - amount);
            return IsDepleted;
        }

        public void Recover(float amount)
        {
            if (IsDepleted || amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
        }
    }
}
```

- [ ] **Step 5: Viết `PlayerRules.cs`**

```csharp
namespace EscapeFire.Player
{
    public static class CrouchRules
    {
        /// <summary>Spec SCENE_FLOW §2.1: crouching means head height &lt; 0.7 × H₀.</summary>
        public const float CROUCH_RATIO = 0.7f;

        public static float ComputeThreshold(float calibratedHeight, float fallbackThreshold)
        {
            return calibratedHeight > 0f ? calibratedHeight * CROUCH_RATIO : fallbackThreshold;
        }
    }

    public static class BreathingRules
    {
        /// <summary>Player inhales smoke when standing and the head is at/above the lowering smoke layer.</summary>
        public static bool IsBreathingSmoke(float headWorldY, float smokeLayerY, bool isCrouching)
        {
            return !isCrouching && headWorldY >= smokeLayerY;
        }
    }
}
```

- [ ] **Step 6: Chạy test → toàn bộ PASS (tổng 22).**

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Analytics/ScoreLedger.cs* Assets/Scripts/Player/OxygenModel.cs* Assets/Scripts/Player/PlayerRules.cs* Assets/Tests/EditMode
git commit -m "feat(core): extract pure score, oxygen and posture rules with tests"
```

---

### Task 6: Nối MonoBehaviour hiện có vào EventBus & model (sửa B1–B6)

**Files (Modify):** toàn bộ 14 script trong `Assets/Scripts/*` (trừ `TelemetryLogger.cs`).

**Nguyên tắc:** giữ nguyên tên class, `[SerializeField]`, method public và C# `event` cũ; chỉ **thêm** `EventBus<...>.Publish(...)` ngay cạnh mỗi `event?.Invoke(...)`. Thêm `using EscapeFire.Core;` khi cần.

- [ ] **Step 1: Publisher đơn giản** — thêm 1 dòng publish cạnh lời gọi event cũ:

| File | Vị trí | Thêm |
| :--- | :--- | :--- |
| `CircuitBreakerSwitch.cs` | sau `OnPowerStateChanged?.Invoke(IsPowerOn);` | `EventBus<PowerStateChangedEvent>.Publish(new PowerStateChangedEvent(IsPowerOn));` |
| `FireHazard.cs` | sau `OnFireExtinguished?.Invoke();` | `EventBus<FireExtinguishedEvent>.Publish(new FireExtinguishedEvent(GetInstanceID()));` |
| `FireAlarmButton.cs` | sau `OnAlarmActivated?.Invoke();` | `EventBus<AlarmActivatedEvent>.Publish(default);` |
| `FireExtinguisher.cs` | sau `OnSafetyPinPulled?.Invoke();` | `EventBus<ExtinguisherPinPulledEvent>.Publish(new ExtinguisherPinPulledEvent(type));` |
| `FireExtinguisher.cs` | sau `OnShockHazardTriggered?.Invoke();` | `EventBus<ShockHazardEvent>.Publish(new ShockHazardEvent(type));` |
| `HealthSystem.cs` | sau `OnPlayerDied?.Invoke(cause);` | `EventBus<PlayerDiedEvent>.Publish(new PlayerDiedEvent(cause));` |
| `NPCSimulator.cs` | trong `RescueNPC()`, sau `IsRescued = true;` | `EventBus<NpcRescuedEvent>.Publish(new NpcRescuedEvent(role));` |
| `DoorHeatChecker.cs` | cuối `CheckDoorHeatWithHand()` (sau khối `if (isDoorHot)`) | `EventBus<DoorHeatCheckedEvent>.Publish(new DoorHeatCheckedEvent(isDoorHot));` |

- [ ] **Step 2: `ScoringManager.cs` dùng `ScoreLedger`**

Thay `TotalScore` setter và `_actionLogs` bằng ledger; giữ nguyên public API:
```csharp
private readonly ScoreLedger _ledger = new ScoreLedger();
public int TotalScore => _ledger.Total;

public void AddScore(int points, string reason)
{
    var entry = _ledger.Add(points, reason, GetElapsedSeconds());
    Debug.Log($"[Scoring] {ScoreLedger.Format(entry)}");
    EventBus<ScoreChangedEvent>.Publish(new ScoreChangedEvent(entry.Delta, _ledger.Total, reason));
}

public void DeductScore(int points, string reason)
{
    var entry = _ledger.Deduct(points, reason, GetElapsedSeconds());
    Debug.LogWarning($"[Scoring] {ScoreLedger.Format(entry)}");
    EventBus<ScoreChangedEvent>.Publish(new ScoreChangedEvent(entry.Delta, _ledger.Total, reason));
}
```
Trong `ExportJSONReport`, `ComplianceLogs` = danh sách `ScoreLedger.Format(e)` cho mỗi entry (dựng bằng vòng `for`, không LINQ). Tham số mặc định `studentId` đổi thành `string studentId = null` và dùng `studentId ?? GameSession.StudentId`; thêm `SessionId = GameSession.SessionId` vào object report.

- [ ] **Step 3: `VRHeightDetector.cs` (B5)**

```csharp
public float EffectiveThreshold => CrouchRules.ComputeThreshold(GameSession.CalibratedHeight, crouchThresholdHeight);

public void UpdateHeight(float height)
{
    CurrentHeight = height;
    bool newlyCrouching = height < EffectiveThreshold;
    if (newlyCrouching != IsCrouching)
    {
        IsCrouching = newlyCrouching;
        OnCrouchStateChanged?.Invoke(IsCrouching);
        EventBus<CrouchStateChangedEvent>.Publish(new CrouchStateChangedEvent(IsCrouching, height));
    }
}
```
Thêm property `public Transform HeadTransform => headTransform;` để `OxygenSystem` đọc độ cao thế giới.

- [ ] **Step 4: `SmokeDensityController.cs` (B2)**

- Chuyển `CurrentSmokeHeight = initialSmokeHeight;` từ `Start()` sang `Awake()` (để object khác đọc được ngay frame đầu).
- Điều kiện hạ khói đổi thành:
```csharp
if (targetFire != null && targetFire.isActiveAndEnabled && !targetFire.IsExtinguished)
```

- [ ] **Step 5: `OxygenSystem.cs` (B1) dùng `OxygenModel` + `BreathingRules`**

```csharp
[Header("References")]
[SerializeField] private VRHeightDetector heightDetector;
[SerializeField] private SmokeDensityController smokeController; // null = no smoke in scene

private OxygenModel _model;

public float CurrentOxygen => _model.Current;
public bool IsDepleted => _model.IsDepleted;

private void Awake()
{
    if (heightDetector == null) heightDetector = GetComponent<VRHeightDetector>();
    if (smokeController == null) smokeController = FindFirstObjectByType<SmokeDensityController>();
    _model = new OxygenModel(maxOxygen);
    ResetOxygen();
}

public void ResetOxygen()
{
    _model = new OxygenModel(maxOxygen);
    OnOxygenChanged?.Invoke(_model.Current);
}

private void Update()
{
    bool breathingSmoke = smokeController != null
        && heightDetector != null
        && heightDetector.HeadTransform != null
        && BreathingRules.IsBreathingSmoke(heightDetector.HeadTransform.position.y,
                                           smokeController.CurrentSmokeHeight,
                                           heightDetector.IsCrouching);

    float before = _model.Current;
    bool justDepleted = _model.Tick(Time.deltaTime, breathingSmoke, depletionRateInSmoke, recoveryRate);
    if (!Mathf.Approximately(before, _model.Current)) OnOxygenChanged?.Invoke(_model.Current);
    if (justDepleted) RaiseDepleted();
}

public void DepleteOxygen(float amount)
{
    if (_model.Deplete(amount)) { OnOxygenChanged?.Invoke(_model.Current); RaiseDepleted(); }
    else OnOxygenChanged?.Invoke(_model.Current);
}

public void RecoverOxygen(float amount)
{
    _model.Recover(amount);
    OnOxygenChanged?.Invoke(_model.Current);
}

private void RaiseDepleted()
{
    OnOxygenDepleted?.Invoke();
    EventBus<OxygenDepletedEvent>.Publish(default);
}
```
Giữ property `MaxOxygen` như cũ. Lưu ý: `ResetOxygen` tạo model mới chỉ khi reset (không phải mỗi frame) → không vi phạm Zero-GC.

- [ ] **Step 6: `LabProgressiveScenarioController.cs` (B3, B4, B6)**

1. Bỏ các field + auto-discover `circuitBreaker`, `oxygenSystem`, `healthSystem` **khỏi logic** (giữ `[SerializeField]` để không mất dữ liệu scene, đánh dấu `[Tooltip("Deprecated: wired via EventBus")]`).
2. `serverFire` tìm bằng `FindFirstObjectByType<FireHazard>(FindObjectsInactive.Include)`.
3. Đăng ký qua bus:
```csharp
private void OnEnable()
{
    EventBus<PowerStateChangedEvent>.Subscribe(HandlePowerStateChanged);
    EventBus<FireExtinguishedEvent>.Subscribe(HandleFireExtinguished);
    EventBus<OxygenDepletedEvent>.Subscribe(HandlePlayerSuffocated);
    EventBus<PlayerDiedEvent>.Subscribe(HandlePlayerDied);
}

private void OnDisable()
{
    EventBus<PowerStateChangedEvent>.Unsubscribe(HandlePowerStateChanged);
    EventBus<FireExtinguishedEvent>.Unsubscribe(HandleFireExtinguished);
    EventBus<OxygenDepletedEvent>.Unsubscribe(HandlePlayerSuffocated);
    EventBus<PlayerDiedEvent>.Unsubscribe(HandlePlayerDied);
}

private void OnDestroy()
{
    if (Instance == this) Instance = null;
}
```
4. Đổi chữ ký handler: `HandlePowerStateChanged(PowerStateChangedEvent e)` dùng `e.IsPowerOn`; `HandleFireExtinguished(FireExtinguishedEvent e)` thêm guard `if (serverFire != null && e.FireInstanceId != serverFire.GetInstanceID()) return;`; `HandlePlayerSuffocated(OxygenDepletedEvent _)`; `HandlePlayerDied(PlayerDiedEvent e)`.
5. Thêm guard đầu `SetPhase`: nếu đã ở `Phase4_CompletedWin`/`Phase5_GameOverFail` thì bỏ qua (tránh chết sau khi đã thắng).
6. Gom tất cả `OnAIMentorMessage?.Invoke(x)` thành:
```csharp
public void SetAIMentorGuidance(string message)
{
    if (string.IsNullOrEmpty(message)) return;
    OnAIMentorMessage?.Invoke(message);
    EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(message));
}
```
7. Cuối `SetPhase`: thêm `EventBus<PhaseChangedEvent>.Publish(new PhaseChangedEvent(CurrentPhase));`.

- [ ] **Step 7: `VRWristHUD.cs` (B3, B4)**

- `FindObjectOfType<T>()` → `FindFirstObjectByType<T>()` (3 chỗ, trong `Start`).
- Bỏ đăng ký `LabProgressiveScenarioController.Instance.OnAIMentorMessage`; thay bằng:
```csharp
private void OnEnable()  => EventBus<MentorMessageEvent>.Subscribe(HandleMentorMessage);
private void OnDisable() => EventBus<MentorMessageEvent>.Unsubscribe(HandleMentorMessage);
private void HandleMentorMessage(MentorMessageEvent e) => UpdateAIMentorText(e.Message);
```
- (Không xử lý zero-GC của string interpolation trong `Update()` ở M0 — ghi chú cho M1: chỉ cập nhật text khi giá trị đổi.)

- [ ] **Step 8: Compile + test + soát cảnh báo**

`refresh_unity` → `read_console`: 0 error, **không còn** warning `FindObjectOfType`. `run_tests` EditMode: 22/22 PASS.

```bash
grep -rn "FindObjectOfType" Assets/Scripts
```
Kỳ vọng: không có kết quả.

- [ ] **Step 9: Commit (tách 2 commit cho dễ review)**

```bash
git add Assets/Scripts/Interactions Assets/Scripts/Hazards/FireHazard.cs Assets/Scripts/Player/HealthSystem.cs Assets/Scripts/NPC Assets/Scripts/Analytics/ScoringManager.cs
git commit -m "refactor(events): publish gameplay events through event bus"
git add Assets/Scripts/Player Assets/Scripts/Hazards/SmokeDensityController.cs Assets/Scripts/Scenario Assets/Scripts/UI
git commit -m "fix(gameplay): gate oxygen loss on smoke and wire controller via bus"
```

---

### Task 7: Dọn Samples & Build Settings

**Files:**
- Delete (nếu không bị tham chiếu): `Assets/FrostweepGames/GCSpeechRecognition/V1/GCSR_Examples/`, `Assets/Samples/XR Hands/1.7.3/HandVisualizer/`, `Assets/Samples/XR Interaction Toolkit/3.4.1/Hands Interaction Demo/`
- **Giữ:** `Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/` (XR Origin, input actions, XR Device Simulator rất có thể đang được `BasicScene` dùng)
- Modify: `ProjectSettings/EditorBuildSettings.asset`

- [ ] **Step 1: Kiểm tra tham chiếu GUID trước khi xoá**

```bash
check_unused() {
  dir="$1"
  guids=$(grep -rhoE "^guid: [0-9a-f]{32}" "$dir" --include="*.meta" | awk '{print $2}')
  hits=0
  for g in $guids; do
    if grep -rlq "$g" Assets ProjectSettings --include="*.unity" --include="*.prefab" --include="*.asset" --include="*.mat" --include="*.controller" --include="*.asmdef" --exclude-dir="$(basename "$dir")" 2>/dev/null; then
      echo "REFERENCED: $g"; hits=$((hits+1))
    fi
  done
  echo "$dir -> $hits external references"
}
check_unused "Assets/FrostweepGames/GCSpeechRecognition/V1/GCSR_Examples"
check_unused "Assets/Samples/XR Hands/1.7.3/HandVisualizer"
check_unused "Assets/Samples/XR Interaction Toolkit/3.4.1/Hands Interaction Demo"
```
Chỉ xoá thư mục có **0 external references**. Thư mục nào có tham chiếu → giữ lại, ghi chú vào roadmap.

- [ ] **Step 2: Xoá trong Unity** (để Unity tự dọn `.meta`): qua MCP `manage_asset` action `delete`, hoặc Project window → Delete. Sau đó `refresh_unity` → 0 error, test vẫn 22/22.

- [ ] **Step 3: Build Settings**

`File > Build Profiles > Scene List`: thêm `Assets/Scenes/BasicScene.unity` ở index 0, bỏ tick `SampleScene` (không xoá file ở M0; M1 sẽ tạo 3 scene chính thức).

- [ ] **Step 4: Commit**

```bash
git add -A Assets/FrostweepGames Assets/Samples ProjectSettings/EditorBuildSettings.asset
git commit -m "chore(clean): remove unused sample scenes and set lab scene in build"
```

---

### Task 8: Kiểm thử chơi thực tế & chốt M0

**Files:**
- Modify: `docs/superpowers/plans/2026-11-04-development-roadmap.md` (đánh dấu M0 xong)

- [ ] **Step 1: Play Mode với XR Device Simulator trong `BasicScene`** — kiểm tra thủ công:
  1. Phase 0 đứng thẳng 30s → **oxy giữ 100%** (B1 đã sửa).
  2. Phase 0/1 → khói **không** hạ (B2).
  3. Phase 2 cháy, đứng thẳng khi khói xuống ngang đầu → oxy giảm; khom → hồi oxy.
  4. Ngắt aptomat → console có `[Scoring] [+20 pts] Ngắt Aptomat...`.
  5. Wrist HUD hiện câu AI Mentor mỗi lần đổi phase.
  6. Dập lửa → Phase 3; gọi `TriggerHallwayEvacuationWin()` (qua MCP `execute_code` hoặc nút debug) → Phase 4, sau đó oxy về 0 **không** chuyển sang Phase 5.
  7. Thoát Play Mode, vào lại → không có log trùng lặp (bus đã reset).

- [ ] **Step 2: Chụp console sạch + kết quả test làm bằng chứng** (lưu vào `Logs/`, không commit).

- [ ] **Step 3: Cập nhật roadmap** — dưới tiêu đề M0 thêm dòng `**Trạng thái:** ✅ Hoàn thành 2026-xx-xx (22 EditMode test, B1–B6 đã sửa)`, và trong mục "Lỗi cần sửa trong plan 2026-10-08" gạch lỗi #2 (`FindObjectOfType`) và #6 (`SetAIMentorGuidance`) là đã xử lý.

- [ ] **Step 4: Commit & merge về `develop/server`**

```bash
git add docs/superpowers/plans/2026-11-04-development-roadmap.md
git commit -m "docs(plan): mark m0 foundation complete"
git checkout develop/server
git merge --no-ff feature/m0-foundation
```
(Push chỉ khi chủ repo đồng ý.)

---

## Definition of Done (M0)
- [ ] 0 compile error, 0 warning `FindObjectOfType` trong `Assets/Scripts`.
- [ ] ≥ 22 EditMode test PASS.
- [ ] `BasicScene` không có "Missing Script"; checklist Task 8 Step 1 đạt 7/7.
- [ ] B1–B6 đã sửa; mọi subscriber huỷ đăng ký trong `OnDisable`.
- [ ] Không còn sample demo không dùng (hoặc có ghi chú lý do giữ lại).

## Ngoài phạm vi M0 (để M1+)
- Xử lý `ShockHazardEvent` → Game Over (M1, cùng điều kiện thắng/thua).
- Chuyển các lời gọi `ScoringManager.Instance.AddScore` trong Interactions sang listener tập trung (M1, khi làm ma trận điểm AAR).
- XRI interactable thật, 3 scene chính, Editor asmdef (M1).

## Self-Review
- **Spec coverage:** roadmap M0 yêu cầu EventBus ✔, GameSession ✔, asmdef ✔ (Editor asmdef dời sang M1 vì chưa có editor script — YAGNI), ≥10 test ✔ (22), dọn Samples ✔, xác nhận MCP ✔ (Task 1 Step 3).
- **Placeholder:** không có TODO/TBD; chỗ duy nhất phụ thuộc máy là đường dẫn `Unity.exe` (đã ghi cách tra).
- **Type consistency:** tên event ở Task 4 khớp với Task 6; `OxygenModel.Tick` trả `bool` dùng đúng ở Task 6 Step 5; `CrouchRules`/`BreathingRules` khớp test.
