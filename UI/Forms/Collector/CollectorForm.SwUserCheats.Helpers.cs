// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.SwUserCheats.Helpers.cs
// Purpose: Save Wizard export Game ID helpers and strict SW pair extraction.
// Notes:
//  • Split from CollectorForm.SwUserCheats.Export.cs during cleanup pass 14.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Export.SaveWizard;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        private static List<string> TryGetActiveGameIdsFromMainForm()
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                    if (f is MainForm mf)
                        return SplitGameIds(mf.CurrentGameIdsCsv);
            }
            catch { /* ignore */ }
            return new List<string>();
        }

        private static List<string> SplitGameIds(string? csvOrText)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(csvOrText)) return list;

            // Accept "CUSA12345,CUSA23456" or whitespace separated; also tolerate quotes.
            foreach (Match m in SwGameIdRegex.Matches(csvOrText))
            {
                var id = m.Value.Trim().ToUpperInvariant();
                if (!list.Contains(id))
                    list.Add(id);
            }
            return list;
        }

private static string GuessLikelyGameId(string? activeKey)
        {
            if (string.IsNullOrWhiteSpace(activeKey))
                return string.Empty;

            // Try to find common title-id patterns.
            var m = SwLikelyGameIdRegex.Match(activeKey);
            return m.Success ? m.Groups[1].Value.ToUpperInvariant() : string.Empty;
        }

        private static bool TryExtractAddrValuePairs(string input, out string pairs)
        {
            // STRICT Save Wizard format:
            //   Every non-empty line must be exactly "8HEX 8HEX" (optionally prefixed with '$' for CMP layouts).
            //   If *any* other directive/text line exists (e.g., Apollo "delete next/insert next"), we reject the cheat.
            //
            // This prevents exporting mixed SW+Apollo blocks into swusercheats.xml.
            pairs = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var rxLine = SwPairLineRegex;

            var tokens = new List<string>();

            // Normalize line endings and validate line-by-line.
            var lines = input.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            foreach (var raw in lines)
            {
                var line = (raw ?? string.Empty).Trim();
                if (line.Length == 0)
                    continue;

                var mm = rxLine.Match(line);
                if (!mm.Success)
                {
                    // Any non-matching line means this is not pure SW code.
                    pairs = string.Empty;
                    return false;
                }

                tokens.Add(mm.Groups[1].Value.ToUpperInvariant());
                tokens.Add(mm.Groups[2].Value.ToUpperInvariant());
            }

            if (tokens.Count == 0)
                return false;

            var joined = string.Join(' ', tokens);
            joined = SwCodeNormalize.NormalizePairs(joined);

            if (!SwCodeNormalize.HasEvenTokenCount(joined))
                return false;

            pairs = joined;
            return true;
        }

    }
}
