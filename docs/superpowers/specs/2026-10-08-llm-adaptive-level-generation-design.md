# Thiết kế Hệ thống Sinh Màn chơi Thích ứng bằng LLM & Kiểm duyệt PCCC 2 Lớp (LLM Adaptive Level Generation & Two-Tier PCCC Validation)

**Đề tài:** Mô phỏng Diễn tập Ứng phó Sự cố & PCCC Thực tế ảo Thích ứng Thông minh (AI-Adaptive Immersive Emergency Response Simulator)  
**Tác giả:** Lê Văn Thành An (Sorbonne University / HUST)  
**Nền tảng:** Meta Quest 2 Standalone | Laptop Server Edge LLM (Gemma / Ollama / Cloud API) | Unity 6 URP  

---

## 1. Mục tiêu & Nguyên tắc Thiết kế Cốt lõi
* **Không ảo giác (Zero Hallucination in Fire Safety):** LLM không được phép tự nghĩ ra quy trình PCCC mới. Mọi quyết định tăng độ khó đều phải tuân thủ nghiêm ngặt tiêu chuẩn TCVN 3890 & NFPA 75 (Data Center Fire Protection).
* **Độ trễ = 0ms cho tương tác vật lý & âm thanh khẩn cấp:** Cảnh báo an toàn phát ngay lập tức từ C# cục bộ (Deterministic Event-Triggered TTS), trong khi LLM chạy bất đồng bộ trên Laptop chỉ lo điều phối nhịp kịch bản vĩ mô (Macro Directives).
* **Bảo vệ tuyệt đối 72 FPS:** Mọi lệnh sinh lửa/khói từ LLM đều bị kiểm soát trần hiệu năng cứng bởi `PerformanceBudgetGovernor` trên chip Snapdragon XR2.

---

## 2. Phần 1: Kiến trúc Pipeline Dữ liệu & Cấu trúc Prompting

### 2.1. Nguồn Dữ liệu Tạo Prompt (Context Pipeline on Laptop)
Mỗi chu kỳ đánh giá (định kỳ 10 giây hoặc khi có mốc sự kiện quan trọng), Laptop Server tự động tổng hợp 3 nguồn thông tin:

1. **Bộ Tri thức PCCC Chuẩn (PCCC Ground Truth Knowledge Base - Tĩnh):**
   * *Luật 1 (NFPA 75 Sec 8.4):* Nghiêm cấm sử dụng chất chữa cháy dẫn điện (Nước/Bọt) trên thiết bị điện tử đang mang điện áp.
   * *Luật 2 (NFPA 2001 / TCVN 7161):* Đám cháy phòng máy chủ kéo dài vượt quá ngưỡng cho phép (90s) không dập được sẽ kích hoạt hệ thống xả khí ngạt FM-200. Con người có 30 giây để sơ tán.
   * *Luật 3 (NFPA 101 Life Safety):* Trước khi mở cửa thoát hiểm phải kiểm tra nhiệt độ tay nắm bằng mu bàn tay để tránh hiện tượng cháy bùng do chênh lệch áp suất (Backdraft).
   * *Luật 4 (Life Safety Invariant):* Luôn bảo đảm ít nhất một lối thoát hiểm an toàn có thể tiếp cận được.
2. **Không gian Hành động Hợp lệ (Safe Directive Action Space - Tĩnh):**
   * `ESCALATE_SECONDARY_FIRE`: Cháy lan sang khay hóa chất tẩy mạch hoặc dây cáp bàn kỹ thuật.
   * `TRIGGER_CO2_PIN_JAM`: Chốt an toàn bình CO2 bị kẹt nhẹ, buộc giật 2 lần để rèn luyện phản xạ.
   * `TRIGGER_ALARM_CABLE_CUT`: Nút báo cháy đứt cáp tín hiệu, chuông không kêu, buộc dùng khẩu lệnh giọng nói để báo động.
   * `ESCALATE_FM200_COUNTDOWN`: Đếm ngược 30 giây xả khí ngạt FM-200, buộc chuyển từ dập lửa sang sơ tán khẩn cấp.
   * `MAINTAIN_CURRENT_PACE`: Giữ nguyên màn chơi, người học đang gặp khó khăn hoặc cần thời gian xử lý.
3. **Dữ liệu Telemetry Thời gian thực từ Kính VR (Động):**
   * `cognitive_friction_cf`: Chỉ số ma sát nhận thức $CF \in [0.0, 1.0]$.
   * `time_elapsed`: Thời gian diễn tập tính bằng giây.
   * `is_power_cut`: Trạng thái Aptomat nguồn tổng (`true/false`).
   * `held_item`: Vật thể đang cầm trên tay (`"None"`, `"CO2_Extinguisher"`, `"Water_Extinguisher"`).
   * `current_zone`: Vị trí người học trong phòng (`"ServerZone"`, `"ExitDoorZone"`, `"WorkstationZone"`).

### 2.2. Mẫu Prompt Chuẩn Hóa (Prompt Template)

```text
[SYSTEM PROMPT]
Bạn là AI Giám đốc Huấn luyện PCCC (Data Center Fire Safety Director) cho phòng máy tính theo tiêu chuẩn TCVN 3890 và NFPA 75.
Nhiệm vụ của bạn là đánh giá chỉ số ma sát nhận thức (CF) và hành vi của sinh viên để quyết định xem có nên leo thang thử thách (Escalate Challenge) hay không.

QUY TẮC BẤT DI BẤT DỊCH:
1. Nếu sinh viên đang quá bình tĩnh (CF <= 0.25) và xử lý quá nhanh: Hãy leo thang thử thách để kiểm tra phản xạ ứng biến.
2. Nếu sinh viên đang hoảng loạn (CF >= 0.75): Tuyệt đối KHÔNG thêm thử thách, trả về "MAINTAIN_CURRENT_PACE".
3. Chỉ được chọn 1 Directive trong danh mục cho phép: ["ESCALATE_SECONDARY_FIRE", "TRIGGER_CO2_PIN_JAM", "TRIGGER_ALARM_CABLE_CUT", "ESCALATE_FM200_COUNTDOWN", "MAINTAIN_CURRENT_PACE"].
4. Đầu ra BẮT BUỘC là định dạng JSON duy nhất, không giải thích dài dòng:
{
  "directive": "<DIRECTIVE_NAME>",
  "target_object": "<TARGET_SLOT>",
  "pccc_rule_reference": "<TCVN_HOAC_NFPA_RULE>",
  "pedagogical_reason": "<Lý do sư phạm ngắn gọn>"
}

[USER PROMPT (TELEMETRY INPUT)]
Thời gian: {time_elapsed}s
Chỉ số ma sát nhận thức CF: {cf_value}
Trạng thái Aptomat: {is_power_cut}
Bình đang cầm: {held_item}
Vị trí người học: {current_zone}
Thử thách đã xuất hiện trước đó: {prior_challenges}
Hãy đưa ra quyết định tiếp theo!
```

---

## 3. Phần 2: Bộ Kiểm duyệt 2 Lớp (Two-Tier PCCC Safety & Hardware Validation)

Mọi chỉ thị từ LLM bắt buộc phải đi qua 2 lớp thẩm định nghiêm ngặt trước khi được phép tác động vào không gian Scene:

```
[Laptop: Edge LLM (Gemma)]
         │
         ↓ (Sinh chuỗi JSON thô)
┌──────────────────────────────────────────────┐
│  LỚP 1: JSON Schema & Semantic Validator     │ (Chạy trên Laptop)
│  • Cú pháp JSON có hợp lệ không?             │
│  • "directive" có nằm trong danh mục trắng?  │
└──────────────────────┬───────────────────────┘
                       │ (Hợp lệ: Gửi qua WebSocket / LAN)
                       ↓
┌──────────────────────────────────────────────┐
│  LỚP 2: C# PCCC Safety & Budget Validator    │ (Chạy trên Kính Quest 2)
│  • Chốt 1: Ngân sách FPS (<= 4 lửa, <= 5 khói)│
│  • Chốt 2: Lối thoát an toàn (Không bít kín) │
│  • Chốt 3: Logic hiện trường (Hợp thời điểm) │
└──────────────────────┬───────────────────────┘
                       │
       ┌───────────────┴───────────────┐
       ↓ (ĐẠT)                         ↓ (TỪ CHỐI)
[Kích hoạt thử thách]          [Hủy bỏ an toàn / Giữ nguyên 72 FPS]
```

### 3.1. Chi tiết 3 Chốt Chặn Kỹ Thuật của Lớp 2 (C# trên Quest 2):
1. **Chốt chặn 1: Ngân sách Phần cứng (Hardware Budget Check):**
   * Nếu LLM gửi directive `ESCALATE_SECONDARY_FIRE`:
   * Gọi `PerformanceBudgetGovernor.Instance.CanSpawnFire()`.
   * Nếu số lượng ngọn lửa active $\ge 4$ ➔ **TỪ CHỐI NGAY LẬP TỨC** (`REJECTED_BUDGET_OVERFLOW`), ngăn chặn hoàn toàn hiện tượng tụt FPS gây say VR.
2. **Chốt chặn 2: Bất biến Sinh tồn PCCC (Life Safety Invariant):**
   * Quy chuẩn an toàn cấm tuyệt đối việc tạo ra một tình huống "chắc chắn tử vong" (Unavoidable Death).
   * Nếu vị trí lửa lan mới nằm ngay trên cửa thoát hiểm duy nhất khi người học chưa kịp di chuyển ➔ **TỪ CHỐI** (`REJECTED_TRAPPED_PLAYER`). Thay vào đó, lửa chỉ được lan sang các bàn máy tính hoặc kệ phụ ở rìa phòng.
3. **Chốt chặn 3: Tính nhất quán về Trạng thái (Prerequisite State Check):**
   * Nếu LLM yêu cầu `TRIGGER_CO2_PIN_JAM` (kẹt chốt bình CO2), nhưng sinh viên đã rút chốt an toàn thành công từ trước đó ➔ **TỪ CHỐI** (`REJECTED_ALREADY_UNPINNED`), tránh mâu thuẫn logic hiện trường.

---

## 4. Phần 3: Cơ chế Kích hoạt Giọng nói Socratic Thời gian thực (Deterministic Voice Trigger Engine)

Toàn bộ việc phát hiện sai phạm và phát giọng nói cảnh báo được giao cho **`DeterministicVoiceTriggerManager.cs` chạy cục bộ trên kính Quest 2**, kết nối trực tiếp với thư viện phát âm thanh ngoại tuyến **Overtone Offline TTS**:

```
[Hành vi của Sinh viên trong VR]
               │
               ↓ (Trigger Collider / State Check cục bộ)
┌────────────────────────────────────────────────────────┐
│  `DeterministicVoiceTriggerManager.cs` (Độ trễ = 0ms)  │
│  Kiểm tra điều kiện sai phạm an toàn PCCC              │
└──────────────────────┬─────────────────────────────────┘
                       │
                       ↓ (Gọi trực tiếp Overtone Offline TTS)
[AI Mentor cất giọng Socratic gợi mở chuẩn chỉ qua tai nghe]
```

### 4.1. Ma trận 5 Tình huống Kích hoạt Giọng nói Socratic Chuẩn Nghiệp vụ:

| STT | Tình huống Hiện trường | Điều kiện Kích hoạt Kỹ thuật (Trigger Condition) | Lời thoại Socratic của AI Mentor (Phát qua Overtone TTS) | Ý nghĩa Sư phạm PCCC Thực tế |
| :---: | :--- | :--- | :--- | :--- |
| **1** | **Xách nhầm bình nước lại gần tủ điện** | Cầm bình Bọt/Nước + Tủ điện đang có tải (`IsPowerOn == true`) + Khoảng cách đến tủ điện $< 2.5\text{m}$. | *"Quan sát lại nguồn phát cháy! Nước có an toàn quanh nguồn điện đang mang tải không?"* | Rèn luyện phản xạ: Tuyệt đối không xịt chất dẫn điện vào đám cháy điện sống. |
| **2** | **Đứng thẳng người trong khói độc** | Chiều cao kính VR $Y > 1.2\text{m}$ liên tục quá $3.0\text{s}$ khi mật độ khói $> 0.3$. | *"Khói độc polymer đang tích tụ ở tầng cao! Hãy hạ thấp trọng tâm và khom người ngay lập tức!"* | Rèn luyện phản xạ: Khói nóng bay lên trần, không khí thở an toàn chỉ nằm sát sàn nhà. |
| **3** | **Lao ra mở cửa thoát hiểm khi tay nắm đang bỏng rát** | Bàn tay lại gần tay nắm cửa $< 0.4\text{m}$ nhưng chưa thực hiện thao tác kiểm tra nhiệt độ. | *"Cẩn thận! Đừng vội vặn tay nắm kim loại! Hãy dùng mu bàn tay kiểm tra nhiệt độ để tránh hiện tượng nổ bùng Oxy!"* | Phòng ngừa hiện tượng nổ bùng Backdraft chết người khi mở cửa đột ngột. |
| **4** | **Chăm chăm dập lửa mà quên báo động** | Thời gian cháy trôi qua $> 45\text{s}$ mà người học chưa bấm nút báo cháy đỏ trên tường. | *"Đám cháy đang phát triển! Hãy kích hoạt chuông báo động để cảnh báo cho các phòng lân cận trước khi quá muộn!"* | Quy chuẩn PCCC: Báo động toàn tòa nhà luôn được ưu tiên hàng đầu trước khi chữa cháy cục bộ. |
| **5** | **Hệ thống xả khí ngạt FM-200 kích hoạt đếm ngược** | Tủ điện cháy $> 90\text{s}$ chưa dập tắt ➔ Còi FM-200 hú đếm ngược 30 giây. | *"Hệ thống xả khí ngạt FM-200 đã đếm ngược! Ngừng dập lửa và chạy ngay ra hành lang trước khi hết oxy trong phòng!"* | Phản xạ Data Center: Khi hệ thống dập lửa tự động rút hết oxy, con người bắt buộc phải tháo chạy để bảo toàn tính mạng. |
