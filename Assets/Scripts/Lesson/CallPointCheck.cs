using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Lesson
{
    /// <summary>Station 4 (alarm): press the manual call point.</summary>
    public class CallPointCheck : MonoBehaviour, IPracticeCheck
    {
        [SerializeField] private FireAlarmButton alarm;

        public void Configure(FireAlarmButton a) => alarm = a;

        public bool Done => alarm != null && alarm.IsActivated;
        public string Instruction => "Ấn mạnh ngón trỏ vào nút báo cháy trên tường.";
        public string Feedback => "";
    }
}
