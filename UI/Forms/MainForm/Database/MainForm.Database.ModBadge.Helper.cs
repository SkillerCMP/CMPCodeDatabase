// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.ModBadge.Helper.cs
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
    // Helper partial with the -M- decision logic.
    public partial class MainForm
    {
        /// <summary>
        /// Returns true if the code body references a Special [MOD] ([Amount:...:...:...])
        /// or a Normal [MOD] whose base name exists in availableModNames (parsed below ^6 = MODS:).
        /// </summary>
        private static bool ShouldShowModBadge(string codeBody, HashSet<string> availableModNames)
        {
            if (string.IsNullOrEmpty(codeBody)) return false;
            if (availableModNames == null) return false;

            foreach (Match m in Regex.Matches(codeBody, @"\[(?<inner>[^\]]+)\]"))
            {
                var inner = m.Groups["inner"].Value.Trim();
                if (inner.Length == 0) continue;

                var parts = inner.Split(':');

                // Special [MOD]: [Amount:…:…:…] (case-insensitive)
                if (parts.Length == 4 && parts[0].Equals("Amount", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Normal [MOD]: base name before ':' must exist as a declared block under MODS
                var baseName = parts[0].Trim();
                if (baseName.Length > 0 && availableModNames.Contains(baseName))
                    return true;
            }
            return false;
        }
    }
}
