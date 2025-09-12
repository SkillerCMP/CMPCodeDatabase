// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Resolve.DatabasesRoot.cs
// Purpose: Helpers to resolve filesystem paths and database roots.
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
    public partial class MainForm : Form
    {
        private TreeNode ResolveDatabasesRootNode()
        {
            if (treeGames == null) throw new InvalidOperationException("treeGames not initialized.");

            if (treeGames.Nodes.Count == 1)
                return treeGames.Nodes[0];

            string? selText = dbSelector?.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selText))
            {
                foreach (TreeNode n in treeGames.Nodes)
                {
                    if (string.Equals(n.Text, selText, StringComparison.OrdinalIgnoreCase))
                        return n;
                }
            }

            var cur = treeGames.SelectedNode;
            if (cur != null)
            {
                while (cur.Parent != null) cur = cur.Parent;
                return cur;
            }

            if (treeGames.Nodes.Count > 0)
                return treeGames.Nodes[0];

            string caption = !string.IsNullOrWhiteSpace(selText) ? selText! : "Databases";
            return treeGames.Nodes.Add(caption);
        }

        private string ResolveDatabasesRoot()
        {
            var node = ResolveDatabasesRootNode();
            return node?.Text ?? string.Empty;
        }
    }
}
