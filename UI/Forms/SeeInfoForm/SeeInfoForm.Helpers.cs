// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/SeeInfoForm/SeeInfoForm.Helpers.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System.Text;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class SeeInfoForm : Form
    {
        // Minimal HTML wrapper so WebBrowser renders consistently.
        private static string WrapHtml(string innerHtml)
        {
            innerHtml ??= string.Empty;
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html><head><meta charset='utf-8'>");
            sb.Append("<style>body{font-family:'Segoe UI',Tahoma,Arial,sans-serif;font-size:9pt;margin:8px;}</style>");
            sb.Append("</head><body>");
            sb.Append(innerHtml);
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
