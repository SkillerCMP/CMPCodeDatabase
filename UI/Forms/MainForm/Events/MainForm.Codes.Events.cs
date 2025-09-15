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
                    txtCodePreview.Text = e.Node?.Tag?.ToString() ?? string.Empty;
                }

        private void TreeCodes_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
                {
                    AddCodeToCollector(e.Node!);
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
            string raw = node.Tag?.ToString() ?? string.Empty;
            if (HasUnresolvedPlaceholders(raw))
            {
                MessageBox.Show("This code still has unresolved placeholders ({MOD}, [MOD], [Amount:...], or generic [Tags]). Fill them first.",
                    "Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string code = Apply64BitHexBlocking(raw);

            if (collectorWindow != null && !collectorWindow.IsDisposed)
            {
if (BlockIfUnresolvedForCollector(node)) return;
                collectorWindow.AddItem(name, code);
            }
            else
            {

                if (BlockIfUnresolvedForCollector(node)) return;
if (!collectorFallback.ContainsKey(name)) collectorFallback[name] = code;
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
                        txtCodePreview.Clear();
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
                        txtCodePreview.Text = tpl;
                        if (originalNodeNames.TryGetValue(node, out string? _))
                        {
                            if (appliedModNames.ContainsKey(node)) appliedModNames.Remove(node);
                            ClearModHighlight();
    node.Text = GetDisplayName(node);
                        }
                    }
                }

        private void SelectMod(TreeNode node)
                {
if (node == null) return;
if (!GateOnAction(node)) return;

string tpl = originalCodeTemplates.ContainsKey(node) ? originalCodeTemplates[node] : node.Tag?.ToString() ?? string.Empty;
if (string.IsNullOrEmpty(tpl)) { MessageBox.Show("No code to modify!"); return; }

// Start at caret; advance left-to-right through remaining tags
int nextStart = txtCodePreview?.SelectionStart ?? 0;

while (true)
{
    // Re-scan tags each pass (positions may shift after replacements)
    var tags = new System.Collections.Generic.List<(string raw, int start, int end)>();
    for (int j = 0; j < tpl.Length; )
    {
        int s = tpl.IndexOf('[', j);
        if (s < 0) break;
        int e = tpl.IndexOf(']', s + 1);
        if (e < 0) break;
        string rawTag = tpl.Substring(s + 1, e - s - 1).Trim();
        tags.Add((rawTag, s, e));
        j = e + 1;
    }

    if (tags.Count == 0) break;

    // Choose next tag at/after current cursor; else first
    int which = -1;
    for (int k = 0; k < tags.Count; k++)
    {
        if (nextStart <= tags[k].end) { which = k; break; }
    }
    if (which < 0) which = 0;

    var (raw, s0, e0) = tags[which];
        try { txtCodePreview.HideSelection = false; HighlightModRange(s0, (e0 - s0 + 1)); txtCodePreview.Select(s0, (e0 - s0 + 1)); txtCodePreview.ScrollToCaret(); txtCodePreview.Update(); } catch { }
        try {
            txtCodePreview.HideSelection = false;
            HighlightModRange(s0, (e0 - s0 + 1));
            txtCodePreview.Select(s0, (e0 - s0 + 1));
            txtCodePreview.ScrollToCaret();
            txtCodePreview.Update();
        } catch { }
    string core = StripTagLabel(raw);

    // 1) Grid/table-backed tags (Excel-style)
    if (modHeaders.ContainsKey(raw) || modHeaders.ContainsKey(core))
    {
        var headers = modHeaders.ContainsKey(raw) ? modHeaders[raw] : modHeaders[core];
        var rows    = modRows.ContainsKey(raw)    ? modRows[raw]    : modRows[core];

        using (var gd = new ModGridDialog(raw, headers, rows))
        {
            if (gd.ShowDialog(this) != DialogResult.OK) break; // stop chain on cancel
            var chosen = gd.SelectedValue ?? string.Empty;
            if (string.IsNullOrEmpty(chosen)) break;

            // Replace this tag instance
            tpl = tpl.Substring(0, s0) + chosen + tpl.Substring(e0 + 1);
            node.Tag = tpl;
            txtCodePreview.Text = tpl;
                try { txtCodePreview.HideSelection = false; txtCodePreview.Select(s0, Math.Max(0, Math.Min((e0 - s0 + 1), txtCodePreview.TextLength - s0))); txtCodePreview.ScrollToCaret(); txtCodePreview.Update(); } catch { }

            if (!string.IsNullOrWhiteSpace(gd.SelectedDisplay)) AppendAppliedModName(node, gd.SelectedDisplay!);

            // Advance caret past inserted text and continue
            nextStart = s0 + chosen.Length;
            continue;
        }
    }
    // 2) Simple list (value-name pairs)
    else if (modDefinitions.ContainsKey(raw) || modDefinitions.ContainsKey(core))
    {
        var items = modDefinitions.ContainsKey(raw) ? modDefinitions[raw] : modDefinitions[core];
        using (var dd = new SimpleModDialog(core, items, $"{node.Text} — {core}"))
        {
            if (dd.ShowDialog(this) != DialogResult.OK) break;
            var chosenVal = dd.SelectedValue ?? string.Empty;
            if (string.IsNullOrEmpty(chosenVal)) break;

            tpl = tpl.Substring(0, s0) + chosenVal + tpl.Substring(e0 + 1);
            node.Tag = tpl;
            txtCodePreview.Text = tpl;
                try { txtCodePreview.HideSelection = false; txtCodePreview.Select(s0, Math.Max(0, Math.Min((e0 - s0 + 1), txtCodePreview.TextLength - s0))); txtCodePreview.ScrollToCaret(); txtCodePreview.Update(); } catch { }

            if (!string.IsNullOrWhiteSpace(dd.SelectedName)) AppendAppliedModName(node, dd.SelectedName!);

            nextStart = s0 + chosenVal.Length;
            continue;
        }
    }
    // 3) Special numeric Amount dialog (with optional <Label>)
    else if (TryParseSpecialAmountTag2(raw, out var tTitle, out var tDef, out var tType, out var tEndian, out var tBox))
    {
        using (var dlg = new SpecialAmountDialog(tTitle, tDef, tType, tEndian, tBox))
        {
            if (dlg.ShowDialog(this) != DialogResult.OK) break;
            var v = dlg.SelectedHex ?? string.Empty;
            if (string.IsNullOrEmpty(v)) break;

            tpl = tpl.Substring(0, s0) + v + tpl.Substring(e0 + 1);
            node.Tag = tpl;
            txtCodePreview.Text = tpl;
                try { txtCodePreview.HideSelection = false; txtCodePreview.Select(s0, Math.Max(0, Math.Min((e0 - s0 + 1), txtCodePreview.TextLength - s0))); txtCodePreview.ScrollToCaret(); txtCodePreview.Update(); } catch { }

            AppendAppliedModName(node, tTitle);

            nextStart = s0 + v.Length;
            continue;
        }
    }
    else
    {
        // Unknown tag type; stop the chain
        MessageBox.Show($"Unknown MOD tag: [{raw}]");
        break;
    }
}

// Update display name after batch
ClearModHighlight();
    node.Text = GetDisplayName(node);
}
}

}