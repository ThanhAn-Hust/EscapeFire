using UnityEngine;
using EscapeFire.Hazards;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Applies the agent to fires according to how the learner actually sprays (PASS):
    /// distance, aim at the base vs the flames, side-to-side sweep and squeeze strength
    /// (<see cref="FireSuppressionModel"/>). Also keeps simple technique statistics for the AAR.
    /// </summary>
    [RequireComponent(typeof(FireExtinguisher))]
    public class ExtinguisherSprayEffect : MonoBehaviour
    {
        private FireExtinguisher _ext;
        private float _lastYaw = float.NaN;
        private float _sweepRate;

        /// <summary>Smoothed horizontal sweep speed of the nozzle (deg/s).</summary>
        public float SweepRate => _sweepRate;
        public float LastEfficiency { get; private set; }
        public float SecondsAtBase { get; private set; }
        public float SecondsAtFlames { get; private set; }
        public float SecondsSweeping { get; private set; }
        public float SecondsSpraying { get; private set; }

        private void Awake() => _ext = GetComponent<FireExtinguisher>();

        private void Update()
        {
            if (!_ext.IsSpraying)
            {
                _lastYaw = float.NaN;
                _sweepRate = Mathf.MoveTowards(_sweepRate, 0f, 60f * Time.deltaTime);
                return;
            }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Transform n = _ext.Nozzle;
            Vector3 dir = n.forward;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (!float.IsNaN(_lastYaw))
            {
                float rate = Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw)) / dt;
                _sweepRate = Mathf.Lerp(_sweepRate, rate, 1f - Mathf.Exp(-dt / 0.4f)); // ~0.4 s window
            }
            _lastYaw = yaw;
            SecondsSpraying += dt;
            if (_sweepRate >= FireSuppressionModel.SweepRateNeeded) SecondsSweeping += dt;

            float best = 0f;
            foreach (var fire in FireHazard.Active)
            {
                if (fire == null || !fire.isActiveAndEnabled || fire.IsExtinguished || fire.IsSuppressed) continue;
                Vector3 basePt = fire.BasePoint;
                Vector3 to = basePt - n.position;
                float dist = to.magnitude;
                if (dist < 1e-3f) continue;
                float angle = Vector3.Angle(dir, to);
                float aimHeight = (n.position + dir * dist).y - basePt.y;
                float eff = FireSuppressionModel.Efficiency(dist, angle, aimHeight, _sweepRate, _ext.Flow);
                if (eff > 0f) fire.Extinguish(_ext.ExtinguishPowerPerSecond * eff * dt);
                if (angle <= FireSuppressionModel.MissAngle && dist <= FireSuppressionModel.MaxRange)
                {
                    if (aimHeight > FireSuppressionModel.BaseBand) SecondsAtFlames += dt; else SecondsAtBase += dt;
                }
                best = Mathf.Max(best, eff);
            }
            LastEfficiency = best;
        }
    }
}
