// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Menu/MainForm.Menu.DownloadButtons.cs
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
using System.Linq;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void EnsureDownloadButtons()
        {
            // Reuse/create a ToolStrip at top
            var tool = this.Controls.OfType<ToolStrip>().FirstOrDefault();
            if (tool == null)
            {
                tool = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, RenderMode = ToolStripRenderMode.System };
                this.Controls.Add(tool);
                tool.BringToFront();
            }

            if (!tool.Items.OfType<ToolStripButton>().Any(b => b.Name == "btnDownloadDatabase"))
            {
                var btnDb = new ToolStripButton("Download Database") { Name = "btnDownloadDatabase", DisplayStyle = ToolStripItemDisplayStyle.Text };
                btnDb.Click += (s, e) =>
                {
                    var url = AppSettings.Instance.DatabaseDownloadUrl;
                    if (string.IsNullOrWhiteSpace(url)) url = "https://drive.google.com/";
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch { MessageBox.Show("Could not open the database link."); }
                };
                tool.Items.Add(btnDb);
            }

            if (!tool.Items.OfType<ToolStripButton>().Any(b => b.Name == "btnDownloadTools"))
            {
                var btnTools = new ToolStripButton("Download Tools") { Name = "btnDownloadTools", DisplayStyle = ToolStripItemDisplayStyle.Text };
                btnTools.Click += (s, e) =>
                {
                    var url = AppSettings.Instance.ToolsDownloadUrl;
                    if (string.IsNullOrWhiteSpace(url)) url = "https://example.com/your-tool-download";
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                    catch { MessageBox.Show("Could not open the tools link."); }
                };
                tool.Items.Add(new ToolStripSeparator());
                tool.Items.Add(btnTools);
            }
        }
    }
}
