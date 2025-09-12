// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.TextAmountTag.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private static bool TryParseTextAmountTag(string tag, out string baseText, out string encodingToken)
        {
            baseText = string.Empty;
            encodingToken = string.Empty;
            if (string.IsNullOrWhiteSpace(tag)) return false;
            var s = tag.Trim();
            if (!s.StartsWith("Amount:", StringComparison.OrdinalIgnoreCase)) return false;
            var parts = s.Split(':');
            if (parts.Length < 4) return false;
            if (!parts[3].Equals("TXT", StringComparison.OrdinalIgnoreCase)) return false;
            baseText = parts[1].Trim();
            encodingToken = parts[2].Trim();
            return true;
        }
    }
}
