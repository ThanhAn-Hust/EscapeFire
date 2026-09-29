# ĐẶC TẢ KỸ THUẬT BỐI CẢNH & KHÔNG GIAN VẬT LÝ PHÒNG LAB
**Dự án:** VR Emergency Response & Safety Simulator (EscapeFire)  
**Tiêu chuẩn thiết kế:** Tỷ lệ thực 1 đơn vị Unity = 1 mét (1 Unit = 1 Meter).  
**Hệ thống Prefab tham chiếu:** `POLYGON Office (Synty)` & `Technical Laboratory Designer`.

---

## 1. TỔNG THỂ KÍCH THƯỚC KHÔNG GIAN (ROOM BOUNDS)
* **Kích thước bao:** Dài $10.0\text{m}$ (Trục X) $\times$ Rộng $8.0\text{m}$ (Trục Z) $\times$ Cao $3.5\text{m}$ (Trục Y).
* **Diện tích khả dụng:** $80\text{m}^2$ sàn phòng học (mô phỏng chuẩn phòng lab đại học Bách Khoa).
* **Tâm phòng (Center Pivot):** $(X = 0, Y = 0, Z = 0)$.
* **Phạm vi tọa độ:**
  * Trục $X$: Từ $-5.0\text{m}$ (Tường Tây) đến $+5.0\text{m}$ (Tường Đông).
  * Trục $Z$: Từ $-4.0\text{m}$ (Tường Nam - Cửa chính) đến $+4.0\text{m}$ (Tường Bắc - Bảng trắng).
  * Trục $Y$: Từ $0.0\text{m}$ (Mặt sàn gạch) đến $3.5\text{m}$ (Hệ trần thạch cao).

---

## 2. BỐ TRÍ 4 VÁCH TƯỜNG & VẬT LIỆU KIẾN TRÚC

### 2.1. Vách Tường Nam (South Wall, $Z = -4.0\text{m}$) - KHU VỰC THOÁT HIỂM CHÍNH
* **Cửa chính ra vào (`Door_MainExit`):**
  * Tọa độ: $(X = 0.0\text{m}, Y = 0.0\text{m}, Z = -4.0\text{m})$. Kích thước: $1.2\text{m} \times 2.2\text{m}$.
  * Prefab: `SM_Bld_Door_01` (Cửa gỗ có ô kính nhỏ chịu nhiệt ở độ cao 1.5m).
  * Tay nắm cửa: Dạng kim loại tròn (`DoorHandle`), gắn script `DoorHeatChecker.cs` (nhiệt độ tăng khi có cháy bên ngoài).
* **Nút bấm Báo cháy khẩn cấp (`FireAlarm_Button`):**
  * Tọa độ: $(X = +0.8\text{m}, Y = 1.4\text{m}, Z = -3.95\text{m})$ (Bên phải cửa chính, vừa tầm tay với).
  * Prefab: `SM_Prop_FireAlarm_Switch_01` (Nút ấn màu đỏ tròn, có nắp kính nhựa trong bảo vệ).
  * Gắn script `FireAlarmButton.cs`.
* **Trạm cứu hỏa tường - Cụm bình CO2 (`Station_CO2_Extinguisher`):**
  * Tọa độ: $(X = -1.2\text{m}, Y = 1.1\text{m}, Z = -3.95\text{m})$ (Bên trái cửa chính).
  * Giá treo kim loại đỏ trên tường chứa **Bình Chữa Cháy CO2 (Khí carbon dioxide)**.
  * Nhận dạng: Thân đỏ, nhãn đen viền bạc "CO2", loa phun hình nón to màu đen chịu lạnh.
  * Tương tác: XR Grab Interactable, rút chốt an toàn trước khi bóp cò.
* **Biển thoát hiểm dạ quang (`Exit_Sign_South`):**
  * Tọa độ: $(X = 0.0\text{m}, Y = 2.4\text{m}, Z = -3.95\text{m})$ (Ngay trên đỉnh khung cửa).
  * Biển chữ nhật xanh lá có đèn LED chạy ắc quy độc lập (sáng liên tục kể cả khi sụt điện).

### 2.2. Vách Tường Bắc (North Wall, $Z = +4.0\text{m}$) - KHU VỰC GIẢNG DẠY & CỤM SERVER CHÍNH
* **Bảng trắng giảng viên (`Whiteboard_Main`):**
  * Tọa độ: $(X = 0.0\text{m}, Y = 1.2\text{m}, Z = +3.95\text{m})$.
  * Kích thước: $3.0\text{m} \times 1.2\text{m}$.
  * Gắn nội dung bài học: "Bài thực hành 03: Thiết lập mạng máy chủ & An toàn hạ tầng CNTT".
* **Cụm Tủ Server Chính - ĐIỂM BÙNG PHÁT HỎA HOẠN (`Server_Rack_Cluster`):**
  * Tọa độ: $(X = +3.5\text{m}, Y = 0.0\text{m}, Z = +3.2\text{m})$ (Góc Đông - Bắc).
  * Prefab: `Server_full` (Gồm 2 tủ rack 42U ghép liền, cửa kính trước nhìn rõ các dàn switch và router).
  * Vị trí nổ tụ điện & khởi phát ngọn lửa: Rack thứ hai, độ cao $Y = 1.4\text{m}$.
  * Gắn script: `FireHazard.cs` (Class C - Cháy điện).
* **Bàn Giảng Viên (`Lectern_Instructor`):**
  * Tọa độ: $(X = -3.2\text{m}, Y = 0.0\text{m}, Z = +3.0\text{m})$ (Góc Tây - Bắc).
  * Trang bị: 1 bàn gỗ giáo viên, 1 màn hình laptop bật sáng, micro để bàn, xấp giáo trình.

### 2.3. Vách Tường Đông (East Wall, $X = +5.0\text{m}$) - HẠ TẦNG ĐIỆN & CẦU DAO TỔNG
* **Tủ Cầu Dao / Aptomat Tổng (`Main_Circuit_Breaker`):**
  * Tọa độ: $(X = +4.95\text{m}, Y = 1.5\text{m}, Z = +1.5\text{m})$.
  * Prefab: `panel_for_server` (Hộp kim loại xám công nghiệp có nắp mica trong suốt).
  * Cần gạt Aptomat: Cần gạt màu đỏ-đen. Trạng thái ban đầu: `ON` (Điện lưới cấp cho toàn bộ phòng và Server).
  * Gắn script: `CircuitBreakerSwitch.cs`. Người chơi dùng tay VR kéo cần gạt xuống vị trí `OFF` để cắt điện.
* **Tủ tài liệu kim loại (`Storage_Cabinet`):**
  * Tọa độ: $(X = +4.9\text{m}, Y = 0.0\text{m}, Z = -1.5\text{m})$. Chứa dây mạng, linh kiện máy tính thừa.

### 2.4. Vách Tường Tây (West Wall, $X = -5.0\text{m}$) - CỬA SỔ & BÌNH BỌT NƯỚC (BẪY LỰA CHỌN)
* **Khung Cửa Sổ Kính Thông Thoát Khí (`Lab_Windows`):**
  * 2 khung cửa sổ kích thước $2.5\text{m} \times 1.5\text{m}$ tại $Z = -1.5\text{m}$ và $Z = +1.5\text{m}$, độ cao bệ cửa $Y = 1.1\text{m}$.
  * Kính mờ cường lực, có thể hé mở lấy không khí khi khói lan dày.
* **Trạm cứu hỏa phụ - Bình Nước / Bọt Foam (`Station_Water_Extinguisher` - DECOY):**
  * Tọa độ: $(X = -4.95\text{m}, Y = 1.1\text{m}, Z = 0.0\text{m})$ (Treo giữa 2 cửa sổ).
  * Bình chữa cháy dạng Nước/Bọt (MFZ4). Thân màu đỏ, có vạch xanh/chữ xanh lá "WATER / FOAM", vòi xịt nhỏ dài.
  * **Cảnh báo sư phạm:** Nghiêm cấm dùng bình này cho cụm server điện đang live. Nếu dùng $\to$ Kích hoạt nổ chập điện (Game Over).
* **Bàn kỹ thuật phụ & Chậu/Khăn lau (`Safety_Wipe_Table`):**
  * Tọa độ: $(X = -4.2\text{m}, Y = 0.0\text{m}, Z = -2.5\text{m})$.
  * Đặt 1 chậu nước nhỏ và cuộn khăn vải cotton. Người chơi có thể nhặt khăn nhúng nước che mũi miệng để giảm tốc độ ngạt khói.

---

## 3. KHU VỰC TRUNG TÂM: 2 DÃY MÁY TÍNH HỌC SINH (WORKSTATIONS)

Xếp song song tạo thành 3 lối đi thông thoáng: Lối đi giữa (rộng 1.8m), 2 lối đi áp vách tường (rộng 1.2m).

```
[Bảng Trắng]          [Bàn GV]                  [Tủ Server - FIRE]
(Z = +4m)             (-3.2, +3.0)              (+3.5, +3.2)
──────────────────────────────────────────────────────────────────
                      ┌───┐            ┌───┐
                      │ 3 │            │ 6 │  (Z = +1.5m)
                      └───┘            └───┘
                      ┌───┐   LỐI ĐI   ┌───┐
                      │ 2 │   TRUNG    │ 5 │  (Z = 0.0m)
                      └───┘    TÂM     └───┘
                      ┌───┐   (1.8m)   ┌───┐
                      │ 1 │            │ 4 │  (Z = -1.5m)
                      └───┘            └───┘
                   (Dãy Tây)        (Dãy Đông)
                   (X = -2.0)       (X = +2.0)
──────────────────────────────────────────────────────────────────
(Z = -4m)         [Bình CO2]  [CỬA CHÍNH]  [Báo Cháy]
```

### 3.1. Dãy Bàn Máy Tính Phía Tây (West Cluster, $X = -2.0\text{m}$)
* Gồm 3 mô-đun bàn máy tính ghép thẳng hàng theo trục Z:
  * **Bàn 1:** Tọa độ $(X = -2.0\text{m}, Y = 0.0\text{m}, Z = -1.5\text{m})$.
  * **Bàn 2:** Tọa độ $(X = -2.0\text{m}, Y = 0.0\text{m}, Z = 0.0\text{m})$ - **Vị trí NPC Hoa ngồi học lúc đầu**.
  * **Bàn 3:** Tọa độ $(X = -2.0\text{m}, Y = 0.0\text{m}, Z = +1.5\text{m})$.
* Trên mỗi bàn trang bị: 1 bàn gỗ văn phòng $1.4\text{m} \times 0.7\text{m} \times 0.75\text{m}$, 1 màn hình máy tính 24-inch, thùng CPU dưới gầm, bàn phím, chuột và 1 ghế xoay văn phòng có bánh xe.

### 3.2. Dãy Bàn Máy Tính Phía Đông (East Cluster, $X = +2.0\text{m}$)
* Gồm 3 mô-đun bàn máy tính tương tự:
  * **Bàn 4:** Tọa độ $(X = +2.0\text{m}, Y = 0.0\text{m}, Z = -1.5\text{m})$.
  * **Bàn 5:** Tọa độ $(X = +2.0\text{m}, Y = 0.0\text{m}, Z = 0.0\text{m})$ - **Vị trí Người chơi (VR Player) đứng xuất phát**.
  * **Bàn 6:** Tọa độ $(X = +2.0\text{m}, Y = 0.0\text{m}, Z = +1.5\text{m})$ - **Vị trí NPC Minh ngồi gõ code**.

---

## 4. HỆ THỐNG TRẦN, ÁNH SÁNG & KHÓI THỂ TÍCH (LIGHTING & SMOKE)

* **Trần nhà (`Ceiling_System`):** Mặt phẳng trần tại $Y = 3.5\text{m}$.
* **Cụm Đèn chiếu sáng thông thường (`Normal_Lights`):**
  * 4 cụm đèn panel LED âm trần tại $(X = \pm 2.0\text{m}, Z = \pm 1.5\text{m}, Y = 3.4\text{m})$. Ánh sáng trắng 5500K, độ rọi 400 Lux.
* **Cụm Đèn Khẩn Cấp Báo Động (`Emergency_Red_Lights`):**
  * 2 đèn quay cảnh báo màu đỏ gắn gần cửa chính và trên nóc tủ Server.
  * Kích hoạt xoay vòng và nhấp nháy nhịp $2\text{Hz}$ khi bước vào Phase sự cố.
* **Bộ Điều Khiển Khói Thể Tích URP (`SmokeDensityController`):**
  * Vùng khói phủ: Toàn bộ thể tích phòng $(10\text{m} \times 8\text{m})$.
  * Mặt trần khói: $Y = 3.5\text{m}$.
  * Đáy tầng khói (Smoke Base Height):
    * Ban đầu: $Y = 3.5\text{m}$ (Không có khói).
    * Giây thứ 30: Hạ xuống $Y = 2.5\text{m}$.
    * Giây thứ 60: Hạ xuống $Y = 1.8\text{m}$.
    * Giây thứ 90: Hạ xuống $Y = 1.2\text{m}$ (Bắt buộc người chơi phải cúi đầu khom lưng).
