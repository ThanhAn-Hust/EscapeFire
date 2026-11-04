# EscapeFire — Lộ trình Phát triển Tổng thể (Development Roadmap)

> **For agentic workers:** Đây là roadmap cấp milestone. Mỗi milestone khi bắt đầu phải được tách thành plan chi tiết riêng bằng `superpowers:writing-plans`, rồi thực thi bằng `superpowers:subagent-driven-development` hoặc `superpowers:executing-plans`.

**Ngày lập:** 2026-11-04 · **Branch:** `develop/server` · **Engine:** Unity 6000.3.15f1, URP 17.3, XRI 3.4.1, OpenXR → Meta Quest 2

---

## 0. Hiện trạng (đã kiểm tra trong repo)

| Hạng mục | Trạng thái |
| :--- | :--- |
| Tài liệu thiết kế | Rất đầy đủ: strategy 20 mục, scene flow, NPC timeline, environment spec, spec LLM 2 lớp, plan 2026-10-08 (6 task) |
| Code gameplay (`Assets/Scripts`) | 14 script nền tảng: Fire/Smoke, Extinguisher, Breaker, Alarm, DoorHeat, NPC (Minh/Hoa), Health/Oxygen/Height, ScoringManager, TelemetryLogger, WristHUD, `LabProgressiveScenarioController` (FSM 6 phase) |
| Scene | Chỉ có `BasicScene` + `SampleScene`. **Chưa có** `00_MainMenu`, `01_ComputerLab_Sim`, `02_AAR_Evaluation` |
| Kiến trúc | Singleton + C# event rời rạc, **chưa có EventBus** (dù spec yêu cầu) |
| Adaptive / AI | Chưa có code: CF tracker, Governor, FM-200, Micro-Governor đều mới nằm trong plan 10-08 |
| Server (Laptop Edge LLM) | **Chưa có gì** — chưa có thư mục `Server/` |
| Test | Chưa có asmdef / EditMode test |
| Unity MCP | Package `com.coplaydev.unity-mcp` đã có trong `manifest.json`; `.mcp.json` đã thêm cho Claude Code |

### Lỗi cần sửa trong plan 2026-10-08 trước khi thực thi
1. `FM200System` gọi `OxygenSystem.TakeDamage(100f)` — method không tồn tại (OxygenSystem có `DepleteOxygen`, `TakeDamage` thuộc `HealthSystem`).
2. `FindObjectOfType` đã deprecated trong Unity 6 → dùng `FindFirstObjectByType` hoặc inject qua `[SerializeField]`.
3. Interface ghi `ExecuteDirective(string)` nhưng code dùng `ExecuteDirective(MicroDirective)` → thống nhất enum, thêm parser string→enum cho directive từ server.
4. `CognitiveFrictionTracker` bắn `OnHighCognitiveFrictionDetected` **mỗi frame** khi CF ≥ 0.75 → spam guidance. Cần hysteresis (vào ≥ 0.75, ra < 0.65) + cooldown.
5. Tên directive giữa plan (`EscalateHazardSecondary`, `TriggerDoorJam`) và spec (`ESCALATE_SECONDARY_FIRE`, `TRIGGER_CO2_PIN_JAM`, `TRIGGER_ALARM_CABLE_CUT`…) không khớp → lấy **spec làm chuẩn**.
6. Plan dùng `MicroAIGovernor` gọi `LabProgressiveScenarioController.SetAIMentorGuidance` — method chưa tồn tại (hiện chỉ có event `OnAIMentorMessage`).

---

## 1. Milestones

### M0 — Nền móng kỹ thuật (≈ 1 tuần)
**Mục tiêu:** dự án sạch, có kiến trúc chung và test được trước khi viết thêm tính năng.
- `EscapeFire.Core`: `EventBus` typed (struct event, Subscribe/Publish/Unsubscribe), `GameSession` (mode Guided/Exam, H₀, session id).
- Assembly definitions: `EscapeFire.Runtime`, `EscapeFire.Editor`, `EscapeFire.Tests.EditMode`.
- Chuyển các singleton hiện có sang publish qua EventBus (giữ API cũ để không vỡ scene).
- Dọn `Assets/Samples` & scene demo Frostweep không dùng; kiểm tra Git LFS cho `.fbx/.png/.wav`.
- Xác nhận Unity MCP chạy được (`refresh_unity`, đọc console) để agent tự kiểm tra compile.

**Done khi:** compile sạch, ≥ 10 EditMode test xanh (EventBus, Scoring, Oxygen, Height), scene cũ vẫn chạy.

### M1 — Vòng chơi cốt lõi chơi được end-to-end (≈ 2 tuần)
**Mục tiêu:** một lượt diễn tập hoàn chỉnh trong headset/XR Device Simulator.
- Tạo 3 scene theo `SCENE_FLOW_AND_SCREENS.md`; `SceneFlowManager` load async + fade.
- `00_MainMenu`: hiệu chuẩn chiều cao H₀, chọn Guided/Exam.
- `01_ComputerLab_Sim`: gắn XRI `XRGrabInteractable`/`XRSimpleInteractable` thật cho bình (rút chốt, bóp cò), aptomat, nút báo cháy, tay nắm cửa; haptic impulse.
- Điều kiện thắng/thua (qua ngưỡng cửa / O₂ = 0 / điện giật) → chuyển `02_AAR_Evaluation` với cờ PASSED/FAILED.
- NPC Minh/Hoa theo `NPC_TIMELINE_STORYBOARD.md`.
- Guided mode: highlight viền vàng + mũi tên sàn.

**Done khi:** chơi từ menu → lab → AAR (bảng điểm tĩnh) không lỗi, giữ ≥ 72 FPS trên Quest 2 với scene hiện tại.

### M2 — Adaptive cục bộ trên kính (≈ 1.5 tuần)
**Mục tiêu:** thực thi plan 2026-10-08 (Task 1–4) **sau khi sửa 6 lỗi ở mục 0**.
- `PerformanceBudgetGovernor` (≤ 4 lửa, ≤ 5 khói) — mọi spawn bắt buộc đi qua.
- `CognitiveFrictionTracker` (cửa sổ 2s, hysteresis).
- Anomalies: FM-200 đếm ngược 30s, kẹt chốt CO₂, Arc Flash/Blackout khi xịt nước lúc điện ON.
- `MicroAIGovernor` + **Lớp 2 Validator C#** (budget / lối thoát / trạng thái tiên quyết) — chạy được kể cả khi không có server.

**Done khi:** test EditMode cho CF formula, validator 3 chốt; demo leo thang khi CF thấp và trấn an khi CF cao.

### M3 — Edge Server trên Laptop (≈ 2 tuần, làm trên `feature/m3-edge-server`)
**Mục tiêu:** AI Director vĩ mô bằng LLM theo spec 2026-10-08.
- Thư mục `Server/` (ngoài `Assets/`): Python 3.12 + `uv`, FastAPI + WebSocket.
- Kênh `ws://<laptop>:8765/session`: kính gửi telemetry mỗi 10s/sự kiện mốc; server trả directive JSON.
- LLM adapter: Ollama (Gemma) mặc định, Cloud API tùy chọn; timeout → `MAINTAIN_CURRENT_PACE`.
- **Lớp 1 Validator**: JSON schema + whitelist directive + `pccc_rule_reference` bắt buộc.
- Unity client: `EdgeDirectorClient` (WebSocket, reconnect, offline fallback dùng M2).
- Log toàn bộ prompt/response để phục vụ đánh giá luận văn.
- pytest cho validator & prompt builder; mock LLM.

**Done khi:** latency LAN ≤ 50 ms, directive sai schema bị loại 100% trong test, rút dây mạng giữa phiên game vẫn chạy.

### M4 — Giọng nói Socratic (≈ 1 tuần)
- `DeterministicVoiceTriggerManager`: 5 tình huống trong spec mục 4.1, phát qua Overtone offline TTS (0 ms logic).
- Walkie-talkie: STT (Frostweep Google Cloud — cần API key, chỉ chạy khi có mạng) → lệnh báo động bằng giọng nói cho anomaly `TRIGGER_ALARM_CABLE_CUT`.

**Done khi:** 5 trigger kích hoạt đúng điều kiện trong test scene, không trigger lặp trong cooldown.

### M5 — AAR & Learning Analytics (≈ 1.5 tuần)
- Ghi quỹ đạo + tư thế mỗi 0.2s → heatmap line 3D trên sa bàn mini-map.
- Scorecard 6 tiêu chí (100 điểm) + radar 5 trục (`stress_resilience = 1 − avgCF`).
- Generative AAR: server sinh nhận xét cá nhân hóa từ telemetry (có fallback template).
- Export JSON + POST `/api/v1/sessions/{id}/complete` (LMS daotao.ai) với hàng đợi offline.

### M6 — Tối ưu Quest 2, build & đánh giá (≈ 1.5 tuần)
- Profiling trên thiết bị: frame ≤ 13.8 ms, CPU logic ≤ 2 ms; LOD, occlusion, bake light, giảm overdraw khói.
- Build APK, cấu hình OpenXR Meta Quest feature.
- Kịch bản thử nghiệm người dùng (pilot 5–10 sinh viên), so sánh Guided vs Exam vs Adaptive.
- Video demo + số liệu cho báo cáo.

---

## 2. Thứ tự & phụ thuộc

```
M0 ──► M1 ──► M2 ──┬──► M4 ──► M5 ──► M6
                   └──► M3 ─────┘
```
M3 (server, Python) có thể làm **song song** với M4 vì khác ngôn ngữ/thư mục; chỉ cần chốt schema directive + telemetry ở đầu M2.

## 3. Rủi ro chính

| Rủi ro | Giảm thiểu |
| :--- | :--- |
| Khói/lửa volumetric làm tụt FPS trên XR2 | Governor cứng từ M2, profiling sớm ngay cuối M1 |
| LLM chậm / ảo giác quy trình PCCC | LLM chỉ chọn trong whitelist; 2 lớp validator; fallback local |
| STT cần mạng + API key | Voice chỉ là kênh phụ; mọi tác vụ đều có thao tác tay thay thế |
| Scene YAML conflict khi nhiều người/agent sửa | Tách prefab, hạn chế sửa scene trực tiếp, wire bằng Editor script |
| Tài liệu đi trước code quá xa | Mỗi milestone phải có demo chạy được trước khi viết thêm spec |

## 4. Bước tiếp theo đề xuất
1. Duyệt roadmap này.
2. Viết plan chi tiết cho **M0** (`superpowers:writing-plans`).
3. Cập nhật plan 2026-10-08 sửa 6 lỗi ở mục 0 trước khi tới M2.
