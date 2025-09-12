// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Settings/ToolPathResolver.cs
// Purpose: Helpers to resolve filesystem paths and database roots.
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

namespace CMPCodeDatabase.Core.Settings
{
    public static class ToolPathResolver
    {
        public static string AppRoot =>
            AppDomain.CurrentDomain.BaseDirectory?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            ?? Environment.CurrentDirectory;

        public static string DefaultToolsDir =>
            Path.Combine(AppRoot, "Files", "Tools");

        public static string? ExpandRoot(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            return path.Replace("%root%", AppRoot, StringComparison.OrdinalIgnoreCase)
                       .Replace("%ROOT%", AppRoot, StringComparison.OrdinalIgnoreCase);
        }

        public static string? FindInDefaultTools(string[]? candidateExeNames = null)
        {
            var dir = DefaultToolsDir;
            if (!Directory.Exists(dir)) return null;

            var candidates = (candidateExeNames is { Length: > 0 })
                ? candidateExeNames
                : new[] { "ApolloPatcher.exe", "ApolloCli.exe", "apollopatcher.exe", "apollocli.exe", "patch.exe" };

            try
            {
                var files = Directory.EnumerateFiles(dir, "*.exe", SearchOption.AllDirectories);
                foreach (var exename in candidates)
                {
                    var hit = files.FirstOrDefault(f => string.Equals(Path.GetFileName(f), exename, StringComparison.OrdinalIgnoreCase));
                    if (hit != null) return hit;
                }
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            return null;
        }

        public static string? ResolvePatchToolPath(string? configuredPath, string[]? candidateExeNames = null)
        {
            var expanded = ExpandRoot(configuredPath);
            if (!string.IsNullOrWhiteSpace(expanded) && File.Exists(expanded))
                return expanded;

            var fromDefault = FindInDefaultTools(candidateExeNames);
            if (!string.IsNullOrWhiteSpace(fromDefault) && File.Exists(fromDefault))
                return fromDefault;

            return null;
        }
    }
}
