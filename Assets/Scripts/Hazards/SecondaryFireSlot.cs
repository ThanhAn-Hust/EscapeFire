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

        public bool Ignite(FireHazard template)
        {
            if (!IsAvailable) return false;
            if (_fire == null)
            {
                if (template == null) return false;
                var go = Instantiate(template.gameObject, transform.position, transform.rotation, transform);
                go.name = $"SecondaryFire_{name}";
                _fire = go.GetComponent<FireHazard>();
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
