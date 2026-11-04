using System;
using System.Collections.Generic;
using System.Globalization;

namespace EscapeFire.Analytics
{
    /// <summary>Pure score bookkeeping (no Unity dependency) used by ScoringManager.</summary>
    public sealed class ScoreLedger
    {
        public readonly struct Entry
        {
            public readonly int Delta;
            public readonly string Reason;
            public readonly float Time;
            public Entry(int delta, string reason, float time) { Delta = delta; Reason = reason; Time = time; }
        }

        private readonly List<Entry> _entries = new List<Entry>(32);

        public int Total { get; private set; }
        public IReadOnlyList<Entry> Entries => _entries;

        public Entry Add(int points, string reason, float time)
        {
            if (points < 0) throw new ArgumentOutOfRangeException(nameof(points), "Use Deduct for penalties.");
            return Record(points, reason, time);
        }

        public Entry Deduct(int points, string reason, float time)
        {
            if (points < 0) throw new ArgumentOutOfRangeException(nameof(points), "Pass a positive penalty.");
            return Record(-points, reason, time);
        }

        public static string Format(Entry entry)
        {
            string sign = entry.Delta >= 0 ? "+" : "-";
            return string.Format(CultureInfo.InvariantCulture, "[{0}{1} pts] {2} (Time: {3:F1}s)",
                sign, Math.Abs(entry.Delta), entry.Reason, entry.Time);
        }

        private Entry Record(int delta, string reason, float time)
        {
            var entry = new Entry(delta, reason ?? string.Empty, time);
            Total += delta;
            _entries.Add(entry);
            return entry;
        }
    }
}
