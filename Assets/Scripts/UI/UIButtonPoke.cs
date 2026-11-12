using UnityEngine;
using UnityEngine.UI;

namespace EscapeFire.UI
{
    /// <summary>
    /// V5.1: a world-space UI button pressed with the fingertip (XRI poke on a thin collider over the button), like the
    /// physical buttons in the lesson room. The controller ray still clicks it through the UI module as before.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonPoke : MonoBehaviour
    {
        [SerializeField] private float cooldown = 0.6f;
        private float _last = -10f;

        public int Presses { get; private set; }

        public void Press()
        {
            if (Time.unscaledTime - _last < cooldown) return;
            var button = GetComponent<Button>();
            if (!button.interactable) return;
            _last = Time.unscaledTime;
            Presses++;
            button.onClick.Invoke();
        }
    }
}
