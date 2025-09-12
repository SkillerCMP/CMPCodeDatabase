// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Context/MainForm.ContextMenu.cs
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
        private void InitializeContextMenu()
                {
                    codesContextMenu = new ContextMenuStrip();
                    codesContextMenu.Items.Add("Reset Code", null, (s, e) => ResetCode(treeCodes.SelectedNode!));
                    codesContextMenu.Items.Add("Select MOD", null, (s, e) => SelectMod(treeCodes.SelectedNode!));
                    codesContextMenu.Items.Add("Copy to Clipboard", null, (s, e) => CopyCode(treeCodes.SelectedNode!));
                    codesContextMenu.Items.Add("Copy to Collector", null, (s, e) => AddCodeToCollector(treeCodes.SelectedNode!));
                    codesContextMenu.Items.Add("Show Note", null, (s, e) => ShowNoteForNode(treeCodes.SelectedNode!));
                    codesContextMenu.Opening += CodesContextMenu_Opening;
                    treeCodes.ContextMenuStrip = codesContextMenu;
                }

        private void CodesContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
                {
                    var node = treeCodes.SelectedNode;
                    bool modsAvailableForNode = node != null && nodeHasMod.Contains(node);
                    bool notesAvailableForNode = node != null && nodeNotes.ContainsKey(node);

                    if (codesContextMenu.Items.Count >= 5)
                    {
                        codesContextMenu.Items[1].Visible = modsAvailableForNode; // Select MOD only when node itself has mod
                        codesContextMenu.Items[4].Visible = notesAvailableForNode; // Show Note only when node has note
                    }
                }

        private void CopyCode(TreeNode? node)
        {
            if (node?.Tag == null) return;
            string text = $"{GetCopyName(node)}{Environment.NewLine}{Apply64BitHexBlocking(node.Tag?.ToString() ?? string.Empty)}";
            Clipboard.SetText(text);
        }

    }
}
