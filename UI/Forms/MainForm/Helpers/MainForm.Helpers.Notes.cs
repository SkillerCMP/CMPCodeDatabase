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


namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private string UnescapeNote(string s)
                        {
                            if (string.IsNullOrEmpty(s)) return string.Empty;
                            return s.Replace("\r\n", "\n").Replace("\n", "\n").Replace("\t", "\t").Replace("\r", "\n");
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
    }
}
