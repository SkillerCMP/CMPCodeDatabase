// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.GroupHeaders.cs
// Purpose: Group-scoped inherited header support for parsed CMP code blocks.
// Notes:
//  • Targeted feature: '$' lines placed directly under a !Group before +Code
//    are inherited by codes inside that group.
//  • Nested groups inherit parent header lines plus their own group header lines.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private static bool TryAddGroupHeaderLine(
            string line,
            TreeNode? currentCodeNode,
            TreeNode? currentGroup,
            TreeNode? currentFileGroup,
            TreeNode parentNode,
            Dictionary<TreeNode, List<string>> groupHeaderLines,
            HashSet<TreeNode> groupsWithParsedCodes)
        {
            // A '$' line is a group header only when it appears inside an active
            // parser group before a '+Code Name'. Once a code node is active,
            // '$' lines remain normal code lines for that code.
            if (currentCodeNode != null || !IsActiveParserGroup(currentGroup, currentFileGroup, parentNode))
                return false;

            if (groupsWithParsedCodes.Contains(currentGroup!))
                return false;

            var headerLine = NormalizeGroupHeaderCodeLine(line);
            if (string.IsNullOrWhiteSpace(headerLine))
                return true;

            if (!groupHeaderLines.TryGetValue(currentGroup!, out var lines))
            {
                lines = new List<string>();
                groupHeaderLines[currentGroup!] = lines;
            }

            lines.Add(headerLine);
            return true;
        }

        private static bool IsActiveParserGroup(TreeNode? currentGroup, TreeNode? currentFileGroup, TreeNode parentNode)
        {
            if (currentGroup == null)
                return false;

            if (ReferenceEquals(currentGroup, currentFileGroup) || ReferenceEquals(currentGroup, parentNode))
                return false;

            return true;
        }


        private static void MarkGroupScopesWithParsedCode(
            TreeNode? currentGroup,
            TreeNode? currentFileGroup,
            TreeNode parentNode,
            HashSet<TreeNode> groupsWithParsedCodes)
        {
            for (var node = currentGroup; node != null; node = node.Parent)
            {
                if (!IsActiveParserGroup(node, currentFileGroup, parentNode))
                    continue;

                groupsWithParsedCodes.Add(node);
            }
        }

        private static string BuildInheritedGroupHeaderBlock(
            TreeNode? currentGroup,
            Dictionary<TreeNode, List<string>> groupHeaderLines)
        {
            if (currentGroup == null || groupHeaderLines.Count == 0)
                return string.Empty;

            var inherited = new List<string>();
            var ancestry = new Stack<TreeNode>();

            for (var node = currentGroup; node != null; node = node.Parent)
                ancestry.Push(node);

            while (ancestry.Count > 0)
            {
                var node = ancestry.Pop();
                if (!groupHeaderLines.TryGetValue(node, out var lines))
                    continue;

                inherited.AddRange(lines.Where(l => !string.IsNullOrWhiteSpace(l)));
            }

            return string.Join(Environment.NewLine, inherited).Trim();
        }

        private static string NormalizeGroupHeaderCodeLine(string? line)
        {
            var text = (line ?? string.Empty).Trim();

            // Match normal CMP code storage: code text is kept internally without '$'.
            if (text.StartsWith("$", StringComparison.Ordinal))
                text = text.Substring(1).TrimStart();

            return text.TrimEnd();
        }
    }
}
