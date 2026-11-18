using UnityEngine;

namespace EscapeFire.NPC
{
    /// <summary>Whole-body poses an NPC can take (the project has no animation clips).</summary>
    public enum BodyPose
    {
        None,             // standing / walking (ProceduralWalk drives the legs)
        CoverHeadCrouch,  // startled by the explosion: forearms over the head, knees bent, bent forward
        CoughHunch,       // smoke: one hand over mouth and nose, bent forward
        SitSlumped,       // overcome by smoke: sitting on the floor, head down
        PressButton,      // jabbing the lift call button
    }

    /// <summary>
    /// Procedural whole-body poses for the PolygonOffice humanoids, bone by bone (same approach as
    /// <see cref="ProceduralWalk"/>): each bone is turned from its rest rotation about the character's own right /
    /// forward axes, blended over time, and the body is lowered so the lowest point (feet, knees or seat) stays on the
    /// floor. While a pose is held the walk cycle is paused.
    /// Angle convention about the right axis: positive tips an upward bone (spine, neck) forward and swings a
    /// downward bone (arm, leg) backward; negative swings arms / thighs forward.
    /// </summary>
    public class NpcBodyPose : MonoBehaviour
    {
        private enum B { UpperArmL, UpperArmR, LowerArmL, LowerArmR, Spine, Neck, UpperLegL, UpperLegR, LowerLegL, LowerLegR, Count }

        private readonly Transform[] _bone = new Transform[(int)B.Count];
        private readonly Quaternion[] _rest = new Quaternion[(int)B.Count];
        private readonly Vector2[] _from = new Vector2[(int)B.Count]; // (about right, about forward) degrees
        private readonly Vector2[] _to = new Vector2[(int)B.Count];
        private readonly Vector2[] _now = new Vector2[(int)B.Count];
        private Transform _model, _hips, _footL, _footR;
        private float _modelY, _restLowest, _t, _duration;
        private ProceduralWalk _walk;
        private bool _ready;

        public BodyPose Current { get; private set; } = BodyPose.None;
        public bool Blending => _ready && _t < _duration;
        /// <summary>0..1 extra elbow straightening on top of PressButton (the jabbing arm).</summary>
        public float Jab { get; set; }

        private void Awake()
        {
            var anim = GetComponentInChildren<Animator>(); // the active model (NPC roots hold several, one shown)
            if (anim == null || anim.avatar == null || !anim.avatar.isHuman) return;
            _model = anim.transform;
            Map(anim, B.UpperArmL, HumanBodyBones.LeftUpperArm); Map(anim, B.UpperArmR, HumanBodyBones.RightUpperArm);
            Map(anim, B.LowerArmL, HumanBodyBones.LeftLowerArm); Map(anim, B.LowerArmR, HumanBodyBones.RightLowerArm);
            Map(anim, B.Spine, HumanBodyBones.Spine); Map(anim, B.Neck, HumanBodyBones.Neck);
            Map(anim, B.UpperLegL, HumanBodyBones.LeftUpperLeg); Map(anim, B.UpperLegR, HumanBodyBones.RightUpperLeg);
            Map(anim, B.LowerLegL, HumanBodyBones.LeftLowerLeg); Map(anim, B.LowerLegR, HumanBodyBones.RightLowerLeg);
            _hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            _footL = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            _footR = anim.GetBoneTransform(HumanBodyBones.RightFoot);
            _ready = _hips != null && _footL != null && _footR != null && _bone[(int)B.UpperLegL] != null;
            if (!_ready) return;
            for (int i = 0; i < _bone.Length; i++) if (_bone[i] != null) _rest[i] = _bone[i].localRotation;
            _modelY = _model.localPosition.y;
            _restLowest = Lowest();
            _walk = GetComponentInChildren<ProceduralWalk>();
        }

        private void Map(Animator a, B b, HumanBodyBones h) => _bone[(int)b] = a.GetBoneTransform(h);

        /// <summary>Blends to <paramref name="pose"/> in <paramref name="seconds"/>.</summary>
        public void Set(BodyPose pose, float seconds)
        {
            Current = pose;
            if (!_ready) return;
            for (int i = 0; i < _now.Length; i++) _from[i] = _now[i];
            Target(pose);
            _t = 0f;
            _duration = Mathf.Max(0.01f, seconds);
            if (_walk != null) _walk.enabled = false;
        }

        private void LateUpdate()
        {
            if (!_ready) return;
            if (Current == BodyPose.None && !Blending)
            {
                if (_walk != null && !_walk.enabled) _walk.enabled = true;
                return; // the walk cycle owns the bones
            }
            _t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t / _duration));
            Vector3 right = transform.right, fwd = transform.forward;
            _model.localPosition = new Vector3(_model.localPosition.x, _modelY, _model.localPosition.z);
            for (int i = 0; i < _bone.Length; i++)
            {
                if (_bone[i] == null) continue;
                _now[i] = Vector2.Lerp(_from[i], _to[i], k);
                Vector2 a = _now[i];
                if (Current == BodyPose.PressButton && k >= 1f && i == (int)B.LowerArmR) a.x += 35f * Jab;
                _bone[i].localRotation = _rest[i];
                if (Mathf.Abs(a.x) > 0.01f) _bone[i].Rotate(right, a.x, Space.World);
                if (Mathf.Abs(a.y) > 0.01f) _bone[i].Rotate(fwd, a.y, Space.World);
            }
            // keep the lowest point of the body on the floor
            var lp = _model.localPosition;
            lp.y = _modelY + (_restLowest - Lowest());
            _model.localPosition = lp;
            if (Current == BodyPose.None && !Blending)
            {
                _model.localPosition = new Vector3(lp.x, _modelY, lp.z);
                for (int i = 0; i < _bone.Length; i++) if (_bone[i] != null) _bone[i].localRotation = _rest[i];
                if (_walk != null) _walk.enabled = true;
            }
        }

        /// <summary>Height of the lowest body point above this object's origin (feet, knees, or the seat).</summary>
        private float Lowest()
        {
            float low = Mathf.Min(_footL.position.y, _footR.position.y);
            low = Mathf.Min(low, _hips.position.y - 0.1f);
            var kl = _bone[(int)B.LowerLegL]; var kr = _bone[(int)B.LowerLegR];
            if (kl != null) low = Mathf.Min(low, kl.position.y - 0.06f);
            if (kr != null) low = Mathf.Min(low, kr.position.y - 0.06f);
            return low - transform.position.y;
        }

        private void Target(BodyPose pose)
        {
            for (int i = 0; i < _to.Length; i++) _to[i] = Vector2.zero;
            switch (pose)
            {
                case BodyPose.CoverHeadCrouch:
                    T(B.UpperArmL, -125f, -30f); T(B.UpperArmR, -125f, 30f);   // elbows up in front of the face, inwards
                    T(B.LowerArmL, -95f, 0f); T(B.LowerArmR, -95f, 0f);         // forearms folded over the head
                    T(B.Spine, 30f, 0f); T(B.Neck, 25f, 0f);
                    T(B.UpperLegL, -65f, 0f); T(B.UpperLegR, -65f, 0f); T(B.LowerLegL, 100f, 0f); T(B.LowerLegR, 100f, 0f);
                    break;
                case BodyPose.CoughHunch:
                    T(B.UpperArmR, -45f, -25f); T(B.LowerArmR, -120f, 0f);      // elbow in front of the chest, hand over mouth and nose
                    T(B.UpperArmL, -25f, 0f);
                    T(B.Spine, 32f, 0f); T(B.Neck, 12f, 0f);
                    T(B.UpperLegL, -25f, 0f); T(B.UpperLegR, -25f, 0f); T(B.LowerLegL, 40f, 0f); T(B.LowerLegR, 40f, 0f);
                    break;
                case BodyPose.SitSlumped:
                    T(B.UpperLegL, -115f, -6f); T(B.UpperLegR, -115f, 6f); T(B.LowerLegL, 85f, 0f); T(B.LowerLegR, 85f, 0f); // knees up, feet flat
                    T(B.Spine, 28f, 0f); T(B.Neck, 35f, 0f);
                    T(B.UpperArmL, -45f, 0f); T(B.UpperArmR, -45f, 0f); T(B.LowerArmL, -20f, 0f); T(B.LowerArmR, -20f, 0f);
                    break;
                case BodyPose.PressButton:
                    T(B.UpperArmR, -80f, 0f); T(B.LowerArmR, -25f, 0f); T(B.Spine, 8f, 0f);
                    break;
            }
        }

        private void T(B b, float aboutRight, float aboutForward) => _to[(int)b] = new Vector2(aboutRight, aboutForward);
    }
}
