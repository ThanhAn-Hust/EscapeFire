using UnityEngine;

namespace EscapeFire.NPC
{
    /// <summary>
    /// Bone-driven walk cycle for the PolygonOffice humanoids (the project ships no walk/run clips).
    /// Swings thighs, bends knees and swings arms in proportion to how fast the character root moves,
    /// and can hunch the spine (escorted / low under smoke). Runs in LateUpdate after any Animator.
    /// </summary>
    public class ProceduralWalk : MonoBehaviour
    {
        [SerializeField] private float strideDegrees = 28f;
        [SerializeField] private float kneeDegrees = 45f;
        [SerializeField] private float armDegrees = 22f;
        [SerializeField] private float stepsPerMeter = 1.35f;

        /// <summary>0 = upright, 1 = strongly bent forward (escorted through smoke).</summary>
        public float Hunch { get; set; }

        private Transform _upperLegL, _upperLegR, _legL, _legR, _shoulderL, _shoulderR, _spine;
        private Quaternion _rUpperLegL, _rUpperLegR, _rLegL, _rLegR, _rShoulderL, _rShoulderR, _rSpine;
        private Vector3 _lastPos;
        private float _phase, _amount;
        private bool _ready;

        private void Start()
        {
            _upperLegL = Find("UpperLeg_L"); _upperLegR = Find("UpperLeg_R");
            _legL = Find("Leg_L"); _legR = Find("Leg_R");
            _shoulderL = Find("Shoulder_L"); _shoulderR = Find("Shoulder_R");
            _spine = Find("Spine_02");
            _ready = _upperLegL != null && _upperLegR != null;
            if (!_ready) return;
            _rUpperLegL = _upperLegL.localRotation; _rUpperLegR = _upperLegR.localRotation;
            _rLegL = Rest(_legL); _rLegR = Rest(_legR);
            _rShoulderL = Rest(_shoulderL); _rShoulderR = Rest(_shoulderR);
            _rSpine = Rest(_spine);
            _lastPos = transform.position;
        }

        private static Quaternion Rest(Transform t) => t != null ? t.localRotation : Quaternion.identity;

        private Transform Find(string bone)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == bone) return t;
            return null;
        }

        private void LateUpdate()
        {
            if (!_ready || Time.deltaTime <= 0f) return;
            Vector3 delta = transform.position - _lastPos;
            delta.y = 0f;
            _lastPos = transform.position;
            float speed = delta.magnitude / Time.deltaTime;
            _amount = Mathf.MoveTowards(_amount, Mathf.Clamp01(speed / 1.2f), 4f * Time.deltaTime);
            _phase += delta.magnitude * stepsPerMeter * Mathf.PI;

            Vector3 right = transform.right;
            float s = Mathf.Sin(_phase) * _amount;
            Pose(_upperLegL, _rUpperLegL, right, -strideDegrees * s);
            Pose(_upperLegR, _rUpperLegR, right, strideDegrees * s);
            Pose(_legL, _rLegL, right, kneeDegrees * _amount * Mathf.Max(0f, Mathf.Sin(_phase + 1.2f)));
            Pose(_legR, _rLegR, right, kneeDegrees * _amount * Mathf.Max(0f, -Mathf.Sin(_phase + 1.2f)));
            Pose(_shoulderL, _rShoulderL, right, armDegrees * s);
            Pose(_shoulderR, _rShoulderR, right, -armDegrees * s);
            Pose(_spine, _rSpine, right, 35f * Hunch);
        }

        private static void Pose(Transform bone, Quaternion rest, Vector3 axis, float degrees)
        {
            if (bone == null) return;
            bone.localRotation = rest;
            if (Mathf.Abs(degrees) > 0.01f) bone.Rotate(axis, degrees, Space.World);
        }
    }
}
