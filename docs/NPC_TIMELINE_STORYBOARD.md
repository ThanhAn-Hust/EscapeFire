# KỊCH BẢN THỜI GIAN THỰC & HÀNH VI NPC (TIMELINE STORYBOARD)
**Dự án:** VR Emergency Response & Safety Simulator (EscapeFire)  
**Thời lượng một lượt diễn tập:** $3\text{ phút } 30\text{ giây}$ ($210\text{s}$).  
**Cơ chế vận hành:** FSM (Finite State Machine) kết hợp Event Triggers và Adaptive ZPD Intervention.

---

## 1. HỆ THỐNG NHÂN VẬT & TÂM LÝ HỌC HÀNH VI

### 👤 1. Người chơi (VR Student Trainee)
* **Xuất phát điểm:** $(X = +2.0\text{m}, Y = 0.0\text{m}, Z = 0.0\text{m})$ (Bàn 5, trước màn hình PC).
* **Trang bị:** Kính VR Meta Quest 6DOF, 2 tay cầm Controller, Đồng hồ sinh học thông minh ở cổ tay trái (`VRWristHUD.cs`).
* **Nhiệm vụ:** Bảo toàn tính mạng, tuân thủ quy chuẩn an toàn PCCC, đưa ra quyết định cứu trợ hợp lý.

### 🏃 2. NPC Minh - Sinh viên hoảng loạn (The Panicked Peer)
* **Vị trí xuất phát:** $(X = +2.0\text{m}, Y = 0.0\text{m}, Z = +1.5\text{m})$ (Bàn 6, ngay phía trước người chơi).
* **Đặc tính tâm lý:** Mất bình tĩnh cực độ, phản ứng theo bản năng tháo chạy, gây nhiễu nhận thức cho người chơi bằng lời nói và hành động cản trở.

### 🧍‍♀️ 3. NPC Hoa - Nạn nhân ngạt khói (The Incapacitated Peer)
* **Vị trí xuất phát:** $(X = -2.0\text{m}, Y = 0.0\text{m}, Z = 0.0\text{m})$ (Bàn 2, dãy đối diện bên trái).
* **Đặc tính tâm lý:** Thiếu kỹ năng phản xạ, cố chấp tìm đồ đạc cá nhân trong đám cháy dẫn đến ngất xỉu do hít khói độc (Tình huống tiến thoái lưỡng nan về đạo đức và quy chuẩn an toàn).

### 🤖 4. AI Socratic Mentor - Trợ lý Ảo Huấn Luyện (Drone / Radio Voice)
* **Kênh giao tiếp:** Loa bộ đàm Walkie-Talkie trên tay áo / HUD đeo tay.
* **Nguyên tắc can thiệp:** Chỉ gợi mở phương pháp tư duy (Socratic Questioning), không cầm tay chỉ việc làm hộ.

---

## 2. TIMELINE DIỄN BIẾN TỪNG GIÂY (CHRONOLOGICAL STORYBOARD)

```
00:00        00:20        00:45        01:15                02:15                03:00      03:30
  │            │            │            │                    │                    │          │
  ▼            ▼            ▼            ▼                    ▼                    ▼          ▼
[Phase 0]   [Phase 1]    [Phase 2]    [Phase 3]            [Phase 4]            [Phase 5]  [End]
Làm quen     Chập điện    Báo động     Dập lửa / Rẽ nhánh   Thoát hiểm khom lưng  AAR Báo cáo
```

---

### ⏱️ PHASE 0: LÀM QUEN & TƯƠNG TÁC BAN ĐẦU (00:00 - 00:20)
* **Bối cảnh:** Phòng lab sáng sủa, tiếng máy tính chạy êm, tiếng gõ bàn phím lách cách của NPC Minh.
* **Diễn biến:**
  * `[00:00]` Màn hình fade-in. Người chơi xuất hiện tại Bàn 5.
  * `[00:05]` Voice AI Mentor: *"Chào bạn. Hãy hoàn tất cắm đầu cáp mạng LAN vào cổng switch phía trước để bắt đầu bài thực hành."*
  * `[00:10]` Người chơi dùng tay cầm Controller nhặt đầu cáp LAN cắm vào khe cắm để làm quen cơ chế Grab 6DOF.
  * `[00:18]` NPC Minh quay sang nói đùa: *"Ê, hôm nay chạy code xong sớm đi làm cốc trà đá cổng trường nhé!"*

---

### ⏱️ PHASE 1: KHỞI PHÁT SỰ CỐ & CHẬP ĐIỆN HỒ QUANG (00:20 - 00:45)
* **Trigger:** Khi đồng hồ điểm `00:20` (hoặc ngay khi người chơi cắm xong cáp mạng).
* **Diễn biến âm thanh & hình ảnh:**
  * `[00:20]` Tủ Server tại $(X = +3.5\text{m}, Z = +3.2\text{m})$ phát tiếng **"TÁCH... BÙM!"** lớn. 
  * Cụm particle tia lửa điện xanh-vàng phóng mạnh ra xung quanh.
  * Khói trắng bắt đầu bốc lên từ nóc tủ server, chuyển dần sang khói đen đặc.
  * Đèn panel trần sụt áp, nhấp nháy 3 lần rồi chuyển sang chế độ đèn khẩn cấp đỏ mờ.
* **Hành vi NPC Minh:**
  * `[00:22]` Minh bật dậy làm đổ mạnh chiếc ghế xoay ra sàn gỗ (`Clatter SFX`). Ghế ngã chắn ngang lối đi giữa dãy bàn.
  * `[00:25]` Minh ôm đầu hét lớn: *"Á! Cháy rồi! Nổ server rồi! Chạy thôi anh em ơi!"*
  * `[00:27]` Minh hoảng loạn chạy thẳng một mạch về phía Cửa Nam $(X = 0, Z = -4.0\text{m})$.
  * `[00:32]` Đến cửa, Minh giật mạnh tay nắm cửa nhưng cửa bị kẹt chốt chưa mở được. Minh đứng đập cửa kêu gào: *"Mở cửa ra! Ai khóa cửa ngoài rồi?!"*
* **Hành vi NPC Hoa:**
  * `[00:23]` Hoa giật mình đứng lên, nhưng thay vì thoát hiểm, Hoa cúi xuống gầm bàn lục tìm chiếc túi xách và laptop cá nhân.
  * `[00:35]` Khói đen bắt đầu lan sang phía Tây. Hoa ho sặc sụa: *"Khụ... khụ... ví tiền của tớ đâu rồi..."*

---

### ⏱️ PHASE 2: PHẢN ỨNG QUY TRÌNH - BÁO ĐỘNG & NGẮT ĐIỆN (00:45 - 01:15)
* **Môi trường:**
  * Khói hạ xuống $Y = 2.2\text{m}$. Còi báo cháy trường học hú vang nhịp liên tục (`Wee-Woo`).
  * Ngọn lửa trên tủ server lan sang xấp giấy A4 trên bàn kỹ thuật bên cạnh (Bán kính cháy đạt $0.8\text{m}$).
* **Nhiệm vụ người học:**
  * Nhấn Nút Báo Cháy đỏ trên tường cạnh cửa chính $(+10\text{ điểm})$.
  * Di chuyển đến vách tường Đông, mở nắp tủ điện, gạt cần Aptomat từ `ON` xuống `OFF` $(+20\text{ điểm})$.
* **Các kịch bản rẽ nhánh tại Phase 2:**
  * **Trường hợp A (Làm đúng):** Người chơi gạt Aptomat $\to$ Tia lửa điện tắt ngấm, tiếng xẹt điện dừng lại, quạt tản nhiệt server tắt. Voice AI Mentor: *"Nguồn điện đã được ngắt an toàn. Hãy xử lý ngọn lửa bằng bình chữa cháy phù hợp."*
  * **Trường hợp B (Bỏ qua ngắt điện, lao vào lấy bình nước xịt):**
    * Khi người chơi bóp bình bọt/nước cách tủ server $< 2.5\text{m}$ khi điện vẫn `ON` $\to$ Hồ quang điện nổ tung làm đen màn hình. Màn hình báo: **"GAME OVER: TỬ VONG DO GIẬT ĐIỆN KHI DÙNG CHẤT CHỮA CHÁY CÓ TÍNH DẪN ĐIỆN VÀO NGUỒN CAO ÁP"**.
  * **Trường hợp C (Lấy bình CO2 dập khi chưa ngắt điện):**
    * Lửa tắt trong 3 giây nhưng sau đó bùng cháy trở lại ngay lập tức. Voice AI Mentor: *"Cảnh báo: Nhiệt lượng từ dòng điện vẫn tiếp tục sinh nhiệt. Cô lập nguồn điện ngay!"*

---

### ⏱️ PHASE 3: DẬP LỬA & PHÂN NHÁNH THÍCH ỨNG ĐỘNG ZPD (01:15 - 02:15)

Tại thời điểm `01:15`, hệ thống tính toán chỉ số hiệu năng $P_t$:

#### 🌿 Nhánh 3A: Chuẩn quy trình ($0.45 \le P_t \le 0.85$)
* Người chơi nhấc **Bình CO2** ở vách tường Nam.
* Thao tác 6DOF bắt buộc:
  1. Tay trái nắm cổ bình, tay phải giật Chốt an toàn (Pull Ring Pin).
  2. Hướng loa phun to vào **gốc ngọn lửa** (khoảng cách an toàn $1.5\text{m} - 2.0\text{m}$).
  3. Bóp cò giữ liên tục $5\text{ giây}$. Dòng khí tuyết CO2 trắng xoá xịt ra làm ngọn lửa lụi tàn hoàn toàn.
* `[01:45]` Lửa tắt. Voice AI Mentor: *"Đám cháy đã được kiểm soát. Khói độc vẫn đang tích tụ ở trần nhà, hãy di chuyển khom lưng ra cửa thoát hiểm!"*

#### 🔥 Nhánh 3B: Thách thức nâng cao ($P_t > 0.85$ - Người chơi xử lý quá xuất sắc)
* Kích hoạt khi người chơi hoàn tất dập lửa dưới 25 giây mà không mắc lỗi nào.
* `[01:30]` Một tiếng rắc lớn vang lên từ trần nhà: Tấm thạch cao trần trước cửa chính sụp xuống, kéo theo đường ống ghen dây điện bốc cháy nhỏ, chắn mất lối thoát chính $(X = 0, Z = -4.0\text{m})$.
* Minh hoảng loạn tháo chạy vào góc phòng.
* Người chơi bị ép phải đổi chiến thuật: Quay người $180^\circ$, cúi thấp tìm lối thoát hiểm ngách qua cửa sổ thông gió phía Tây hoặc cửa kỹ thuật phía sau bảng trắng.

#### 🆘 Nhánh 3C: Hỗ trợ giảm tải nhận thức ($P_t < 0.45$ - Người chơi hoảng loạn, đứng im)
* Kích hoạt khi người chơi chạy lòng vòng không biết làm gì $> 30\text{ giây}$ hoặc thanh Oxy tụt quá $30\%$.
* Khói được giữ cố định ở độ cao $1.8\text{m}$ (không hạ sâu hơn để tránh tụt FPS và giảm chóng mặt).
* Tiếng rè radio Walkie-Talkie vang lên: *"Tập trung nghe chỉ dẫn: Nhìn sang vách tường bên phải cửa ra vào, có bình chữa cháy màu đỏ gắn loa đen hình nón. Lấy bình đó và rút chốt an toàn ngay!"*

---

### ⏱️ PHASE 4: TÌNH HUỐNG CỨU NẠN & THOÁT HIỂM KHOM LƯNG (02:15 - 03:00)
* **Khói độc hạ xuống $Y = 1.2\text{m}$:**
  * Bắt buộc người chơi phải hạ headset $< 1.2\text{m}$ (ngồi xổm hoặc quỳ thực tế).
  * Nếu đứng thẳng: Màn hình co hẹp viền đen (Vignette), tiếng tim đập dồn dập (`Heartbeat SFX`), chỉ số Oxy tụt $-10\%/\text{giây}$.
* **Tình huống NPC Hoa:**
  * `[02:20]` Hoa ngã gục bất tỉnh ở góc Bàn 2 $(X = -2.5\text{m}, Z = 0.0\text{m})$.
  * Quyết định của người chơi:
    * **Lựa chọn 1 (Thoát hiểm đơn độc):** Người chơi khom lưng lần ra cửa. Được chấm điểm an toàn cá nhân ($+50\text{ điểm}$).
    * **Lựa chọn 2 (Cứu nạn có trách nhiệm):** Người chơi di chuyển đến cạnh Hoa, dùng tay nắm lấy cánh tay/vai Hoa và kéo lê theo (Drag mechanic). Người chơi được thưởng điểm Nhân đạo & Ứng phó chuyên nghiệp ($+30\text{ điểm}$).
* **Kiểm tra nhiệt độ tay nắm cửa:**
  * Người chơi đến trước cửa thoát hiểm. Nếu đưa tay trực tiếp mở ngay khi tay nắm đỏ rực $\to$ Bị bỏng rát (Controller rung mạnh, $-15\text{ điểm}$).
  * Phải dùng mu bàn tay chạm nhẹ thử nhiệt độ trước, hoặc dùng vạt áo/khăn lau ướt lót tay để vặn chốt mở cửa.

---

### ⏱️ PHASE 5: TỔNG KẾT SAU HÀNH ĐỘNG (AAR DEBRIEF) (03:00 - 03:30)
* Người chơi đẩy cửa bước ra ngoài hành lang thoát hiểm an toàn.
* Màn hình từ từ fade sang không gian tổng kết AAR Virtual Room.
* AI Mentor hiện hình dạng 3D Avatar/Bảng thông số kỹ thuật phân tích toàn diện.
