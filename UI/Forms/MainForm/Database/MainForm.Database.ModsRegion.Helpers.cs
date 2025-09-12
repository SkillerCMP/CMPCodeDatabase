// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.ModsRegion.Helpers.cs
// Purpose: Database discovery, selector, and tree building.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Helpers for parsing the MODS region (^6 = MODS:).
    /// This variant intentionally omits ShouldShowModBadge to avoid duplicate definitions.
    /// </summary>
    public partial class MainForm
    {
        /// <summary>
        /// Returns the substring of a CMP text that starts *after* the "^6 = MODS:" line.
        /// If the marker is missing, returns the original text (back-compat).
        /// </summary>
        private static string ExtractModsRegion(string allText)
        {
            if (string.IsNullOrWhiteSpace(allText)) return allText ?? string.Empty;

            // Normalize newlines then scan for the marker line.
            var lines = allText.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var t = lines[i].Trim();
                if (Regex.IsMatch(t, @"^\^?\s*6\s*=\s*MODS\s*:?\s*$", RegexOptions.IgnoreCase))
                {
                    return string.Join("\n", lines, i + 1, lines.Length - (i + 1));
                }
            }
            return allText; // marker not found → legacy files still work
        }

        /// <summary>
        /// Collects all block names declared under the MODS region: [Name] ... [/Name].
        /// Indentation/spacing inside the region is ignored.
        /// </summary>
        private static HashSet<string> CollectAvailableModNames(string modsText)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(modsText)) return names;

            foreach (Match m in Regex.Matches(
                         modsText,
                         @"\[(?<name>[A-Za-z0-9_]+)\][\s\S]*?\[/\k<name>\]",
                         RegexOptions.IgnoreCase))
            {
                names.Add(m.Groups["name"].Value);
            }
            return names;
        }
    }
}
