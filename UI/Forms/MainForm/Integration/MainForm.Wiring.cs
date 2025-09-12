// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Integration/MainForm.Wiring.cs
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
    /// <summary>
    /// Unified wiring point. Replace your existing MainForm.Wiring.cs with this to avoid duplicate overrides.
    /// Calls TryWireSeeInfo() so the Games right-click "See info" works for both folder and .txt games.
    /// </summary>
    public partial class MainForm : Form
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // These calls are idempotent (each has its own guard), so it's safe to call on every handle creation.
            try { TryWireCollectorSync(); } catch { /* optional: not present in all builds */ }
            try { TryWireDatabaseRootSwitch(); } catch { /* optional: not present in all builds */ }
            try { TryWireSeeInfo(); } catch { /* must be present from this patch */ }
            // try { TryWireAutoSend(); } catch { /* if/when you re-enable Step 5 */ }
        }
    }
}