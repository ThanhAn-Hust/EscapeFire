using Unity.XR.CoreUtils;
using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Interactions;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// A space next to the fire room (corridor, stairwell) that fills with smoke only through open doors
    /// (plan V2.6): its thickness moves towards (source thickness × leak) while a linked door is open and slowly clears
    /// while all are shut. Shows a lowering smoke layer and fogs the view when the learner's head is inside.
    /// </summary>
    public class SmokeZone : MonoBehaviour
    {
        [SerializeField] private Bounds space = new Bounds(Vector3.zero, Vector3.one);
        [SerializeField] private SmokeDensityController roomSource;
        [SerializeField] private SmokeZone zoneSource;
        [SerializeField] private ExitDoor[] exitDoors = new ExitDoor[0];
        [SerializeField] private PushBarDoor[] fireDoors = new PushBarDoor[0];
        [SerializeField] private float leak = 0.7f;
        [SerializeField] private float fillSeconds = 25f;
        [SerializeField] private Transform layer; // dark slab under the ceiling
        [SerializeField] private Color smokeColor = new Color(0.12f, 0.12f, 0.13f);

        private Transform _head;
        private Material _mat;

        /// <summary>0 = clear air, 1 = as thick as the fire room.</summary>
        public float Thickness { get; private set; }
        public Bounds Space => space;
        /// <summary>Smoke layer bottom (world y): high when thin, low when thick.</summary>
        public float LayerBottom => Mathf.Lerp(space.max.y - 0.2f, space.min.y + 1.0f, Thickness);

        public void Configure(Bounds b, SmokeDensityController room, SmokeZone upstream, ExitDoor[] exits, PushBarDoor[] doors, Transform smokeLayer)
        {
            space = b; roomSource = room; zoneSource = upstream; exitDoors = exits; fireDoors = doors; layer = smokeLayer;
        }

        /// <summary>AI challenge: smoke pours into this space (e.g. a stairwell) regardless of the doors.</summary>
        public void Flood(float thickness) => Thickness = Mathf.Max(Thickness, thickness);

        private void Update()
        {
            bool open = false;
            foreach (var d in exitDoors) if (d != null && d.IsOpen) open = true;
            foreach (var d in fireDoors) if (d != null && d.IsOpen) open = true;
            float source = roomSource != null ? roomSource.Thickness : zoneSource != null ? zoneSource.Thickness : 0f;
            float target = open ? source * leak : 0f;
            float rate = 1f / Mathf.Max(1f, fillSeconds);
            Thickness = target > Thickness ? Mathf.MoveTowards(Thickness, target, rate * Time.deltaTime) : Mathf.MoveTowards(Thickness, target, rate * 0.25f * Time.deltaTime);

            if (layer != null)
            {
                float bottom = LayerBottom, top = space.max.y;
                layer.position = new Vector3(space.center.x, (bottom + top) * 0.5f, space.center.z);
                layer.localScale = new Vector3(space.size.x, Mathf.Max(0.05f, top - bottom), space.size.z);
                var r = layer.GetComponent<Renderer>();
                if (r != null)
                {
                    if (_mat == null) { _mat = RuntimeVisuals.ParticleMaterial(smokeColor); r.sharedMaterial = _mat; }
                    _mat.SetColor("_BaseColor", new Color(smokeColor.r, smokeColor.g, smokeColor.b, Mathf.Lerp(0f, 0.7f, Thickness)));
                    r.enabled = Thickness > 0.02f;
                }
            }

            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
            }
            if (_head != null && space.Contains(_head.position) && Thickness > 0.02f)
            {
                bool inSmoke = _head.position.y > LayerBottom;
                SmokeFog.Request(0.02f + 0.06f * Thickness + (inSmoke ? 0.15f * Thickness : 0f), Color.Lerp(new Color(0.35f, 0.35f, 0.36f), smokeColor, Thickness));
            }
        }
    }
}
