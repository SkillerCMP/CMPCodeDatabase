// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.Tabs.CodeBrowser.cs
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
        private string BuildCodeBrowserHtml()
                {
                    return @"
        <h1>Code Browser</h1>
        <ul>
          <li><b>Double-click a code</b> → adds it to the <i>Collector</i>.</li>
          <li><b>Prefix's</b> code/group → context actions (<i>View Note</i>, <i>Apply Mod</i>, etc.).</li>
          <li><b>Right-click</b> code/group → context actions (<i>View Note</i>, <i>Apply Mod</i>, etc.).</li>
          <li><b>Notes</b>: braces <code>{ ... }</code> are rendered as HTML (supports &lt;b&gt;, &lt;strong&gt;, &lt;i&gt;, &lt;span style='color:#...'&gt;, &lt;br&gt;, etc.).</li>
          <li><b>Refresh</b> → re-scan the <code>Database/</code> folder.</li>
        </ul>

        <h2>Markers in the Tree</h2>
        <ul>
          <li><span class='code'>-M-</span> item has modifiers</li>
          <li><span class='code'>-N-</span> item has a note</li>
          <li><span class='code'>-NM-</span> both</li>
        </ul>";
                }
    }
}
