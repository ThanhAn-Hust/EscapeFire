# QUY CHUẨN LẬP TRÌNH & NGUYÊN TẮC PHÁT TRIỂN (CODING STANDARDS & AGENT RULES)
**Dự án:** VR Emergency Response & Safety Simulator (EscapeFire)  
**Tài liệu tham chiếu:** Embedded Hermes Directive & Unity XR Clean Code Guidelines.

---

## 1. NGUYÊN TẮC CỐT LÕI (CORE PRINCIPLES)

1. **YAGNI & Ponytail Principle (Giải pháp tối thiểu, không over-engineer):**
   * Không viết các abstract pattern phức tạp nếu bài toán chỉ cần một MonoBehaviour đơn giản.
   * Ưu tiên C# Standard Library, Unity Built-in API (`UnityEngine`, `UnityEngine.XR.Interaction.Toolkit`) trước khi cài thêm bất kỳ 3rd-party plugin nào.
2. **Kỷ luật Hiệu năng VR Standalone (Quest 72/90 FPS Target):**
   * Tuyệt đối **KHÔNG cấp phát bộ nhớ động (Zero Garbage Collection Allocation)** trong các hàm vòng lặp thường xuyên (`Update()`, `FixedUpdate()`, `LateUpdate()`).
   * Cấm gọi `new`, `GetComponent<T>()`, `FindObjectOfType<T>()`, `GameObject.Find()` bên trong `Update()`. Toàn bộ cache phải thực hiện ở `Awake()` hoặc `Start()`.
   * Sử dụng Object Pooling cho các hiệu ứng hạt particle (tia lửa, mảnh vỡ) thay vì `Instantiate()` và `Destroy()` liên tục.
3. **Deterministic Guardrails (Bảo vệ tính tất định của kịch bản):**
   * Logic quy chuẩn PCCC (Cắt điện $\to$ Chọn bình $\to$ Thoát hiểm) là **bất di bất dịch** và được điều khiển bằng C# State Machine (`LabProgressiveScenarioController.cs`).
   * Không bao giờ phụ thuộc 100% vào mạng internet hay LLM API để vận hành game loop. Nếu LLM mất kết nối, hệ thống tự động rơi về chế độ Fallback với các câu thoại và nhánh kịch bản cố định có sẵn.

---

## 2. QUY CHUẨN CẤU TRÚC CODE C# (C# ARCHITECTURE RULES)

### 2.1. Phân chia Namespace rõ ràng
Mọi file script mới phải nằm trong root namespace `EscapeFire.*`:
* `EscapeFire.Scenario`: Quản lý Phase, Director và vòng đời kịch bản.
* `EscapeFire.Hazards`: Lửa, khói thể tích, hồ quang điện, nổ tụ điện.
* `EscapeFire.Interactions`: Kế thừa từ `XRBaseInteractable` hoặc `XRGrabInteractable` (Bình CO2, cần gạt Aptomat, tay nắm cửa, nút báo cháy).
* `EscapeFire.Player`: Chiều cao kính VR, hệ thống Oxy, máu, tư thế khom lưng.
* `EscapeFire.NPC`: Hệ thống điều hướng NavMesh, hành vi hoảng loạn, ngạt khói.
* `EscapeFire.Analytics`: Thu thập Telemetry, tính toán điểm $P_t$, xuất báo cáo AAR.
* `EscapeFire.UI`: Wrist HUD, Hologram dashboard.

### 2.2. Format & Quy tắc Đặt Tên
* **Class & Struct:** `PascalCase` (ví dụ: `CircuitBreakerSwitch`, `TelemetryLogger`).
* **Method:** `PascalCase` (ví dụ: `CutPower()`, `CalculatePerformanceIndex()`).
* **Public Property / Event:** `PascalCase` (ví dụ: `CurrentPhase`, `OnPowerCut`).
* **Private Field:** `_camelCase` với dấu gạch dưới (ví dụ: `_isPowerCut`, `_smokeDensity`).
* **Serialized Field trong Inspector:** `camelCase` (ví dụ: `[SerializeField] private float fireExtinguishRadius;`).
* **Hằng số:** `UPPER_SNAKE_CASE` (ví dụ: `MAX_SMOKE_DENSITY`).

---

## 3. QUY TRÌNH QUẢN LÝ GIT & COMMIT CHUẨN HÓA

1. **Quy tắc Nhánh (Branching Rule):**
   * Nhánh làm việc chính của laptop và server: `develop/laptop` và `develop/server`.
   * Mọi tính năng mới viết trên branch `feature/*`, sửa lỗi trên `fix/*`.
   * Luôn thực hiện `git fetch origin` và rebase trước khi push code để tránh conflict.
2. **Quy chuẩn Conventional Commits:**
   * Cú pháp: `<type>(<scope>): <subject>`
   * Các type hợp lệ: `feat`, `fix`, `docs`, `refactor`, `perf`, `chore`.
   * Tiêu đề $\le 72$ ký tự, bắt đầu bằng chữ thường, không có dấu chấm ở cuối.
3. **Collaborative Commit Tagging (Bắt buộc theo Hermes Directive):**
   * Mọi commit phải ghi nhận Author chính và luôn gắn trailer hợp tác ở cuối commit message:
     ```text
     feat(scenario): implement 4-phase progressive emergency event controller
     
     Co-authored-by: Weamis Hermes <hermes@vietnh.io.vn>
     ```
