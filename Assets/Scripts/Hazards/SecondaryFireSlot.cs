using UnityEngine;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// A pre-placed location where a secondary fire may spread (chemical tray, cable desk...).
    /// Slots marked <see cref="blocksExit"/> are never used (Life-Safety Invariant: always keep an exit).
    /// The fire is cloned from a template once and reused (no Instantiate during play after the first ignite).
    /// </summary>
    public class SecondaryFireSlot : MonoBehaviour
    {
        [SerializeField] private bool blocksExit = false;
        [SerializeField] private string description = "Khay hóa chất tẩy mạch";

        private FireHazard _fire;

        public bool BlocksExit => blocksExit;
        public string Description => description;
        public bool IsBurning => _fire != null && _fire.isActiveAndEnabled && !_fire.IsExtinguished;
        public bool IsAvailable => !blocksExit && !IsBurning;

        /// <summary>
        /// The template FireHazard sits on the server-rack root, so a clone carries the whole rack. Keep only the
        /// flame particles visible and drop the clone's colliders so no phantom rack appears on the desk.
        /// </summary>
        public static void HideEverythingButFlames(FireHazard fire)
        {
            if (fire == null) return;
            fire.ResolveParticles();
            var flames = fire.Particles != null ? fire.Particles.transform : null;
            foreach (var r in fire.GetComponentsInChildren<Renderer>(true))
                if (flames == null || !r.transform.IsChildOf(flames)) r.enabled = false;
            foreach (var c in fire.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            if (flames != null) flames.localPosition = new Vector3(0f, flames.localPosition.y > 0.3f ? 0.3f : flames.localPosition.y, 0f);
        }

        public bool Ignite(FireHazard template)
        {
            if (!IsAvailable) return false;
            if (_fire == null)
            {
                if (template == null) return false;
                var go = Instantiate(template.gameObject, transform.position, transform.rotation, transform);
                go.name = $"SecondaryFire_{name}";
                _fire = go.GetComponent<FireHazard>();
                HideEverythingButFlames(_fire);
                go.SetActive(false);
            }
            _fire.gameObject.SetActive(true);
            _fire.ResetFire();
            return _fire.isActiveAndEnabled && !_fire.IsSuppressed;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = blocksExit ? Color.gray : new Color(1f, 0.4f, 0f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
