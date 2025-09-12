// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Find/MainForm.Find.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────


namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void PromptFind()
                {
                    using var f = new InputBox("Find text (codes / notes):");
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        string q = f.Value.Trim();
                        if (q.Length == 0) return;
                        // Search node text / notes
                        foreach (TreeNode n in treeCodes.Nodes)
                        {
                            var hit = FindInTree(n, q);
                            if (hit != null) { treeCodes.SelectedNode = hit; hit.EnsureVisible(); return; }
                        }
                        MessageBox.Show("No matches in Codes tree.");
                    }
                }

        private TreeNode? FindInTree(TreeNode node, string q)
                {
                    if (node.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return node;
                    if (nodeNotes.TryGetValue(node, out var note) && note.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return node;
                    foreach (TreeNode c in node.Nodes)
                    {
                        var t = FindInTree(c, q);
                        if (t != null) return t;
                    }
                    return null;
                }

    }
}
