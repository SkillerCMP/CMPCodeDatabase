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

            // Build with stable names so we can toggle by name (no brittle indexes)
            codesContextMenu.Items.Add(new ToolStripMenuItem("Reset Code", null,
                (s, e) => ResetCode(treeCodes.SelectedNode!)) { Name = "mnuResetCode" });

            codesContextMenu.Items.Add(new ToolStripMenuItem("Select MOD", null,
                (s, e) => SelectMod(treeCodes.SelectedNode!)) { Name = "mnuSelectMod" });

            codesContextMenu.Items.Add(new ToolStripMenuItem("Copy to Clipboard", null,
                (s, e) => CopyCode(treeCodes.SelectedNode!)) { Name = "mnuCopyClipboard" });

            codesContextMenu.Items.Add(new ToolStripMenuItem("Copy to Collector", null,
                (s, e) => AddCodeToCollector(treeCodes.SelectedNode!)) { Name = "mnuCopyToCollector" });

            // NEW: Add Checked → Collector
            codesContextMenu.Items.Add(new ToolStripMenuItem("Add Checked to Collector", null, (s, e) =>
            {
                int added = AddAllCheckedToCollector();
                if (added <= 0)
                    MessageBox.Show(this, "No checked code items were found.", "Add Checked to Collector",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }) { Name = "mnuAddCheckedToCollector" });

            codesContextMenu.Items.Add(new ToolStripMenuItem("Show Note", null,
                (s, e) => ShowNoteOrPopupForNode(treeCodes.SelectedNode!)) { Name = "mnuShowNote" });

            codesContextMenu.Opening += CodesContextMenu_Opening;
            treeCodes.ContextMenuStrip = codesContextMenu;
        }

        private void CodesContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var node = treeCodes.SelectedNode;

            bool modsAvailableForNode  = node != null && nodeHasMod.Contains(node);
            bool notesAvailableForNode = node != null &&
                                         (nodeNotes.ContainsKey(node) ||
                                          (nodePopupNotes.ContainsKey(node)));

            // Toggle visibility/enabled via names (robust if order changes)
            if (codesContextMenu?.Items != null)
            {
                var miSelectMod   = codesContextMenu.Items["mnuSelectMod"];
                var miShowNote    = codesContextMenu.Items["mnuShowNote"];
                var miAddChecked  = codesContextMenu.Items["mnuAddCheckedToCollector"];
                var miCopyToColl  = codesContextMenu.Items["mnuCopyToCollector"];

                if (miSelectMod != null)  miSelectMod.Visible  = modsAvailableForNode;
                if (miShowNote  != null)  miShowNote.Visible   = notesAvailableForNode;

                // Enable single-item add only if selected node has code text
                if (miCopyToColl != null) miCopyToColl.Enabled = (node?.Tag != null) &&
                                                                 !string.IsNullOrWhiteSpace(node.Tag.ToString());

                // Enable batch add only if ANY checked code exists in the tree
                if (miAddChecked != null) miAddChecked.Enabled = AnyCheckedCodeNodes();
            }
        }

        private void CopyCode(TreeNode? node)
        {
            if (node?.Tag == null) return;
            string text = $"{GetCopyName(node)}{Environment.NewLine}{Apply64BitHexBlocking(node.Tag?.ToString() ?? string.Empty)}";
            Clipboard.SetText(text);
        }
		
		private bool AnyCheckedCodeNodes()
        {
            if (treeCodes == null) return false;
            foreach (TreeNode n in treeCodes.Nodes)
                if (IsCheckedCodeNodeDeep(n)) return true;
            return false;
        }

        private int AddAllCheckedToCollector()
        {
            if (treeCodes == null) return 0;
            var list = new List<TreeNode>();
            foreach (TreeNode n in treeCodes.Nodes)
                CollectCheckedCodeNodes(n, list);

            int added = 0;
            foreach (var node in list)
            {
                try { AddCodeToCollector(node); added++; }
                catch { /* keep going */ }
            }
            return added;
        }

        private bool IsCheckedCodeNodeDeep(TreeNode n)
        {
            if (n == null) return false;
            if (n.Checked && n.Tag != null && !string.IsNullOrWhiteSpace(n.Tag.ToString()))
                return true;
            foreach (TreeNode c in n.Nodes)
                if (IsCheckedCodeNodeDeep(c)) return true;
            return false;
        }

        private void CollectCheckedCodeNodes(TreeNode n, List<TreeNode> acc)
        {
            if (n == null) return;
            if (n.Checked && n.Tag != null && !string.IsNullOrWhiteSpace(n.Tag.ToString()))
                acc.Add(n);
            foreach (TreeNode c in n.Nodes)
                CollectCheckedCodeNodes(c, acc);
        }
    }
}