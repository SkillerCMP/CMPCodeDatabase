// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.PatchRunner.Streaming.cs
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
using System.Threading.Tasks;

namespace CMPCodeDatabase
{
    public partial class CollectorForm
    {
        private static string Q(string? s) => string.IsNullOrEmpty(s) ? "\"\"" : $"\"{s}\"";

        private async Task<int> LaunchPatcherStreamingAsync(string exePath, string args, string? workingDir = null)
        {
            var tcs = new TaskCompletionSource<int>();

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = workingDir ?? (System.IO.Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) AppendLog(e.Data); };
            proc.ErrorDataReceived  += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) AppendLog(e.Data); };
            proc.Exited += (s, e) => tcs.TrySetResult(proc.ExitCode);

            AppendLog($"> {System.IO.Path.GetFileName(exePath)} {args}");
            if (!proc.Start())
            {
                AppendLog("Failed to start patcher.");
                return -1;
            }
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            var code = await tcs.Task.ConfigureAwait(true);
            AppendLog($"[exit] {code}");
            return code;
        }
    }
}