namespace EscapeFire.Scenario
{
    /// <summary>Safe directive action space (spec 2026-10-08 §2.1). LLM / local governor may only pick these.</summary>
    public enum Directive
    {
        MaintainCurrentPace,
        EscalateSecondaryFire,
        TriggerCo2PinJam,
        TriggerAlarmCableCut,
        EscalateFm200Countdown
    }

    public enum DirectiveResult
    {
        Accepted,
        RejectedUnknownDirective,
        RejectedWrongPhase,
        RejectedLearnerPanicking,
        RejectedBudgetOverflow,
        RejectedTrappedPlayer,
        RejectedAlreadyUnpinned,
        RejectedAlarmAlreadyRaised,
        RejectedAlreadyActive
    }

    public static class DirectiveCodes
    {
        public static string ToCode(Directive d)
        {
            switch (d)
            {
                case Directive.EscalateSecondaryFire: return "ESCALATE_SECONDARY_FIRE";
                case Directive.TriggerCo2PinJam: return "TRIGGER_CO2_PIN_JAM";
                case Directive.TriggerAlarmCableCut: return "TRIGGER_ALARM_CABLE_CUT";
                case Directive.EscalateFm200Countdown: return "ESCALATE_FM200_COUNTDOWN";
                default: return "MAINTAIN_CURRENT_PACE";
            }
        }

        /// <summary>Parses the wire code used by the edge server (case-insensitive, trimmed).</summary>
        public static bool TryParse(string code, out Directive directive)
        {
            directive = Directive.MaintainCurrentPace;
            if (string.IsNullOrWhiteSpace(code)) return false;
            switch (code.Trim().ToUpperInvariant())
            {
                case "MAINTAIN_CURRENT_PACE": directive = Directive.MaintainCurrentPace; return true;
                case "ESCALATE_SECONDARY_FIRE": directive = Directive.EscalateSecondaryFire; return true;
                case "TRIGGER_CO2_PIN_JAM": directive = Directive.TriggerCo2PinJam; return true;
                case "TRIGGER_ALARM_CABLE_CUT": directive = Directive.TriggerAlarmCableCut; return true;
                case "ESCALATE_FM200_COUNTDOWN": directive = Directive.EscalateFm200Countdown; return true;
                default: return false;
            }
        }
    }

    /// <summary>Snapshot of scene state needed by the Tier-2 validator (filled by MicroAIGovernor).</summary>
    public struct DirectiveContext
    {
        public bool EmergencyActive;          // Phase 2/3 and not finished
        public bool LearnerPanicking;         // CF state == Panic
        public int ActiveFires;
        public int MaxFires;
        public bool HasSafeSecondarySlot;     // a free slot that does not block the only exit
        public bool AnyExtinguisherUnpinned;  // pin jam would contradict the scene
        public bool AlarmAlreadyRaised;
        public bool AlarmCableAlreadyCut;
        public bool PinJamAlreadyApplied;
        public bool Fm200Active;
    }

    /// <summary>
    /// Tier-2 PCCC safety &amp; hardware validator (spec §3.1): hardware budget, life-safety invariant,
    /// prerequisite state. Pure logic so it is unit-tested and identical for local and LLM directives.
    /// </summary>
    public static class DirectiveValidator
    {
        public static DirectiveResult Validate(Directive directive, in DirectiveContext ctx)
        {
            if (directive == Directive.MaintainCurrentPace) return DirectiveResult.Accepted;
            if (!ctx.EmergencyActive) return DirectiveResult.RejectedWrongPhase;
            if (ctx.LearnerPanicking) return DirectiveResult.RejectedLearnerPanicking;

            switch (directive)
            {
                case Directive.EscalateSecondaryFire:
                    if (ctx.ActiveFires >= ctx.MaxFires) return DirectiveResult.RejectedBudgetOverflow;
                    if (!ctx.HasSafeSecondarySlot) return DirectiveResult.RejectedTrappedPlayer;
                    return DirectiveResult.Accepted;

                case Directive.TriggerCo2PinJam:
                    if (ctx.PinJamAlreadyApplied) return DirectiveResult.RejectedAlreadyActive;
                    if (ctx.AnyExtinguisherUnpinned) return DirectiveResult.RejectedAlreadyUnpinned;
                    return DirectiveResult.Accepted;

                case Directive.TriggerAlarmCableCut:
                    if (ctx.AlarmCableAlreadyCut) return DirectiveResult.RejectedAlreadyActive;
                    if (ctx.AlarmAlreadyRaised) return DirectiveResult.RejectedAlarmAlreadyRaised;
                    return DirectiveResult.Accepted;

                case Directive.EscalateFm200Countdown:
                    if (ctx.Fm200Active) return DirectiveResult.RejectedAlreadyActive;
                    return DirectiveResult.Accepted;

                default:
                    return DirectiveResult.RejectedUnknownDirective;
            }
        }
    }
}
