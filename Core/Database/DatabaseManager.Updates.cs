// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Database/DatabaseManager.Updates.cs
// Purpose: Delta-update download flow for DatabaseManager.
// Notes:
//  • Split from DatabaseManager.cs during cleanup pass 16.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Diagnostics;
using CMPCodeDatabase.UI.Dialogs;

namespace CMPCodeDatabase;

public static partial class DatabaseManager
{
    /// <summary>
    /// Download only files that are missing or changed for the selected databases (delta update).
    /// This is intended for the "Check for Database Updates…" flow.
    /// </summary>
    public static async Task DownloadDatabaseUpdatesAsync(IWin32Window owner, IReadOnlyList<ManifestDatabase> databases)
    {
        if (databases.Count == 0) return;

        var root = GetLocalDatabaseRoot();
        Directory.CreateDirectory(root);

        using var progress = new DatabaseProgressDialog();
        using var cts = new CancellationTokenSource();

        progress.CancelRequested += () =>
        {
            try { cts.Cancel(); } catch (Exception ex) { SafeLog.Write("DatabaseManager.Cancel", ex); }
        };

        progress.Show(owner);

        try
        {
            foreach (var db in databases)
            {
                await DownloadDatabaseDeltaAsync(db, root, progress, cts.Token).ConfigureAwait(true);
            }

            progress.SetDone("Update complete.");
            MessageBox.Show(owner, "Database update complete.", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            progress.SetDone("Cancelled.");
            MessageBox.Show(owner, "Database update cancelled.", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            progress.SetDone("Update failed.");
            MessageBox.Show(owner, "Database update failed:\n\n" + ex.Message, "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progress.Close();
        }
    }

    private static bool NeedsDownload(string dbDir, ManifestDatabase db, ManifestFile file)
    {
        var filePath = file.Path ?? "";
        if (string.IsNullOrWhiteSpace(filePath)) return true;

        var rel = SanitizeRelativePath(filePath);
        var target = System.IO.Path.Combine(dbDir, rel);

        if (!File.Exists(target)) return true;

        // Prefer SHA1 if available (quick pre-check by size first).
        if (!string.IsNullOrWhiteSpace(file.Sha1))
        {
            if (file.Size > 0)
            {
                var localSize = new FileInfo(target).Length;
                if (localSize != file.Size) return true;
            }

            var sha1 = ComputeSha1Hex(target);
            return !sha1.Equals(file.Sha1, StringComparison.OrdinalIgnoreCase);
        }

        // Fallback: size compare
        if (file.Size > 0)
        {
            var size = new FileInfo(target).Length;
            return size != file.Size;
        }

        // No reliable metadata; treat as "up to date"
        return false;
    }

    private static async Task DownloadDatabaseDeltaAsync(ManifestDatabase db, string root, DatabaseProgressDialog progress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(db.Name)) return;

        var dbDir = System.IO.Path.Combine(root, SanitizeFolderName(db.Name));
        Directory.CreateDirectory(dbDir);

        var candidates = db.Files ?? new List<ManifestFile>();
        if (candidates.Count == 0) return;

        // Filter to only missing/changed files.
        var toDownload = candidates.Where(f => NeedsDownload(dbDir, db, f)).ToList();
        if (toDownload.Count == 0)
        {
            progress.SetProgress(db.Name, 0, 0, "(up to date)");
            return;
        }

        var total = toDownload.Count;

        // Download multiple files at a time for speed (I/O bound).
        var maxParallel = Math.Clamp(Environment.ProcessorCount, 2, 8);
        using var sem = new SemaphoreSlim(maxParallel, maxParallel);

        var done = 0;
        long lastProgressTicksUtc = 0;

        async Task DownloadOneAsync(ManifestFile file)
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ct.ThrowIfCancellationRequested();

                var filePath = file.Path ?? "";
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new InvalidOperationException($"Manifest contains an empty file path for database '{db.Name}'.");

                if (ShouldReportProgress(ref lastProgressTicksUtc))
                    progress.SetProgress(db.Name, Math.Max(0, Volatile.Read(ref done)), total, filePath);

                var rel = SanitizeRelativePath(filePath);
                var target = System.IO.Path.Combine(dbDir, rel);

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);

                var tmp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await DownloadFileToAsync(file.Url, tmp, ct).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(file.Sha1))
                    {
                        var sha1 = ComputeSha1Hex(tmp);
                        if (!sha1.Equals(file.Sha1, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException($"SHA1 mismatch for {db.Name}/{filePath}\nExpected: {file.Sha1}\nActual:   {sha1}");
                    }

                    if (File.Exists(target)) File.Delete(target);
                    File.Move(tmp, target);
                }
                finally
                {
                    if (File.Exists(tmp))
                    {
                        try { File.Delete(tmp); } catch (Exception ex) { SafeLog.Write("DatabaseManager.TempCleanup", ex, tmp); }
                    }
                }

                var nowDone = Interlocked.Increment(ref done);
                progress.SetProgress(db.Name, nowDone, total, filePath);
            }
            finally
            {
                sem.Release();
            }
        }

        var tasks = toDownload.Select(DownloadOneAsync).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
