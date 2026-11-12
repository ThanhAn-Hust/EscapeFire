using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EscapeFire.Analytics;
using EscapeFire.Core;
using EscapeFire.Interactions;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Connects the headset to the laptop edge AI Director over LAN WebSocket.
    /// Sends telemetry every <see cref="telemetryIntervalSeconds"/>, applies returned macro directives through
    /// <see cref="MicroAIGovernor.TryExecute(string,string)"/> (Tier-2 validation), and reports the outcome back.
    /// While connected the local escalation loop is paused; on disconnect it resumes automatically (offline fallback).
    /// Network I/O runs off the main thread; Unity objects are only touched in Update.
    /// </summary>
    public class EdgeDirectorClient : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "ws://192.168.1.100:8765/session";
        [SerializeField] private float telemetryIntervalSeconds = 10f;
        [SerializeField] private float reconnectDelaySeconds = 5f;
        [SerializeField] private bool enabledInGuidedMode = false;
        [SerializeField] private float serverZoneRadius = 3f;

        private readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _outbox = new ConcurrentQueue<string>();
        private readonly List<string> _priorChallenges = new List<string>();
        private int _acceptedChallenges;
        private CancellationTokenSource _cts;
        private volatile bool _connected;
        private bool _wasConnected;
        private float _nextTelemetry;
        private float _startTime;

        private CircuitBreakerSwitch _breaker;
        private ExtinguisherXRBinding[] _extinguishers;

        public bool IsConnected => _connected;

        private bool Enabled => enabledInGuidedMode || GameSession.Mode == TrainingMode.Exam;

        private void Start()
        {
            _startTime = Time.time;
            // Even when adaptive play is off (Guided), the edge server still serves the AI debrief and mock LMS.
            GameSession.EdgeHttpBase = ToHttpBase(serverUrl);
            if (!LmsSync.UseCustomEndpoint) LmsSync.BaseUrl = GameSession.EdgeHttpBase;
            _breaker = FindFirstObjectByType<CircuitBreakerSwitch>();
            _extinguishers = FindObjectsByType<ExtinguisherXRBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (!Enabled) return;
            Connect(serverUrl);
        }

        /// <summary>(Re)connects to an edge server, e.g. from a settings menu or a test. Drops any previous connection.</summary>
        public void Connect(string url)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            serverUrl = url;
            GameSession.EdgeHttpBase = ToHttpBase(url);
            if (!LmsSync.UseCustomEndpoint) LmsSync.BaseUrl = GameSession.EdgeHttpBase; // edge server doubles as mock LMS
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _ = Task.Run(() => ConnectionLoop(url, token));
        }

        private float _emergencyStart = -1f;

        private void OnEnable()
        {
            EventBus<DirectiveResolvedEvent>.Subscribe(OnDirectiveResolved);
            EventBus<PhaseChangedEvent>.Subscribe(OnPhaseChanged);
        }

        private void OnDisable()
        {
            EventBus<DirectiveResolvedEvent>.Unsubscribe(OnDirectiveResolved);
            EventBus<PhaseChangedEvent>.Unsubscribe(OnPhaseChanged);
        }

        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.Phase == ScenarioPhase.Phase2_ServerExplosionFire && _emergencyStart < 0f) _emergencyStart = Time.time;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            SetLocalEscalation(true);
        }

        private void Update()
        {
            if (_connected != _wasConnected)
            {
                _wasConnected = _connected;
                SetLocalEscalation(!_connected);
                Debug.Log(_connected
                    ? $"[EdgeDirector] Connected to {serverUrl}; local escalation paused."
                    : "[EdgeDirector] Disconnected; local Micro-AI fallback active.");
            }

            while (_inbox.TryDequeue(out string message))
            {
                HandleServerMessage(message);
            }

            if (_connected && Time.time >= _nextTelemetry)
            {
                _nextTelemetry = Time.time + telemetryIntervalSeconds;
                _outbox.Enqueue(EdgeProtocol.SerializeTelemetry(BuildTelemetry()));
            }
        }

        private void HandleServerMessage(string message)
        {
            if (EdgeProtocol.TryParseChallenge(message, out var challenge))
            {
                var gov = MicroAIGovernor.Instance;
                string why = gov != null ? gov.TryExecuteChallenge(challenge, "edge") : "no_governor";
                if (why == null) _acceptedChallenges++;
                Debug.Log($"[EdgeDirector] challenge ({(string)challenge["pccc_rule_reference"]}) -> {why ?? "Accepted"}");
                _outbox.Enqueue(EdgeProtocol.SerializeResult("CHALLENGE", why == null ? DirectiveResult.Accepted : DirectiveResult.RejectedPrerequisiteMissing));
                return;
            }
            if (!EdgeProtocol.TryParseDirective(message, out var reply)) return;
            if (reply.Directive == DirectiveCodes.ToCode(Directive.MaintainCurrentPace)) return;

            var governor = MicroAIGovernor.Instance;
            DirectiveResult result = governor != null
                ? governor.TryExecute(reply.Directive, "edge")
                : DirectiveResult.RejectedWrongPhase;

            Debug.Log($"[EdgeDirector] {reply.Directive} ({reply.PcccRuleReference}, {reply.LatencyMs:F0}ms) -> {result}");
            _outbox.Enqueue(EdgeProtocol.SerializeResult(reply.Directive, result));
        }

        private void OnDirectiveResolved(DirectiveResolvedEvent e)
        {
            if (e.Directive == Directive.MaintainCurrentPace || !CountsAsUsed(e.Result)) return;
            string code = DirectiveCodes.ToCode(e.Directive);
            if (_priorChallenges.Contains(code)) return;
            _priorChallenges.Add(code);
            if (e.Result == DirectiveResult.Accepted) _acceptedChallenges++;
        }

        /// <summary>
        /// Accepted challenges, and ones the headset can never accept in this drill (alarm already raised, pin already
        /// out, already active, prerequisite gone), are reported as used so the LLM stops proposing them again.
        /// Transient rejections (panicking learner, FPS budget, wrong phase) stay retryable.
        /// </summary>
        public static bool CountsAsUsed(DirectiveResult result) =>
            result == DirectiveResult.Accepted ||
            result == DirectiveResult.RejectedAlarmAlreadyRaised ||
            result == DirectiveResult.RejectedAlreadyUnpinned ||
            result == DirectiveResult.RejectedAlreadyActive ||
            result == DirectiveResult.RejectedPrerequisiteMissing;

        private List<string> _weak;
        /// <summary>VL.3: the learner's weakest PCCC skills from earlier sessions (read once per drill).</summary>
        private List<string> WeakSkills => _weak ?? (_weak = EscapeFire.Analytics.SkillStore.WeakSkills(3));

        private TelemetryPayload BuildTelemetry()
        {
            var controller = LabProgressiveScenarioController.Instance;
            var tracker = CognitiveFrictionTracker.Instance;
            var payload = new TelemetryPayload
            {
                SessionId = GameSession.SessionId,
                TimeElapsed = Time.time - _startTime,
                CognitiveFriction = tracker != null ? tracker.CurrentCF : 0.5f,
                IsPowerCut = _breaker != null && !_breaker.IsPowerOn,
                HeldItem = DescribeHeldItem(),
                CurrentZone = DescribeZone(controller),
                Phase = controller != null ? controller.CurrentPhase.ToString() : "",
                EmergencyElapsed = _emergencyStart >= 0f ? Time.time - _emergencyStart : 0f
            };
            payload.PriorChallenges.AddRange(_priorChallenges);
            payload.AcceptedChallenges = _acceptedChallenges;
            payload.WeakSkills.AddRange(WeakSkills);
            var runner = ChallengeRunner.Instance;
            if (runner != null)
            {
                payload.PriorPieces.AddRange(runner.AppliedCodes);
                payload.BlockedRoutes.AddRange(runner.BlockedRoutes);
            }
            return payload;
        }

        private string DescribeHeldItem()
        {
            for (int i = 0; _extinguishers != null && i < _extinguishers.Length; i++)
            {
                var binding = _extinguishers[i];
                if (binding == null || !binding.IsHeld) continue;
                switch (binding.Extinguisher.Type)
                {
                    case ExtinguisherType.CO2: return "CO2_Extinguisher";
                    case ExtinguisherType.WaterFoam: return "Water_Extinguisher";
                    default: return "Powder_Extinguisher";
                }
            }
            return "None";
        }

        private string DescribeZone(LabProgressiveScenarioController controller)
        {
            var cam = Camera.main;
            if (cam == null || controller == null || controller.ServerFire == null) return "Unknown";
            float distance = Vector3.Distance(cam.transform.position, controller.ServerFire.transform.position);
            return distance <= serverZoneRadius ? "ServerZone" : "WorkstationZone";
        }

        /// <summary>ws://host:port/session → http://host:port</summary>
        public static string ToHttpBase(string wsUrl)
        {
            if (!Uri.TryCreate(wsUrl, UriKind.Absolute, out var uri)) return string.Empty;
            string scheme = uri.Scheme == "wss" ? "https" : "http";
            return $"{scheme}://{uri.Host}:{uri.Port}";
        }

        private static void SetLocalEscalation(bool enabled)
        {
            if (MicroAIGovernor.Instance != null) MicroAIGovernor.Instance.LocalEscalationEnabled = enabled;
        }

        // ------------------------------------------------------------------ background thread

        private async Task ConnectionLoop(string url, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (var socket = new ClientWebSocket())
                {
                    try
                    {
                        await socket.ConnectAsync(new Uri(url), token);
                        _connected = true;
                        var receive = ReceiveLoop(socket, token);
                        var send = SendLoop(socket, token);
                        await Task.WhenAny(receive, send);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // Server unreachable / dropped: fall through to reconnect. Local governor keeps working.
                    }
                    finally
                    {
                        _connected = false;
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ReceiveLoop(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[8192];
            var builder = new StringBuilder();
            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close) return;
                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage) continue;
                _inbox.Enqueue(builder.ToString());
                builder.Clear();
            }
        }

        private async Task SendLoop(ClientWebSocket socket, CancellationToken token)
        {
            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                if (_outbox.TryDequeue(out string message))
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                }
                else
                {
                    await Task.Delay(50, token);
                }
            }
        }
    }
}
