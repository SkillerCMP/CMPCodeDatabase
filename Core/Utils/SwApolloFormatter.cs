// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Utils/SwApolloFormatter.cs
// Purpose: WinForms UI logic for this form.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace CMPCodeDatabase.Formatters
{
    /// <summary>
    /// Apollo-style "SW format encoding" (Path A, non-conflicting).
    /// - Reflows only hex-only lines (8-hex tokens) into pairs: "XXXXXXXX YYYYYYYY".
    /// - Pads odd token with "00000000".
    /// - Leaves any mixed/script lines verbatim (search/Insert Next/JSON/[Amount:...] etc.).
    /// </summary>
    public static class SwApolloFormatter
    {
        private static readonly Regex OnlyHexAndWhitespace = new Regex(@"^(?:\s|[0-9A-Fa-f])+$", RegexOptions.Compiled);
        private static readonly Regex HexToken8 = new Regex(@"[0-9A-Fa-f]{8}", RegexOptions.Compiled);
        // Reflow threshold: only reformat when there are at least two 32-bit words
        private const int MinWordsForReflow = 2;

        public static string NormalizeSwBlocksForCollector(string text)
        {
            if (text == null) return string.Empty;

            var src = text.Replace("\r\n", "\n"); // normalize
            var lines = src.Split('\n');
            var sb = new StringBuilder(text.Length + 64);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                // Only operate on lines that are purely hex/whitespace and contain at least one 8-hex token
                if (trimmed.Length > 0 && OnlyHexAndWhitespace.IsMatch(trimmed))
                {
                    var matches = HexToken8.Matches(trimmed);
                    if (matches.Count > 0)
                    {
                        // Generic two-column reflow with partial-word padding, only when >= 2 dwords.
                        // Steps:
                        // 1) join all hex (strip spaces), 2) pad final partial to 8 hex,
                        // 3) split into 8-hex words, 4) if odd word count, add filler 00000000,
                        // 5) print two words per line.
                        string clean = Regex.Replace(trimmed, @"\s+", "").ToUpperInvariant();
                        if (clean.Length > 0)
                        {
                            var words = new System.Collections.Generic.List<string>();
                            for (int p = 0; p < clean.Length; p += 8)
                            {
                                int remain = Math.Min(8, clean.Length - p);
                                string w = clean.Substring(p, remain);
                                if (remain < 8) w = w + new string('0', 8 - remain); // keep trailing partial (e.g., 426F78 -> 426F7800)
                                words.Add(w);
                            }

                            // Only reformat when there are at least two full 8-hex tokens (pre-padding)
                            if (matches.Count >= MinWordsForReflow)
                            {
                                if ((words.Count % 2) != 0) words.Add("00000000"); // keep continuation rows two-wide
                                for (int k = 0; k < words.Count; k += 2)
                                    sb.Append(words[k]).Append(' ').Append(words[k + 1]).Append('\n');
                                continue;
                            }
                        }

                        // Below-threshold (single dword or less): keep the original line verbatim
                        sb.Append(line).Append('\n');
                        continue;

                    }
                }

                // Not a hex-only line with 8-hex tokens: keep verbatim
                sb.Append(line).Append('\n');
            }

            // Remove the last trailing newline we added
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n') sb.Length -= 1;
            return sb.ToString().Replace("\n", Environment.NewLine);
        }
    }
}
