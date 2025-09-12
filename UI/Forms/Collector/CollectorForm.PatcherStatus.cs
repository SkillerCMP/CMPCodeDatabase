// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.PatcherStatus.cs
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
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private FlowLayoutPanel? patcherBar;
        private Label? lblPatcher;
        private Label? lblPatcherValue;

        /// <summary>
        /// Ensures the "Patcher:" row exists and is positioned directly UNDER the Target file row.
        /// Safe to call multiple times.
        /// </summary>
        private void EnsurePatcherStatusUi()
        {
            try
            {
                if (patcherBar != null && !patcherBar.IsDisposed)
                    return;

                patcherBar = new FlowLayoutPanel
                {
                    Name = "patcherBar",
                    Dock = DockStyle.Top,
                    Height = 24,
                    Padding = new Padding(8, 0, 8, 0),
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    AutoSize = false
                };

                lblPatcher = new Label
                {
                    AutoSize = true,
                    Text = "Patcher:",
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(0, 6, 6, 0),
                    Margin = new Padding(0, 0, 6, 0)
                };

                lblPatcherValue = new Label
                {
                    AutoSize = true,
                    Text = "patcher.exe",
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(0, 6, 0, 0)
                };

                patcherBar.Controls.Add(lblPatcher);
                patcherBar.Controls.Add(lblPatcherValue);

                // Insert directly under the dataBar (Target file) if it exists.
                if (dataBar != null && !dataBar.IsDisposed)
                {
                    // Remove and re-add to force order: [ ... (above) ... , patcherBar, dataBar, ... (below) ... ]
                    var keepDataBar = dataBar;
                    if (Controls.Contains(keepDataBar))
                        Controls.Remove(keepDataBar);

                    Controls.Add(patcherBar); // this will be below
                    Controls.Add(keepDataBar); // this will be above (closer to top)
                }
                else
                {
                    Controls.Add(patcherBar);
                    patcherBar.BringToFront();
                }
            }
            catch { /* non-fatal */ }
        }

        /// <summary>
        /// Updates the label showing the patcher file name. Also ensures the row exists.
        /// </summary>
        internal void UpdatePatcherStatus(string? exePath)
        {
            try
            {
                EnsurePatcherStatusUi();
                if (lblPatcherValue == null) return;

                string name = "patcher.exe";
                if (!string.IsNullOrWhiteSpace(exePath))
                    name = Path.GetFileName(exePath);

                lblPatcherValue.Text = name;
            }
            catch
            {
                // ignore UI errors
            }
        }
    }
}
