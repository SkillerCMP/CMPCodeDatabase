using System.Text;
// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Events/MainForm.Codes.Events.cs
// Purpose: MainForm event handlers for buttons/menus and code actions.
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
        private void TreeCodes_AfterSelect(object? sender, TreeViewEventArgs e)
                {
                    txtCodePreview!.Text = e.Node?.Tag?.ToString() ?? string.Empty;
                }

        private void TreeCodes_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            var node = e.Node;
            if (node == null) return;

            // Optional UX: if a code still has unresolved MOD placeholders, open "Select MOD" first.
            // If the user resolves all placeholders, auto-add to Collector and then Reset the code.
            if (CMPCodeDatabase.Core.Settings.AppSettings.Instance.DoubleClickResolveModsThenAddToCollector)
            {
                // IMPORTANT: Use the same unresolved-placeholder logic as the Collector gate (supports [NAME] / [NAME<...>] and Amount).
                string codeText = node.Tag as string ?? node.Tag?.ToString() ?? string.Empty;

                // If Tag is empty, fall back to the original template (so double-click still prompts on untouched codes).
                if (string.IsNullOrWhiteSpace(codeText) && originalCodeTemplates != null)
                {
                    if (originalCodeTemplates.TryGetValue(node, out var tpl) && tpl != null)
                        codeText = tpl;
                }

                if (!string.IsNullOrWhiteSpace(codeText) && IsUnresolvedForCollector(codeText))
                {
                    // Ensure the node is selected so Select MOD operates on the correct code.
                    treeCodes.SelectedNode = node;

                    // Open Select MOD / Amount dialog.
                    SelectMod(node);

                    // Re-check after the dialog. Only add+reset if fully resolved.
                    string after = node.Tag as string ?? node.Tag?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(after) && originalCodeTemplates != null)
                    {
                        if (originalCodeTemplates.TryGetValue(node, out var tpl2) && tpl2 != null)
                            after = tpl2;
                    }

                    if (!string.IsNullOrWhiteSpace(after) && !IsUnresolvedForCollector(after))
                    {
                        AddCodeToCollector(node);
                        ResetCode(node);
                    }

                    return; // handled (either added+reset or user canceled/left placeholders)
                }
            }

            AddCodeToCollector(node);
        }

        private void TreeCodes_KeyDown(object? sender, KeyEventArgs e)
                {
                    if (e.KeyCode == Keys.Delete)
                    {
                        if (treeCodes.SelectedNode != null) ResetCode(treeCodes.SelectedNode);
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.F2)
                    {
                        treeCodes.SelectedNode?.BeginEdit();
                        e.Handled = true;
                    }
                }

        private void AddCodeToCollector(TreeNode node)
                {
        if (node?.Tag == null) return;
        if (!GateOnAction(node)) return;
            string name = GetCopyName(node);
			string groupPath = GetGroupPath(node);
		// Collector names double as Save Wizard group paths.
		// GetGroupPath() is intentionally UI-friendly ("A / B / C"). Normalize to backslashes here.
		groupPath = groupPath.Replace(" / ", "\\").Replace("/", "\\");
		if (!string.IsNullOrWhiteSpace(groupPath))
			name = groupPath + "\\" + name;
            string raw = node.Tag?.ToString() ?? string.Empty;
            if (HasUnresolvedPlaceholders(raw))
            {
                MessageBox.Show("This code still has unresolved placeholders ({MOD}, [MOD], [Amount:...], or generic [Tags]). Fill them first.",
                    "Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string code = Apply64BitHexBlocking(raw);
            var meta = GetCollectorMetaForNode(node);


            // Where to send items depends on the layout mode.
            if (CMPCodeDatabase.Core.Settings.AppSettings.Instance.UseTabbedPreviewCollector
                && collectorTab != null && !collectorTab.IsDisposed)
            {
                if (BlockIfUnresolvedForCollector(node, code)) return;
                collectorTab.AddItem(name, code, meta.Author, meta.Description);
            }
            else if (collectorWindow != null && !collectorWindow.IsDisposed)
            {
                if (BlockIfUnresolvedForCollector(node, code)) return;
                collectorWindow.AddItem(name, code, meta.Author, meta.Description);
            }
            else
            {
                if (BlockIfUnresolvedForCollector(node)) return;

                if (!collectorFallback.ContainsKey(name)) collectorFallback[name] = code;
                collectorFallbackMeta[name] = meta;
            }
            ShowCollectorWindow();
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
                {
                    // Clear screens
                    try
                    {
                        treeGames.Nodes.Clear();
                        treeCodes.Nodes.Clear();
                        txtCodePreview!.Clear();
                    }
                    catch { /* ignore */ }

                    // Reload the full database list and games
                    LoadDatabaseSelector();
    
                }

        private void ResetCode(TreeNode node)
                {
                    ClearAppliedModNames(node);

                    if (node == null) return;
                    if (originalCodeTemplates.TryGetValue(node, out string? tpl))
                    {
                        node.Tag = tpl;
                        txtCodePreview!.Text = tpl;
                        if (originalNodeNames.TryGetValue(node, out string? _))
                        {
                            if (appliedModNames.ContainsKey(node)) appliedModNames.Remove(node);
                            ClearModHighlight();
    node.Text = GetDisplayName(node);
                        }
                    }
                }

    }
}
