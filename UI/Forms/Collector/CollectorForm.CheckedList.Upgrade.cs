// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.CheckedList.Upgrade.cs
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
        private void EnsureCollectorCheckedListMode()
        {
            try
            {
                var fld = this.GetType().GetField("listCollector",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (fld == null) return;

                var current = fld.GetValue(this) as ListBox;
                if (current == null) return;
                if (current is CheckedListBox) return;

                var parent = current.Parent ?? this;
                var idx = parent.Controls.GetChildIndex(current);
                var clb = new CheckedListBox
                {
                    Name = current.Name,
                    Bounds = current.Bounds,
                    Dock = current.Dock,
                    Anchor = current.Anchor,
                    Font = current.Font,
                    ForeColor = current.ForeColor,
                    BackColor = current.BackColor,
                    IntegralHeight = false,
                    HorizontalScrollbar = true,
                    CheckOnClick = true,
                    SelectionMode = current.SelectionMode == SelectionMode.None
                        ? SelectionMode.One
                        : current.SelectionMode
                };

                foreach (var it in current.Items) clb.Items.Add(it);
                clb.Tag = current.Tag;

                parent.SuspendLayout();
                parent.Controls.Remove(current);
                current.Dispose();
                parent.Controls.Add(clb);
                parent.Controls.SetChildIndex(clb, idx);
                parent.ResumeLayout();

                fld.SetValue(this, clb);
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }
    }
}
