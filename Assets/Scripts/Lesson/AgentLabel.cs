using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Lesson
{
    /// <summary>What kind of agent a training extinguisher holds (lesson station 2).</summary>
    public class AgentLabel : MonoBehaviour
    {
        public ExtinguisherType Type;
        public string DisplayName;
    }
}
