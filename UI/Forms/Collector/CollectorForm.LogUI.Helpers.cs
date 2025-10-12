// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.LogUI.Helpers.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        /// <summary>
        /// One-time UI tweaks executed on load to stabilize the layout and layout order.
        /// </summary>
        private void TryInitPatchUI()
        {
            try { EnsurePatcherStatusUi(); } catch { }
            try { UpdatePatcherStatus(PatchProgramExePath); } catch { }

            MoveRunAndPatcherBarsIntoLogPanel();
            RenameRunButtons();
            MoveCopyButtonsBesideSelectAll();
            try { TweakBackupsBar(); } catch { }
            try { WireBackupBarLeftLayout(); } catch { }

            // Collapse any empty bottom-docked placeholders and zero padding/margins
            try { CollapseEmptyBottomDockers(); } catch { }
            try { this.Padding = Padding.Empty; this.Margin = Padding.Empty; if (_logPanel != null) { _logPanel.Padding = Padding.Empty; _logPanel.Margin = Padding.Empty; } } catch { }

            // Final anti-gap pass (auto-size and flatten bottom host chain)
            try { FixCollectorBottomGap(); } catch { }

            // Make the log window taller (best-effort, after initial layout completes).
            try { BeginInvoke(new Action(() => EnlargeLogWindowByFactor(1.0))); } catch { }
        }

        private void RenameRunButtons()
        {
            var run = FindByTextDeep<Button>(this, "Run Patch (Checked)") ??
                      FindByTextDeep<Button>(this, "Run Patch");
            if (run != null) run.Text = "Run Patch";

            var prev = FindByTextDeep<Button>(this, "Preview (Checked)") ??
                       FindByTextDeep<Button>(this, "Preview");
            if (prev != null) prev.Text = "Preview";
        }

        private void MoveRunAndPatcherBarsIntoLogPanel()
        {
            if (_logPanel == null || _logPanel.IsDisposed) return;

            // Find any "Run Patch" button and treat its parent as the bar
            var runBtn = FindByTextDeep<Button>(this, "Run Patch") ??
                         FindByTextDeep<Button>(this, "Run Patch (Checked)");
            var runBar = runBtn?.Parent as Control;
            if (runBar != null && runBar.Parent != _logPanel)
            {
                try
                {
                    var oldParent = runBar.Parent;      // capture BEFORE reparent
                    runBar.Dock = DockStyle.Top;
                    runBar.Parent?.Controls.Remove(runBar);
                    _logPanel.Controls.Add(runBar);
                    runBar.BringToFront();
                    TweakBackupsBar();
                    WireBackupBarLeftLayout();

                    // Collapse the old parent if it became an empty bottom-docked placeholder
                    if (oldParent != null && oldParent.Controls.Count == 0 && oldParent.Dock == DockStyle.Bottom)
                    {
                        oldParent.Visible = false;
                        oldParent.Dock = DockStyle.Top;
                        oldParent.Height = 0;
                        oldParent.Margin = Padding.Empty;
                        oldParent.Padding = Padding.Empty;
                    }
                }
                catch { }
            }

            // Ensure patcher bar lives just below the run bar
            try
            {
                var patcher = this.Controls.Find("patcherBar", true);
                if (patcher != null && patcher.Length > 0)
                {
                    var p = patcher[0];
                    if (p.Parent != _logPanel)
                    {
                        var oldParentP = p.Parent;      // capture BEFORE reparent
                        p.Parent?.Controls.Remove(p);
                        p.Dock = DockStyle.Top;
                        _logPanel.Controls.Add(p);

                        // Collapse the old parent if it became an empty bottom-docked placeholder
                        if (oldParentP != null && oldParentP.Controls.Count == 0 && oldParentP.Dock == DockStyle.Bottom)
                        {
                            oldParentP.Visible = false;
                            oldParentP.Dock = DockStyle.Top;
                            oldParentP.Height = 0;
                            oldParentP.Margin = Padding.Empty;
                            oldParentP.Padding = Padding.Empty;
                        }
                    }

                    if (runBar != null)
                    {
                        _logPanel.Controls.SetChildIndex(p, 1);
                        _logPanel.Controls.SetChildIndex(runBar, 0);
                    }
                    p.BringToFront();
                }
            }
            catch { }

            // Final sweep, in case any other bottom placeholders remain
            try { CollapseEmptyBottomDockers(); } catch { }
        }

        private void MoveCopyButtonsBesideSelectAll()
        {
            var opsPanel = FindByTextDeep<Button>(this, "Select All")?.Parent as FlowLayoutPanel;
            if (opsPanel == null) return;

            var invert = opsPanel.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Invert");
            if (invert != null) invert.Visible = false;

            foreach (var cap in new[] { "Copy Checked", "Copy All", "Clear" })
            {
                var btn = FindByTextDeep<Button>(this, cap);
                if (btn == null || btn.Parent == null || ReferenceEquals(btn.Parent, opsPanel)) continue;
                btn.Parent.Controls.Remove(btn);
                opsPanel.Controls.Add(btn);
            }
        }

        private void TweakBackupsBar()
        {
            // Set checkbox checked, and place Open/Restore before the checkbox (initial pass)
            var openBtn = FindByTextDeep<Button>(this, "Open Backups");
            var restoreBtn = FindByTextDeep<Button>(this, "Restore Backup");
            var chk = FindCheckboxNearBottom();

            if (chk != null) chk.Checked = true;

            if (openBtn != null && restoreBtn != null && chk != null)
            {
                var parent = openBtn.Parent;
                if (parent != null)
                {
                    parent.RightToLeft = RightToLeft.No;
                    openBtn.Anchor   = AnchorStyles.Top | AnchorStyles.Left;
                    restoreBtn.Anchor= AnchorStyles.Top | AnchorStyles.Left;
                    chk.Anchor       = AnchorStyles.Top | AnchorStyles.Left;

                    openBtn.Left = 8; openBtn.Top = 3;
                    restoreBtn.Left = openBtn.Right + 6; restoreBtn.Top = 3;
                    chk.Left = restoreBtn.Right + 12; chk.Top = 6;
                }
            }

            // Make sure Clear Log is hosted in the same bar and anchored right
            var clear = FindByTextDeep<Button>(this, "Clear Log");
            var bar = openBtn?.Parent ?? restoreBtn?.Parent ?? chk?.Parent;
            if (clear != null && bar != null && clear.Parent != bar)
            {
                try
                {
                    clear.Parent?.Controls.Remove(clear);
                    bar.Controls.Add(clear);
                }
                catch { }
            }
            if (clear != null)
            {
                clear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                // position finalized in WireBackupBarLeftLayout handler
            }
        }

        /// <summary>
        /// Strongly enforces the layout on every resize, even if another handler tries to right-align the left buttons.
        /// </summary>
        private void WireBackupBarLeftLayout()
        {
            var open = _btnOpenBackups ?? FindByTextDeep<Button>(this, "Open Backups");
            var restore = _btnRestoreBackup ?? FindByTextDeep<Button>(this, "Restore Backup");
            var chk = _chkBackup ?? FindCheckboxNearBottom();
            var clear = FindByTextDeep<Button>(this, "Clear Log");

            Control bar = open?.Parent ?? restore?.Parent ?? chk?.Parent ?? _backupBar;
            if (bar == null) return;
            bar.RightToLeft = RightToLeft.No;

            // Ensure Clear Log lives in this bar
            if (clear != null && clear.Parent != bar)
            {
                try { clear.Parent?.Controls.Remove(clear); bar.Controls.Add(clear); } catch { }
            }

            // Subscribe our finalizer layout (run last)
            bar.Resize -= BackupBar_ForceLeftLayout_Handler;
            bar.Resize += BackupBar_ForceLeftLayout_Handler;

            // Do an initial layout pass
            BackupBar_ForceLeftLayout_Handler(bar, EventArgs.Empty);
        }

        private void BackupBar_ForceLeftLayout_Handler(object? sender, EventArgs e)
        {
            Control bar = sender as Control ?? _backupBar;
            if (bar == null) return;

            var open = _btnOpenBackups ?? FindByTextDeep<Button>(this, "Open Backups");
            var restore = _btnRestoreBackup ?? FindByTextDeep<Button>(this, "Restore Backup");
            var chk = _chkBackup ?? FindCheckboxNearBottom();
            var clear = FindByTextDeep<Button>(this, "Clear Log");

            int left = 8;
            if (open != null)
            {
                open.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                open.Left = left; open.Top = 3;
                left = open.Right + 6;
            }
            if (restore != null)
            {
                restore.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                restore.Left = left; restore.Top = 3;
                left = restore.Right + 12;
            }
            if (chk != null)
            {
                chk.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                chk.Left = left; chk.Top = 6;
            }
            if (clear != null)
            {
                if (clear.Parent != bar) { try { clear.Parent?.Controls.Remove(clear); bar.Controls.Add(clear); } catch { } }
                clear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                clear.Top = 3;
                clear.Left = bar.Width - clear.Width - 8;
            }
        }

        /// <summary>
        /// Try to make the log window ~3x taller. Works with common layouts (plain panel, SplitContainer, TableLayoutPanel).
        /// Best-effort; clipped to form height.
        /// </summary>
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
                Control bar = openBtn?.Parent ?? restoreBtn?.Parent ?? clearBtn?.Parent;
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
                Control host = bar.Parent;
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
