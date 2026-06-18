using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CMPCodeDatabase;

public static partial class DatabaseManager
{
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
}
