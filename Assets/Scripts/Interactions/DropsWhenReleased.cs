using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// A small prop (radio, towel) rests kinematic where the scene placed it, and only falls under gravity after the
    /// learner has picked it up and let it go — no free-falling bodies at scene start, real physics once handled.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public class DropsWhenReleased : MonoBehaviour
    {
        private XRGrabInteractable _grab;
        private Rigidbody _body;
        private bool _released;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
        }

        private void OnEnable() => _grab.selectExited.AddListener(OnReleased);
        private void OnDisable() => _grab.selectExited.RemoveListener(OnReleased);

        private void OnReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs _) => _released = true;

        private void LateUpdate()
        {
            // XRI restores the rigidbody state on release; switch to physics afterwards.
            if (!_released || _grab.isSelected) return;
            _released = false;
            _body.isKinematic = false;
            _body.useGravity = true;
        }
    }
}
