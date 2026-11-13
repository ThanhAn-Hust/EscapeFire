# V5.4 Hiệu năng + V5.5 Thoải mái VR

Đo bằng `V5PerfTests` (category `Perf`): chất lượng **Low** (mức Android dùng, `Performance URP Config`), lửa + khói đang chạy (Phase 2), 4 góc nhìn chính. Kết quả ghi vào `Logs/perf/v5_perf.json` (trước: `Logs/perf/v5_perf_before.json`).
Draw call / SetPass / tam giác là số đếm, không phụ thuộc máy. Thời gian khung hình trong Editor chỉ có giá trị so sánh tương đối. Chưa đo trên kính vì kính đang hỏng.

## Số liệu trước / sau

| Góc nhìn | Batches (trước → sau) | SetPass | Tam giác (trước → sau) |
|---|---|---|---|
| Tổng quan phòng lab | 76 → **46** | 20 → 22 | 24k → 26k |
| Hành lang tầng 3 | 103 → **68** | 12 → 11 | 18k → 15k |
| Cầu thang A | 14 → **11** | 5 → 6 | 3,9k → 4,1k |
| Sân / điểm tập kết | 385 → **83** | 39 → 23 | 44k → **18k** |

Ngân sách thường dùng cho Quest 2 ở 72 Hz là khoảng 100–200 draw call và dưới 100k tam giác. Sau khi tối ưu, cả 4 góc nhìn đều nằm trong ngân sách. Ảnh chụp trước và sau giống hệt nhau, tức là không mất vật nào do culling.

## Đã làm

1. **Đèn phụ trên Quest**: `Performance URP Config` trước đây tắt hẳn đèn phụ (`AdditionalLightsRenderingMode = 0`), nên trên kính đèn thang, đèn EXIT và ánh lửa không chiếu sáng gì cả. Nay đặt chế độ per-vertex (rẻ), tối đa 2 đèn mỗi vật, tắt soft shadow.
2. **Static batching**: `RescueBuilder.MarkStatic()` đánh dấu Batching/Occluder/Occludee cho mọi phần không di chuyển của scene lab, tổng cộng 320 vật. Các phần vẫn để động: cửa chống cháy, cửa thang máy, lớp khói, NPC, nạn nhân, vật cầm được, ghế bị NPC xô đổ, lửa.
3. **Occlusion culling**: `FlowScenesBuilder.BakeOcclusion()` bake mất khoảng 2 giây. `RescueBuilder.Run` tự bake lại mỗi khi dựng lại tòa nhà. Đây là bước giảm mạnh nhất ở góc nhìn sân: các phòng sau tường không còn bị vẽ.

Chỉ số còn theo dõi: 11 đèn đang bật nhưng chỉ 1 đèn đổ bóng (đèn mặt trời), 250–380 hạt lửa/khói đang sống.

## V5.5 Thoải mái VR: checklist

| Mục | Trạng thái | Kiểm chứng |
|---|---|---|
| Vignette khi di chuyển/xoay | ✓ ở cả 5 scene (10 provider) | `V5ComfortTests.EveryScene_Rig_VignetteAndSnapTurn` |
| Snap turn 45°, tắt xoay mượt | ✓ ở cả 5 scene | cùng test trên + `RightStick_TurnsInSnapSteps` (đúng một bước 45°) |
| Đi cầu thang không giật | ✓ xuống 1 đoạn thang 1,78 m, tối đa 2,8 cm mỗi khung, không có cú giật > 6 cm | `Stairs_DownOneFlight_NoHeadJolts` |
| Tốc độ theo tư thế (đứng / khom / bò) | ✓ có từ V0 | `V0RigTests` |
| Hiệu chỉnh chiều cao ở menu | ✓ **đã sửa lỗi** | `Menu_ConfirmHeight_ByFingertip_CalibratesCrouch` |
| Chế độ ngồi | — không làm (tuỳ chọn) | |

**Lỗi đã sửa**: nút "XÁC NHẬN CHIỀU CAO" đọc `localPosition.y` của camera. Khi rig chạy ở chế độ Device (camera offset cao 1,12 m), chiều cao đọc được chỉ khoảng 0,7 m, bị báo "không hợp lệ" và không lưu, nên ngưỡng khom vẫn dùng mặc định 1,65 m. Nay chiều cao được tính là khoảng cách từ đầu tới sàn của rig, đúng ở cả chế độ Floor lẫn Device. Ví dụ người cao 1,82 m giờ bị tính là khom ngay khi đầu hạ xuống dưới 1,46 m.

## Cần kiểm tra trên kính (khi sửa xong)

- Frame time thật (OVR Metrics): mục tiêu ≥ 72 FPS, dưới 5 % khung hình vượt 13,8 ms, nhất là khi đứng sát đám cháy (nhiều hạt).
- Cảm giác khi đi cầu thang và khi xoay, với 2–3 người chơi thử.
