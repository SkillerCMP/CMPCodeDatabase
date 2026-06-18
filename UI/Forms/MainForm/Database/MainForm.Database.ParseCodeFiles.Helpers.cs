// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.ParseCodeFiles.Helpers.cs
// Purpose: Helper methods used by ParseCodeFilesInFolder.
// Notes:
//  • Cleanup pass split only; parser flow remains in MainForm.Database.cs.
//  • Keep helper ordering and branch semantics aligned with the original parser.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private static readonly System.Text.RegularExpressions.Regex CodeFileHeaderRegex = new(
            @"^\^?\s*4\s*=\s*FILE\s*:\s*(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private bool TryHandleGameNoteLine(string line, bool inModsSection, TreeNode? currentFileGroup, TreeNode parentNode)
        {
            // --- Game Note: '{}' on its own line (outside MODS; and not a '+' code line) ---
            if (!inModsSection)
            {
                var head = line.TrimStart();
                if (head.StartsWith("{", StringComparison.Ordinal) && head.EndsWith("}", StringComparison.Ordinal) && !head.StartsWith("+", StringComparison.Ordinal))
                {
                    string inner = head.Substring(1, head.Length - 2).Trim();
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
                    return true;
                }
            }

            return false;
        }

        private bool TryHandleCodeFileHeaderLine(string line, TreeNode parentNode, ref TreeNode? currentFileGroup, ref TreeNode? currentGroup)
        {
            // Handle ^4 = FILE: <caption> as a top-level group within this game
            var trimmed = line.Trim();
            var mFile = CodeFileHeaderRegex.Match(trimmed);
            if (!mFile.Success) return false;

            var caption = mFile.Groups[1].Value.Trim();
            if (caption.Length == 0) caption = "FILE";
            currentFileGroup = new TreeNode(caption);
            parentNode.Nodes.Add(currentFileGroup);
            originalNodeNames[currentFileGroup] = caption;
            currentGroup = null;
            return true; // do not treat as content line
        }

        private TreeNode CreateCodeNodeFromPlusLine(string line, TreeNode? currentGroup, TreeNode? currentFileGroup, TreeNode parentNode, string? inheritedGroupHeaderBlock = null)
        {
            string rawName = line.Substring(1).Trim();
            string baseName = rawName;
            string? note = null;
            string? popup = null;

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

            var currentCodeNode = new TreeNode(baseName);
            (currentGroup ?? currentFileGroup ?? parentNode).Nodes.Add(currentCodeNode);

            var inheritedHeaders = (inheritedGroupHeaderBlock ?? string.Empty).Trim();
            originalCodeTemplates[currentCodeNode] = inheritedHeaders;
            currentCodeNode.Tag = inheritedHeaders;
            originalNodeNames[currentCodeNode] = baseName;

            if (!string.IsNullOrEmpty(note))
            {
                nodeNotes[currentCodeNode] = note;
                currentCodeNode.Text = GetDisplayName(currentCodeNode);
            }

            if (!string.IsNullOrEmpty(popup))
            {
                if (!nodePopupNotes.ContainsKey(currentCodeNode)) nodePopupNotes[currentCodeNode] = new List<string>();
                nodePopupNotes[currentCodeNode].Add(popup);
                currentCodeNode.Text = GetDisplayName(currentCodeNode);
            }

            return currentCodeNode;
        }

        private void HandleGroupLine(string line, TreeNode? currentFileGroup, TreeNode parentNode, ref TreeNode? currentGroup)
        {
            if (line.Trim() == "!!")
            {
                currentGroup = currentGroup?.Parent;
                return;
            }

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

        private void AddCodeLineToNode(string line, TreeNode? currentCodeNode)
        {
            // v1.01 behavior: store code WITHOUT leading '$' so downstream SW formatter can reflow
            if (currentCodeNode == null) return;

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

        private void RefreshModBadgesForTree(TreeNode parentNode)
        {
            // Post-pass: recompute -M- for every code node under this file's root (robust)
            var available = new HashSet<string>(modDefinitions.Keys, StringComparer.OrdinalIgnoreCase);

            // Depth-first traversal over this file's tree (parentNode is the root group for this file)
            var stack = new Stack<TreeNode>();
            stack.Push(parentNode);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                foreach (TreeNode ch in t.Nodes) stack.Push(ch);

                if (originalCodeTemplates.TryGetValue(t, out var codeTpl))
                {
                    bool should = ShouldShowModBadgeSimple(codeTpl) || ShouldShowModBadge(codeTpl, available);
                    if (should) nodeHasMod.Add(t); else nodeHasMod.Remove(t);
                    t.Text = GetDisplayName(t);
                }
            }
        }
    }
}
