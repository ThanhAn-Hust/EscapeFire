# TÀI LIỆU CHIẾN LƯỢC KIẾN TRÚC TOÀN DIỆN: AI EMERGENCY RESPONSE LAB
**Đề tài:** Mô phỏng Diễn tập Ứng phó Sự cố & PCCC Thực tế ảo Thích ứng Thông minh (AI-Adaptive Immersive Emergency Response Simulator)  
**Tác giả / Nhóm nghiên cứu:** Project III - Đại học Bách Khoa Hà Nội (HUST)  
**Master Stack:** Unity 6 (6000.3.15f1) | Universal Render Pipeline (URP 17.3.0) | XR Interaction Toolkit (XRI 3.4.1) | Meta Quest / OpenXR  

---

## MỤC LỤC CHI TIẾT
1. [Phân tích & So sánh 2 Repository tham khảo (Inferno vs FireBusters VR)](#1-phân-tích--so-sánh-2-repository-tham-khảo)
2. [Vấn đề Kỹ thuật & Rủi ro khi Migration](#2-vấn-đề-kỹ-thuật--rủi-ro-khi-migration)
3. [Chiến lược Kiến trúc: Không Fork nguyên bản, chắt lọc Logic cốt lõi](#3-chiến-lược-kiến-trúc-không-fork-nguyên-bản)
4. [Đối chiếu Danh mục Asset hiện có và Khả năng đáp ứng](#4-đối-chiếu-danh-mục-asset-hiện-có)
5. [Đề xuất Concept Đột phá: "AI Emergency Response Lab"](#5-đề-xuất-concept-đột-phá-ai-emergency-response-lab)
6. [Kịch bản Tình huống Mẫu (Dynamic Emergency Scenarios A–F)](#6-kịch-bản-tình-huống-mẫu-dynamic-scenarios)
7. [AI Scenario Director (Đạo diễn Tình huống Động)](#7-ai-scenario-director-đạo-diễn-tình-huống-động)
8. [AI Safety Instructor (Trợ lý Giảng dạy Socratic)](#8-ai-safety-instructor-trợ-lý-socratic)
9. [Vòng lặp Tương tác Giọng nói Hai chiều (STT ↔ AI Reasoning ↔ TTS)](#9-vòng-lặp-tương-tác-giọng-nói-hai-chiều)
10. [Hệ thống NPC Đa chiều & Tình huống Tiến thoái Lưỡng nan](#10-hệ-thống-npc-đa-chiều)
11. [Sự khác biệt Cốt lõi: Game truyền thống vs. Chu trình Học tập EdTech](#11-sự-khác-biệt-cốt-lõi-về-chu-trình-học-tập)
12. [Thiết kế Prototype Không gian Nhỏ nhưng Tương tác Cao](#12-thiết-kế-prototype-không-gian-tương-tác-cao)
13. [Cơ chế Khói độc (Smoke as a Gameplay Mechanic)](#13-cơ-chế-khói-độc-smoke-as-gameplay)
14. [Âm thanh Không gian làm Nguồn Định hướng Thông tin (Spatial Audio)](#14-âm-thanh-không-gian-làm-nguồn-định-hướng)
15. [Hệ thống Đánh giá Sau Hành động (AI After-Action Review - AAR)](#15-hệ-thống-đánh-giá-sau-hành-động-aar)
16. [Mô hình Dữ liệu Telemetry & Dashboard Phân tích Sinh viên](#16-mô-hình-dữ-liệu-telemetry--dashboard)
17. [Định vị Khoa học & Học thuật của Đề tài (Framing Innovation)](#17-định-vị-khoa-học--học-thuật-của-đề-tài)
18. [Chiến lược Kỹ thuật Chi tiết: Tách & Tái sử dụng](#18-chiến-lược-kỹ-thuật-chi-tiết)
19. [Sơ đồ Kiến trúc Hệ thống Tổng thể (System Architecture Diagram)](#19-sơ-đồ-kiến-trúc-hệ-thống-tổng-thể)
20. [Bảng Ma trận Mổ xẻ Kỹ thuật & Migration Roadmap](#20-bảng-ma-trận-mổ-xẻ-kỹ-thuật--migration-roadmap)

---

### 1. PHÂN TÍCH & SO SÁNH 2 REPOSITORY THAM KHẢO

Hai repository mã nguồn mở trên GitHub đại diện cho hai trường phái tiếp cận PCCC trong VR:

#### 🔥 Repository 1: `ISTE-VIT/Inferno` ([GitHub](https://github.com/ISTE-VIT/Inferno))
* **Bản chất:** Là một **"Training Simulator"** chuẩn mực trong trường đại học (mô phỏng theo tòa nhà TT Building của Đại học VIT).
* **Đặc tính kỹ thuật nổi bật:**
  - Tòa nhà đại học chi tiết, các điểm kích hoạt báo động (alarm / manual call points).
  - Bản đồ các tuyến đường thoát hiểm (evacuation pathways).
  - Prefab ngọn lửa có khả năng dập tắt bằng nhiều loại thiết bị (bình bọt, CO2, vòi nước).
  - Phản hồi đa giác quan (Audio, Visual, Haptics).
  - **Grading System & Thuật toán AI:** Đánh giá tuyến đường di chuyển (pathway), thời gian phản xạ (timing), và hiệu quả (efficiency), lưu trữ dữ liệu nặc danh phục vụ nghiên cứu.
* **Giá trị cốt lõi kế thừa:** Skeleton kiến trúc của Inferno:
  $$\text{Building} \longrightarrow \text{Fire} \longrightarrow \text{Player Actions} \longrightarrow \text{Evacuation / Firefighting} \longrightarrow \text{Telemetry} \longrightarrow \text{Performance Evaluation}$$

#### 🔥 Repository 2: `marcusleeeugene/FireBusters-VR` ([GitHub](https://github.com/marcusleeeugene/FireBusters-VR))
* **Bản chất:** Là một **"Interactive VR Game"** có tính tự do cao (Player Agency).
* **Cấu trúc 3 Chế độ chơi bài bản:**
  1. *Fire Extinguisher Level:* Dạy lý thuyết và phân loại bình chữa cháy theo từng đám cháy.
  2. *Sandbox Level (Open-World Concept):* Cho phép người chơi tự do tạo lửa, thử nghiệm đốt các vật liệu khác nhau và dùng các chất chữa cháy khác nhau để quan sát phản ứng.
  3. *Fire Escape Level:* Diễn tập quy trình thoát hiểm khẩn cấp trong tòa nhà.
* **Giá trị cốt lõi kế thừa:** Khái niệm Sandbox và cơ chế vật lý tương tác 6DOF – làm những điều chỉ có VR mới thể hiện được mà 2D không làm được.

---

### 2. VẤN ĐỀ KỸ THUẬT & RỦI RO KHI MIGRATION

* **Cảnh báo phiên bản (Version Warning):**
  - `FireBusters-VR` yêu cầu cứng: **Unity 2020.2.1f1 + HTC Vive (SteamVR SDK Legacy)**.
  - `Inferno` được xây dựng trên Unity 2019/2020 với Built-in Render Pipeline.
* **Rủi ro Dependency Hell:**
  - Nếu clone toàn bộ repository về rồi mở bằng Unity mới, dự án sẽ biến thành một *"dự án sửa lỗi di trú (Migration Project)"* tốn hàng trăm giờ chỉ để vá lỗi API gãy của SteamVR, lỗi Shader hồng (Pink Shader do lệch Built-in sang URP) và lỗi Package Manager.
  - Việc này làm phân tán nguồn lực khỏi mục tiêu chính của đồ án là **Nghiên cứu EdTech & AI**.

---

### 3. CHIẾN LƯỢC KIẾN TRÚC: KHÔNG FORK NGUYÊN BẢN

Chiến lược tối ưu là **giữ nguyên Master Project hiện đại** và chỉ bóc tách logic/gameplay concepts:

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
           │                               │                               │
   FireBusters Concept              Inferno Concept                  Your Own Design
(6DOF Physics & Sandbox)       (Telemetry & Evacuation)       (Director + Socratic Tutor)
```

---

### 4. ĐỐI CHIẾU DANH MỤC ASSET HIỆN CÓ VÀ KHẢ NĂNG ĐÁP ỨNG

Kho Asset hiện tại của dự án đã sẵn sàng và khớp hoàn hảo với các tầng kiến trúc:

| Asset đã Import | Phiên bản | Vai trò Kiến trúc | Giá trị Gia tăng so với 2 Repo cũ |
| :--- | :---: | :--- | :--- |
| **POLYGON Office (Synty)** | `v1.07` | Môi trường tổng thể, phòng ốc, cửa, bàn ghế | Stylized Low-Poly chuẩn đẹp, cực nhẹ cho Meta Quest Standalone |
| **Technical Laboratory Designer** | `v2.0` | Phòng thí nghiệm kỹ thuật, thiết bị máy móc | Cung cấp đúng bối cảnh IT/Lab nghiên cứu |
| **Server rack computer servers** | `v1.0` | Hạ tầng mạng, cụm tủ Server | Điểm khởi phát sự cố chập điện thực tế |
| **Real Fire & Smoke** | `v1.13` | Particle lửa, tia lửa điện, khói ban đầu | Hiệu ứng cháy chân thực |
| **Responsive Smokes URP** | `v1.2.0` | Khói thể tích Volumetric URP tương tác | Giảm tầm nhìn theo thời gian thực (Gameplay Mechanic) |
| **Google Cloud Speech (STT)** | `v5.0` | Nhận diện giọng nói người học | Tương tác đàm thoại tự nhiên với AI Mentor |
| **Overtone Offline TTS** | `v1.5.2` | Phát giọng nói AI ngoại tuyến | Phản hồi tức thì, không phụ thuộc kết nối Internet |
| **XR Interaction Toolkit** | `v3.4.1` | Tương tác VR chuẩn OpenXR | Cầm nắm, rút chốt, gạt aptomat, XR Device Simulator |
| **Universal Render Pipeline** | `v17.3.0` | Pipeline đồ họa chiếu sáng hiện đại | Đã chuyển đổi 100% sạch lỗi màu tím (Magenta Shader) |

---

### 5. ĐỀ XUẤT CONCEPT ĐỘT PHÁ: "AI EMERGENCY RESPONSE LAB"

Thay vì làm một ứng dụng *"VR Fire Safety"* thông thường với các bảng hướng dẫn cứng nhắc, đồ án định vị thành một **Trình giả lập Ứng phó Sự cố Mở thích ứng thông minh (Open-Ended Emergency Response Simulator)**:
* **Không Tutorial dài dòng:** Người học không bị ép buộc *"Bước 1: nhặt bình"*, *"Bước 2: bấm nút A"*.
* **Tự do hành động:** Người học bước vào phòng lab, tự quan sát, lắng nghe, suy luận logic và chịu trách nhiệm với hậu quả hành vi của mình.

---

### 6. KỊCH BẢN TÌNH HUỐNG MẪU (DYNAMIC SCENARIOS)

Người học xuất hiện trong phòng Lab Kỹ thuật đầy đủ thiết bị (Server racks, PC, thiết bị điện, hóa chất, bình chữa cháy, cửa thoát hiểm, chuông báo cháy, cảm biến khói).
Sau khoảng 30 giây thực hành bình thường, chuỗi sự cố ngẫu nhiên bắt đầu:
$$\text{Lỗi chập điện} \longrightarrow \text{Tia lửa} \longrightarrow \text{Khói tích tụ} \longrightarrow \text{Còi báo động} \longrightarrow \text{Cháy lan}$$

Các biến thể ngẫu nhiên (Scenarios):
* **Scenario A:** Server quá tải nhiệt bốc cháy âm ỉ.
* **Scenario B:** Chập điện bảng điện tổng (Arc Flash).
* **Scenario C:** Cháy lan sang khay hóa chất tẩy rửa mạch.
* **Scenario D:** Đám cháy bùng lớn chắn mất cửa thoát hiểm chính.
* **Scenario E:** Khói đen tích tụ nhanh làm mất hoàn toàn tầm nhìn.
* **Scenario F:** Nhiều ổ cháy bùng phát đồng thời (Multiple Fires).

---

### 7. AI SCENARIO DIRECTOR (ĐẠO DIỄN TÌNH HUỐNG ĐỘNG)

AI không phải là một con bot vô thưởng vô phạt đứng trong góc phòng, mà đóng vai trò là **Hệ thống Đạo diễn Mô phỏng (Scenario Director)**:

```
                       ┌─────────────────────────┐
                       │   PLAYER PERFORMANCE    │
                       └────────────┬────────────┘
                                    │
                                    ↓
                       ┌─────────────────────────┐
                       │  AI SCENARIO DIRECTOR   │
                       └────────────┬────────────┘
                                    │
            ┌───────────────────────┼───────────────────────┐
            ↓                       ↓                       ↓
   ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
   │   FIRE SPREAD   │     │  SMOKE DENSITY  │     │  NPC BEHAVIOR   │
   │  Tốc độ cháy lan│     │ Mật độ khói mù  │     │ Hoảng loạn/Cản trở│
   └─────────────────┘     └─────────────────┘     └─────────────────┘
```

* **Thích ứng năng lực:** Nếu người chơi xử lý chọn bình quá nhanh và chuẩn xác ➔ AI tăng độ khó bằng cách làm lửa lan sang vật liệu khác hoặc khóa lối thoát chính.
* **Uốn nắn hành vi:** Nếu người chơi chỉ chăm chăm lao vào dập lửa mà quên kiểm tra lối thoát hiểm ➔ AI tạo kịch bản khói chặn đường để buộc người chơi phải suy nghĩ lại về ưu tiên bảo toàn tính mạng.

---

### 8. AI SAFETY INSTRUCTOR (TRỢ LÝ SOCRATIC)

Áp dụng phương pháp Sư phạm Gợi mở (Socratic Method) qua giọng nói:
* Khi người học cầm **Bình Nước/Bọt** tiến lại gần cụm Server đang cháy:
  - *Dữ liệu hệ thống ghi nhận:* `Object: Server | Fire Class: Electrical (Class C) | Player Tool: Water Extinguisher | Distance: 1.8m`.
  - Thay vì ngắt game báo *"Sai rồi"*, AI Mentor cất giọng gợi mở:
    > *"Hãy quan sát kỹ loại thiết bị đang phát lửa."*
  - Nếu người học vẫn tiếp tục giơ vòi xịt:
    > *"Nước có an toàn khi sử dụng quanh thiết bị điện đang mang điện áp không?"*

---

### 9. VÒNG LẶP TƯƠNG TÁC GIỌNG NÓI HAI CHIỀU

Sự kết hợp giữa **Google Cloud STT** và **Overtone Offline TTS** tạo nên tam giác tương tác:

$$\text{Người học (Voice)} \overset{\text{STT}}{\rightleftharpoons} \text{AI Mentor (Lý luận / Context)} \overset{\text{TTS}}{\rightleftharpoons} \text{Môi trường VR (Vật lý 6DOF)}$$

* *Người học hỏi:* "Tôi nên dùng bình nào để dập lửa ở đây?"
* *AI xử lý:* Nhận diện vị trí người học đang đứng trước tủ điện.
* *AI trả lời (TTS):* "Hãy quan sát ký hiệu trên thân bình và nguồn phát cháy trước tiên."

---

### 10. HỆ THỐNG NPC ĐA CHIỀU

Trong phòng có 3 nhân vật với tâm lý khác nhau:
1. **NPC 1 (Hoảng loạn):** La hét chạy sai hướng *"CỬA THOÁT HIỂM ĐẰNG KIA!"* (hướng đám cháy). Người chơi phải giữ bình tĩnh, không hùa theo đám đông.
2. **NPC 2 (Kỹ thuật viên am hiểu):** Biết quy trình an toàn nhưng bị kẹt tay hoặc cần người hỗ trợ.
3. **NPC 3 (Nạn nhân ngạt khói):** Bị ngất ở góc phòng.
* **Tình huống tiến thoái lưỡng nan:** Dập lửa? Thoát hiểm ngay? Cứu người? Hay bấm báo động trước? Không có một đáp án duy nhất đúng, điểm số đánh giá theo tính logic và đạo đức an toàn.

---

### 11. SỰ KHÁC BIỆT CỐT LÕI VỀ CHU TRÌNH HỌC TẬP

| Hệ thống | Chu trình Vận hành | Đánh giá |
| :--- | :--- | :--- |
| **FireBusters VR** | $\text{Cháy} \to \text{Người chơi} \to \text{Chọn đúng bình} \to \text{Điểm}$ | Dạng Game arcade đối kháng cháy |
| **Inferno** | $\text{Cháy} \to \text{Thoát hiểm} \to \text{Đo Quãng đường / Thời gian} \to \text{Điểm}$ | Diễn tập sơ tán quy chuẩn |
| **AI Response Lab (Đồ án mới)** | $\text{Môi trường động} \to \text{Quan sát} \to \text{Lý luận} \to \text{Hành động} \to \text{AI Giám sát} \to \text{Môi trường thích ứng} \to \text{Người học rút kinh nghiệm} \to \text{AI Tổng kết}$ | **Chu trình Sư phạm EdTech hoàn chỉnh** |

---

### 12. THIẾT KẾ PROTOTYPE KHÔNG GIAN TƯƠNG TÁC CAO

Không cần dựng cả một trường đại học rộng lớn nhưng trống rỗng. Tập trung vào một không gian khép kín (18m x 14m x 4.5m) nhưng **cực kỳ đậm đặc tương tác**:
$$\text{POLYGON Office} \to \text{Phòng Server High-Tech} \to \text{Hạ tầng Điện} \to \text{Hạt lửa & Khói URP} \to \text{Aptomat} \to \text{Bình CO2} \to \text{Hành lang Thoát hiểm}$$

---

### 13. CƠ CHẾ KHÓI ĐỘC (SMOKE AS GAMEPLAY)

Khói được định nghĩa bằng hàm mật độ và cản trở giác quan:
* $\text{Density} = 0.0 \implies \text{Tầm nhìn } 100\%$
* $\text{Density} = 0.3 \implies \text{Tầm nhìn } 70\%$
* $\text{Density} = 0.7 \implies \text{Tầm nhìn } 30\%$
* $\text{Density} = 1.0 \implies \text{Mất hoàn toàn định hướng thị giác}$
* **Cơ chế bắt buộc người học:** Phải physically **khom người / quỳ gối dưới 1.2m**, lần theo chân tường, nhìn đèn thoát hiểm màu xanh ở tầm thấp, lắng nghe âm thanh và nhớ lại sơ đồ phòng.

---

### 14. ÂM THANH KHÔNG GIAN LÀM NGUỒN ĐỊNH HƯỚNG (SPATIAL AUDIO)

Khi thị giác suy giảm do khói mù:
$$\text{Visual Information} \downarrow \quad \implies \quad \text{Auditory Information} \uparrow$$
* Tiếng còi báo động định hướng cửa thoát hiểm.
* Tiếng lách tách chập điện định vị nguy hiểm cần tránh xa.
* Tiếng la hét của NPC giúp định vị nạn nhân cần cứu nạn.

---

### 15. HỆ THỐNG ĐÁNH GIÁ SAU HÀNH ĐỘNG (AAR)

Sau khi kết thúc, hệ thống dựng lại dòng thời gian chính xác:
```text
[00:00] Sinh viên bước vào phòng lab
[00:14] Sự cố chập điện phát sinh
[00:18] Sinh viên nhận biết khói bốc lên
[00:31] Sinh viên kích hoạt nút báo cháy
[00:47] Sinh viên tiếp cận giá treo bình chữa cháy
[01:02] Phát hiện lấy nhầm bình Nước cho đám cháy điện
[01:17] Sinh viên đổi sang bình CO2, ngắt Aptomat
[01:24] Khom người tìm lối thoát hiểm
[01:51] Thoát hiểm thành công ra hành lang an toàn
```

**Báo cáo Đánh giá của AI Mentor (Debrief):**
* *Điểm làm tốt:* Nhận diện khói sớm, ngắt aptomat trước khi dập, duy trì tư thế khom người chuẩn định mức.
* *Điểm cần cải thiện:* Mất 31 giây do bối rối trong việc chọn đúng bình CO2.
* *Đề xuất kịch bản tiếp theo:* Tăng độ khó với tình huống cửa thoát hiểm chính bị kẹt.

---

### 16. MÔ HÌNH DỮ LIỆU TELEMETRY & DASHBOARD

Dữ liệu buổi tập được chuẩn hóa JSON cho Web Dashboard giảng viên:
```json
{
  "session": {
    "student_id": "SV2026_HUST",
    "scenario_id": "LAB_ELECTRICAL_FIRE_01",
    "duration_seconds": 111.0,
    "metrics": {
      "fire_identification_score": 82,
      "extinguisher_selection_score": 61,
      "evacuation_efficiency_score": 91,
      "decision_making_latency": 14.2,
      "crouch_compliance_percent": 88.5
    }
  }
}
```

---

### 17. ĐỊNH VỊ KHOA HỌC & HỌC THUẬT CỦA ĐỀ TÀI

* **Tránh phát biểu:** *"Đề tài của chúng em là đưa ChatGPT vào game VR PCCC."*
* **Tuyên ngôn đề tài chuẩn học thuật:**
  > *"Môi trường học tập thực tế ảo thích ứng thông minh (AI-Adaptive Immersive Learning Environment) có khả năng tự động điều phối tình huống khẩn cấp mở dựa trên phân tích hành vi người học thời gian thực."*
* **Điểm đột phá khoa học:** VR 6DOF + Open-ended Interaction + Dynamic Scenarios + Voice AI + Behavioral Telemetry + Adaptive Difficulty + AI Debrief.

---

### 18. CHIẾN LƯỢC KỸ THUẬT CHI TIẾT

```
Master Project (Unity 6 + URP 17.3 + XRI 3.4.1)
  ├── Kế thừa từ FireBusters: Logic phân loại đám cháy (Class A/B/C/D), cơ chế tương tác bình xịt vật lý
  ├── Kế thừa từ Inferno: Thuật toán đo đường đi thoát hiểm, cấu trúc drill đại học, khung telemetry
  └── Độc quyền phát triển: AI Scenario Director, Voice Socratic Tutor, Phân tích hành vi EdTech
```

---

### 19. SƠ ĐỒ KIẾN TRÚC HỆ THỐNG TỔNG THỂ

```
┌────────────────────────────────────────────────────────┐
│                       VR PLAYER                        │
│            Meta Quest / OpenXR Tracking 6DOF           │
└───────────────────────────┬────────────────────────────┘
                            │ (XR Interaction Toolkit)
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
│ • Interactive NPCs                                     │
└───────────────────────────┬────────────────────────────┘
                            │ (Event Dispatcher)
                            ↓
┌────────────────────────────────────────────────────────┐
│                     AI CONTROLLER                      │
├────────────────────────────────────────────────────────┤
│ • Scenario Director (Điều phối độ khó & Biến số)       │
│ • Socratic AI Tutor (Gợi mở tư duy qua Voice TTS)      │
│ • NPC Intelligence & Panic State Machine               │
│ • Adaptive Difficulty Scaler                           │
│ • After-Action Review (AAR) Generator                  │
└───────────────────────────┬────────────────────────────┘
                            │ (Telemetry Stream)
                            ↓
┌────────────────────────────────────────────────────────┐
│                   LEARNING ANALYTICS                   │
├────────────────────────────────────────────────────────┤
│ • Actions Timeline & Mistake Logger                    │
│ • Cognitive Decision Latency Tracker                   │
│ • Evacuation Path Efficiency Calculator                │
│ • JSON Telemetry Exporter for Instructor Dashboard     │
└────────────────────────────────────────────────────────┘
```

---

### 20. BẢNG MA TRẬN MỔ XẺ KỸ THUẬT & MIGRATION ROADMAP

| Thành phần (Component) | `FireBusters-VR` | `ISTE-VIT/Inferno` | Kế thừa / Giữ lại? | Viết mới / Nâng cấp? |
| :--- | :--- | :--- | :---: | :---: |
| **Fire System** | Hạt lửa cơ bản, dập tắt theo collider | Hạt lửa gắn trigger diện tích | 📥 Port logic dập lửa | 🔄 Rewrite URP Particle & Tốc độ cháy lan |
| **Extinguisher** | 6DOF rút chốt, bóp cò, phân loại bình A/B/C/D | Bình xịt cơ bản | 📥 Lấy logic phân loại | 🔄 Rewrite trên XRI 3.4.1 GrabInteractable |
| **Evacuation** | Lối thoát tĩnh | Đo đạc lộ trình thoát hiểm ngắn nhất | 📥 Port thuật toán đo đường | 🔄 Rewrite hỗ trợ khói che khuất tầm nhìn |
| **Player Interaction** | SteamVR Actions cũ | Custom XR Input | ❌ Discard toàn bộ | 🚀 Xây mới 100% trên Unity OpenXR hiện đại |
| **Haptics** | Rung tay cầm HTC Vive | Rung phản hồi cơ bản | 📥 Port ý tưởng rung | 🔄 Chuẩn hóa Haptic Impulse của XRI 3.4 |
| **Scoring** | Điểm số dập lửa | Điểm theo thời gian & khoảng cách | 📥 Lấy công thức nền | 🚀 Nâng cấp thành Ma trận đánh giá EdTech |
| **AI Layer** | Không có | Thuật toán đánh giá tĩnh | 📥 Lấy tiêu chí đánh giá | 🚀 Xây mới AI Director & Socratic Tutor |
| **Telemetry** | Không lưu trữ | Lưu nặc danh cơ bản | 📥 Lấy cấu trúc mốc sự kiện | 🚀 Chuẩn hóa JSON Schema & Web Dashboard |
| **Unity Version** | 2020.2.1f1 | 2019 / 2020 | ❌ Discard phiên bản cũ | 🚀 Master Project chạy **Unity 6 (6000.3)** |
| **XR Framework** | SteamVR Legacy | Legacy XR | ❌ Discard SDK cũ | 🚀 Dùng chuẩn **XRI 3.4.1 + OpenXR** |
| **3D Assets** | Cũ / Lỗi shader | Mesh VIT trường học cũ | ❌ Discard mesh cũ | 💎 Dùng kho Asset URP Synty + Lab Designer |
