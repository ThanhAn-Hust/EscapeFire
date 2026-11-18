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
        private float _shownAt = -10f;

        public int Presses { get; private set; }

        // A button that just appeared (panel swap) under a finger still pressing the previous one must not fire,
        // neither by poke nor by the UI module's click when that finger lifts: it is inert for a moment.
        private void OnEnable()
        {
            _shownAt = Time.unscaledTime;
            var button = GetComponent<Button>();
            if (button.interactable) { button.interactable = false; _armPending = true; }
        }

        private bool _armPending;

        private void Update()
        {
            if (_armPending && Time.unscaledTime - _shownAt >= 0.5f)
            {
                _armPending = false;
                GetComponent<Button>().interactable = true;
            }
        }

        private void OnDisable()
        {
            if (_armPending) { _armPending = false; GetComponent<Button>().interactable = true; }
        }

        public void Press()
        {
            if (Time.unscaledTime - _last < cooldown || Time.unscaledTime - _shownAt < 0.5f) return;
            var button = GetComponent<Button>();
            if (!button.interactable) return;
            _last = Time.unscaledTime;
            Presses++;
            button.onClick.Invoke();
        }
    }
}
