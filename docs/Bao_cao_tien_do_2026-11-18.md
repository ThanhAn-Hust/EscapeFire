# Báo cáo tiến độ EscapeFire — 18/11/2026

Lê Văn Thanh An — 20236018. Đồ án III: mô phỏng VR diễn tập PCCC có AI thích nghi.

Báo cáo gồm ba phần:
1. Đã cải tiến những gì kể từ các buổi góp ý 8/10 và 12/11.
2. Câu hỏi 1, về EdTech: làm sao để học được kỹ năng PCCC.
3. Câu hỏi 2, về AI Edge (Gemma 4B dựng trên laptop): dùng để làm gì, và em cần nó làm gì.

Mọi tính năng dưới đây đều có test tự động chạy hoàn toàn bằng input VR giả lập: tay cầm, đầu, ngón tay đi qua XR Interaction Toolkit thật, không gọi tắt hàm trong game. Trạng thái hiện tại: EditMode 150/150, PlayMode 76/76 (test hiệu năng chỉ chạy khi có card đồ họa), server 85/85. Video kiểm chứng sẽ quay tối nay. **Chưa kiểm chứng trên kính thật**, vì kính Quest 2 đang hỏng.

---

## 1. Đã cải tiến những gì

### 1.1 Đối chiếu với góp ý của thầy

| Góp ý | Đã làm | Kiểm chứng |
| :--- | :--- | :--- |
| 8/10: tương tác còn "cơ bản, chưa có gì mới"; cần làm đúng quy trình | Tay người co ngón, chỉ cầm được vật ở gần (không còn "tia laser" bắt đồ); nút báo cháy phải ấn bằng ngón tay sâu khoảng 9 mm; bình chữa cháy: nhấc khỏi giá, rút chốt bằng hai tay, cầm loa, bóp cò analog, đo đúng thao tác PASS; tủ điện: mở cửa rồi gạt cần hết hành trình; cửa: áp mu bàn tay thử nóng ít nhất 1 giây, xoay tay nắm rồi mới đẩy; dìu người yếu bằng cách đỡ nách | V0: cả ván chơi chỉ bằng input VR được 100/100 |
| 8/10: kịch bản phải đúng luật, có nguồn | 12 luật PCCC (R01–R12), mỗi luật có nguồn (Luật 55/2024/QH15, TCVN, NFPA, OSHA…) trong `docs/pccc_ground_truth.md`; bỏ các điểm "cho không"; AAR hiện nguồn của từng lỗi | V1: test không cho phép luật "mồ côi" (luật không gắn với thao tác nào) |
| 8/10: muốn "wow", tình huống mới lạ | Tòa nhà nhiều tầng: thoát bằng cầu thang bộ qua cửa chống cháy, thang máy là bẫy, có điểm tập kết; cứu nạn: khăn ướt che mũi, gọi 114 đủ 4 ý, kéo nạn nhân bất tỉnh (kỹ thuật Rautek), giữ người hoảng loạn không cho vào thang máy, báo cáo điểm danh | V2–V3: cả ván xuống 2 tầng tới điểm tập kết được 100/100 |
| 8/10: dùng AI để sinh môi trường mới | Giáo viên gõ mô tả → Gemma sinh `ScenarioSpec` → kiểm duyệt 2 tầng → dựng phòng thật bằng asset; AI ghép thử thách từ 7 "mảnh" có tham số (Challenge Composer) | V4: 5 phòng mẫu dựng được và chơi được (thư viện, căng tin, văn phòng, xưởng, lab) |
| 12/11: điểm số là công cụ, không phải mục đích; quan trọng là còn lại bao nhiêu trong đầu | Mô hình kỹ năng: mỗi buổi ghi ĐẠT/CHƯA cho từng luật, ước lượng mức thành thạo bằng BKT; vá 4 lỗ hổng chấm điểm (ví dụ báo động muộn vẫn đủ điểm); giao thức đo ghi nhớ (trước / sau / 1 tuần) và chuyển giao (phòng mới chưa gặp) | VL: `docs/evaluation_protocol.md` chờ thầy duyệt |
| 12/11: kiến thức còn hổng thì nhắc lại dồn dập, thỉnh thoảng nhắc lại | AI ưu tiên thử thách luyện đúng kỹ năng còn yếu (`weak_skills`); CF vẫn quyết định *khi nào* và *mức nào* (giữ theo quyết định của em) | VL: có chỉ số "nhắm đúng kỹ năng yếu" trong bộ đánh giá |
| 12/11: gặp nhóm game design về độ khó / flow | Đã chuẩn bị câu hỏi (`docs/vl_game_design_meeting.md`) | **Chưa gặp** |

### 1.2 Cải tiến tuần này: "game chạy ngon" và đồ họa

- **Chơi trọn luồng không kẹt (V5.1):** Menu → Bài giảng → Menu → Diễn tập → Đánh giá (AAR) → Menu → Phòng sinh bởi AI → AAR → Làm lại. Mọi nút đều bấm được bằng đầu ngón tay, qua 7 lần chuyển scene thật. Test tự động phát hiện và em đã sửa các lỗi thật:
  - người chơi xuất hiện quay lưng với menu (lệch 130°);
  - bàn chắn trước bảng kết quả;
  - font thiếu ký tự;
  - chữ đè lên nút;
  - nhiều bộ quản lý tương tác XR cùng lúc.
- **Lỗi đã biết (V5.2):** HUD cổ tay chồng chữ; khói hành lang là khối đen; nhân vật biến mất rồi "hiện" ở điểm tập kết; hành lang có khói không tối; giá sách trống.
- **Ổn định (V5.3):** chơi 3 ván liên tiếp không rò sự kiện, không nhân đôi đối tượng, console sạch lỗi.
- **Hiệu năng Quest (V5.4):**
  - cấu hình Quest trước đây tắt hẳn đèn phụ, nên đèn cầu thang, đèn EXIT và ánh lửa không chiếu sáng gì; em đã bật lại;
  - static batching và occlusion culling: góc nhìn sân giảm từ 385 xuống 83 draw call, từ 44k xuống 18k tam giác (`docs/v5_performance_comfort.md`).
- **Thoải mái VR (V5.5):** có vignette và snap turn ở cả 5 scene; đi cầu thang không giật (tối đa 2,8 cm mỗi khung hình). Sửa lỗi nút hiệu chỉnh chiều cao không lưu.
- **Đồ họa (V6):**
  - trần tấm có đèn panel, sprinkler, đầu báo khói;
  - khung cửa thép, sơn chân tường;
  - biển PCCC tiếng Việt (LỐI THOÁT phát sáng, sơ đồ thoát nạn tầng 3, cấm dùng thang máy khi cháy, điểm tập kết);
  - cầu thang có lan can; mặt tiền có cửa sổ; sân có cây, ghế, cột đèn;
  - khi mất điện, đèn trần tắt và chỉ còn đèn sự cố.
  - Gameplay không đổi. Em cố ý không đặt bình chữa cháy hay nút báo cháy giả, vì người học có thể nhầm với đồ thật.

---

## 2. Câu hỏi 1 — EdTech: làm sao để học được kỹ năng PCCC

### 2.1 "Kỹ năng PCCC" gồm ba loại, mỗi loại học một cách

| Loại | Ví dụ trong PCCC | Học bằng cách | Đo bằng |
| :--- | :--- | :--- | :--- |
| **Kiến thức** (biết) | Gọi 114; cháy điện không dùng nước; không đi thang máy khi cháy | Học có ví dụ + **tự nhớ lại** (trả lời câu hỏi) + nhắc lại giãn cách | Trắc nghiệm trước / sau / 1 tuần |
| **Thao tác** (tay làm được) | PASS (rút chốt – hướng loa – bóp cò – quét); ấn nút báo cháy; ngắt cầu dao; kéo nạn nhân | Làm đi làm lại với phản hồi ngay, đúng động tác thật | Đo trong VR: đúng thứ tự, đúng khoảng cách, đủ thời gian |
| **Quyết định dưới áp lực** (chọn đúng khi hoảng) | Chọn bình theo loại cháy; khi nào bỏ chữa cháy để thoát; giữ thấp trong khói; thử cửa nóng | Luyện trong tình huống giống thật, có căng thẳng vừa phải, rồi **phản tư** sau buổi | Hành vi trong một **phòng mới chưa gặp** (chuyển giao) |

Học PCCC chỉ bằng lý thuyết suông thì mới dừng ở loại 1. Người gặp cháy thật thất bại ở loại 2 và 3: tay lóng ngóng với bình, hoảng loạn rồi chạy vào thang máy. VR có giá trị nhất ở hai loại này, vì cho phép **luyện thao tác và quyết định trong tình huống nguy hiểm mà không nguy hiểm thật**.

### 2.2 Các nguyên lý học tập em dùng

1. **Ba giai đoạn hình thành kỹ năng** (Fitts & Posner): *hiểu* (cần hướng dẫn) → *luyện* (bớt lỗi) → *tự động* (làm được cả khi căng thẳng). Lộ trình trong game theo đúng ba giai đoạn: **Bài giảng → Có hướng dẫn → Sát hạch**, với gợi ý giảm dần (scaffolding / fading). Ở Sát hạch không còn gợi ý.
2. **Học qua trải nghiệm** (Kolb): làm → nhìn lại → rút ra quy tắc → làm lại. Trong game: diễn tập → **AAR** (mỗi lỗi kèm luật và nguồn, câu hỏi gợi mở kiểu Socratic) → nút "Làm lại".
3. **Luyện tập có chủ đích** (deliberate practice, Ericsson): luyện đúng chỗ còn yếu, có phản hồi ngay, không luyện lại cái đã giỏi. Đây là câu trả lời cho góp ý *"kiến thức còn hổng thì dồn dập, thỉnh thoảng nhắc lại"*: mô hình kỹ năng biết luật nào còn CHƯA, và AI chọn thử thách luyện đúng luật đó.
4. **Tự nhớ lại và giãn cách** (retrieval practice, spacing): nhớ lại một quy tắc ngay lúc cần dùng giúp nhớ lâu hơn đọc lại. Trong game: Mentor **hỏi trước** rồi mới nhắc; trắc nghiệm lặp lại sau 1 tuần.
5. **Độ khó vừa phải**, vấn đề thầy đặt ra hôm 12/11. Độ khó không phải mục tiêu mà là *công cụ*: khó quá thì hoảng và bỏ cuộc, dễ quá thì không học được gì. Khái niệm "khó có ích" (desirable difficulties, Bjork) gợi ý: khó ở đúng kỹ năng cần luyện thì có ích, còn khó ở chỗ không liên quan (điều khiển rườm rà, giao diện rối) thì có hại. Vì vậy em tách hai việc:
   - CF (độ ma sát nhận thức, đo từ hành vi) quyết định *khi nào* và *mức nào*;
   - mô hình kỹ năng quyết định *luyện cái gì*.

   Còn mức "vừa phải" cụ thể là bao nhiêu thì em chưa biết. Câu này để hỏi nhóm game design và đo trong pilot.
6. **Luyện chịu áp lực có kiểm soát** (stress inoculation): khói xuống dần, mất điện, bình kẹt chốt… tăng từng nấc, không bao giờ chặn mọi lối thoát. Nguyên tắc "luôn còn một lối thoát" được kiểm tra bằng code.

### 2.3 Bằng chứng nghiên cứu (và giới hạn)

- Çakiroğlu & Gökoğlu (2019, *Computers & Education* 133:56–68) dùng VR để huấn luyện kỹ năng hành vi an toàn cháy cho học sinh. Kỹ năng được cải thiện và phần lớn học sinh chuyển được sang môi trường thật. Cỡ mẫu rất nhỏ: 10 em. ([tóm tắt](https://avesis.ktu.edu.tr/yayin/6bf74ded-47c2-4f61-a1ae-52c6e72a8d14/development-of-fire-safety-behavioral-skills-via-virtual-reality))
- Morélot và cộng sự (2021, *Computers & Education*): mức nhập vai giúp học **thao tác (procedural)** nhưng **không** giúp học khái niệm, và cảm giác hiện diện không tự làm tăng việc học. ([tóm tắt](https://bohrium.dp.tech/paper/arxiv/863394277277827073))
- **Hệ quả cho thiết kế:** VR mạnh ở thao tác và quyết định, yếu ở kiến thức thuần. Vì vậy EscapeFire giữ phòng bài giảng và câu hỏi tự nhớ lại cho phần kiến thức, không trông vào riêng việc nhập vai. Phần đồ họa đẹp không phải là thứ làm người ta học tốt hơn: nó chỉ giúp nhận ra lối thoát, biển báo, khói giống ngoài đời, tức là phục vụ chuyển giao.

### 2.4 Làm sao biết người học đã học được

Không dùng tổng điểm làm bằng chứng, vì điểm có thể "lách" (đã liệt kê 4 lỗ hổng và vá). Thiết kế đo nằm trong `docs/evaluation_protocol.md`:
- **Hai nhóm:** A là kịch bản cố định, B là đạo diễn AI (CF + kỹ năng yếu). Bài giảng giống nhau.
- **Ba lần đo:** trắc nghiệm theo từng luật *trước*, *ngay sau*, *sau 1 tuần* (đo ghi nhớ).
- **Bài chuyển giao:** một **phòng sinh mới chưa gặp**, chấm hành vi (chọn đúng bình, ngắt điện, giữ thấp, thử cửa, thoát đúng), không chấm tổng điểm.
- **Thêm:** bảng hỏi căng thẳng, cảm giác hiện diện, say VR, để phát hiện tác hại của độ khó.

Giới hạn: pilot chỉ 6–10 người mỗi nhóm, nên kết quả chỉ là xu hướng. Tham số BKT đang là giả định, cần hiệu chỉnh từ dữ liệu.

---

## 3. Câu hỏi 2 — AI Edge (Gemma 4B): phục vụ mục đích gì

### 3.1 Cụ thể là gì

- **Mô hình:** **Gemma 3 4B** (`gemma3:4b`) của Google, mã nguồn mở trọng số, khoảng 4 tỉ tham số, chạy bằng **Ollama trên laptop** cùng mạng Wi-Fi với kính.
- **"Edge"** nghĩa là AI chạy *cạnh* người học (máy trong phòng tập), không gửi lên cloud.
- **Luồng hoạt động:** kính gửi telemetry mỗi 10 giây → server Python (`Server/escapefire_edge`) → trả về một quyết định đã được kiểm duyệt.
- **Lý do chọn chạy tại chỗ:**
  - **Riêng tư:** dữ liệu học viên không rời phòng tập.
  - **Không cần Internet:** phòng tập có thể không có mạng.
  - **Độ trễ thấp:** đo được trung vị 2,3 giây khi model đã nạp sẵn.
  - **Không tốn phí API.**
  - Quest 2 quá yếu để tự chạy LLM, nên cần một laptop đi kèm.
- **Khi server tắt hoặc chậm:** kính tự chuyển sang bộ điều khiển cục bộ (Micro-AI Governor) và vẫn chơi được.

### 3.2 Em cần AI làm gì, và không làm gì

Nguyên tắc: **AI chỉ làm những việc mà luật cứng làm kém, và mọi đầu ra đều bị kiểm duyệt.**

| Việc | Vì sao cần AI (luật cứng làm kém) | Đầu vào → đầu ra | Kiểm duyệt / dự phòng |
| :--- | :--- | :--- | :--- |
| **1. Đạo diễn thích nghi** (Challenge Composer) | Số tổ hợp tình huống lớn (khói × mất điện × thiết bị hỏng × hành vi NPC…); if-else chỉ lặp lại vài kịch bản | CF, vùng đang đứng, thiết bị đang cầm, **kỹ năng còn yếu**, thử thách đã dùng → `ChallengeSpec` (1–3 mảnh có tham số) | Tầng 1: schema + whitelist + tham số + luật PCCC tham chiếu (server và kính dùng chung mã lỗi, 19 ca test chung). Tầng 2: trên cảnh thật (luôn còn một buồng thang, không chặn chỗ người học đứng, ngân sách FPS). Sai thì giữ nguyên (MAINTAIN) |
| **2. Sinh phòng từ mô tả** của giáo viên | Mỗi lần đánh giá chuyển giao cần một phòng *chưa gặp*; vẽ tay từng phòng thì không mở rộng được | "Thư viện tầng 2, cháy giá sách, 1 người kẹt…" → `ScenarioSpec` → bộ xếp phòng dựng cảnh bằng asset | Validator (13 ca chung), kiểm tra có đường thoát và đủ chỗ cho người, cho sửa 1 lần theo lỗi, không được thì dùng mẫu dự phòng |
| **3. Nhận xét sau buổi** (AAR debrief) | Viết lại bảng điểm thành câu hỏi gợi mở hợp ngữ cảnh | Bảng điểm + lỗi + luật → đoạn phản hồi Socratic ≤ 900 ký tự | Lỗi, quá giờ hoặc trả về rác thì dùng mẫu soạn sẵn |
| ~~Chấm điểm~~ | **Không giao cho AI:** chấm điểm phải đúng luật và lặp lại được | — | Chấm bằng luật xác định (R01–R12) |
| ~~Quyết định an toàn~~ | **Không giao cho AI:** không bao giờ được chặn hết lối thoát hay gây say VR | — | Bất biến an toàn cài cứng trong code |

### 3.3 Số liệu đã đo

| Lần đo | Kết quả |
| :--- | :--- |
| 05/11 (benchmark đầu, chưa tối ưu) | Nạp model lần đầu 25 giây; trung vị 10,4 giây mỗi quyết định; 18/18 JSON hợp lệ nhưng chỉ 12/18 qua kiểm duyệt (lặp thử thách, vượt cấp độ, ngoài whitelist) |
| 11/11 (sau khi sửa: giữ model nạp sẵn, prompt gọn, chặn lặp) | 89 lần gọi LLM: trung vị **2,3 giây**, p90 2,8 giây; **83 %** quyết định qua kiểm duyệt, 17 % bị chặn và giữ nguyên |
| Lỗi còn thấy | Phần nhận xét AAR của Gemma 4B đôi khi trích sai luật hoặc khen tiêu chí yếu, nên vẫn phải giữ kiểm duyệt và mẫu dự phòng |

**Chưa có:** so sánh ba chế độ đạo diễn trên Gemma thật: `rules` (if-else) / `enum` (AI chọn 1 trong 7) / `composer` (AI ghép mảnh). Script đã sẵn (`Server/bench/director_ablation.py`). Kết quả này mới trả lời được câu "**AI có hơn if-else không**".

### 3.4 Câu trả lời ngắn cho câu hỏi của thầy

> Em dùng Gemma 3 4B chạy tại chỗ cho ba việc mà luật cứng làm kém: **(1)** chọn và ghép tình huống khó nhắm vào kỹ năng PCCC người học còn yếu; **(2)** sinh phòng mới từ mô tả của giáo viên để kiểm tra người học có chuyển giao được kỹ năng sang chỗ chưa gặp không; **(3)** viết nhận xét gợi mở sau buổi. AI **không** chấm điểm và **không** quyết định an toàn: mọi đầu ra đi qua hai tầng kiểm duyệt, lỗi thì quay về luật cứng. Lý do chạy edge: riêng tư, không cần Internet, độ trễ khoảng 2 giây, không tốn phí. Điều em còn phải chứng minh: so với kịch bản cố định, đạo diễn AI có giúp **nhớ lâu hơn và làm đúng hơn ở phòng mới** không. Đó là pilot theo giao thức đánh giá.

---

## 4. Việc tiếp theo và cần thầy cho ý kiến

1. **Thầy duyệt giao thức đánh giá:** bảng hành vi chấm bài chuyển giao đã đủ chưa, cỡ mẫu pilot bao nhiêu.
2. **Gặp nhóm game design** về độ khó "vừa phải" và cách đo flow.
3. **Chạy benchmark Gemma thật** cho ba chế độ đạo diễn và cho việc sinh phòng.
4. **Pilot** khi kính sửa xong; đo frame time thật trên Quest.
5. **Video kiểm chứng toàn luồng** (tối nay).
