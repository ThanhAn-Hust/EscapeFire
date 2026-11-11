using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using EscapeFire.Core;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Wall bracket (plan Phần B.1-4): the bottle hangs on a hook, so it must be lifted up a few centimetres
    /// before it comes off; pulling it straight out does nothing. Grab transformer on the bottle's
    /// <see cref="XRGrabInteractable"/>; after detaching it follows the hand normally.
    /// </summary>
    public class ExtinguisherMount : XRBaseGrabTransformer
    {
        [Tooltip("How high the bottle must be lifted off the hook (m).")]
        [SerializeField] private float liftToDetach = 0.045f;
        [SerializeField] private float maxLift = 0.06f;

        private Pose _mountPose;
        private bool _hasMountPose;

        public bool IsMounted { get; private set; } = true;
        public event System.Action OnDetached;

        protected override void Start()
        {
            base.Start();
            _mountPose = new Pose(transform.position, transform.rotation);
            _hasMountPose = true;
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            if (!IsMounted || !_hasMountPose) return;
            float lift = Mathf.Clamp(targetPose.position.y - _mountPose.position.y, 0f, maxLift);
            targetPose = new Pose(_mountPose.position + Vector3.up * lift, _mountPose.rotation);
            if (lift >= liftToDetach)
            {
                IsMounted = false;
                OnDetached?.Invoke();
                EventBus<ExtinguisherDetachedEvent>.Publish(new ExtinguisherDetachedEvent(GetComponent<FireExtinguisher>()));
            }
        }
    }

    public readonly struct ExtinguisherDetachedEvent
    {
        public readonly FireExtinguisher Extinguisher;
        public ExtinguisherDetachedEvent(FireExtinguisher e) { Extinguisher = e; }
    }
}
