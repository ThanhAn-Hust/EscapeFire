# Chơi EscapeFire không cần kính: XR Interaction Simulator

Khi không cắm Quest 2, bấm **Play** trong Unity Editor: `HeadsetPlayModeGuard` tự bật **XR Interaction Simulator** (XRI 3.4.1).
Bàn phím + chuột điều khiển **đầu** và **hai tay cầm**; game phản ứng y như với kính thật (cùng XR rig, cùng interactor).

Phím lấy trực tiếp từ `Assets/Samples/XR Interaction Toolkit/3.4.1/XR Interaction Simulator/*.inputactions`.

## Phím cơ bản

| Phím | Tác dụng |
| :--- | :--- |
| `Tab` | Đổi thiết bị đang điều khiển (đầu → tay trái → tay phải…) |
| `H` | Điều khiển **đầu** |
| `[` / `]` | Điều khiển **tay trái** / **tay phải** |
| `W A S D` | Dịch chuyển thiết bị đang chọn: tiến/lùi/trái/phải |
| `Q` / `E` | Hạ / nâng (dùng cho **khom người**: chọn đầu `H`, nhấn `Q`) |
| Giữ chuột phải + di chuột | Xoay thiết bị đang chọn (nhìn quanh / xoay cổ tay) |
| Phím mũi tên | Xoay bằng bàn phím |
| `Z` / `C` / `V` | Khoá trục Z / Y / X khi di chuyển (căn tay chính xác) |
| `R` | Đặt lại vị trí thiết bị |
| `G` | **Grip** (nắm) — tay đang chọn; giữ `Shift` để dùng tay trái |
| `T` | **Trigger** (bóp cò) |
| `I J K L` | Cần analog (đi lại bằng locomotion) |
| `1` / `2` | Nút A/X, B/Y |
| `X` | Menu thao tác nhanh của simulator |

Ghi chú: trong simulator, Grip/Trigger là bật/tắt đầy (0 hoặc 1), không có mức bóp analog như tay cầm thật.

## Thao tác bài học → phím

| Thao tác (kế hoạch Phần B.1) | Cách làm trong simulator |
| :--- | :--- |
| Khom / bò dưới khói | `H` rồi `Q` để hạ đầu (≈ 0,8 H₀ là khom, < 0,55 H₀ là bò); tốc độ đi tự giảm |
| Bấm nút báo cháy | `]` chọn tay phải, `W`/`Q`/`E` đưa tay **chạm** nút (tay phải ở sát nút, chĩa từ xa không được), `G` |
| Lấy bình chữa cháy | Đưa tay tới tay xách bình, giữ `G`, nâng lên (`E`) rồi kéo ra (`S`) |
| Rút chốt (V0.5) | Tay trái giữ bình (`Shift`+`G`), tay phải tới chốt, `G`, kéo ra (`D`/`S`) |
| Ngắm và xịt | Tay cầm loa phun chĩa vào **gốc lửa**, `T` để bóp; quét trái-phải bằng chuột phải + di chuột |
| Gạt Aptomat (V0.6) | Tay tới cần gạt, `G`, kéo xuống (`Q`) hết hành trình |
| Kiểm tra cửa (V0.7) | Xoay cổ tay để **mu bàn tay** hướng vào cửa (chuột phải + di chuột), chạm cửa ≥ 1 s |
| Mở cửa (V0.7) | `G` tay nắm, xoay xuống, đẩy (`W`) |
| Dìu Hoa (V0.8) | Hạ đầu (`H`,`Q`), tay tới nách Hoa, `G`, nâng lên (`E`), đi (`I J K L`) giữ `G` |

## Không kiểm được bằng simulator

- Cảm giác cầm nắm thật, trọng lượng, rung (haptic).
- Say VR khi di chuyển / xuống cầu thang.
- Chiều cao, sải tay thật của người chơi.
- Bóp cò analog (simulator chỉ 0/1).

Những điểm này kiểm khi kính sửa xong (checklist V6.7 trong kế hoạch).

## Kiểm thử tự động

Claude chạy các thao tác trên bằng `Assets/Tests/PlayMode/XR/XRInputDriver.cs`. Driver tạo HMD + 2 tay cầm giả (cùng loại thiết bị simulator dùng) và điều khiển bằng code, không gọi thẳng hàm game:

```bash
bash Tools/run_playmode.sh "V0RigTests" v0rig
```

Ảnh góc nhìn người chơi lưu ở `Logs/Demo/<giờ>_xr/`.
