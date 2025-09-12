// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.Events.cs
// Purpose: MainForm event handlers for buttons/menus and code actions.
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
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private void CopyChecked()
        {
            var blocks = new List<string>();
            for (int i = 0; i < clbCollector.Items.Count; i++)
            {
                if (!clbCollector.GetItemChecked(i)) continue;
                string name = clbCollector.Items[i]?.ToString() ?? string.Empty;
                if (name.Length == 0) continue;
                if (collectorCodeMap.TryGetValue(name, out var code))
                    blocks.Add($"{name}{Environment.NewLine}{Apply64BitHexBlocking(code)}");
            }
            if (blocks.Count > 0)
                Clipboard.SetText(string.Join(Environment.NewLine + Environment.NewLine, blocks));
        }

        private void CopyAll()
        {
            var blocks = new List<string>();
            foreach (var obj in clbCollector.Items)
            {
                string name = obj?.ToString() ?? string.Empty;
                if (name.Length == 0) continue;
                if (collectorCodeMap.TryGetValue(name, out var code))
                    blocks.Add($"{name}{Environment.NewLine}{Apply64BitHexBlocking(code)}");
            }
            if (blocks.Count > 0)
                Clipboard.SetText(string.Join(Environment.NewLine + Environment.NewLine, blocks));
        }
    }
}
