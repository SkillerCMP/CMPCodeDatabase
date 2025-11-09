using System;
using System.Drawing;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private Button _btnExpandToggle;

        /// <summary>
        /// Call once in OnShown: try { WireCodesExpandCollapseToggle(); } catch {}
        /// Adds a button next to "Reload DB" that toggles between "Expand All" and "Collapse All".
        /// Label auto-updates based on whether all group nodes are expanded, including when the codes tree is rebuilt.
        /// Also wires Ctrl+E (expand) and Ctrl+Shift+E (collapse) when the Codes tree has focus.
        /// </summary>
        private void WireCodesExpandCollapseToggle()
        {
            if (treeCodes == null || treeCodes.IsDisposed) return;
            if (_btnExpandToggle != null && !_btnExpandToggle.IsDisposed) return;

            // 1) Find the "Reload DB" button anywhere in the form.
            var reloadBtn = FindDeepButtonByText(this, "reload"); // matches "Reload", "Reload DB", etc.
            if (reloadBtn == null)
            {
                // Fallback: still create a button near the codes tree
                var host = treeCodes.Parent ?? this;
                _btnExpandToggle = MakeToggleButton();
                host.Controls.Add(_btnExpandToggle);
                // place at top-right of the tree area
                _btnExpandToggle.Location = new Point(
                    Math.Max(0, treeCodes.Left + treeCodes.Width - _btnExpandToggle.Width),
                    Math.Max(0, treeCodes.Top - _btnExpandToggle.Height - 4)
                );
            }
            else
            {
                // 2) Create the toggle button and place it immediately to the right of Reload
                _btnExpandToggle = MakeToggleButton();

                var parent = reloadBtn.Parent ?? this;
                parent.Controls.Add(_btnExpandToggle);

                // Try to align on the same row, to the right with a small gap.
                _btnExpandToggle.Location = new Point(reloadBtn.Right + 8, reloadBtn.Top);
                _btnExpandToggle.Anchor = reloadBtn.Anchor;
                _btnExpandToggle.Height = reloadBtn.Height;

                // If a FlowLayoutPanel: ensure ordering directly after Reload.
                var flp = parent as FlowLayoutPanel;
                if (flp != null)
                {
                    int idx = parent.Controls.GetChildIndex(reloadBtn);
                    try { parent.Controls.SetChildIndex(_btnExpandToggle, idx); } catch { }
                }
            }

            // 3) Keep label in sync with tree state across typical lifecycle events
            treeCodes.AfterExpand     -= TreeCodes_AfterAny_UpdateToggle;
            treeCodes.AfterCollapse   -= TreeCodes_AfterAny_UpdateToggle;
            treeCodes.AfterSelect     -= TreeCodes_AfterAny_UpdateToggle;
            treeCodes.VisibleChanged  -= TreeCodes_VisibleChanged_UpdateToggle;
            treeCodes.HandleCreated   -= TreeCodes_HandleCreated_UpdateToggle;

            treeCodes.AfterExpand     += TreeCodes_AfterAny_UpdateToggle;
            treeCodes.AfterCollapse   += TreeCodes_AfterAny_UpdateToggle;
            treeCodes.AfterSelect     += TreeCodes_AfterAny_UpdateToggle;
            treeCodes.VisibleChanged  += TreeCodes_VisibleChanged_UpdateToggle;
            treeCodes.HandleCreated   += TreeCodes_HandleCreated_UpdateToggle;

            // 4) Re-enable hotkeys for expand/collapse when the Codes tree has focus
            try { this.KeyPreview = true; } catch { }
            this.KeyDown -= MainForm_Keys_ExpandCollapse_TOGGLE;
            this.KeyDown += MainForm_Keys_ExpandCollapse_TOGGLE;

            UpdateExpandToggleLabel();
        }

        private Button MakeToggleButton()
        {
            var b = new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                UseVisualStyleBackColor = true,
                Text = "Expand All",
                Padding = new Padding(6, 3, 6, 3),
                Margin = new Padding(6, 0, 0, 0),
                Name = "btnExpandCollapseToggle"
            };
            b.Click += (s, e) =>
            {
                if (AreAllGroupsExpanded())
                    CollapseAllGroups();
                else
                    ExpandAllGroups();
                UpdateExpandToggleLabel();
            };
            return b;
        }

        // Fire on several tree events (expand/collapse/select) to keep label correct
        private void TreeCodes_AfterAny_UpdateToggle(object sender, EventArgs e)
        {
            UpdateExpandToggleLabel();
        }

        // If the control is recreated/shown, refresh the label
        private void TreeCodes_HandleCreated_UpdateToggle(object sender, EventArgs e)
        {
            // Defer a tick to allow caller to finish repopulating
            try { this.BeginInvoke((Action)UpdateExpandToggleLabel); } catch { UpdateExpandToggleLabel(); }
        }
        private void TreeCodes_VisibleChanged_UpdateToggle(object sender, EventArgs e)
        {
            UpdateExpandToggleLabel();
        }

        /// <summary>
        /// Call this from any code path that rebuilds the codes tree (optional):
        /// e.g., after Nodes.Clear / repopulate. Safe to call even if the button isn't present.
        /// </summary>
        private void NotifyCodesTreeRebuilt_REFRESH()
        {
            try { this.BeginInvoke((Action)UpdateExpandToggleLabel); } catch { UpdateExpandToggleLabel(); }
        }

        private void UpdateExpandToggleLabel()
        {
            if (_btnExpandToggle == null || _btnExpandToggle.IsDisposed) return;
            _btnExpandToggle.Text = AreAllGroupsExpanded() ? "Collapse All" : "Expand All";
        }

        // CHANGE #1: Treat null/empty tree as "not expanded" so label shows "Expand All"
        private bool AreAllGroupsExpanded()
        {
            if (treeCodes == null) return false;
            if (treeCodes.Nodes.Count == 0) return false;
            foreach (TreeNode n in treeCodes.Nodes)
                if (!IsGroupSubtreeFullyExpanded(n))
                    return false;
            return true;
        }

        // Define "group" as any node with children; leaves (codes) don't matter for expansion.
        // We also treat nodes with Tag==null as groups (your convention), but we won't strictly require it.
        private bool IsGroupSubtreeFullyExpanded(TreeNode n)
        {
            if (n == null) return true;

            bool isGroup = n.Nodes != null && n.Nodes.Count > 0;
            if (isGroup && !n.IsExpanded) return false;

            foreach (TreeNode c in n.Nodes)
                if (!IsGroupSubtreeFullyExpanded(c))
                    return false;

            return true;
        }

        private void ExpandAllGroups()
        {
            if (treeCodes == null) return;
            treeCodes.BeginUpdate();
            try { treeCodes.ExpandAll(); }
            finally { treeCodes.EndUpdate(); }
        }

        private void CollapseAllGroups()
        {
            if (treeCodes == null) return;
            treeCodes.BeginUpdate();
            try
            {
                foreach (TreeNode n in treeCodes.Nodes)
                    n.Collapse(false);
            }
            finally { treeCodes.EndUpdate(); }
        }

        // CHANGE #2: Bring back hotkeys (Ctrl+E expand, Ctrl+Shift+E collapse) when Codes tree has focus
        private void MainForm_Keys_ExpandCollapse_TOGGLE(object sender, KeyEventArgs e)
        {
            if (!(treeCodes?.Focused == true || (treeCodes?.ContainsFocus ?? false))) return;

            if (e.Control && !e.Shift && e.KeyCode == Keys.E)
            {
                ExpandAllGroups();
                e.Handled = e.SuppressKeyPress = true;
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.E)
            {
                CollapseAllGroups();
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        // --- utility: find a button by Text (contains) in the whole control tree ---
        private Button FindDeepButtonByText(Control root, string containsLower)
        {
            if (root == null) return null;
            containsLower = (containsLower ?? "").ToLowerInvariant();
            foreach (Control c in root.Controls)
            {
                var b = c as Button;
                if (b != null)
                {
                    var txt = (b.Text ?? "").ToLowerInvariant();
                    if (txt.Contains(containsLower))
                        return b;
                }
                var deep = FindDeepButtonByText(c, containsLower);
                if (deep != null) return deep;
            }
            return null;
        }
    }
}
