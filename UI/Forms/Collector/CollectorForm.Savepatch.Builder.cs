// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorControl.Savepatch.Builder.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        private static readonly Regex SavepatchFileHeaderRegex = new(@"^\s*\^4\s*=\s*FILE\s*:\s*(.+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SavepatchHashHeaderRegex = new(@"^\s*\^1\s*=\s*Hash\s*:\s*(.+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SavepatchGameHeaderRegex = new(@"^\s*\^2\s*=\s*GameID\s*:\s*(.+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SavepatchGameIdRegex = new(@"\b[A-Za-z]{4}\d{5}\b", RegexOptions.Compiled);

        private string BuildTempSavepatch(IReadOnlyList<KeyValuePair<string,string>> entries, out string contentOut)
        {
            var rxFile = SavepatchFileHeaderRegex;
            var rxHash = SavepatchHashHeaderRegex;
            var rxGame = SavepatchGameHeaderRegex;

            var files = new LinkedList<string>();
            var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hashes = new LinkedList<string>();
            var seenHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var gameIds = new LinkedList<string>();
            var seenGameIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string NormalizeFile(string s) => s.Trim();

            var sectionBuilder = new StringBuilder();
            bool firstSection = true;

            foreach (var kv in entries)
            {
                var name = kv.Key?.Trim() ?? "PATCH";
                var code = kv.Value ?? string.Empty;
                List<string> keptLines = [];

                using (var sr = new StringReader(code))
                {
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var raw = line;

                        var mF = rxFile.Match(raw);
                        if (mF.Success)
                        {
                            var fileVal = NormalizeFile(mF.Groups[1].Value);
                            if (seenFiles.Add(fileVal)) files.AddLast(fileVal);
                            continue;
                        }
                        var mH = rxHash.Match(raw);
                        if (mH.Success)
                        {
                            var h = mH.Groups[1].Value.Trim();
                            if (seenHashes.Add(h)) hashes.AddLast(h);
                            continue;
                        }
                        var mG = rxGame.Match(raw);
                        if (mG.Success)
                        {
                            // Accept either a single ID or a CSV/whitespace list on one line.
                            foreach (Match m in SavepatchGameIdRegex.Matches(mG.Groups[1].Value))
                            {
                                var id = m.Value.Trim().ToUpperInvariant();
                                if (seenGameIds.Add(id)) gameIds.AddLast(id);
                            }
                            continue;
                        }

                        if (raw.StartsWith("[INFO:", StringComparison.OrdinalIgnoreCase))
                        {
                            keptLines.Add("; " + raw.Trim());
                            continue;
                        }

                        keptLines.Add(raw);
                    }
                }

                var body = Apply64BitHexBlocking(string.Join(Environment.NewLine, keptLines)).TrimEnd();

                if (!firstSection) sectionBuilder.AppendLine();
                firstSection = false;

                sectionBuilder.AppendLine("[" + name + "]");
                sectionBuilder.AppendLine(body);
            }

            var sb = new StringBuilder();
            // Dynamic header with database name
            string __dbName = string.Empty;
            try { foreach (Form f in Application.OpenForms) if (f is MainForm mf) { __dbName = mf.CurrentDatabaseName; break; } } catch { }
            if (!string.IsNullOrWhiteSpace(__dbName))
                sb.AppendLine($";{__dbName} CMP Collector Patch");
            else
                sb.AppendLine(";CMP Collector Patch");
            sb.AppendLine("; Built By CMP Collector");
            if (!string.IsNullOrWhiteSpace(__dbName)) sb.AppendLine($"; Database: {__dbName}");
            // Active game IDs for Save Wizard export context (comment-only; not CMP metadata)
            string? __gameIdsCsv = null;
            try
            {
                foreach (Form f in Application.OpenForms)
                    if (f is MainForm mf) { __gameIdsCsv = mf.CurrentGameIdsCsv; break; }
            }
            catch { /* ignore */ }

            string? __headerGameIds = null;
            if (!string.IsNullOrWhiteSpace(__gameIdsCsv))
                __headerGameIds = __gameIdsCsv!.Trim();
            else if (gameIds.Count > 0)
                __headerGameIds = string.Join(",", gameIds);

            if (!string.IsNullOrWhiteSpace(__headerGameIds))
                sb.AppendLine($"; Game ID: {__headerGameIds}");

            foreach (var h in hashes) sb.AppendLine("^1 = Hash: " + h);
            foreach (var f in files) sb.AppendLine("^4 = FILE: " + f);
            sb.AppendLine();
            sb.Append(sectionBuilder.ToString());

            var content = sb.ToString();
            contentOut = content;

            var dir = Path.Combine(Path.GetTempPath(), "CMPCollector");
            Directory.CreateDirectory(dir);
            string fileName = $"CollectorPatch_TMP.savepatch";
            string outPath = Path.Combine(dir, fileName);
            File.WriteAllText(outPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier:false));
            return outPath;
        }
    }
}
