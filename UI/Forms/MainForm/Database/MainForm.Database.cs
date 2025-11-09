// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.cs
// Purpose: Database discovery, selector, and tree building.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────


using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void LoadDatabaseSelector()
                {
                    string databasesRoot = GetDatabasesRootPath();
                    dbSelector.Items.Clear();
                    if (!Directory.Exists(databasesRoot)) return;

                    foreach (var folder in Directory.GetDirectories(databasesRoot))
                        dbSelector.Items.Add(Path.GetFileName(folder));

                    if (dbSelector.Items.Count > 0)
                        dbSelector.SelectedIndex = 0;
                }

        private void DbSelector_SelectedIndexChanged(object? sender, EventArgs e)
                {
                    if (dbSelector.SelectedItem == null) return;
                    var root = GetDatabasesRootPath();
                    codeDirectory = Path.Combine(root, dbSelector.SelectedItem?.ToString() ?? string.Empty);
                    LoadGames();
                }

        private void LoadGames()
                {
                    treeGames.Nodes.Clear();
                    if (string.IsNullOrEmpty(codeDirectory) || !Directory.Exists(codeDirectory)) return;

                    foreach (var folder in Directory.GetDirectories(codeDirectory))
                        treeGames.Nodes.Add(new TreeNode(Path.GetFileName(folder)) { Tag = folder });

                    foreach (TreeNode n in treeGames.Nodes) n.Collapse();

                    treeCodes.BeginUpdate();
            treeCodes.Nodes.Clear();
                    txtCodePreview.Clear();
					treeCodes.EndUpdate();
TreeViewExtent.UpdateHorizontalExtent(treeCodes);
try { NotifyCodesTreeRebuilt_REFRESH(); } catch { }
                }

        private void TreeGames_AfterSelect(object? sender, TreeViewEventArgs e)
                {
                    if (e.Node?.Tag == null) return;
                    LoadCodes(e.Node.Tag.ToString()!);
                }

        private void LoadCodes(string folder)
                {
                    treeCodes.BeginUpdate();
					treeCodes.Nodes.Clear();
                    originalCodeTemplates.Clear();
                    originalNodeNames.Clear();
                    nodeNotes.Clear();
                    nodeHasMod.Clear();
                    appliedModNames.Clear();
                    modDefinitions.Clear();
                    modHeaders.Clear();
                    modRows.Clear();

                    BuildTreeFromFolder(folder, null);

            // Compose right root caption from ^3 and ^2 of a representative file (if any)
            try
            {
                string folderName = Path.GetFileName(folder);
                string? firstTxt = Directory.EnumerateFiles(folder, "*.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (treeCodes.Nodes.Count > 0)
                {
                    string rootCaptionForFolder = folderName;
                    if (firstTxt != null)
                    {
                        var header = TryReadHeader(firstTxt);
                        var _name = header.Name ?? folderName;
                        rootCaptionForFolder = !string.IsNullOrEmpty(header.GameId) ? $"{_name} - {header.GameId}" : _name;
                    }
                    treeCodes.Nodes[0].Text = rootCaptionForFolder;
                    originalNodeNames[treeCodes.Nodes[0]] = rootCaptionForFolder;
                }
            }
            catch { }

                    ApplyBoldStyling(treeCodes.Nodes);

                    foreach (TreeNode n in treeCodes.Nodes) n.Collapse();
                    txtCodePreview.Clear();
					treeCodes.EndUpdate();
    TreeViewExtent.UpdateHorizontalExtent(treeCodes);
	try { NotifyCodesTreeRebuilt_REFRESH(); } catch { }
                }

        private void BuildTreeFromFolder(string currentFolder, TreeNode? parentNode)
                {
                    var __folderName = Path.GetFileName(currentFolder);
                    var folderNode = new TreeNode(__folderName);
                    originalNodeNames[folderNode] = __folderName;

                    foreach (var sub in Directory.EnumerateDirectories(currentFolder))
                        BuildTreeFromFolder(sub, folderNode);

                    var txtFiles = Directory.EnumerateFiles(currentFolder, "*.txt").ToArray();
                    if (txtFiles.Length > 0)
                        ParseCodeFilesInFolder(txtFiles, folderNode);

                    if (folderNode.Nodes.Count > 0 || txtFiles.Length > 0)
                    {
                        if (parentNode == null) treeCodes.Nodes.Add(folderNode);
                        else parentNode.Nodes.Add(folderNode);
                    }
                }

        private void ParseCodeFilesInFolder(string[] txtFiles, TreeNode parentNode)
                {
                    foreach (var file in txtFiles)
            {
                        bool __inModsSection = false;

                TreeNode? currentFileGroup = null;
TreeNode? currentGroup = null;
                        TreeNode? currentCodeNode = null;
                        string? currentModTag = null;
                        List<(string Value, string Name)>? currentModList = null;
                        bool headerCollected = false;

                        foreach (var raw in File.ReadLines(file))
                        {
                            if (string.IsNullOrWhiteSpace(raw)) continue;
                            if (raw.StartsWith("^1") || raw.StartsWith("^2")) continue;
                            
                            // --- MODS guard: enter on exact '^6 = MODS:' and leave on any other caret header ---
                            var __trim = (raw ?? string.Empty).Trim();
                            if (__trim.StartsWith("^6 = MODS:", StringComparison.Ordinal)) __inModsSection = true;
                            else if (__trim.StartsWith("^", StringComparison.Ordinal) && !__trim.StartsWith("^6 = MODS:", StringComparison.Ordinal)) __inModsSection = false;

                            string line = raw.TrimEnd();

                            // --- Game Note: '{}' on its own line (outside MODS; and not a '+' code line) ---
                            if (!__inModsSection)
                            {
                                var __head = line.TrimStart();
                                if (__head.StartsWith("{", StringComparison.Ordinal) && __head.EndsWith("}", StringComparison.Ordinal) && !__head.StartsWith("+", StringComparison.Ordinal))
                                {
                                    string inner = __head.Substring(1, __head.Length - 2).Trim();
                                    string html = UnescapeNote(inner);
                                    TreeNode target = currentFileGroup ?? parentNode;
                                    if (target != null)
                                    {
                                        if (nodeNotes.TryGetValue(target, out var ex) && !string.IsNullOrEmpty(ex))
                                            nodeNotes[target] = ex + "<hr/>" + html;
                                        else
                                            nodeNotes[target] = html;
                                        target.Text = GetDisplayName(target);
                                    }
                                    continue;
                                }
                            }


                            // Handle ^4 = FILE: <caption> as a top-level group within this game
                            var trimmed = line.Trim();
                            var mFile = System.Text.RegularExpressions.Regex.Match(trimmed, @"^\^?\s*4\s*=\s*FILE\s*:\s*(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (mFile.Success)
                            {
                                var caption = mFile.Groups[1].Value.Trim();
                                if (caption.Length == 0) caption = "FILE";
                                currentFileGroup = new TreeNode(caption);
                                parentNode.Nodes.Add(currentFileGroup);
                                originalNodeNames[currentFileGroup] = caption;
                                currentGroup = null;
                                continue; // do not treat as content line
                            }



                            if (line.StartsWith("+"))
                            {
                                string rawName = line.Substring(1).Trim();
                                string baseName = rawName;
                                string note = null;
                                string popup = null;
                            
                                // Extract double-brace popup first: +Title{{  }}
                                int d1 = rawName.IndexOf("{{");
                                int d2 = (d1 >= 0) ? rawName.IndexOf("}}", d1 + 2) : -1;
                                if (d1 >= 0 && d2 > d1)
                                {
                                    popup = UnescapeNote(rawName.Substring(d1 + 2, d2 - d1 - 2).Trim());
                                    rawName = (rawName.Substring(0, d1) + rawName.Substring(d2 + 2)).Trim();
                                }
                            
                                // Now parse single-brace inline note: +Title{  }
                                int s = rawName.IndexOf('{');
                                int e = (s >= 0) ? rawName.IndexOf('}', s + 1) : -1;
                                if (s >= 0 && e > s)
                                {
                                    note = UnescapeNote(rawName.Substring(s + 1, e - s - 1).Trim());
                                    baseName = rawName.Substring(0, s).Trim();
                                }
                                else
                                {
                                    baseName = rawName.Trim();
                                }
                            
                                currentCodeNode = new TreeNode(baseName);
                                (currentGroup ?? currentFileGroup ?? parentNode).Nodes.Add(currentCodeNode);
                            
                                originalCodeTemplates[currentCodeNode] = string.Empty;
                                currentCodeNode.Tag = string.Empty;
                                originalNodeNames[currentCodeNode] = baseName;
                            
                                if (!string.IsNullOrEmpty(note)) { nodeNotes[currentCodeNode] = note; currentCodeNode.Text = GetDisplayName(currentCodeNode); }
                                if (!string.IsNullOrEmpty(popup))
                                {
                                    if (!nodePopupNotes.ContainsKey(currentCodeNode)) nodePopupNotes[currentCodeNode] = new List<string>();
                                    nodePopupNotes[currentCodeNode].Add(popup);
                                    currentCodeNode.Text = GetDisplayName(currentCodeNode);
                                }
                            }

                            else if (line.StartsWith("!"))
                            {
                                if (line.Trim() == "!!")
                                {
                                    currentGroup = currentGroup?.Parent;
                                }
                                else
{
    string rawGroup = line.Substring(1).Trim();
    string groupName = rawGroup;
    string? groupNote = null;
    List<string>? popupNotes = null;

    // Only treat as group-note if pattern contains ':{' (per spec: !..:{})
    if (rawGroup.IndexOf(":{", StringComparison.Ordinal) >= 0)
    {
        // FIRST: look for popup-style {{ ... }}
        int d1 = rawGroup.IndexOf("{{", StringComparison.Ordinal);
        int d2 = (d1 >= 0) ? rawGroup.IndexOf("}}", d1 + 2, StringComparison.Ordinal) : -1;
        if (d1 >= 0 && d2 > d1)
        {
            // popup note (auto-open)
            string popup = UnescapeNote(rawGroup.Substring(d1 + 2, d2 - (d1 + 2)).Trim());
            popupNotes = new List<string> { popup };
            // strip the :{{...}} part out of the name
            groupName = rawGroup.Substring(0, d1).Trim();
        }
        else
        {
            // OLD single-brace style { ... } -> stays as normal note
            int gs = rawGroup.IndexOf('{');
            int ge = rawGroup.IndexOf('}');
            if (gs >= 0 && ge > gs)
            {
                groupNote = UnescapeNote(rawGroup.Substring(gs + 1, ge - gs - 1).Trim());
                groupName = rawGroup.Substring(0, gs).Trim();
            }
        }
    }

    TreeNode newGroup = new TreeNode(groupName);
    (currentGroup ?? currentFileGroup ?? parentNode).Nodes.Add(newGroup);
    originalNodeNames[newGroup] = groupName;

    if (popupNotes != null)
    {
        if (!nodePopupNotes.ContainsKey(newGroup))
            nodePopupNotes[newGroup] = new List<string>();
        // add all popup notes we found (usually 1)
        foreach (var p in popupNotes)
            nodePopupNotes[newGroup].Add(p);
        newGroup.Text = GetDisplayName(newGroup);
    }
    else if (!string.IsNullOrEmpty(groupNote))
    {
        // single-brace { ... } stays as normal note
        nodeNotes[newGroup] = groupNote;
        newGroup.Text = GetDisplayName(newGroup);
    }

    currentGroup = newGroup;
}
                            }
                            else if (line.StartsWith("["))
                            {
                                if (line.StartsWith("[/"))
                                {
                                    if (!string.IsNullOrEmpty(currentModTag) && currentModList != null)
                                        modDefinitions[currentModTag] = new List<(string, string)>(currentModList);
                                    currentModTag = null;
                                    currentModList = null;
                                    headerCollected = false;
                                }
                                else
                                {
                                    currentModTag = line.Trim('[', ']');
                                    currentModList = new List<(string, string)>();
                                    headerCollected = false;
                                }
                            }
                            
                            else if (line.StartsWith("$"))
{

// v1.01 behavior: store code WITHOUT leading '$' so downstream SW formatter can reflow
if (currentCodeNode == null) continue;

string codeLine = line.Substring(1).TrimEnd();

// Keep original template (used for placeholder gating) without '$'
if (originalCodeTemplates.TryGetValue(currentCodeNode, out var tpl) && !string.IsNullOrEmpty(tpl))
    originalCodeTemplates[currentCodeNode] = tpl + Environment.NewLine + codeLine;
else
    originalCodeTemplates[currentCodeNode] = codeLine;

// Keep the working code on the node Tag without '$' (what Add→Collector uses)
var working = currentCodeNode.Tag as string ?? string.Empty;
currentCodeNode.Tag = string.IsNullOrEmpty(working)
    ? codeLine
    : working + Environment.NewLine + codeLine;
}
else if (currentModTag != null)
                            {
                                // Inside a MOD block
                                if (!headerCollected)
                                {
                                    // Detect headers line if it contains '>' but not '='
                                    if (line.Contains('>') && !line.Contains('='))
                                    {
                                        var headers = line.Split('>').Select(h => h.Trim()).Where(h => h.Length > 0).ToList();
                                        if (headers.Count >= 2)
                                        {
                                            modHeaders[currentModTag] = headers;
                                            modRows[currentModTag] = new List<string[]>();
                                            headerCollected = true;
                                            continue;
                                        }
                                    }
                                    // Otherwise fall through as data for simple pairs
                                }

                                // Data rows
                                if (modHeaders.ContainsKey(currentModTag))
                                {
                                    // headered table
                                    var headers = modHeaders[currentModTag];
                                    string[] parts;
                                    if (line.Contains('\t')) parts = line.Split('\t');
                                    else if (line.Contains('>')) parts = line.Split('>');
                                    else if (line.Contains('=')) parts = line.Split('='); // allow VALUE=NAME when exactly 2 headers
                                    else parts = new[] { line };

                                    parts = parts.Select(p => p.Trim()).ToArray();

// Accept rows that have at least VALUE + NAME; INFO is optional.
// (ModGridDialog already pads missing cells to empty string.)
if (parts.Length >= 2)
{
    modRows[currentModTag].Add(parts);
    currentModList!.Add((parts[0], parts[1]));
}

                                }
                                else
                                {
                                    // simple pair lines: "VAL=NAME" or "VAL<TAB>NAME"
                                    if (line.Contains('='))
                                    {
                                        var parts = line.Split(new[] { '=' }, 2);
                                        if (parts.Length == 2)
                                            currentModList!.Add((parts[0].Trim(), parts[1].Trim()));
                                    }
                                    else if (line.Contains('\t'))
                                    {
                                        var parts = line.Split('\t');
                                        if (parts.Length >= 2)
                                            currentModList!.Add((parts[0].Trim(), parts[1].Trim()));
                                    }
                                }
                            }
                        }
                        // Post-pass: recompute -M- for every code node under this file's root (robust)
                        {
                            var __available = new HashSet<string>(modDefinitions.Keys, StringComparer.OrdinalIgnoreCase);

                            // Depth-first traversal over this file's tree (parentNode is the root group for this file)
                            var __stack = new Stack<TreeNode>();
                            __stack.Push(parentNode);
                            while (__stack.Count > 0)
                            {
                                var t = __stack.Pop();
                                foreach (TreeNode ch in t.Nodes) __stack.Push(ch);

                                if (originalCodeTemplates.TryGetValue(t, out var codeTpl))
                                {
                                    bool should = ShouldShowModBadgeSimple(codeTpl) || ShouldShowModBadge(codeTpl, __available);
                                    if (should) nodeHasMod.Add(t); else nodeHasMod.Remove(t);
                                    t.Text = GetDisplayName(t);
                                }
                            }
                        }
                        // end postpass marker

                    }
                }

    
        private string GetDatabasesRootPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                System.IO.Path.Combine(baseDir, "Database"),
                System.IO.Path.Combine(baseDir, "File", "Database"),
                System.IO.Path.Combine(baseDir, "Files", "Database")
            };
            foreach (var p in candidates)
                if (System.IO.Directory.Exists(p)) return p;
            return candidates[0];
        }

}
}