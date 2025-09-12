// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.LayoutTweaks.cs
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
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private void TryCompactOpsRow()
        {
            try
            {
                var selectAll = FindControlByText<Button>(this, "Select All");
                var selectNone = FindControlByText<Button>(this, "Select None");
                if (selectAll == null || selectNone == null) return;

                var copyChecked = FindControlByText<Button>(this, "Copy Checked");
                var copyAll     = FindControlByText<Button>(this, "Copy All");
                var clearBtn    = FindControlByText<Button>(this, "Clear");

                FlowLayoutPanel host = selectAll.Parent as FlowLayoutPanel;
                if (host == null || host.IsDisposed)
                {
                    host = new FlowLayoutPanel
                    {
                        Name = "opsCompactRow",
                        Dock = DockStyle.Top,
                        Height = 36,
                        Padding = new Padding(8, 4, 8, 4),
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = false
                    };
                    Controls.Add(host);
                    host.BringToFront();
                }

                void move(Control c)
                {
                    if (c == null) return;
                    if (c.Parent == host) return;
                    c.Parent?.Controls.Remove(c);
                    host.Controls.Add(c);
                    c.Margin = new Padding(0, 4, 8, 4);
                }

                move(selectAll);
                move(selectNone);
                move(copyChecked);
                move(copyAll);
                move(clearBtn);
            }
            catch { }
        }
    }
}
