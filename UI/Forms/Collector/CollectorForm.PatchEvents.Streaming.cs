// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.PatchEvents.Streaming.cs
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Streaming variant of RunPatch that writes to the bottom log instead of showing a popup.
    /// This does not remove the original RunPatch; we rewire the run buttons to call this method.
    /// </summary>
    public partial class CollectorForm : Form
    {
        private async void RunPatchStreaming(bool onlyChecked)
        {
            var entries = CollectEntries(onlyChecked);
            if (entries.Count == 0)
            {
                MessageBox.Show(this, "No entries to patch.", "Patch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build selection "1-<n>"
            string selection = $"1-{entries.Count}";

            // Build the savepatch file
            string content;
            string savepatchPath = BuildTempSavepatch(entries, out content);

            // Determine data path (use current DataFilePath or ask the callback)
            string? dataPath = !string.IsNullOrWhiteSpace(DataFilePath) ? DataFilePath : null;
            if (string.IsNullOrWhiteSpace(dataPath))
            {
                try { dataPath = ResolveDataFilePath?.Invoke(); } catch { }
            }

            // Notify listeners (keeps external patch program integration)
            PatchRunRequested?.Invoke(this,
                new PatchRequestEventArgs(onlyChecked, entries, savepatchPath, content, selection, dataPath));

            // Build args: <savepatch> <selection> <dataFile>
            var args = $"{Q(savepatchPath)} {selection} {Q(dataPath ?? string.Empty)}";

            // Ensure we have a patcher exe path (PatcherBoot partial should have set PatchProgramExePath)
            var exePath = PatchProgramExePath;
            try { var __db = TryReadDatabaseNameFromSavepatch(savepatchPath); if (!string.IsNullOrWhiteSpace(__db)) { exePath = DbCfg.ResolvePatcherPath(__db); PatchProgramExePath = exePath; try { UpdatePatcherStatus(exePath); } catch { } } } catch { }
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                string db = string.Empty;
                try { foreach (Form f in Application.OpenForms) if (f is MainForm mf) { db = mf.CurrentDatabaseName; break; } } catch {}
                exePath = CMPCodeDatabase.DbCfg.ResolvePatcherPath(db);
            }
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show(this, "Patch tool not found. Please set it in Settings.", "Patch",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Start fresh and stream output
            ClearLog();
            AppendLog("=== Patch start ===");
            AppendLog($"Args: {Path.GetFileName(savepatchPath)} {selection} {Path.GetFileName(dataPath ?? string.Empty)}");

            var exit = await LaunchPatcherStreamingAsync(exePath, args);

            AppendLog("=== Patch end ===");
            if (exit != 0)
            {
                MessageBox.Show(this, $"Patch finished with exit code {exit}. See log for details.",
                    "Patch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}