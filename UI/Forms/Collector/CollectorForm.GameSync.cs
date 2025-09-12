// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.GameSync.cs
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
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Keeps the Collector in sync with the currently active game.
    /// Clears items when the active game key changes.
    /// </summary>
    public partial class CollectorForm : Form
    {
        private string? _activeGameKey;

        /// <summary>
        /// Set the active game key. If it changes, the Collector list and map are cleared.
        /// </summary>
        public void SetActiveGame(string? key)
        {
            if (string.Equals(_activeGameKey ?? string.Empty, key ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                return;

            _activeGameKey = key;
            try
            {
                clbCollector.Items.Clear();
                collectorCodeMap.Clear();
            }
            catch { /* non-fatal */ }
        }
    }
}