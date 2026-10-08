# TÀI LIỆU CHIẾN LƯỢC KIẾN TRÚC TOÀN DIỆN: AI EMERGENCY RESPONSE LAB
**Đề tài:** Mô phỏng Diễn tập Ứng phó Sự cố & PCCC Thực tế ảo Thích ứng Thông minh (AI-Adaptive Immersive Emergency Response Simulator)  
**Tác giả / Nhóm nghiên cứu:** Project III - Đại học Bách Khoa Hà Nội (HUST)  
**Master Stack:** Unity 6 (6000.3.15f1) | Universal Render Pipeline (URP 17.3.0) | XR Interaction Toolkit (XRI 3.4.1) | Meta Quest 2 / OpenXR  
**Kiến trúc Hạ tầng:** Hybrid Client-Edge (Kính Meta Quest 2 ↔ LAN / WebSocket ↔ Laptop Server chạy Ollama/Gemma hoặc Gemini Bridge)  

---

## MỤC LỤC CHI TIẾT
1. [Phân tích & So sánh 2 Repository tham khảo (Inferno vs FireBusters VR)](#1-phân-tích--so-sánh-2-repository-tham-khảo)
2. [Vấn đề Kỹ thuật & Rủi ro khi Migration](#2-vấn-đề-kỹ-thuật--rủi-ro-khi-migration)
3. [Chiến lược Kiến trúc: Không Fork nguyên bản, chắt lọc Logic cốt lõi](#3-chiến-lược-kiến-trúc-không-fork-nguyên-bản)
4. [Đối chiếu Danh mục Asset hiện có và Khả năng đáp ứng](#4-đối-chiếu-danh-mục-asset-hiện-có)
5. [Đề xuất Concept Đột phá: "AI Emergency Response Lab"](#5-đề-xuất-concept-đột-phá-ai-emergency-response-lab)
6. [Kịch bản Tình huống Mẫu (Dynamic Emergency Scenarios A–F)](#6-kịch-bản-tình-huống-mẫu-dynamic-scenarios)
7. [AI Scenario Director & Kiến trúc Hybrid Client-Edge](#7-ai-scenario-director--kiến-trúc-hybrid-client-edge)
   - 7.1. [Mô hình Khoa học: Ước lượng Stress qua Động học 6DOF (Tham khảo IEEE VR / ACM CHI)](#71-mô-hình-khoa-học-ước-lượng-stress-qua-động-học-6dof)
   - 7.2. [Kho Kịch bản Dị thường 5–7 Phút (Edge-Case Anomalies)](#72-kho-kịch-bản-dị-thường-57-phút-edge-case-anomalies)
8. [AI Safety Instructor (Trợ lý Giảng dạy Socratic)](#8-ai-safety-instructor-trợ-lý-socratic)
9. [Vòng lặp Tương tác Giọng nói Hai chiều (STT ↔ AI Reasoning ↔ TTS)](#9-vòng-lặp-tương-tác-giọng-nói-hai-chiều)
   - 9.1. [Ra lệnh cho NPC đồng đội bằng Giọng nói & Kỹ năng Lãnh đạo Khẩn cấp](#91-ra-lệnh-cho-npc-đồng-đội-bằng-giọng-nói)
   - 9.2. [AI Hội thoại Hai chiều theo Ngữ cảnh Thực tế (Context-Aware Socratic Dialogue)](#92-ai-hội-thoại-hai-chiều-theo-ngữ-cảnh-thực-tế)
10. [Hệ thống NPC Đa chiều & Tình huống Tiến thoái Lưỡng nan](#10-hệ-thống-npc-đa-chiều)
11. [Sự khác biệt Cốt lõi: Game truyền thống vs. Chu trình Học tập EdTech](#11-sự-khác-biệt-cốt-lõi-về-chu-trình-học-tập)
12. [Thiết kế Prototype Không gian Nhỏ nhưng Tương tác Cao](#12-thiết-kế-prototype-không-gian-tương-tác-cao)
13. [Cơ chế Khói độc (Smoke as a Gameplay Mechanic)](#13-cơ-chế-khói-độc-smoke-as-gameplay)
14. [Âm thanh Không gian làm Nguồn Định hướng Thông tin (Spatial Audio)](#14-âm-thanh-không-gian-làm-nguồn-định-hướng)
15. [Hệ thống Đánh giá Sau Hành động (AI After-Action Review - AAR)](#15-hệ-thống-đánh-giá-sau-hành-động-aar)
   - 15.1. [AI Tự động sinh Giáo án Khắc phục Cá nhân hóa (Generative Remedial Learning)](#151-ai-tự-động-sinh-giáo-án-khắc-phục-cá-nhân-hóa)
16. [Tích hợp Chuẩn EdTech LMS daotao.ai & Dashboard Báo cáo](#16-tích-hợp-chuẩn-edtech-lms-daotaoai--dashboard-báo-cáo)
17. [Định vị Khoa học & Học thuật của Đề tài (Framing Innovation)](#17-định-vị-khoa-học--học-thuật-của-đề-tài)
18. [Chiến lược Kỹ thuật Chi tiết: Tách & Tái sử dụng](#18-chiến-lược-kỹ-thuật-chi-tiết)
19. [Sơ đồ Kiến trúc Hệ thống Tổng thể (Hệ sinh thái AI 4 Trụ cột)](#19-sơ-đồ-kiến-trúc-hệ-thống-tổng-thể-hệ-sinh-thái-ai-4-trụ-cột)
20. [Bảng Ma trận Mổ xẻ Kỹ thuật & Migration Roadmap](#20-bảng-ma-trận-mổ-xẻ-kỹ-thuật--migration-roadmap)

---

### 1. PHÂN TÍCH & SO SÁNH 2 REPOSITORY THAM KHẢO

#### 🔥 Repository 1: `ISTE-VIT/Inferno` ([GitHub](https://github.com/ISTE-VIT/Inferno))
* **Bản chất:** Training Simulator chuẩn trường đại học VIT.
* **Cốt lõi:** Chuỗi quy trình:
  $$\text{Building} \longrightarrow \text{Fire} \longrightarrow \text{Player Actions} \longrightarrow \text{Evacuation / Firefighting} \longrightarrow \text{Telemetry} \longrightarrow \text{Performance Evaluation}$$

#### 🔥 Repository 2: `marcusleeeugene/FireBusters-VR` ([GitHub](https://github.com/marcusleeeugene/FireBusters-VR))
* **Bản chất:** Interactive VR Game tự do cao với 3 chế độ: *Extinguisher Level ➔ Sandbox Level ➔ Fire Escape Level*.
* **Cốt lõi:** Thao tác vật lý 6DOF và cơ chế thử nghiệm tự do dập lửa.

---

### 2. VẤN ĐỀ KỸ THUẬT & RỦI RO KHI MIGRATION
* `FireBusters-VR` dùng **Unity 2020.2.1f1 + HTC Vive (SteamVR Legacy)**.
* `Inferno` dùng Unity 2019/2020 Built-in Pipeline.
* **Quyết định:** Không fork nguyên bản để tránh nợ kỹ thuật (Dependency Hell); giữ Master Project hiện đại trên Unity 6 và bóc tách logic.

---

### 3. CHIẾN LƯỢC KIẾN TRÚC: KHÔNG FORK NGUYÊN BẢN

```
                  ┌─────────────────────────────────────────────────┐
                  │                 MASTER PROJECT                  │
                  │   Unity 6 (6000.3) • URP 17.3 • XRI 3.4 • OpenXR │
                  └────────────────────────┬────────────────────────┘
                                           │
         ┌─────────────────────────────────┼─────────────────────────────────┐
         ↓                                 ↓                                 ↓
┌──────────────────────┐        ┌──────────────────────┐        ┌──────────────────────┐
│    VR INTERACTION    │        │     FIRE SYSTEM      │        │      AI SYSTEM       │
└──────────┬───────────┘        └──────────┬───────────┘        └──────────┬───────────┘
           ↑                               ↑                               ↑
   FireBusters Concept              Inferno Concept                  Your Own Design
(6DOF Physics & Sandbox)       (Telemetry & Evacuation)       (Director + Socratic Tutor)
```

---

### 4. ĐỐI CHIẾU DANH MỤC ASSET HIỆN CÓ

| Asset đã Import | Phiên bản | Vai trò Kiến trúc |
| :--- | :---: | :--- |
| **POLYGON Office (Synty)** | `v1.07` | Môi trường tổng thể, phòng ốc, cửa, bàn ghế |
| **Technical Laboratory Designer** | `v2.0` | Phòng thí nghiệm kỹ thuật, thiết bị máy móc |
| **Server rack computer servers** | `v1.0` | Hạ tầng mạng, cụm tủ Server khởi phát sự cố |
| **Real Fire & Smoke** | `v1.13` | Particle lửa, tia lửa điện, khói ban đầu |
| **Responsive Smokes URP** | `v1.2.0` | Khói thể tích Volumetric URP giảm tầm nhìn theo thời gian |
| **Google Cloud Speech (STT)** | `v5.0` | Nhận diện giọng nói người học |
| **Overtone Offline TTS** | `v1.5.2` | Phát giọng nói AI ngoại tuyến không phụ thuộc Internet |
| **XR Interaction Toolkit** | `v3.4.1` | Tương tác VR chuẩn OpenXR trên Meta Quest 2 |
| **Universal Render Pipeline** | `v17.3.0` | Đồ họa tối ưu cho kính di động, sạch lỗi màu tím |

---

### 5. ĐỀ XUẤT CONCEPT ĐỘT PHÁ: "AI EMERGENCY RESPONSE LAB"
Chuyển hóa từ một Game PCCC thụ động thành một **Trình giả lập Ứng phó Sự cố Mở thích ứng thông minh (Open-Ended Emergency Response Simulator)**:
* Không Tutorial cầm tay chỉ việc.
* Tự do quan sát, lắng nghe, suy luận logic và chịu trách nhiệm với hậu quả hành vi.

---

### 6. KỊCH BẢN TÌNH HUỐNG MẪU (DYNAMIC SCENARIOS A–F)
* **Scenario A:** Server quá tải nhiệt bốc cháy âm ỉ.
* **Scenario B:** Chập điện bảng điện tổng (Arc Flash).
* **Scenario C:** Cháy lan sang khay hóa chất tẩy rửa mạch.
* **Scenario D:** Đám cháy bùng lớn chắn mất cửa thoát hiểm chính.
* **Scenario E:** Khói đen tích tụ nhanh làm mất hoàn toàn tầm nhìn.
* **Scenario F:** Nhiều ổ cháy bùng phát đồng thời (Multiple Fires).

---

### 7. AI SCENARIO DIRECTOR & KIẾN TRÚC HYBRID CLIENT-EDGE

Nhằm đảm bảo **kính Quest 2 duy trì ổn định 72 FPS** và **không bị ngắt quãng game loop do trễ mạng**, hệ thống áp dụng kiến trúc lai:
1. **Tại Kính VR (Client):** C# Dynamic State Machine xử lý ngay lập tức các biến số vật lý (lửa lan, khói hạ thấp, cửa kẹt) với **độ trễ = 0ms**.
2. **Tại Laptop / Edge Server qua LAN/WebSocket:** Chạy mô hình ngôn ngữ lớn (Ollama / Gemma local hoặc Gemini API) chịu trách nhiệm nhận Telemetry, suy luận hội thoại Socratic và sinh báo cáo tổng kết.

---

#### 7.1. MÔ HÌNH KHOA HỌC: ƯỚC LƯỢNG STRESS QUA ĐỘNG HỌC 6DOF (THAM KHẢO IEEE VR / ACM CHI)

##### a, Cơ sở Lý thuyết & Khảo sát Học thuật
Thay vì đưa ra các ngưỡng phần trăm tùy tiện, mô hình được xây dựng dựa trên các nghiên cứu khoa học uy tín về tải nhận thức và động học hành vi trong thực tế ảo:
* **Lý thuyết Động học Đầu & Mắt (Head Kinematics & Visual Search Entropy):** Theo các nghiên cứu trên *IEEE Transactions on Visualization and Computer Graphics (TVCG)* và *ACM CHI*, khi người dùng chịu áp lực cao (High Cognitive/Emotional Stress), vận tốc góc quay đầu (Head Angular Velocity) và độ phân tán góc nhìn (Gaze/Orientation Dispersion Entropy) tăng vọt do hiện tượng mất phương hướng không gian (Spatial Disorientation).
* **Hiện tượng Rung giật Vi mô (Micro-tremor & Motor Jitter):** Theo y sinh học, kích thích thần kinh giao cảm (Sympathetic Nervous System Arousal) làm tăng trương lực cơ, dẫn đến các dao động tần số cao (8–12 Hz) ở bàn tay, đo được qua cảm biến gia tốc trên tay cầm VR.
* **Động học Di chuyển Bất quy tắc (Locomotion Jerkiness):** Được đo bằng đạo hàm bậc 3 của vị trí (Jerk metric: $\frac{d^3x}{dt^3}$), phản ánh việc người dùng đổi hướng đột ngột, tiến lùi mất kiểm soát.

##### b, Ranh giới phần cứng Meta Quest 2
* Meta Quest 2 **không có Eye Tracking**. Hệ thống tập trung đo đạc **Head-Gaze (Hướng nhìn đầu)** và **Controller Kinematics (Dao động tay cầm)** thông qua chip IMU 6DOF (1000Hz internal, 72–90Hz polling).
* Thuật ngữ chuẩn trong luận văn: **"Behavioral & Kinematic Stress Estimation"**.

##### c, Thuật toán Cửa sổ Trượt (Sliding Window 1.5s - 2.0s)
Chạy cực nhẹ với chi phí CPU < 0.05ms trên Snapdragon XR2:

```csharp
// 1. Vận tốc góc quay đầu chuẩn hóa (Head Angular Velocity):
float headAngularSpeed = Quaternion.Angle(currentHeadRot, prevHeadRot) / Time.deltaTime;
float normHeadSpeed = Mathf.InverseLerp(15f, 180f, headAngularSpeed); // 15-180 deg/s

// 2. Độ phân tán dao động tay cầm (Controller Tremor Variance):
float handJitter = Vector3.Distance(currentHandPos, prevHandPos) - expectedDelta;
float normHandJitter = Mathf.Clamp01(handJitter / maxJitterThreshold);

// 3. Độ giật cục điều hướng (Thumbstick Jerkiness):
float stickAngularChange = Vector2.Angle(currentStick, prevStick) / Time.deltaTime;
float normStickJerk = Mathf.Clamp01(stickAngularChange / 180f);

// 4. Chỉ số Căng thẳng Tổng hợp (Stress/Panic Index: 0.0 -> 1.0):
float stressIndex = (0.45f * normHeadSpeed) + (0.35f * normHandJitter) + (0.20f * normStickJerk);
```

##### d, Phân cấp 3 Vùng Trạng thái Tâm lý & Can thiệp Sư phạm
* 🟢 **Vùng 1 - Kiểm soát Ổn định ($0.0 \le \text{Stress} \le 0.40$):** Cử động mượt, tầm nhìn quét có chủ đích ➔ AI giữ nguyên nhịp độ hoặc tăng nhẹ biến cố.
* 🟡 **Vùng 2 - Căng thẳng Cảnh giác ($0.40 < \text{Stress} \le 0.75$):** Tần suất đảo đầu tăng, tìm kiếm lối thoát ➔ AI theo dõi quyết định tiếp theo.
* 🔴 **Vùng 3 - Hoảng loạn Mất kiểm soát ($\text{Stress} > 0.75$):** Tay run, quay đầu lia lịa, va chạm đồ vật ➔ **AI Mentor chủ động cất giọng trấn tĩnh:** *"Bình tĩnh, hít thở sâu, quan sát vạch sơn màu xanh dưới chân tường"*.

---

#### 7.2. KHO KỊCH BẢN DỊ THƯỜNG 5–7 PHÚT (EDGE-CASE ANOMALIES)

Màn chơi được kéo dài **5–7 phút**, phân thành 3 hồi (Acts) với 4 kịch bản dị thường được kích hoạt tự động theo hành vi hoặc thời gian:

1. **Dị thường 1: Hiện tượng Nổ Bùng Oxy (Backdraft tại Cửa thoát hiểm):**
   * *Điều kiện Trigger:* Người học hoảng sợ lao thẳng ra giật chốt mở cửa mà không kiểm tra độ nóng tay nắm bằng mu bàn tay ➔ Chênh lệch áp suất gây tiếng nổ bùng, ngọn lửa tạt ngược hất văng người chơi (Trừ 30 điểm an toàn).
2. **Dị thường 2: Tủ Pin UPS Dự phòng Phát nổ (Lithium Thermal Runaway):**
   * *Điều kiện Trigger:* Đám cháy tủ server kéo dài quá 90 giây mà chưa ngắt điện tổng ➔ Lửa lan xuống cụm pin UPS dưới sàn, phát ra khói trắng độc nồng nặc và tia lửa bắn xối xả, vô hiệu hóa bình chữa cháy thông thường, buộc phải chuyển ngay sang phương án sơ tán khẩn cấp.
3. **Dị thường 3: Nút Báo cháy bị Hỏng Cơ học / Đứt cáp tín hiệu:**
   * *Điều kiện Trigger:* Người học chạy tới đập nút báo cháy nhưng chuông không kêu ➔ AI Mentor cất giọng: *"Hệ thống chuông tự động bị đứt cáp, hãy dùng khẩu lệnh giọng nói để báo động cho các phòng bên cạnh!"*.
4. **Dị thường 4: Đồng đội Chỉ sai đường do Mất phương hướng (Misleading NPC Panic):**
   * *Điều kiện Trigger:* Khi khói đen hạ thấp che khuất tầm nhìn ➔ NPC Minh chỉ tay về phía cửa sổ kính và hét *"Chạy lối này!"* (thực tế là tầng cao không có ban công). Người học phải giữ chính kiến, từ chối chạy theo và dẫn đường cho đồng đội ra cửa thoát hiểm thật.

---

### 8. AI SAFETY INSTRUCTOR (TRỢ LÝ SOCRATIC)
Khi sinh viên xách nhầm bình nước lại gần tủ điện đang có tải:
* AI không ngắt game hay cấm đoán mà đặt câu hỏi phản biện qua giọng nói:
  > *"Quan sát kỹ nguồn phát cháy! Nước có an toàn quanh nguồn điện đang mang tải không?"*

---

### 9. VÒNG LẶP TƯƠNG TÁC GIỌNG NÓI HAI CHIỀU

$$\text{Người học (Voice)} \overset{\text{STT}}{\rightleftharpoons} \text{AI Mentor (Lý luận / Context)} \overset{\text{TTS}}{\rightleftharpoons} \text{Môi trường VR (Vật lý 6DOF)}$$

#### 9.1. RA LỆNH CHO NPC ĐỒNG ĐỘI BẰNG GIỌNG NÓI (VOICE-CONTROLLED NPC TEAM LEADERSHIP)
* Học viên hô to: *"Minh, cúi thấp người xuống!"*, *"Hoa, chạy ra cửa ngay!"*.
* AI STT bắt khẩu lệnh ➔ Kích hoạt State Machine của NPC chuyển sang trạng thái tuân thủ quy trình, rèn luyện kỹ năng lãnh đạo khẩn cấp.

#### 9.2. AI HỘI THOẠI HAI CHIỀU THEO NGỮ CẢNH THỰC TẾ (CONTEXT-AWARE SOCRATIC DIALOGUE)
* Học viên nhấn giữ nút A/X hỏi tự do: *"Thầy ơi, đám cháy này dùng bình bọt được không?"*.
* Client gửi kèm ngữ cảnh (`hazard_type`, `power_status`, `stress_index`) lên Edge Server ➔ LLM sinh câu trả lời sư phạm thực địa qua Overtone TTS.

---

### 10. HỆ THỐNG NPC ĐA CHIỀU
Gồm 3 NPC: NPC 1 (Hoảng loạn chạy sai hướng), NPC 2 (Am hiểu an toàn), NPC 3 (Bị ngất do ngạt khói). Đặt người học vào tình thế lựa chọn: Dập lửa, sơ tán, cứu người hay bấm báo động.

---

### 11. SỰ KHÁC BIỆT CỐT LÕI VỀ CHU TRÌNH HỌC TẬP
* **FireBusters VR:** $\text{Cháy} \to \text{Chọn bình} \to \text{Điểm}$.
* **Inferno:** $\text{Cháy} \to \text{Thoát hiểm} \to \text{Thời gian} \to \text{Điểm}$.
* **AI Response Lab (Đồ án mới):**
  $$\text{Môi trường động} \to \text{Quan sát} \to \text{Lý luận} \to \text{Hành động} \to \text{AI Giám sát Stress} \to \text{Môi trường thích ứng} \to \text{Rút kinh nghiệm} \to \text{AI Tổng kết}$$

---

### 12. THIẾT KẾ PROTOTYPE KHÔNG GIAN TƯƠNG TÁC CAO
Không gian khép kín 18m x 14m x 4.5m nhìn ra thành phố, bao gồm 12 bàn máy tính, tủ server, bảng điện tổng, bình PCCC, cửa thoát hiểm và hành lang thang bộ.

---

### 13. CƠ CHẾ KHÓI ĐỘC (SMOKE AS GAMEPLAY)
* Mật độ khói tăng dần: $0.0 \to 0.3 \to 0.7 \to 1.0$.
* Người học bắt buộc phải **khom người physically dưới 1.2m** để không bị trừ thanh Oxy.

---

### 14. ÂM THANH KHÔNG GIAN LÀM NGUỒN ĐỊNH HƯỚNG
Khi thị giác suy giảm, âm thanh không gian (tiếng còi hú, tiếng chập điện, tiếng la hét của NPC) trở thành nguồn thông tin chính để sinh viên định vị đường thoát hiểm.

---

### 15. HỆ THỐNG ĐÁNH GIÁ SAU HÀNH ĐỘNG (AAR)
Tái hiện dòng thời gian chính xác từng giây của toàn bộ phiên diễn tập (từ lúc phát hiện khói, ngắt aptomat, chọn bình, đến lúc sơ tán).

#### 15.1. AI TỰ ĐỘNG SINH GIÁO ÁN KHẮC PHỤC CÁ NHÂN HÓA (GENERATIVE REMEDIAL LEARNING)
* LLM sinh **Narrative Feedback Report** phân tích nguyên nhân gốc rễ sai sót.
* Tự động sinh kịch bản diễn tập tiếp theo nhắm đúng vào điểm yếu của từng sinh viên.

---

### 16. TÍCH HỢP CHUẨN EDTECH LMS DAOTAO.AI & DASHBOARD BÁO CÁO

Kết nối qua REST API `POST /api/v1/sessions/{id}/complete` và cung cấp:
1. **Dành cho Sinh viên:**
   * **Radar Chart 5 trục:** (1) Nhận diện nguy cơ & Ngắt điện, (2) Phân loại chất chữa cháy, (3) Kỹ năng khom người né khói, (4) Xử lý tình huống dị thường, (5) Chỉ số vững vàng tâm lý (Stress Resilience).
   * **Timeline hành động từng giây.**
   * **Bản nhận xét sư phạm văn bản tự nhiên của AI.**
2. **Dành cho Giảng viên:** Dashboard quản lý cả lớp, xem tỷ lệ hoàn thành, phát hiện nhanh sinh viên mắc lỗi chí mạng (xịt nước vào điện, gây nổ Backdraft).

---

### 17. ĐỊNH VỊ KHOA HỌC & HỌC THUẬT CỦA ĐỀ TÀI
Tuyên ngôn: *"Môi trường học tập thực tế ảo thích ứng thông minh có khả năng tự động điều phối tình huống khẩn cấp mở dựa trên phân tích động học hành vi và nhận thức người học thời gian thực."*

---

### 18. CHIẾN LƯỢC KỸ THUẬT CHI TIẾT
Master Project chạy trên Unity 6 + URP 17.3 + XRI 3.4.1; bóc tách logic từ FireBusters và Inferno, bổ sung tầng AI 4 trụ cột độc quyền.

---

### 19. SƠ ĐỒ KIẾN TRÚC HỆ THỐNG TỔNG THỂ (HỆ SINH THÁI AI 4 TRỤ CỘT)

```
┌────────────────────────────────────────────────────────┐
│                       VR PLAYER                        │
│            Meta Quest 2 Tracking 6DOF (Head & Hands)   │
└───────────────────────────┬────────────────────────────┘
                            │ (XR Interaction Toolkit 3.4.1)
    ┌───────────────────────┼───────────────────────┐
    ↓                       ↓                       ↓
[Grab / Interact]      [Movement / Crouch]    [Voice Input STT]
    │                       │                       │
    └───────────────────────┼───────────────────────┘
                            ↓
┌────────────────────────────────────────────────────────┐
│                   SIMULATION ENGINE                    │
├────────────────────────────────────────────────────────┤
│ • Fire Hazards (Spread logic)                          │
│ • Volumetric Smoke (Density & Occlusion)               │
│ • Electrical Panels & Extinguishers                    │
│ • Dynamic Doors & Obstacles                            │
│ • Interactive Multi-Agent NPCs                         │
└───────────────────────────┬────────────────────────────┘
                            │ (Event Bus Trục trung tâm)
                            ↓
┌────────────────────────────────────────────────────────┐
│              HỆ THỐNG AI ĐIỀU PHỐI 4 TRỤ CỘT           │
├────────────────────────────────────────────────────────┤
│ 1. AI BIOMETRICS: Ước lượng Stress qua Động học 6DOF   │
│ 2. AI DIRECTOR: Điều phối độ khó & Tình huống thích ứng│
│ 3. VOICE SOCRATIC: Hỏi gợi mở & Nhận lệnh điều khiển   │
│ 4. GENERATIVE AAR: Tự sinh nhận xét & Kịch bản khắc phục│
└───────────────────────────┬────────────────────────────┘
                            │ (Telemetry Stream qua WebSocket / LAN)
                            ↓
┌────────────────────────────────────────────────────────┐
│                   LEARNING ANALYTICS                   │
├────────────────────────────────────────────────────────┤
│ • Actions Timeline & Cognitive Decision Latency        │
│ • Stress Resilience & Crouch Compliance Metrics        │
│ • Evacuation Path Efficiency Calculator                │
│ • Cổng tích hợp API đồng bộ LMS daotao.ai              │
└────────────────────────────────────────────────────────┘
```

---

### 20. BẢNG MA TRẬN MỔ XẺ KỸ THUẬT & MIGRATION ROADMAP

| Thành phần (Component) | `FireBusters-VR` | `ISTE-VIT/Inferno` | Kế thừa / Giữ lại? | Viết mới / Nâng cấp? |
| :--- | :--- | :--- | :---: | :---: |
| **Fire System** | Hạt lửa cơ bản | Trigger diện tích | 📥 Port logic dập lửa | 🔄 Rewrite URP Particle & Tốc độ cháy lan |
| **Extinguisher** | 6DOF rút chốt, bóp cò | Bình xịt cơ bản | 📥 Lấy logic phân loại | 🔄 Rewrite trên XRI 3.4.1 GrabInteractable |
| **Evacuation** | Lối thoát tĩnh | Đo quãng đường ngắn nhất | 📥 Port thuật toán | 🔄 Rewrite khói cản trở tầm nhìn |
| **Player Interaction** | SteamVR Actions cũ | Custom XR Input | ❌ Discard SDK cũ | 🚀 Xây mới trên Unity OpenXR hiện đại |
| **Haptics** | Rung tay cầm HTC Vive | Rung phản hồi cơ bản | 📥 Port ý tưởng | 🔄 Chuẩn hóa Haptic Impulse của XRI 3.4 |
| **Scoring** | Điểm số dập lửa | Điểm theo thời gian/khoảng cách | 📥 Lấy công thức nền | 🚀 Nâng cấp thành Ma trận đánh giá EdTech |
| **AI Layer** | Không có | Thuật toán đánh giá tĩnh | 📥 Lấy tiêu chí đánh giá | 🚀 Xây mới Hệ sinh thái AI 4 Trụ cột |
| **Telemetry** | Không lưu trữ | Lưu nặc danh cơ bản | 📥 Lấy mốc sự kiện | 🚀 Chuẩn hóa JSON & Đồng bộ LMS daotao.ai |
| **Unity Version** | 2020.2.1f1 | 2019 / 2020 | ❌ Discard phiên bản cũ | 🚀 Master Project chạy **Unity 6 (6000.3)** |
| **XR Framework** | SteamVR Legacy | Legacy XR | ❌ Discard SDK cũ | 🚀 Dùng chuẩn **XRI 3.4.1 + OpenXR** |
| **3D Assets** | Cũ / Lỗi shader | Mesh VIT trường học cũ | ❌ Discard mesh cũ | 💎 Dùng kho Asset URP Synty + Lab Designer |
