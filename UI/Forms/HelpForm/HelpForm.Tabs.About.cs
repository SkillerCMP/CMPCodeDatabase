// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.Tabs.About.cs
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
    public partial class HelpForm : Form
    {
        private string BuildAboutHtml()
                {
                    return @"
        <h1>About</h1>
        <p><b>CMP Code Database</b> — helper and browser for text‑based code sets.</p>
        <p>Baseline: <span class='code'>CMPCodeDatabase_v1_0</span>. This Help window consolidates previous help topics and adds the new <b>Code Text</b> grammar tab.</p>";
                }
    }
}
