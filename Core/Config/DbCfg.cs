// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Config/DbCfg.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    internal static class DbCfg
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, string> _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _mapNormalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static DateTime _lastLoad = DateTime.MinValue;
        private static readonly string _cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "Database", "DBCFG.txt");

        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        private static void EnsureLoaded()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_cfgPath))
                    {
                        _map.Clear();
                        _mapNormalized.Clear();
                        _lastLoad = DateTime.UtcNow;
                        return;
                    }

                    var ts = File.GetLastWriteTimeUtc(_cfgPath);
                    if (ts <= _lastLoad && _map.Count > 0) return;

                    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var mapN = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var raw in File.ReadAllLines(_cfgPath))
                    {
                        if (string.IsNullOrWhiteSpace(raw)) continue;
                        var line = raw.Trim();
                        if (line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("//")) continue;

                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = line.Substring(0, eq).Trim();
                        string val = line.Substring(eq + 1).Trim().Trim('\"');
                        if (key.Length == 0 || val.Length == 0) continue;

                        map[key] = val;
                        mapN[Normalize(key)] = val;
                    }

                    _map = map;
                    _mapNormalized = mapN;
                    _lastLoad = ts;
                }
                catch
                {
                    _map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _mapNormalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    _lastLoad = DateTime.UtcNow;
                }
            }
        }

        internal static string? GetConfiguredExeName(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName)) return null;
            EnsureLoaded();

            if (_map.TryGetValue(databaseName.Trim(), out var exe0))
                return exe0;

            var norm = Normalize(databaseName);
            if (_mapNormalized.TryGetValue(norm, out var exe1))
                return exe1;

            foreach (var kv in _mapNormalized)
                if (norm.Contains(kv.Key)) return kv.Value;

            return null;
        }

        private static string? GetToolPath(string exeName)
        {
            if (string.IsNullOrWhiteSpace(exeName)) return null;

            // Expand %ROOT% or absolute
            var expanded = ToolPathResolver.ExpandRoot(exeName);
            if (!string.IsNullOrWhiteSpace(expanded) && File.Exists(expanded))
                return expanded;

            string tools = ToolPathResolver.DefaultToolsDir;
            if (!Directory.Exists(tools)) return null;

            // If a bare file name, compose then search
            var composed = Path.Combine(tools, exeName);
            if (File.Exists(composed)) return composed;

            try
            {
                var hit = Directory.EnumerateFiles(tools, "*.exe", SearchOption.TopDirectoryOnly)
                                   .FirstOrDefault(f => string.Equals(Path.GetFileName(f), exeName, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            catch { }
            return null;
        }

        internal static string ResolvePatcherPath(string databaseName, string defaultExeName = "patcher.exe")
        {
            try
            {
                var cfgExe = GetConfiguredExeName(databaseName);
                var candidate = !string.IsNullOrEmpty(cfgExe) ? GetToolPath(cfgExe!) : null;
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                    return candidate;
            }
            catch { }

            var def = GetToolPath(defaultExeName);
            return def ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "Tools", defaultExeName);
        }

        /// <summary>
        /// Returns null if the resolved path would be the default.
        /// </summary>
        internal static string? ResolvePatcherPathSmart(string databaseName, string defaultExeName = "patcher.exe")
        {
            try
            {
                var def = ResolvePatcherPath(string.Empty, defaultExeName);
                var cfgExe = GetConfiguredExeName(databaseName);
                var candidate = !string.IsNullOrEmpty(cfgExe) ? GetToolPath(cfgExe!) : null;
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                {
                    if (!string.Equals(Path.GetFileName(candidate), Path.GetFileName(def), StringComparison.OrdinalIgnoreCase))
                        return candidate;
                }
            }
            catch { }
            return null;
        }
    }
}
