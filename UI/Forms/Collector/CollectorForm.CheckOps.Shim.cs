// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.CheckOps.Shim.cs
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
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private void SetAllChecked(bool value)
        {
            var clb = FindClb();
            if (clb == null) return;
            // Suppress flicker/per-item events if needed
            clb.BeginUpdate();
            try
            {
                for (int i = 0; i < clb.Items.Count; i++)
                    clb.SetItemChecked(i, value);
            }
            finally { clb.EndUpdate(); }
        }

        private void InvertAllChecks()
        {
            var clb = FindClb();
            if (clb == null) return;
            clb.BeginUpdate();
            try
            {
                for (int i = 0; i < clb.Items.Count; i++)
                    clb.SetItemChecked(i, !clb.GetItemChecked(i));
            }
            finally { clb.EndUpdate(); }
        }
    }
}