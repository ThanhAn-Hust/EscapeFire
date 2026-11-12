using UnityEngine;

namespace EscapeFire.Generation
{
    /// <summary>Puts a humanoid character into a relaxed standing pose at runtime (arms down), keeping its placement.</summary>
    public static class RuntimePose
    {
        public static void Stand(GameObject model)
        {
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return;
            var handler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = new HumanPose();
            handler.GetHumanPose(ref pose);
            Set(ref pose, "Left Arm Down-Up", -0.75f);
            Set(ref pose, "Right Arm Down-Up", -0.75f);
            Set(ref pose, "Left Arm Front-Back", 0.15f);
            Set(ref pose, "Right Arm Front-Back", 0.15f);
            Set(ref pose, "Left Forearm Stretch", 0.85f);
            Set(ref pose, "Right Forearm Stretch", 0.85f);
            Vector3 p = animator.transform.position;
            Quaternion r = animator.transform.rotation;
            handler.SetHumanPose(ref pose);
            animator.transform.SetPositionAndRotation(p, r);
            handler.Dispose();
            animator.enabled = false;
        }

        private static void Set(ref HumanPose pose, string muscle, float value)
        {
            int i = System.Array.IndexOf(HumanTrait.MuscleName, muscle);
            if (i >= 0) pose.muscles[i] = value;
        }
    }
}
