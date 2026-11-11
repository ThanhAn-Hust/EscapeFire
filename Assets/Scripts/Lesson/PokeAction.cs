using TMPro;
using UnityEngine;
using UnityEngine.Events;
using EscapeFire.Core;

namespace EscapeFire.Lesson
{
    /// <summary>A push button pressed with the fingertip (wired to an XRSimpleInteractable's selectEntered).</summary>
    public class PokeAction : MonoBehaviour
    {
        [SerializeField] private UnityEvent onPressed = new UnityEvent();
        [SerializeField] private float cooldown = 0.5f;
        private float _last = -10f;

        public UnityEvent OnPressed => onPressed;
        public int Presses { get; private set; }

        public void Press()
        {
            if (Time.time - _last < cooldown) return;
            _last = Time.time;
            Presses++;
            onPressed.Invoke();
        }
    }
}
