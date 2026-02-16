using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CMPCodeDatabase.UI.Dialogs;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase;

public partial class MainForm
{
    private void DatabaseVisitSite()
    {
        var url = AppSettings.Instance.DatabaseDownloadUrl;
        TryOpenUrl(url);
    }

    private void DatabaseOpenLocalFolder()
    {
        var root = DatabaseManager.GetLocalDatabaseRoot();
        if (string.IsNullOrWhiteSpace(root))
        {
            MessageBox.Show(this, "Database folder could not be resolved.", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Directory.CreateDirectory(root);
        TryOpenFolder(root);
    }

    private async Task DatabaseDownloadDatabaseAsync()
    {
        using var dlg = new DatabasePickerDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var selected = dlg.SelectedDatabase;
        if (selected is null) return;

        await DatabaseManager.DownloadDatabasesAsync(this, new[] { selected }, promptBeforeDownloading: false);
        // Refresh selector if we’re pointing at Files\Database
        LoadDatabaseSelector_FilesRoot();
    }

    private async Task DatabaseDownloadAllDatabasesAsync()
    {
        var confirm = MessageBox.Show(
            this,
            "This will download ALL available databases into your local Files\\Database folder.\n\nContinue?",
            "Download All Databases",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        await DatabaseManager.DownloadAllDatabasesAsync(this);
        LoadDatabaseSelector_FilesRoot();
    }

    private async Task DatabaseCheckForUpdatesAsync()
    {
        using var dlg = new DatabaseUpdatesDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var selected = dlg.SelectedDatabasesToUpdate;
        if (selected is null || selected.Length == 0) return;

        await DatabaseManager.DownloadDatabaseUpdatesAsync(this, selected);
        LoadDatabaseSelector_FilesRoot();
    }



private async Task DatabaseExportLocalManifestAsync()
{
    var root = DatabaseManager.GetLocalDatabaseRoot();
    Directory.CreateDirectory(root);

    using var sfd = new SaveFileDialog
    {
        Title = "Export Local Manifest",
        Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        FileName = "manifest.local.json",
        InitialDirectory = root
    };

    if (sfd.ShowDialog(this) != DialogResult.OK) return;

    try
    {
        var json = await Task.Run(() => DatabaseManager.BuildLocalManifestJson(root, txtOnly: true, includeFiles: true));
        await File.WriteAllTextAsync(sfd.FileName, json);
        MessageBox.Show(this, "Local manifest exported.", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show(this, "Failed to export local manifest:\n\n" + ex.Message, "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

    private static void TryOpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open URL:\n{url}\n\n{ex.Message}", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void TryOpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open folder:\n{path}\n\n{ex.Message}", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
