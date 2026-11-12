# Giao thức đánh giá EscapeFire (VL.4) — bản nháp để GVHD duyệt

**Câu hỏi nghiên cứu.** Khi đạo diễn AI giữ nhịp độ khó theo trạng thái người học (CF) **và** nhắm thử thách vào kỹ năng
PCCC còn yếu, người học có (1) nhớ kiến thức lâu hơn và (2) làm đúng hơn trong một phòng chưa từng gặp so với kịch bản
cố định không?

**Vì sao không dùng điểm số làm bằng chứng** (GVHD 12/11): điểm là công cụ, có thể "lách" (xem
`docs/vl_scoring_loopholes.md`); thứ cần đo là kiến thức còn đọng lại và hành vi đúng khi gặp tình huống mới.

## Thiết kế

| | Nhóm A — kịch bản cố định | Nhóm B — đạo diễn AI |
| :--- | :--- | :--- |
| Bài giảng VR (`01_Lesson`) | như nhau | như nhau |
| 2 buổi diễn tập có hướng dẫn | thử thách theo thứ tự cố định (`--director-mode rules`) | CF chọn lúc / mức, Gemma ghép mảnh nhắm kỹ năng yếu (`--director-mode composer`) |
| Bài chuyển giao | **phòng sinh mới chưa gặp** (V4, `03_Generated`, cùng một spec cho cả hai nhóm) | như nhóm A |

Phân nhóm ngẫu nhiên (bốc thăm), cỡ mẫu nhỏ (pilot 6–10 người/nhóm) — kết quả là **xu hướng**, không khẳng định thống kê;
ghi rõ hạn chế này trong báo cáo.

## Thời điểm đo

1. **Trước** buổi học: trắc nghiệm đề A (`docs/vl_quiz_forms.md`, 12 câu = 12 luật R01–R12).
2. **Ngay sau** buổi 2: trắc nghiệm đề B + bài chuyển giao trong phòng mới.
3. **Sau 1 tuần**: trắc nghiệm đề C + (nếu được) một phòng mới khác — đo **ghi nhớ**.

## Chỉ số

| Chỉ số | Nguồn | Ghi chú |
| :--- | :--- | :--- |
| Điểm trắc nghiệm theo từng luật (trước / sau / 1 tuần) | giấy hoặc form online | chênh lệch sau − trước, 1 tuần − sau |
| Hành vi đúng trong phòng chuyển giao: chọn đúng bình theo lớp cháy (R03), ngắt điện (R02), giữ thấp (R07), thử cửa (R08), thoát đúng (R10) | `SkillEvidence` từ VR, `Server/bench/learning_report.py` | không phụ thuộc tổng điểm |
| Mức thành thạo BKT từng kỹ năng qua các buổi | `skill_mastery` trong báo cáo LMS | tham số BKT mặc định (giả định, ghi rõ) |
| Tỉ lệ thử thách nhắm đúng kỹ năng yếu | `Server/bench/director_ablation.py` + log đạo diễn | chỉ nhóm B |
| Trải nghiệm: căng thẳng, hiện diện (presence), say VR | bảng hỏi ngắn sau buổi | phát hiện tác dụng phụ của độ khó |
| Thời gian, số lần thất bại | log phiên | |

## Quy trình và an toàn

- Phiếu đồng ý tham gia; được dừng bất cứ lúc nào (say VR, khó chịu).
- Buổi VR ≤ 20 phút / lần; có người đứng cạnh; không gian trống 2 × 2 m.
- Dữ liệu chỉ lưu mã học viên, không lưu họ tên trong log; xóa sau khi hoàn thành đồ án.

## Xuất số liệu

```bash
cd Server
uv run python bench/make_quiz.py --out ../docs/vl_quiz_forms.md
uv run python bench/learning_report.py --out ../docs/vl_learning_data.csv
```

## Còn mở

- Cách chấm bài chuyển giao cần GVHD duyệt (bảng hành vi trên đủ chưa?).
- Hỏi nhóm game design (VL.6) về chỉ số "học được" và cỡ mẫu.
