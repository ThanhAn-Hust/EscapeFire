using System;
using UnityEngine;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Player;
using EscapeFire.Analytics;
using EscapeFire.Core;

namespace EscapeFire.Scenario
{
    public enum ScenarioPhase
    {
        Phase0_PeacefulPractice,      // Peaceful practice (Fire OFF, Light Normal)
        Phase1_PowerFlickerWarning,   // Electrical spark warning (Red light flickering)
        Phase2_ServerExplosionFire,   // Fire erupts, Fire Alarm sounds, Smoke lowers
        Phase3_SmokeEvacuation,       // Fire extinguished or smoke dense, player evacuates to hallway
        Phase4_CompletedWin,          // Reached Emergency Staircase in Hallway -> WIN!
        Phase5_GameOverFail           // Suffocated or electrocuted -> FAIL
    }

    /// <summary>
    /// Master Controller driving the full VR PCCC game loop across all phases.
    /// Handles Phase 0 -> Phase 5 transitions, AI Mentor warnings, auto-wiring, and Win/Loss rules.
    /// </summary>
    public class LabProgressiveScenarioController : MonoBehaviour
    {
        public static LabProgressiveScenarioController Instance { get; private set; }

        [Header("Phase Duration Timings (Seconds)")]
        [SerializeField] private float peacefulDurationSeconds = 15f;
        [SerializeField] private float warningDurationSeconds = 10f;

        [Header("Scene Object References (Auto-discovered if null)")]
        [SerializeField] private FireHazard serverFire;
        [SerializeField] private GameObject redEmergencyLight;
        [SerializeField] private AudioSource alarmAudioSource;

        public ScenarioPhase CurrentPhase { get; private set; } = ScenarioPhase.Phase0_PeacefulPractice;
        public event Action<ScenarioPhase> OnPhaseChanged;
        public event Action<string> OnAIMentorMessage;

        private float _timer = 0f;
        private bool _hasPowerBeenCut = false;
        private bool _hasFireBeenExtinguished = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void OnEnable()
        {
            EventBus<PowerStateChangedEvent>.Subscribe(HandlePowerStateChanged);
            EventBus<FireExtinguishedEvent>.Subscribe(HandleFireExtinguished);
            EventBus<OxygenDepletedEvent>.Subscribe(HandlePlayerSuffocated);
            EventBus<PlayerDiedEvent>.Subscribe(HandlePlayerDied);
        }

        private void OnDisable()
        {
            EventBus<PowerStateChangedEvent>.Unsubscribe(HandlePowerStateChanged);
            EventBus<FireExtinguishedEvent>.Unsubscribe(HandleFireExtinguished);
            EventBus<OxygenDepletedEvent>.Unsubscribe(HandlePlayerSuffocated);
            EventBus<PlayerDiedEvent>.Unsubscribe(HandlePlayerDied);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // Fire object may already be inactive in the scene, so include inactive objects.
            if (serverFire == null) serverFire = FindFirstObjectByType<FireHazard>(FindObjectsInactive.Include);

            SetPhase(ScenarioPhase.Phase0_PeacefulPractice);
        }

        private bool IsFinished => CurrentPhase == ScenarioPhase.Phase4_CompletedWin || CurrentPhase == ScenarioPhase.Phase5_GameOverFail;

        private void Update()
        {
            if (IsFinished) return;

            _timer += Time.deltaTime;

            // Auto-advance from Phase 0 -> Phase 1 -> Phase 2
            if (CurrentPhase == ScenarioPhase.Phase0_PeacefulPractice && _timer >= peacefulDurationSeconds)
            {
                SetPhase(ScenarioPhase.Phase1_PowerFlickerWarning);
            }
            else if (CurrentPhase == ScenarioPhase.Phase1_PowerFlickerWarning && _timer >= (peacefulDurationSeconds + warningDurationSeconds))
            {
                SetPhase(ScenarioPhase.Phase2_ServerExplosionFire);
            }
        }

        public void SetPhase(ScenarioPhase newPhase)
        {
            // Terminal phases are final: suffocating after reaching the hallway must not turn a win into a fail.
            if (IsFinished) return;

            CurrentPhase = newPhase;
            Debug.Log($"[ProgressiveScenarioManager] Phase changed to: {newPhase}");

            switch (newPhase)
            {
                case ScenarioPhase.Phase0_PeacefulPractice:
                    if (serverFire != null) serverFire.gameObject.SetActive(false);
                    if (redEmergencyLight != null) redEmergencyLight.SetActive(false);
                    SetAIMentorGuidance("Chào mừng sinh viên đến buổi thực hành Lab. Hãy làm quen với các thiết bị trên bàn.");
                    break;

                case ScenarioPhase.Phase1_PowerFlickerWarning:
                    if (redEmergencyLight != null) redEmergencyLight.SetActive(true);
                    SetAIMentorGuidance("CẢNH BÁO: Tụ điện Tủ Server có dấu hiệu bốc khói! Hãy kiểm tra và ngắt Cầu dao Aptomat ngay!");
                    if (ScoringManager.Instance != null)
                        ScoringManager.Instance.AddScore(10, "Phát hiện dấu hiệu chập điện sớm");
                    break;

                case ScenarioPhase.Phase2_ServerExplosionFire:
                    if (serverFire != null)
                    {
                        serverFire.gameObject.SetActive(true);
                        serverFire.ResetFire();
                    }
                    if (alarmAudioSource != null && !alarmAudioSource.isPlaying)
                    {
                        alarmAudioSource.Play();
                    }
                    SetAIMentorGuidance("CHÁY RỒI! Bấm chuông báo cháy, ngắt Aptomat, lấy bình CO2 xịt dập lửa và khom người né khói độc!");
                    break;

                case ScenarioPhase.Phase3_SmokeEvacuation:
                    SetAIMentorGuidance("Lửa đã được khống chế! Khom người di chuyển ra Cửa chính và chạy ra Lối thoát hiểm khẩn cấp!");
                    break;

                case ScenarioPhase.Phase4_CompletedWin:
                    if (ScoringManager.Instance != null)
                    {
                        ScoringManager.Instance.AddScore(30, "Thoát hiểm an toàn ra hành lang PCCC");
                        string jsonReport = ScoringManager.Instance.ExportJSONReport();
                        Debug.Log($"[WIN] Telemetry Report:\n{jsonReport}");
                    }
                    SetAIMentorGuidance("XUẤT SẮC! Bạn đã hoàn thành xuất sắc diễn tập PCCC và thoát hiểm an toàn!");
                    break;

                case ScenarioPhase.Phase5_GameOverFail:
                    SetAIMentorGuidance("THẤT BẠI: Bạn đã vi phạm quy tắc an toàn hoặc hít quá nhiều khói độc. Hãy thử lại!");
                    break;
            }

            OnPhaseChanged?.Invoke(CurrentPhase);
            EventBus<PhaseChangedEvent>.Publish(new PhaseChangedEvent(CurrentPhase));
        }

        /// <summary>Single entry point for AI Mentor guidance (scenario phases, MicroAIGovernor, voice triggers).</summary>
        public void SetAIMentorGuidance(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            OnAIMentorMessage?.Invoke(message);
            EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(message));
        }

        private void HandlePowerStateChanged(PowerStateChangedEvent e)
        {
            if (!e.IsPowerOn && !_hasPowerBeenCut)
            {
                _hasPowerBeenCut = true;
                Debug.Log("[ProgressiveScenario] Aptomat Power Cut Off Detected!");
                if (ScoringManager.Instance != null)
                {
                    ScoringManager.Instance.AddScore(20, "Ngắt Aptomat nguồn điện tổng an toàn");
                }
            }
        }

        private void HandleFireExtinguished(FireExtinguishedEvent e)
        {
            // Only the main server fire drives the scenario; secondary fires (M2) are ignored here.
            if (serverFire != null && e.FireInstanceId != serverFire.GetInstanceID()) return;

            if (!_hasFireBeenExtinguished)
            {
                _hasFireBeenExtinguished = true;
                if (ScoringManager.Instance != null)
                {
                    ScoringManager.Instance.AddScore(25, "Dập tắt hoàn toàn đám cháy tủ server bằng bình CO2");
                }
                SetPhase(ScenarioPhase.Phase3_SmokeEvacuation);
            }
        }

        public void TriggerHallwayEvacuationWin()
        {
            SetPhase(ScenarioPhase.Phase4_CompletedWin);
        }

        private void HandlePlayerSuffocated(OxygenDepletedEvent _)
        {
            SetPhase(ScenarioPhase.Phase5_GameOverFail);
        }

        private void HandlePlayerDied(PlayerDiedEvent e)
        {
            SetPhase(ScenarioPhase.Phase5_GameOverFail);
        }
    }
}
