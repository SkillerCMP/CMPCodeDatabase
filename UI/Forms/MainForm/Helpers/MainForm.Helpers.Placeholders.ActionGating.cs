using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>
        /// Blocks sending to Collector if any unresolved placeholders remain.
        /// Uses DECLARED MOD names (from ^6=MODS) and the angle-safe checker.
        /// </summary>
        private bool BlockIfUnresolvedForCollector(TreeNode node)
        {
            if (node == null) return false;

            // Prefer the original template (what gating should look at). Fallback to Tag.
            var codeTpl = string.Empty;
            if (originalCodeTemplates != null && originalCodeTemplates.TryGetValue(node, out var tpl) && tpl != null)
                codeTpl = tpl;
            else
                codeTpl = node.Tag as string ?? string.Empty;

            // Declared MOD names (e.g., [Item]…[/Item], [Size]…[/Size], …)
            var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (modDefinitions != null)
            {
                foreach (var k in modDefinitions.Keys)
                    declared.Add(k);
            }

            // Angle-safe: [NAME] and [NAME<...>] both count by base NAME; [Amount:...:...:...] always counts.
            var unresolved = HasUnresolvedPlaceholders_ModsAware(codeTpl, declared);
            if (unresolved)
            {
                MessageBox.Show(
                    this,
                    "This code still has unresolved placeholders and cannot be added to the Collector.\n" +
                    "Fill the [NAME] / [NAME<...>] values (or Amount) first.",
                    "Unresolved placeholders",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return true; // block
            }

            return false; // allow
        }
    }
}
