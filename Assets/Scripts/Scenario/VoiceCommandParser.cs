using System.Globalization;
using System.Text;

namespace EscapeFire.Scenario
{
    public enum VoiceCommand
    {
        None,
        RaiseFireAlarm,   // "Cháy! Có cháy!", "báo cháy", "fire"
        CallForHelp       // "cứu", "gọi 114"
    }

    /// <summary>
    /// Maps a (Vietnamese or English) speech transcript to a command. Case-insensitive; diacritics are respected
    /// when present and ignored only for unaccented input. Pure; runs once per utterance.
    /// </summary>
    public static class VoiceCommandParser
    {
        // Accented phrases are used when the transcript carries diacritics (normal STT output), so that
        // "chạy đi" (run!) or "chảy nước" (water leaking) are NOT mistaken for "cháy" (fire).
        private static readonly string[] ALARM_ACCENTED = { "cháy", "báo động", "fire", "alarm" };
        private static readonly string[] HELP_ACCENTED = { "cứu", "114", "help" };
        // Fallback for transcripts typed/returned without diacritics.
        private static readonly string[] ALARM_PLAIN = { "chay", "bao dong", "fire", "alarm" };
        private static readonly string[] HELP_PLAIN = { "cuu", "114", "help" };

        public static VoiceCommand Parse(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript)) return VoiceCommand.None;

            bool hasDiacritics = false;
            foreach (char c in transcript)
            {
                if (c > 127) { hasDiacritics = true; break; }
            }

            string text = " " + (hasDiacritics ? Simplify(transcript) : Normalize(transcript)) + " ";
            string[] alarm = hasDiacritics ? ALARM_ACCENTED : ALARM_PLAIN;
            string[] help = hasDiacritics ? HELP_ACCENTED : HELP_PLAIN;

            for (int i = 0; i < alarm.Length; i++)
            {
                if (ContainsWord(text, alarm[i])) return VoiceCommand.RaiseFireAlarm;
            }
            for (int i = 0; i < help.Length; i++)
            {
                if (ContainsWord(text, help[i])) return VoiceCommand.CallForHelp;
            }
            return VoiceCommand.None;
        }

        /// <summary>Lowercase (NFC), punctuation → single spaces, diacritics kept.</summary>
        public static string Simplify(string input)
        {
            string lowered = input.ToLowerInvariant().Normalize(NormalizationForm.FormC);
            var sb = new StringBuilder(lowered.Length);
            bool lastSpace = false;
            foreach (char c in lowered)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastSpace = false;
                }
                else if (!lastSpace)
                {
                    sb.Append(' ');
                    lastSpace = true;
                }
            }
            return sb.ToString().Trim();
        }

        /// <summary>Lowercase, remove Vietnamese diacritics (incl. đ), keep letters/digits/spaces only.</summary>
        public static string Normalize(string input)
        {
            string decomposed = input.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            bool lastSpace = false;
            foreach (char c in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastSpace = false;
                }
                else if (!lastSpace)
                {
                    sb.Append(' ');
                    lastSpace = true;
                }
            }
            return sb.ToString().Trim();
        }

        private static bool ContainsWord(string paddedText, string phrase)
        {
            return paddedText.Contains(" " + phrase + " ");
        }
    }
}
