using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CMPCodeDatabase.UI.Dialogs;

namespace CMPCodeDatabase;

public static class DatabaseManager
{
    // Remote manifest location (default). You can swap this later to a different host without code changes.
    public const string DefaultManifestUrl = "https://drive.google.com/uc?export=download&id=1UGa5b3AnhWMSA7vNAhWB8qXTJWZkrOiK";

    private static readonly HttpClient _http = new(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });

    public sealed class ManifestRoot
{
    public int SchemaVersion { get; set; }
    public string? GeneratedUtc { get; set; }

    // Supports either:
    //  - "generator": "SomeTool 1.2.3"
    //  - "generator": { "name": "SomeTool", "version": "1.2.3" }
    [JsonConverter(typeof(ManifestGeneratorConverter))]
    public ManifestGenerator? Generator { get; set; }

    public List<ManifestDatabase> Databases { get; set; } = new();
}

public sealed class ManifestGenerator
{
    public string? Name { get; set; }
    public string? Version { get; set; }
}

private sealed class ManifestGeneratorConverter : JsonConverter<ManifestGenerator?>
{
    public override ManifestGenerator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            // Best-effort parse: treat the last token as version if it looks version-ish.
            var parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return new ManifestGenerator { Name = parts[0] };

            var last = parts[^1];
            var name = string.Join(" ", parts[..^1]);
            return new ManifestGenerator { Name = name, Version = last };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var obj = doc.RootElement;

            var gen = new ManifestGenerator();

            if (obj.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                gen.Name = n.GetString();

            if (obj.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
                gen.Version = v.GetString();

            // If it was an object but empty/unknown, treat as null.
            if (string.IsNullOrWhiteSpace(gen.Name) && string.IsNullOrWhiteSpace(gen.Version))
                return null;

            return gen;
        }

        throw new JsonException("Invalid generator value.");
    }

    public override void Write(Utf8JsonWriter writer, ManifestGenerator? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        if (!string.IsNullOrWhiteSpace(value.Name))
            writer.WriteString("name", value.Name);
        if (!string.IsNullOrWhiteSpace(value.Version))
            writer.WriteString("version", value.Version);
        writer.WriteEndObject();
    }
}

public sealed class ManifestDatabase
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int FileCount { get; set; }
        public ManifestDownload? Download { get; set; }
        public List<ManifestFile> Files { get; set; } = new();
    }

    public sealed class ManifestDownload
    {
        public string Type { get; set; } = "";
        public string? Url { get; set; }
    }

    public sealed class ManifestFile
    {
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public string Sha1 { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public sealed record UpdateInfo(string DatabaseName, int ChangedFileCount, IReadOnlyList<string> ChangedFiles);

    public static string GetLocalDatabaseRoot()
        => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "Database");

    public static async Task<ManifestRoot> GetRemoteManifestAsync(CancellationToken ct)
    {
        using var resp = await _http.GetAsync(DefaultManifestUrl, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var root = JsonSerializer.Deserialize<ManifestRoot>(json, options);
        if (root is null) throw new InvalidOperationException("Manifest.json could not be parsed.");

        // Normalize null list
        root.Databases ??= new List<ManifestDatabase>();
        foreach (var db in root.Databases)
        {
            db.Files ??= new List<ManifestFile>();
            db.Name ??= "";
            db.Type ??= "";
        }

        return root;
    }

    public static async Task DownloadAllDatabasesAsync(IWin32Window owner)
    {
        var manifest = await GetRemoteManifestAsync(CancellationToken.None);
        await DownloadDatabasesAsync(owner, manifest.Databases.ToArray(), promptBeforeDownloading: false);
    }

    public static async Task DownloadDatabasesAsync(IWin32Window owner, IReadOnlyList<ManifestDatabase> databases, bool promptBeforeDownloading)
    {
        if (databases.Count == 0) return;

        var root = GetLocalDatabaseRoot();
        Directory.CreateDirectory(root);

        if (promptBeforeDownloading)
        {
            var msg = "Download the following databases?\n\n" + string.Join("\n", databases.Select(d => "- " + d.Name));
            var confirm = MessageBox.Show(owner, msg, "Download Databases", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
        }

        using var progress = new DatabaseProgressDialog();
        using var cts = new CancellationTokenSource();

        progress.CancelRequested += () =>
        {
            try { cts.Cancel(); } catch { /* ignore */ }
        };

        progress.Show(owner);

        try
        {
            foreach (var db in databases)
            {
                await DownloadDatabaseAsync(db, root, progress, cts.Token).ConfigureAwait(true);
            }

            progress.SetDone("Download complete.");
            MessageBox.Show(owner, "Database download complete.", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            progress.SetDone("Cancelled.");
            MessageBox.Show(owner, "Database download cancelled.", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            progress.SetDone("Download failed.");
            MessageBox.Show(owner, "Database download failed:\n\n" + ex.Message, "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progress.Close();
        }
    }

    
    private static async Task DownloadDatabaseAsync(ManifestDatabase db, string root, DatabaseProgressDialog progress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(db.Name)) return;

        var dbDir = System.IO.Path.Combine(root, SanitizeFolderName(db.Name));
        Directory.CreateDirectory(dbDir);

        var total = db.Files.Count;
        if (total <= 0) return;

        // Download multiple files at a time for speed (I/O bound).
        // Limit concurrency to avoid hammering the network / Drive throttling.
        var maxParallel = Math.Clamp(Environment.ProcessorCount, 2, 8);
        using var sem = new SemaphoreSlim(maxParallel, maxParallel);

        var done = 0;

        async Task DownloadOneAsync(ManifestFile file)
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ct.ThrowIfCancellationRequested();

                var filePath = file.Path ?? "";
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new InvalidOperationException($"Manifest contains an empty file path for database '{db.Name}'.");

                // Optional: show "working on" this file (doesn't increment done yet)
                progress.SetProgress(db.Name, Math.Max(0, Volatile.Read(ref done)), total, filePath);

                var rel = SanitizeRelativePath(filePath);
                var target = System.IO.Path.Combine(dbDir, rel);

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);

                // Download to unique temp, verify, then replace
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
                    // Clean temp if something went wrong before the move.
                    if (File.Exists(tmp))
                    {
                        try { File.Delete(tmp); } catch { /* ignore */ }
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

        var tasks = db.Files.Select(DownloadOneAsync).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }



    
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
            try { cts.Cancel(); } catch { /* ignore */ }
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

        async Task DownloadOneAsync(ManifestFile file)
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ct.ThrowIfCancellationRequested();

                var filePath = file.Path ?? "";
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new InvalidOperationException($"Manifest contains an empty file path for database '{db.Name}'.");

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
                        try { File.Delete(tmp); } catch { /* ignore */ }
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

public static async Task<UpdateInfo[]> CheckForUpdatesAsync(CancellationToken ct)
    {
        var remote = await GetRemoteManifestAsync(ct).ConfigureAwait(false);
        var localRoot = GetLocalDatabaseRoot();
        Directory.CreateDirectory(localRoot);

        var results = new List<UpdateInfo>();

        foreach (var db in remote.Databases)
        {
            var dbDir = System.IO.Path.Combine(localRoot, SanitizeFolderName(db.Name));
            if (!Directory.Exists(dbDir)) continue; // Only compare installed DBs

            var changedFiles = new List<string>();

            foreach (var file in db.Files)
            {
                var rel = SanitizeRelativePath(file.Path);
                var localPath = System.IO.Path.Combine(dbDir, rel);

                if (!File.Exists(localPath))
                {
                    changedFiles.Add(rel + " (missing)");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(file.Sha1))
                {
                    var sha1 = ComputeSha1Hex(localPath);
                    if (!sha1.Equals(file.Sha1, StringComparison.OrdinalIgnoreCase))
                        changedFiles.Add(rel);
                }
                else
                {
                    // No SHA1 -> compare size
                    var size = new FileInfo(localPath).Length;
                    if (size != file.Size) changedFiles.Add(rel);
                }
            }

            if (changedFiles.Count > 0)
                results.Add(new UpdateInfo(db.Name, changedFiles.Count, changedFiles));
        }

        return results.ToArray();
    }


public static string BuildLocalManifestJson(string localRoot, bool txtOnly, bool includeFiles)
{
    Directory.CreateDirectory(localRoot);

    var root = new ManifestRoot
    {
        SchemaVersion = 1,
        GeneratedUtc = DateTime.UtcNow.ToString("O"),
        Generator = new ManifestGenerator { Name = "CMPCodeDatabase (local)", Version = "local" },
        Databases = new List<ManifestDatabase>()
    };

    foreach (var dbDir in Directory.EnumerateDirectories(localRoot))
    {
        var name = Path.GetFileName(dbDir);
        if (string.IsNullOrWhiteSpace(name)) continue;

        var db = new ManifestDatabase
        {
            Name = name,
            Type = "local",
            Download = new ManifestDownload { Type = "local", Url = null },
            Files = new List<ManifestFile>()
        };

        var files = Directory.EnumerateFiles(dbDir, "*", SearchOption.AllDirectories);

        foreach (var filePath in files)
        {
            var ext = Path.GetExtension(filePath);
            if (txtOnly && !ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)) continue;

            var rel = Path.GetRelativePath(dbDir, filePath).Replace('\\', '/');
            var fi = new FileInfo(filePath);

            db.Files.Add(new ManifestFile
            {
                Path = rel,
                Size = fi.Length,
                Sha1 = ComputeSha1Hex(filePath),
                Url = ""
            });
        }

        db.FileCount = db.Files.Count;

        if (!includeFiles)
            db.Files.Clear();

        root.Databases.Add(db);
    }

    var options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    return JsonSerializer.Serialize(root, options);
}
    public static string ComputeSha1Hex(string filePath)
    {
        using var sha1 = SHA1.Create();
        using var fs = File.OpenRead(filePath);
        var hash = sha1.ComputeHash(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task DownloadFileToAsync(string url, string targetPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Manifest contained an empty file URL.");

        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var file = File.Create(targetPath);
        await stream.CopyToAsync(file, ct).ConfigureAwait(false);
    }

    private static string SanitizeFolderName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name.Trim();
    }

    private static string SanitizeRelativePath(string rel)
    {
        // Normalize to forward slashes and trim leading/trailing separators.
        rel = rel.Replace('\\', '/').Trim('/');

        // Block path traversal safely (allow filenames containing "..." etc).
        var segments = rel.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s == ".."))
            throw new InvalidOperationException("Invalid manifest path.");

        var parts = segments
            .Select(p => string.Join("_",
                p.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        return Path.Combine(parts);
    }
}