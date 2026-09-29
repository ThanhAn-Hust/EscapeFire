# THIẾT KẾ CÁC MÀN CHƠI & LUỒNG CHUYỂN GIAO GIAO DIỆN (SCENE FLOW & SCREENS)
**Dự án:** VR Emergency Response & Safety Simulator (EscapeFire)  
**Nền tảng mục tiêu:** Meta Quest Standalone (Android/OpenXR) & PCVR Link.  
**Engine:** Unity 6 (6000.3) | URP 17.3 | XR Interaction Toolkit 3.4.1.

---

## 1. SƠ ĐỒ CHUYỂN MÀN TỔNG THỂ (SCENE ARCHITECTURE)

Hệ thống được tổ chức thành 3 Scene chính kết nối tuần tự theo luồng sư phạm khép kín:

```
┌─────────────────────────────────┐
│     SCENE 0: 00_MainMenu        │
│  - Phòng chờ thực tế ảo (Lobby) │
│  - Căn chỉnh chiều cao kính VR  │
│  - Chọn bài học & Chế độ chơi   │
└────────────────┬────────────────┘
                 │ Bấm "BẮT ĐẦU DIỄN TẬP"
                 ▼
┌─────────────────────────────────┐
│  SCENE 1: 01_ComputerLab_Sim    │ ◄─── (Cốt lõi Gameplay 6DOF)
│  - Không gian phòng Lab 80m²    │
│  - FSM 5 Phases & NPC Tương tác │
│  - Thu thập Telemetry thời gian thực
└────────────────┬────────────────┘
                 │ Thoát hiểm an toàn / Game Over
                 ▼
┌─────────────────────────────────┐
│    SCENE 2: 02_AAR_Evaluation   │
│  - Phòng Phân tích Sau Hành động│
│  - Dựng lại Lộ trình (Heatmap)  │
│  - Bảng Điểm & AI Mentor Nhận xét
└─────────────────────────────────┘
```

---

## 2. CHI TIẾT TỪNG MÀN CHƠI (SCENE SPECIFICATIONS)

### 2.1. SCENE 0: `00_MainMenu` (Phòng Chờ Định Chuẩn & Hướng Dẫn)
* **Môi trường:**
  * Một phòng họp hiện đại nhỏ $5\text{m} \times 5\text{m}$ yên tĩnh, ánh sáng dịu nhẹ (3500K).
  * Trước mặt người chơi là một màn hình cong 3D kích thước lớn lơ lửng trong không gian (World Space Canvas).
* **Các thành phần giao diện (UI Modules):**
  1. **Calibration Station (Hiệu chuẩn chiều cao):**
     * Yêu cầu người chơi đứng thẳng tự nhiên và bấm nút "XÁC NHẬN CHIỀU CAO". Hệ thống lưu giá trị $H_0$ làm mốc chuẩn để tính toán trạng thái khom lưng ($< 0.7 \times H_0$) trong lúc khói độc bùng phát.
  2. **Tùy chọn Chế độ diễn tập (Training Modes):**
     * **Chế độ 1: Huấn luyện có Hướng dẫn (Guided Mode - Dành cho người mới):** Có highlight viền vàng các vật thể cần tương tác, mũi tên chỉ đường thoát hiểm dưới sàn.
     * **Chế độ 2: Đánh giá Sát hạch Thực chiến (Exam Mode - Chuẩn bảo vệ đồ án):** Không có UI gợi ý, âm thanh và khói đạt mức tối đa, kích hoạt toàn bộ cơ chế thích ứng động ZPD và NPC hoảng loạn.
  3. **Bảng Tóm tắt Thao tác 6DOF:**
     * Trực quan hóa nút bấm trên Meta Quest Touch Controller: Grip (Nắm vật thể), Trigger (Bóp cò bình chữa cháy), Thumbstick (Di chuyển/Xoay góc nhìn).
* **Điều kiện chuyển Scene:**
  * Người chơi dùng tia Raycast từ tay cầm trỏ vào nút **"BẮT ĐẦU BÀI TẬP PHÒNG LAB"** $\to$ Fade màn hình sang đen $\to$ Load Async Scene `01_ComputerLab_Sim`.

---

### 2.2. SCENE 1: `01_ComputerLab_Sim` (Phòng Lab Diễn Tập Khẩn Cấp)
* **Môi trường:** Toàn bộ không gian phòng Lab máy tính $10\text{m} \times 8\text{m} \times 3.5\text{m}$ theo đúng đặc tả `SCENE_ENVIRONMENT_SPEC.md`.
* **Giao diện Trong Game (In-Game HUD - Gắn trên cổ tay trái `VRWristHUD`):**
  * Không dùng UI cố định dính vào mặt kính (Head-locked canvas) để tránh phá vỡ cảm giác nhập vai (Break-in-Presence) và không gây chóng mặt.
  * Toàn bộ thông tin hiển thị trên chiếc Đồng hồ đeo tay thông minh (Smart Wristwatch):
    * **Thanh Oxy / Độ trong lành khí thở (0 - 100%):** Màu xanh lá $\to$ Vàng $\to$ Đỏ chớp nháy khi hít phải khói độc.
    * **Trạng thái tư thế (Crouch Indicator):** Icon người đứng thẳng (Màu đỏ cảnh báo) hoặc người khom lưng (Màu xanh an toàn).
    * **Bộ đàm cứu nạn (Walkie-Talkie Button):** Giữ nút Grip tay trái gần tai để kích hoạt mic nói chuyện với AI Mentor.
* **Các màn hình Popup ngữ cảnh tối thiểu (Contextual World-Space Popups):**
  * Chỉ xuất hiện khi người chơi vi phạm lỗi an toàn nghiêm trọng (Fatal Safety Violation):
    * *"CẢNH BÁO NGUY HIỂM TÍNH MẠNG: Dòng điện 3 pha chưa ngắt!"* (Nổi lên phía trên tủ điện 3 giây rồi biến mất).
* **Điều kiện kết thúc màn:**
  * **Thành công (Evacuated):** Người chơi mở được cửa chính hoặc cửa thoát phụ và bước qua ngưỡng cửa ra hành lang $\to$ Ghi nhận kết quả `PASSED` $\to$ Chuyển sang `02_AAR_Evaluation`.
  * **Thất bại (Failed):** Thanh Oxy tụt về $0\%$ (Ngạt khói) HOẶC bị điện giật tử vong $\to$ Màn hình tối dần trong tiếng thở dốc $\to$ Chuyển sang `02_AAR_Evaluation` với cờ `FAILED`.

---

### 2.3. SCENE 2: `02_AAR_Evaluation` (Phòng Tổng Kết & Phân Tích Dữ Liệu Sau Hành Động)
* **Môi trường:**
  * Phòng lab ảo dạng công nghệ cao (Hologram Command Room).
  * Ở giữa phòng là một sa bàn 3D thu nhỏ (Holographic Mini-map) của phòng lab vừa chơi.
* **Các bảng dữ liệu phân tích chi tiết (AAR Dashboard):**
  1. **Bản đồ Lộ trình & Nhiệt lượng (3D Heatmap Path):**
     * Một đường line màu biểu diễn chính xác quỹ đạo di chuyển của người học từ lúc bắt đầu đến khi kết thúc.
     * Đoạn đường màu đỏ: Những vị trí người học đứng thẳng hít khói quá lâu.
     * Điểm đánh dấu tròn: Nơi tương tác với nút báo cháy, tủ aptomat, bình cứu hỏa.
  2. **Bảng Điểm Tiêu chuẩn PCCC (Compliance Scorecard):**
     * Kích hoạt báo động sớm: `10 / 10 điểm`.
     * Ngắt cầu dao trước khi dập: `20 / 20 điểm`.
     * Chọn đúng chủng loại bình (CO2 cho điện): `20 / 20 điểm`.
     * Kỹ thuật rút chốt & bóp cò gốc lửa: `15 / 15 điểm`.
     * Duy trì tư thế khom lưng dưới khói ($< 1.2\text{m}$): `15 / 20 điểm` (Bị trừ do đứng thẳng 8 giây lúc tìm đường).
     * Hỗ trợ đồng đội (Cứu nạn NPC): `15 / 15 điểm`.
     * **TỔNG ĐIỂM:** `90 / 100` (Xếp loại: **XUẤT SẮC - ĐẠT CHUẨN AN TOÀN**).
  3. **Khuyến nghị Cá nhân hóa từ AI Mentor (AI Debriefing Text & Audio):**
     * *"Bạn đã có phản xạ ngắt cầu dao rất nhanh và chuẩn xác. Tuy nhiên, khi khói đã hạ xuống dưới 1.5m, bạn có xu hướng ngẩng cao đầu để tìm đường trong 8 giây ở khu vực Bàn 4, điều này có thể dẫn đến bỏng đường hô hấp trong môi trường thực tế. Hãy rèn luyện thói quen men theo chân tường."*
  4. **Các nút điều hướng:**
     * Nút **"LÀM LẠI BÀI TẬP (RETRY)"**: Tái khởi động lại Scene 1 với kịch bản được xáo trộn ngẫu nhiên.
     * Nút **"XUẤT BÁO CÁO PDF / JSON"**: Gửi dữ liệu telemetry lên web server để giảng viên theo dõi.
     * Nút **"VỀ MENU CHÍNH"**: Quay về Scene 0.
