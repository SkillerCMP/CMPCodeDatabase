// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Integration/MainForm.PatcherConfig.cs
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
using System.IO;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        internal string CurrentDatabaseName
        {
            get
            {
                try { return dbSelector?.SelectedItem?.ToString() ?? string.Empty; }
                catch { return string.Empty; }
            }
        }

        internal string GetPatcherExeForCurrentDB()
        {
            var db = CurrentDatabaseName;
            return DbCfg.ResolvePatcherPath(db);
        }
    }
}
