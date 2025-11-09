using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Returns a friendly group path for the given code node, e.g. "Masters / Attack Mods".
        /// - Walks up through group nodes (Tag == null), skipping the top game root.
        /// - Trims trailing ":" and common badges (e.g., "-M- ").
        /// </summary>
        private string GetGroupPath(TreeNode codeNode)
        {
            if (codeNode == null) return string.Empty;
            var parts = new List<string>();

            TreeNode cur = codeNode.Parent;
            while (cur != null)
            {
                // Stop at the top game root
                if (cur.Parent == null) break;

                // Only treat structural/group nodes (no code payload)
                if (cur.Tag == null)
                {
                    var label = cur.Text ?? string.Empty;

                    // Remove badges and trailing ":"
                    if (label.StartsWith("-NM- ")) label = label.Substring(5);
                    else if (label.StartsWith("-M- ")) label = label.Substring(4);
                    else if (label.StartsWith("-N- ")) label = label.Substring(4);
                    if (label.StartsWith("[-NM-] ")) label = label.Substring(7);
                    else if (label.StartsWith("[-M-] ")) label = label.Substring(6);
                    else if (label.StartsWith("[-N-] ")) label = label.Substring(6);
                    if (label.EndsWith(":")) label = label.Substring(0, label.Length - 1);

                    label = label.Trim();
                    if (!string.IsNullOrEmpty(label))
                        parts.Insert(0, label);
                }
                cur = cur.Parent;
            }

            return string.Join(" / ", parts);
        }
    }
}
