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
        [Tooltip("Held in one hand at a time (bottle body): the other hand works the pin / horn instead of stealing it.")]
        [SerializeField] private bool exclusive;
        public bool Exclusive { get => exclusive; set => exclusive = value; }

        [Tooltip("Max distance (m) from the hand to the object's colliders.")]
        [SerializeField] private float reach = 0.15f;
        [Tooltip("Pressed with a fingertip in real life (call point, lift button): only the poke interactor may select.")]
        [SerializeField] private bool pokeOnly;

        public bool canProcess => isActiveAndEnabled;

        public float Reach { get => reach; set => reach = value; }
        public bool PokeOnly { get => pokeOnly; set => pokeOnly = value; }

        // A press-only object must not be hovered by the grab interactor either: in the hand's interaction group
        // that hover would block the fingertip (poke) interactor from taking over.
        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable) =>
            (!pokeOnly || interactor is XRPokeInteractor) && InReach(interactor, interactable);

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            // XRI re-checks select filters every frame: something already in the hand stays in the hand.
            if (interactable != null && interactable.interactorsSelecting.Contains(interactor)) return true;
            if (pokeOnly && !(interactor is XRPokeInteractor)) return false;
            if (exclusive && interactable.isSelected && !interactable.interactorsSelecting.Contains(interactor)) return false;
            return InReach(interactor, interactable);
        }

        private bool InReach(IXRInteractor interactor, IXRInteractable interactable)
        {
            if (interactable == null || interactor == null) return false;
            return interactable.GetDistanceSqrToInteractor(interactor) <= reach * reach;
        }
    }
}
