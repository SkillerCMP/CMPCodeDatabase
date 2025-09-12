// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.Tabs.Collector.cs
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
        private string BuildCollectorHtml()
                {
                    return @"
        <h1>Collector</h1>
        <ul>
          <li><b>Double‑click</b> a code in the browser to add it to the Collector.</li>
          <li><b>Right‑click</b> inside the Collector for actions (copy, clear, export).</li>
          <li>If a code uses modifiers, the chosen value is appended to the display name (e.g., <i>Attack Booster (Medium)</i>).</li>
        </ul>";
                }
    }
}
