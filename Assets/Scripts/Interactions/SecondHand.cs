using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Finds "the other hand" working on a held object: a controller interactor that is NOT holding the object,
    /// has its grip pressed and its hand within reach of a point (pin ring, horn handle).
    /// Uses the controller's real grip input through XRI, so it behaves the same on the headset and in tests.
    /// </summary>
    public static class SecondHand
    {
        public static XRBaseInputInteractor FindGripping(XRGrabInteractable heldObject, Vector3 point, float reach)
        {
            XRBaseInputInteractor best = null;
            float bestSqr = reach * reach;
            foreach (var i in Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (!i.isActiveAndEnabled) continue;
                if (heldObject != null && heldObject.isSelected && heldObject.interactorsSelecting.Contains(i)) continue;
                if (!i.selectInput.ReadIsPerformed()) continue;
                float d = (i.transform.position - point).sqrMagnitude;
                if (d <= bestSqr) { bestSqr = d; best = i; }
            }
            return best;
        }

        /// <summary>True when the object is currently held by a hand (any interactor).</summary>
        public static bool IsHeld(XRGrabInteractable g) => g != null && g.isSelected;
    }
}
