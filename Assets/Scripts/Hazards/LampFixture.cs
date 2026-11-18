using UnityEngine;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// The lit face of a ceiling light panel. It is drawn unlit (it is the lamp), so it must go dark itself when the
    /// normal lighting fails (BlackoutController): only the battery emergency lights stay on.
    /// </summary>
    public class LampFixture : MonoBehaviour
    {
        [SerializeField] private Material onMaterial;
        [SerializeField] private Material offMaterial;

        public bool IsOn { get; private set; } = true;

        public void Configure(Material on, Material off)
        {
            onMaterial = on;
            offMaterial = off;
        }

        public void SetOn(bool on)
        {
            IsOn = on;
            var r = GetComponent<Renderer>();
            var m = on ? onMaterial : offMaterial;
            if (r != null && m != null) r.sharedMaterial = m;
        }
    }
}
