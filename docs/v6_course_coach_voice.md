# V6 — Khoá học 6 màn, hướng dẫn từng bước, giọng Mentor tiếng Việt

Làm sau playtest vai người mới (`docs/v6_novice_playtest.md`, 18/11/2026).

## 1. Lộ trình trong menu

**1. Bài giảng** (Trạm 0 làm quen tay cầm + 5 trạm PCCC) → **2. Khoá học 6 màn** → **3. Trắc nghiệm tổng** (12 câu) → **4. Thử thách phòng lạ** (AI tạo).

Menu, trắc nghiệm, khoá học và AAR nằm trong "Trung tâm huấn luyện PCCC": phòng thật, có băng rôn và poster (PASS, 114, tam giác cháy, 4 tại chỗ), tủ chữa cháy và cửa thoát hiểm. Nút bị khoá có màu xám, dòng trạng thái nói việc nên làm tiếp theo.

## 2. Khoá học (Resources/Course.json)

| Màn | Bối cảnh | Dựng từ | Điểm mới |
| :-: | :--- | :--- | :--- |
| 1 | Phòng học — thùng rác cháy (lớp A) | Phòng sinh `classroom_trash` | Có cả bình nước và bình CO2 |
| 2 | Thư viện — cháy giá sách | Phòng sinh `library` | CO2 làm lửa tắt rồi bùng lại |
| 3 | Căng tin — chảo dầu (lớp B) | Phòng sinh `canteen_oil` | **Nước vào dầu thì lửa bùng lên** (thua, có giải thích) |
| 4 | Phòng Lab — cháy tủ điện, sơ tán, cứu người | Ván lab, chế độ Có hướng dẫn | Mentor nhắc từng bước |
| 5 | Đêm mất điện, khói dày | Ván lab | Tình huống ép lúc cháy: tắt đèn lab, khói tràn xuống, khói chặn buồng thang B. Đi qua cùng bộ kiểm duyệt với thử thách AI |
| 6 | Sát hạch tổng hợp | Ván lab, chế độ Sát hạch | Không gợi ý; AI chọn thử thách nhắm kỹ năng yếu |

- **Sao:** đạt từ 90 điểm được 3 sao, từ 70 được 2 sao, qua màn được 1 sao.
- **Mở khoá:** màn 1 mở sau bài giảng; mỗi màn sau mở khi qua màn trước.
- **AAR:** tiêu đề ghi tên màn và số sao; có nút **MÀN TIẾP THEO**.
- **Test:** `V6CourseTests`, `V1ProgressionTests`; `V5FlowTests` đi qua khoá học vào màn 4.

## 3. Hướng dẫn từng bước (GuidedCoach, chỉ ở chế độ Có hướng dẫn)

Mỗi lần chỉ một việc: báo động → gọi 114 → ngắt điện → lấy bình → rút chốt → PASS → dìu Hoa → thử cửa → mở cửa → giữ Minh → kéo Nam → xuống thang bộ → báo cáo điểm danh. Việc nào không còn cần thì bỏ qua: lửa đã tắt thì không nhắc ngắt điện nữa; đã ra hành lang thì không nhắc chữa cháy nữa.

Thang gợi ý cho việc hiện tại:
- **0 giây:** hỏi để người học tự nhớ lại.
- **8 giây:** nói rõ việc cần làm.
- **16 giây:** vật cần dùng sáng lên, có mũi tên vàng nhấp nhô phía trên.
- **24 giây:** mũi tên trước mặt chỉ hướng, kèm hướng dẫn cách làm.

Mọi lời Mentor đều hiện thành **phụ đề trước mắt** (`ViewSubtitle`), không phải nhìn xuống cổ tay. Test: `V6CoachTests`.

## 4. Giọng Mentor tiếng Việt (AI Edge)

Gemma chỉ sinh ra chữ. Phần đọc thành tiếng do **Piper** đảm nhận (giọng `vi_VN-vais1000-medium`), chạy offline trên chính laptop edge: một câu mất khoảng 0,25 giây trên CPU.

Game tìm giọng cho mỗi câu theo thứ tự:
1. File thu sẵn.
2. **Gói giọng** đóng sẵn trong game (`Resources/Voice/<key>.wav`), dùng được cả khi server tắt.
3. **`POST /tts`** của server edge, cho các câu AI sinh ra lúc chơi (có lưu đệm).
4. Nếu tất cả đều không có thì chỉ hiện phụ đề.

Câu của nhân vật, dạng "[Minh] …", không đọc bằng giọng Mentor. Mã `key` của mỗi câu là 16 ký tự hex đầu của MD5(UTF-8 của câu đã bỏ khoảng trắng hai đầu); server và kính dùng chung quy tắc này.

**Tạo gói giọng:** khi chạy trong Editor, mỗi câu chưa có giọng được ghi vào `Logs/voice_lines.txt`. Sau đó chạy:
```
cd Server
uv run python -m escapefire_edge.voice_pack --lines ../Logs/voice_lines.txt --out ../Assets/Resources/Voice
```

**Cài đặt:** mô hình giọng nằm ngoài repo, ở `EscapeFire_AI/models/piper/` (đổi chỗ bằng biến `ESCAPEFIRE_TTS_MODEL`). Thư viện: `piper-tts` (đã có trong `Server/pyproject.toml`).

**Ghi nguồn (bắt buộc theo giấy phép):** giọng nói tổng hợp bằng Piper (rhasspy, giấy phép MIT), mô hình `vi_VN-vais1000-medium` huấn luyện từ bộ dữ liệu **VAIS-1000 Vietnamese Speech Synthesis Corpus** (giấy phép CC BY 4.0, https://ieee-dataport.org/documents/vais-1000-vietnamese-speech-synthesis-corpus).
