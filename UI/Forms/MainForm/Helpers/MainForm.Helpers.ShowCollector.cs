// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.ShowCollector.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void ShowCollectorWindow()
        {
            if (collectorWindow == null || collectorWindow.IsDisposed)
            {
                collectorWindow = new CollectorForm();
                foreach (var kv in collectorFallback)
{                    if (BlockIfUnresolvedForCollector(null, kv.Value)) continue;
                    collectorWindow.AddItem(kv.Key, kv.Value);
}
            }
            if (!collectorWindow.Visible) collectorWindow.Show(this);
            if (collectorWindow.WindowState == FormWindowState.Minimized)
                collectorWindow.WindowState = FormWindowState.Normal;
            collectorWindow.Activate();
            try { collectorWindow.TopMost = true; collectorWindow.TopMost = false; } catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            collectorWindow.BringToFront();
        }
    }
}
