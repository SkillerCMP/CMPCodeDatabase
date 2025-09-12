// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Placeholders.ExtractSet.cs
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
using System.Text.RegularExpressions;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>
        /// Extract variable names defined via 'set [Name]:...' or 'set [Name]=...' in a code block.
        /// Brackets are optional; we accept 'set Name:...' as well, but [Name] wins if both appear.
        /// </summary>
        internal static HashSet<string> ExtractSetVariableNames(string codeText)
        {
            var setNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(codeText)) return setNames;

            // Match: set [name]:..., set [name]=..., allowing whitespace; multiline.
            var rxBracket = new Regex(@"^\s*set\s*\[\s*(?<name>[^\]\r\n]+)\s*\]\s*[:=]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in rxBracket.Matches(codeText))
            {
                var n = m.Groups["name"]?.Value?.Trim();
                if (!string.IsNullOrEmpty(n)) setNames.Add(n);
            }

            // Also accept non-bracket form: set name: ... (avoid capturing hex literals etc. by stopping at : or =)
            var rxBare = new Regex(@"^\s*set\s+(?<name>[A-Za-z0-9_]+)\s*[:=]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in rxBare.Matches(codeText))
            {
                var n = m.Groups["name"]?.Value?.Trim();
                if (!string.IsNullOrEmpty(n)) setNames.Add(n);
            }

            return setNames;
        }
    }
}
