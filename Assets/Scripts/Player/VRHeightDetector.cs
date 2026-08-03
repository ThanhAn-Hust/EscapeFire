using System;
using UnityEngine;

namespace EscapeFire.Player
{
    /// <summary>
    /// Monitors VR Headset height relative to floor to detect crouching.
    /// Used for smoke avoidance mechanics in PCCC training.
    /// </summary>
    public class VRHeightDetector : MonoBehaviour
    {
        [Header("Height Threshold Settings")]
        [Tooltip("Threshold height in meters. Below this height, the player is considered crouching.")]
        [SerializeField] private float crouchThresholdHeight = 1.2f;

        [Tooltip("Transform of the VR Headset (Main Camera).")]
        [SerializeField] private Transform headTransform;

        public float CrouchThresholdHeight 
        { 
            get => crouchThresholdHeight; 
            set => crouchThresholdHeight = value; 
        }

        public bool IsCrouching { get; private set; }
        public float CurrentHeight { get; private set; }

        public event Action<bool> OnCrouchStateChanged;

        private void Start()
        {
            if (headTransform == null && Camera.main != null)
            {
                headTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (headTransform != null)
            {
                UpdateHeight(headTransform.localPosition.y);
            }
        }

        public void UpdateHeight(float height)
        {
            CurrentHeight = height;
            bool newlyCrouching = height < crouchThresholdHeight;

            if (newlyCrouching != IsCrouching)
            {
                IsCrouching = newlyCrouching;
                OnCrouchStateChanged?.Invoke(IsCrouching);
            }
        }
    }
}
