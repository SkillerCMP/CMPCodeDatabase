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
                        // Reflow: two tokens per output line: "XXXXXXXX YYYYYYYY"
                        for (int t = 0; t < matches.Count; t += 2)
                        {
                            string left = matches[t].Value.ToUpperInvariant();
                            string right = (t + 1 < matches.Count) ? matches[t + 1].Value.ToUpperInvariant() : "00000000";
                            sb.Append(left).Append(' ').Append(right).Append('\n');
                        }
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
