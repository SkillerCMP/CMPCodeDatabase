// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.TargetFileBar.cs
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
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
private void SetDataFilePath(string? path)
{
    DataFilePath = string.IsNullOrWhiteSpace(path) ? null : path;
    txtDataFile.Text = DataFilePath ?? string.Empty;
    try { tt.SetToolTip(txtDataFile, DataFilePath ?? string.Empty); } catch { }
    OnTargetFileChanged(DataFilePath);   // clears ❌ so you can re-test on the new file
}


        private void BrowseDataFile()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select target file to patch",
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                SetDataFilePath(ofd.FileName);
            }
        }

        private void TxtDataFile_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (files != null && files.Length > 0 && File.Exists(files[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void TxtDataFile_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (files != null && files.Length > 0 && File.Exists(files[0]))
            {
                SetDataFilePath(files[0]);
            }
        }
    }
}
