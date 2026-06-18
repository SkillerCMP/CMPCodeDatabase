// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.FilesRoot.cs
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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Switch database root to %ROOT%\Files\Database and support games as either folders or top-level .txt files.
    /// Additive: unhooks old handlers and wires new ones without replacing existing parser logic.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Active game's GameID metadata (from ^2 = GameID: ...), if available.
        /// Used by Collector exports (e.g., swusercheats.xml) to auto-select the correct Save Wizard game.
        /// </summary>
        internal string? CurrentGameIdsCsv { get; private set; }

        private bool _dbFilesRootWired;
        private string? _dbRoot_FilesDatabase;   // %ROOT%\Files\Database
        private string? _dbSelectedPath;         // %ROOT%\Files\Database\<Database>

        internal void TryWireDatabaseRootSwitch()
        {
            if (_dbFilesRootWired) return;
            if (dbSelector == null || treeGames == null) return;

            // Unhook old handlers so we can control the root and selection behavior
            try { dbSelector.SelectedIndexChanged -= DbSelector_SelectedIndexChanged; } catch { /* ignore if signature mismatch */ }
            try { treeGames.AfterSelect -= TreeGames_AfterSelect; } catch { /* ignore */ }

            // Hook our new handlers
            dbSelector.SelectedIndexChanged += DbSelector_SelectedIndexChanged_FilesRoot;
            treeGames.AfterSelect += TreeGames_AfterSelect_FilesRoot;

            // Compute new root and populate selector
            _dbRoot_FilesDatabase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "Database");
            LoadDatabaseSelector_FilesRoot();

            _dbFilesRootWired = true;
        }

        private void LoadDatabaseSelector_FilesRoot()
        {
            dbSelector.Items.Clear();
            if (string.IsNullOrWhiteSpace(_dbRoot_FilesDatabase) || !Directory.Exists(_dbRoot_FilesDatabase))
                return;

            foreach (var folder in Directory.EnumerateDirectories(_dbRoot_FilesDatabase))
                dbSelector.Items.Add(Path.GetFileName(folder));

            if (dbSelector.Items.Count > 0)
                dbSelector.SelectedIndex = 0;
        }

        private void DbSelector_SelectedIndexChanged_FilesRoot(object? sender, EventArgs e)
        {
            if (dbSelector.SelectedItem == null) return;
            if (string.IsNullOrWhiteSpace(_dbRoot_FilesDatabase)) return;

            var dbName = dbSelector.SelectedItem?.ToString() ?? string.Empty;
            _dbSelectedPath = Path.Combine(_dbRoot_FilesDatabase, dbName);
            LoadGames_FilesRoot();
        

            // Reset search state and base cache when switching DB
            try
            {
                try
                {
                    _suppressGameSearchTextChanged = true;
                    _txtGameSearch?.Clear();
                }
                finally
                {
                    _suppressGameSearchTextChanged = false;
                }

                // Clear base so no stale list is reused.
                _allGames.Clear();
                _lastHitIndex = -1;

                // Capture the freshly loaded DB as the new base, then apply empty filter
                CaptureCurrentAsBaseIfBetter();
                ApplyGamesFilter(string.Empty);
            }
            catch { /* best-effort */ }
}

        private void LoadGames_FilesRoot()
        {
            treeGames.BeginUpdate();
            try
            {
                treeGames.Nodes.Clear();
                if (string.IsNullOrWhiteSpace(_dbSelectedPath) || !Directory.Exists(_dbSelectedPath)) return;

                // Folders as games (legacy behavior)
                foreach (var folder in Directory.EnumerateDirectories(_dbSelectedPath))
                    treeGames.Nodes.Add(new TreeNode(Path.GetFileName(folder)) { Tag = folder });

                // Top-level *.txt files as games (new behavior; fast listing)
                foreach (var file in Directory.EnumerateFiles(_dbSelectedPath, "*.txt"))
                {
                    // Use filename-derived display for fast listing (no header reads here)
                    var display = CleanDisplayNameFromFile(file);
                    treeGames.Nodes.Add(new TreeNode(display) { Tag = file });
                }

                foreach (TreeNode n in treeGames.Nodes) n.Collapse();
            }
            finally
            {
                treeGames.EndUpdate();
            }

            // Clear codes panel
            treeCodes.Nodes.Clear();
            txtCodePreview.Clear();
            try { NotifyCodesTreeRebuilt_REFRESH(); } catch { }
        }

        private void TreeGames_AfterSelect_FilesRoot(object? sender, TreeViewEventArgs e)
        {
            var tag = e.Node?.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(tag)) return;

            if (Directory.Exists(tag))
            {
                // Use existing folder recursion logic
                LoadCodes(tag);
            }
            else if (File.Exists(tag) && string.Equals(Path.GetExtension(tag), ".txt", StringComparison.OrdinalIgnoreCase))
            {
                LoadCodesFromSingleFile(tag, e.Node!.Text);
            }
        }

        /// <summary>
        /// Build the right-hand codes tree from a single top-level game .txt file.
        /// Reuses the existing ParseCodeFilesInFolder() for consistency.
        /// </summary>
        private void LoadCodesFromSingleFile(string filePath, string gameDisplayName)
        {
            treeCodes.BeginUpdate();
treeCodes.Nodes.Clear();

            // Track current game's GameID metadata for Collector exports
            try
            {
                var header = TryReadHeader(filePath);
                CurrentGameIdsCsv = string.IsNullOrWhiteSpace(header.GameId) ? null : header.GameId.Trim();
            }
            catch { CurrentGameIdsCsv = null; }


            // Reset parsing state (mirrors LoadCodes behavior)
            originalCodeTemplates.Clear();
            originalNodeNames.Clear();
            nodeNotes.Clear();
            nodeHasMod.Clear();
            appliedModNames.Clear();
            modDefinitions.Clear();
            modHeaders.Clear();
            modRows.Clear();

            var root = new TreeNode(gameDisplayName);
            treeCodes.Nodes.Add(root);
            ParseCodeFilesInFolder(new[] { filePath }, root);

            ApplyBoldStyling(treeCodes.Nodes);
            foreach (TreeNode n in treeCodes.Nodes) n.Collapse();
            txtCodePreview.Clear();
            treeCodes.EndUpdate();
            TreeViewExtent.UpdateHorizontalExtent(treeCodes);
            try { NotifyCodesTreeRebuilt_REFRESH(); } catch { }
        }

    }
}
