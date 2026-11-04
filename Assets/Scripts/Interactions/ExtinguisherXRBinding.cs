using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Core;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Bridges XRI 3.x grab/activate input to <see cref="FireExtinguisher"/>:
    /// Grip = hold, Trigger (activate) = spray, deactivate = stop.
    /// The safety pin is pulled via <see cref="PullPin"/> (wire it to the pin's XRSimpleInteractable selectEntered),
    /// or automatically on first trigger when <see cref="autoPullPinOnFirstTrigger"/> is set (Guided mode helper).
    /// </summary>
    [RequireComponent(typeof(FireExtinguisher))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ExtinguisherXRBinding : MonoBehaviour
    {
        [SerializeField] private CircuitBreakerSwitch circuitBreaker;
        [SerializeField] private bool autoPullPinOnFirstTrigger = false;
        [Tooltip("Hangs on its wall bracket (kinematic) until first grabbed; physics only after the learner lets go.")]
        [SerializeField] private bool mountedOnWall = true;

        [Header("Haptics")]
        [SerializeField] private float sprayHapticAmplitude = 0.25f;
        [SerializeField] private float shockHapticAmplitude = 1.0f;
        [SerializeField] private float shockHapticDuration = 0.6f;

        private FireExtinguisher _extinguisher;
        private XRGrabInteractable _grab;
        private XRBaseInputInteractor _holder;
        private Rigidbody _body;

        public bool IsHeld => _grab != null && _grab.isSelected;
        public FireExtinguisher Extinguisher => _extinguisher;

        private void Awake()
        {
            _extinguisher = GetComponent<FireExtinguisher>();
            _grab = GetComponent<XRGrabInteractable>();
            _body = GetComponent<Rigidbody>();
            if (_body != null && mountedOnWall)
            {
                _body.isKinematic = true; // stay on the bracket instead of dropping to the floor at Play
                _body.interpolation = RigidbodyInterpolation.Interpolate;
            }
            if (circuitBreaker == null) circuitBreaker = FindFirstObjectByType<CircuitBreakerSwitch>();
        }

        private void OnEnable()
        {
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);
            _grab.activated.AddListener(OnTriggerPressed);
            _grab.deactivated.AddListener(OnTriggerReleased);
        }

        private void OnDisable()
        {
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
            _grab.activated.RemoveListener(OnTriggerPressed);
            _grab.deactivated.RemoveListener(OnTriggerReleased);
            _extinguisher.StopSpray();
        }

        /// <summary>Hook for the safety pin interactable (UnityEvent friendly).</summary>
        public void PullPin()
        {
            _extinguisher.PullSafetyPin();
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _holder = args.interactorObject as XRBaseInputInteractor;
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            _extinguisher.StopSpray();
            _holder = null;
            // XRI restores the pre-grab kinematic state on release; once taken off the wall it obeys gravity.
            if (_body != null && mountedOnWall)
            {
                _body.isKinematic = false;
                _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }

        private void OnTriggerPressed(ActivateEventArgs args)
        {
            if (!_extinguisher.IsPinPulled && autoPullPinOnFirstTrigger && GameSession.Mode == TrainingMode.Guided)
            {
                _extinguisher.PullSafetyPin();
            }

            bool powerLive = circuitBreaker != null && circuitBreaker.IsPowerOn;
            bool spraying = _extinguisher.TrySpray(powerLive);

            if (_holder == null) return;
            if (spraying) _holder.SendHapticImpulse(sprayHapticAmplitude, 0.1f);
            else if (_extinguisher.Type == ExtinguisherType.WaterFoam && powerLive)
                _holder.SendHapticImpulse(shockHapticAmplitude, shockHapticDuration);
        }

        private void OnTriggerReleased(DeactivateEventArgs args)
        {
            _extinguisher.StopSpray();
        }
    }
}
