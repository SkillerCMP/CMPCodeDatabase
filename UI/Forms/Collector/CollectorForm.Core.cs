// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.Core.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        public void AddItem(string name, string code)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (collectorCodeMap.ContainsKey(name)) return;

            // On first item after a game switch: resolve patcher and update label
            try
            {
                bool first = (clbCollector?.Items?.Count ?? 0) == 0 && collectorCodeMap.Count == 0;
                if (first)
                {
                    string dbName = string.Empty;
                    try
                    {
                        foreach (Form f in Application.OpenForms)
                            if (f is MainForm mf) { dbName = mf.CurrentDatabaseName; break; }
                    }
                    catch { }

                    var resolved = DbCfg.ResolvePatcherPath(dbName);
                    if (!string.IsNullOrWhiteSpace(resolved) &&
                        !string.Equals(PatchProgramExePath, resolved, StringComparison.OrdinalIgnoreCase))
                    {
                        PatchProgramExePath = resolved;
                        try { UpdatePatcherStatus(PatchProgramExePath); } catch { }
                    }
                }
            }
            catch { }

            collectorCodeMap[name] = code ?? string.Empty;
            clbCollector.Items.Add(name, true);
        }
        public IEnumerable<string> GetAllNames() => collectorCodeMap.Keys.ToArray();

        public string GetCodeByName(string name) =>
            collectorCodeMap.TryGetValue(name, out var code) ? code : string.Empty;

        private void ClearAll()
        {
            if (clbCollector.Items.Count == 0) return;
            var res = MessageBox.Show(this, "Clear all collected entries?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res != DialogResult.Yes) return;

            clbCollector.Items.Clear();
            collectorCodeMap.Clear();
        }
    }
}
