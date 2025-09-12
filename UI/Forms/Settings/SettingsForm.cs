// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Settings/SettingsForm.cs
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
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    public class SettingsForm : Form
    {
        private TextBox txtPatchTool;
        private Button btnBrowse;
        private CheckBox chkShowPatchLog;
        private CheckBox chkOpenCollectorOnAdd;
        private TextBox txtDbUrl;
        private TextBox txtToolsUrl;
        private Button btnOk;
        private Button btnCancel;

        public SettingsForm()
        {
            Text = "Settings";
            Width = 620;
            Height = 260;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblPatch = new Label { Text = "Patch Tool:", Left = 12, Top = 16, AutoSize = true };
            txtPatchTool = new TextBox { Left = 120, Top = 12, Width = 360, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            btnBrowse = new Button { Text = "Browse…", Left = 490, Top = 10, Width = 100, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            chkShowPatchLog = new CheckBox { Text = "Show patch log by default", Left = 120, Top = 44, AutoSize = true };
            chkOpenCollectorOnAdd = new CheckBox { Text = "Open Collector window when adding codes", Left = 120, Top = 68, AutoSize = true };

            var lblDbUrl = new Label { Text = "Database Download URL:", Left = 12, Top = 100, AutoSize = true };
            txtDbUrl = new TextBox { Left = 180, Top = 96, Width = 410, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lblToolsUrl = new Label { Text = "Tools Download URL:", Left = 12, Top = 128, AutoSize = true };
            txtToolsUrl = new TextBox { Left = 180, Top = 124, Width = 410, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            btnOk = new Button { Text = "OK", Left = 430, Top = 168, Width = 120, DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            btnCancel = new Button { Text = "Cancel", Left = 300, Top = 168, Width = 120, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };

            Controls.AddRange(new Control[] { lblPatch, txtPatchTool, btnBrowse, chkShowPatchLog, chkOpenCollectorOnAdd, lblDbUrl, txtDbUrl, lblToolsUrl, txtToolsUrl, btnOk, btnCancel });

            Load += (_, __) => LoadSettings();
            btnBrowse.Click += (_, __) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Select Patch Tool",
                    Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
                    InitialDirectory = Directory.Exists(ToolPathResolver.DefaultToolsDir) ? ToolPathResolver.DefaultToolsDir : ToolPathResolver.AppRoot
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                    txtPatchTool.Text = ofd.FileName;
            };

            btnOk.Click += (_, __) =>
            {
                if (SaveSettings())
                    DialogResult = DialogResult.OK;
            };
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;
        }

        private void LoadSettings()
        {
            var s = AppSettings.Instance;
            txtPatchTool.Text = s.PatchToolPath ?? string.Empty;
            chkShowPatchLog.Checked = s.ShowPatchLogByDefault;
            chkOpenCollectorOnAdd.Checked = s.OpenCollectorOnAdd;
            txtDbUrl.Text = s.DatabaseDownloadUrl ?? string.Empty;
            txtToolsUrl.Text = s.ToolsDownloadUrl ?? string.Empty;
        }

        private bool SaveSettings()
        {
            var s = AppSettings.Instance;
            s.PatchToolPath = txtPatchTool.Text?.Trim();
            s.ShowPatchLogByDefault = chkShowPatchLog.Checked;
            s.OpenCollectorOnAdd = chkOpenCollectorOnAdd.Checked;
            s.DatabaseDownloadUrl = txtDbUrl.Text?.Trim();
            s.ToolsDownloadUrl = txtToolsUrl.Text?.Trim();
            s.Save();
            return true;
        }
    }
}
