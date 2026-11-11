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

        public static SessionFacts Empty => new SessionFacts
        {
            FailCause = string.Empty,
            FireStartedAt = -1f,
            AlarmRaisedAt = -1f,
            PowerCutAt = -1f,
            FirstSprayAt = -1f,
            PassBaseRatio = -1f,
            PassSweepRatio = -1f
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
            var items = new List<ScorecardItem>(6);

            bool alarm = f.AlarmRaisedAt >= 0f;
            items.Add(Item("alarm", "Kích hoạt báo động sớm", alarm ? 10 : 0, 10,
                alarm ? $"Báo động sau {Since(f.FireStartedAt, f.AlarmRaisedAt):F0}s kể từ khi cháy" : "Không kích hoạt báo động"));

            bool cutBeforeSpray = f.PowerCutAt >= 0f && (f.FirstSprayAt < 0f || f.PowerCutAt <= f.FirstSprayAt);
            items.Add(Item("power_cut", "Ngắt cầu dao trước khi dập", cutBeforeSpray ? 20 : (f.PowerCutAt >= 0f ? 8 : 0), 20,
                cutBeforeSpray ? "Ngắt Aptomat trước khi xịt" : (f.PowerCutAt >= 0f ? "Ngắt điện nhưng sau khi đã xịt" : "Không ngắt Aptomat")));

            bool rightAgent = f.FirstSprayAt >= 0f && !f.FirstSprayWithConductiveAgent && !f.UsedConductiveAgentOnLivePower;
            items.Add(Item("agent", "Chọn đúng chủng loại bình (CO2 cho điện)", rightAgent ? 20 : 0, 20,
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

            int crouch = Math.Max(0, 20 - (int)Math.Ceiling(f.StandingInSmokeSeconds * CROUCH_PENALTY_PER_SECOND));
            items.Add(Item("crouch", "Duy trì khom lưng dưới khói", crouch, 20,
                f.StandingInSmokeSeconds > 0.5f ? $"Đứng thẳng trong khói {f.StandingInSmokeSeconds:F0} giây" : "Luôn giữ tư thế thấp"));

            items.Add(Item("rescue", "Hỗ trợ đồng đội (cứu nạn NPC)", f.NpcRescued ? 15 : 0, 15,
                f.NpcRescued ? "Đã đưa bạn Hoa ra khỏi vùng khói" : "Chưa hỗ trợ bạn Hoa"));

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

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
