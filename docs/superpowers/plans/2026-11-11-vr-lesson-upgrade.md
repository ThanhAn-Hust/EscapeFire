# EscapeFire — Kế hoạch nâng cấp bài giảng VR, V0 → V6 (2026-11-11)

**Nhánh:** `feature/vr-lesson-upgrade` (tách từ `feature/m0-foundation` @ `f02af00`). Mỗi mốc = 1+ commit, test xanh, ảnh kiểm chứng, push.
**Đầu vào:**
- Góp ý GVHD 08/10 — [`docs/Feedback_8.10_Transcript.md`](../../Feedback_8.10_Transcript.md)
- Góp ý người dùng sau video demo 11/11 (NPC, khói, CO2, cửa, tường, cầu thang, "phải đúng trên kính VR")
- Tham khảo trên mạng (mục 3)
- Code hiện tại (mục 1)

**Ràng buộc:** kính Quest 2 của người dùng đang trục trặc → mọi kiểm chứng phải chạy được **không cần kính** (mục 2).

---

## 0. Góp ý của giảng viên → yêu cầu thiết kế

| Ý của GVHD (transcript) | Yêu cầu rút ra | Mốc |
| :--- | :--- | :-: |
| "làm theo cái đấy mà nó sai… phải tìm hiểu thực tế chữa cháy phải thực sự làm gì" [00:00] | Mọi bước và điểm số phải **đối chiếu nguồn PCCC** (bảng chân lý có trích dẫn), không tự nghĩ ra | V1 |
| "bài giảng… từng bước, từ dễ đến khó… bài giảng trước, sau đó là thực hành" [00:47] | Chuỗi học: **Bài giảng tương tác → Thực hành có hướng dẫn → Sát hạch → AAR**, độ khó tăng dần | V1 |
| "tương tác VR… thao tác cơ bản, chưa có gì mới… ghép model" [03:05] | Cần **điểm mới kỹ thuật**, không chỉ ghép asset | V4 |
| "prompt như thế nào… sinh ra các lớp học khác nhau, môi trường học khác nhau… prompt đồ đạc, object filler" [03:50, 07:08] | **Prompt → kịch bản + môi trường**: LLM sinh đặc tả phòng học/đồ đạc/mối nguy, bộ sinh dựng cảnh, validator đảm bảo an toàn | V4 |
| "chiều sâu, một kỹ thuật nào đấy… mô-đun hóa… tích hợp vào hệ thống chung" [06:28, 12:09] | Tách **gói UPM độc lập** có API, tài liệu, sample, để nhóm dùng lại | V5 |
| "wow factor"; "show hàng trong 15 phút" [09:07, 10:16] | Hình ảnh ấn tượng + **kịch bản trình diễn 15 phút** đã tập trước | V6 |

## 1. Sự thật hiện tại (đã kiểm tra trong code)

Video demo 11/11 **không phải giả lập VR**: `DemoRecording` gọi thẳng hàm game. Chưa chứng minh được thao tác tay trên kính.

| Thao tác | Nối XR | Vấn đề |
| :--- | :--- | :--- |
| Cầm bình (Grip), xịt (Trigger) | ✅ `ExtinguisherXRBinding` | |
| Rút chốt an toàn | ❌ | Exam mode **không rút được** (chốt không có interactable; chỉ Guided tự rút) → không xịt được |
| Bấm chuông, gạt Aptomat, kiểm tra tay nắm | ✅ `XRSimpleInteractable` (select) | gạt Aptomat chỉ là "chọn", chưa phải kéo cần gạt |
| Mở cửa | ⚠️ | chỉ khi collider tên "Hand" chạm lần 2, chưa nối select/poke |
| Cứu Hoa | ⚠️ | "chọn" từ xa bằng tia; chưa đỡ/dìu/kéo bằng tay |
| Báo cháy bằng giọng | ⚠️ | cần STT, Editor dùng phím V |
| Di chuyển, cầu thang | ⚠️ | locomotion mặc định; chưa có cầu thang; chưa xử lý say VR |

## 2. Giả lập VR khi không có kính

Có ba cách, dùng kết hợp:

1. **XR Interaction Simulator (đã có trong project, XRI 3.4.1)** — người dùng tự chơi trong Editor bằng bàn phím/chuột, điều khiển đầu + 2 tay cầm (Grip/Trigger/Poke).
   `HeadsetPlayModeGuard` đã tự bật nó khi không cắm kính. V0 sẽ viết hướng dẫn phím tắt cho từng thao tác.
   **Không kiểm được:** cảm giác tay thật, haptic, say VR, chiều cao thật.
2. **Kiểm thử tự động qua input XR giả lập** (Claude chạy được ở batchmode). PlayMode test điều khiển controller/HMD giả của XRI
   (simulator hoặc thiết bị Input System giả) để Grip/Trigger/Poke vào **đúng interactable**, assert kết quả, chụp ảnh/quay video.
   Thay cho việc gọi hàm trực tiếp. Video báo cáo tuần sẽ quay bằng đường này.
3. **Meta XR Simulator** (giả lập runtime Quest qua OpenXR) — tuỳ chọn khi cần giống kính hơn; phải tải từ Meta → **hỏi người dùng trước**.

Kiểm chứng cuối trên Quest 2 thật để dành khi kính sửa xong (checklist ở V6).

## 3. Tham khảo

- Tổng quan serious game sơ tán VR: tác nhân ảo ảnh hưởng lựa chọn lối thoát. ([arXiv 1805.09138](https://arxiv.org/pdf/1805.09138))
- Mô phỏng sơ tán Unity: **say VR nặng nhất khi đi cầu thang**. ([Theseus/Xamk](https://www.theseus.fi/handle/10024/894298))
- VR vs thí nghiệm thật: cách di chuyển làm lệch số liệu sơ tán. ([Fire Technology](https://link.springer.com/article/10.1007/s10694-021-01172-4))
- Đa giác quan tăng hiệu quả huấn luyện. ([Safety Solutions](https://www.safetysolutions.net.au/content/fire/article/-multi-sensory-virtual-environments-offer-safety-benefits-537833656))
- Bình chữa cháy VR dạy **PASS**, bóp cò mới xả. ([FLAIM](https://manuals.plus/flaim/pass-extinguisher-manual), [PixoVR](https://pixovr.com/vr-training-content/fire-suppression-freerangexr/))
- Cứu người bất tỉnh trong khói: kéo sát sàn, giữ đầu thấp, xuống cầu thang bằng chính kiểu kéo đó. ([Iowa FF](https://sites.google.com/view/iowafirefightertraining/training-articles/truck-company/leg-lock), [Fire Engineering](https://www.fireengineering.com/firefighting/civilian-rescue-removing-the-occupant/), [Miami-Dade](https://mdsceh.miamidade.gov/mobi/moms/section%2006%20-%20victim%20removal%20techniques.pdf))
- Nhà cao tầng: không dùng thang máy (NFPA 101 §7.14). ([TDI Texas](https://Www.Tdi.Texas.Gov/pubs/videoresource/qahighrisebldge.pdf))
- PCCC Việt Nam: cầu thang bộ theo EXIT, tay tựa tường/lan can (nóng thì quay lại), khăn ướt, cúi thấp/bò, không xông qua khói dày, kẹt thì đóng cửa + bịt khe, gọi **114**. ([SGGP cao ốc](https://www.sggp.org.vn/thoat-nan-khi-chay-cao-oc-post471983.html), [SGGP chung cư](https://www.sggp.org.vn/bo-tui-ngay-ky-nang-thoat-hiem-hoa-hoan-khi-sinh-song-tai-chung-cu-cao-tang-post710988.html))
- Cần bổ sung ở V1: văn bản chính thức (Luật PCCC & CNCH 2024, QCVN 06:2022/BXD, NFPA 10 về bình chữa cháy).

---

## V0 — Thao tác đúng trên kính + kiểm chứng không cần kính

**Mục tiêu:** mọi thao tác hiện có làm được bằng tay cầm VR thật; có harness test đi qua input XR giả lập.

Việc cần làm:
1. **Chốt an toàn**: chốt là `XRGrabInteractable` riêng; kéo ra quá ~8 cm → `PullSafetyPin()`; kẹt (AI) cần 2 lần kéo. Hiện mô hình chốt + vòng kéo.
2. **Cần gạt Aptomat**: lever kéo xuống (XRI lever hoặc grab ràng buộc trục) thay cho "chọn".
3. **Cửa**: grab tay nắm → kiểm tra nhiệt bằng mu bàn tay (poke) → đẩy cánh theo bản lề (`ExitDoor` nhận lực từ tay, tự đóng nếu là cửa chống cháy).
4. **Cứu Hoa**: grab vai → Hoa đứng dậy, đi theo tay đang nắm (dìu); thả tay → Hoa dừng.
5. **Harness `XRInputDriver`** (PlayMode, chỉ dùng cho test): đặt tư thế HMD/tay, nhấn Grip/Trigger/Poke qua XR Interaction Simulator hoặc thiết bị giả.
   Viết lại `DemoShots`/`DemoRecording` để **không gọi hàm game trực tiếp**.
6. Tài liệu `docs/VR_SIMULATOR_CONTROLS.md`: phím tắt simulator cho từng thao tác, để người dùng tự chơi khi kính hỏng.

**Xong khi:**
- PlayMode test chạy **chỉ bằng input XR giả**: lấy CO2 → rút chốt → ngắt Aptomat → xịt → dập lửa; mở cửa; dìu Hoa ra cửa.
- `verify.sh --unity` xanh.
- Ảnh chụp từng thao tác.

## V1 — Bài giảng đúng nghiệp vụ, dễ → khó

**Mục tiêu:** chuỗi học có lý thuyết trước thực hành; mọi bước có nguồn.

1. **Bảng chân lý PCCC** `docs/pccc_ground_truth.md` + `PcccRules.json`:
   - mỗi bước/điểm số/thông điệp Mentor có trích dẫn (Luật PCCC & CNCH 2024, QCVN 06:2022, NFPA 10/75/101/2001, hướng dẫn Cảnh sát PCCC);
   - rà lại kịch bản hiện tại và sửa chỗ sai (ví dụ cửa Lab "nóng" đã sửa).
2. **Phòng bài giảng tương tác (Lesson Room)**, 3–5 phút. Mỗi trạm có câu hỏi kiểm tra nhanh:
   - tam giác cháy;
   - phân loại đám cháy và chọn bình;
   - PASS trên bình tập (không lửa);
   - RACE / báo cháy 114;
   - thoát nạn: cúi thấp, khăn ướt, không thang máy.
3. **Thang học tập**: Bài giảng → Thực hành Guided (có gợi ý, không phạt) → Sát hạch Exam (AI Director tăng độ khó) → AAR so sánh với ván trước.
4. Menu chọn bài theo cấp độ; mở khoá cấp sau khi đạt ngưỡng.

**Xong khi:**
- Kịch bản bài giảng chạy bằng input XR giả.
- Mỗi luật chấm điểm map tới 1 nguồn (test kiểm không có luật "mồ côi").
- AAR hiển thị nguồn khi trừ điểm.

## V2 — Tòa nhà nhiều tầng, cầu thang, thang máy mồi bẫy

**Mục tiêu:** thoát nạn trọn vẹn từ tầng 5 xuống sân.

1. Tòa nhà rút gọn:
   - tầng 5: Lab + hành lang + sảnh thang máy + 2 buồng thang bộ;
   - tầng 4–2: chỉ buồng thang + chiếu nghỉ, biển số tầng;
   - tầng 1: cửa ra sân + điểm tập kết.
2. Cầu thang:
   - bậc + **collider dốc**, tay vịn chạm được (cộng điểm "tay tựa lan can");
   - cửa buồng thang chống cháy tự đóng;
   - đèn EXIT/đèn sự cố.
3. **Thang máy mồi bẫy**: bấm nút → Mentor hỏi Socratic + trừ điểm (NFPA 101 §7.14 / hướng dẫn VN).
4. Khói theo không gian: mỗi khu một `SmokeDensityController`; khói lọt buồng thang khi cửa mở lâu (cửa chống cháy đóng → giữ khói).
5. Locomotion chống say:
   - teleport + snap turn;
   - continuous chậm có vignette;
   - tuỳ chọn "đi cầu thang bằng teleport chiếu nghỉ".
6. Dựng bằng editor tool idempotent (`BuildingBuilder`), không sửa tay YAML; ưu tiên PolygonOffice.

**Xong khi:**
- Test bằng input XR giả đi từ Lab xuống sân.
- Audit 0 lỗi.
- Ảnh từng khu.
- Đo FPS trong Editor (ngân sách cho Quest ở V6).

## V3 — Cứu nạn và kịch bản thoát nạn chuẩn

1. **Hoa tỉnh nhưng yếu**: dìu bằng tay (grab vai), cả hai khom thấp, tốc độ chậm.
2. **Hoa bất tỉnh** (Exam / AI chọn):
   - kéo sát sàn bằng hai tay (Rautek / blanket drag), đầu nạn nhân thấp;
   - kéo xuống cầu thang chậm, bảo vệ đầu cổ;
   - sai kỹ thuật (bế đứng trong khói) → Mentor nhắc.
3. **Khăn ướt**: lấy khăn, nhúng nước ở bồn rửa / chai nước, đưa lên mặt → giảm tốc độ mất oxy, cộng điểm.
4. **Gọi 114 qua bộ đàm**: khẩu lệnh phải có địa chỉ + tầng + loại cháy; STT chấm đủ ý.
5. **Điểm tập kết**: điểm danh Minh/Hoa; Minh hoảng loạn có thể chạy vào thang máy → người học phải ngăn.
6. Chấm điểm mới trong AAR (radar thêm trục "Cứu nạn" và "Thoát nạn").

**Xong khi:**
- Playthrough test cho từng nhánh: dìu, kéo, quên Hoa, dùng thang máy, đúng chuẩn.
- AAR hiển thị điểm mới có nguồn.

## V4 — Điểm mới kỹ thuật: Prompt → Kịch bản + Môi trường (đáp ứng [03:50], [07:08])

**Ý tưởng:** giáo viên gõ một câu ("cháy bếp căng-tin tầng 2, 30 sinh viên, lớp năm nhất"). LLM sinh **ScenarioSpec JSON**:
- loại phòng, kích thước, bố trí;
- đồ đạc (object filler từ catalog asset);
- mối nguy và vị trí bình chữa cháy;
- NPC và vai trò;
- mục tiêu học tập, cấp độ.

Bộ sinh dựng cảnh và kịch bản; **validator an toàn** bảo đảm cảnh hợp lệ.

1. Schema `ScenarioSpec` + catalog asset có metadata:
   - kích thước, "chặn lối đi?", "dễ cháy?", vật liệu;
   - nhóm: phòng Lab, thư viện, văn phòng, căng-tin, xưởng.
2. **Edge server**: endpoint `/scenario/generate` (Gemma local).
   - Few-shot + JSON mode + **validator tầng 1**: whitelist asset, luật PCCC (khoảng cách bình chữa cháy, lối thoát tối thiểu, loại bình đúng đám cháy).
3. **Bộ sinh trong Unity**:
   - layout theo lưới và ràng buộc;
   - đặt object filler;
   - NavMesh kiểm tra luôn có lối thoát (Life-Safety Invariant);
   - nối các module tương tác (bình, Aptomat, cửa, NPC);
   - **validator tầng 2** trên kính.
4. AI Director hiện có tự dùng ScenarioSpec để chọn thử thách hợp phòng.
5. Đánh giá (có số liệu cho báo cáo/paper):
   - tỉ lệ spec hợp lệ, thời gian sinh;
   - đa dạng cảnh;
   - % cảnh qua validator;
   - so sánh Gemma 4B vs 7B.

**Xong khi:**
- Từ 5 prompt khác nhau sinh 5 cảnh chơi được.
- Test: mọi cảnh có lối thoát, bình đúng loại, không vật chắn cửa.
- Ảnh 5 cảnh.

## V5 — Mô-đun hoá để nhóm dùng lại (đáp ứng [06:28], [12:09])

1. Tách thành gói UPM trong `Packages/`:
   - `com.escapefire.adaptive-director`: CF tracker, Micro-AI Governor, client edge, validator;
   - `com.escapefire.pccc-interactions`: bình + chốt, Aptomat, cửa chống cháy, báo cháy, NPC cứu nạn, khói/lửa;
   - `com.escapefire.scenario-gen`: ScenarioSpec, bộ sinh, catalog;
   - `com.escapefire.aar`: chấm điểm, radar, LMS report;
   - edge server Python thành package riêng.
2. Mỗi gói có:
   - asmdef riêng, không phụ thuộc vòng;
   - README + API + sample scene;
   - test riêng.
3. Hướng dẫn tích hợp vào dự án khác của nhóm: thêm gói qua git URL, 1 sample tối giản.

**Xong khi:**
- Một project Unity trống thêm từng gói, sample chạy được.
- Test của từng gói xanh.

## V6 — "Wow factor", hiệu năng Quest 2, trình diễn 15 phút

1. Hình ảnh:
   - lửa/khói Real Fire & Smoke đúng quy mô;
   - ánh sáng sự cố, đèn đỏ xoay, phản chiếu;
   - âm thanh không gian (lửa, còi, FM-200);
   - tay VR (XR Hands) thay vì tay cầm;
   - hiệu ứng nhiễu tầm nhìn khi thiếu oxy.
2. Hiệu năng Quest 2:
   - ≥ 72 FPS, < 5 % frame vượt 13.8 ms;
   - gộp vật liệu, occlusion, LOD, ngân sách particle.
3. **Kịch bản trình diễn 15 phút** `docs/defense_demo_script.md`:
   - 1' vấn đề;
   - 3' bài giảng;
   - 5' ván Sát hạch có AI tăng độ khó;
   - 2' prompt sinh cảnh mới trực tiếp;
   - 2' AAR;
   - 2' mô-đun + số liệu.
4. Video báo cáo + ảnh quay bằng input XR giả lập.
5. Checklist chơi thật trên Quest 2 khi kính sửa xong: 3 ván, ghi thao tác khó/sai, đo FPS bằng `FrameTimeMonitor`.

**Xong khi:**
- Video 3–5 phút + ảnh.
- Kịch bản 15 phút tập được.
- Số liệu FPS (Editor; Quest khi có kính).

---

## Thứ tự và quy tắc làm việc

- Làm tuần tự V0 → V6. V0 là điều kiện cho mọi mốc sau: mọi test mới đều dùng `XRInputDriver`.
- Mỗi mốc:
  - viết test trước;
  - `bash Tools/verify.sh --unity` (đóng Editor);
  - chụp ảnh `Logs/Demo/<time>_shots`;
  - tự xem ảnh;
  - commit + push lên `feature/vr-lesson-upgrade`.
- Không merge vào `develop/server` khi người dùng chưa duyệt.
- Asset mới (Meta XR Simulator, XR Hands sample, model nhân vật/animation) → hỏi người dùng trước khi tải.
- AI thật (Ollama gemma3:4b) chỉ chạy khi người dùng bật:
  `cd Server && uv run python -m escapefire_edge.app --llm-timeout 25 --debrief-timeout 45`.

## Prompt giao việc (dán vào phiên mới)

```
Dự án EscapeFire (Unity 6000.3.15f1, URP, XRI 3.4.1, Quest 2), repo E:\HUST_2\20261\ProjectIII\EscapeFire, nhánh feature/vr-lesson-upgrade.
Đọc trước: docs/superpowers/plans/2026-11-11-vr-lesson-upgrade.md (kế hoạch V0→V6), docs/Feedback_8.10_Transcript.md (góp ý GVHD),
docs/superpowers/plans/2026-11-05-status-and-next-steps.md. Trả lời người dùng bằng tiếng Việt.

Làm mốc tiếp theo chưa xong trong kế hoạch (bắt đầu V0), đúng mục "Xong khi" của mốc đó, rồi dừng lại báo cáo.
Yêu cầu cứng:
1. Thao tác người học phải chạy trên kính VR thật bằng XR Interaction Toolkit; kính đang hỏng nên kiểm chứng bằng input XR giả lập
   (XR Interaction Simulator / thiết bị giả trong PlayMode test). Không gọi thẳng hàm game trong test/demo. Nói rõ cái gì chưa kiểm được trên kính.
2. Nội dung PCCC phải có nguồn (mục 3 + bảng chân lý V1); sửa mọi bước sai nghiệp vụ.
3. Dựng cảnh bằng editor tool idempotent trong Assets/Editor/EscapeFire; không sửa tay scene YAML; hỏi trước khi tải asset mới.
4. Sau mỗi mốc: bash Tools/verify.sh --unity (đóng Unity Editor trước), chụp ảnh bằng PlayMode test (batchmode không -nographics), tự xem ảnh,
   commit + push lên feature/vr-lesson-upgrade. Không quay video trừ khi được yêu cầu. Không merge.
5. AI thật chỉ chạy khi người dùng bật Ollama.
```
