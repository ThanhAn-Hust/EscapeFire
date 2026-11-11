using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using EscapeFire.Core;

namespace EscapeFire.Player
{
    /// <summary>
    /// Real-life movement limits: thumbstick speed follows the learner's actual posture (standing / crouched /
    /// crawling), so you cannot sprint while crouched under the smoke. Lives on the XR Origin.
    /// </summary>
    public class PostureLocomotion : MonoBehaviour
    {
        [SerializeField] private ContinuousMoveProvider moveProvider;
        [SerializeField] private Transform head;

        public Posture Current { get; private set; } = Posture.Standing;

        private void Awake()
        {
            if (moveProvider == null) moveProvider = GetComponentInChildren<ContinuousMoveProvider>(true);
            if (head == null)
            {
                var origin = GetComponent<XROrigin>();
                if (origin != null && origin.Camera != null) head = origin.Camera.transform;
            }
        }

        private void Update()
        {
            if (head == null) return;
            float floorY = transform.position.y;
            Current = PostureRules.Classify(head.position.y - floorY, GameSession.CalibratedHeight);
            if (moveProvider != null) moveProvider.moveSpeed = PostureRules.MaxSpeed(Current);
        }
    }
}
