// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Settings/PatcherPathResolver.cs
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

namespace CMPCodeDatabase.Core.Settings
{
    /// <summary>
    /// Resolves the patcher executable path. Default is %ROOT%\Files\Tools\patcher.exe.
    /// </summary>
    public static class PatcherPathResolver
    {
        private static readonly string[] Candidates = new[]
        {
            "patcher.exe", "Patcher.exe", "ApolloPatcher.exe", "patch.exe"
        };

        public static string GetDefaultPath()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(root, "Files", "Tools", "patcher.exe");
        }

        public static string? Resolve(AppSettings s)
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string defaultPath = GetDefaultPath();

            // 1) Configured path
            var configuredRaw = (s.PatchToolPath ?? string.Empty).Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(configuredRaw))
            {
                var configured = Environment.ExpandEnvironmentVariables(configuredRaw);

                // Absolute file
                if (File.Exists(configured))
                    return configured;

                // Relative to root
                if (!Path.IsPathRooted(configured))
                {
                    var combined = Path.GetFullPath(Path.Combine(root, configured));
                    if (File.Exists(combined))
                        return combined;
                }

                // If it's a directory, probe common names inside it
                if (Directory.Exists(configured))
                {
                    foreach (var name in Candidates)
                    {
                        var probe = Path.Combine(configured, name);
                        if (File.Exists(probe)) return probe;
                    }
                }
            }

            // 2) Default location
            if (File.Exists(defaultPath))
                return defaultPath;

            // 3) Defensive probes in %ROOT%\Files\Tools
            var toolsDir = Path.Combine(root, "Files", "Tools");
            foreach (var name in Candidates)
            {
                var probe = Path.Combine(toolsDir, name);
                if (File.Exists(probe)) return probe;
            }

            return null;
        }
    }
}