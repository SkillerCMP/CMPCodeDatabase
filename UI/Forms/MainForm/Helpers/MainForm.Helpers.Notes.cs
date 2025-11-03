// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Notes.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────
using System.Linq;  // make sure this is at the top
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private string UnescapeNote(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s
                .Replace("\r\n", "\n")
                .Replace("\n", "\n")
                .Replace("\t", "\t")
                .Replace("\r", "\n");
        }

        private void ShowNoteForNode(TreeNode node)
        {
            if (node == null) return;
            if (nodeNotes.TryGetValue(node, out string? noteHtml))
            {
                using (var f = new NotesViewerForm(GetCopyName(node), noteHtml ?? string.Empty))
                {
                    f.ShowDialog(this);
                }
            }
            else
            {
                MessageBox.Show("No note available for this item.");
            }
        }

        // NEW: auto-open group popup notes on expand
        private void TreeCodes_AfterExpand_ShowGroupNote(object? sender, TreeViewEventArgs e)
{
    var node = e.Node;
    if (node == null) return;

    // only auto-open popup-style notes ({{...}}) that the loader put into nodePopupNotes
    if (nodePopupNotes.TryGetValue(node, out var popups) && popups != null && popups.Count > 0)
    {
        string key = GetCopyName(node);
        if (string.IsNullOrWhiteSpace(key))
            key = node.Text ?? string.Empty;

        // honor "don't show again this session"
        if (suppressedPopupNotes.Contains(key))
            return;

        string html = string.Join("<hr/>", popups.Select(UnescapeNote));
        using (var dlg = new NotePopupDialog(GetCopyName(node), html))
        {
            var result = dlg.ShowDialog(this);
            if (dlg.Suppress)
                suppressedPopupNotes.Add(key);
        }
    }

    // IMPORTANT:
    // do NOT auto-open nodeNotes[...] here -- single-brace { ... } stays manual
}
    }
}
