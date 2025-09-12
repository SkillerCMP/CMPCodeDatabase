// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/SeeInfoForm/SeeInfoForm.Load.cs
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
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace CMPCodeDatabase
{
    public partial class SeeInfoForm : Form
    {
        private void LoadData()
        {
            // Build the set of files to consider based on ctor path
            IEnumerable<string> files;
            if (!string.IsNullOrWhiteSpace(_singleFile) && File.Exists(_singleFile))
            {
                files = new[] { _singleFile };
            }
            else if (!string.IsNullOrWhiteSpace(_path) && Directory.Exists(_path))
            {
                files = Directory.EnumerateFiles(_path, "*.txt", SearchOption.TopDirectoryOnly);
            }
            else
            {
                noteBrowser.DocumentText = WrapHtml("<i>(Path not found)</i>");
                return;
            }

            // Aggregate text (or just the single file)
            string text = string.Join("\n\n", files.Select(f => {
                try { return File.ReadAllText(f); } catch { return string.Empty; }
            }));

            // Header note (rendered as HTML)
            var gameNoteHtml = ParseTopGameNoteHtml(text);
            noteBrowser.DocumentText = WrapHtml(string.IsNullOrWhiteSpace(gameNoteHtml) ? "<i>(No note)</i>" : gameNoteHtml);

            // Game ID / Hash table
            var idHashTable = ParseIdHash(text);
            gridIds.DataSource = idHashTable;
            EnsureIdGridColumns();

            // Credits
            var creditsCounts = ParseCredits(text);
            PopulateCredits(creditsCounts);
        }

        // Overload for counts-only maps
        private void PopulateCredits(Dictionary<string, int> counts)
        {
            listCredits.BeginUpdate();
            listCredits.Items.Clear();
            foreach (var kvp in counts.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var count = kvp.Value;
                var lvi = new ListViewItem(kvp.Key);
                lvi.SubItems.Add(count.ToString());
                lvi.SubItems.Add(string.Empty); // no role info available
                listCredits.Items.Add(lvi);
            }
            // Default sort: by Count descending
            listCredits.ListViewItemSorter = new ListViewItemComparer(1, false);
            listCredits.Tag = Tuple.Create(1, false);
            listCredits.Sort();
            listCredits.EndUpdate();
        }
    }
}
