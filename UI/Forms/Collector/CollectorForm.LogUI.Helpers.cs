// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorControl.LogUI.Helpers.cs
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
    public partial class CollectorControl : UserControl
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
            run?.Text = "Run Patch";

            var prev = FindByTextDeep<Button>(this, "Preview (Checked)") ??
                       FindByTextDeep<Button>(this, "Preview");
            prev?.Text = "Preview";
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
            invert?.Visible = false;

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

            chk?.Checked = true;

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

            Control? bar = open?.Parent ?? restore?.Parent ?? chk?.Parent ?? _backupBar;
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
            Control? bar = sender as Control ?? _backupBar;
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

    }
}
