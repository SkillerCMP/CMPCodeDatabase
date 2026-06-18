// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.LogUI.LayoutHelpers.cs
// Purpose: Collector log/bottom-bar layout helper methods.
// Notes:
//  • Split from CollectorForm.LogUI.Helpers.cs during cleanup pass 7.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        private void EnlargeLogWindowByFactor(double factor)
        {
            if (_logPanel == null || _logPanel.IsDisposed) return;

            const double ratio = 0.40; // 40%

            // Case 1: _logPanel is inside a SplitContainer → adjust splitter
            var split = GetAncestor<SplitContainer>(_logPanel);
            if (split != null && split.Orientation == Orientation.Horizontal)
            {
                // Give ~40% to the panel with the log.
                int target = (int)(split.Height * ratio);
                if (_logPanel.Parent == split.Panel1)
                    split.SplitterDistance = target;                 // top (Panel1) = 40%
                else if (_logPanel.Parent == split.Panel2)
                    split.SplitterDistance = split.Height - target;  // bottom (Panel2) = 60%
                return;
            }

            // Case 2: TableLayoutPanel → change row percent to 40%
            var table = _logPanel.Parent as TableLayoutPanel;
            if (table != null)
            {
                int row = table.GetRow(_logPanel);

                // Make rows percent so we can redistribute
                for (int i = 0; i < table.RowStyles.Count; i++)
                    table.RowStyles[i].SizeType = SizeType.Percent;

                // Log row = 40%, others share remaining 60%
                int others = Math.Max(1, table.RowStyles.Count - 1);
                float otherPct = (float)((1.0 - ratio) * 100.0 / others); // e.g., 60 / others

                for (int i = 0; i < table.RowStyles.Count; i++)
                    table.RowStyles[i].Height = (i == row) ? 40f : otherPct;

                table.PerformLayout();
                return;
            }

            // Case 3: plain panel → set to ~40% of form client height (clamped)
            int desired = (int)(this.ClientSize.Height * ratio);
            desired = Math.Max(80, Math.Min(desired, this.ClientSize.Height - 60));
            _logPanel.Height = desired;
            _logPanel.PerformLayout();
        }

        /// <summary>
        /// Collapses any empty bottom-docked containers that would otherwise reserve blank space.
        /// </summary>
        private void CollapseEmptyBottomDockers()
        {
            try
            {
                foreach (Control c in this.Controls)
                {
                    if (c.Dock == DockStyle.Bottom && (c.Controls.Count == 0 || c.Height < 8))
                    {
                        c.Visible = false;            // don't render
                        c.Dock = DockStyle.Top;       // neutralize docking
                        c.Height = 0;                 // release reserved space
                        c.Margin = Padding.Empty;
                        c.Padding = Padding.Empty;
                    }
                }
            }
            catch { /* best-effort */ }
        }

        /// <summary>
        /// Final pass: ensure the bottom bar shrinks to content and no oversized host keeps extra space.
        /// </summary>
        private void FixCollectorBottomGap()
        {
            try
            {
                var openBtn    = FindByTextDeep<Button>(this, "Open Backups");
                var restoreBtn = FindByTextDeep<Button>(this, "Restore Backup");
                var clearBtn   = FindByTextDeep<Button>(this, "Clear Log");
                Control? bar = openBtn?.Parent ?? restoreBtn?.Parent ?? clearBtn?.Parent;
                if (bar == null) return;

                EnsureBottomBarAutoSize(bar);
                FlattenBottomBarHostChain(bar);
            }
            catch { /* best-effort */ }
        }

private void EnsureBottomBarAutoSize(Control bar)
{
    try
    {
        // Size to content and sit at the bottom
        bar.Margin = Padding.Empty;
        bar.Padding = Padding.Empty;
        bar.AutoSize = true;

        // Some containers (Form/UserControl) have AutoSizeMode; set it only if present
        var asmProp = bar.GetType().GetProperty("AutoSizeMode");
        if (asmProp != null && asmProp.PropertyType == typeof(AutoSizeMode))
        {
            try { asmProp.SetValue(bar, AutoSizeMode.GrowAndShrink, null); } catch { }
        }

        bar.Dock = DockStyle.Bottom;

        // Normalize child margins to avoid accidental extra height
        foreach (Control x in bar.Controls)
            x.Margin = new Padding(6, 3, 6, 3);

        // Clamp to preferred height so the bar doesn't keep extra space
        int prefH = bar.PreferredSize.Height;
        if (prefH > 0) bar.Height = prefH;
    }
    catch { }
}

        private void FlattenBottomBarHostChain(Control bar)
        {
            try
            {
                Control? host = bar.Parent;
                if (host == null) return;

                // Walk up: if a parent is bottom-docked and only hosts this bar, remove/neutralize it.
                while (host != null && host != this && host.Controls.Count <= 1 && host.Dock == DockStyle.Bottom)
                {
                    var parent = host.Parent;

                    host.Controls.Remove(bar);
                    bar.Dock = DockStyle.Bottom;
                    if (parent != null)
                    {
                        parent.Controls.Add(bar);
                        parent.Controls.SetChildIndex(bar, 0);
                    }

                    // Neutralize the empty host
                    host.Visible = false;
                    host.Dock = DockStyle.Top;
                    host.Height = 0;
                    host.Margin = Padding.Empty;
                    host.Padding = Padding.Empty;

                    host = parent;
                }

                // Make sure some central container fills remaining space
                foreach (Control c in this.Controls)
                {
                    if (c == bar) continue;
                    if (c.Dock == DockStyle.Fill || c is SplitContainer || c is TableLayoutPanel)
                    {
                        c.Dock = DockStyle.Fill;
                        break;
                    }
                }
            }
            catch { }
        }

        private static T? GetAncestor<T>(Control c) where T : class
        {
            var p = c?.Parent;
            while (p != null)
            {
                if (p is T t) return t;
                p = p.Parent;
            }
            return null;
        }

        private CheckBox? FindCheckboxNearBottom()
        {
            // A heuristic finder for the "Backup before patch" checkbox
            foreach (Control c in this.Controls)
            {
                var chk = c as CheckBox;
                if (chk != null && (chk.Text?.IndexOf("Backup", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                    return chk;
            }
            // deep search fallback
            return this.Controls.OfType<Control>()
                .SelectMany(GetAllChildren)
                .OfType<CheckBox>()
                .FirstOrDefault(cb => (cb.Text?.IndexOf("Backup", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }

        private static Control[] GetAllChildren(Control c)
        {
            var list = c.Controls.Cast<Control>().ToList();
            foreach (Control child in c.Controls)
                list.AddRange(GetAllChildren(child));
            return list.ToArray();
        }

        private static T? FindByTextDeep<T>(Control root, string text) where T : Control
        {
            if (root is T && string.Equals(root.Text, text, StringComparison.OrdinalIgnoreCase))
                return (T)root;

            foreach (Control child in root.Controls)
            {
                var f = FindByTextDeep<T>(child, text);
                if (f != null) return f;
            }
            return null;
        }

    }
}
