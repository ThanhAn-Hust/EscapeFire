using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EscapeFire.Scenario
{
    /// <summary>
    /// What a 114 fire call must contain (plan Phần B.1-2, rule R02): the address, the floor, what is burning and
    /// whether anyone is trapped. Pure logic (EditMode-testable): feed it what the caller said (speech-to-text), it
    /// keeps which items were heard and what to ask back. Matching ignores Vietnamese diacritics (STT output varies).
    /// </summary>
    public sealed class Call114Checker
    {
        public enum Item { Address, Floor, What, People }

        private static readonly Item[] Order = { Item.Address, Item.Floor, Item.What, Item.People };
        private readonly HashSet<Item> _heard = new HashSet<Item>();

        public bool Has(Item i) => _heard.Contains(i);
        public bool IsComplete => _heard.Count == Order.Length;

        public IEnumerable<Item> Missing
        {
            get { foreach (var i in Order) if (!_heard.Contains(i)) yield return i; }
        }

        /// <summary>Adds one utterance; returns the items it newly covered.</summary>
        public List<Item> Hear(string transcript)
        {
            var added = new List<Item>();
            if (string.IsNullOrWhiteSpace(transcript)) return added;
            string t = " " + Plain(transcript) + " ";
            Check(Item.Address, Regex.IsMatch(t, @"dia chi|duong|\bpho\b|\btoa\b|bach khoa|dai hoc|\btruong\b|\bngo\b|\bquan\b|phuong|\bso \d+"), added);
            Check(Item.Floor, Regex.IsMatch(t, @"\btang\s*(\d+|mot|hai|ba|bon|nam|sau|bay|tam|chin|muoi|tret)\b"), added);
            Check(Item.What, Regex.IsMatch(t, @"\bchay\b") && Regex.IsMatch(t, @"server|may chu|tu dien|\bdien\b|phong|\blab\b|thiet bi|hoa chat|\bkhoi\b|may tinh"), added);
            Check(Item.People, Regex.IsMatch(t, @"mac ket|bi ket|nguoi bi|\bngat\b|bat tinh|nan nhan|khong (co )?ai|con nguoi|co nguoi|bi thuong|\bkhong co nguoi"), added);
            return added;
        }

        /// <summary>The 114 operator's question for the first missing item (null when complete).</summary>
        public string NextQuestion()
        {
            foreach (var i in Missing) return Question(i);
            return null;
        }

        public static string Question(Item i)
        {
            switch (i)
            {
                case Item.Address: return "114 xin nghe. Địa chỉ đám cháy ở đâu, tòa nhà nào?";
                case Item.Floor: return "Cháy ở tầng mấy?";
                case Item.What: return "Cái gì đang cháy? Cháy thiết bị điện hay chất gì?";
                default: return "Có ai bị mắc kẹt hoặc bị thương không?";
            }
        }

        private void Check(Item i, bool matched, List<Item> added)
        {
            if (matched && _heard.Add(i)) added.Add(i);
        }

        /// <summary>Lower case, no diacritics, đ → d.</summary>
        public static string Plain(string s)
        {
            string d = s.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(d.Length);
            foreach (char c in d)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
