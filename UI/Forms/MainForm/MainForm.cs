using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.cs
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
        // Tracks applied MOD labels per node, keyed by a "title" (text before ':') or the full label if no ':'.
                // This lets us replace values for the same MOD title (e.g., "HP: 9999" overwrites "HP: Max") and avoid duplicates.
                private readonly Dictionary<TreeNode, Dictionary<string, string>> appliedModLabelMap =
                    new Dictionary<TreeNode, Dictionary<string, string>>();

                private static string ExtractModKey(string label)
                {
                    if (string.IsNullOrWhiteSpace(label)) return string.Empty;
                    int idx = label.IndexOf(':');
                    if (idx > 0) return label.Substring(0, idx).Trim();
                    return label.Trim().ToUpperInvariant();
                }

                private void ClearAppliedModNames(TreeNode node)
                {
                    if (node == null) return;
                    appliedModNames.Remove(node);
                    appliedModLabelMap.Remove(node);
                }

                private string codeDirectory = null!;

                private readonly Dictionary<string, List<(string Value, string Name)>> modDefinitions =
                    new Dictionary<string, List<(string Value, string Name)>>();

                // Enhanced: headered tables for MODs: Tag -> headers + rows
                private readonly Dictionary<string, List<string>> modHeaders = new();
                private readonly Dictionary<string, List<string[]>> modRows = new();

                private readonly Dictionary<TreeNode, string> originalCodeTemplates = new();
                private readonly Dictionary<TreeNode, string> originalNodeNames = new();
                private readonly Dictionary<TreeNode, string> nodeNotes = new();
                private readonly Dictionary<TreeNode, string> nodeCredits = new();


                // Popup notes per node (double-brace {{...}}) and session suppression
                private readonly Dictionary<TreeNode, List<string>> nodePopupNotes = new();
                private readonly HashSet<string> suppressedPopupNotes = new();

        // Last highlighted MOD range in the preview (start, length)
        private (int start, int length)? _lastModHighlight = null;
                private readonly HashSet<TreeNode> nodeHasMod = new();
                private readonly Dictionary<TreeNode, string> appliedModNames = new();

                private ContextMenuStrip codesContextMenu = null!;
                // Bold font reused for group/subgroup nodes
                private Font _boldNodeFont = null!;

                // UI controls
                private ComboBox dbSelector = null!;
                private TreeView treeGames = null!;
                private TreeView treeCodes = null!;
                private TextBox txtCodePreview = null!;
                private Button btnRefresh = null!;

                // Collector & Calculator windows
                private CollectorForm? collectorWindow;
                // Tabbed layout (optional): embeds CollectorControl beside Code Preview
                private TabControl? tabPreviewCollector;
                private TabPage? tabPreview;
                private TabPage? tabCollector;
                private CollectorControl? collectorTab;
                private EnhancedCalculatorForm? calculatorWindow;
private Form? databaseStatsWindow;
                // Fallback collector storage when collector window is closed
                private readonly Dictionary<string, string> collectorFallback = new();
                private readonly Dictionary<string, CMPCodeDatabase.Core.Models.CollectorItemMeta> collectorFallbackMeta = new(StringComparer.OrdinalIgnoreCase);


                public MainForm()
                {
                    InitializeComponent();
                    InitializeContextMenu();
        			GameInfoContextHelper.Attach(treeGames, node => (node.Tag as string) ?? string.Empty);
                    LoadDatabaseSelector();
                }

                // Formats hex payloads into 64-bit (16 hex chars) blocks, one per line.
                [GeneratedRegex(@"[0-9A-Fa-f]{2,}", RegexOptions.None)]
                private static partial Regex Rx_HexWord_Generated();
                private static readonly Regex HexWord = Rx_HexWord_Generated();

        // Nested MOD dialogs and the enhanced calculator were moved to modular partial files during cleanup pass 4.
        private void AppendAppliedModName(TreeNode node, string displayName)

        {
            if (node == null) return;
            if (string.IsNullOrWhiteSpace(displayName)) return;

            if (!appliedModLabelMap.TryGetValue(node, out var map))
            {
                map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                appliedModLabelMap[node] = map;
            }

            string key = ExtractModKey(displayName);
            if (string.IsNullOrEmpty(key)) key = displayName.Trim().ToUpperInvariant();

            map[key] = displayName.Trim();

            appliedModNames[node] = string.Join(", ", map.Values);
        }

        // --- Note UI helpers (popup-aware) ---
        private bool HasPopupNote(TreeNode node) =>
            node != null && nodePopupNotes.TryGetValue(node, out var list) && list != null && list.Count > 0;

        private void ShowNoteOrPopupForNode(TreeNode node)
        {
            if (node == null) return;
            if (HasPopupNote(node))
            {
                // Show the popup dialog (non-gating here)
                string html = string.Join("<hr/>", nodePopupNotes[node].Select(UnescapeNote));
                using (var dlg = new NotePopupDialog(GetCopyName(node), html))
                {
                    dlg.ShowDialog(this);
                }
                return;
            }
            ShowNoteForNode(node);
        }

        // Gate actions (Select MOD / Add to Collector). Return true if user chooses Continue.
        private bool GateOnAction(TreeNode node)
        {
            if (node == null) return true;
            // If suppressed for the session, allow through
            string key = GetCopyName(node);
            if (string.IsNullOrWhiteSpace(key)) key = node.Text ?? string.Empty;
            if (suppressedPopupNotes.Contains(key)) return true;

            if (!nodePopupNotes.TryGetValue(node, out var list) || list == null || list.Count == 0) return true;

            string html = string.Join("<hr/>", list.Select(UnescapeNote));
            using (var dlg = new NotePopupDialog(GetCopyName(node), html))
            {
                var result = dlg.ShowDialog(this);
                if (dlg.Suppress) suppressedPopupNotes.Add(key);
                return result == DialogResult.OK; // Continue => true; Don't Use => false
            }
        }

}
}