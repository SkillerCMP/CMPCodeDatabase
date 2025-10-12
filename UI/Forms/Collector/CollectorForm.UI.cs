// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.UI.cs
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
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        public CollectorForm()
        {
            // Window
            Text = "Code Collector";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(660, 680);
            Size = new Size(780, 820);

            // List with checkboxes
            clbCollector = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                HorizontalScrollbar = true,
                CheckOnClick = true
            };

            // === Top PATCH bar (restored) ===
            var patchBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };
            btnPatchRunSelected      = new Button { Text = "Run Patch (Checked)", AutoSize = true };
            btnPatchRunAll           = new Button { Text = "Run Patch (All)",     AutoSize = true };
            btnPatchPreviewSelected  = new Button { Text = "Preview (Checked)",   AutoSize = true };
            patchBar.Controls.AddRange(new Control[] { btnPatchRunSelected, btnPatchRunAll, btnPatchPreviewSelected });

            // === New DATA FILE bar (drag/drop target) ===
            dataBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(8, 0, 8, 0),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };
            lblDataFile   = new Label { Text = "Target file:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 8, 6, 0) };
            txtDataFile   = new TextBox { ReadOnly = true, Width = 200, AllowDrop = true };
            btnBrowseData = new Button { Text = "Browse...", AutoSize = true };
            btnClearData  = new Button { Text = "Clear",     AutoSize = true };

            // DnD on textbox
            txtDataFile.DragEnter += TxtDataFile_DragEnter;
            txtDataFile.DragDrop  += TxtDataFile_DragDrop;

            // Wire clicks
            btnBrowseData.Click += (s, e) => BrowseDataFile();
            btnClearData.Click  += (s, e) => SetDataFilePath(null);

            dataBar.Controls.AddRange(new Control[] { lblDataFile, txtDataFile, btnBrowseData, btnClearData });

            // Bulk check operations
            var opsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };
            btnSelectAll  = new Button { Text = "Select All",  AutoSize = true };
            btnSelectNone = new Button { Text = "Select None", AutoSize = true };
            btnInvert     = new Button { Text = "Invert",      AutoSize = true };
            opsPanel.Controls.AddRange(new Control[] { btnSelectAll, btnSelectNone, btnInvert });

            // Bottom action bar
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(8)
            };

            btnCopyChecked = new Button { Text = "Copy Checked", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            btnCopyAll     = new Button { Text = "Copy All",     AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            btnClear       = new Button { Text = "Clear",        AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Bottom };

            // Wire events
            btnCopyChecked.Click += (s, e) => CopyChecked();
            btnCopyAll.Click     += (s, e) => CopyAll();
            btnClear.Click       += (s, e) => ClearAll();
            btnSelectAll.Click   += (s, e) => SetAllChecked(true);
            btnSelectNone.Click  += (s, e) => SetAllChecked(false);
            btnInvert.Click      += (s, e) => InvertAllChecks();

            btnPatchRunSelected.Click     += (s, e) => RunPatch(onlyChecked: true);
            btnPatchRunAll.Click          += (s, e) => RunPatch(onlyChecked: false);
            btnPatchPreviewSelected.Click += (s, e) => PreviewPatch(onlyChecked: true);

            // Keyboard shortcuts
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.A) { SetAllChecked(true); e.SuppressKeyPress = true; }
                else if (e.Control && e.Shift && e.KeyCode == Keys.A) { SetAllChecked(false); e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.C) { CopyChecked(); e.SuppressKeyPress = true; }
                else if (e.Control && e.Shift && e.KeyCode == Keys.C) { CopyAll(); e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.R) { RunPatch(onlyChecked: true); e.SuppressKeyPress = true; }
                else if (e.Control && e.Shift && e.KeyCode == Keys.R) { RunPatch(onlyChecked: false); e.SuppressKeyPress = true; }
                else if (e.Control && e.KeyCode == Keys.P) { PreviewPatch(onlyChecked: true); e.SuppressKeyPress = true; }
            };

            // Compose
            var buttonsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true
            };
            buttonsFlow.Controls.AddRange(new Control[] { btnClear, btnCopyAll, btnCopyChecked });

            bottomPanel.Controls.Add(buttonsFlow);
            bottomPanel.Controls.Add(new Panel { Dock = DockStyle.Fill }); // spacer

            Controls.Add(clbCollector);
            Controls.Add(bottomPanel);
            Controls.Add(opsPanel);
            Controls.Add(dataBar);   // new data bar just under patch bar
            Controls.Add(patchBar);  // patch bar at the very top
        }
    }
}
