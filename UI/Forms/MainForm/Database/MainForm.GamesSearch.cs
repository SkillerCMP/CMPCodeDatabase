// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.GamesSearch.cs
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
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private TextBox? _txtGameSearch;
        private int _lastHitIndex = -1;

        // Snapshot of the FULL games list. We only grow/refresh this when
        // an external load adds more items — never when we are filtering.
        private sealed class GameEntry
        {
            public string Text = "";
            public string? Tag;
            public int ImageIndex;
            public int SelectedImageIndex;
        }
        private List<GameEntry> _allGames = new List<GameEntry>();
        private int _baseSig = 0;
        private bool _filteringInternal = false;

        internal void TryInitGameSearchUI()
        {
            if (_txtGameSearch != null) return;

            _txtGameSearch = new TextBox
            {
                PlaceholderText = "Search games (name or ID)…"
            };

            // Place ONLY under the Games section (left), just below the DB selector and above the tree
            var parent = treeGames.Parent;
            parent.Controls.Add(_txtGameSearch);

            int left = treeGames.Left;
            int width = treeGames.Width;
            int top = treeGames.Top - 28; // default slot above the tree
            try
            {
                if (dbSelector != null)
                    top = Math.Max(top, dbSelector.Bottom + 6);
            }
            catch { /* dbSelector may be null in some layouts */ }

            int height = Math.Max(23, _txtGameSearch.PreferredHeight);
            _txtGameSearch.SetBounds(left, top, width, height);

            // If it overlaps the tree, push the tree down and reclaim height so bottom stays stable
            if (treeGames.Top <= _txtGameSearch.Bottom)
            {
                int delta = (_txtGameSearch.Bottom + 6) - treeGames.Top;
                treeGames.Top += delta;
                treeGames.Height = Math.Max(100, treeGames.Height - delta);
            }

            // Capture base list if available
            CaptureCurrentAsBaseIfBetter();

            // Wire search behavior
            _txtGameSearch.TextChanged += (s, e) =>
            {
                // Keep base snapshot fresh only when the visible list grew (external reload)
                CaptureCurrentAsBaseIfBetter();
                ApplyGamesFilter(_txtGameSearch.Text);
            };
            _txtGameSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    JumpToFirstMatch(_txtGameSearch.Text, next: true);
                    e.Handled = true;
                }
            };
        }

        private void CaptureCurrentAsBaseIfBetter()
        {
            // If we are rebuilding due to our own filtering, ignore
            if (_filteringInternal) return;

            // If no base yet, or current tree has MORE items (likely external reload), refresh base
            if (_allGames.Count == 0 || treeGames.Nodes.Count > _allGames.Count)
            {
                _allGames = treeGames.Nodes.Cast<TreeNode>().Select(n => new GameEntry
                {
                    Text = n.Text ?? "",
                    Tag = n.Tag?.ToString(),
                    ImageIndex = n.ImageIndex,
                    SelectedImageIndex = n.SelectedImageIndex
                }).ToList();
                _baseSig = ComputeSig(_allGames);
            }
        }

        private static int ComputeSig(List<GameEntry> list)
        {
            unchecked
            {
                int h = 17;
                foreach (var g in list)
                {
                    h = h * 31 + (g.Text?.GetHashCode() ?? 0);
                    h = h * 31 + (g.Tag?.GetHashCode() ?? 0);
                }
                return h;
            }
        }

        private void ApplyGamesFilter(string? query)
        {
            var q = (query ?? "").Trim();

            // Empty → show full base list (and if we never captured base, capture now)
            if (q.Length == 0)
            {
                if (_allGames.Count == 0)
                    CaptureCurrentAsBaseIfBetter();

                if (_allGames.Count > 0 && treeGames.Nodes.Count != _allGames.Count)
                {
                    RebuildGamesTree(_allGames);
                }
                _lastHitIndex = -1;
                return;
            }

            // Filter by name or path/ID in Tag
            var filtered = _allGames.Where(g =>
                (g.Text?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                (g.Tag?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
            ).ToList();

            RebuildGamesTree(filtered);
        }

        private void RebuildGamesTree(List<GameEntry> items)
        {
            _filteringInternal = true;
            treeGames.BeginUpdate();
            try
            {
                var selectedTag = treeGames.SelectedNode?.Tag?.ToString();
                treeGames.Nodes.Clear();
                foreach (var g in items)
                {
                    var n = new TreeNode(g.Text)
                    {
                        Tag = g.Tag,
                        ImageIndex = g.ImageIndex,
                        SelectedImageIndex = g.SelectedImageIndex
                    };
                    treeGames.Nodes.Add(n);
                    if (selectedTag != null && string.Equals(selectedTag, g.Tag, StringComparison.OrdinalIgnoreCase))
                        treeGames.SelectedNode = n;
                }
                if (treeGames.SelectedNode == null && treeGames.Nodes.Count > 0)
                    treeGames.SelectedNode = treeGames.Nodes[0];
            }
            finally
            {
                treeGames.EndUpdate();
                _filteringInternal = false;
            }
        }

        private void JumpToFirstMatch(string query, bool next = false)
        {
            if (string.IsNullOrWhiteSpace(query) || treeGames.Nodes.Count == 0)
                return;

            var q = query.Trim();
            var nodes = treeGames.Nodes.Cast<TreeNode>().ToList();

            int start = next && _lastHitIndex >= 0 ? (_lastHitIndex + 1) % nodes.Count : 0;
            for (int k = 0; k < nodes.Count; k++)
            {
                int i = (start + k) % nodes.Count;
                var n = nodes[i];
                var text = n.Text ?? "";
                var tag = n.Tag?.ToString() ?? "";
                if (text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    treeGames.SelectedNode = n;
                    n.EnsureVisible();
                    _lastHitIndex = i;
                    return;
                }
            }
        }
    }
}
