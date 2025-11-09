// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.Savepatch.OnShown.cs
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

namespace CMPCodeDatabase
{
    public partial class CollectorForm : System.Windows.Forms.Form
    {
        // Do not hook drag & drop or Ctrl+O for .savepatch import
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                AllowDrop = false;
            }
            catch { }
        
            try { ApplyFixedCollectorSizing(); } catch { }
			try { EnsureShortcuts_SHORT(); EnsureOpsMenu_MENU(); } catch {}
			try { EnsureCollectorTools_ELFCRC(); } catch {}
        }
    }
}
