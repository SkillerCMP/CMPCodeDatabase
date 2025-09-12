// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.Tabs.Shortcuts.cs
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
        private string BuildShortcutsHtml()
                {
                    return @"
        <h1>Shortcuts & Mouse Actions</h1>
                            <h2>Global</h2>
                            <ul>
                              <li><b>F1</b> — Open Help</li>
                              <li><b>Ctrl+L</b> — Toggle Collector window (View → Collector)</li>
                              <li><b>Ctrl+K</b> — Toggle Calculator window (View → Calculator)</li>
                            </ul>
                            <h3>Collector</h3>
                            <ul>
                              <li><b>Ctrl+A</b> — Select all</li>
                              <li><b>Ctrl+C</b> — Copy selected</li>
                              <li><b>Delete</b> — Remove selected</li>
                            </ul>
                            <h3>Code Browser (Mouse)</h3>
                            <ul>
                              <li><b>Double-click code</b> — Add to Collector</li>
                              <li><b>Right-click code/group</b> — Context menu (View Note, Apply Mod, etc.)</li>
                            </ul>";
                }
    }
}
