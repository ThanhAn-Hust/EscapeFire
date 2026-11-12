using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// Controls volumetric smoke density and smoke layer lowering over time.
    /// </summary>
    public class SmokeDensityController : MonoBehaviour
    {
        [Header("Smoke Settings")]
        [SerializeField] private float initialSmokeHeight = 3.2f;
        [SerializeField] private float minSmokeHeight = 1.0f; // Height smoke lowers down to
        [SerializeField] private float smokeLoweringSpeed = 0.05f; // Meters per second

        [Header("References")]
        [SerializeField] private FireHazard targetFire;
        [SerializeField] private Transform smokeVolumeTransform;
        [Tooltip("Whose eyes the smoke fog applies to. Defaults to the main camera (the learner's head).")]
        [SerializeField] private Transform viewer;

        [Header("Visuals")]
        [SerializeField] private Color smokeColor = new Color(0.10f, 0.10f, 0.11f, 1f);
        [SerializeField] private float plumeRate = 28f;
        [SerializeField] private float ceilingRate = 40f;

        private ParticleSystem _plume, _ceiling;
        private Material _volumeMaterial;
        private bool _fogWas;
        private Color _fogColorWas;
        private float _fogDensityWas;
        private FogMode _fogModeWas;
        private bool _visualsBuilt;

        /// <summary>0 when the layer is at the ceiling, 1 when it has dropped to its minimum height.</summary>
        public float Thickness => Mathf.Clamp01((initialSmokeHeight - CurrentSmokeHeight) / Mathf.Max(0.01f, initialSmokeHeight - minSmokeHeight));

        public void SetViewer(Transform t) => viewer = t;

        /// <summary>True if the point is inside this room's footprint (the smoke volume).</summary>
        public bool ContainsXZ(Vector3 p)
        {
            if (smokeVolumeTransform == null || !smokeVolumeTransform.TryGetComponent<Renderer>(out var room)) return true;
            Bounds rb = room.bounds;
            return p.x >= rb.min.x && p.x <= rb.max.x && p.z >= rb.min.z && p.z <= rb.max.z;
        }

        public float CurrentSmokeHeight { get; private set; }
        public bool HasSurged { get; private set; }

        /// <summary>True while the layer is high enough for a sudden drop to matter.</summary>
        public bool CanSurge => !HasSurged && CurrentSmokeHeight - minSmokeHeight > 0.9f;

        /// <summary>Anomaly TRIGGER_SMOKE_SURGE: thick smoke suddenly rolls down and keeps lowering faster.</summary>
        public bool Surge(float drop = 0.8f, float speedMultiplier = 2f)
        {
            if (!CanSurge) return false;
            HasSurged = true;
            CurrentSmokeHeight = Mathf.Max(minSmokeHeight, CurrentSmokeHeight - drop);
            smokeLoweringSpeed *= speedMultiplier;
            if (smokeVolumeTransform != null)
            {
                Vector3 pos = smokeVolumeTransform.position;
                pos.y = CurrentSmokeHeight;
                smokeVolumeTransform.position = pos;
            }
            return true;
        }

        private void Start()
        {
            if (targetFire == null && Scenario.LabProgressiveScenarioController.Instance != null)
            {
                targetFire = Scenario.LabProgressiveScenarioController.Instance.ServerFire;
            }
        }

        private void Awake()
        {
            CurrentSmokeHeight = initialSmokeHeight;
        }


        private bool Burning => targetFire != null && targetFire.isActiveAndEnabled && !targetFire.IsExtinguished;

        /// <summary>
        /// The smoke used to be a flat grey slab under the ceiling. Add what the learner should see: a black plume
        /// rising off the burning rack, a rolling layer under the ceiling that thickens as it drops, a darker
        /// volume, and fog that closes in when the head is inside the smoke.
        /// </summary>
        private void BuildVisuals()
        {
            _visualsBuilt = true;
            _fogWas = RenderSettings.fog; _fogColorWas = RenderSettings.fogColor;
            _fogDensityWas = RenderSettings.fogDensity; _fogModeWas = RenderSettings.fogMode;

            Vector3 firePos = targetFire.transform.position + Vector3.up * 2f;
            bool any = false;
            Bounds b = default;
            foreach (var r in targetFire.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (any) firePos = new Vector3(b.center.x, b.max.y, b.min.z + 0.3f);

            _plume = MakeSmoke("SmokePlume", firePos, Quaternion.Euler(-90f, 0f, 0f), ParticleSystemShapeType.Cone,
                Vector3.one, new ParticleSystem.MinMaxCurve(0.8f, 1.5f), new ParticleSystem.MinMaxCurve(6f, 9f),
                new ParticleSystem.MinMaxCurve(0.7f, 1.3f), 0.75f);
            var shape = _plume.shape;
            shape.angle = 22f;
            shape.radius = 0.35f;

            Vector3 roomCentre = smokeVolumeTransform != null ? smokeVolumeTransform.position : transform.position;
            Vector3 roomSize = smokeVolumeTransform != null ? smokeVolumeTransform.lossyScale : new Vector3(16f, 1f, 12f);
            _ceiling = MakeSmoke("SmokeCeilingLayer", new Vector3(roomCentre.x, initialSmokeHeight, roomCentre.z), Quaternion.identity,
                ParticleSystemShapeType.Box, new Vector3(roomSize.x * 0.9f, 0.3f, roomSize.z * 0.9f),
                new ParticleSystem.MinMaxCurve(1.6f, 2.8f), new ParticleSystem.MinMaxCurve(7f, 11f),
                new ParticleSystem.MinMaxCurve(0.05f, 0.25f), 0.6f);

            if (smokeVolumeTransform != null && smokeVolumeTransform.TryGetComponent<Renderer>(out var vr))
            {
                _volumeMaterial = RuntimeVisuals.ParticleMaterial(new Color(smokeColor.r, smokeColor.g, smokeColor.b, 0.35f));
                vr.sharedMaterial = _volumeMaterial;
            }
        }

        private ParticleSystem MakeSmoke(string name, Vector3 pos, Quaternion rot, ParticleSystemShapeType shapeType, Vector3 boxScale,
            ParticleSystem.MinMaxCurve size, ParticleSystem.MinMaxCurve lifetime, ParticleSystem.MinMaxCurve speed, float alpha)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, true);
            go.transform.SetPositionAndRotation(pos, rot);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(smokeColor.r, smokeColor.g, smokeColor.b, alpha);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = shapeType;
            shape.scale = boxScale;
            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.6f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.2f;
            // Roll along the ceiling instead of passing through it.
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.Planes;
            var ceilingPlane = new GameObject("CeilingPlane").transform;
            ceilingPlane.SetParent(transform, true);
            ceilingPlane.SetPositionAndRotation(new Vector3(pos.x, initialSmokeHeight + 1.1f, pos.z), Quaternion.Euler(180f, 0f, 0f));
            collision.AddPlane(ceilingPlane);
            collision.dampen = 0.6f;
            collision.bounce = 0f;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = RuntimeVisuals.ParticleMaterial(Color.white);
            pr.sortingFudge = 10f;
            return ps;
        }

        private void UpdateVisuals()
        {
            bool burning = Burning;
            if (burning && !_visualsBuilt) BuildVisuals();
            if (!_visualsBuilt) return;

            float k = Thickness;
            float intensity = Mathf.Clamp(targetFire.FireIntensity / Mathf.Max(1f, targetFire.InitialIntensity), 0.4f, 1.6f);
            SetRate(_plume, burning ? plumeRate * intensity : 0f);
            if (_ceiling != null)
            {
                var p = _ceiling.transform.position;
                _ceiling.transform.position = new Vector3(p.x, CurrentSmokeHeight + 0.35f, p.z);
                SetRate(_ceiling, ceilingRate * Mathf.Lerp(0.3f, 1.2f, k));
            }
            if (_volumeMaterial != null)
                _volumeMaterial.SetColor("_BaseColor", new Color(smokeColor.r, smokeColor.g, smokeColor.b, Mathf.Lerp(0.3f, 0.75f, k)));

            Transform eye = viewer;
            if (eye == null && Camera.main != null) eye = Camera.main.transform;
            if (eye == null) return;
            // Only while the head is inside this room (corridor / stairs have their own SmokeZone).
            if (smokeVolumeTransform != null && smokeVolumeTransform.TryGetComponent<Renderer>(out var room))
            {
                Bounds rb = room.bounds;
                if (eye.position.x < rb.min.x || eye.position.x > rb.max.x || eye.position.z < rb.min.z || eye.position.z > rb.max.z) return;
            }
            bool headInSmoke = eye.position.y > CurrentSmokeHeight;
            SmokeFog.Request(0.02f + 0.07f * k + (headInSmoke ? 0.18f : 0f), Color.Lerp(new Color(0.35f, 0.35f, 0.36f), smokeColor, k));
        }

        private static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var e = ps.emission;
            e.rateOverTime = rate;
            if (rate > 0f && !ps.isPlaying) ps.Play();
        }

        private void LateUpdate() => UpdateVisuals();

        private void Update()
        {
            // Only lower smoke while the fire is actually burning (inactive in Phase 0/1).
            if (targetFire != null && targetFire.isActiveAndEnabled && !targetFire.IsExtinguished)
            {
                // Smoke lowers down as fire burns
                CurrentSmokeHeight = Mathf.Max(minSmokeHeight, CurrentSmokeHeight - (smokeLoweringSpeed * Time.deltaTime));

                if (smokeVolumeTransform != null)
                {
                    Vector3 pos = smokeVolumeTransform.position;
                    pos.y = CurrentSmokeHeight;
                    smokeVolumeTransform.position = pos;
                }
            }
        }
    }
}
