// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.SwUserCheats.Gamelist.cs
// Purpose: Save Wizard gamelist path resolution and TEMP discovery helpers.
// Notes:
//  • Split from CollectorForm.SwUserCheats.Export.cs during cleanup pass 5.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        // ────────────────────────────────────────────────────────────────────
        // Save Wizard gamelist auto-discovery
        // ────────────────────────────────────────────────────────────────────

        private static string GetToolsDir()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "Tools");
        }

        private static string GetDefaultGamelistPath()
        {
            // Default location (no extension) because some SW dumps name it just "gamelist".
            return Path.Combine(GetToolsDir(), "gamelist");
        }

        private static string GetDefaultGamelistXmlPath()
        {
            // Back-compat / alternate naming.
            return Path.Combine(GetToolsDir(), "gamelist.xml");
        }

        /// <summary>
        /// Attempts to resolve a usable gamelist path.
        /// If none is configured, scans %TEMP%/%TMP% for "gamelist" or gamelist*.xml, copies it to Files\Tools\gamelist,
        /// and sets settings.SwGameListPath to that default.
        /// If already configured to Files\Tools\gamelist (or gamelist.xml), does a quick scan of %TEMP% for a newer one and updates the copy.
        /// </summary>
        private static bool TryResolveOrAcquireGamelistXml(AppSettings settings, out string? gamelistPath, out string? note)
        {
            gamelistPath = null;
            note = null;

            Directory.CreateDirectory(GetToolsDir());
            var toolsGamelist = GetDefaultGamelistPath();
            var toolsGamelistXml = GetDefaultGamelistXmlPath();

            // If settings points somewhere else but exists, copy it into Files\Tools and switch to default.
            var configured = settings.SwGameListPath;
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            {
                try
                {
                    gamelistPath = CopyGamelistIntoTools(configured!, settings);
                    note = "Copied configured gamelist into Files\\Tools\\.";
                    return true;
                }
                catch { /* fall through */ }
            }

            // If Files\Tools\gamelist (or gamelist.xml) already exists, use it.
            if (File.Exists(toolsGamelist) || File.Exists(toolsGamelistXml))
            {
                gamelistPath = File.Exists(toolsGamelist) ? toolsGamelist : toolsGamelistXml;

                // Quick check in %TEMP%: if a newer gamelist exists, refresh our copy.
                var tmpCandidate = FindBestGamelistInTemp(gamelistPath);
                if (tmpCandidate != null)
                {
                    try
                    {
                        gamelistPath = CopyGamelistIntoTools(tmpCandidate!, settings);
                        note = "Updated Files\\Tools\\gamelist from a newer TEMP copy.";
                    }
                    catch { }
                }
                return true;
            }

            // Otherwise: scan TEMP/TMP and copy best candidate into Files\Tools
            var candidate = FindBestGamelistInTemp(existingToolsPath: null);
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                try
                {
                    gamelistPath = CopyGamelistIntoTools(candidate!, settings);
                    note = "Acquired gamelist from TEMP and stored it in Files\\Tools\\.";
                    return true;
                }
                catch { /* ignore */ }
            }

            return false;
        }

        /// <summary>Copies src gamelist (or gamelist.xml) into Files\Tools\gamelist, updates settings path, and returns the destination.</summary>
        private static string CopyGamelistIntoTools(string srcPath, AppSettings settings)
        {
            Directory.CreateDirectory(GetToolsDir());
            var dest = GetDefaultGamelistPath();

            File.Copy(srcPath, dest, overwrite: true);

            // Preserve timestamp so "newer" checks work sensibly
            try { File.SetLastWriteTimeUtc(dest, File.GetLastWriteTimeUtc(srcPath)); } catch { }

            settings.SwGameListPath = dest;
            return dest;
        }

        /// <summary>
        /// Finds the best candidate gamelist*.xml under %TEMP%/%TMP%.
        /// If existingToolsPath is provided, only returns a candidate that appears newer than that file.
        /// Search is intentionally capped to stay "quick".
        /// </summary>
        private static string? FindBestGamelistInTemp(string? existingToolsPath)
        {
            var tempRoot = Path.GetTempPath();
            DateTime existingUtc = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(existingToolsPath) && File.Exists(existingToolsPath))
            {
                try { existingUtc = File.GetLastWriteTimeUtc(existingToolsPath); } catch { }
            }

            string? best = null;
            DateTime bestUtc = DateTime.MinValue;
            long bestLen = -1;

            // Some SW dumps name it "gamelist" (no extension); others use "gamelist.xml".
            foreach (var pattern in new[] { "gamelist", "gamelist*.xml" })
            {
                foreach (var file in EnumerateFilesCapped(tempRoot, pattern, maxDepth: 6, maxDirs: 2000, maxFiles: pattern.EndsWith(".xml") ? 4000 : 2000))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        if (!fi.Exists) continue;

                        // Filter out tiny junk copies; real gamelist is typically large.
                        if (fi.Length < 64 * 1024) continue;

                        var utc = fi.LastWriteTimeUtc;
                        if (utc < existingUtc) continue;

                        // Prefer newest; tie-break on size.
                        if (best == null || utc > bestUtc || (utc == bestUtc && fi.Length > bestLen))
                        {
                            best = fi.FullName;
                            bestUtc = utc;
                            bestLen = fi.Length;
                        }
                    }
                    catch { }
                }
            }

            return best;
        }

        private static IEnumerable<string> EnumerateFilesCapped(string root, string pattern, int maxDepth, int maxDirs, int maxFiles)
        {
            var stack = new Stack<(string dir, int depth)>();
            stack.Push((root, 0));

            int dirCount = 0;
            int fileCount = 0;

            while (stack.Count > 0 && dirCount < maxDirs && fileCount < maxFiles)
            {
                var (dir, depth) = stack.Pop();
                dirCount++;

                IEnumerable<string>? files = null;
                try { files = Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly); } catch { }

                if (files != null)
                {
                    foreach (var f in files)
                    {
                        yield return f;
                        fileCount++;
                        if (fileCount >= maxFiles) yield break;
                    }
                }

                if (depth >= maxDepth) continue;

                IEnumerable<string>? dirs = null;
                try { dirs = Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly); } catch { }
                if (dirs != null)
                {
                    foreach (var d in dirs)
                    {
                        stack.Push((d, depth + 1));
                        if (stack.Count > maxDirs * 2) break; // soft cap to avoid runaway
                    }
                }
            }
        }

    }
}
