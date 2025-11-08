using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private bool _suppressCheckCascade;

        /// <summary>
        /// Call once during startup (e.g., in OnShown): try { WireTreeCheckCascade(); } catch {}
        /// Enables group checkbox behavior: checking any group node applies the same
        /// checked state to all descendant nodes (subgroups and codes). Checking the
        /// root will therefore check everything.
        /// </summary>
        private void WireTreeCheckCascade()
        {
            if (treeCodes == null || treeCodes.IsDisposed) return;

            // Avoid double-wiring
            try { treeCodes.AfterCheck -= TreeCodes_AfterCheck_CASCADE; } catch { }
            treeCodes.AfterCheck += TreeCodes_AfterCheck_CASCADE;
        }

        private void TreeCodes_AfterCheck_CASCADE(object sender, TreeViewEventArgs e)
        {
            if (_suppressCheckCascade) return;

            try
            {
                _suppressCheckCascade = true;
                // Apply the same checked state to all descendants
                SetCheckDeep(e.Node, e.Node.Checked);
            }
            finally
            {
                _suppressCheckCascade = false;
            }
        }

        private void SetCheckDeep(TreeNode node, bool isChecked)
        {
            if (node == null) return;
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Checked != isChecked)
                    child.Checked = isChecked;
                SetCheckDeep(child, isChecked);
            }
        }
    }
}
