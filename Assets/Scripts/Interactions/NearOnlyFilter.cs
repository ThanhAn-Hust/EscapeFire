using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// "Hands do the work of hands" (plan Phần B.0-2): things you press, pull or grab in real life can only be
    /// hovered/selected when the hand is actually at the object — no pointing a ray from across the room.
    /// Add to an interactable and list it in its starting hover + select filters.
    /// </summary>
    public class NearOnlyFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
        [Tooltip("Max distance (m) from the hand to the object's colliders.")]
        [SerializeField] private float reach = 0.15f;

        public bool canProcess => isActiveAndEnabled;

        public float Reach { get => reach; set => reach = value; }

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) => InReach(interactor, interactable);

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable) => InReach(interactor, interactable);

        private bool InReach(IXRInteractor interactor, IXRInteractable interactable)
        {
            if (interactable == null || interactor == null) return false;
            return interactable.GetDistanceSqrToInteractor(interactor) <= reach * reach;
        }
    }
}
