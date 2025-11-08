// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Menu/MainForm.Menu.DownloadButtons.Hook.cs
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
    public partial class MainForm : Form
    {
        // Ensure the Download buttons are always injected, even if another patch overwrote OnLoad.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try { EnsureDownloadButtons(); } catch { /* no-op if method missing */ }
			try { WireTreeCheckCascade(); } catch {}
        }
    }
}
