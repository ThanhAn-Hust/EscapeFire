# EscapeFire — Kế hoạch nâng cấp bài giảng VR, V0 → V6 (bản chi tiết)

**Ngày:** 2026-11-11.
**Nhánh:** `feature/vr-lesson-upgrade`, tách từ `feature/m0-foundation` @ `f02af00`.

**Đầu vào:**
- góp ý GVHD 08/10 ([transcript](../../Feedback_8.10_Transcript.md));
- góp ý GVHD 12/11 ([transcript](../../Feedback_12_11_26_Transcript.md)) → mốc **VL** (học thật), làm trước V5;
- góp ý người dùng sau demo 11/11;
- tài liệu tham khảo (Phần E);
- code hiện tại.

**Ưu tiên số 1 của người dùng:** mọi hoạt động trong VR phải **giống ngoài đời thực nhất có thể**.
Phần B là đặc tả bắt buộc cho từng thao tác; mọi mốc V0 → V6 đều phải tuân theo nó.

**Ràng buộc:** kính Quest 2 đang trục trặc. Mọi kiểm chứng chạy **không cần kính** bằng input XR giả lập (Phần C).
Kiểm tra trên kính thật để dành tới khi kính sửa xong.

Mục lục:
- A. Góp ý → yêu cầu
- B. Đặc tả thao tác "như đời thực"
- C. Kiểm chứng không cần kính
- D. Lộ trình V0 → V6 (từng phase), thêm mốc VL giữa V4 và V5
- E. Nguồn
- F. Quy tắc làm việc và prompt giao việc

---

## A. Góp ý → yêu cầu

| Nguồn | Góp ý | Yêu cầu | Mốc |
| :--- | :--- | :--- | :-: |
| GVHD [00:00] | "làm theo… mà nó sai… phải tìm hiểu thực tế chữa cháy" | Mọi bước, điểm số, câu Mentor có **nguồn PCCC**; rà lại kịch bản | V1 |
| GVHD [00:47] | "từ dễ đến khó… bài giảng trước, sau đó thực hành" | Bài giảng tương tác → Thực hành có hướng dẫn → Sát hạch → AAR | V1 |
| GVHD [03:05] | "thao tác cơ bản, chưa có gì mới" | Thao tác thật bằng tay (Phần B), kỹ thuật đo động tác (PASS sweep, kéo Rautek…) | V0, V3 |
| GVHD [03:50, 07:08] | "prompt… sinh lớp học, môi trường học, object filler" | Prompt → ScenarioSpec → sinh cảnh có validator an toàn | V4 |
| GVHD [06:28, 12:09] | "chiều sâu… mô-đun hoá… người khác dùng được" | **Bỏ** theo quyết định người dùng 12/11: không tách gói; ưu tiên game chạy ngon. Mã vẫn giữ thiết kế lỏng (EventBus, validator thuần logic) trong một assembly | — |
| Người dùng 12/11 | "đừng mô-đun hoá, quan trọng là game chạy ngon" | V5 đổi thành **game chạy ngon**: chơi trọn luồng không kẹt, sửa lỗi đã biết, ổn định, hiệu năng Quest 2, thoải mái VR | V5 |
| GVHD [09:07, 10:16] | "wow factor", "show trong 15 phút" | Hình ảnh, âm thanh, hiệu năng, kịch bản trình diễn | V6 |
| Người dùng 11/11 | demo không phải VR thật | Test/video chỉ đi qua input XR giả lập | V0 |
| Người dùng 11/11 | chưa có cầu thang | Tòa nhà nhiều tầng, cầu thang bộ, thang máy mồi bẫy | V2 |
| Người dùng 11/11 | cứu NPC chưa chuẩn | Dìu (tỉnh) / kéo Rautek (bất tỉnh), giữ đầu thấp | V3 |
| GVHD 12/11 [02:31–03:07, 08:21–08:52] | "không ai dám nói làm game khó hơn hay dễ đi thì hay hơn… phù hợp rất khó biết"; flow cũng có nghiên cứu cho là có hại | **Giữ** chỉnh độ khó theo CF (quyết định của người dùng 12/11); bổ sung để AI phục vụ **việc học**: CF quyết định *khi nào / nặng tới đâu*, mô hình kỹ năng quyết định *luyện kỹ năng nào*; báo cáo không lấy "độ khó phù hợp" làm bằng chứng học | VL.3 |
| GVHD 12/11 [03:20–03:41, 09:03–09:45] | "điểm số chỉ là công cụ… rốt cuộc còn bao nhiêu thứ đọng lại"; lớp vui, đánh giá cao nhưng kiến thức không còn | Đo **ghi nhớ và chuyển giao** (trước/sau, sau 1 tuần, phòng chưa gặp), không lấy điểm 100 làm bằng chứng | VL.4 |
| GVHD 12/11 [06:30–06:44] | AI tối ưu phần thưởng chứ không tối ưu hành vi; phần thưởng sai → hành vi sai | Bảng điểm là "phần thưởng": rà xem có thể "lách điểm" mà không biết làm không; chấm theo bằng chứng hành vi từng kỹ năng | VL.1, VL.4 |
| GVHD 12/11 [10:48–11:03] | đưa kiến thức còn hổng vào "dồn dập… thêm dần dần, thỉnh thoảng nhắc lại"; biết kiến thức nào đang hổng chứ không phải xem độ khó | Mô hình kỹ năng R01–R12 theo từng người; đạo diễn **nhắm vào kỹ năng yếu**; luyện giãn cách, xen kẽ | VL.1–VL.3, VL.5 |
| GVHD 12/11 [12:05–15:52] (bản ghi nhiễu, hiểu ~60%) | AI còn dùng được: xác định người học đang học gì; imitation learning / mô phỏng để NPC cư xử giống người | Ưu tiên AI cho chẩn đoán kỹ năng (VL); NPC có "tâm lý" bằng LLM để sau (V6, tuỳ chọn); imitation learning ngoài phạm vi (thiếu dữ liệu người thật) | VL, V6 |
| GVHD 12/11 [00:51–02:14, 08:10–08:21] | gặp nhóm game design (2 bạn) qua online, 15–30 phút | Chuẩn bị câu hỏi, ghi kết quả vào kế hoạch | VL.6 |
| Người dùng 11/11 | thao tác phải chính xác như ngoài đời | Phần B | mọi mốc |

---

## B. Đặc tả thao tác "như đời thực"

### B.0 Nguyên tắc chung (áp dụng cho mọi thao tác)

1. **Tỉ lệ 1:1 và cơ thể thật.**
   - Đồ vật kích thước thật (bình CO2 5 kg cao ~0,6–0,7 m, nút báo cháy ~0,1 m, tay nắm cửa cao ~1,0 m).
   - Tư thế (đứng/khom/bò) đo từ **chiều cao đầu thật** so với H₀ hiệu chuẩn. Không có nút "ngồi xuống".
2. **Tay làm việc của tay.**
   - Thao tác ngoài đời làm bằng tay ở cự ly gần thì chỉ làm được bằng **tay ở cự ly gần** (near / poke / grab).
   - **Tắt chọn từ xa bằng tia** cho các vật này: interaction layer `HandOnly`.
   - Tia chỉ còn cho menu và teleport.
3. **Hai tay khi đời thực cần hai tay.**
   - Bình chữa cháy: một tay xách/bóp cò, tay kia cầm loa phun.
   - Kéo nạn nhân: hai tay.
   - Rút chốt: một tay giữ thân bình, tay kia rút.
4. **Đúng thứ tự, đúng động tác.**
   - Chốt phải rút trước khi bóp được cò.
   - Cửa phải xoay tay nắm rồi mới đẩy.
   - Cần gạt phải kéo hết hành trình.
   - Không có "bấm một nút là xong".
5. **Ràng buộc vật lý.**
   - Tay không xuyên tường/vật rắn: physics hand có giới hạn, tay ảo dừng ở bề mặt.
   - Vật nặng theo tay có quán tính (XRI `VelocityTracking`).
   - Cửa quay theo bản lề; cần gạt theo trục.
6. **Thông số thực.**
   - Tầm phun, thời gian xả, tốc độ đi khom, thời gian khói hạ… lấy từ nguồn (V1 xác minh).
   - Số chưa xác minh phải ghi `// TODO(V1): verify source`.
7. **Phản hồi đa giác quan.**
   - Haptic theo lực (bóp cò rung đều, chốt kẹt rung giật, cửa nóng rung + âm "xèo").
   - Âm thanh không gian (lửa, còi, xả khí CO2).
   - Hình ảnh tay (XR Hands).
8. **Không đọc chữ để biết phải làm gì trong chế độ Sát hạch.**
   - Người học phải nhận ra từ môi trường: nhãn bình, biển EXIT, khói.
   - Gợi ý chữ chỉ có ở Bài giảng/Guided.
9. **Ngoại lệ có chủ đích (ghi rõ trong báo cáo).**
   - Di chuyển xa bằng cần analog / teleport (phòng thật có giới hạn).
   - Xuống cầu thang có thể teleport theo chiếu nghỉ để giảm say VR.
   - Không mô phỏng được: nhiệt, mùi, trọng lượng thật (thay bằng haptic + tốc độ theo tay).
10. **Hỗ trợ cả tay cầm và bắt tay (hand tracking).**
    - Grip = nắm, Trigger = bóp (dạng analog).
    - Với hand tracking: pinch/grab gesture của XR Hands.

### B.1 Catalog thao tác

Mỗi thao tác mô tả: **Ngoài đời** → **Trong VR** (input, ngưỡng) → **Phản hồi** → **Sai thì sao** → **Test giả lập**.
Ngưỡng là đề xuất ban đầu, chỉnh lại khi có kính.

#### 1. Bấm nút báo cháy (manual call point)
- **Ngoài đời:** ấn mạnh vào ô kính/nút (loại ấn vỡ kính hoặc nút nhấn), chuông kêu ngay.
- **VR:**
  - Ngón trỏ (poke) ấn vào mặt nút, **độ sâu ≥ 8 mm** (XRI `XRPokeFilter`, `PokeThresholdData`).
  - Không chọn từ xa được.
- **Phản hồi:** nắp nhựa lõm xuống, đèn đỏ sáng, chuông không gian, haptic ngắn.
- **Sai:** chạm hờ không đủ sâu → không kích hoạt. AI "đứt cáp chuông" → nút lõm nhưng im lặng, phải hô bằng giọng (thao tác 2).
- **Test:** `Poke_CallPoint_RaisesAlarm`, `Poke_Shallow_DoesNothing`.

#### 2. Hô báo cháy / gọi 114 qua bộ đàm hoặc điện thoại
- **Ngoài đời:** hô "Cháy!" để báo người xung quanh; gọi 114 nói rõ địa chỉ, tầng, đám cháy gì, có người mắc kẹt không.
- **VR:**
  - Cầm bộ đàm (grab), **đưa lên gần miệng (< 15 cm từ HMD)** và giữ nút PTT (trigger).
  - Speech-to-text chấm khẩu lệnh.
  - Gọi 114 phải đủ 4 ý: địa chỉ, tầng, loại cháy, người mắc kẹt.
- **Phản hồi:** tiếng rè bộ đàm, trực ban 114 (TTS) hỏi lại ý còn thiếu.
- **Sai:** nói khi không bấm PTT → không ai nghe; thiếu ý → bị hỏi lại, trừ điểm.
- **Test:** `Radio_NearMouth_PTT_Transcript_RaisesAlarm` (transcript giả), `Call114_MissingFloor_AsksBack`.

#### 3. Ngắt điện Aptomat
- **Ngoài đời:** mở cửa tủ điện, gạt cần Aptomat tổng xuống vị trí OFF; không chạm khi tay ướt.
- **VR:**
  - Cửa tủ điện có bản lề: grab mép cửa, kéo mở.
  - Cần gạt là vật ràng buộc theo trục (XRI lever / `XRGrabInteractable` + joint): grab cần, **kéo hết hành trình ~90°** mới OFF.
  - Thả giữa chừng → cần bật về.
- **Phản hồi:** tiếng "cạch", đèn phòng tắt (còn đèn sự cố), máy tính tắt, haptic.
- **Sai:**
  - Dùng bình nước khi chưa ngắt → điện giật (đã có).
  - Tay đang cầm khăn ướt mà gạt → Mentor nhắc (V3).
- **Test:** `Lever_FullTravel_CutsPower`, `Lever_HalfTravel_SpringsBack`.

#### 4. Chọn và lấy bình chữa cháy
- **Ngoài đời:**
  - Đọc nhãn: CO2 có loa phun đen và không có đồng hồ; bột ABC có đồng hồ áp.
  - Chọn đúng loại cho cháy điện.
  - Nhấc bình **lên rồi ra khỏi giá treo**.
- **VR:**
  - Bình trên giá: phải nâng lên ~5 cm rồi kéo ra (ràng buộc giá) mới tách khỏi giá.
  - Cầm tay xách bằng Grip.
  - Có thể cầm lên **xoay xem nhãn** (gaze check đếm là "đã kiểm tra nhãn").
- **Phản hồi:** bình nặng: theo tay có trễ nhẹ (velocity tracking), tay hạ thấp; tiếng kim loại.
- **Sai:** lấy bình nước cho cháy điện → Guided: Mentor hỏi; Exam: trừ điểm/điện giật nếu xịt khi còn điện.
- **Test:** `Extinguisher_LiftThenPull_DetachesFromBracket`, `Extinguisher_InspectLabel_Recorded`.

#### 5. PASS — Pull, Aim, Squeeze, Sweep (bình CO2)
- **Ngoài đời:**
  - **Pull:** một tay giữ thân/tay xách, tay kia xoay nhẹ và giật chốt (phá niêm chì).
  - **Aim:** cầm **tay cầm của loa phun** (không cầm trực tiếp loa: CO2 lạnh gây bỏng lạnh), chĩa vào **gốc lửa**, đứng cách ~1–2,5 m (TODO V1: xác minh tầm phun theo loại bình).
  - **Squeeze:** bóp cò; nhả là ngừng.
  - **Sweep:** quét qua lại ở gốc lửa đến khi tắt; CO2 xả hết trong khoảng ~10–20 s (TODO V1: xác minh).
- **VR:**
  - **Pull:**
    - chốt là `XRGrabInteractable` riêng, chỉ grab được khi **tay kia đang cầm bình**;
    - kéo chốt ra ≥ 6 cm theo trục chốt → rút;
    - chốt kẹt (AI) cần xoay ≥ 30° rồi kéo, hoặc 2 lần giật.
  - **Aim:**
    - loa phun có điểm grab riêng (tay cầm loa), tay thứ hai cầm loa thì hướng phun = hướng loa;
    - một tay → phun theo hướng bình, kém chính xác hơn;
    - cầm vào thân loa (vùng lạnh) → cảnh báo bỏng lạnh, trừ điểm.
  - **Squeeze:**
    - trigger analog **≥ 0,6**;
    - lưu lượng tỉ lệ độ bóp;
    - nhả → ngừng ngay.
  - **Sweep:**
    - đo góc quét ngang của loa trong cửa sổ 1 s;
    - quét ≥ 15° mỗi lượt và trúng vùng gốc lửa (cone cast vào collider "FireBase") thì dập hiệu quả 100 %;
    - phun cố định một điểm 50 %; phun vào ngọn lửa trên cao 20 %.
  - **Tầm và thời lượng:**
    - hiệu quả giảm khi > 2,5 m, bằng 0 khi > 4 m;
    - bình có lượng chất chữa cháy hữu hạn (đồng hồ thời gian xả).
- **Phản hồi:**
  - tiếng xả khí lớn;
  - luồng khí trắng nở rộng (đã có jet);
  - haptic rung đều theo độ bóp;
  - băng giá đọng ở loa;
  - lửa nhỏ dần theo hiệu quả quét.
- **Sai (mỗi lỗi ghi vào AAR với nguồn):**
  - chưa rút chốt mà bóp;
  - đứng quá gần/xa;
  - phun vào ngọn;
  - không quét;
  - hết bình mà lửa còn → phải quyết định sơ tán.
- **Test:**
  - `PASS_PinRequiresOtherHandHolding`;
  - `PASS_TriggerBelowThreshold_NoDischarge`;
  - `PASS_SweepAtBase_ExtinguishesFasterThanStatic`;
  - `PASS_AimAtFlames_Ineffective`;
  - `PASS_OutOfRange_NoEffect`;
  - `PASS_HoldColdHorn_Warns`.

#### 6. Cúi thấp, bò dưới khói
- **Ngoài đời:** không khí sạch gần sàn; đi khom hoặc bò, che mũi miệng.
- **VR:**
  - Tư thế tính từ chiều cao đầu thật so với H₀:
    - đứng > 0,8 H₀;
    - khom 0,55–0,8 H₀;
    - bò < 0,55 H₀.
  - Khi bò: di chuyển bằng **vung tay chạm sàn** (arm-swing locomotion, tuỳ chọn) hoặc analog chậm.
  - Tốc độ tối đa: đứng 1,4 m/s, khom 0,8, bò 0,4.
- **Phản hồi:**
  - đầu trong lớp khói → tầm nhìn mù (fog đã có), tiếng ho, oxy giảm nhanh;
  - dưới lớp khói → nhìn rõ hơn.
- **Sai:** đứng thẳng trong khói (đã có chấm điểm + Socratic).
- **Test:** `Posture_FromHeadHeight`, `Crawl_SpeedCapped`, `HeadInSmoke_OxygenDropsFaster`.

#### 7. Khăn ướt che mũi miệng
- **Ngoài đời:** làm ướt khăn/vải, gấp, che mũi miệng khi đi qua khói.
- **VR:**
  - Grab khăn (cloth mềm hoặc mesh skinned đơn giản).
  - Mở vòi nước: xoay tay vặn ≥ 90°.
  - Giữ khăn dưới dòng nước ≥ 2 s → ướt (đổi vật liệu, nhỏ giọt).
  - Đưa lên mặt (< 8 cm trước HMD, phía dưới mắt) và **giữ bằng một tay**.
  - Tay đó bận: không cầm được vật khác. Đây là ràng buộc thật: phải chọn giữa giữ khăn và mang bình.
- **Phản hồi:** tiếng thở qua vải, viền nhìn hơi mờ, oxy giảm chậm hơn.
- **Sai:** khăn khô hiệu quả thấp; bỏ khăn trong khói → Mentor nhắc.
- **Test:** `Towel_WetUnderTap`, `Towel_HeldToFace_ReducesOxygenLoss`, `Towel_OccupiesHand`.

#### 8. Kiểm tra cửa trước khi mở
- **Ngoài đời:**
  - Áp **mu bàn tay** lên cánh cửa (từ trên xuống) và tay nắm.
  - Nóng → không mở, tìm lối khác. Không nóng → mở hé, đứng nép bên cạnh, quan sát khói rồi mới đi.
- **VR:**
  - **Mu bàn tay**: hướng pháp tuyến lòng bàn tay (XR Hands joint / tư thế controller) quay **ra xa** bề mặt.
  - Tiếp xúc ≥ 1 s ở cánh cửa **và** tay nắm.
  - Lòng bàn tay áp vào cửa nóng → "bỏng", trừ điểm.
- **Phản hồi:** cửa nóng: haptic mạnh + "xèo" + Mentor; cửa nguội: haptic nhẹ.
- **Sai:** mở cửa khi chưa kiểm tra (đã có trừ điểm); mở cửa nóng → backdraft (V3/V4 biến thể).
- **Test:** `DoorCheck_BackOfHand_Counts`, `DoorCheck_Palm_Burns`, `DoorCheck_TooShort_NotCounted`.

#### 9. Mở cửa / cửa chống cháy / thanh thoát hiểm
- **Ngoài đời:**
  - Cửa thường: xoay tay nắm xuống rồi đẩy/kéo.
  - Cửa buồng thang chống cháy: **đẩy thanh thoát hiểm (panic bar)**, cửa có tay co thủy lực tự đóng; **không chèn cửa mở**.
- **VR:**
  - Tay nắm: grab + xoay ≥ 30° → chốt nhả.
  - Đẩy cánh: lực theo tốc độ tay khi chạm (hinge physics hoặc grab cánh).
  - Panic bar: poke/đẩy thanh ≥ 2 cm.
  - Door closer: cánh tự đóng sau ~3–5 s khi không giữ.
  - Chèn vật vào khe cửa → khói lọt (trừ điểm).
- **Phản hồi:** tiếng chốt, tiếng cửa đóng sập, khói dừng khi cửa đóng.
- **Test:** `Door_RotateHandleThenPush_Opens`, `Door_PushWithoutHandle_Stays`, `FireDoor_AutoCloses`, `PanicBar_PushOpens`.

#### 10. Dìu nạn nhân còn tỉnh (Hoa yếu)
- **Ngoài đời:**
  - Nói chuyện trấn an.
  - Choàng tay nạn nhân qua vai mình, tay mình đỡ eo/nách.
  - Cả hai đi khom, chậm, ra lối thoát.
- **VR:**
  - Ngồi xuống ngang tầm (đầu < 0,7 H₀).
  - Grab điểm **nách/vai** của Hoa, kéo lên → Hoa đứng dậy (IK/tư thế khom).
  - Hoa đi theo **vị trí tay đang đỡ** (spring joint mềm).
  - Thả tay → Hoa dừng/khuỵu.
  - Tốc độ cả hai ≤ 0,7 m/s.
  - Trấn an bằng giọng ("Bình tĩnh, đi theo mình") → Hoa hợp tác hơn (tuỳ chọn).
- **Phản hồi:** tiếng ho của Hoa, Hoa nghiêng người vào tay, haptic nhẹ khi đỡ.
- **Sai:** kéo quá nhanh → Hoa ngã; đứng thẳng trong khói khi dìu → cả hai mất oxy nhanh.
- **Test:** `Escort_GrabArmpit_LiftsVictim`, `Escort_ReleaseStops`, `Escort_TooFast_VictimFalls`.

#### 11. Kéo nạn nhân bất tỉnh (Rautek / kéo bằng chăn)
- **Ngoài đời:**
  - Kiểm tra phản ứng, gọi.
  - Đỡ nạn nhân ngồi dậy từ phía đầu, luồn hai tay dưới nách nắm cẳng tay nạn nhân (**Rautek**).
  - Kéo lùi; hoặc lăn lên chăn rồi kéo.
  - **Giữ đầu nạn nhân thấp** vì khói; xuống cầu thang bằng chính cách kéo đó, bảo vệ đầu cổ.
- **VR:**
  - Nạn nhân là ragdoll / ArticulationBody (hông, thân, đầu, tay chân), nặng.
  - Phải **grab cả hai nách bằng hai tay**; một tay → không nhấc nổi.
  - Lùi bằng analog/đi thật, tốc độ ≤ 0,5 m/s; thân nạn nhân kéo lê trên sàn.
  - Biến thể chăn: grab mép chăn hai tay.
  - Cầu thang: kéo từng bậc, đầu nạn nhân phía trên; va đầu mạnh → trừ điểm.
- **Phản hồi:** tiếng kéo lê, haptic nặng, tay bị kéo trễ (velocity tracking + trọng lượng).
- **Sai:** bế đứng/vác vai trong khói → Mentor nhắc "giữ đầu thấp"; một tay → không kéo được.
- **Test:** `Rautek_TwoHandsRequired`, `Rautek_DragKeepsHeadLow`, `Drag_DownStairs_HeadProtected`.

#### 12. Đi theo biển EXIT, tay tựa tường/lan can
- **Ngoài đời:** theo biển EXIT; trong khói, **một tay lần theo tường**; trên cầu thang **vịn lan can**; thấy nóng thì quay lại.
- **VR:**
  - Chạm tường/lan can bằng tay (collider tay chạm, tỉ lệ thời gian chạm khi di chuyển trong khói) → điểm "lần tường".
  - Lan can grab được: giữ lan can khi xuống bậc.
  - Tường/lan can nóng gần đám cháy → haptic + Mentor "quay lại".
- **Test:** `Wallfollow_TouchRatioScored`, `Handrail_HoldWhileDescending`.

#### 13. Cầu thang bộ
- **Ngoài đời:** xuống cầu thang bộ, không dùng thang máy; không chạy; vịn lan can; nếu khói từ dưới lên thì dùng cầu thang khác.
- **VR:**
  - Mặt bậc thật (hình ảnh) + **collider dốc** (đi êm).
  - Xuống bằng analog chậm (≤ 0,8 m/s) kèm **tunneling vignette**, hoặc **teleport tới chiếu nghỉ** (tuỳ chọn chống say).
  - Mỗi tầng có số tầng sáng.
  - Cửa buồng thang chống cháy ở mỗi tầng (thao tác 9).
- **Phản hồi:** tiếng bước theo bậc, nhịp camera không lắc (tránh say).
- **Test:** `Stairs_DescendFloor5To1`, `Stairs_TeleportLandingOption`.

#### 14. Thang máy (bẫy)
- **Ngoài đời:** không dùng thang máy khi cháy.
- **VR:**
  - Poke nút gọi.
  - Cửa thang máy mở (bẫy): bước vào → Exam trừ nặng, Guided Mentor hỏi Socratic.
  - Biển "Không sử dụng thang máy khi có cháy" là một phần môi trường.
- **Test:** `Elevator_CallButton_Penalised`.

#### 15. Điểm tập kết, điểm danh
- **Ngoài đời:** tới điểm tập kết, báo người phụ trách số người, ai còn ở trong.
- **VR:** NPC trưởng ca hỏi; trả lời bằng giọng (STT) hoặc chọn ngón tay số lượng (fallback).
- **Test:** `Assembly_ReportMissingPerson`.

### B.2 Thiết lập rig để đạt các đặc tả trên (V0.1)

- Dùng `Complete XR Origin Set Up Hands Variant` (XR Hands 1.7.3 có sẵn), hiện **tay thật** cho cả tay cầm và hand tracking.
- Interaction layers:
  - `HandOnly`: near / poke / grab, cho mọi vật Phần B;
  - `Ray`: UI, teleport.
- Physics hands: tay hiển thị dừng ở collider (không xuyên tường); khi lệch > 15 cm so với tay thật → nhấp nháy viền.
- Locomotion:
  - analog (tốc độ theo tư thế);
  - snap turn 30°;
  - teleport (tuỳ chọn);
  - tunneling vignette khi di chuyển.
- Hiệu chuẩn H₀ ở menu (đã có) + chiều dài tay (đặt tay ngang vai).

---

## C. Kiểm chứng không cần kính

1. **Người dùng tự chơi:** XR Interaction Simulator trong Editor (bàn phím + chuột điều khiển đầu và hai tay). File `docs/VR_SIMULATOR_CONTROLS.md` liệt kê phím cho từng thao tác B.1.
2. **Kiểm thử tự động `XRInputDriver`:**
   - Harness PlayMode điều khiển HMD + 2 controller giả.
   - API: `SetHead(pose)`, `MoveHand(hand, pose, seconds)`, `Grip(hand, bool)`, `Trigger(hand, value)`, `Poke(hand, target, depth)`, `HandPose(hand, palmDirection)`.
   - Dùng thiết bị Input System giả (`InputTestFixture` / XRI simulated devices).
   - Mỗi test B.1 viết bằng API này; **cấm** gọi hàm game trực tiếp trong test thao tác.
3. **Video/ảnh báo cáo:** `DemoRecording`/`DemoShots` chuyển sang `XRInputDriver`; overlay hiện tay ảo để người xem thấy thao tác.
4. **Meta XR Simulator:** tuỳ chọn, cần tải → hỏi người dùng.
5. **Kính thật:** checklist V6.7 khi kính sửa xong. Mọi báo cáo phải ghi "chưa kiểm trên kính" cho phần chưa thử.

---

## D. Lộ trình V0 → V6

Mỗi phase:
- có **Việc**, **File chính**, **Xong khi**;
- kết thúc bằng `bash Tools/verify.sh --unity` + ảnh `Logs/Demo/<time>_shots` + commit.

### V0 — Nền tảng VR thật + harness kiểm chứng

| Phase | Việc | File chính | Xong khi |
| :-: | :--- | :--- | :--- |
| V0.1 | Rig "như thật": Hands Variant, interaction layers `HandOnly`/`Ray`, physics hands, locomotion + vignette, tốc độ theo tư thế (B.0, B.2) | `RigSetup` editor tool, `PostureLocomotion.cs` | ảnh tay ảo trong lab; test `Posture_FromHeadHeight` |
| V0.2 | `XRInputDriver` + test mẫu poke nút báo cháy; `VR_SIMULATOR_CONTROLS.md` | `Assets/Tests/PlayMode/XR/XRInputDriver.cs` | `Poke_CallPoint_RaisesAlarm` xanh chỉ bằng input giả |
| V0.3 | Nút báo cháy độ sâu poke (B.1-1) | `FireAlarmButton.cs`, prefab nút | 2 test B.1-1 |
| V0.4 | Bình chữa cháy: giá treo, grab nặng, xem nhãn (B.1-4) | `ExtinguisherMount.cs`, `ExtinguisherXRBinding.cs` | 2 test B.1-4 |
| V0.5 | PASS đầy đủ: chốt hai tay, loa phun grab riêng, trigger analog, đo sweep, tầm, thời lượng, bỏng lạnh (B.1-5) | `ExtinguisherPin.cs`, `ExtinguisherHorn.cs`, `SweepMeter.cs`, `FireBaseTarget.cs` | 6 test B.1-5 |
| V0.6 | Tủ điện: cửa bản lề + cần gạt hành trình (B.1-3) | `BreakerPanelDoor.cs`, `BreakerLever.cs` | 2 test B.1-3 |
| V0.7 | Kiểm tra cửa bằng mu bàn tay + mở cửa xoay tay nắm/đẩy (B.1-8, B.1-9 phần cửa thường) | `DoorHeatChecker.cs`, `ExitDoor.cs`, `DoorHandle.cs` | 5 test |
| V0.8 | Dìu Hoa bằng grab nách (B.1-10) | `EscortGrip.cs`, `NPCSimulator.cs` | 3 test B.1-10 |
| V0.9 | Chuyển `DemoShots`/`DemoRecording` sang `XRInputDriver`; chụp ảnh từng thao tác | `DemoRecording.cs` | không còn lời gọi hàm game trong demo; bộ ảnh V0 |

**Rủi ro:**
- API simulated devices của XRI khó điều khiển từ test → fallback `InputTestFixture` + `XRSimulatedController`.
- Physics hands gây rung: giới hạn lực joint.

### V1 — Bài giảng đúng nghiệp vụ, dễ → khó

| Phase | Việc | File chính | Xong khi |
| :-: | :--- | :--- | :--- |
| V1.1 | Thu thập nguồn chính thức: Luật PCCC & CNCH 2024, QCVN 06:2022/BXD, TCVN về bình chữa cháy, NFPA 10/75/101/2001, tài liệu Cảnh sát PCCC; xác minh mọi `TODO(V1)` số liệu Phần B | `docs/pccc_ground_truth.md` | mỗi số liệu Phần B có nguồn hoặc ghi "ước lượng" |
| V1.2 | `PcccRules.json`: mỗi bước, điểm, thông điệp Mentor → id nguồn; test "không có luật mồ côi" | `Assets/Resources/PcccRules.json`, `PcccRuleBook.cs` | test xanh |
| V1.3 | Rà kịch bản hiện tại theo ground truth, sửa chỗ sai (thứ tự báo động/ngắt điện/dập, tầm phun, thời gian FM-200…) | `LabProgressiveScenarioController.cs`, `SocraticRules.cs` | báo cáo rà soát trong `docs/` |
| V1.4 | Phòng Bài giảng tương tác: 5 trạm (tam giác cháy, phân loại đám cháy và chọn bình, PASS trên bình tập, báo cháy và 114, thoát nạn); mỗi trạm làm thật theo Phần B, có câu hỏi kiểm tra | `Scenes/01_Lesson.unity`, `LessonStation.cs` | đi hết 5 trạm bằng input giả |
| V1.5 | Thang học tập: Bài giảng → Guided → Exam, mở khoá theo ngưỡng; Guided có gợi ý/không phạt, Exam không chữ hướng dẫn (B.0-8) | `MainMenuController.cs`, `ProgressionStore.cs` | test mở khoá |
| V1.6 | AAR hiển thị nguồn cho từng lỗi; so sánh với lần chơi trước | `AARController.cs`, `AfterActionReport.cs` | ảnh AAR có trích dẫn |

### V2 — Tòa nhà nhiều tầng và cầu thang

| Phase | Việc | File chính | Xong khi |
| :-: | :--- | :--- | :--- |
| V2.1 | Đặc tả tòa nhà theo QCVN 06 (bề rộng hành lang/thang, chiều cao bậc, cửa chống cháy, chiếu sáng sự cố); sơ đồ thoát nạn treo tường | `docs/building_spec.md` | đặc tả có nguồn |
| V2.2 | `BuildingBuilder` idempotent: tầng 5 (Lab + hành lang + sảnh thang máy + 2 buồng thang), tầng 4–2 buồng thang, tầng 1 + sân; asset PolygonOffice | `Assets/Editor/EscapeFire/BuildingBuilder.cs` | audit 0 lỗi, ảnh từng khu |
| V2.3 | Cầu thang: bậc thật + collider dốc, lan can grab được, số tầng, đèn EXIT/sự cố (B.1-12, B.1-13) | `Handrail.cs`, `StairVolume.cs` | 4 test |
| V2.4 | Cửa buồng thang chống cháy + panic bar + door closer (B.1-9) | `FireDoor.cs` | 2 test |
| V2.5 | Thang máy mồi bẫy (B.1-14) | `ElevatorTrap.cs` | test |
| V2.6 | Khói theo vùng: mỗi không gian một controller, khói lọt qua cửa mở, cửa đóng giữ khói | `SmokeZone.cs`, `SmokeDensityController.cs` | ảnh khói hành lang/buồng thang |
| V2.7 | Locomotion chống say trên cầu thang (vignette, teleport chiếu nghỉ); ngân sách FPS (occlusion giữa tầng) | rig, `OcclusionPortals` | test đi tầng 5 → sân bằng input giả; FPS Editor |

### V3 — Cứu nạn và kịch bản thoát nạn đầy đủ

| Phase | Việc | File chính | Xong khi |
| :-: | :--- | :--- | :--- |
| V3.1 | Nạn nhân ragdoll/ArticulationBody; Rautek hai tay; kéo bằng chăn; cầu thang (B.1-11) | `VictimBody.cs`, `RautekGrip.cs` | 3 test B.1-11 |
| V3.2 | Khăn ướt + vòi nước (B.1-7) | `WetTowel.cs`, `WaterTap.cs` | 3 test |
| V3.3 | Bộ đàm/điện thoại: PTT gần miệng, khẩu lệnh 114 đủ 4 ý, TTS trực ban hỏi lại (B.1-2) | `Radio.cs`, `Call114Dialogue.cs` | 2 test |
| V3.4 | Lần tường/lan can chấm điểm (B.1-12) | `WallFollowTracker.cs` | test |
| V3.5 | Điểm tập kết + điểm danh (B.1-15); Minh hoảng loạn chạy vào thang máy → phải ngăn | `AssemblyPoint.cs`, `NPCSimulator.cs` | test |
| V3.6 | Kịch bản trọn vẹn tầng 5 → sân; chấm điểm mới (Cứu nạn, Thoát nạn) trong AAR + radar | `ScenarioController`, AAR | playthrough từng nhánh: dìu, kéo, quên Hoa, thang máy, chuẩn |

### V4 — Prompt → Kịch bản + Môi trường

| Phase | Việc | File chính | Xong khi |
| :-: | :--- | :--- | :--- |
| V4.1 | Schema `ScenarioSpec` (phòng, kích thước, bố trí, object filler, mối nguy, bình chữa cháy, NPC + trạng thái tỉnh/bất tỉnh, mục tiêu học, cấp độ) | `Server/escapefire_edge/scenario_schema.py`, `ScenarioSpec.cs` | test round-trip JSON |
| V4.2 | Catalog asset có metadata (kích thước, chặn lối?, dễ cháy?, nhóm phòng) cho Lab, thư viện, văn phòng, căng-tin, xưởng | `Assets/Resources/AssetCatalog.json`, tool sinh catalog | catalog ≥ 60 asset |
| V4.3 | `/scenario/generate`: Gemma JSON mode + few-shot + validator tầng 1 (whitelist asset, luật PCCC: bình đúng loại, khoảng cách tới bình, ≥ 2 lối thoát, không vật chắn cửa) | `scenario_generator.py`, `scenario_validator.py` | pytest; đo tỉ lệ hợp lệ với 20 prompt |
| V4.4 | Bộ sinh Unity: layout lưới + ràng buộc, đặt object filler, gắn module tương tác Phần B, NavMesh kiểm tra lối thoát (validator tầng 2) | `ScenarioBuilder.cs`, `LayoutSolver.cs` | 5 prompt → 5 cảnh chơi được |
| V4.5 | AI Director dùng ScenarioSpec chọn thử thách phù hợp phòng | `director.py`, `MicroAIGovernor.cs` | test |
| V4.5a | **Challenge Composer** (thay "chọn 1 trong 7 enum"): code cung cấp mảnh nguyên tố có tham số, mỗi mảnh có giới hạn an toàn — điểm cháy (vị trí, cường độ), tốc độ/độ dày khói, tắt đèn theo vùng, hỏng thiết bị (chốt, áp, chuông, tay nắm cửa), chặn tuyến đường, hành vi NPC (Minh chạy vào thang máy, Hoa ngất chỗ khác…), hẹn giờ kích hoạt. Gemma sinh `ChallengeSpec` JSON ghép nhiều mảnh + lý do sư phạm + luật trích dẫn (R01–R12). Thử thách mới không cần viết code | `challenge_schema.py`, `challenge_composer.py`, `ChallengeSpec.cs`, `ChallengeRunner.cs` | pytest schema + round-trip; 3 thử thách ghép chạy được trong Lab/tòa nhà, test qua XR driver |
| V4.5b | Validator 2 tầng cho `ChallengeSpec`, **dùng chung schema, catalog và validator với Prompt → ScenarioSpec**: tầng 1 (server) whitelist mảnh + tham số trong giới hạn + tiến độ theo bậc + không dồn thử thách khi CF cao; tầng 2 (kính) luôn còn lối thoát (NavMesh), ngân sách FPS. Spec bị chặn → rơi về bản enum | `challenge_validator.py`, `ChallengeValidator.cs` | test mỗi luật chặn có ca âm/dương |
| V4.5c | Giữ **AI Director enum hiện tại làm fallback / baseline** (cờ cấu hình chọn chế độ: luật cứng / enum / composer) | `director.py`, `MicroAIGovernor.cs` | test chuyển chế độ + fallback khi LLM lỗi/timeout |
| V4.6 | Đánh giá cho báo cáo: tỉ lệ hợp lệ, thời gian sinh, đa dạng, % qua validator; Gemma 4B vs 12B | `Server/bench/scenario_benchmark.py` | bảng số liệu trong `docs/` |
| V4.7 | **Ablation AI Director** — 3 phiên bản: (1) bộ luật cứng if-else, (2) Gemma chọn enum (hiện tại), (3) Gemma ghép mảnh. Chỉ số kỹ thuật: độ đa dạng tình huống (số tổ hợp khác nhau / entropy trên N phiên mô phỏng), tỉ lệ hợp lệ qua validator, độ trễ. **Sau góp ý 12/11, chỉ số chính chuyển sang VL**: tỉ lệ thử thách nhắm đúng kỹ năng yếu (VL.3), và khi có pilot: ghi nhớ / chuyển giao (VL.4) — đa dạng hay điểm số không chứng minh được việc học. Là số liệu chứng minh đóng góp của LLM cho luận văn | `Server/bench/director_ablation.py` | bảng + biểu đồ trong `docs/`, chạy với Ollama thật khi người dùng bật |

> Lý do V4.5a–c, V4.7 (bổ sung 2026-11-12, người dùng đồng ý): hiện Gemma chỉ chọn trong whitelist 7 directive và các lần chạy 11/11 luôn chọn đúng thứ tự bậc (PIN_JAM → SMOKE_SURGE → ALARM_CABLE_CUT/BLACKOUT → FM200) — một luật if-else làm được y hệt; dòng prompt "Lượt này CHỈ được chọn trong…" càng khiến nó giống luật cứng. Không trả lời được "sao phải dùng LLM" (GVHD: "chưa có gì mới"). Ghép mảnh có tham số + validator an toàn + ablation có số liệu là câu trả lời.

### VL — Học thật: chẩn đoán kỹ năng, nhắm điểm yếu, đo ghi nhớ và chuyển giao (góp ý GVHD 12/11)

**Vì sao:** GVHD chỉ ra rằng "độ khó phù hợp" không phải mục tiêu và điểm số chỉ là công cụ; thứ cần chứng minh là người học
còn nhớ và làm đúng trong tình huống thật. Câu hỏi nghiên cứu mở rộng từ *"AI làm game khó vừa phải"* thành
*"AI giữ nhịp độ khó theo trạng thái người học (CF — giữ nguyên) **và** tìm ra kỹ năng PCCC còn hổng để thiết kế tình huống
luyện đúng kỹ năng đó; người học có nhớ lâu hơn và làm đúng trong phòng chưa từng gặp không?"*. Chỉnh độ khó theo CF được
giữ (quyết định của người dùng 12/11); điều thay đổi là cách chứng minh: bằng ghi nhớ và chuyển giao, không bằng điểm. Làm **trước V5**, vì nó quyết định luận văn chứng minh được gì.

**Đã có sẵn để tận dụng:** mỗi mảnh thử thách (V4) gắn một luật R01–R12; mỗi luật đã có bằng chứng đo từ hành vi VR
(`SessionFacts`: giây đứng thẳng trong khói, tỉ lệ phun vào gốc / quét, thử cửa bằng mu bàn tay, gọi 114 đủ ý, lần tường,
không dùng thang máy, Rautek, điểm danh…); phòng sinh từ prompt (V4) dùng làm **bài kiểm tra chuyển giao**.

**Cơ sở lý thuyết (bổ sung Phần E khi viết):** knowledge tracing (Corbett & Anderson 1995, BKT); desirable difficulties
(Bjork & Bjork 2011: giãn cách, xen kẽ, buộc nhớ lại); retrieval practice (Roediger & Karpicke 2006); tranh luận flow
trong học tập (Csikszentmihalyi; các nghiên cứu GVHD nhắc là flow có thể không phục vụ việc học) — hỏi thêm nhóm game design.

| Phase | Việc | File chính | Xong khi |
| :-: | :--- | :--- | :--- |
| VL.1 | **Mô hình kỹ năng**: bảng bằng chứng luật → quan sát đúng/sai mỗi buổi (vd R07: đứng thẳng trong khói < 2 s; R04: rút chốt + gốc ≥ 60 % + quét ≥ 50 %; R08: mu bàn tay ≥ 1 s trước khi mở…); ước lượng mức thành thạo từng kỹ năng bằng BKT (tham số mặc định từ tài liệu, ghi rõ là giả định); lưu theo người học qua nhiều buổi | `Assets/Resources/SkillEvidence.json`, `SkillModel.cs` (thuần logic), `ProgressionStore` | test EditMode: chuỗi buổi giả lập → mức thành thạo tăng/giảm đúng; không luật nào thiếu bằng chứng |
| VL.2 | **Rà "lách điểm"** (góp ý phần thưởng sai → hành vi sai): với từng mục bảng điểm, tìm cách được điểm mà không làm đúng (vd khom cả buổi kể cả khi không có khói, xịt bừa đủ giây); sửa bằng chấm theo bằng chứng hành vi, AAR hiện theo kỹ năng thay vì tổng điểm | `AfterActionReport.cs`, `AARController.cs` | danh sách lỗ hổng + test cho từng lỗ đã vá; AAR có thanh thành thạo R01–R12 |
| VL.3 | **Đạo diễn nhắm điểm yếu**: telemetry gửi `weak_skills` (thành thạo thấp nhất, có giãn cách: kỹ năng đã vững thì lâu lâu mới hỏi lại); prompt composer yêu cầu mảnh có luật thuộc `weak_skills`; chế độ `rules` chọn theo bảng cố định để so sánh; **giữ nguyên cơ chế CF** (CF thấp → được thêm thử thách / tăng cấp, CF cao → giữ nhịp + trấn an), CF vẫn quyết định *khi nào* và *mấy mảnh, cấp nào*, `weak_skills` chỉ quyết định *mảnh nào* | `schema.py`, `challenge.py`, `director.py`, `EdgeDirectorClient.cs`, `MicroAIGovernor.cs` | pytest + test kính: với `weak_skills=[R08]` thử thách đưa ra luyện được R08; chỉ số "nhắm đúng kỹ năng yếu" trong `Server/bench/director_ablation.py` |
| VL.4 | **Thiết kế đánh giá** (để báo cáo, không cần code nhiều): bài trắc nghiệm 12 câu theo R01–R12 (trước / ngay sau / sau 1 tuần); bài chuyển giao trong **phòng sinh mới chưa gặp** (V4) chấm bằng bằng chứng hành vi; 2 nhóm nhỏ (đạo diễn nhắm điểm yếu vs kịch bản cố định); bảng hỏi trải nghiệm ngắn; đồng ý tham gia | `docs/evaluation_protocol.md`, câu hỏi trong `PcccRules.json`, `QuizStation` ở phòng bài giảng | GVHD duyệt protocol; công cụ xuất số liệu mỗi người học (JSON/CSV) |
| VL.5 | **Khó có chủ đích trong bài học**: Mentor hỏi trước khi gợi ý (buộc nhớ lại), giảm dần gợi ý qua các buổi, xen kẽ kỹ năng giữa các buổi, nhắc lại kỹ năng yếu ở đầu buổi sau | `SocraticRules.cs`, `LessonController.cs`, `MentorVoice.cs` | test: buổi 2 bắt đầu bằng kỹ năng yếu của buổi 1; số gợi ý giảm theo mức thành thạo |
| VL.6 | **Trao đổi với nhóm game design** (online 15–30 phút). Câu hỏi: (1) đo "học được" trong game bằng chỉ số nào? (2) flow và desirable difficulty mâu thuẫn ở đâu, nên chọn gì cho huấn luyện an toàn? (3) thiết kế thử nghiệm với ít người tham gia thế nào cho có giá trị? | ghi kết quả vào Phần A | đã gặp, kết luận ghi lại, VL.3–VL.5 điều chỉnh theo |

**Không làm (ghi lý do để trả lời hội đồng):** imitation learning cho NPC — cần dữ liệu hành vi người thật trong đám cháy,
không có trong phạm vi đồ án; NPC có "tâm lý" bằng LLM (Minh hoảng loạn trò chuyện thật) để V6, tuỳ chọn.

### V5 — Game chạy ngon (thay cho "Mô-đun hoá", quyết định người dùng 12/11)

**Mục tiêu:** một người cầm kính vào là chơi trọn từ menu tới AAR, không kẹt, không lỗi, không giật, không say.
Mô-đun hoá đã thử ngày 12/11 rồi hoàn tác (không commit): không đáng rủi ro so với lợi ích cho đồ án.

| Phase | Việc | File chính | Xong khi |
| :-: | :--- | :--- | :--- |
| V5.1 | **Chơi trọn luồng**: Menu → Bài giảng → Có hướng dẫn (Lab + tòa nhà) → Sát hạch → AAR → Thử lại / Về menu, và Menu → phòng sinh (V4). Thêm nút chọn phòng sinh vào menu; mọi màn đều có đường ra | `MainMenuController`, `AARController`, `SceneFlow`, menu builder | 1 test PlayMode đi hết luồng chỉ bằng input VR, qua cả chuyển scene; không màn nào bị kẹt |
| V5.2 | **Sửa lỗi đã biết**: HUD cổ tay chồng chữ; khói hành lang bài giảng là khối tối; Hoa / Minh biến mất rồi "hiện" ở điểm tập kết (cho đi theo xuống cầu thang hoặc giải thích rõ); kiểm tra lại độ tối khi mất điện hành lang; giá sách trống trong phòng sinh; ảnh chặn Minh lệch khung | `VRWristHUD`, `LessonRoomBuilder`, `NPCSimulator`, `PanicRunner`, `ScenarioBuilder` | mỗi lỗi có ảnh trước/sau hoặc test |
| V5.3 | **Ổn định**: Console sạch lỗi / cảnh báo trong cả luồng; chơi 3 ván liên tiếp không rò sự kiện (EventBus) hay vật thể; tải lại scene không nhân đôi hook / singleton; server LLM tắt giữa chừng vẫn chơi được (fallback) | toàn bộ | test "3 ván liên tiếp" + log Console = 0 lỗi |
| V5.4 | **Hiệu năng Quest 2** (chuyển từ V6.3): ≥ 72 FPS, < 5 % khung hình vượt 13,8 ms; giảm đèn điểm trong tòa nhà (đèn thang, ExitGlow), tắt bóng thừa, static batching, gộp vật liệu, ngân sách particle lửa/khói, culling giữa tầng | `QuestPerformanceSettings`, `BuildingBuilder`, `FrameTimeMonitor` | số liệu khung hình trên luồng chơi (Editor cấu hình Quest; kính khi có) |
| V5.5 | **Thoải mái VR**: vignette khi di chuyển, snap turn, đi cầu thang không giật, hiệu chỉnh chiều cao đầu buổi, chế độ ngồi (tuỳ chọn) | rig, `PostureLocomotion`, `VRHeightDetector` | checklist thoải mái; test hiệu chỉnh chiều cao |
| V5.6 | **Build APK + checklist cài lên kính** (khi kính sửa xong): build Android, cấu hình OpenXR Meta, kết nối server LAN | build settings, `Server/README.md` | APK build không lỗi; checklist cài đặt |

### V6 — Wow factor, hiệu năng, trình diễn

| Phase | Việc | Xong khi |
| :-: | :--- | :--- |
| V6.1 | Hình ảnh: lửa/khói đúng quy mô, đèn sự cố xoay, phản chiếu, tay XR Hands, hiệu ứng thiếu oxy | bộ ảnh trước/sau |
| V6.2 | Âm thanh không gian: lửa, còi, xả CO2, cửa, ho, bộ đàm | checklist âm thanh |
| V6.3 | (đã chuyển sang V5.4 — hiệu năng là điều kiện "game chạy ngon") | — |
| V6.4 | Kịch bản bảo vệ 15 phút: 1' vấn đề, 3' bài giảng, 5' Sát hạch với AI, 2' prompt sinh cảnh trực tiếp, 2' AAR, 2' số liệu học tập (VL: kỹ năng, ghi nhớ, chuyển giao) | `docs/defense_demo_script.md`, tập thử đúng giờ |
| V6.5 | Video 3–5 phút + ảnh quay bằng `XRInputDriver`, tay ảo hiện rõ | video + timeline |
| V6.6 | Pilot 5–10 sinh viên (Guided vs Exam vs Exam+LLM) nếu có kính | dữ liệu `TelemetryReports` |
| V6.7 | Checklist kính thật: từng thao tác B.1, cảm giác, say VR, FPS | biên bản thử |

---

## E. Nguồn đã tham khảo (V1.1 bổ sung văn bản chính thức)

- Serious game sơ tán VR, tác nhân ảnh hưởng lựa chọn lối thoát: [arXiv 1805.09138](https://arxiv.org/pdf/1805.09138)
- Say VR khi đi cầu thang trong mô phỏng sơ tán Unity: [Theseus/Xamk](https://www.theseus.fi/handle/10024/894298)
- VR vs thí nghiệm thật trong nghiên cứu sơ tán: [Fire Technology](https://link.springer.com/article/10.1007/s10694-021-01172-4)
- Đa giác quan trong huấn luyện cháy VR: [Safety Solutions](https://www.safetysolutions.net.au/content/fire/article/-multi-sensory-virtual-environments-offer-safety-benefits-537833656)
- PASS trong trình mô phỏng bình chữa cháy: [FLAIM](https://manuals.plus/flaim/pass-extinguisher-manual), [PixoVR](https://pixovr.com/vr-training-content/fire-suppression-freerangexr/)
- Kéo nạn nhân, giữ đầu thấp, xuống cầu thang: [Iowa FF](https://sites.google.com/view/iowafirefightertraining/training-articles/truck-company/leg-lock), [Fire Engineering](https://www.fireengineering.com/firefighting/civilian-rescue-removing-the-occupant/), [Miami-Dade](https://mdsceh.miamidade.gov/mobi/moms/section%2006%20-%20victim%20removal%20techniques.pdf)
- Không dùng thang máy khi cháy (NFPA 101 §7.14): [TDI Texas](https://Www.Tdi.Texas.Gov/pubs/videoresource/qahighrisebldge.pdf)
- Hướng dẫn thoát nạn của PCCC Việt Nam: [SGGP cao ốc](https://www.sggp.org.vn/thoat-nan-khi-chay-cao-oc-post471983.html), [SGGP chung cư](https://www.sggp.org.vn/bo-tui-ngay-ky-nang-thoat-hiem-hoa-hoan-khi-sinh-song-tai-chung-cu-cao-tang-post710988.html)
- Góp ý GVHD 08/10: [transcript](../../Feedback_8.10_Transcript.md)

Ghi chú:
- Các số liệu đánh dấu `TODO(V1)` (tầm phun, thời gian xả, ngưỡng tư thế, tốc độ) là **đề xuất ban đầu**, chưa phải số chuẩn.
- Chưa tìm thấy nghiên cứu đo riêng hiệu quả học khi đi cầu thang đầy khói trong VR.

---

## F. Quy tắc làm việc và prompt giao việc

**Quy tắc:**
- Làm tuần tự theo phase.
- Mỗi phase:
  - test trước;
  - `bash Tools/verify.sh --unity` (đóng Unity Editor);
  - ảnh;
  - tự xem ảnh;
  - commit + push `feature/vr-lesson-upgrade`.
- Không merge khi chưa được duyệt.
- Hỏi trước khi tải asset/package mới (Meta XR Simulator, model, animation).
- AI thật (Ollama gemma3:4b) chỉ chạy khi người dùng bật:
  `cd Server && uv run python -m escapefire_edge.app --llm-timeout 25 --debrief-timeout 45`.

**Prompt giao việc:**

```
Dự án EscapeFire (Unity 6000.3.15f1, URP, XRI 3.4.1, XR Hands 1.7.3, Quest 2), repo E:\HUST_2\20261\ProjectIII\EscapeFire,
nhánh feature/vr-lesson-upgrade. Đọc trước: docs/superpowers/plans/2026-11-11-vr-lesson-upgrade.md (Phần B là đặc tả bắt buộc),
docs/Feedback_8.10_Transcript.md, docs/superpowers/plans/2026-11-05-status-and-next-steps.md. Trả lời bằng tiếng Việt.

Làm phase tiếp theo chưa xong trong Phần D (bắt đầu V0.1), đúng cột "Xong khi", rồi báo cáo và làm tiếp phase sau nếu không bị chặn.
Yêu cầu cứng:
1. Mỗi thao tác phải đúng đặc tả Phần B (giống ngoài đời: tay gần, hai tay khi cần, đúng thứ tự, ràng buộc vật lý, ngưỡng, phản hồi).
2. Kính đang hỏng: kiểm chứng bằng XRInputDriver (input XR giả lập) — không gọi thẳng hàm game trong test thao tác hay demo.
   Ghi rõ phần nào chưa kiểm trên kính thật.
3. Số liệu PCCC phải có nguồn hoặc đánh dấu TODO(V1).
4. Dựng cảnh bằng editor tool idempotent trong Assets/Editor/EscapeFire; không sửa tay scene YAML; hỏi trước khi tải asset mới.
5. Sau mỗi phase: bash Tools/verify.sh --unity (đóng Unity Editor trước), chụp ảnh bằng PlayMode test (batchmode không -nographics),
   tự xem ảnh, commit + push. Không quay video trừ khi được yêu cầu. Không merge.
```

---

## G. Trạng thái thực hiện (cập nhật liên tục)

| Mốc | Trạng thái | Commit | Ghi chú / còn lại |
| :-: | :--- | :--- | :--- |
| V0 | ✅ xong (2026-11-12) | `2b84642`…`1efdfc3` | Harness `XRInputDriver`; tay người + co ngón; near-only; tư thế→tốc độ; nút báo cháy ấn ngón 9 mm; bình: giá treo, chốt 2 tay, loa + ống, cò analog, đo PASS; tủ điện cửa + cần gạt 70°; cửa: mu bàn tay ≥1 s, xoay tay nắm rồi đẩy; dìu Hoa bằng nách; cả ván chỉ bằng input VR (100/100). Còn: hand tracking (chỉ tay cầm), kiểm tra "cầm vào loa lạnh", xem nhãn bình, video demo qua XR (V6) |
| V1 | ✅ xong (2026-11-12) | `d4ddea6`, `d737e78` | 12 luật có nguồn (`PcccRules.json`, `docs/pccc_ground_truth.md`), test không luật mồ côi; sửa kịch bản (điểm miễn phí, gợi ý ở Sát hạch, chấm PASS theo số đo, bình 12 s); AAR hiện nguồn; font tiếng Việt; phòng bài giảng 5 trạm (`01_Lesson`); lộ trình Bài giảng → Có hướng dẫn → Sát hạch. Còn: HUD cổ tay chồng chữ (V6), khói hành lang bài giảng là khối tối (V6) |
| V2 | ✅ xong (2026-11-12) | xem `git log` "feat(building): V2" | `BuildingBuilder` (menu `EscapeFire > V2 > Build Building`): hành lang tầng 3 khoét 2 lối ra buồng thang A/B + thang máy; mỗi buồng thang 2 tầng (11 bậc × 2,8 m mỗi vế, dốc 35° cho CharacterController, chiếu nghỉ, lan can `Handrail`, biển số tầng, đèn); cửa chống cháy panic bar + tay co thủy lực (`PushBarDoor`); thang máy bẫy (`ElevatorTrap`: bấm gọi −20, Sát hạch bước vào = trượt); khói theo vùng (`SmokeZone` + `SmokeFog` một chủ sở hữu fog); mặt tiền + sân + điểm tập kết (vùng thắng dời ra đây). Cả ván diễn tập đi thang bộ bằng thumbstick xuống 2 tầng tới điểm tập kết (100/100). Còn: chỉ 2 tầng (kế hoạch 5 tầng), chưa dùng asset PolygonOffice (hộp xám), Hoa tự đi ra sau cửa Lab rồi biến mất (V3: điểm danh tại điểm tập kết), `docs/building_spec.md` chưa viết, chưa đo FPS / occlusion giữa tầng (V6), điểm lan can chưa đưa vào AAR (V3) |
| V3 | ✅ xong (2026-11-12) | xem `git log` "feat(rescue): V3" | Dựng bằng `EscapeFire > V3 > Build Rescue Scenario`. Bồn rửa + vòi vặn ¼ vòng (`WaterTap`) + khăn (`WetTowel`: ướt sau ~2 s dưới nước, che mũi miệng ≤ 10 cm → oxy giảm còn 30%); bộ đàm (`Radio`: cầm sát miệng ≤ 15 cm + giữ nút nói) → trực ban 114 (`Call114Dialogue`/`Call114Checker`: đủ 4 ý địa chỉ, tầng, đám cháy, người mắc kẹt; thiếu thì hỏi lại; không phân biệt dấu); nạn nhân bất tỉnh Nam (`VictimBody`: Rautek bắt buộc 2 tay dưới nách, kéo lê kiểu rơ-moóc, đầu cao theo tay, tuột tay nếu đi nhanh, an toàn khi vào buồng thang A, giữ cửa chống cháy mở khi đang kéo qua); Minh hoảng loạn ở thang máy (`PanicRunner`: nắm vai trước khi cửa mở → đi thang bộ, không thì kẹt thang máy); điểm tập kết (`AssemblyPoint`: đội trưởng hỏi, ấn bảng báo cáo, đáp án theo diễn biến thật); lần tường/lan can (`WallFollowTracker`). Thang điểm 7 mục (thêm Cứu nạn, Thoát nạn; báo động gồm 114). Vùng teleport chỉ nhận tia teleport (trước đây nắm gần sàn sẽ "nắm" luôn sàn). Test giới hạn 72 fps (batchmode quá nhanh làm bước đi khi khom nhỏ hơn `minMoveDistance`). Cả ván chỉ bằng input VR: 100/100. Còn: kéo nạn nhân xuống cầu thang nhiều tầng, kéo bằng chăn, nhận giọng nói thật (STT đang giả lập trong test), Hoa/Minh chỉ "hiện" ở điểm tập kết chứ không đi theo xuống, ảnh chặn Minh hơi lệch khung (V6) |
| V4 | 🟡 phần code xong (2026-11-12), còn số liệu LLM thật | xem `git log` "feat(generation): V4" | Danh mục dùng chung `V4Catalog.json` (server + kính, test kiểm khớp). **Challenge Composer**: 7 mảnh có tham số (khói, tắt đèn theo vùng, hỏng thiết bị, điểm cháy, chặn buồng thang, hành vi NPC, FM-200 + hẹn giờ), Gemma sinh `ChallengeSpec`; validator tầng 1 (`challenge.py`) và tầng 1 lặp lại trên kính (`ChallengeValidator.cs`) **cùng mã lỗi, cùng 19 ca dùng chung** (`Server/tests/fixtures/`); tầng 2 cảnh thật (`ChallengeRunner.cs`: tuyến/vùng/thiết bị tồn tại, luôn còn một buồng thang, không chặn buồng thang người học đang đứng, ngân sách FPS). 3 chế độ đạo diễn `--director-mode rules|enum|composer` (enum giữ làm baseline/fallback). **Prompt → phòng**: `POST /scenario/generate` (Gemma + 1 lần sửa theo lỗi validator + mẫu dự phòng), `ScenarioValidator.cs` cùng 13 ca dùng chung, `LayoutSolver` (lưới 0,25 m, chừa lối 1,5 m trước cửa, kiểm tra đường tới lối ra cho lửa/bình/NPC/điểm xuất phát và ≥ 90 % sàn — thay NavMesh), `ScenarioBuilder` + scene `03_Generated`, thư viện 21 nhóm / 62 prefab PolygonOffice, bình và lửa thật lấy từ Lab. Test: 5 phòng mẫu dựng được + ảnh; thư viện cháy lớp A dập bằng bình nước rồi ra EXIT, phòng máy xịt nước vào cháy điện → trượt (đều chỉ qua input VR); buồng thang A bị chặn → người học đi thang B xuống điểm tập kết. Công cụ `Server/bench/` (ablation 3 chế độ, benchmark 20 prompt). **Còn**: chạy 2 benchmark với Gemma thật (cần bật Ollama), so Gemma 4B và 12B, nút chọn phòng trong menu chính, prompt bằng giọng nói, 5 phòng mẫu hiện là mẫu viết tay/mẫu dự phòng, chưa phải đầu ra Gemma, hiệu lực chất chữa cháy theo lớp chỉ có: nước vào điện = trượt, CO2 vào lớp A = cháy lại 1 lần |
| VL | 🟡 phần code xong (2026-11-12); còn việc của người dùng: gặp nhóm game design, GVHD duyệt giao thức, chạy pilot | xem `git log` "feat(learning): VL" | **VL.1** `SkillEvidence` (mỗi buổi → đạt/chưa theo R01–R12, chỉ khi buổi đó có cơ hội thể hiện), `SkillModel` (BKT, tham số mặc định = giả định cần hiệu chỉnh), `SkillStore` theo học viên; nguồn bằng chứng: diễn tập, trạm bài giảng, phòng sinh (R03). **VL.2** `docs/vl_scoring_loopholes.md`: vá 4 lỗ (báo động muộn vẫn đủ điểm, nói "địa chỉ" thay cho địa chỉ, đỡ Hoa rồi bỏ lại, đứng thẳng trong khói hành lang/buồng thang không bị tính); AAR hiện ĐẠT/CHƯA + mức thành thạo từng kỹ năng; LMS có `skill_observations`, `skill_mastery`. **VL.3** mỗi mảnh/directive ghi kỹ năng nó luyện (catalog `skills`, `option_skills`, `directive_skills`); telemetry gửi `weak_skills`; composer ưu tiên kỹ năng yếu và báo `targets_weak_skill`; đạo diễn cục bộ ưu tiên directive luyện kỹ năng yếu; **CF giữ nguyên** (quyết định *khi nào / mức nào*); ablation có cột "nhắm đúng kỹ năng yếu". **VL.4** 12 câu trắc nghiệm trong `PcccRules.json`, `Server/bench/make_quiz.py` → `docs/vl_quiz_forms.md` (3 đề trước/sau/1 tuần), `Server/bench/learning_report.py` → CSV, `docs/evaluation_protocol.md`. **VL.5** Mentor hỏi trước rồi mới chỉ, kỹ năng đã vững chỉ được hỏi; đầu buổi có hướng dẫn nhắc kỹ năng còn yếu buổi trước. **VL.6** `docs/vl_game_design_meeting.md` (câu hỏi + chỗ ghi kết quả). **Còn**: buổi gặp nhóm game design, GVHD duyệt giao thức, pilot với người thật, hiệu chỉnh tham số BKT bằng dữ liệu pilot, trạm trắc nghiệm trong VR (hiện dùng đề giấy / form), số liệu ablation với Gemma thật |
| V5 | 🟡 đang làm — đổi nội dung 12/11: **game chạy ngon** (bỏ mô-đun hoá theo quyết định người dùng; bản thử tách gói đã hoàn tác, không commit) | xem `git log` "feat(flow): V5.1" | **V5.1 xong**: test `V5FlowTests` đi trọn Menu → Bài giảng → Menu → Lab → AAR → Menu → phòng sinh → AAR → Làm lại, mọi nút bấm bằng đầu ngón tay, qua 7 lần chuyển scene thật. Đã thêm: nút "4. PHÒNG MỚI" ở menu (lần lượt 5 phòng mẫu, mỗi lần một bố trí), nút "VỀ MENU" ở phòng bài giảng, phòng sinh kết thúc → ghi kết quả 3 mục → AAR, AAR "Làm lại" về đúng màn vừa chơi, nút menu/AAR bấm được bằng ngón tay (`UIButtonPoke`, tia ray vẫn dùng được). **Lỗi thật đã sửa (V5.2)**: rig ở Menu và AAR xuất hiện quay lưng 130° với bảng (trên kính người chơi sẽ không thấy menu); bàn hologram AAR chắn trước bảng kết quả (không tới gần bấm được); ký tự "✔ ○" không có trong font tiếng Việt → hiện khoảng trắng ở menu và trạm bài giảng; dòng trạng thái menu đè nút; radar 5 trục hiện 0 % cho phòng sinh (giờ ẩn). **Còn**: V5.2 phần còn lại (HUD cổ tay, khói hành lang bài giảng, Hoa/Minh ở điểm tập kết, độ tối hành lang, giá sách trống), V5.3–V5.6; cảnh báo "nhiều XR Interaction Manager" khi chuyển scene cần xem ở V5.3 |
| V6 | ⏳ | | |

Lệnh hay dùng: `bash Tools/verify.sh --unity` (toàn bộ), `bash Tools/run_playmode.sh "<filter>" <log>` (chạy chọn lọc, có GPU, ảnh ở `Logs/Demo/<giờ>_xr`).
Dựng cảnh: `EscapeFire > V0 > Setup Realistic Rig (Lab)`, `EscapeFire > V3 > Build Rescue Scenario` (dựng lại cả V2), `EscapeFire > V4 > Build Generated Scene`, `EscapeFire > V2 > Build Building (stairs, fire doors, elevator, yard)`, `EscapeFire > V1 > Build Lesson Room`, `EscapeFire > V1 > Setup Vietnamese Font`.
