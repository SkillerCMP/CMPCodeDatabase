using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>
        /// Returns true if 'codeText' still has unresolved placeholders for declared MOD names.
        /// Angle-safe (base NAME only) and Amount special-cased.
        /// </summary>
        private bool IsUnresolvedForCollector(string codeText)
        {
            var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (modDefinitions != null)
            {
                foreach (var k in modDefinitions.Keys)
                    declared.Add(k);
            }
            return HasUnresolvedPlaceholders_ModsAware(codeText ?? string.Empty, declared);
        }

        /// <summary>
        /// Prefer the actual code being sent; if that's empty, try node.Tag; finally the original template.
        /// 'node' may be null in contexts that don't have a TreeNode.
        /// </summary>
        private bool BlockIfUnresolvedForCollector(TreeNode node, string codeOverride = null)
        {
            string codeText = codeOverride ?? string.Empty;

            if (string.IsNullOrWhiteSpace(codeText) && node != null)
            {
                codeText = node.Tag as string ?? string.Empty;

                if (string.IsNullOrWhiteSpace(codeText) && originalCodeTemplates != null)
                {
                    string tpl;
                    if (originalCodeTemplates.TryGetValue(node, out tpl) && tpl != null)
                        codeText = tpl;
                }
            }

            if (IsUnresolvedForCollector(codeText))
            {
                MessageBox.Show(
                    this,
                    "This code still has unresolved placeholders and cannot be added to the Collector.\n" +
                    "Fill the [NAME] / [NAME<...>] values (or Amount) first.",
                    "Unresolved placeholders",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return true;
            }
            return false;
        }
    }
}
