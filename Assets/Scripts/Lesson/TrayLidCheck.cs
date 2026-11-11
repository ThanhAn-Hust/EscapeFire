using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Lesson
{
    /// <summary>Station 1 (fire triangle): smother a tray fire by putting the lid on it — removes the oxygen.</summary>
    public class TrayLidCheck : MonoBehaviour, IPracticeCheck
    {
        [SerializeField] private Transform lid;
        [SerializeField] private Transform tray;
        [SerializeField] private FireHazard fire;

        public void Configure(Transform lidT, Transform trayT, FireHazard f) { lid = lidT; tray = trayT; fire = f; }

        public bool Done => fire != null && fire.IsExtinguished;
        public string Instruction => "Cầm nắp kim loại, đậy kín lên khay đang cháy.";
        public string Feedback => "";

        private void Update()
        {
            if (Done || lid == null || tray == null || fire == null) return;
            Vector3 d = lid.position - tray.position;
            float flat = new Vector2(d.x, d.z).magnitude;
            if (flat < 0.08f && d.y > -0.02f && d.y < 0.14f && Vector3.Dot(lid.up, Vector3.up) > 0.8f)
                fire.Extinguish(1e6f); // no oxygen left under the lid
        }
    }
}
