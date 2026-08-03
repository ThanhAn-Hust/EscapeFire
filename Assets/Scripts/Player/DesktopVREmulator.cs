using UnityEngine;
using EscapeFire.Interactions;
using EscapeFire.NPC;

namespace EscapeFire.Player
{
    /// <summary>
    /// Standalone Desktop Keyboard/Mouse VR Emulator.
    /// Allows testing the VR PCCC game directly on PC without a physical VR Headset:
    /// - WASD / Mouse: Move and look around.
    /// - C key: Crouch / Stand toggle (tests VRHeightDetector smoke avoidance).
    /// - E key / Left Click: Interact with Aptomat, Fire Alarm Button, Fire Extinguisher, Door Heat, and NPCs.
    /// - Space / Hold Left Click: Spray CO2 Fire Extinguisher.
    /// </summary>
    public class DesktopVREmulator : MonoBehaviour
    {
        [Header("Desktop Navigation")]
        [SerializeField] private float moveSpeed = 4.0f;
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float standingHeight = 1.6f;
        [SerializeField] private float crouchingHeight = 0.9f;

        [Header("Interaction Raycast")]
        [SerializeField] private float interactDistance = 3.5f;

        [Header("References")]
        [SerializeField] private Transform headCameraTransform;
        [SerializeField] private VRHeightDetector heightDetector;
        [SerializeField] private FireExtinguisher currentHeldExtinguisher;

        private float _rotationX = 0f;
        private float _rotationY = 0f;
        private bool _isCrouching = false;

        private void Start()
        {
            if (headCameraTransform == null && Camera.main != null)
            {
                headCameraTransform = Camera.main.transform;
            }
            if (heightDetector == null)
            {
                heightDetector = GetComponent<VRHeightDetector>();
            }

            Debug.Log("[DesktopVREmulator] VR Emulator active! Use WASD to move, Mouse to look, C to crouch, E/Left Click to interact.");
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleCrouch();
            HandleInteractions();
        }

        private void HandleLook()
        {
            if (Input.GetMouseButton(1) || Input.GetMouseButton(0)) // Hold mouse button to look around
            {
                _rotationY += Input.GetAxis("Mouse X") * mouseSensitivity * 2f;
                _rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity * 2f;
                _rotationX = Mathf.Clamp(_rotationX, -85f, 85f);

                transform.rotation = Quaternion.Euler(0f, _rotationY, 0f);
                if (headCameraTransform != null)
                {
                    headCameraTransform.localRotation = Quaternion.Euler(_rotationX, 0f, 0f);
                }
            }
        }

        private void HandleMovement()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 moveDir = (transform.right * moveX + transform.forward * moveZ).normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }

        private void HandleCrouch()
        {
            // Toggle Crouch with C key
            if (Input.GetKeyDown(KeyCode.C))
            {
                _isCrouching = !_isCrouching;
                float targetY = _isCrouching ? crouchingHeight : standingHeight;

                if (headCameraTransform != null)
                {
                    Vector3 pos = headCameraTransform.localPosition;
                    pos.y = targetY;
                    headCameraTransform.localPosition = pos;
                }

                if (heightDetector != null)
                {
                    heightDetector.UpdateHeight(targetY);
                }

                Debug.Log($"[DesktopVREmulator] Crouch toggled: {(_isCrouching ? "CROUCHING (0.9m)" : "STANDING (1.6m)")}");
            }
        }

        private void HandleInteractions()
        {
            // E key or Left Click to interact
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                Ray ray = new Ray(headCameraTransform.position, headCameraTransform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
                {
                    // 1. Aptomat Switch
                    var breaker = hit.collider.GetComponentInParent<CircuitBreakerSwitch>();
                    if (breaker != null)
                    {
                        breaker.TogglePower();
                        Debug.Log("[Emulator] Toggled Aptomat Power!");
                        return;
                    }

                    // 2. Fire Alarm Button
                    var alarmBtn = hit.collider.GetComponentInParent<FireAlarmButton>();
                    if (alarmBtn != null)
                    {
                        alarmBtn.PushAlarmButton();
                        Debug.Log("[Emulator] Pressed Fire Alarm Button!");
                        return;
                    }

                    // 3. Fire Extinguisher
                    var ext = hit.collider.GetComponentInParent<FireExtinguisher>();
                    if (ext != null)
                    {
                        currentHeldExtinguisher = ext;
                        ext.PullSafetyPin();
                        Debug.Log("[Emulator] Picked up Fire Extinguisher and pulled safety pin!");
                        return;
                    }

                    // 4. Door Heat Checker
                    var doorHeat = hit.collider.GetComponentInParent<DoorHeatChecker>();
                    if (doorHeat != null)
                    {
                        doorHeat.CheckDoorHeatWithHand();
                        Debug.Log("[Emulator] Checked Door Heat!");
                        return;
                    }

                    // 5. NPC Rescue
                    var npc = hit.collider.GetComponentInParent<NPCSimulator>();
                    if (npc != null)
                    {
                        npc.RescueNPC();
                        Debug.Log("[Emulator] Rescued NPC!");
                        return;
                    }
                }
            }

            // Hold Space / Left Mouse to Spray Extinguisher
            if (currentHeldExtinguisher != null)
            {
                if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
                {
                    currentHeldExtinguisher.TrySpray();
                }
                else if (Input.GetKeyUp(KeyCode.Space) || Input.GetMouseButtonUp(0))
                {
                    currentHeldExtinguisher.StopSpray();
                }
            }
        }
    }
}
