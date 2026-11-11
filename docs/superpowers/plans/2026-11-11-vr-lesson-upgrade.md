# EscapeFire — Kế hoạch nâng cấp bài giảng VR (2026-11-11)

> Đầu vào: góp ý của người dùng sau video demo 11/11 (NPC, khói, CO2, cửa, tường, cầu thang, "phải đúng trên kính VR"),
> tài liệu tham khảo trên mạng (mục 3), và trạng thái code nhánh `feature/m0-foundation` (commit `79bbbfb`).
> Thiếu: file góp ý `transcript_output.txt` (chưa có trên máy; chỉ có `Feedback 8.10.m4a`) — bổ sung vào mục 2 khi có.

---

## 1. Sự thật hiện tại (đã kiểm tra trong code)

**Video demo KHÔNG phải giả lập VR.** `DemoRecording` gọi thẳng hàm game (`PullSafetyPin`, `TrySpray`, `RescueNPC`, `OpenDoor`…).
Nó chứng minh logic + hình ảnh, chưa chứng minh thao tác tay trên kính.

Thao tác đã nối với XR (Quest 2 qua Link hoặc XR Interaction Simulator):

| Thao tác | Nối XR | Ghi chú |
| :--- | :--- | :--- |
| Cầm bình (Grip), xịt (Trigger) | ✅ `ExtinguisherXRBinding` | haptic khi xịt / điện giật |
| Rút chốt an toàn | ❌ **Exam mode không rút được** | chỉ Guided tự rút ở lần bóp cò đầu; chốt chưa có interactable → người chơi thật không xịt được trong Sát hạch |
| Bấm chuông, gạt Aptomat | ✅ `XRSimpleInteractable.selectEntered` | |
| Kiểm tra tay nắm cửa | ✅ select → `CheckDoorHeatWithHand` | |
| Mở cửa | ⚠️ chỉ khi collider tên "Hand/Controller" chạm lần 2 | chưa nối select/poke |
| Cứu Hoa | ✅ select → `RescueNPC` (đi theo `Camera.main`) | thao tác "chọn" từ xa bằng tia, chưa phải đỡ/dìu |
| Khom người | ✅ chiều cao đầu thật (H₀ hiệu chuẩn) | |
| Báo cháy bằng giọng | ⚠️ cần STT; Editor dùng phím V | |
| Di chuyển | XRI locomotion mặc định | chưa có cầu thang, chưa kiểm tra say VR |

## 2. Góp ý cần xử lý

Từ người dùng (11/11): NPC trượt không bước (đã sửa bằng ProceduralWalk), khói (đã thêm plume + fog), CO2 (đã có luồng phun),
cứu Hoa (đã đổi sang dìu theo), cửa (đã có bản lề), tường (đã có kính + trần) — **nhưng tất cả mới kiểm bằng ảnh, chưa trên kính**.
Mới: **chưa có lên/xuống cầu thang**; **phải đúng trên kính VR**; **làm bài giảng "xịn" hơn**.

`transcript_output.txt` (góp ý tháng trước): _chưa có — chèn tóm tắt vào đây trước khi làm M1._

## 3. Tham khảo (rút ra yêu cầu thiết kế)

- Tổng quan serious game sơ tán VR: tác nhân ảo ảnh hưởng mạnh tới lựa chọn lối thoát → NPC "chạy sai hướng" là thử thách sư phạm tốt. ([arXiv 1805.09138](https://arxiv.org/pdf/1805.09138))
- Mô phỏng sơ tán Unity (Xamk): người dùng **say VR nhiều nhất khi đi cầu thang** → cần teleport/snap-turn, tốc độ chậm, có tay vịn. ([Theseus](https://www.theseus.fi/handle/10024/894298))
- So sánh thí nghiệm thật vs HMD: định hướng không gian và cách di chuyển làm lệch dữ liệu → đo thời gian sơ tán phải ghi rõ kiểu locomotion. ([Fire Technology](https://link.springer.com/article/10.1007/s10694-021-01172-4))
- Đa giác quan (nhiệt, mùi khói) tăng hiệu quả → trên Quest: âm thanh không gian + haptic + nhiễu tầm nhìn thay thế. ([Safety Solutions](https://www.safetysolutions.net.au/content/fire/article/-multi-sensory-virtual-environments-offer-safety-benefits-537833656))
- Bình chữa cháy VR thương mại dạy **PASS** (Pull–Aim–Squeeze–Sweep), bóp cò mới xả, có tab "kỹ thuật". ([FLAIM manual](https://manuals.plus/flaim/pass-extinguisher-manual), [PixoVR](https://pixovr.com/vr-training-content/fire-suppression-freerangexr/))
- Cứu người bất tỉnh trong khói: kéo sát sàn (blanket drag / leg lock), **giữ đầu nạn nhân thấp**, xuống cầu thang bằng chính kiểu kéo đó, bảo vệ đầu cổ. ([Iowa FF – leg lock](https://sites.google.com/view/iowafirefightertraining/training-articles/truck-company/leg-lock), [Fire Engineering – civilian rescue](https://www.fireengineering.com/firefighting/civilian-rescue-removing-the-occupant/), [Miami-Dade victim removal](https://mdsceh.miamidade.gov/mobi/moms/section%2006%20-%20victim%20removal%20techniques.pdf))
- Nhà cao tầng: **không dùng thang máy** (NFPA 101 §7.14). ([TDI Texas](https://Www.Tdi.Texas.Gov/pubs/videoresource/qahighrisebldge.pdf))
- Hướng dẫn PCCC Việt Nam: đi cầu thang bộ theo biển EXIT, **tay tựa tường/lan can — thấy nóng thì quay lại**, **khăn ướt che mũi miệng**, cúi thấp/bò, không xông qua khói dày, kẹt thì đóng cửa + bịt khe, **gọi 114**, không nhảy từ tầng cao. ([SGGP – thoát nạn cao ốc](https://www.sggp.org.vn/thoat-nan-khi-chay-cao-oc-post471983.html), [SGGP – chung cư](https://www.sggp.org.vn/bo-tui-ngay-ky-nang-thoat-hiem-hoa-hoan-khi-sinh-song-tai-chung-cu-cao-tang-post710988.html))

## 4. Kịch bản mới (đề xuất) — "Cháy phòng Lab tầng 5"

1. **Briefing (30 s)** trong phòng Lab: nhiệm vụ, sơ đồ thoát nạn trên tường (2 cầu thang bộ, 1 thang máy).
2. **Sự cố**: chập điện → cháy tủ server (giữ nguyên chuỗi hiện có: báo động, ngắt Aptomat, PASS với CO2).
3. **Quyết định** dập hay sơ tán (FM-200, bình yếu áp…) — AI Director giữ vai trò tăng độ khó.
4. **Cứu nạn**: Hoa **còn tỉnh nhưng yếu** → dìu (đặt tay lên vai, đi khom); biến thể Exam: Hoa **bất tỉnh** → kéo sát sàn (Rautek / blanket drag), đầu thấp, tốc độ chậm.
5. **Hành lang tầng 5**: khói lan theo trần, đèn EXIT, **thang máy "mồi bẫy"** (bấm = trừ điểm + Mentor hỏi Socratic), cửa chống cháy tự đóng.
6. **Cầu thang bộ** xuống 2–3 tầng (rút gọn): cửa buồng thang chống cháy (kiểm tra tay nắm), tay vịn, chiếu nghỉ, biển số tầng; biến thể AI: khói tràn vào cầu thang A → phải quay lại dùng cầu thang B.
7. **Điểm tập kết** ngoài sân: điểm danh (Minh đã ra? Hoa?), gọi **114** qua bộ đàm (khẩu lệnh có địa chỉ), AAR.

Chấm điểm thêm: không dùng thang máy, đóng cửa chống cháy sau lưng, khăn ướt, tay chạm tường/lan can, chọn cầu thang an toàn, báo 114 đủ thông tin.

## 5. Môi trường cần dựng

- Tòa nhà rút gọn: tầng 5 (Lab + hành lang + sảnh thang máy + 2 buồng thang), các tầng dưới chỉ là buồng thang + chiếu nghỉ, tầng 1 có lối ra sân.
- Cầu thang thật bằng asset PolygonOffice (nếu có) hoặc mesh bậc + **collider dạng dốc** (đi êm, đỡ say), tay vịn, cửa chống cháy tự đóng (`ExitDoor` + hinge spring), biển EXIT/số tầng phát sáng, đèn sự cố.
- Khói theo tầng: mỗi không gian có `SmokeDensityController` riêng (lớp khói, plume, fog), khói lọt vào buồng thang khi cửa mở lâu.
- Hiệu năng Quest 2: ≤ 72 FPS, gộp vật liệu, occlusion giữa các tầng, particle budget qua `PerformanceBudgetGovernor`.

## 6. Bắt buộc đúng trên kính VR

- Mỗi thao tác phải có **interactable XR thật**: chốt bình (`XRGrabInteractable` riêng, kéo ra), bóp cò (activate), cửa (poke/grab tay nắm + đẩy), thang máy (poke nút), dìu Hoa (grab vai, NPC đi theo tay), kéo Hoa bất tỉnh (grab hai nách, kéo lùi), khăn ướt (grab → đưa lên mặt), tay vịn (chạm).
- Di chuyển: continuous move tốc độ thấp + **teleport**, snap turn; cầu thang dùng dốc + vignette giảm say.
- **Kiểm chứng tự động bằng input giả lập XRI**, không gọi hàm game trực tiếp: PlayMode test dùng XR Interaction Simulator / thiết bị Input System giả để Grip/Trigger/Poke vào đúng interactable và assert kết quả. Bản demo video mới phải chạy bằng **cùng đường input đó**.
- Checklist chơi thật trên Quest 2 (qua Link): người dùng chơi 3 ván, ghi lại thao tác nào khó/sai.

## 7. Lộ trình (mỗi mốc = commit + test + ảnh)

| Mốc | Nội dung | Xong khi |
| :-: | :--- | :--- |
| V0 | Vá lỗ hổng XR: chốt bình grab-able, cửa mở bằng select/poke, dìu Hoa bằng grab | PlayMode test bằng input XR giả: rút chốt → xịt → dập lửa; mở cửa; dìu Hoa |
| V1 | Harness test XR (simulated controllers) + chuyển `DemoRecording` sang input XR | demo chạy hết ván không gọi hàm game trực tiếp |
| V2 | Tòa nhà rút gọn: hành lang tầng 5, sảnh thang máy, 2 buồng thang, 2 tầng dưới, sân tập kết | audit 0 lỗi, ảnh chụp từng khu |
| V3 | Kịch bản mới: thang máy mồi bẫy, cửa chống cháy tự đóng, khăn ướt, tay vịn, 114, điểm danh; Hoa tỉnh/bất tỉnh | chấm điểm mới trong AAR + test playthrough từng nhánh |
| V4 | AI Director thêm thử thách tầng/cầu thang (khói vào cầu thang A, cửa kẹt) + validator | test server + Unity |
| V5 | Locomotion/say VR, hiệu năng Quest 2 (FPS), âm thanh không gian | `FrameTimeMonitor` ≥ 72 FPS trên kính |
| V6 | Quay demo + ảnh báo cáo tuần bằng đường input XR | video + timeline |

---

## 8. Prompt giao việc (dán vào phiên mới)

```
Dự án EscapeFire (Unity 6000.3.15f1, URP, XRI 3.4.1, Quest 2), repo E:\HUST_2\20261\ProjectIII\EscapeFire, nhánh feature/m0-foundation.
Đọc trước: docs/superpowers/plans/2026-11-05-status-and-next-steps.md và docs/superpowers/plans/2026-11-11-vr-lesson-upgrade.md (kế hoạch này).
Trả lời người dùng bằng tiếng Việt.

Mục tiêu: nâng cấp bài giảng PCCC VR theo mục 4–7 của kế hoạch, làm tuần tự các mốc V0 → V6, mỗi mốc commit riêng và push.
Yêu cầu cứng:
1. Mọi thao tác của người học phải chạy được trên kính VR thật bằng XR Interaction Toolkit (grab/activate/poke/select).
   Không được "giả" bằng cách gọi thẳng hàm game; test và video demo phải đi qua input XR giả lập (XR Interaction Simulator
   hoặc thiết bị Input System giả trong PlayMode test). Nói rõ chỗ nào chưa kiểm được trên kính thật.
2. Nội dung PCCC theo mục 3 (PASS, không dùng thang máy, cầu thang bộ theo EXIT, tay tựa tường/lan can, khăn ướt, cúi thấp,
   đóng cửa chống cháy, gọi 114, cứu người: dìu khi tỉnh / kéo sát sàn khi bất tỉnh, giữ đầu thấp). Trích nguồn trong code/AAR khi cần.
3. Dựng cảnh bằng editor tool idempotent trong Assets/Editor/EscapeFire (giống LabSceneDresser), không sửa tay scene YAML.
   Ưu tiên asset sẵn có (PolygonOffice, Real Fire & Smoke); không tải asset mới khi chưa hỏi người dùng.
4. Quest 2: giữ ≥ 72 FPS, cầu thang dùng collider dốc + teleport/vignette để giảm say VR.
5. Kiểm chứng sau mỗi mốc: bash Tools/verify.sh --unity (đóng Unity Editor trước; batchmode không -nographics cho ảnh),
   chụp ảnh từng khu bằng PlayMode test (Logs/Demo/<time>_shots) và tự xem ảnh. Không quay video trừ khi được yêu cầu.
6. AI thật (Ollama gemma3:4b) chỉ chạy khi người dùng bật; server: cd Server && uv run python -m escapefire_edge.app --llm-timeout 25 --debrief-timeout 45.
7. Không merge vào develop/server; không động tới "Feedback 8.10.m4a" và Server/tools/.
Bắt đầu bằng V0 (vá lỗ hổng XR ở mục 1 của kế hoạch). Nếu thiếu thông tin góp ý (transcript_output.txt) thì hỏi người dùng trước khi làm V3.
```
