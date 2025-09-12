// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.PatchEvents.cs
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private async void RunPatch(bool onlyChecked)
        {
            var entries = CollectEntries(onlyChecked);
            if (entries.Count == 0)
            {
                MessageBox.Show(this, "No entries to patch.", "Patch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string content;
            string savepatchPath = BuildTempSavepatch(entries, out content);

            // Build selection string "1-<totalBlocks>"
            string selection = $"1-{entries.Count}";

            // Prefer the small drop bar's selected path; fallback to callback
            string? dataPath = !string.IsNullOrWhiteSpace(DataFilePath) ? DataFilePath : null;
            if (string.IsNullOrWhiteSpace(dataPath))
            {
                try { dataPath = ResolveDataFilePath?.Invoke(); } catch { }
            }

            // Ensure we have a patcher exe path
            string exePath = PatchProgramExePath;
            try { var __db = TryReadDatabaseNameFromSavepatch(savepatchPath); if (!string.IsNullOrWhiteSpace(__db)) { exePath = DbCfg.ResolvePatcherPath(__db); PatchProgramExePath = exePath; try { UpdatePatcherStatus(exePath); } catch { } } } catch { }
            if (string.IsNullOrWhiteSpace(exePath))
            {
                string db = string.Empty;
                try { foreach (Form f in Application.OpenForms) if (f is MainForm mf) { db = mf.CurrentDatabaseName; break; } } catch {}
                exePath = CMPCodeDatabase.DbCfg.ResolvePatcherPath(db);
            }
            if (!File.Exists(exePath))
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Locate your Patch Program (Patcher.exe)",
                    Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
                    Multiselect = false,
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    exePath = ofd.FileName;
                    PatchProgramExePath = exePath; // remember for this session
                }
                else
                {
                    MessageBox.Show(this, "Patch Program not selected. Aborting.", "Patch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Raise event so host can run its own logic too (optional)
            var evt = new PatchRequestEventArgs(onlyChecked, entries, savepatchPath, content, selection, dataPath);
            PatchRunRequested?.Invoke(this, evt);

            // Launch patcher with 3 args: <savepatchPath> <selection> <dataFilePath or empty>
            var result = await LaunchPatcherAsync(exePath, savepatchPath, selection, dataPath ?? string.Empty);

            // Show concise result
            var sb = new StringBuilder();
            sb.AppendLine($"Args: {Path.GetFileName(savepatchPath)} {selection} {Path.GetFileName(dataPath ?? string.Empty)}");
            sb.AppendLine($"Exit: {result.ExitCode}");
            if (!string.IsNullOrWhiteSpace(result.StdOut))
            {
                sb.AppendLine();
                sb.AppendLine("[stdout]");
                sb.AppendLine(result.StdOut.Length > 2000 ? result.StdOut.Substring(0, 2000) + "..." : result.StdOut);
            }
            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                sb.AppendLine();
                sb.AppendLine("[stderr]");
                sb.AppendLine(result.StdErr.Length > 2000 ? result.StdErr.Substring(0, 2000) + "..." : result.StdErr);
            }

            MessageBox.Show(this, sb.ToString(), "Patch Result", MessageBoxButtons.OK,
                result.ExitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private async void PreviewPatch(bool onlyChecked)
        {
            var entries = CollectEntries(onlyChecked);
            if (entries.Count == 0)
            {
                MessageBox.Show(this, "No entries to preview.", "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string content;
            string path = BuildTempSavepatch(entries, out content);

            string selection = $"1-{entries.Count}";
            string? dataPath = !string.IsNullOrWhiteSpace(DataFilePath) ? DataFilePath : null;
            if (string.IsNullOrWhiteSpace(dataPath))
            {
                try { dataPath = ResolveDataFilePath?.Invoke(); } catch { }
            }

            PatchPreviewRequested?.Invoke(this, new PatchRequestEventArgs(onlyChecked, entries, path, content, selection, dataPath));

            // Built-in preview window shows the actual .savepatch content
            using var dlg = new Form { Text = "Preview (.savepatch)", StartPosition = FormStartPosition.CenterParent, Width = 820, Height = 640 };
            var tb = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, ReadOnly = true, WordWrap = false };
            tb.Text = content;
            dlg.Controls.Add(tb);
            dlg.ShowDialog(this);
        }
    }
}
