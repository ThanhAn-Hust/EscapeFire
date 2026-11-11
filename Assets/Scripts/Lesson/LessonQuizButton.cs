using TMPro;
using UnityEngine;
using UnityEngine.Events;
using EscapeFire.Core;

namespace EscapeFire.Lesson
{
    /// <summary>Answer button of a station quiz.</summary>
    public class LessonQuizButton : MonoBehaviour
    {
        [SerializeField] private LessonStation station;
        [SerializeField] private int index;

        public void Configure(LessonStation s, int i) { station = s; index = i; }
        public void Press() { if (station != null) station.Answer(index); }
    }
}
