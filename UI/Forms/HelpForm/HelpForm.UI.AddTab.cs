// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.UI.AddTab.cs
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
        private void AddTab(string title, string html)
                {
                    var page = new TabPage(title);
                    var view = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
                    view.DocumentText = Wrap(html);
                    page.Controls.Add(view);
                    tabs.TabPages.Add(page);
                }
    }
}
