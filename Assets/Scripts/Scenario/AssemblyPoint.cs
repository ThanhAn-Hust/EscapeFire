using UnityEngine;
using EscapeFire.Core;
using EscapeFire.NPC;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// Assembly point roll call (plan Phần B.1-15, rule R12): reaching the assembly point does not end the drill by
    /// itself — the warden asks who is still inside and the learner answers by pressing one of the report buttons on
    /// his board with a fingertip (voice could replace it later). The right answer depends on what really happened:
    /// victims left in the stairwell / corridor, someone trapped in the lift. Then the drill is complete.
    /// </summary>
    public class AssemblyPoint : MonoBehaviour
    {
        public enum Report { AllOut = 0, StillInsideStairwell = 1, StillInsideCorridorOrLift = 2 }

        [SerializeField] private GameObject board;            // shown when the learner arrives
        [SerializeField] private GameObject hoaHere;          // Hoa standing with the group if she got out
        [SerializeField] private GameObject minhHere;
        [SerializeField] private float answerTimeout = 45f;

        private float _arrivedAt = -1f;

        public static AssemblyPoint Instance { get; private set; }
        public bool Arrived => _arrivedAt >= 0f;
        public bool Answered { get; private set; }
        public bool AnswerCorrect { get; private set; }
        public Report Expected => ExpectedReport();

        public void Configure(GameObject reportBoard, GameObject hoa, GameObject minh)
        {
            board = reportBoard; hoaHere = hoa; minhHere = minh;
        }

        private void Awake()
        {
            Instance = this;
            if (board != null) board.SetActive(false);
            if (hoaHere != null) hoaHere.SetActive(false);
            if (minhHere != null) minhHere.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>The learner reached the assembly point (called by the exit zone).</summary>
        public void LearnerArrived()
        {
            if (Arrived) return;
            _arrivedAt = Time.time;
            if (board != null) board.SetActive(true);
            bool hoaOut = false, minhOut = true;
            foreach (var n in FindObjectsByType<NPCSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (n.Role == NPCRole.HoaFaint) hoaOut = n.IsRescued;
            var minh = FindFirstObjectByType<PanicRunner>();
            if (minh != null) minhOut = !minh.Trapped;
            if (hoaHere != null) hoaHere.SetActive(hoaOut);
            if (minhHere != null) minhHere.SetActive(minhOut);
            Say("[Đội trưởng PCCC] Bạn ra rồi, tốt! Đã đủ người chưa? Còn ai trong tòa nhà không, ở đâu? Báo cho tôi trên bảng.");
        }

        public void ReportAllOut() => Answer(Report.AllOut);
        public void ReportStairwell() => Answer(Report.StillInsideStairwell);
        public void ReportCorridorOrLift() => Answer(Report.StillInsideCorridorOrLift);

        public void Answer(Report r)
        {
            if (!Arrived || Answered) return;
            Answered = true;
            Report expected = ExpectedReport();
            bool liftAndStairs = expected == Report.StillInsideCorridorOrLift && AnyVictimSafeInStairwell();
            AnswerCorrect = r == expected || (liftAndStairs && r == Report.StillInsideStairwell);
            Say(AnswerCorrect
                ? "[Đội trưởng PCCC] Rõ. Tôi báo ngay vị trí cho lực lượng chữa cháy cứu nạn. Cảm ơn bạn!"
                : $"[Đội trưởng PCCC] Không đúng — {Truth()} Báo sai làm lực lượng cứu nạn mất thời gian vàng.");
            EventBus<RollCallAnsweredEvent>.Publish(new RollCallAnsweredEvent(AnswerCorrect));
            Finish();
        }

        private void Update()
        {
            if (Arrived && !Answered && Time.time - _arrivedAt > answerTimeout)
            {
                Answered = true;
                AnswerCorrect = false;
                Say("[Đội trưởng PCCC] Bạn chưa báo người còn trong tòa nhà — lần sau hãy báo ngay khi tới điểm tập kết.");
                Finish();
            }
        }

        private static void Finish()
        {
            var c = LabProgressiveScenarioController.Instance;
            if (c != null) c.TriggerHallwayEvacuationWin();
        }

        private static Report ExpectedReport()
        {
            var minh = FindFirstObjectByType<PanicRunner>();
            if (minh != null && minh.Trapped) return Report.StillInsideCorridorOrLift;
            bool anyVictim = false;
            foreach (var v in VictimBody.All)
            {
                anyVictim = true;
                if (!v.IsSafe) return Report.StillInsideCorridorOrLift;
            }
            return anyVictim ? Report.StillInsideStairwell : Report.AllOut;
        }

        private static bool AnyVictimSafeInStairwell()
        {
            foreach (var v in VictimBody.All) if (v.IsSafe) return true;
            return false;
        }

        private static string Truth()
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var v in VictimBody.All) parts.Add(v.IsSafe ? "một nạn nhân bất tỉnh ở buồng thang A tầng 3" : "một nạn nhân bất tỉnh ở hành lang tầng 3");
            var minh = FindFirstObjectByType<PanicRunner>();
            if (minh != null && minh.Trapped) parts.Add("Minh kẹt trong thang máy");
            return parts.Count == 0 ? "mọi người đã ra hết." : "còn " + string.Join(", ", parts) + ".";
        }

        private static void Say(string m) => EventBus<MentorMessageEvent>.Publish(new MentorMessageEvent(m));
    }
}
