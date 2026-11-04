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

            controller.TriggerHallwayEvacuationWin();
        }

        private static bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("MainCamera")) return true;
            if (other.GetComponent<CharacterController>() != null) return true;
            return other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null;
        }
    }
}
