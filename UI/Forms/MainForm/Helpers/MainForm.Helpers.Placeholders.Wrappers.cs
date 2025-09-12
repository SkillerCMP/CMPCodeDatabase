// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Placeholders.Wrappers.cs
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

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>
        /// Compatibility wrapper: old call name that forwards to the mods-aware version.
        /// Keeps all existing call sites compiling while enforcing the new rules.
        /// </summary>
        private bool HasUnresolvedPlaceholders(string codeBody)
        {
            return HasUnresolvedPlaceholders_ModsAware(codeBody);
        }
    }
}
