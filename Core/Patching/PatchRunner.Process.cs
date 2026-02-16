using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMPCodeDatabase.Patching
{
    /// <summary>
    /// Runs the patch tool as a separate process and streams output into an optional log sink.
    /// </summary>
    public sealed class PatchRunnerProcess : IPatchRunner
    {
        private readonly string _tool;

        public PatchRunnerProcess(string toolPath)
        {
            ArgumentNullException.ThrowIfNull(toolPath);
            _tool = toolPath;
        }

        public Task RunAsync(string patchFilePath, int cheatCount, IWin32Window owner) =>
            RunAsync(patchFilePath, cheatCount, owner, null, CancellationToken.None);

        public async Task RunAsync(
            string patchFilePath,
            int cheatCount,
            IWin32Window owner,
            IPatchLogSink? log,
            CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _tool,
                Arguments = $"--patch \"{patchFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            log?.WriteLine($"[cmd]  {_tool} {psi.Arguments}");
            log?.WriteLine($"[patch] {patchFilePath}");
            log?.WriteLine($"[count] {cheatCount}");

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var tcs = new TaskCompletionSource<int>();

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    log?.WriteLine("[out] " + e.Data);
            };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    log?.WriteLine("[err] " + e.Data);
            };

            proc.Exited += (_, _) => tcs.TrySetResult(proc.ExitCode);

            try
            {
                if (!proc.Start())
                    throw new InvalidOperationException("Failed to start patch process.");

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                using var reg = ct.Register(() =>
                {
                    try
                    {
                        if (!proc.HasExited)
                            proc.Kill(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                });

                var code = await tcs.Task.ConfigureAwait(true);
                log?.WriteLine($"[exit] {code}");

                if (code != 0)
                {
                    MessageBox.Show(
                        owner,
                        $"Patch tool exited with code {code}.",
                        "Patch failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                log?.WriteLine("[exception] " + ex);

                MessageBox.Show(
                    owner,
                    $"Could not launch patch tool:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
