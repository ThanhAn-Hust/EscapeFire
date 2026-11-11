using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using EscapeFire.Core;

namespace EscapeFire.NPC
{
    /// <summary>
    /// Helping a conscious but weak victim out (plan Phần B.1-10): get down to her level, take her under the arm /
    /// shoulder with one hand and lift; she stands up bent low and walks where the supporting hand goes. Let go and
    /// she stops; pull too hard / fast and you lose her. Driven by the controller's real grip through XRI.
    /// </summary>
    [RequireComponent(typeof(NPCSimulator))]
    public class EscortGrip : MonoBehaviour
    {
        [SerializeField] private float reach = 0.18f;
        [Tooltip("The rescuer's head must be lower than this to take hold of someone lying on the floor (m).")]
        [SerializeField] private float maxHeadHeightToLift = 1.25f;
        [SerializeField] private float supportDistance = 0.35f;
        [Tooltip("If the hand gets this far from her, the grip is lost (m).")]
        [SerializeField] private float loseGripDistance = 1.5f;

        private NPCSimulator _npc;
        private Transform _armpit;
        private Transform _head;
        private XRBaseInputInteractor _holder;
        private bool _lostHintShown;
        private float _heldSince;

        public bool IsSupported => _holder != null;

        private void Awake()
        {
            _npc = GetComponent<NPCSimulator>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "Shoulder_R" || t.name == "Shoulder_L") { _armpit = t; break; }
        }

        private void Start()
        {
            var origin = FindFirstObjectByType<XROrigin>();
            if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
        }

        /// <summary>Where a hand takes hold: the shoulder / armpit (falls back to chest height).</summary>
        public Vector3 GripPoint => _armpit != null ? _armpit.position : transform.position + Vector3.up * (_npc.IsFainted && !_npc.IsRescued ? 0.25f : 1.2f);

        private void Update()
        {
            if (_npc.Role != NPCRole.HoaFaint || !_npc.IsFainted) return;
            if (_holder == null)
            {
                var hand = FindGrippingHand();
                if (hand == null) return;
                if (!_npc.IsRescued && _head != null && _head.position.y - transform.position.y > maxHeadHeightToLift)
                {
                    if (GameSession.Mode == TrainingMode.Guided && !_lostHintShown)
                    {
                        _lostHintShown = true;
                        EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Hạ thấp người xuống ngang tầm Hoa rồi mới đỡ bạn ấy dậy — khói ở trên cao."));
                    }
                    return;
                }
                _holder = hand;
                _heldSince = Time.time;
                _npc.SetEscortTarget(hand.transform, supportDistance);
                if (!_npc.IsRescued) _npc.RescueNPC();
                return;
            }
            if (!_holder.selectInput.ReadIsPerformed())
            {
                Release();
                return;
            }
            Vector3 gap = _holder.transform.position - transform.position;
            gap.y = 0f;
            if (Time.time - _heldSince > 0.6f && gap.magnitude > loseGripDistance)
            {
                Release();
                EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent("Đi chậm lại! Hoa không theo kịp và đã tuột tay."));
            }
        }

        private void Release()
        {
            _holder = null;
            _npc.SetEscortTarget(null, supportDistance);
        }

        private XRBaseInputInteractor FindGrippingHand()
        {
            Vector3 p = GripPoint;
            XRBaseInputInteractor best = null;
            float bestSqr = reach * reach;
            foreach (var i in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (!i.isActiveAndEnabled || i.hasSelection || !i.selectInput.ReadIsPerformed()) continue;
                float d = (i.transform.position - p).sqrMagnitude;
                if (d <= bestSqr) { bestSqr = d; best = i; }
            }
            return best;
        }
    }
}
