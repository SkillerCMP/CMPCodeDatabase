// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Placeholders.ModsAware.cs
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
    /// <summary>
    /// Mods-aware placeholder check aligned with -M- rules.
    /// </summary>
    public partial class MainForm
    {
        // New method name so we don't collide with any existing HasUnresolvedPlaceholders.
        private bool HasUnresolvedPlaceholders_ModsAware(string codeBody)
        {
            if (string.IsNullOrWhiteSpace(codeBody)) return false;

            // Names of Normal [MOD] tables parsed for the current file/game.
            var available = new HashSet<string>(modDefinitions.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (Match m in Regex.Matches(codeBody, @"\[(?<inner>[^\]]+)\]"))
            {
                var inner = m.Groups["inner"].Value.Trim();
                if (inner.Length == 0) continue;

                var parts = inner.Split(':');
                var baseName = parts[0].Trim();

                // Special [MOD] — Amount + 3 sections  -> needs user input
                if (parts.Length == 4 && baseName.Equals("Amount", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Normal [MOD] declared under ^6 = MODS: -> needs selection
                if (available.Contains(baseName))
                    return true;
            }

            // Bare variables (e.g., [hash], [size], or bare [AMOUNT] without a table) do not block.
            return false;
        }
    }
}
