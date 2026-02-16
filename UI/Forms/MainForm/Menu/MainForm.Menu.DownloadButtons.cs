// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: URL/UI/Forms/MainForm/Menu/MainForm.Menu.DownloadButtons.cs
// Purpose: MainForm toolbar items for database/tools download shortcuts.
//
// Change (QOL, no functional DB logic change):
//  • Removes the legacy top-bar "Download Database" ToolStrip button.
//  • Keeps "Download Tools" button (unchanged behavior).
//  • Safely strips any existing Download Database items if found.
//
// This file intentionally only touches the toolbar/button wiring.
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
            // Find an existing top ToolStrip; otherwise create one.
            var tool = this.Controls.OfType<ToolStrip>()
                .FirstOrDefault(ts => ts.Dock == DockStyle.Top)
                ?? this.Controls.OfType<ToolStrip>().FirstOrDefault();

            if (tool == null)
            {
                tool = new ToolStrip
                {
                    Dock = DockStyle.Top,
                    GripStyle = ToolStripGripStyle.Hidden,
                    RenderMode = ToolStripRenderMode.System,
                    Name = "mainToolStrip"
                };

                this.Controls.Add(tool);
                tool.BringToFront();
            }

            // Remove legacy "Download Database" item(s) if present.
            foreach (var item in tool.Items.Cast<ToolStripItem>().ToArray())
            {
                var name = item.Name ?? string.Empty;
                var text = item.Text ?? string.Empty;

                if (name.Equals("btnDownloadDatabase", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("btnDb", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("Download Database", StringComparison.OrdinalIgnoreCase) ||
                    text.Replace("&", string.Empty).Equals("Download Database", StringComparison.OrdinalIgnoreCase))
                {
                    tool.Items.Remove(item);
                    item.Dispose();
                }
            }

            // Ensure "Download Tools" exists.
            if (!tool.Items.OfType<ToolStripItem>().Any(i =>
                    (i.Name ?? string.Empty).Equals("btnDownloadTools", StringComparison.OrdinalIgnoreCase) ||
                    (i.Text ?? string.Empty).Replace("&", string.Empty)
                        .Equals("Download Tools", StringComparison.OrdinalIgnoreCase)))
            {
                var btnTools = new ToolStripButton("Download Tools")
                {
                    Name = "btnDownloadTools",
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    AutoToolTip = true,
                    ToolTipText = "Open the tools download page"
                };

                btnTools.Click += (s, e) =>
                {
                    var url = AppSettings.Instance.ToolsDownloadUrl;
                    if (string.IsNullOrWhiteSpace(url))
                        url = "https://github.com/bucanero/apollo-lib/releases";

                    try
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    catch
                    {
                        MessageBox.Show("Could not open the tools link.", "CMPCodeDatabase",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                tool.Items.Add(btnTools);
            }

            // Clean up redundant separators (leading/trailing/duplicates).
            for (int i = tool.Items.Count - 1; i >= 0; i--)
            {
                if (tool.Items[i] is not ToolStripSeparator)
                    continue;

                bool isLeading = i == 0;
                bool isTrailing = i == tool.Items.Count - 1;
                bool prevIsSep = i > 0 && tool.Items[i - 1] is ToolStripSeparator;
                bool nextIsSep = i < tool.Items.Count - 1 && tool.Items[i + 1] is ToolStripSeparator;

                if (isLeading || isTrailing || prevIsSep || nextIsSep)
                {
                    var sep = tool.Items[i];
                    tool.Items.RemoveAt(i);
                    sep.Dispose();
                }
            }
        }
    }
}
