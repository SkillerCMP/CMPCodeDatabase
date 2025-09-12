// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.Tabs.Notes.cs
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
        private string BuildNotesHtml()
                {
                    return @"
        <h1>Notes</h1>
        <p>Notes are authored inline using braces and support simple HTML:</p>
        <p>If a Note Is Setup at the top of the .txt file it will show as a Game Note</p>
        <pre>+Infinite Health{Use offline only.Applies to campaign mode.}</pre>
        <p>Supported tags: &lt;b&gt;, &lt;strong&gt;, &lt;i&gt;, &lt;span style='color:#...;'&gt;, &lt;br&gt;.</p>";
                }
    }
}
