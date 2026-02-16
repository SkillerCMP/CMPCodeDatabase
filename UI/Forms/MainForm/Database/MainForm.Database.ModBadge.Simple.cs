using System;
using System.Text.RegularExpressions;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        
        // Simple scan regex for bracketed placeholders like: [Amount:...], [Joker:...], [STAR:...]
        private static readonly Regex _starSimpleScanRx = new(@"\[(?<inner>[^\[\]]+)\]", RegexOptions.CultureInvariant);

        private static Regex StarSimpleScanRx() => _starSimpleScanRx;

// Simple hard-allow scan (Amount, Joker/JKR, STAR) — mirrors Amount behavior
        private static bool ShouldShowModBadgeSimple(string template)
        {
            if (string.IsNullOrWhiteSpace(template)) return false;

            foreach (Match m in StarSimpleScanRx().Matches(template))
            {
                var inner = m.Groups["inner"].Value.Trim();
                if (string.IsNullOrEmpty(inner)) continue;

                var parts = inner.Split(':');
                if (parts.Length == 0) continue;

                // [Amount:..:..:..] always badges
                if (parts.Length == 4 && parts[0].Equals("Amount", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Hard-allow Joker / JKR / STAR exactly like Amount
                if (parts[0].Equals("Joker", StringComparison.OrdinalIgnoreCase) ||
                    parts[0].Equals("JKR",   StringComparison.OrdinalIgnoreCase) ||
                    parts[0].Equals("STAR",  StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
