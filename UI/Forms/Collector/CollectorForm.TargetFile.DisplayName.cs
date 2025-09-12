// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.TargetFile.DisplayName.cs
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
    /// <summary>
    /// Filename-only display for the target-file textbox.
    /// Exposes TryWireFilenameOnly() called from CollectorForm.Wiring.
    /// </summary>
    public partial class CollectorForm : Form
    {
        private bool _filenameOnlyInitDone;
        private bool _suppressDataFileTextChange;

        internal void TryWireFilenameOnly()
        {
            if (_filenameOnlyInitDone) return;
            if (txtDataFile == null || txtDataFile.IsDisposed) return;

            _filenameOnlyInitDone = true;
            txtDataFile.TextChanged -= TxtDataFile_TextChanged_FilenameOnly;
            txtDataFile.TextChanged += TxtDataFile_TextChanged_FilenameOnly;
            ApplyFilenameOnlyDisplay();
        }

        private void ApplyFilenameOnlyDisplay()
        {
            if (txtDataFile == null) return;

            _suppressDataFileTextChange = true;
            string full = DataFilePath ?? string.Empty;
            string display = string.IsNullOrWhiteSpace(full) ? string.Empty : Path.GetFileName(full);
            txtDataFile.Text = display;

            try { tt?.SetToolTip(txtDataFile, full); } catch { /* ignore */ }

            _suppressDataFileTextChange = false;
        }

        private void TxtDataFile_TextChanged_FilenameOnly(object? sender, EventArgs e)
        {
            if (_suppressDataFileTextChange) return;
            if (txtDataFile == null) return;

            string currentText = txtDataFile.Text ?? string.Empty;

            try
            {
                if (!string.IsNullOrWhiteSpace(DataFilePath))
                    tt?.SetToolTip(txtDataFile, DataFilePath);
                else
                    tt?.SetToolTip(txtDataFile, currentText);
            }
            catch { /* ignore */ }

            if (!string.IsNullOrWhiteSpace(DataFilePath) &&
                string.Equals(currentText, Path.GetFileName(DataFilePath), StringComparison.Ordinal))
                return;

            if (!string.IsNullOrWhiteSpace(currentText) &&
                (Path.IsPathRooted(currentText) || currentText.Contains(Path.DirectorySeparatorChar.ToString())))
            {
                DataFilePath = currentText;
            }

            _suppressDataFileTextChange = true;
            string display = string.IsNullOrWhiteSpace(DataFilePath) ? string.Empty : Path.GetFileName(DataFilePath);
            txtDataFile.Text = display;
            _suppressDataFileTextChange = false;
        }
    }
}