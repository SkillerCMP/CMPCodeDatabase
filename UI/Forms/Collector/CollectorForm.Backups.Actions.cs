// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.Backups.Actions.cs
// Purpose: Backup create/open/restore actions for the Collector backup bar.
// Notes:
//  • Split from CollectorForm.Backups.cs during cleanup pass 9.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
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

    }
}
