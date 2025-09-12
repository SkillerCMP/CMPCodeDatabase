// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Progress.cs
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
        private string BuildProgressTitle(TreeNode node, string tag, int index, int total)
                        {
                            string baseName = originalNodeNames.ContainsKey(node) ? originalNodeNames[node] : (node.Text ?? "Code");
                            string existing = appliedModNames.ContainsKey(node) ? appliedModNames[node] : string.Empty;
                            string progress = $"{baseName} — {tag}: {index}/{total}";
                            if (!string.IsNullOrWhiteSpace(existing)) progress += $" ({existing})";
                            return progress;
                        }
    }
}
