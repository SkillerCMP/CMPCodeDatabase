// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Database/DatabaseManager.IO.cs
// Purpose: Low-level file/download/path helpers for DatabaseManager.
// Notes:
//  • Split from DatabaseManager.cs during cleanup pass 15.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CMPCodeDatabase;

public static partial class DatabaseManager
{
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
