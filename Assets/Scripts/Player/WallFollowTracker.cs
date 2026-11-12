using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Player
{
    /// <summary>
    /// "Một tay lần theo tường" (plan Phần B.1-12, rule R10): while the learner is walking through smoke, measures the
    /// share of that time with a hand touching a wall or a handrail. Touch = a controller within a few cm of a solid
    /// upright surface (walls, door frames, rails), not the floor and not the learner's own rig.
    /// </summary>
    public class WallFollowTracker : MonoBehaviour
    {
        [SerializeField] private float touchRadius = 0.1f;
        [SerializeField] private float minWalkSpeed = 0.2f;
        [SerializeField] private float minThickness = 0.15f;

        private readonly Collider[] _hits = new Collider[16];
        private Transform _head;
        private Vector3 _last;
        private bool _hasLast;
        private SmokeDensityController _lab;

        public static WallFollowTracker Instance { get; private set; }
        public float SecondsWalkingInSmoke { get; private set; }
        public float SecondsTouching { get; private set; }
        /// <summary>Share of walking-in-smoke time with a hand on a wall / rail; -1 = never walked in smoke.</summary>
        public float Ratio => SecondsWalkingInSmoke < 1f ? -1f : SecondsTouching / SecondsWalkingInSmoke;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
                if (_head == null) return;
            }
            if (_lab == null) _lab = FindFirstObjectByType<SmokeDensityController>();
            Vector3 p = _head.position;
            float dt = Time.deltaTime;
            Vector3 v = _hasLast && dt > 0f ? (p - _last) / dt : Vector3.zero;
            _last = p; _hasLast = true;
            v.y = 0f;
            if (v.magnitude < minWalkSpeed || !InSmoke(p)) return;

            SecondsWalkingInSmoke += dt;
            if (HandOnWall()) SecondsTouching += dt;
        }

        private bool InSmoke(Vector3 p)
        {
            if (_lab != null && _lab.Thickness > minThickness && _lab.ContainsXZ(p) && p.y < 4.5f && p.y > -0.5f) return true;
            foreach (var z in FindObjectsByType<SmokeZone>(FindObjectsSortMode.None))
                if (z.Thickness > minThickness && z.Space.Contains(p)) return true;
            return false;
        }

        private bool HandOnWall()
        {
            foreach (var rail in FindObjectsByType<Handrail>(FindObjectsSortMode.None)) if (rail.Touched) return true;
            foreach (var hand in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (!hand.isActiveAndEnabled) continue;
                Vector3 h = hand.transform.position;
                int n = Physics.OverlapSphereNonAlloc(h, touchRadius, _hits, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var c = _hits[i];
                    if (c.GetComponentInParent<XROrigin>() != null) continue;
                    if (c.bounds.size.y < 0.8f) continue; // floor slabs, furniture tops, props
                    Vector3 cp = c.ClosestPoint(h);
                    Vector3 off = h - cp;
                    if (off.sqrMagnitude < 1e-6f || Mathf.Abs(off.y) < 0.7f * off.magnitude) return true;
                }
            }
            return false;
        }
    }
}
