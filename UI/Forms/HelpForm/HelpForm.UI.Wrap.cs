// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.UI.Wrap.cs
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
        private string Wrap(string inner)
                {
                    string css = @"<style>
        body { margin:0; padding:14px; font-family:'Segoe UI', Tahoma, Arial, sans-serif; background:#1e1e1e; color:#eaeaea; }
        h1 { font-size:22px; margin:0 0 12px 0; }
        h2 { font-size:16px; margin:14px 0 8px 0; color:#cfe3ff; }
        p, li { font-size:13.5px; line-height:1.45; color:#dcdcdc; }
        ul { margin:6px 0 0 22px; padding:0; }
        code, pre, .code { font-family:Consolas, 'Courier New', monospace; }
        pre { background:#1b1b1c; border:1px solid #3c3c3c; border-radius:8px; padding:10px; overflow:auto; font-size:12.5px; }
        .block { background:#252526; border:1px solid #3c3c3c; border-radius:8px; padding:12px; margin:0 0 12px 0; }
        .badge { display:inline-block; background:#2d2d30; border:1px solid #454545; border-radius:6px; padding:2px 6px; margin-right:8px; font-family:Consolas, 'Courier New', monospace; color:#cfe3ff; }
        .title { font-weight:600; }
        .desc { margin-top:6px; color:#c8c8c8; font-size:13px; line-height:1.35; }
        .code { display:inline-block; background:#1b1b1c; border:1px solid #3c3c3c; border-radius:6px; padding:2px 6px; color:#eaeaea; }
        .small { color:#bfbfbf; font-size:12px; }
        hr { border:none; border-top:1px solid #3c3c3c; margin:10px 0; }
                    </style>";
                    return "<!DOCTYPE html><html><head><meta charset='utf-8'><meta http-equiv='X-UA-Compatible' content='IE=11' />" + css + "</head><body>" + inner + "</body></html>";
                }
    }
}
