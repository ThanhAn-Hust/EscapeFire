using EscapeFire.Analytics;
using EscapeFire.Interactions;
using EscapeFire.NPC;
using EscapeFire.Scenario;

namespace EscapeFire.Core
{
    public readonly struct PhaseChangedEvent
    {
        public readonly ScenarioPhase Phase;
        public PhaseChangedEvent(ScenarioPhase phase) { Phase = phase; }
    }

    public readonly struct MentorMessageEvent
    {
        public readonly string Message;
        public MentorMessageEvent(string message) { Message = message; }
    }

    public readonly struct PowerStateChangedEvent
    {
        public readonly bool IsPowerOn;
        public PowerStateChangedEvent(bool isPowerOn) { IsPowerOn = isPowerOn; }
    }

    public readonly struct FireExtinguishedEvent
    {
        public readonly int FireInstanceId;
        public FireExtinguishedEvent(int fireInstanceId) { FireInstanceId = fireInstanceId; }
    }

    public readonly struct AlarmActivatedEvent { }

    public readonly struct ExtinguisherPinPulledEvent
    {
        public readonly ExtinguisherType Type;
        public ExtinguisherPinPulledEvent(ExtinguisherType type) { Type = type; }
    }

    public readonly struct ShockHazardEvent
    {
        public readonly ExtinguisherType Type;
        public ShockHazardEvent(ExtinguisherType type) { Type = type; }
    }

    public readonly struct CrouchStateChangedEvent
    {
        public readonly bool IsCrouching;
        public readonly float Height;
        public CrouchStateChangedEvent(bool isCrouching, float height) { IsCrouching = isCrouching; Height = height; }
    }

    public readonly struct OxygenDepletedEvent { }

    public readonly struct PlayerDiedEvent
    {
        public readonly string Cause;
        public PlayerDiedEvent(string cause) { Cause = cause; }
    }

    public readonly struct ScoreChangedEvent
    {
        public readonly int Delta;
        public readonly int Total;
        public readonly string Reason;
        public ScoreChangedEvent(int delta, int total, string reason) { Delta = delta; Total = total; Reason = reason; }
    }

    public readonly struct NpcRescuedEvent
    {
        public readonly NPCRole Role;
        public NpcRescuedEvent(NPCRole role) { Role = role; }
    }

    public readonly struct DoorHeatCheckedEvent
    {
        public readonly bool WasHot;
        public DoorHeatCheckedEvent(bool wasHot) { WasHot = wasHot; }
    }

    public readonly struct CognitiveStateChangedEvent
    {
        public readonly CognitiveState State;
        public readonly float CognitiveFriction;
        public CognitiveStateChangedEvent(CognitiveState state, float cf) { State = state; CognitiveFriction = cf; }
    }

    public readonly struct DirectiveResolvedEvent
    {
        public readonly Directive Directive;
        public readonly DirectiveResult Result;
        public readonly string Source;
        public DirectiveResolvedEvent(Directive directive, DirectiveResult result, string source)
        {
            Directive = directive; Result = result; Source = source;
        }
    }

    public readonly struct VoiceCommandEvent
    {
        public readonly VoiceCommand Command;
        public readonly string Transcript;
        public VoiceCommandEvent(VoiceCommand command, string transcript) { Command = command; Transcript = transcript; }
    }

    public readonly struct ExtinguisherSprayEvent
    {
        public readonly ExtinguisherType Type;
        public readonly bool PowerLive;
        public ExtinguisherSprayEvent(ExtinguisherType type, bool powerLive) { Type = type; PowerLive = powerLive; }
    }

    public enum Fm200Stage { Started, Tick, Discharged }

    public readonly struct Fm200CountdownEvent
    {
        public readonly Fm200Stage Stage;
        public readonly float RemainingSeconds;
        public Fm200CountdownEvent(Fm200Stage stage, float remaining) { Stage = stage; RemainingSeconds = remaining; }
    }
}
