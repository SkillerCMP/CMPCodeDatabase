// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Integration/MainForm.GamesSeeInfo.cs
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
    /// <summary>
    /// Wires the GameInfoContextHelper so "See info" / "Open file..." work for either:
    /// - Folder games (Tag = folder path)
    /// - Single .txt games (Tag = full .txt path)
    /// </summary>
    public partial class MainForm : Form
    {
        private bool _seeInfoWired;

        internal void TryWireSeeInfo()
        {
            if (_seeInfoWired) return;
            if (treeGames == null) return;

            GameInfoContextHelper.Attach(
                treeGames,
                node =>
                {
                    // We only attach to top-level Game nodes
                    if (node == null || node.Parent != null) return null;

                    var tag = node.Tag as string;
                    if (string.IsNullOrWhiteSpace(tag)) return null;

                    if (Directory.Exists(tag))
                        return tag; // folder-style game

                    if (File.Exists(tag) && string.Equals(Path.GetExtension(tag), ".txt", StringComparison.OrdinalIgnoreCase))
                        return tag; // single .txt as game -> pass file path so we parse only this file

                    return null;
                });

            _seeInfoWired = true;
        }
    }
}
