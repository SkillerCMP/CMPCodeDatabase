// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Context/MainForm.Games.ContextMenu.cs
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Right-click context menu for the Games tree (left pane).
    /// Adds "See info" that works for both folder games and top-level .txt games.
    /// </summary>
    public partial class MainForm : Form
    {
        private bool _gamesCtxWired;
        private ContextMenuStrip? gamesContextMenu;

        internal void TryWireGamesContextMenu()
        {
            if (_gamesCtxWired) return;
            if (treeGames == null) return;

            gamesContextMenu = new ContextMenuStrip();
            gamesContextMenu.Items.Add("See info", null, (s, e) => ShowGameInfo(treeGames.SelectedNode));
            gamesContextMenu.Opening += GamesContextMenu_Opening;

            treeGames.ContextMenuStrip = gamesContextMenu;
            _gamesCtxWired = true;
        }

        private void GamesContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var node = treeGames?.SelectedNode;
            bool hasNode = node != null && node.Tag != null;
            if (gamesContextMenu != null && gamesContextMenu.Items.Count > 0)
            {
                gamesContextMenu.Items[0].Enabled = hasNode;
            }
            e.Cancel = !hasNode;
        }

        private void ShowGameInfo(TreeNode? node)
        {
            if (node == null) return;
            var tag = node.Tag?.ToString();
            var sb = new StringBuilder();

            sb.AppendLine($"Game: {node.Text}");

            if (!string.IsNullOrWhiteSpace(tag) && Directory.Exists(tag))
            {
                // Folder game
                int fileCount = 0;
                int folderCount = 0;
                try
                {
                    folderCount = Directory.GetDirectories(tag, "*", SearchOption.AllDirectories).Length;
                    fileCount = Directory.GetFiles(tag, "*.txt", SearchOption.AllDirectories).Length;
                }
                catch { /* ignore */ }

                sb.AppendLine("Type: Folder");
                sb.AppendLine($"Path: {tag}");
                sb.AppendLine($"Subfolders: {folderCount}");
                sb.AppendLine($".txt files: {fileCount}");
            }
            else if (!string.IsNullOrWhiteSpace(tag) && File.Exists(tag) && string.Equals(Path.GetExtension(tag), ".txt", StringComparison.OrdinalIgnoreCase))
            {
                // Top-level .txt game
                string? nameFromHeader = TryReadGameNameFromTxt_Context(tag);
                var fi = new FileInfo(tag);
                sb.AppendLine("Type: Single .txt");
                sb.AppendLine($"File: {tag}");
                if (!string.IsNullOrWhiteSpace(nameFromHeader) && !string.Equals(nameFromHeader, node.Text, StringComparison.Ordinal))
                    sb.AppendLine($"^3 = NAME: {nameFromHeader}");
                sb.AppendLine($"Size: {fi.Length:N0} bytes");
                sb.AppendLine($"Modified: {fi.LastWriteTime}");
            }
            else
            {
                sb.AppendLine("Type: Unknown");
                if (!string.IsNullOrWhiteSpace(tag)) sb.AppendLine($"Tag: {tag}");
            }

            MessageBox.Show(this, sb.ToString(), "Game Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Local helper to parse ^3 = NAME: ... (kept separate name to avoid conflicts with other partials)
        private string? TryReadGameNameFromTxt_Context(string file)
        {
            try
            {
                foreach (var raw in File.ReadLines(file))
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var line = raw.Trim();
                    var m = Regex.Match(line, @"^\^3\s*=\s*NAME\s*:\s*(.+)$", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        var name = m.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(name)) return name;
                    }
                    if (line.StartsWith("^") && !line.StartsWith("^3", StringComparison.Ordinal))
                        break;
                }
            }
            catch { /* ignore */ }
            return null;
        }
    }
}