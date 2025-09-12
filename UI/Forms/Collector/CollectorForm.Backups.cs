// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.Backups.cs
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
    /// Exposes TryInitBackupsUI() called from CollectorForm.Wiring.
    /// </summary>
    public partial class CollectorForm : Form
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
                var mi = typeof(CollectorForm).GetMethod("EnsureLogPanel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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

            _backupBar = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8, 4, 8, 6) };
            
			_btnOpenBackups = new Button
            {
                Text = "Open Backups",
                Width = 120,
                Height = 24,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            _btnRestoreBackup = new Button
            {
                Text = "Restore Backup",
                Width = 120,
                Height = 24,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            _chkBackup = new CheckBox { Text = "Backup before patch", Checked = true,
                AutoSize = true,
                Left = 240,
                Top = 6,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };

            _backupBar.Controls.Add(_chkBackup);
            _backupBar.Controls.Add(_btnOpenBackups);
            _backupBar.Controls.Add(_btnRestoreBackup);

            host.Controls.Add(_backupBar);
            _backupBar.BringToFront();

            _backupBar.Resize += (s, _) =>
            {
                if (_btnRestoreBackup != null && _btnOpenBackups != null)
                {
                    _btnRestoreBackup.Left = _backupBar.Width - _btnRestoreBackup.Width - 8;
                    _btnRestoreBackup.Top = 3;
                    _btnOpenBackups.Left = _btnRestoreBackup.Left - _btnOpenBackups.Width - 6;
                    _btnOpenBackups.Top = 3;
                }
            };
            _backupBar.PerformLayout();
            LayoutBackupRowOnce();

            
            // Ensure Clear Log button is available and moved to backup row (right-aligned)
            try {
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
                    _clearLogBtn.Top = 3;
                    _clearLogBtn.Left = _backupBar.Width - _clearLogBtn.Width - 8;
                    _clearLogBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                }
            } catch { }
if (_btnOpenBackups != null) _btnOpenBackups.Click += (s, e) => OpenBackupsFolder();
            if (_btnRestoreBackup != null) _btnRestoreBackup.Click += (s, e) => RestoreBackupInteractive();
        }

        
        private void LayoutBackupRowOnce()
        {
            try
            {
                int left = 8;
                if (_btnOpenBackups != null) { _btnOpenBackups.Left = left; _btnOpenBackups.Top = 3; left = _btnOpenBackups.Right + 6; }
                if (_btnRestoreBackup != null) { _btnRestoreBackup.Left = left; _btnRestoreBackup.Top = 3; left = _btnRestoreBackup.Right + 12; }
                if (_chkBackup != null) { _chkBackup.Left = left; _chkBackup.Top = 6; }
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
                    _clearLogBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                    _clearLogBtn.Top = 3;
                    _clearLogBtn.Left = _backupBar.Width - _clearLogBtn.Width - 8;
                }
            } catch { }
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
                var f = typeof(CollectorForm).GetField("_activeGameKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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