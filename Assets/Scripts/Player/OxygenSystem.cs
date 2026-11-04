using System;
using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Hazards;

namespace EscapeFire.Player
{
    /// <summary>
    /// Manages player oxygen: depletes only while standing with the head inside the lowering smoke layer,
    /// recovers otherwise. Logic lives in <see cref="OxygenModel"/> / <see cref="BreathingRules"/>.
    /// </summary>
    public class OxygenSystem : MonoBehaviour
    {
        [Header("Oxygen Settings")]
        [SerializeField] private float maxOxygen = 100f;
        [SerializeField] private float depletionRateInSmoke = 10f; // Oxygen lost per second when breathing smoke
        [SerializeField] private float recoveryRate = 5f;

        [Header("References")]
        [SerializeField] private VRHeightDetector heightDetector;
        [Tooltip("Smoke layer source. Auto-discovered if empty; no smoke controller = no smoke in scene.")]
        [SerializeField] private SmokeDensityController smokeController;

        private OxygenModel _model;

        public float MaxOxygen { get => maxOxygen; set => maxOxygen = value; }
        public float CurrentOxygen => _model != null ? _model.Current : maxOxygen;
        public bool IsDepleted => _model != null && _model.IsDepleted;

        public event Action<float> OnOxygenChanged;
        public event Action OnOxygenDepleted;

        private void Awake()
        {
            if (heightDetector == null)
            {
                heightDetector = GetComponent<VRHeightDetector>();
            }
            if (smokeController == null)
            {
                smokeController = FindFirstObjectByType<SmokeDensityController>();
            }
            ResetOxygen();
        }

        public void ResetOxygen()
        {
            _model = new OxygenModel(maxOxygen);
            OnOxygenChanged?.Invoke(_model.Current);
        }

        private void Update()
        {
            bool breathingSmoke = smokeController != null
                && heightDetector != null
                && heightDetector.HeadTransform != null
                && BreathingRules.IsBreathingSmoke(heightDetector.HeadTransform.position.y,
                                                   smokeController.CurrentSmokeHeight,
                                                   heightDetector.IsCrouching);

            float before = _model.Current;
            bool justDepleted = _model.Tick(Time.deltaTime, breathingSmoke, depletionRateInSmoke, recoveryRate);

            if (!Mathf.Approximately(before, _model.Current))
            {
                OnOxygenChanged?.Invoke(_model.Current);
            }
            if (justDepleted)
            {
                RaiseDepleted();
            }
        }

        public void DepleteOxygen(float amount)
        {
            bool justDepleted = _model.Deplete(amount);
            OnOxygenChanged?.Invoke(_model.Current);
            if (justDepleted)
            {
                RaiseDepleted();
            }
        }

        public void RecoverOxygen(float amount)
        {
            _model.Recover(amount);
            OnOxygenChanged?.Invoke(_model.Current);
        }

        private void RaiseDepleted()
        {
            OnOxygenDepleted?.Invoke();
            EventBus<OxygenDepletedEvent>.Publish(default);
        }
    }
}
