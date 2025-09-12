// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Dialogs/MissingFilesDialog.cs
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
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase.UI.Dialogs
{
    public sealed class MissingFilesDialog : Form
    {
        public MissingFilesDialog(bool missingFilesRoot, bool missingDatabase, bool missingTools)
        {
            Text = "Setup Required";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 640;
            Height = 360;

            var lblTitle = new Label
            {
                Text = "Some required folders were missing and have been created:",
                AutoSize = true,
                Left = 16, Top = 16, Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10, FontStyle.Bold)
            };

            var sb = new StringBuilder();
            if (missingFilesRoot) sb.AppendLine(@"• Files\");
            if (missingDatabase)  sb.AppendLine(@"• Files\Database");
            if (missingTools)     sb.AppendLine(@"• Files\Tools");
            if (sb.Length == 0)   sb.AppendLine("• (none)");
            var missingList = sb.ToString();

            var txtMissing = new TextBox
            {
                Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None,
                Left = 24, Top = 48, Width = 580, Height = 64, TabStop = false,
                Text = missingList
            };

            var instructionsText =
                "To finish setup:" + Environment.NewLine +
                "1) Download and unzip the Database files into:  " + @"Files\Database" + Environment.NewLine +
                "2) Download and unzip the Tools files into:     " + @"Files\Tools" + Environment.NewLine +
                Environment.NewLine +
                "After this, Press ReloadDB on Main Screen After closing this one, and then the app will be fully functional.";

            var instructions = new TextBox
            {
                Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None,
                Left = 16, Top = 120, Width = 600, Height = 120, TabStop = false,
                Text = instructionsText
            };

            var btnDb = new Button { Text = "Open Database Page", Left = 16, Top = 250, Width = 180 };
            var btnTools = new Button { Text = "Open Tools Page", Left = 204, Top = 250, Width = 160 };
            var btnClose = new Button { Text = "Close", Left = 500, Top = 250, Width = 100, DialogResult = DialogResult.OK };

            btnDb.Click += (s, e) =>
            {
                var url = AppSettings.Instance.DatabaseDownloadUrl;
                if (string.IsNullOrWhiteSpace(url)) url = "https://drive.google.com/";
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { MessageBox.Show(this, "Could not open the database page."); }
            };

            btnTools.Click += (s, e) =>
            {
                var url = AppSettings.Instance.ToolsDownloadUrl;
                if (string.IsNullOrWhiteSpace(url)) url = "https://example.com/";
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { MessageBox.Show(this, "Could not open the tools page."); }
            };

            Controls.Add(lblTitle);
            Controls.Add(txtMissing);
            Controls.Add(instructions);
            Controls.Add(btnDb);
            Controls.Add(btnTools);
            Controls.Add(btnClose);
        }
    }
}
