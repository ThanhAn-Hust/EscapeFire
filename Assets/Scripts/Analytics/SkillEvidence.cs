using System.Collections.Generic;

namespace EscapeFire.Analytics
{
    public struct SkillObservation
    {
        public string Skill;
        public bool Correct;
        public string Why;
    }

    /// <summary>
    /// Plan VL.1: turns one drill (its measured behaviour, <see cref="SessionFacts"/>) into correct / wrong observations
    /// per PCCC rule. A skill is only observed when the drill gave a chance to show it (no FM-200 countdown → nothing
    /// about R11). R05 (CO2 charge) and R12 (fire triangle) are knowledge items: the lesson and the quiz observe them.
    /// The thresholds are the rule book's (R04 PASS: aim at the base ≥ 60 %, sweep ≥ 50 % of spraying time).
    /// </summary>
    public static class SkillEvidence
    {
        public const float StandingInSmokeTolerance = 2f;
        public const float AlarmWithinSeconds = 30f;
        public const float VictimHeadLow = 1.0f;

        public static readonly string[] AllSkills =
        {
            "R01_ALARM_EARLY", "R02_POWER_OFF_FIRST", "R03_RIGHT_AGENT", "R04_PASS", "R05_CO2_CHARGE", "R06_EVACUATE_IF_EMPTY",
            "R07_STAY_LOW", "R08_DOOR_BACK_OF_HAND", "R09_RESCUE_KEEP_LOW", "R10_NO_ELEVATOR", "R11_FM200_EVACUATE", "R12_FIRE_TRIANGLE"
        };

        public static List<SkillObservation> FromDrill(in SessionFacts f)
        {
            var list = new List<SkillObservation>();
            if (f.FireStartedAt < 0f) return list; // no emergency, nothing shown

            float alarmAfter = f.AlarmRaisedAt < 0f ? -1f : f.AlarmRaisedAt - f.FireStartedAt;
            bool alarm = alarmAfter >= 0f && alarmAfter <= AlarmWithinSeconds && (!f.Call114Present || f.Call114Complete);
            Add(list, "R01_ALARM_EARLY", alarm, f.AlarmRaisedAt < 0f ? "không báo động" :
                alarmAfter > AlarmWithinSeconds ? $"báo động muộn ({alarmAfter:F0}s)" : (f.Call114Present && !f.Call114Complete ? "chưa gọi 114 đủ ý" : "báo động sớm"));

            if (f.FirstSprayAt >= 0f || f.PowerCutAt >= 0f)
            {
                bool cutFirst = f.PowerCutAt >= 0f && (f.FirstSprayAt < 0f || f.PowerCutAt <= f.FirstSprayAt);
                Add(list, "R02_POWER_OFF_FIRST", cutFirst, cutFirst ? "ngắt điện trước khi xịt" : "xịt khi chưa ngắt điện");
            }
            if (f.FirstSprayAt >= 0f)
            {
                bool right = !f.FirstSprayWithConductiveAgent && !f.UsedConductiveAgentOnLivePower;
                Add(list, "R03_RIGHT_AGENT", right, right ? "đúng chất chữa cháy" : "dùng nước/bọt cho cháy điện");
            }
            if (f.PinPulled)
            {
                bool measured = f.PassBaseRatio >= 0f && f.PassSweepRatio >= 0f;
                bool pass = f.FireExtinguished && (!measured || (f.PassBaseRatio >= 0.6f && f.PassSweepRatio >= 0.5f));
                Add(list, "R04_PASS", pass, pass ? "PASS đúng, dập tắt" : measured ? $"nhắm gốc {f.PassBaseRatio:P0}, quét {f.PassSweepRatio:P0}" : "chưa dập tắt");
            }
            if (f.LowPressureApplied || f.Fm200Started)
                Add(list, "R06_EVACUATE_IF_EMPTY", f.Passed, f.Passed ? "bình hết / FM-200: đã sơ tán" : "không kịp sơ tán");

            bool stayedLow = f.StandingInSmokeSeconds < StandingInSmokeTolerance;
            Add(list, "R07_STAY_LOW", stayedLow, stayedLow ? "luôn giữ thấp" : $"đứng thẳng trong khói {f.StandingInSmokeSeconds:F0}s");

            if (f.Passed || f.DoorHeatChecked)
                Add(list, "R08_DOOR_BACK_OF_HAND", f.DoorHeatChecked, f.DoorHeatChecked ? "thử cửa bằng mu bàn tay" : "mở cửa không thử nhiệt");

            if (f.HoaTracked || f.VictimPresent)
            {
                bool rescue = (!f.HoaTracked || f.HoaOut) && (!f.VictimPresent || (f.VictimSafe && (f.VictimMaxHeadHeight < 0f || f.VictimMaxHeadHeight <= VictimHeadLow)));
                Add(list, "R09_RESCUE_KEEP_LOW", rescue, rescue ? "đưa người bị nạn ra, giữ thấp" : "chưa đưa hết người bị nạn ra an toàn");
            }
            if (f.Passed)
            {
                bool wall = f.WallFollowRatio < 0f || f.WallFollowRatio >= 0.3f;
                bool exitOk = !f.ElevatorUsed && (!f.PanicNpcPresent || f.PanicNpcStopped) && wall;
                Add(list, "R10_NO_ELEVATOR", exitOk, f.ElevatorUsed ? "dùng thang máy" : !wall ? "không lần tường trong khói" :
                    (f.PanicNpcPresent && !f.PanicNpcStopped) ? "để Minh vào thang máy" : "thang bộ, lần tường");
            }
            if (f.Fm200Started)
                Add(list, "R11_FM200_EVACUATE", f.Passed, f.Passed ? "sơ tán khi FM-200 đếm ngược" : "còn trong phòng khi xả khí");
            return list;
        }

        private static void Add(List<SkillObservation> list, string skill, bool correct, string why) =>
            list.Add(new SkillObservation { Skill = skill, Correct = correct, Why = why });
    }
}
