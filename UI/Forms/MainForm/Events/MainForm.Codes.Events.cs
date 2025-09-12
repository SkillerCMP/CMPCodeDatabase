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
                collectorWindow.AddItem(name, code);
            }
            else
            {
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
                            node.Text = GetDisplayName(node);
                        }
                    }
                }

        private void SelectMod(TreeNode node)
                {
                    if (node == null) return;
                    string tpl = originalCodeTemplates.ContainsKey(node) ? originalCodeTemplates[node] : node.Tag?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(tpl)) { MessageBox.Show("No code to modify!"); return; }

                    // Collect unique tags in template, in the order they appear
                    var modTags = new System.Collections.Generic.List<string>();
                    int p = 0;
                    while ((p = tpl.IndexOf('[', p)) != -1)
                    {
                        int q = tpl.IndexOf(']', p);
                        if (q == -1) break;
                        string tag = tpl.Substring(p + 1, q - p - 1).Trim();
                        if (!string.IsNullOrEmpty(tag) && !modTags.Contains(tag)) modTags.Add(tag);
                        p = q + 1;
                    }
                    
                    // Exclude tags that are satisfied by 'set [name]:...' statements
                    if (modTags.Count > 0)
                    {
                        var _filtered = new List<string>();
                        var _set = ExtractSetVariableNames(tpl);
                        foreach (var t in modTags) { if (!_set.Contains(t)) _filtered.Add(t); }
                        modTags = _filtered;
                    }
    if (modTags.Count == 0) { MessageBox.Show("No MOD tags found for this code."); return; }

                    int caret = txtCodePreview?.SelectionStart ?? -1;

                    foreach (var tag in modTags)
                    {
                
                        int originalTotal = FindAllTagOccurrences(tpl, tag).Count;
        // First, if caret sits inside this tag, fill that single occurrence first
                        if (IsCaretInsideTag(tpl, caret, tag))
                        {
                            // Choose value for caret-targeted occurrence
                            if (modHeaders.ContainsKey(tag))
                            {
                                using var gd = new ModGridDialog(tag, modHeaders[tag], modRows.TryGetValue(tag, out var rows0) ? rows0 : new System.Collections.Generic.List<string[]>(), BuildProgressTitle(node, tag, ComputeCaretIndex(tpl, tag, caret), FindAllTagOccurrences(tpl, tag).Count));

                                // pre-dialog highlight (caret occurrence)
                                try { int __hi=-1; foreach (var o in FindAllTagOccurrences(tpl, tag)) { if (caret >= o.Start && caret <= o.End) { __hi = o.Start; break; } } if (__hi >= 0) { int __e = tpl.IndexOf("]", __hi); int __len = (__e >= __hi) ? (__e - __hi + 1) : 2; HighlightModRange(__hi, __len); } } catch { }
                                                                if (gd.ShowDialog(this) == DialogResult.OK)
                                {
                                    var v = gd.SelectedValue ?? "";
                                    if (!string.IsNullOrEmpty(v))
                                    {
                                        // Find occurrence at/around caret
                                        var occ0 = FindAllTagOccurrences(tpl, tag);
                                        int pos = -1;
                                        foreach (var o in occ0) { if (caret >= o.Start && caret <= o.End) { pos = o.Start; break; } }
                                        if (pos >= 0) { int __end = tpl.IndexOf("]", pos); int __len = (__end >= pos) ? (__end - pos + 1) : 2; HighlightModRange(pos, __len); tpl = ReplaceOneOccurrenceAtIndex(tpl, pos, v); txtCodePreview.Text = tpl; }
                                        if (!string.IsNullOrWhiteSpace(gd.SelectedDisplay)) AppendAppliedModName(node, gd.SelectedDisplay!);
                                    }
                                }
                            }
                            else
                            {
                                using var sd = new SimpleModDialog(tag, modDefinitions.TryGetValue(tag, out var list0) ? list0 : new System.Collections.Generic.List<(string, string)>(), BuildProgressTitle(node, tag, ComputeCaretIndex(tpl, tag, caret), FindAllTagOccurrences(tpl, tag).Count));

                                // pre-dialog highlight (caret occurrence)
                                try { int __hi=-1; foreach (var o in FindAllTagOccurrences(tpl, tag)) { if (caret >= o.Start && caret <= o.End) { __hi = o.Start; break; } } if (__hi >= 0) { int __e = tpl.IndexOf("]", __hi); int __len = (__e >= __hi) ? (__e - __hi + 1) : 2; HighlightModRange(__hi, __len); } } catch { }
                                                                if (sd.ShowDialog(this) == DialogResult.OK)
                                {
                                    var v = sd.SelectedValue ?? "";
                                    if (!string.IsNullOrEmpty(v))
                                    {
                                        var occ0 = FindAllTagOccurrences(tpl, tag);
                                        int pos = -1;
                                        foreach (var o in occ0) { if (caret >= o.Start && caret <= o.End) { pos = o.Start; break; } }
                                        if (pos >= 0) { int __end = tpl.IndexOf("]", pos); int __len = (__end >= pos) ? (__end - pos + 1) : 2; HighlightModRange(pos, __len); tpl = ReplaceOneOccurrenceAtIndex(tpl, pos, v); txtCodePreview.Text = tpl; }
                                        if (!string.IsNullOrWhiteSpace(sd.SelectedName)) AppendAppliedModName(node, sd.SelectedName!);
                                    }
                                }
                            }
                        }

                        // Now, ALWAYS step through all remaining occurrences (if any)
                        int initialTotal = originalTotal;
                
                            while (true)
                            {
                                var occList = FindAllTagOccurrences(tpl, tag);
                                if (occList.Count == 0) break;
                                int index = (initialTotal - occList.Count) + 1;
                                int startIdx = occList[0].Start;

                                // Special [Amount:VAL:TYPE:ENDIAN] handling
                                
                if (TryParseTextAmountTag(tag, out var tBase, out var tEnc))
                {
                    string encToken = string.IsNullOrWhiteSpace(tEnc) ? "UTF08" : tEnc;
                    var encoding = MapEncodingToken(encToken);
                    int maxBytes = int.MaxValue;
                    if (!string.Equals(tBase, "NA", StringComparison.OrdinalIgnoreCase))
                        maxBytes = tBase.Length;

                    using (var dlg = new TextAmountDialog(encToken, maxBytes))
                    {
                        dlg.Text = BuildProgressTitle(node, tag, index, initialTotal);

                        // pre-dialog highlight (first unresolved occurrence)
                        try { int __e = tpl.IndexOf("]", startIdx); int __len = (__e >= startIdx) ? (__e - startIdx + 1) : 2; HighlightModRange(startIdx, __len); } catch { }
                                                if (dlg.ShowDialog(this) != DialogResult.OK) break;
                        var userText = dlg.ResultText ?? string.Empty;
                        var bytes = encoding.GetBytes(userText);
                        if (bytes.Length > maxBytes)
                        {
                            MessageBox.Show($"Text exceeds the limit of {maxBytes} bytes for {encToken}.",
                                "Too long", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            continue;
                        }
                        { int __end = tpl.IndexOf("]", startIdx); int __len = (__end >= startIdx) ? (__end - startIdx + 1) : 2; HighlightModRange(startIdx, __len); tpl = ReplaceOneOccurrenceAtIndex(tpl, startIdx, userText); txtCodePreview.Text = tpl; }continue;
                    }
                }
                else if (TryParseSpecialAmountTag(tag, out var tTitle, out var tDef, out var tType, out var tEndian))
                {
                    using (var dlg = new SpecialAmountDialog(tTitle, tDef, tType, tEndian))
                    {
                        dlg.Text = BuildProgressTitle(node, tag, index, initialTotal);

                        // pre-dialog highlight (first unresolved occurrence)
                        try { int __e = tpl.IndexOf("]", startIdx); int __len = (__e >= startIdx) ? (__e - startIdx + 1) : 2; HighlightModRange(startIdx, __len); } catch { }
                                                if (dlg.ShowDialog(this) != DialogResult.OK) break;
                        var v0 = dlg.ResultHex ?? "";
                        if (string.IsNullOrEmpty(v0)) break;
                        { int __end = tpl.IndexOf("]", startIdx); int __len = (__end >= startIdx) ? (__end - startIdx + 1) : 2; HighlightModRange(startIdx, __len); tpl = ReplaceOneOccurrenceAtIndex(tpl, startIdx, v0); txtCodePreview.Text = tpl; }continue;
                    }
                }


                                if (modHeaders.ContainsKey(tag))
                                {
                                    using var grid = new ModGridDialog(tag, modHeaders[tag], modRows[tag], BuildProgressTitle(node, tag, index, initialTotal));

                                    // pre-dialog highlight (first unresolved occurrence)
                                    try { int __e = tpl.IndexOf("]", startIdx); int __len = (__e >= startIdx) ? (__e - startIdx + 1) : 2; HighlightModRange(startIdx, __len); } catch { }
                                                                        if (grid.ShowDialog(this) != DialogResult.OK) break;
                                    var v1 = grid.SelectedValue ?? "";
                                    if (string.IsNullOrEmpty(v1)) break;
                                    { int __end = tpl.IndexOf("]", startIdx); int __len = (__end >= startIdx) ? (__end - startIdx + 1) : 2; HighlightModRange(startIdx, __len); tpl = ReplaceOneOccurrenceAtIndex(tpl, startIdx, v1); txtCodePreview.Text = tpl; }
                                    if (!string.IsNullOrWhiteSpace(grid.SelectedDisplay)) AppendAppliedModName(node, grid.SelectedDisplay!);
                                    continue;
                                }
                                else
                                {
                                    using var dd = new SimpleModDialog(tag, modDefinitions.TryGetValue(tag, out var listX) ? listX : new System.Collections.Generic.List<(string,string)>(), BuildProgressTitle(node, tag, index, initialTotal));

                                    // pre-dialog highlight (first unresolved occurrence)
                                    try { int __e = tpl.IndexOf("]", startIdx); int __len = (__e >= startIdx) ? (__e - startIdx + 1) : 2; HighlightModRange(startIdx, __len); } catch { }
                                                                        if (dd.ShowDialog(this) != DialogResult.OK) break;
                                    var v2 = dd.SelectedValue ?? "";
                                    if (string.IsNullOrEmpty(v2)) break;
                                    { int __end = tpl.IndexOf("]", startIdx); int __len = (__end >= startIdx) ? (__end - startIdx + 1) : 2; HighlightModRange(startIdx, __len); tpl = ReplaceOneOccurrenceAtIndex(tpl, startIdx, v2); txtCodePreview.Text = tpl; }
                                    if (!string.IsNullOrWhiteSpace(dd.SelectedName)) AppendAppliedModName(node, dd.SelectedName!);
                                    continue;
                                }
                            }

                    }

                    node.Tag = tpl;
                    txtCodePreview!.Text = tpl;

                    node.Text = GetDisplayName(node);
                }

    }
}
