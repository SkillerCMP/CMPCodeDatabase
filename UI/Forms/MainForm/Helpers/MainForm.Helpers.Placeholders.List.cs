// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Placeholders.List.cs
// Purpose: UI composition, menus, and layout for the MainForm.
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
using System.Linq;
using System.Text.RegularExpressions;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>
        /// Get a distinct list of unresolved bracket tokens [X] that are not defined by 'set [X]:...' or 'set [X]=...'.
        /// </summary>
        internal static List<string> FindUnresolvedPlaceholders(string codeText)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(codeText)) return list;

            // Reuse helpers from the other partial
            var setNames = ExtractSetVariableNames(codeText);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Local copy of the bracket regex to avoid visibility issues if files compile separately first
            var bracket = new Regex(@"\[(?<name>[^\[\]\r\n]+)\]", RegexOptions.Compiled);

            foreach (Match m in bracket.Matches(codeText))
            {
                var name = m.Groups["name"]?.Value?.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                if (setNames.Contains(name)) continue;   // ignore set-defined variables

                if (seen.Add(name))
                    list.Add(name);
            }

            return list;
        }
    }
}
