using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Lesson
{
    /// <summary>Station 2 (right agent): put the right extinguisher on the "electrical fire" stand.</summary>
    public class SocketAgentCheck : MonoBehaviour, IPracticeCheck
    {
        [SerializeField] private XRSocketInteractor socket;
        [SerializeField] private ExtinguisherType required = ExtinguisherType.CO2;

        public void Configure(XRSocketInteractor s, ExtinguisherType t) { socket = s; required = t; }

        public bool Done { get; private set; }
        public string Instruction => "Chọn bình phù hợp và đặt lên bệ \"tủ điện đang cháy\".";
        public string Feedback { get; private set; } = "";

        private void Update()
        {
            if (Done || socket == null || !socket.hasSelection) return;
            var label = socket.interactablesSelected[0].transform.GetComponent<AgentLabel>();
            if (label == null) return;
            if (label.Type == required) Done = true;
            else Feedback = $"Bình {label.DisplayName} không dùng cho thiết bị điện đang có điện — chất chữa cháy dẫn điện gây giật. Hãy lấy ra và chọn bình khác.";
        }
    }
}
