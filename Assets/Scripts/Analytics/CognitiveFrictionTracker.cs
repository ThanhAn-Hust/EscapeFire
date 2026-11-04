using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Scenario;

namespace EscapeFire.Analytics
{
    /// <summary>
    /// Real-time Cognitive Friction (CF) from Quest 2 kinematics only (no extra sensors):
    /// head angular velocity over a 2 s sliding window, decision hesitation (time since last safety action
    /// during the emergency) and safety mistakes (score deductions). Publishes state transitions with hysteresis.
    /// Zero allocation per frame (fixed bucket ring).
    /// </summary>
    public class CognitiveFrictionTracker : MonoBehaviour
    {
        private const int BUCKETS = 20;

        public static CognitiveFrictionTracker Instance { get; private set; }

        [SerializeField] private Transform headTransform;
        [SerializeField] private float windowSeconds = 2f;
        [Header("Weights")]
        [SerializeField] private float headWeight = 0.40f;
        [SerializeField] private float hesitationWeight = 0.35f;
        [SerializeField] private float mistakeWeight = 0.25f;
        [SerializeField] private float tauNominalDecisionSeconds = 15f;
        [SerializeField] private int maxTolerableMistakes = 4;

        private readonly float[] _buckets = new float[BUCKETS];
        private int _bucketIndex;
        private float _bucketTimer;
        private Quaternion _lastRotation;
        private bool _hasLastRotation;

        private CognitiveFrictionModel _model;
        private int _mistakes;
        private float _hesitation;
        private bool _emergencyActive;

        public float CurrentCF => _model != null ? _model.Current : 0f;
        public float AverageCF => _model != null ? _model.Average : 0f;
        public CognitiveState State => _model != null ? _model.State : CognitiveState.Nominal;
        public int Mistakes => _mistakes;
        public float HesitationSeconds => _hesitation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _model = new CognitiveFrictionModel(headWeight, hesitationWeight, mistakeWeight,
                tauNominalDecisionSeconds, maxTolerableMistakes);
        }

        private void Start()
        {
            if (headTransform == null && Camera.main != null) headTransform = Camera.main.transform;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EventBus<PhaseChangedEvent>.Subscribe(OnPhase);
            EventBus<ScoreChangedEvent>.Subscribe(OnScore);
            EventBus<AlarmActivatedEvent>.Subscribe(OnAlarm);
            EventBus<PowerStateChangedEvent>.Subscribe(OnPower);
            EventBus<ExtinguisherPinPulledEvent>.Subscribe(OnPin);
            EventBus<FireExtinguishedEvent>.Subscribe(OnExtinguished);
            EventBus<DoorHeatCheckedEvent>.Subscribe(OnDoor);
            EventBus<NpcRescuedEvent>.Subscribe(OnRescue);
        }

        private void OnDisable()
        {
            EventBus<PhaseChangedEvent>.Unsubscribe(OnPhase);
            EventBus<ScoreChangedEvent>.Unsubscribe(OnScore);
            EventBus<AlarmActivatedEvent>.Unsubscribe(OnAlarm);
            EventBus<PowerStateChangedEvent>.Unsubscribe(OnPower);
            EventBus<ExtinguisherPinPulledEvent>.Unsubscribe(OnPin);
            EventBus<FireExtinguishedEvent>.Unsubscribe(OnExtinguished);
            EventBus<DoorHeatCheckedEvent>.Unsubscribe(OnDoor);
            EventBus<NpcRescuedEvent>.Unsubscribe(OnRescue);
        }

        private void Update()
        {
            if (headTransform == null) return;

            Quaternion rot = headTransform.rotation;
            if (_hasLastRotation) _buckets[_bucketIndex] += Quaternion.Angle(_lastRotation, rot);
            _lastRotation = rot;
            _hasLastRotation = true;

            if (_emergencyActive) _hesitation += Time.deltaTime;

            _bucketTimer += Time.deltaTime;
            float bucketDuration = windowSeconds / BUCKETS;
            if (_bucketTimer < bucketDuration) return;
            _bucketTimer -= bucketDuration;

            float sum = 0f;
            for (int i = 0; i < BUCKETS; i++) sum += _buckets[i];
            float angularVelocity = sum / windowSeconds;

            _bucketIndex = (_bucketIndex + 1) % BUCKETS;
            _buckets[_bucketIndex] = 0f;

            if (_model.Sample(angularVelocity, _hesitation, _mistakes))
            {
                EventBus<CognitiveStateChangedEvent>.Publish(new CognitiveStateChangedEvent(_model.State, _model.Current));
            }
        }

        /// <summary>Any meaningful safety action ends the current hesitation streak.</summary>
        public void RegisterDecisiveAction() => _hesitation = 0f;

        public void RecordMistake() => _mistakes++;

        private void OnPhase(PhaseChangedEvent e)
        {
            _emergencyActive = e.Phase == ScenarioPhase.Phase2_ServerExplosionFire
                               || e.Phase == ScenarioPhase.Phase3_SmokeEvacuation;
            _hesitation = 0f;
        }

        private void OnScore(ScoreChangedEvent e)
        {
            if (e.Delta < 0) RecordMistake();
            else RegisterDecisiveAction();
        }

        private void OnAlarm(AlarmActivatedEvent _) => RegisterDecisiveAction();
        private void OnPower(PowerStateChangedEvent _) => RegisterDecisiveAction();
        private void OnPin(ExtinguisherPinPulledEvent _) => RegisterDecisiveAction();
        private void OnExtinguished(FireExtinguishedEvent _) => RegisterDecisiveAction();
        private void OnDoor(DoorHeatCheckedEvent _) => RegisterDecisiveAction();
        private void OnRescue(NpcRescuedEvent _) => RegisterDecisiveAction();
    }
}
