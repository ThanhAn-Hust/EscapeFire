using System;
using System.Collections.Generic;
using System.Text;

namespace EscapeFire.Analytics
{
    /// <summary>Raw facts about one drill, collected from gameplay events (times are seconds since session start, -1 = never).</summary>
    [Serializable]
    public struct SessionFacts
    {
        public bool Passed;
        public string FailCause;
        public float DurationSeconds;
        public float FireStartedAt;
        public float AlarmRaisedAt;
        public float PowerCutAt;
        public float FirstSprayAt;
        public bool FirstSprayWithConductiveAgent;
        public bool UsedConductiveAgentOnLivePower;
        public bool PinPulled;
        public bool FireExtinguished;
        /// <summary>Share of spraying time aimed at the base of the fire (PASS "Aim"), -1 = not measured.</summary>
        public float PassBaseRatio;
        /// <summary>Share of spraying time with a side-to-side sweep (PASS "Sweep"), -1 = not measured.</summary>
        public float PassSweepRatio;
        public float StandingInSmokeSeconds;
        public bool NpcRescued;
        public bool DoorHeatChecked;
        public int AnomaliesTriggered;
        public float AverageCognitiveFriction;
        // ---- V3: 114 call, rescue, evacuation (Present = that element exists in the scene)
        public bool Call114Present;
        public bool Call114Complete;
        public int Call114AskBacks;
        public bool VictimPresent;
        public bool VictimSafe;
        public bool PanicNpcPresent;
        public bool PanicNpcStopped;
        public bool ElevatorUsed;
        /// <summary>Share of walking-in-smoke time with a hand on a wall / rail, -1 = not measured.</summary>
        public float WallFollowRatio;
        public bool RollCallPresent;
        public bool RollCallCorrect;

        public static SessionFacts Empty => new SessionFacts
        {
            FailCause = string.Empty,
            FireStartedAt = -1f,
            AlarmRaisedAt = -1f,
            PowerCutAt = -1f,
            FirstSprayAt = -1f,
            PassBaseRatio = -1f,
            PassSweepRatio = -1f,
            WallFollowRatio = -1f
        };
    }

    [Serializable]
    public struct ScorecardItem
    {
        public string Key;
        public string Label;
        public int Points;
        public int MaxPoints;
        public string Note;
    }

    /// <summary>Five-axis skill radar for the LMS (each 0..1).</summary>
    [Serializable]
    public struct SkillRadar
    {
        public float RiskDetection;
        public float ExtinguisherSelection;
        public float CrouchCompliance;
        public float AnomalyHandling;
        public float StressResilience;
    }

    /// <summary>
    /// PCCC compliance scorecard (100 points, spec SCENE_FLOW §2.3), skill radar and a deterministic
    /// Vietnamese debrief used when the generative (LLM) debrief is unavailable.
    /// </summary>
    public static class AfterActionReport
    {
        public const float CROUCH_PENALTY_PER_SECOND = 1f;
        public const float ALARM_FAST_SECONDS = 30f;

        public static List<ScorecardItem> BuildScorecard(in SessionFacts f)
        {
            var items = new List<ScorecardItem>(7);

            // Raise the alarm (R01): call point; with a radio in the scene the 114 call is part of it.
            bool alarm = f.AlarmRaisedAt >= 0f;
            int call = f.Call114Complete ? (f.Call114AskBacks == 0 ? 4 : 3) : 0;
            int alarmPts = f.Call114Present ? (alarm ? 6 : 0) + call : (alarm ? 10 : 0);
            string alarmNote = alarm ? $"Báo động sau {Since(f.FireStartedAt, f.AlarmRaisedAt):F0}s kể từ khi cháy" : "Không kích hoạt báo động";
            if (f.Call114Present)
                alarmNote += f.Call114Complete ? (f.Call114AskBacks == 0 ? "; gọi 114 đủ 4 ý" : $"; gọi 114 đủ ý sau {f.Call114AskBacks} lần bị hỏi lại") : "; chưa gọi 114 đủ địa chỉ, tầng, đám cháy, người mắc kẹt";
            items.Add(Item("alarm", "Báo động và gọi 114", alarmPts, 10, alarmNote));

            bool cutBeforeSpray = f.PowerCutAt >= 0f && (f.FirstSprayAt < 0f || f.PowerCutAt <= f.FirstSprayAt);
            items.Add(Item("power_cut", "Ngắt cầu dao trước khi dập", cutBeforeSpray ? 15 : (f.PowerCutAt >= 0f ? 6 : 0), 15,
                cutBeforeSpray ? "Ngắt Aptomat trước khi xịt" : (f.PowerCutAt >= 0f ? "Ngắt điện nhưng sau khi đã xịt" : "Không ngắt Aptomat")));

            bool rightAgent = f.FirstSprayAt >= 0f && !f.FirstSprayWithConductiveAgent && !f.UsedConductiveAgentOnLivePower;
            items.Add(Item("agent", "Chọn đúng chủng loại bình (CO2 cho điện)", rightAgent ? 15 : 0, 15,
                f.UsedConductiveAgentOnLivePower ? "Dùng nước/bọt lên thiết bị đang có điện" :
                f.FirstSprayAt < 0f ? "Chưa sử dụng bình chữa cháy" :
                rightAgent ? "Dùng bình CO2/bột phù hợp" : "Bình đầu tiên không phù hợp cháy điện"));

            // PASS (OSHA): Pull 3 + Aim at the base 4 + Sweep 3 + fire out 5. Unmeasured aim/sweep (-1) is not penalised.
            bool measured = f.PassBaseRatio >= 0f && f.PassSweepRatio >= 0f;
            int aim = !f.PinPulled ? 0 : !measured ? 4 : (int)Math.Round(4f * Math.Min(1f, f.PassBaseRatio / 0.6f));
            int sweep = !f.PinPulled ? 0 : !measured ? 3 : (int)Math.Round(3f * Math.Min(1f, f.PassSweepRatio / 0.5f));
            int technique = (f.PinPulled ? 3 : 0) + aim + sweep + (f.FireExtinguished ? 5 : 0);
            string passNote = measured ? $" — nhắm gốc {f.PassBaseRatio * 100f:F0}%, quét {f.PassSweepRatio * 100f:F0}% thời gian phun" : "";
            items.Add(Item("technique", "Kỹ thuật PASS (rút chốt, nhắm gốc, bóp, quét)", technique, 15,
                (f.FireExtinguished ? "Dập tắt hoàn toàn đám cháy" : f.PinPulled ? "Đã rút chốt nhưng chưa dập tắt" : "Chưa rút chốt") + passNote));

            int crouch = Math.Max(0, 15 - (int)Math.Ceiling(f.StandingInSmokeSeconds * CROUCH_PENALTY_PER_SECOND));
            items.Add(Item("crouch", "Duy trì khom lưng dưới khói", crouch, 15,
                f.StandingInSmokeSeconds > 0.5f ? $"Đứng thẳng trong khói {f.StandingInSmokeSeconds:F0} giây" : "Luôn giữ tư thế thấp"));

            // Rescue (R09): Hoa supported out 8; unconscious victim dragged to the stairwell 4; panicking colleague kept
            // out of the lift 3. Elements absent from the scene count with Hoa.
            int rescue = (f.NpcRescued ? 8 : 0)
                       + (f.VictimPresent ? (f.VictimSafe ? 4 : 0) : (f.NpcRescued ? 4 : 0))
                       + (f.PanicNpcPresent ? (f.PanicNpcStopped ? 3 : 0) : (f.NpcRescued ? 3 : 0));
            var rescueNotes = new List<string> { f.NpcRescued ? "dìu Hoa ra khỏi vùng khói" : "chưa hỗ trợ Hoa" };
            if (f.VictimPresent) rescueNotes.Add(f.VictimSafe ? "kéo nạn nhân bất tỉnh vào buồng thang (Rautek)" : "bỏ lại nạn nhân bất tỉnh ở hành lang");
            if (f.PanicNpcPresent) rescueNotes.Add(f.PanicNpcStopped ? "ngăn Minh đi thang máy" : "để Minh vào thang máy");
            items.Add(Item("rescue", "Cứu nạn", rescue, 15, Capitalize(string.Join(", ", rescueNotes))));

            // Evacuation (R10): only for a completed evacuation. Stairs not lift 5, hand on wall / rail in smoke 4,
            // correct roll-call report 6.
            int evac = 0;
            var evacNotes = new List<string>();
            if (f.Passed)
            {
                evac += f.ElevatorUsed ? 0 : 5;
                evacNotes.Add(f.ElevatorUsed ? "đã gọi / dùng thang máy" : "đi thang bộ, không dùng thang máy");
                bool wall = f.WallFollowRatio < 0f || f.WallFollowRatio >= 0.3f;
                evac += wall ? 4 : (int)Math.Round(4f * f.WallFollowRatio / 0.3f);
                if (f.WallFollowRatio >= 0f) evacNotes.Add($"tay lần tường / lan can {f.WallFollowRatio * 100f:F0}% thời gian đi trong khói");
                evac += !f.RollCallPresent || f.RollCallCorrect ? 6 : 0;
                if (f.RollCallPresent) evacNotes.Add(f.RollCallCorrect ? "báo đúng người còn trong tòa nhà" : "báo sai / không báo người còn trong tòa nhà");
            }
            else evacNotes.Add("chưa thoát nạn tới điểm tập kết");
            items.Add(Item("evacuation", "Thoát nạn", evac, 15, Capitalize(string.Join(", ", evacNotes))));

            return items;
        }

        public static int Total(List<ScorecardItem> items)
        {
            int sum = 0;
            for (int i = 0; i < items.Count; i++) sum += items[i].Points;
            return sum;
        }

        public static SkillRadar BuildRadar(in SessionFacts f, List<ScorecardItem> items)
        {
            float alarmSpeed = 0f;
            if (f.AlarmRaisedAt >= 0f)
            {
                float t = Since(f.FireStartedAt, f.AlarmRaisedAt);
                alarmSpeed = t <= ALARM_FAST_SECONDS ? 1f : Clamp01(1f - (t - ALARM_FAST_SECONDS) / 60f);
            }
            float powerCut = Ratio(items, "power_cut");

            return new SkillRadar
            {
                RiskDetection = Clamp01(0.5f * alarmSpeed + 0.3f * powerCut + (f.DoorHeatChecked ? 0.2f : 0f)),
                ExtinguisherSelection = Ratio(items, "agent"),
                CrouchCompliance = Ratio(items, "crouch"),
                AnomalyHandling = f.AnomaliesTriggered == 0 ? (f.Passed ? 1f : 0.5f) : (f.Passed ? 1f : 0f),
                StressResilience = Clamp01(1f - f.AverageCognitiveFriction)
            };
        }

        public static string TemplateDebrief(in SessionFacts f, List<ScorecardItem> items)
        {
            var good = new List<string>();
            var improve = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.Points >= it.MaxPoints) good.Add(it.Label.ToLowerInvariant());
                else improve.Add($"{it.Label.ToLowerInvariant()} ({it.Note.ToLowerInvariant()})");
            }

            var sb = new StringBuilder();
            sb.Append(f.Passed ? "Bạn đã thoát hiểm an toàn. " : $"Buổi diễn tập chưa đạt: {f.FailCause} ");
            if (good.Count > 0) sb.Append("Điểm mạnh: ").Append(string.Join(", ", good)).Append(". ");
            if (improve.Count > 0) sb.Append("Cần cải thiện: ").Append(string.Join("; ", improve)).Append(". ");
            if (f.StandingInSmokeSeconds > 3f)
                sb.Append("Hãy rèn thói quen khom người và men theo chân tường khi khói xuống thấp, vì khí nóng gây bỏng đường hô hấp. ");
            if (f.UsedConductiveAgentOnLivePower)
                sb.Append("Tuyệt đối không dùng nước/bọt khi thiết bị còn điện: ngắt Aptomat trước, rồi dùng bình CO2. ");
            return sb.ToString().Trim();
        }

        private static ScorecardItem Item(string key, string label, int points, int max, string note) =>
            new ScorecardItem { Key = key, Label = label, Points = Math.Max(0, Math.Min(points, max)), MaxPoints = max, Note = note };

        private static float Ratio(List<ScorecardItem> items, string key)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Key == key) return items[i].MaxPoints == 0 ? 0f : (float)items[i].Points / items[i].MaxPoints;
            }
            return 0f;
        }

        private static float Since(float from, float to) => from >= 0f ? Math.Max(0f, to - from) : to;

        private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
