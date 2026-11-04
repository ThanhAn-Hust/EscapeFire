using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;
using EscapeFire.Player;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Local, deterministic Socratic coach (spec §4): samples the learner's state each frame during the emergency,
    /// evaluates <see cref="SocraticRules"/> and sends the matching line to the AI Mentor (HUD + voice). No network.
    /// </summary>
    public class DeterministicVoiceTriggerManager : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private CircuitBreakerSwitch electricalPanel;
        [SerializeField] private DoorHeatChecker exitDoor;
        [SerializeField] private SmokeDensityController smoke;
        [SerializeField] private VRHeightDetector heightDetector;

        private readonly SocraticRules _rules = new SocraticRules();
        private ExtinguisherXRBinding[] _extinguishers;
        private FireAlarmButton[] _alarms;
        private float _fireBurnSeconds;

        private void Start()
        {
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (leftHand == null) leftHand = FindTransform("Left Controller", "LeftHand Controller", "Left Hand");
            if (rightHand == null) rightHand = FindTransform("Right Controller", "RightHand Controller", "Right Hand");
            if (electricalPanel == null) electricalPanel = FindFirstObjectByType<CircuitBreakerSwitch>();
            if (exitDoor == null) exitDoor = FindFirstObjectByType<DoorHeatChecker>();
            if (smoke == null) smoke = FindFirstObjectByType<SmokeDensityController>();
            if (heightDetector == null) heightDetector = FindFirstObjectByType<VRHeightDetector>();
            _extinguishers = FindObjectsByType<ExtinguisherXRBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _alarms = FindObjectsByType<FireAlarmButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private void Update()
        {
            var controller = LabProgressiveScenarioController.Instance;
            if (controller == null || head == null) return;
            var phase = controller.CurrentPhase;
            if (phase != ScenarioPhase.Phase2_ServerExplosionFire && phase != ScenarioPhase.Phase3_SmokeEvacuation) return;

            var fire = controller.ServerFire;
            bool burning = fire != null && fire.isActiveAndEnabled && !fire.IsExtinguished;
            if (burning) _fireBurnSeconds += Time.deltaTime;

            var inputs = new SocraticInputs
            {
                HoldingConductiveAgent = IsHoldingConductiveAgent(),
                PanelPowerOn = electricalPanel != null && electricalPanel.IsPowerOn,
                DistanceToPanel = electricalPanel != null
                    ? Vector3.Distance(head.position, electricalPanel.transform.position)
                    : float.MaxValue,
                HeadInSmoke = smoke != null && head.position.y >= smoke.CurrentSmokeHeight,
                Crouching = heightDetector != null && heightDetector.IsCrouching,
                HandToHandleDistance = exitDoor != null ? NearestHandDistance(exitDoor.transform.position) : float.MaxValue,
                DoorHot = exitDoor != null && exitDoor.IsDoorHot,
                DoorHeatChecked = exitDoor != null && exitDoor.HasCheckedHeat,
                FireBurning = burning,
                FireBurnSeconds = _fireBurnSeconds,
                AlarmRaised = AnyAlarmRaised()
            };

            var fired = _rules.Evaluate(inputs, Time.time, Time.deltaTime);
            if (fired == SocraticTrigger.None) return;

            // Most safety-critical first; only one line per frame.
            if ((fired & SocraticTrigger.WaterNearLivePanel) != 0) Say(controller, SocraticTrigger.WaterNearLivePanel);
            else if ((fired & SocraticTrigger.HotHandleUnchecked) != 0) Say(controller, SocraticTrigger.HotHandleUnchecked);
            else if ((fired & SocraticTrigger.StandingInSmoke) != 0) Say(controller, SocraticTrigger.StandingInSmoke);
            else if ((fired & SocraticTrigger.AlarmNotRaised) != 0) Say(controller, SocraticTrigger.AlarmNotRaised);
        }

        private static void Say(LabProgressiveScenarioController controller, SocraticTrigger trigger)
        {
            controller.SetAIMentorGuidance(SocraticRules.LineFor(trigger));
        }

        private bool IsHoldingConductiveAgent()
        {
            for (int i = 0; _extinguishers != null && i < _extinguishers.Length; i++)
            {
                var b = _extinguishers[i];
                if (b != null && b.IsHeld && b.Extinguisher.Type == ExtinguisherType.WaterFoam) return true;
            }
            return false;
        }

        private bool AnyAlarmRaised()
        {
            for (int i = 0; _alarms != null && i < _alarms.Length; i++)
            {
                if (_alarms[i] != null && _alarms[i].IsActivated) return true;
            }
            return false;
        }

        private float NearestHandDistance(Vector3 target)
        {
            float d = float.MaxValue;
            if (leftHand != null) d = Mathf.Min(d, Vector3.Distance(leftHand.position, target));
            if (rightHand != null) d = Mathf.Min(d, Vector3.Distance(rightHand.position, target));
            return d;
        }

        private static Transform FindTransform(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var go = GameObject.Find(names[i]);
                if (go != null) return go.transform;
            }
            return null;
        }
    }
}
