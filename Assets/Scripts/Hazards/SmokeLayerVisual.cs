using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// V5.2: a smoke layer that reads as smoke, not a dark box — the volume (this object's renderer bounds) becomes a
    /// light translucent haze and is filled with slowly drifting soft puffs; the bottom of the layer stays visible.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class SmokeLayerVisual : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(0.17f, 0.17f, 0.18f);
        [SerializeField] private float hazeAlpha = 0.3f;
        [SerializeField] private float puffAlpha = 0.55f;
        [SerializeField] private int maxParticles = 180;

        public ParticleSystem Puffs { get; private set; }

        private void Start()
        {
            var r = GetComponent<Renderer>();
            Bounds b = r.bounds;
            r.sharedMaterial = RuntimeVisuals.ParticleMaterial(new Color(color.r, color.g, color.b, hazeAlpha));

            var go = new GameObject("SmokePuffs");
            go.transform.SetParent(transform.parent, true);
            go.transform.SetPositionAndRotation(b.center, Quaternion.identity);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(color.r, color.g, color.b, puffAlpha);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission;
            emission.rateOverTime = maxParticles / 7.5f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(b.size.x * 0.9f, b.size.y * 0.7f, b.size.z * 0.9f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.9f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.2f;
            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = RuntimeVisuals.ParticleMaterial(Color.white, false, RuntimeVisuals.SoftDot);
            ps.Play();
            Puffs = ps;
        }
    }
}
