// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Names.cs
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
        private string GetDisplayName(TreeNode node)
                        {
                if (node == null) return string.Empty;

                    // Base name
                    string baseName = originalNodeNames.ContainsKey(node) ? originalNodeNames[node] : (node.Text ?? string.Empty);

                    // Badges: -M- for MOD, -N- for Note, -NM- for both
                    bool hasM = nodeHasMod.Contains(node);
                    bool hasN = nodeNotes.ContainsKey(node);
                    string badge = hasM && hasN ? "-NM-" : hasM ? "-M-" : hasN ? "-N-" : string.Empty;

                    // Applied MOD name (e.g., "(HP: 123)")
                    string applied = (appliedModNames.ContainsKey(node) && !string.IsNullOrEmpty(appliedModNames[node]))
                        ? " (" + appliedModNames[node] + ")"
                        : string.Empty;

                    // Prefix badge to the code name
                    string nameWithBadge = string.IsNullOrEmpty(badge) ? baseName : (badge + " " + baseName);

                    return nameWithBadge + applied;
                }

        private string GetCopyName(TreeNode node)
        
                {
                    if (node == null) return string.Empty;

                    string baseName;
                    if (originalNodeNames.ContainsKey(node) && !string.IsNullOrEmpty(originalNodeNames[node]))
                    {
                        baseName = originalNodeNames[node];
                    }
                    else
                    {
                        // Fall back to node.Text but strip any badge prefix like "-M- ", "-N- ", "-NM- "
                        string raw = node.Text ?? string.Empty;
                        if (raw.StartsWith("-NM- ")) raw = raw.Substring(5);
                        else if (raw.StartsWith("-M- ")) raw = raw.Substring(4);
                        else if (raw.StartsWith("-N- ")) raw = raw.Substring(4);
                        // Also handle bracketed style "[-NM-] " if ever used
                        if (raw.StartsWith("[-NM-] ")) raw = raw.Substring(7);
                        else if (raw.StartsWith("[-M-] ")) raw = raw.Substring(6);
                        else if (raw.StartsWith("[-N-] ")) raw = raw.Substring(6);
                        baseName = raw;
                    }

                    string applied = (appliedModNames.ContainsKey(node) && !string.IsNullOrEmpty(appliedModNames[node]))
                        ? " (" + appliedModNames[node] + ")"
                        : string.Empty;

                    return baseName + applied;
                }
    }
}
