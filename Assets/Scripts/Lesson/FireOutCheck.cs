using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Lesson
{
    /// <summary>Station 3 (PASS) and the like: done when the training fire is out.</summary>
    public class FireOutCheck : MonoBehaviour, IPracticeCheck
    {
        [SerializeField] private FireHazard fire;
        [SerializeField] private ExtinguisherSprayEffect technique;

        public void Configure(FireHazard f, ExtinguisherSprayEffect fx) { fire = f; technique = fx; }

        public bool Done => fire != null && fire.IsExtinguished;
        public string Instruction => "Nhấc bình CO2 khỏi giá, rút chốt bằng tay kia, cầm loa phun, đứng cách ~2 m, nhắm GỐC lửa, bóp cò và quét qua lại.";
        public string Feedback
        {
            get
            {
                if (technique == null || technique.SecondsSpraying < 1f) return "";
                float b = technique.SecondsAtBase / technique.SecondsSpraying, s = technique.SecondsSweeping / technique.SecondsSpraying;
                if (b < 0.5f) return "Đang phun vào ngọn lửa — hãy hạ loa xuống GỐC lửa.";
                if (s < 0.3f) return "Nhắm đúng gốc rồi — giờ quét qua lại để phủ hết gốc lửa.";
                return "";
            }
        }
    }
}
