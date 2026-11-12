using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using EscapeFire.Core;
using EscapeFire.Hazards;
using EscapeFire.Interactions;

namespace EscapeFire.Generation
{
    /// <summary>
    /// Rules of a generated room (plan V4.4): the fire burns from the start; the learner may fight it with the right
    /// extinguisher or leave. Water / foam on an electrical (class E) fire = electrocution (fail, rule R03 / NFPA 75);
    /// CO2 on a solid (class A) fire knocks the flames down but the embers re-ignite once (CO2 cools very little).
    /// Reaching an exit corridor ends the drill.
    /// </summary>
    public class GeneratedDrill : MonoBehaviour
    {
        public enum State { Idle, Running, Won, Failed }

        [SerializeField] private float shockDistance = 4f;

        private ScenarioSpec _spec;
        private FireHazard _fire;
        private readonly List<Bounds> _exits = new List<Bounds>();
        private Transform _head;
        private bool _co2Used, _waterUsed, _reignited;

        public State Current { get; private set; } = State.Idle;
        public string Outcome { get; private set; } = "";
        public bool FireOut => _fire != null && _fire.IsExtinguished;
        public bool Reignited => _reignited;
        public FireHazard Fire => _fire;

        public void Begin(ScenarioSpec spec, FireHazard fire, List<Bounds> exits)
        {
            _spec = spec;
            if (_fire != null) _fire.OnFireExtinguished -= OnExtinguished;
            _fire = fire;
            _exits.Clear();
            _exits.AddRange(exits);
            _co2Used = _waterUsed = _reignited = false;
            if (_fire != null) _fire.OnFireExtinguished += OnExtinguished;
            Current = State.Running;
            Outcome = "";
            Say($"Tình huống: {spec.Title}. Cháy lớp {spec.FireClass}. Chọn đúng bình hoặc thoát ra lối EXIT.");
        }

        private void OnEnable() => EventBus<ExtinguisherSprayEvent>.Subscribe(OnSpray);

        private void OnDisable()
        {
            EventBus<ExtinguisherSprayEvent>.Unsubscribe(OnSpray);
            if (_fire != null) _fire.OnFireExtinguished -= OnExtinguished;
        }

        private void OnSpray(ExtinguisherSprayEvent e)
        {
            if (Current != State.Running) return;
            if (e.Type == ExtinguisherType.CO2) _co2Used = true; else _waterUsed = true;
            if (e.Type == ExtinguisherType.WaterFoam && _spec.FireClass == "E" && _fire != null && !_fire.IsExtinguished && HeadNear(_fire.BasePoint, shockDistance))
                End(State.Failed, "Điện giật: phun nước/bọt vào thiết bị điện đang cháy (dùng bình CO2, ngắt điện trước).");
        }

        private void OnExtinguished()
        {
            if (Current != State.Running) return;
            if (_spec.FireClass == "A" && _co2Used && !_waterUsed && !_reignited)
            {
                _reignited = true;
                StartCoroutine(Reignite());
                return;
            }
            Say("Đám cháy đã tắt. Rời phòng theo lối EXIT.");
        }

        private IEnumerator Reignite()
        {
            yield return new WaitForSeconds(3f);
            if (Current != State.Running || _fire == null) yield break;
            _fire.ResetFire();
            Say("Than hồng bùng cháy lại! CO2 làm ngạt lửa nhưng làm nguội rất ít — giấy, gỗ còn âm ỉ dễ bùng lại. Dùng bình nước/bọt hoặc sơ tán.");
        }

        private void Update()
        {
            if (Current != State.Running) return;
            if (_head == null)
            {
                var origin = FindFirstObjectByType<XROrigin>();
                if (origin != null && origin.Camera != null) _head = origin.Camera.transform;
                if (_head == null) return;
            }
            foreach (var b in _exits)
                if (b.Contains(_head.position))
                {
                    End(State.Won, FireOut ? "Dập tắt đám cháy và thoát ra an toàn." : "Thoát ra an toàn (đám cháy chưa tắt — đã báo lực lượng chữa cháy).");
                    return;
                }
        }

        private bool HeadNear(Vector3 p, float distance) => _head != null && Vector3.Distance(_head.position, p) <= distance;

        private void End(State s, string outcome)
        {
            Current = s;
            Outcome = outcome;
            Say(outcome);
            // VL.1: an unseen generated room is the transfer test of "right agent for the fire class" (R03)
            if (_co2Used || _waterUsed)
            {
                bool right = !(_spec.FireClass == "E" && _waterUsed) && !_reignited;
                EscapeFire.Analytics.SkillStore.Record(new List<EscapeFire.Analytics.SkillObservation>
                {
                    new EscapeFire.Analytics.SkillObservation { Skill = "R03_RIGHT_AGENT", Correct = right, Why = "phòng sinh: " + _spec.Title }
                });
            }
        }

        private static void Say(string m) => EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(m));
    }
}
