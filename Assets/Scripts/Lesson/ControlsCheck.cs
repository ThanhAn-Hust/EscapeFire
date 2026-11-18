using Unity.XR.CoreUtils;
using UnityEngine;

namespace EscapeFire.Lesson
{
    /// <summary>
    /// Station 0 (playtest 18/11: nobody taught the controllers, yet station 3 needs a two-handed extinguisher): four
    /// small tasks, in order — pick up the block (grip), press the green button with the fingertip (poke), get down
    /// low (real crouch), walk onto the circle with the left thumbstick. Each is checked from what really happened.
    /// </summary>
    public class ControlsCheck : MonoBehaviour, IPracticeCheck
    {
        [SerializeField] private Transform block;
        [SerializeField] private float blockRestY;
        [SerializeField] private PokeAction button;
        [SerializeField] private Transform circle;
        [SerializeField] private float circleRadius = 0.45f;

        private Transform _head, _origin;
        private int _step;
        private float _lowFor;

        public int StepsDone => _step;

        public void Configure(Transform grabBlock, PokeAction pokeButton, Transform floorCircle)
        {
            block = grabBlock; blockRestY = grabBlock.position.y; button = pokeButton; circle = floorCircle;
        }

        public bool Done => _step >= 4;

        public string Instruction
        {
            get
            {
                string[] tasks =
                {
                    "Bóp nút HÔNG tay cầm (ngón giữa) để nắm khối hộp vàng, nhấc nó lên.",
                    "Chạm đầu NGÓN TRỎ vào nút xanh trên bàn (không cần bóp nút nào).",
                    "Khom người hoặc ngồi xuống thật thấp (đầu thấp hơn mặt bàn) trong 1 giây.",
                    "Gạt cần analog TRÁI để đi tới vòng tròn xanh trên sàn.",
                };
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < tasks.Length; i++)
                    sb.Append(i < _step ? "<color=#7CFC9A>✓ " : i == _step ? "<color=#FFFFFF>→ " : "<color=#8C96A0>  ")
                      .Append(i + 1).Append(". ").Append(tasks[i]).Append("</color>\n");
                return sb.ToString();
            }
        }

        public string Feedback => _step < 4 ? $"Làm quen tay cầm: {_step}/4" : "";

        private void Update()
        {
            if (_head == null)
            {
                var o = FindFirstObjectByType<XROrigin>();
                if (o != null && o.Camera != null) { _head = o.Camera.transform; _origin = o.transform; }
                if (_head == null) return;
            }
            switch (_step)
            {
                case 0: if (block != null && block.position.y > blockRestY + 0.2f) _step++; break;
                case 1: if (button != null && button.Presses > 0) _step++; break;
                case 2:
                    _lowFor = _head.position.y - _origin.position.y < 0.95f ? _lowFor + Time.deltaTime : 0f;
                    if (_lowFor > 1f) _step++;
                    break;
                case 3:
                    Vector3 d = _head.position - circle.position; d.y = 0f;
                    if (d.magnitude < circleRadius) _step++;
                    break;
            }
        }
    }
}
