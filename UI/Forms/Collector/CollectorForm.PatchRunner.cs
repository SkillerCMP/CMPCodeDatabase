// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.PatchRunner.cs
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private sealed class PatcherResult
        {
            public int ExitCode { get; set; }
            public string StdOut { get; set; } = string.Empty;
            public string StdErr { get; set; } = string.Empty;
        }

        private Task<PatcherResult> LaunchPatcherAsync(string exePath, string savepatchPath, string selection, string dataPath)
        {
            return Task.Run(() =>
            {
                var pr = new PatcherResult();
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    psi.ArgumentList.Add(savepatchPath);
                    psi.ArgumentList.Add(selection);
                    psi.ArgumentList.Add(dataPath);

                    using var proc = Process.Start(psi);
                    if (proc == null)
                    {
                        pr.StdErr = "Failed to start patcher process.";
                        pr.ExitCode = -1;
                        return pr;
                    }

                    var stdout = new StringBuilder();
                    var stderr = new StringBuilder();

                    proc.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                    proc.ErrorDataReceived  += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    // wait up to 5 minutes; adjust if needed
                    if (!proc.WaitForExit(5 * 60 * 1000))
                    {
                        try { proc.Kill(true); } catch {}
                        pr.StdErr = "Timeout waiting for patcher to complete.";
                        pr.ExitCode = -2;
                        return pr;
                    }

                    pr.ExitCode = proc.ExitCode;
                    pr.StdOut = stdout.ToString().Trim();
                    pr.StdErr = stderr.ToString().Trim();
                    return pr;
                }
                catch (Exception ex)
                {
                    pr.StdErr = ex.ToString();
                    pr.ExitCode = -3;
                    return pr;
                }
            });
        }
    }
}
