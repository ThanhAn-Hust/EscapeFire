using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeFire.Analytics;
using EscapeFire.Core;

namespace EscapeFire.Tests.PlayMode.XR
{
    /// <summary>
    /// The gameplay video: ONE complete drill played only through simulated VR input — exactly the steps of
    /// <see cref="V0FullDrillTests.FullDrill_ThroughVRInputOnly"/> (call point, 114 on the radio, CO2 off its bracket,
    /// pin, breaker, PASS on the fire, helping Hoa, door check, stopping Minh, Rautek drag, stairs, roll call) —
    /// recorded from the learner's eyes with Vietnamese captions. Output: Logs/Video/&lt;time&gt;/gameplay.mp4 + timeline.md.
    /// Run alone with a GPU: bash Tools/run_playmode.sh "EscapeFire.Tests.PlayMode.XR.V6GameplayVideo.RecordOneDrill" video.log
    /// </summary>
    [Category("Demo")]
    public class V6GameplayVideo : V0FullDrillTests
    {
        // caption shown AFTER each step screenshot = what the learner does next
        private static readonly (string shot, string done, string next)[] Steps =
        {
            ("01_fire_breaks_out", "Tủ server bốc cháy", "Bước 1 — Đi tới nút báo cháy, ấn bằng đầu ngón tay"),
            ("02_call_point_pressed", "Đã ấn nút báo cháy", "Bước 2 — Cầm bộ đàm sát miệng, giữ nút nói, gọi 114: địa chỉ, tầng, đám cháy, người mắc kẹt"),
            ("02b_calling_114", "Đã gọi 114 đủ 4 thông tin", "Bước 3 — Khom người, nắm quai xách, nhấc bình CO2 khỏi giá treo"),
            ("03_extinguisher_in_hand", "Đã nhấc bình khỏi giá", "Bước 4 — Tay kia nắm vòng chốt, rút chốt an toàn"),
            ("04_pin_pulled", "Đã rút chốt", "Bước 5 — Ngắt điện trước khi chữa cháy điện: mở cửa tủ điện, gạt cầu dao xuống hết"),
            ("05_power_cut", "Đã ngắt cầu dao", "Bước 6 — PASS: cầm loa phun, tiến lại gần, khom thấp, hướng vào GỐC lửa, bóp cò, quét qua lại"),
            ("06_spraying_at_the_base", "Đang phun vào gốc lửa", "PASS — giữ cò, quét loa qua lại ở gốc lửa cho tới khi tắt hẳn"),
            ("07_fire_out", "Lửa đã tắt", "Bước 7 — Hoa choáng vì khói, ngồi gục: đặt bình xuống, khom người, đỡ nách dìu dậy"),
            ("08_helping_hoa_up", "Đã đỡ Hoa dậy", "Bước 8 — Dìu Hoa ra cửa phòng, cả hai đi khom dưới lớp khói"),
            ("09_leading_hoa_to_the_door", "Đã tới cửa", "Bước 9 — Áp MU bàn tay lên cửa ít nhất 1 giây để thử nóng"),
            ("10_back_of_hand_on_door", "Cửa không nóng", "Bước 10 — Xoay tay nắm rồi đẩy cửa"),
            ("11_door_open", "Đã mở cửa", "Ra hành lang tầng 3 — đi theo biển LỐI THOÁT, không dùng thang máy"),
            ("12_out_in_the_corridor", "Đã ra hành lang", "Bước 11 — Đặt Hoa ngồi tạm cạnh tường, chạy tới giữ Minh đang bấm thang máy"),
            ("13_stopping_minh_at_the_lift", "Đã giữ Minh lại: Minh quay lại dìu Hoa xuống thang B", "Bước 12 — Nam bất tỉnh trước cửa buồng thang A: mở cửa, quỳ phía đầu, kéo Rautek thẳng qua cửa"),
            ("14_rautek_04_in_the_stairwell", "Đã kéo Nam vào buồng thang an toàn", "Bước 13 — Đi thang bộ xuống 2 tầng, bám lan can"),
            ("15_going_down_the_stairs", "Đang xuống cầu thang", "Xuống tiếp tới tầng 1, đẩy thanh cửa thoát hiểm ra sân"),
            ("16_at_the_assembly_point", "Đã tới điểm tập kết", "Bước 14 — Báo cáo người điều phối: Nam còn ở buồng thang A"),
            ("17_reported_to_the_warden", "Đã báo cáo điểm danh", "Hoàn thành buổi diễn tập"),
        };

        private XRVideoRecorder _rec;

        private void OnShot(string name)
        {
            if (_rec == null) return;
            foreach (var s in Steps)
                if (s.shot == name) { _rec.Done(s.done); _rec.Caption(s.next); return; }
        }

        [UnityTest, Timeout(1200000)]
        public IEnumerator RecordOneDrill()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Recording needs a GPU (run without -nographics).");
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Video", DateTime.Now.ToString("yyyyMMdd_HHmm"));
            _rec = XRVideoRecorder.Begin(dir);
            _rec.Caption("Phòng Lab 302, tầng 3 — một tủ server sắp bốc cháy");
            ShotTaken += OnShot;
            try
            {
                yield return FullDrill_ThroughVRInputOnly();
                // the result as the learner gets it at the end of the drill
                var sb = new StringBuilder("<b>KẾT QUẢ BUỔI DIỄN TẬP</b>\n");
                foreach (var it in SessionResult.Scorecard) sb.AppendLine($"{(it.Points == it.MaxPoints ? "✓" : "✗")} {it.Label}: {it.Points}/{it.MaxPoints}");
                if (SessionResult.Scorecard.Count > 0) sb.AppendLine($"\n<b>Tổng: {AfterActionReport.Total(SessionResult.Scorecard)}/100</b>");
                sb.Append($"Kỹ năng PCCC đạt: {SessionResult.SkillObservations.Count(o => o.Correct)}/{SessionResult.SkillObservations.Count}");
                _rec.Caption("Hoàn thành: báo động, gọi 114, ngắt điện, dập lửa bằng PASS, cứu 2 người, thoát bằng thang bộ, điểm danh");
                _rec.Summary(sb.ToString());
                for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime) yield return null;
            }
            finally
            {
                ShotTaken -= OnShot;
                string path = _rec.VideoPath;
                long frames = _rec.Frames;
                _rec.Stop();
                Debug.Log($"[Video] {path} frames={frames} ({frames / 30f:0.0}s)");
            }
        }
    }
}
