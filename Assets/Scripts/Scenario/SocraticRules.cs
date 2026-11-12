using System;

namespace EscapeFire.Scenario
{
    [Flags]
    public enum SocraticTrigger
    {
        None = 0,
        WaterNearLivePanel = 1,   // spec §4.1 #1
        StandingInSmoke = 2,      // #2
        HotHandleUnchecked = 4,   // #3
        AlarmNotRaised = 8        // #4  (#5 FM-200 is announced by FM200System itself)
    }

    /// <summary>Snapshot of what the learner is doing, gathered each frame by DeterministicVoiceTriggerManager.</summary>
    public struct SocraticInputs
    {
        public bool HoldingConductiveAgent;  // water / foam extinguisher in hand
        public bool PanelPowerOn;
        public float DistanceToPanel;
        public bool HeadInSmoke;
        public bool Crouching;
        public float HandToHandleDistance;
        public bool DoorHot;
        public bool DoorHeatChecked;
        public bool FireBurning;
        public float FireBurnSeconds;
        public bool AlarmRaised;
    }

    /// <summary>
    /// Deterministic (0 ms, no network) detection of the 5 Socratic coaching situations (spec 2026-10-08 §4.1).
    /// Each trigger has its own cooldown so the mentor never spams; the alarm reminder fires once per drill.
    /// </summary>
    public sealed class SocraticRules
    {
        public const float PANEL_DISTANCE = 2.5f;
        public const float SMOKE_STANDING_SECONDS = 3f;
        public const float HANDLE_DISTANCE = 0.4f;
        public const float ALARM_REMINDER_SECONDS = 45f;
        public const float REPEAT_COOLDOWN_SECONDS = 15f;

        private float _standingInSmoke;
        private float _lastWater = float.NegativeInfinity;
        private float _lastSmoke = float.NegativeInfinity;
        private float _lastHandle = float.NegativeInfinity;
        private bool _alarmReminderGiven;

        public SocraticTrigger Evaluate(in SocraticInputs i, float now, float deltaTime)
        {
            var fired = SocraticTrigger.None;

            if (i.HoldingConductiveAgent && i.PanelPowerOn && i.DistanceToPanel < PANEL_DISTANCE
                && now - _lastWater >= REPEAT_COOLDOWN_SECONDS)
            {
                _lastWater = now;
                fired |= SocraticTrigger.WaterNearLivePanel;
            }

            _standingInSmoke = i.HeadInSmoke && !i.Crouching ? _standingInSmoke + deltaTime : 0f;
            if (_standingInSmoke >= SMOKE_STANDING_SECONDS && now - _lastSmoke >= REPEAT_COOLDOWN_SECONDS)
            {
                _lastSmoke = now;
                fired |= SocraticTrigger.StandingInSmoke;
            }

            if (i.DoorHot && !i.DoorHeatChecked && i.HandToHandleDistance < HANDLE_DISTANCE
                && now - _lastHandle >= REPEAT_COOLDOWN_SECONDS)
            {
                _lastHandle = now;
                fired |= SocraticTrigger.HotHandleUnchecked;
            }

            if (!_alarmReminderGiven && i.FireBurning && !i.AlarmRaised && i.FireBurnSeconds > ALARM_REMINDER_SECONDS)
            {
                _alarmReminderGiven = true;
                fired |= SocraticTrigger.AlarmNotRaised;
            }

            return fired;
        }

        /// <summary>The PCCC skill a coaching situation is about (for hint fading by mastery).</summary>
        public static string SkillOf(SocraticTrigger trigger)
        {
            switch (trigger)
            {
                case SocraticTrigger.WaterNearLivePanel: return "R03_RIGHT_AGENT";
                case SocraticTrigger.StandingInSmoke: return "R07_STAY_LOW";
                case SocraticTrigger.HotHandleUnchecked: return "R08_DOOR_BACK_OF_HAND";
                case SocraticTrigger.AlarmNotRaised: return "R01_ALARM_EARLY";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// VL.5 (desirable difficulty: retrieval before telling): the first time a situation comes up the mentor only
        /// asks a question; the instruction (<see cref="LineFor"/>) comes if it happens again. A learner who has
        /// mastered the skill only ever gets the question.
        /// </summary>
        public static string QuestionFor(SocraticTrigger trigger)
        {
            switch (trigger)
            {
                case SocraticTrigger.WaterNearLivePanel:
                    return "Dừng một chút: bình bạn đang cầm có dùng được cho thiết bị còn điện không?";
                case SocraticTrigger.StandingInSmoke:
                    return "Khói đang ở ngang đầu bạn. Tư thế của bạn lúc này đã đúng chưa?";
                case SocraticTrigger.HotHandleUnchecked:
                    return "Trước khi mở cánh cửa này, bạn cần kiểm tra điều gì?";
                case SocraticTrigger.AlarmNotRaised:
                    return "Lửa đã cháy được một lúc. Những người ở phòng bên cạnh đã biết chưa?";
                default:
                    return string.Empty;
            }
        }

        /// <summary>Which line to say: question first, instruction on repeats, question only once mastered.</summary>
        public static string LineFor(SocraticTrigger trigger, int timesBefore, bool mastered) =>
            mastered || timesBefore == 0 ? QuestionFor(trigger) : LineFor(trigger);

        public static string LineFor(SocraticTrigger trigger)
        {
            switch (trigger)
            {
                case SocraticTrigger.WaterNearLivePanel:
                    return "Quan sát lại nguồn phát cháy! Nước có an toàn quanh nguồn điện đang mang tải không?";
                case SocraticTrigger.StandingInSmoke:
                    return "Khói độc đang tích tụ ở tầng cao! Hãy hạ thấp trọng tâm và khom người ngay lập tức!";
                case SocraticTrigger.HotHandleUnchecked:
                    return "Cẩn thận! Đừng vội vặn tay nắm kim loại! Hãy dùng mu bàn tay kiểm tra nhiệt độ trước đã!";
                case SocraticTrigger.AlarmNotRaised:
                    return "Đám cháy đang phát triển! Hãy kích hoạt chuông báo động để cảnh báo các phòng lân cận trước khi quá muộn!";
                default:
                    return string.Empty;
            }
        }
    }
}
