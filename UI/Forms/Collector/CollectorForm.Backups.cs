// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorControl.Backups.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Backup UI + logic integrated into Collector.
    /// Exposes TryInitBackupsUI() called from CollectorControl.Wiring.
    /// </summary>
    public partial class CollectorControl : UserControl
    {
        private Panel? _backupBar;
        private CheckBox? _chkBackup;
        private Button? _clearLogBtn;
        private Button? _btnOpenBackups;
        private Button? _btnRestoreBackup;
        private bool _backupInitDone;
        private bool _backupEventWired;

        internal void TryInitBackupsUI()
        {
            if (_backupInitDone) return;

            EnsureLogPanelSafe();
            EnsureBackupBar();

            if (!_backupEventWired)
            {
                PatchRunRequested -= OnPatchRunRequested_Backup;
                PatchRunRequested += OnPatchRunRequested_Backup;
                _backupEventWired = true;
            }

            _backupInitDone = true;
        }

        private void EnsureLogPanelSafe()
        {
            try
            {
                var mi = typeof(CollectorControl).GetMethod("EnsureLogPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mi?.Invoke(this, null);
            }
            catch { /* ignore */ }
        }

        private void EnsureBackupBar()
        {
            if (_backupBar != null && !_backupBar.IsDisposed) return;

            // Use Control for host so null-coalescing works (Panel vs. Form both derive from Control)
            Control host = this.Controls.OfType<Panel>()
                                        .Cast<Control>()
                                        .FirstOrDefault(p => p.Controls.OfType<RichTextBox>().Any())
                              ?? (Control)this;

            // Height based on current font so it survives Windows Accessibility -> Text size
            int barH = Math.Max(40, (int)Math.Ceiling(this.Font.Height * 2.6));

            _backupBar = new Panel { Dock = DockStyle.Bottom, Height = barH, Padding = new Padding(8, 4, 8, 6) };

            _btnOpenBackups = new Button
            {
                Text = "Open Backups",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 4, 10, 4),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            _btnRestoreBackup = new Button
            {
                Text = "Restore Backup",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 4, 10, 4),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            _chkBackup = new CheckBox
            {
                Text = "Backup before patch",
                Checked = true,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };

            _backupBar.Controls.Add(_chkBackup);
            _backupBar.Controls.Add(_btnOpenBackups);
            _backupBar.Controls.Add(_btnRestoreBackup);

            host.Controls.Add(_backupBar);
            _backupBar.BringToFront();

            _backupBar.Resize += (s, _) => LayoutBackupRowOnce();

            // Ensure Clear Log button is available and moved to backup row (right-aligned)
            try
            {
                if (_clearLogBtn == null)
                {
                    var found = this.Controls.Find("btnClearLog", true);
                    _clearLogBtn = (found != null && found.Length > 0 ? found[0] as Button : null)
                                   ?? FindDeepByText<Button>(this, "Clear Log");
                }
                if (_clearLogBtn != null && _clearLogBtn.Parent != _backupBar)
                {
                    _clearLogBtn.Parent?.Controls.Remove(_clearLogBtn);
                    _backupBar.Controls.Add(_clearLogBtn);
                }
                if (_clearLogBtn != null)
                {
                    _clearLogBtn.AutoSize = true;
                    _clearLogBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    _clearLogBtn.Padding = new Padding(10, 4, 10, 4);
                    _clearLogBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                }
            }
            catch { }

            try { BeginInvoke(new Action(() => LayoutBackupRowOnce())); } catch { LayoutBackupRowOnce(); }

            if (_btnOpenBackups != null) _btnOpenBackups.Click += (s, e) => OpenBackupsFolder();
            if (_btnRestoreBackup != null) _btnRestoreBackup.Click += (s, e) => RestoreBackupInteractive();
        }

        
        private void LayoutBackupRowOnce()
        {
            try
            {
                if (_backupBar == null) return;

                int padL = _backupBar.Padding.Left;
                int padR = _backupBar.Padding.Right;

                // Right-aligned: Clear Log
                if (_clearLogBtn == null)
                {
                    var found = this.Controls.Find("btnClearLog", true);
                    _clearLogBtn = (found != null && found.Length > 0 ? found[0] as Button : null)
                                   ?? FindDeepByText<Button>(this, "Clear Log");
                }
                if (_clearLogBtn != null)
                {
                    if (_clearLogBtn.Parent != _backupBar)
                    {
                        _clearLogBtn.Parent?.Controls.Remove(_clearLogBtn);
                        _backupBar.Controls.Add(_clearLogBtn);
                    }
                    _clearLogBtn.AutoSize = true;
                    _clearLogBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    _clearLogBtn.Padding = new Padding(10, 4, 10, 4);
                    _clearLogBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                }

                int yCenter(Control c)
                {
                    int y = (_backupBar.ClientSize.Height - c.Height) / 2;
                    return Math.Max(0, y);
                }

                int clearLeft = _backupBar.ClientSize.Width - padR;
                if (_clearLogBtn != null)
                {
                    _clearLogBtn.Location = new System.Drawing.Point(_backupBar.ClientSize.Width - _clearLogBtn.Width - padR, yCenter(_clearLogBtn));
                    clearLeft = _clearLogBtn.Left - 12;
                }

                // Left-to-right: Open, Restore, Checkbox
                int left = padL;
                if (_btnOpenBackups != null)
                {
                    _btnOpenBackups.Location = new System.Drawing.Point(left, yCenter(_btnOpenBackups));
                    left = _btnOpenBackups.Right + 8;
                }
                if (_btnRestoreBackup != null)
                {
                    _btnRestoreBackup.Location = new System.Drawing.Point(left, yCenter(_btnRestoreBackup));
                    left = _btnRestoreBackup.Right + 14;
                }

                if (_chkBackup != null)
                {
                    int avail = Math.Max(60, clearLeft - left);
                    int pref = _chkBackup.PreferredSize.Width;

                    if (pref > avail)
                    {
                        _chkBackup.AutoSize = false;
                        _chkBackup.AutoEllipsis = true;
                        _chkBackup.Width = avail;
                    }
                    else
                    {
                        _chkBackup.AutoEllipsis = false;
                        _chkBackup.AutoSize = true;
                    }

                    _chkBackup.Location = new System.Drawing.Point(left, yCenter(_chkBackup));
                }
            }
            catch { }
        }

        private static T? FindDeepByText<T>(Control root, string text) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c is T tt && string.Equals(c.Text, text, StringComparison.OrdinalIgnoreCase))
                    return (T)c;
                var sub = FindDeepByText<T>(c, text);
                if (sub != null) return sub;
            }
            return null;
        }
    
    }
}