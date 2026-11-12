using UnityEngine;
using EscapeFire.Scenario;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Trigger volume placed past the exit door threshold (hallway / emergency stairs).
    /// When the player's head or body enters, the drill is won.
    /// Requires a Collider with isTrigger; the player needs a Rigidbody or CharacterController to fire triggers.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EvacuationExitZone : MonoBehaviour
    {
        [Tooltip("Only counts after the fire phase started (prevents walking out during peaceful practice).")]
        [SerializeField] private bool requireEmergency = true;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;

            var controller = LabProgressiveScenarioController.Instance;
            if (controller == null) return;
            if (requireEmergency && controller.CurrentPhase < ScenarioPhase.Phase2_ServerExplosionFire) return;

            Reached(controller);
        }

        /// <summary>At the assembly point the warden's roll call comes first (V3.5); without one, reaching it wins.</summary>
        private static void Reached(LabProgressiveScenarioController controller)
        {
            var ap = AssemblyPoint.Instance;
            if (ap != null) ap.LearnerArrived();
            else controller.TriggerHallwayEvacuationWin();
        }

        private Collider _zone;
        private Transform _head;

        /// <summary>
        /// Walking out for real (room-scale) moves only the tracked head, which has no collider: also win when the
        /// learner's head is inside the zone.
        /// </summary>
        private void Update()
        {
            if (_zone == null) _zone = GetComponent<Collider>();
            if (_head == null)
            {
                var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
                if (_head == null) return;
            }
            var controller = LabProgressiveScenarioController.Instance;
            if (controller == null || (requireEmergency && controller.CurrentPhase < ScenarioPhase.Phase2_ServerExplosionFire)) return;
            if (controller.CurrentPhase >= ScenarioPhase.Phase4_CompletedWin) return;
            if (_zone.bounds.Contains(_head.position)) Reached(controller);
        }

        private static bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("MainCamera")) return true;
            if (other.GetComponent<CharacterController>() != null) return true;
            return other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null;
        }
    }
}
