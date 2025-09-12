// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Styling.cs
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
        private void ApplyBoldStyling(TreeNodeCollection nodes)
                        {
                            if (nodes == null) return;
                            foreach (TreeNode n in nodes)
                            {
                                ApplyBoldStyling(n);
                                if (n.Nodes != null && n.Nodes.Count > 0)
                                    ApplyBoldStyling(n.Nodes);
                            }
                        }

        private void ApplyBoldStyling(TreeNode node)
                        {
                            if (node == null) return;
                            bool isGroupOrSubgroup = node.Nodes != null && node.Nodes.Count > 0;
                            node.NodeFont = isGroupOrSubgroup ? _boldNodeFont : null;
}
    }
}
