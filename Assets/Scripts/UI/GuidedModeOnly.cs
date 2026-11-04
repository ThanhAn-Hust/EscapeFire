using UnityEngine;
using EscapeFire.Core;

namespace EscapeFire.UI
{
    /// <summary>
    /// Shows hint objects (yellow outlines, floor arrows, labels) only in Guided mode; hides them in Exam mode.
    /// </summary>
    public class GuidedModeOnly : MonoBehaviour
    {
        [SerializeField] private GameObject[] hintObjects;

        private void Start()
        {
            bool guided = GameSession.Mode == TrainingMode.Guided;
            if (hintObjects == null || hintObjects.Length == 0)
            {
                gameObject.SetActive(guided);
                return;
            }
            for (int i = 0; i < hintObjects.Length; i++)
            {
                if (hintObjects[i] != null) hintObjects[i].SetActive(guided);
            }
        }
    }
}
