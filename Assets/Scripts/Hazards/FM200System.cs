using System.Collections;
using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Player;
using EscapeFire.Scenario;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// Server-room clean-agent suppression (FM-200 / NFPA 2001). Rule: if the server fire burns longer than
    /// <see cref="autoTriggerAfterBurnSeconds"/>, a 30 s evacuation countdown starts; on discharge the room
    /// becomes unbreathable and anyone still inside dies. Can also be triggered by the adaptive governor.
    /// </summary>
    public class FM200System : MonoBehaviour
    {
        public static FM200System Instance { get; private set; }

        [SerializeField] private float countdownSeconds = 30f;
        [SerializeField] private float autoTriggerAfterBurnSeconds = 90f;
        [SerializeField] private FireHazard watchedFire;
        [SerializeField] private AudioSource sirenAudio;
        [SerializeField] private GameObject dischargeVfx;

        private float _burnTime;
        private int _lastAnnouncedSecond = -1;

        public bool IsCountdownActive { get; private set; }
        public bool HasDischarged { get; private set; }
        public float RemainingSeconds { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            RemainingSeconds = countdownSeconds;
        }

        private void Start()
        {
            if (watchedFire == null)
            {
                var controller = LabProgressiveScenarioController.Instance;
                watchedFire = controller != null ? controller.ServerFire : null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (IsCountdownActive || HasDischarged || watchedFire == null) return;
            if (!watchedFire.isActiveAndEnabled || watchedFire.IsExtinguished) return;

            _burnTime += Time.deltaTime;
            if (_burnTime >= autoTriggerAfterBurnSeconds) TriggerCountdown();
        }

        /// <summary>V4 FM200 piece: countdown of a given length (catalog bounds 20-45 s).</summary>
        public void TriggerCountdown(float seconds)
        {
            if (IsCountdownActive || HasDischarged) return;
            countdownSeconds = Mathf.Clamp(seconds, 10f, 60f);
            TriggerCountdown();
        }

        public void TriggerCountdown()
        {
            if (IsCountdownActive || HasDischarged) return;
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            IsCountdownActive = true;
            RemainingSeconds = countdownSeconds;
            if (sirenAudio != null)
            {
                sirenAudio.loop = true;
                sirenAudio.Play();
            }
            EventBus<Fm200CountdownEvent>.Publish(new Fm200CountdownEvent(Fm200Stage.Started, RemainingSeconds));
            EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(
                "Hệ thống xả khí ngạt FM-200 đã đếm ngược! Ngừng dập lửa và chạy ngay ra hành lang trước khi hết oxy trong phòng!"));

            while (RemainingSeconds > 0f)
            {
                yield return null;
                RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
                int whole = Mathf.CeilToInt(RemainingSeconds);
                if (whole != _lastAnnouncedSecond)
                {
                    _lastAnnouncedSecond = whole;
                    EventBus<Fm200CountdownEvent>.Publish(new Fm200CountdownEvent(Fm200Stage.Tick, RemainingSeconds));
                }
            }

            IsCountdownActive = false;
            HasDischarged = true;
            if (dischargeVfx != null) dischargeVfx.SetActive(true);
            EventBus<Fm200CountdownEvent>.Publish(new Fm200CountdownEvent(Fm200Stage.Discharged, 0f));

            // Win is final in the scenario controller, so this only kills a player still in the room.
            var health = FindFirstObjectByType<HealthSystem>();
            if (health != null) health.TakeDamage(health.MaxHealth * 10f, "Ngạt khí FM-200: không sơ tán kịp trong 30 giây.");
        }
    }
}
