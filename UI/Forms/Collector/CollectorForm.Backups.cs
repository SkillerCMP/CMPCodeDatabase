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
    
        private void OnPatchRunRequested_Backup(object? sender, PatchRequestEventArgs e)
        {
            try
            {
                if (_chkBackup == null || !_chkBackup.Checked) return;
                if (string.IsNullOrWhiteSpace(DataFilePath) || !File.Exists(DataFilePath))
                {
                    AppendLog("Backup skipped: no valid data file selected.");
                    return;
                }

                string appRoot = AppDomain.CurrentDomain.BaseDirectory;
                string gameName = GetActiveGameNameForBackup();
                string zipPath = CreateBackupZipSafe(appRoot, gameName, DataFilePath);
                AppendLog($"[backup] {Path.GetFileName(zipPath)} created.");
            }
            catch (Exception ex)
            {
                AppendLog("[backup] ERROR: " + ex.Message);
            }
        }

        private string GetActiveGameNameForBackup()
        {
            try
            {
                var f = typeof(CollectorControl).GetField("_activeGameKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var v = f?.GetValue(this) as string;
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            catch { /* ignore */ }

            try
            {
                if (!string.IsNullOrWhiteSpace(this.Text)) return this.Text;
            }
            catch { /* ignore */ }

            return Path.GetFileNameWithoutExtension(DataFilePath ?? "UnknownGame") ?? "UnknownGame";
        }

        private void OpenBackupsFolder()
        {
            try
            {
                string appRoot = AppDomain.CurrentDomain.BaseDirectory;
                string gameName = GetActiveGameNameForBackup();
                string root = GetBackupsRootSafe(appRoot, gameName, out var gameFolder);
                Directory.CreateDirectory(gameFolder);
                Process.Start("explorer.exe", gameFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Open Backups", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreBackupInteractive()
        {
            try
            {
                string appRoot = AppDomain.CurrentDomain.BaseDirectory;
                string gameName = GetActiveGameNameForBackup();
                string root = GetBackupsRootSafe(appRoot, gameName, out var gameFolder);

                using var ofd = new OpenFileDialog
                {
                    Title = "Select backup ZIP to restore",
                    Filter = "ZIP files (*.zip)|*.zip|All files (*.*)|*.*",
                    InitialDirectory = Directory.Exists(gameFolder) ? gameFolder : root,
                    Multiselect = false
                };
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                string zipPath = ofd.FileName;
                if (string.IsNullOrWhiteSpace(DataFilePath))
                {
                    MessageBox.Show(this, "No target data file selected to restore into.", "Restore Backup",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var targetDir = Path.GetDirectoryName(DataFilePath) ?? AppDomain.CurrentDomain.BaseDirectory;

                using var zip = ZipFile.OpenRead(zipPath);
                var entry = zip.Entries.FirstOrDefault();
                if (entry == null)
                {
                    MessageBox.Show(this, "ZIP is empty.", "Restore Backup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var targetPath = Path.Combine(targetDir, entry.Name);
                var overwrite = DialogResult.Yes == MessageBox.Show(this,
                    $"Restore '{entry.Name}' to:\n{targetPath}\n\nThis will overwrite the existing file if present. Continue?",
                    "Restore Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (!overwrite) return;

                entry.ExtractToFile(targetPath, true);
                AppendLog($"[backup] Restored '{entry.Name}' to {targetDir}");
                MessageBox.Show(this, "Restore completed.", "Restore Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Restore Backup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string CreateBackupZipSafe(string appRoot, string gameName, string dataFilePath)
        {
            try
            {
                var t = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                    .FirstOrDefault(x => x.FullName == "ApolloGUI.BackupManager");
                if (t != null)
                {
                    var createZip = t.GetMethod("CreateBackupZip", new[] { typeof(AppSettings), typeof(string), typeof(string), typeof(string) });
                    if (createZip != null)
                    {
                        var zipPath = createZip.Invoke(null, new object?[] { AppSettings.Instance, appRoot, gameName, dataFilePath }) as string;
                        if (!string.IsNullOrWhiteSpace(zipPath)) return zipPath!;
                    }
                }
            }
            catch { /* ignore */ }

            var folder = GetBackupsRootSafe(appRoot, gameName, out var gameFolder);
            Directory.CreateDirectory(gameFolder);
            var modName = Path.GetFileNameWithoutExtension(dataFilePath) ?? "data";
            modName = Sanitize(modName);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var zipName = $"({modName})_{stamp}.zip";
            var zipFull = Path.Combine(gameFolder, zipName);
            using (var zip = ZipFile.Open(zipFull, ZipArchiveMode.Create))
            {
                var entryName = Path.GetFileName(dataFilePath);
                zip.CreateEntryFromFile(dataFilePath, entryName, CompressionLevel.Optimal);
            }
            return zipFull;
        }

        private string GetBackupsRootSafe(string appRoot, string gameName, out string gameFolder)
        {
            try
            {
                var t = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                    .FirstOrDefault(x => x.FullName == "ApolloGUI.BackupManager");
                if (t != null)
                {
                    var getRoot = t.GetMethod("GetBackupsRoot", new[] { typeof(AppSettings), typeof(string) });
                    var sanitize = t.GetMethod("Sanitize", new[] { typeof(string) });
                    if (getRoot != null && sanitize != null)
                    {
                        var root = getRoot.Invoke(null, new object?[] { AppSettings.Instance, appRoot }) as string ?? Path.Combine(appRoot, "Backups");
                        var safeGame = sanitize.Invoke(null, new object?[] { gameName }) as string ?? "UnknownGame";
                        gameFolder = Path.Combine(root, safeGame);
                        return root;
                    }
                }
            }
            catch { /* ignore */ }

            var rootFallback = Path.Combine(appRoot, "Backups");
            gameFolder = Path.Combine(rootFallback, Sanitize(gameName));
            return rootFallback;
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
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