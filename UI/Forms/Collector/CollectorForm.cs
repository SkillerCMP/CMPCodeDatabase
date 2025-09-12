// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        // Core UI
        private CheckedListBox clbCollector;
        private Button btnCopyChecked;
        private Button btnCopyAll;
        private Button btnClear;

        // Bulk check ops
        private Button btnSelectAll;
        private Button btnSelectNone;
        private Button btnInvert;

        // Patch bar buttons (restored)
        private Button btnPatchRunSelected;
        private Button btnPatchRunAll;
        private Button btnPatchPreviewSelected;

        // Target file bar (new)
        private FlowLayoutPanel dataBar;
        private Label lblDataFile;
        private TextBox txtDataFile;
        private Button btnBrowseData;
        private Button btnClearData;
        private readonly ToolTip tt = new ToolTip();

        // Name -> Code text
        private readonly Dictionary<string, string> collectorCodeMap = new(StringComparer.OrdinalIgnoreCase);

        // Optional integration settings (host can set these)
        public string? PatchProgramExePath { get; set; } // e.g., Patcher.exe full path
        public Func<string?>? ResolveDataFilePath { get; set; } // callback to get data file path at run-time

        // Currently selected target data file (from the small drop bar)
        public string? DataFilePath { get; private set; }

        // Events to integrate with your external Patch Program
        public event EventHandler<PatchRequestEventArgs>? PatchRunRequested;
        public event EventHandler<PatchRequestEventArgs>? PatchPreviewRequested;

        // Helper: collect entries
        private List<KeyValuePair<string,string>> CollectEntries(bool onlyChecked)
        {
            var list = new List<KeyValuePair<string,string>>();
            for (int i = 0; i < clbCollector.Items.Count; i++)
            {
                bool include = !onlyChecked || clbCollector.GetItemChecked(i);
                if (!include) continue;
                string name = clbCollector.Items[i]?.ToString() ?? string.Empty;
                if (name.Length == 0) continue;
                if (collectorCodeMap.TryGetValue(name, out var code))
                    list.Add(new KeyValuePair<string, string>(name, code));
            }
            return list;
        }
    }

    // Strongly-typed event payload for patch requests
    public sealed class PatchRequestEventArgs : EventArgs
    {
        public bool OnlyChecked { get; }
        public IReadOnlyList<KeyValuePair<string,string>> Entries { get; }
        public string SavepatchPath { get; }
        public string SavepatchContent { get; }
        public string SelectionString { get; }
        public string? DataFilePath { get; }

        public PatchRequestEventArgs(
            bool onlyChecked,
            IReadOnlyList<KeyValuePair<string,string>> entries,
            string savepatchPath,
            string savepatchContent,
            string selectionString,
            string? dataFilePath)
        {
            OnlyChecked = onlyChecked;
            Entries = entries;
            SavepatchPath = savepatchPath;
            SavepatchContent = savepatchContent;
            SelectionString = selectionString;
            DataFilePath = dataFilePath;
        }
    }
}
