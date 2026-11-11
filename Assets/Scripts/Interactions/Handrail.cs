using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EscapeFire.Interactions
{
    /// <summary>
    /// Stair handrail (plan Phần B.1-12, rule R10: "tay tựa lan can"). Counts the time a hand is on the rail while the
    /// learner is on the stairs (resting on it or gripping it), for the AAR.
    /// </summary>
    public class Handrail : MonoBehaviour
    {
        [SerializeField] private Vector3 from;
        [SerializeField] private Vector3 to;
        [SerializeField] private float touchRadius = 0.08f;

        public static float SecondsTouched { get; private set; }
        public static void ResetStats() => SecondsTouched = 0f;
        public bool Touched { get; private set; }

        public void Configure(Vector3 a, Vector3 b) { from = a; to = b; }

        private void Update()
        {
            Touched = false;
            foreach (var i in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (!i.isActiveAndEnabled) continue;
                Vector3 p = i.transform.position;
                Vector3 ab = to - from;
                float t = Mathf.Clamp01(Vector3.Dot(p - from, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                if ((from + ab * t - p).sqrMagnitude <= touchRadius * touchRadius) { Touched = true; break; }
            }
            if (Touched) SecondsTouched += Time.deltaTime;
        }
    }
}
