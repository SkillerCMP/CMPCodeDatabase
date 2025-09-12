// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.CheckOps.cs
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
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private Button? _btnSelectAll;
        private Button? _btnSelectNone;
        private Button? _btnInvert;

        private void EnsureCheckOpsUi()
        {
            if (_btnSelectAll != null && !_btnSelectAll.IsDisposed)
                return;

            Control? host = Controls.Find("patchPanel", true).FirstOrDefault()
                           ?? Controls.Find("panelBottom", true).FirstOrDefault()
                           ?? Controls.Find("bottomPanel", true).FirstOrDefault();

            var panel = host as FlowLayoutPanel;
            if (panel == null)
            {
                panel = new FlowLayoutPanel
                {
                    Name = "patchPanel",
                    Dock = DockStyle.Bottom,
                    Height = 40,
                    FlowDirection = FlowDirection.LeftToRight,
                    Padding = new Padding(6),
                    AutoSize = false
                };
                Controls.Add(panel);
            }

            _btnSelectAll = new Button { Text = "Select All", AutoSize = true, Margin = new Padding(6, 6, 6, 6) };
            _btnSelectNone = new Button { Text = "Select None", AutoSize = true, Margin = new Padding(6, 6, 6, 6) };
            _btnInvert = new Button { Text = "Invert", AutoSize = true, Margin = new Padding(6, 6, 18, 6) };

            _btnSelectAll.Click += (s, e) => BulkCheck(true);
            _btnSelectNone.Click += (s, e) => BulkCheck(false);
            _btnInvert.Click += (s, e) => InvertChecks();

            panel.Controls.Add(_btnSelectAll);
            panel.Controls.Add(_btnSelectNone);
            panel.Controls.Add(_btnInvert);
        }

        private void BulkCheck(bool value)
        {
            var clb = FindClb();
            if (clb == null) return;

            for (int i = 0; i < clb.Items.Count; i++)
                clb.SetItemChecked(i, value);
        }

        private void InvertChecks()
        {
            var clb = FindClb();
            if (clb == null) return;

            for (int i = 0; i < clb.Items.Count; i++)
                clb.SetItemChecked(i, !clb.GetItemChecked(i));
        }
    }
}
