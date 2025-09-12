// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Integration/MainForm.CollectorGameSync.cs
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
    /// (Wiring is invoked from MainForm.Wiring.OnHandleCreated)
    /// </summary>
    public partial class MainForm : Form
    {
        private bool _collectorSyncWired;
        private string? _activeGameKey;

        internal void TryWireCollectorSync()
        {
            if (_collectorSyncWired) return;
            if (treeGames == null || dbSelector == null) return;

            treeGames.AfterSelect += TreeGames_AfterSelect_ClearCollector;
            dbSelector.SelectedIndexChanged += DbSelector_SelectedIndexChanged_ClearCollector;

            _collectorSyncWired = true;
        }

        private void TreeGames_AfterSelect_ClearCollector(object? sender, TreeViewEventArgs e)
        {
            string? key = e?.Node?.Tag?.ToString();
            SetActiveGameKeyAndClearIfChanged(key);
        }

        private void DbSelector_SelectedIndexChanged_ClearCollector(object? sender, EventArgs e)
        {
            SetActiveGameKeyAndClearIfChanged($"DB::{dbSelector?.SelectedItem?.ToString()}");
        }

        private void SetActiveGameKeyAndClearIfChanged(string? key)
        {
            if (string.Equals(_activeGameKey ?? string.Empty, key ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                return;

            _activeGameKey = key;

            try { collectorFallback.Clear(); } catch { /* ignore */ }

            try
            {
                if (collectorWindow != null && !collectorWindow.IsDisposed)
                    collectorWindow.SetActiveGame(key);
            }
            catch { /* non-fatal */ }
        }
    }
}