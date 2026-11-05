namespace EscapeFire.Scenario
{
    /// <summary>Safe directive action space (spec 2026-10-08 §2.1). LLM / local governor may only pick these.</summary>
    public enum Directive
    {
        MaintainCurrentPace,
        EscalateSecondaryFire,
        TriggerCo2PinJam,
        TriggerAlarmCableCut,
        EscalateFm200Countdown,
        TriggerSmokeSurge,               // tier 1: smoke layer suddenly drops 0.8 m and thickens faster
        TriggerBlackout,                 // tier 2: normal lights fail, only emergency + EXIT lights remain
        TriggerExtinguisherLowPressure   // tier 3: CO2 extinguisher has only a few seconds of agent left
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
        RejectedAlreadyActive,
        RejectedPrerequisiteMissing,
        RejectedTierTooHigh
    }

    /// <summary>
    /// Progressive difficulty: the n-th challenge of a drill may only come from tiers 1..n
    /// (both the local governor and the LLM edge server enforce this).
    /// </summary>
    public static class DirectiveTiers
    {
        public const int MAX_TIER = 4;

        public static int TierOf(Directive d)
        {
            switch (d)
            {
                case Directive.TriggerCo2PinJam:
                case Directive.TriggerSmokeSurge:
                    return 1;
                case Directive.TriggerAlarmCableCut:
                case Directive.TriggerBlackout:
                    return 2;
                case Directive.EscalateSecondaryFire:
                case Directive.TriggerExtinguisherLowPressure:
                    return 3;
                case Directive.EscalateFm200Countdown:
                    return 4;
                default:
                    return 0;
            }
        }

        /// <summary>Highest tier allowed for the next challenge given how many were already applied.</summary>
        public static int AllowedTier(int challengesSoFar) => System.Math.Min(challengesSoFar + 1, MAX_TIER);
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
                case Directive.TriggerSmokeSurge: return "TRIGGER_SMOKE_SURGE";
                case Directive.TriggerBlackout: return "TRIGGER_BLACKOUT";
                case Directive.TriggerExtinguisherLowPressure: return "TRIGGER_EXTINGUISHER_LOW_PRESSURE";
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
                case "TRIGGER_SMOKE_SURGE": directive = Directive.TriggerSmokeSurge; return true;
                case "TRIGGER_BLACKOUT": directive = Directive.TriggerBlackout; return true;
                case "TRIGGER_EXTINGUISHER_LOW_PRESSURE": directive = Directive.TriggerExtinguisherLowPressure; return true;
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
        public int ChallengesSoFar;           // accepted challenges this drill (tier gating)
        public bool SmokeCanSurge;            // smoke layer exists and is still high enough to drop
        public bool SmokeSurgeApplied;
        public bool BlackoutActive;
        public bool PowerAlreadyCut;          // breaker off: lights are already out, a blackout adds nothing
        public bool HasCo2Extinguisher;
        public bool LowPressureApplied;
        public bool FireBurning;
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
            if (DirectiveTiers.TierOf(directive) > DirectiveTiers.AllowedTier(ctx.ChallengesSoFar))
                return DirectiveResult.RejectedTierTooHigh;

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

                case Directive.TriggerSmokeSurge:
                    if (ctx.SmokeSurgeApplied) return DirectiveResult.RejectedAlreadyActive;
                    if (!ctx.SmokeCanSurge) return DirectiveResult.RejectedPrerequisiteMissing;
                    return DirectiveResult.Accepted;

                case Directive.TriggerBlackout:
                    if (ctx.BlackoutActive) return DirectiveResult.RejectedAlreadyActive;
                    if (ctx.PowerAlreadyCut) return DirectiveResult.RejectedPrerequisiteMissing;
                    return DirectiveResult.Accepted;

                case Directive.TriggerExtinguisherLowPressure:
                    if (ctx.LowPressureApplied) return DirectiveResult.RejectedAlreadyActive;
                    if (!ctx.HasCo2Extinguisher || !ctx.FireBurning) return DirectiveResult.RejectedPrerequisiteMissing;
                    return DirectiveResult.Accepted;

                default:
                    return DirectiveResult.RejectedUnknownDirective;
            }
        }
    }
}
