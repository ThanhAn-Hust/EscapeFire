# VL.2 — Rà "lách điểm" trong bảng điểm PCCC

GVHD 12/11: *AI tối ưu phần thưởng chứ không tối ưu hành vi; phần thưởng sai thì hành vi sai.* Bảng điểm 100 điểm
chính là "phần thưởng" của người học (và của đạo diễn AI). Với từng mục, câu hỏi là: **có cách nào được điểm mà
không thật sự làm đúng?**

| Mục | Cách lách (trước VL.2) | Đã sửa | Test |
| :--- | :--- | :--- | :--- |
| Báo động + 114 | Bấm chuông ở phút thứ 5 vẫn đủ điểm | Điểm theo độ trễ: ≤ 30 s đủ, ≤ 90 s 60 %, sau đó 30 % (R01 "báo ngay") | `VLSkillTests.LateAlarm_LosesPoints` |
| Gọi 114 | Chỉ cần nói cụm "địa chỉ" là tính đủ ý địa chỉ | Phải có địa danh / số cụ thể (đường, tòa, trường, số nhà…) | `VLSkillTests.SayingTheWordAddress_IsNotAnAddress` |
| Cứu nạn (Hoa) | Đỡ Hoa dậy rồi bỏ lại vẫn được 8 điểm | Đủ 8 điểm khi Hoa thật sự ra khỏi phòng; chỉ đỡ dậy được 3 | `VLSkillTests.HoaPickedUpButLeftBehind_IsNotARescue` |
| Giữ thấp | Chỉ đo khói trong Lab; đứng thẳng trong hành lang / buồng thang đầy khói không bị trừ (và lớp khói Lab lại bị áp sai ra ngoài phòng) | Lớp khói Lab chỉ tính trong Lab; khói hành lang / buồng thang (`SmokeZone`) cũng tính | PlayMode cả ván (`FullDrill_ThroughVRInputOnly`) |
| Kỹ thuật PASS | Ngoài VR (không đo được tỉ lệ nhắm / quét) được tính đủ | Giữ nguyên: trong VR luôn đo được; ghi rõ là giới hạn khi chạy logic không có kính | — |
| Thoát nạn: lần tường | Không đi trong khói thì được đủ 4 điểm | Giữ nguyên (không có khói thì không cần lần tường); mô hình kỹ năng **không** ghi R10 là "đạt" chỉ nhờ điều này | `SkillEvidence` |
| Điểm danh | 3 nút → đoán bừa đúng 1/3 | Giới hạn đã biết; mô hình kỹ năng dùng BKT có tham số "đoán" nên một lần đúng không làm R10 thành "vững" | `VLSkillTests` (BKT) |

**Thay đổi cách nhìn kết quả:** AAR giờ hiện từng kỹ năng R01–R12 mà buổi tập đã kiểm tra (ĐẠT / CHƯA, lý do) và mức
thành thạo qua các buổi, không chỉ tổng điểm. Tổng điểm vẫn giữ cho tương thích LMS nhưng không dùng làm bằng chứng
học được (xem `docs/evaluation_protocol.md`).
