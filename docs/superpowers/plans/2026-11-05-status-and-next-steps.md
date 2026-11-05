# EscapeFire — Trạng thái dự án & Hướng đi tiếp theo (2026-11-05)

> Tài liệu bàn giao: đọc file này trước khi làm tiếp. Chi tiết từng milestone ở
> `2026-11-04-development-roadmap.md`, plan M0 ở `2026-11-04-m0-foundation.md`.

**Nhánh làm việc:** `feature/m0-foundation` (đã push lên `origin`, **chưa merge** vào `develop/server`).
**Engine:** Unity 6000.3.15f1 · URP 17.3 · XRI 3.4.1 · OpenXR → Meta Quest 2.

---

## 1. Thành tựu đã đạt được

### Gameplay (M0 → M1)
- Vòng chơi hoàn chỉnh: `00_MainMenu` (hiệu chuẩn H₀, chọn Guided/Exam) → `BasicScene` (phòng lab) → `02_AAR_Evaluation`.
- Kịch bản 6 phase: yên bình → chập điện → cháy server → khói/sơ tán → thắng / thua.
- Tương tác XR: cầm bình (Grip), xịt (Trigger), rút chốt, ngắt Aptomat, nút báo cháy, kiểm tra tay nắm cửa, cứu Hoa.
- 4 cách thua: điện giật (bình nước/bọt khi còn điện), ngạt khói, FM-200 xả khí, tử vong; thắng khi ra hành lang.
- NPC: Minh hoảng loạn chạy ra cửa; Hoa ngất sau 20 s, cứu được rồi tự đi ra.

### Scene (M6, dựng tự động bằng `EscapeFire > Setup All`)
- Bàn/màn hình/ghế/NPC dùng asset PolygonOffice thật (đã sửa 17 vật liệu mất texture), NPC đứng tư thế tự nhiên.
- Bình CO₂ cạnh cửa thoát (tường nam), bình nước "mồi bẫy" trên tường đông gần tủ điện; nút báo cháy cạnh cửa.
- Lớp khói hạ dần khi cháy, ánh sáng môi trường đủ để thấy lối thoát, wrist HUD (Oxy/Máu/Tư thế/AI Mentor).
- Cắm Quest 2 qua Link là chạy: `HeadsetPlayModeGuard` tự tắt simulator; không cắm kính thì XR Interaction Simulator chính thức điều khiển bằng bàn phím/chuột.

### AI thích ứng (M2 + M3 + M4)
- **CF tracker** (ma sát nhận thức) từ xoay đầu, chần chừ, lỗi an toàn — có hysteresis.
- **Micro-AI Governor** (trên kính, 0 ms): người học bình tĩnh → tăng độ khó; hoảng loạn → trấn an, không bao giờ thêm thử thách.
- **Edge server** (`Server/`, FastAPI + WebSocket, Ollama/Gemma): LLM chọn thử thách vĩ mô; kiểm duyệt 2 lớp (lớp 1 trên laptop: JSON + whitelist + trích dẫn luật PCCC + chống lặp + bậc độ khó; lớp 2 trên kính: ngân sách FPS, lối thoát, trạng thái hiện trường). Mất mạng → tự quay về AI cục bộ.
- **Socratic coach** (deterministic): nhắc khi cầm bình nước gần tủ điện, đứng thẳng trong khói, sờ tay nắm nóng, quên báo động; FM-200 đếm ngược.
- **Bộ đàm**: "Cháy! Có cháy!" → báo động (phân biệt cháy/chạy/chảy). Không có STT thì nhấn V trong Editor.

### Danh mục thử thách AI (độ khó tăng dần — thử thách thứ n chỉ được chọn ở bậc ≤ n, mỗi loại 1 lần/ván, tối đa 4, quan sát ≥ 15 s sau khi cháy)
| Bậc | Thử thách | Mục tiêu rèn luyện |
| :-: | :--- | :--- |
| 1 | `TRIGGER_CO2_PIN_JAM` — chốt kẹt, giật 2 lần | Xử lý thiết bị trục trặc |
| 1 | `TRIGGER_SMOKE_SURGE` — khói đặc tràn xuống 0.8 m, hạ nhanh gấp đôi | Phản xạ hạ thấp người |
| 2 | `TRIGGER_ALARM_CABLE_CUT` — chuông không kêu | Báo động bằng giọng nói |
| 2 | `TRIGGER_BLACKOUT` — mất điện chiếu sáng, còn đèn đỏ khẩn cấp + đèn EXIT xanh | Định hướng trong tối |
| 3 | `ESCALATE_SECONDARY_FIRE` — cháy lan sang bàn xa cửa | Đánh giá lại tình huống |
| 3 | `TRIGGER_EXTINGUISHER_LOW_PRESSURE` — CO₂ chỉ còn ~4 s phun | Quyết định dập tiếp hay sơ tán |
| 4 | `ESCALATE_FM200_COUNTDOWN` — còi FM-200 30 s | Bắt buộc sơ tán |

Validator chặn hợp lý: blackout khi đã ngắt Aptomat (đèn vốn đã tắt), khói dày khi khói đã thấp, yếu áp khi lửa đã tắt, cháy lan khi vượt 4 đám cháy hoặc chỉ còn chỗ chặn lối thoát.

### AAR (M5)
- Bảng điểm 100 đ (6 tiêu chí theo spec), radar 5 trục, sa bàn quỹ đạo (đỏ = đứng thẳng trong khói), nhận xét mẫu + nhận xét LLM (`POST /aar/debrief`).
- Báo cáo JSON lưu tại `%USERPROFILE%\AppData\LocalLow\DefaultCompany\EscapeFire\TelemetryReports\`.
- Phần gửi LMS (daotao.ai) **ngoài phạm vi** dự án — giữ nguyên code hàng đợi, không phát triển thêm.

---

## 2. Kiểm chứng (chạy được, có số liệu)

| Lớp | Kết quả gần nhất |
| :--- | :--- |
| Compile 5 assembly + Microsoft.Unity.Analyzers | sạch |
| EditMode (Unity thật) | 93/93 |
| PlayMode (Unity thật): 11 test vật lý/kịch bản + 9 ván chơi thử | 20/20 |
| Server pytest | 26/26 |
| Audit scene | 0 lỗi (2 cảnh báo: bảng trắng và tay gạt Aptomat vẫn là khối hộp) |

Lệnh: `bash Tools/verify.sh` (nhanh, không cần license) · `bash Tools/verify.sh --unity` (đóng Editor trước; cần đăng nhập Unity Hub).
Nhật ký chơi thử: `Logs/Playthroughs/*.md` · Ảnh scene: `Logs/Snapshots/*.png`.

---

## 3. Môi trường AI local

- Ollama 0.40 đã cài (`%LOCALAPPDATA%\Programs\Ollama`), biến môi trường người dùng `OLLAMA_MODELS = E:\HUST_2\20261\ProjectIII\EscapeFire_AI\models` (ngoài repo).
- Mô hình: `gemma3:4b` (3.1 GB) — **đã tải, CHƯA chạy suy luận/test**. Chỉ chạy test LLM khi người dùng yêu cầu.
- Chạy server với LLM thật: `cd Server && uv run python -m escapefire_edge.app` (mặc định `gemma3:4b`, cổng 8765). Nếu app Ollama không tự bật server: `ollama serve`.
- Phần cứng: RTX 4050 6 GB, i5-13500H, 16 GB RAM.

---

## 4. Hướng đi sắp tới (theo thứ tự ưu tiên)

1. **Test với LLM thật (khi người dùng bảo)**: chạy `Run08` với Gemma thay `--fake-llm`; đo độ trễ (mục tiêu < 6 s/lượt), tỉ lệ JSON hợp lệ, tỉ lệ bị lớp 1 chặn (sai luật/vượt bậc/lặp), chất lượng nhận xét AAR. Nếu Gemma 4B yếu: thử `qwen2.5:7b` hoặc few-shot trong prompt.
2. **Chơi thật trên Quest 2 qua Link**: đổi runtime OpenXR sang Meta Quest Link (máy đang là SteamVR), chơi 3 ván Guided/Exam, ghi lại cảm giác cầm bình, chiều cao khom, độ đọc của wrist HUD.
3. **Âm thanh**: giọng Việt cho AI Mentor (thu âm 5 câu Socratic hoặc tải giọng TTS tiếng Việt cho Overtone); âm thanh bình xịt, còi FM-200.
4. **Hoàn thiện hình ảnh**: thay bảng trắng & tay gạt Aptomat bằng asset; hiệu ứng khói thật (Real Fire & Smoke) quanh tủ server; animation đơn giản cho Minh khi chạy.
5. **Tối ưu Quest 2**: build APK (`EscapeFire > M6 > Build Quest 2 APK`), đo FPS bằng `FrameTimeMonitor` (mục tiêu ≥ 72 FPS, < 5 % frame vượt 13.8 ms).
6. **Đánh giá cho luận văn**: pilot 5–10 sinh viên (Guided vs Exam vs Exam+LLM), so sánh điểm, CF trung bình, số thử thách xử lý được; dữ liệu từ `TelemetryReports` + `Server/logs/director_*.jsonl`.
7. **Merge** `feature/m0-foundation` → `develop/server` sau khi người dùng chơi thử xác nhận.

## 5. Lưu ý kỹ thuật cho người làm tiếp
- Thứ tự khởi tạo: `LabProgressiveScenarioController` tìm `ServerFire` trong `Awake` (khói/FM-200 đọc trong `Start`).
- Mọi thứ AI thêm vào phải đi qua `MicroAIGovernor.TryExecute` → `DirectiveValidator`; thêm thử thách mới = enum + code + `DirectiveTiers` + validator + `Apply` + server `ALLOWED_DIRECTIVES`/`DIRECTIVE_TIERS`/mô tả + test.
- Không dùng `??`/`?.` với `UnityEngine.Object` (analyzer UNT0007/0008 sẽ báo).
- Test không được ghi vào thư mục dữ liệu thật: dùng `LmsSync.OutboxFolder` / `TelemetryLogger.FolderOverride`.
