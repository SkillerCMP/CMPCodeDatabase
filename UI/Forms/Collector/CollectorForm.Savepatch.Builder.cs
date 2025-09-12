// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.Savepatch.Builder.cs
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
    public partial class CollectorForm : Form
    {
        private string BuildTempSavepatch(IReadOnlyList<KeyValuePair<string,string>> entries, out string contentOut)
        {
            var rxFile = new Regex(@"^\s*\^4\s*=\s*FILE\s*:\s*(.+)\s*$", RegexOptions.IgnoreCase);
            var rxHash = new Regex(@"^\s*\^1\s*=\s*Hash\s*:\s*(.+)\s*$", RegexOptions.IgnoreCase);
            var rxGame = new Regex(@"^\s*\^2\s*=\s*GameID\s*:\s*(.+)\s*$", RegexOptions.IgnoreCase);

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
                var keptLines = new List<string>();

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
                            var g = mG.Groups[1].Value.Trim();
                            if (seenGameIds.Add(g)) gameIds.AddLast(g);
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
foreach (var h in hashes) sb.AppendLine("^1 = Hash: " + h);
            foreach (var g in gameIds) sb.AppendLine("^2 = GameID: " + g);
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
