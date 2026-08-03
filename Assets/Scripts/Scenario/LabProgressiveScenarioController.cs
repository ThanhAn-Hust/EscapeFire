using System;
using UnityEngine;
using EscapeFire.Hazards;

namespace EscapeFire.Scenario
{
    public enum ScenarioPhase
    {
        Phase0_PeacefulPractice,
        Phase1_PowerFlickerWarning,
        Phase2_ServerExplosionFire,
        Phase3_SmokeEvacuation
    }

    /// <summary>
    /// Controls progressive phase flow from peaceful lab practice (Phase 0)
    /// to emergency fire and evacuation (Phase 1-3).
    /// </summary>
    public class LabProgressiveScenarioController : MonoBehaviour
    {
        [Header("Phase Timings")]
        [SerializeField] private float peacefulDurationSeconds = 30f;
        [SerializeField] private float warningDurationSeconds = 10f;

        [Header("References")]
        [SerializeField] private FireHazard serverFire;
        [SerializeField] private GameObject redEmergencyLight;
        [SerializeField] private AudioSource alarmAudioSource;

        public ScenarioPhase CurrentPhase { get; private set; } = ScenarioPhase.Phase0_PeacefulPractice;
        public event Action<ScenarioPhase> OnPhaseChanged;

        private float _timer = 0f;

        private void Start()
        {
            SetPhase(ScenarioPhase.Phase0_PeacefulPractice);
        }

        private void Update()
        {
            _timer += Time.deltaTime;

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
            CurrentPhase = newPhase;
            Debug.Log($"[ProgressiveScenario] Advanced to phase: {newPhase}");

            switch (newPhase)
            {
                case ScenarioPhase.Phase0_PeacefulPractice:
                    if (serverFire != null) serverFire.gameObject.SetActive(false);
                    if (redEmergencyLight != null) redEmergencyLight.SetActive(false);
                    break;

                case ScenarioPhase.Phase1_PowerFlickerWarning:
                    if (redEmergencyLight != null) redEmergencyLight.SetActive(true);
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
                    break;
            }

            OnPhaseChanged?.Invoke(CurrentPhase);
        }
    }
}
