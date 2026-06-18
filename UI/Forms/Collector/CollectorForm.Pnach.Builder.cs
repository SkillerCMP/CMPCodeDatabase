// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.Pnach.Builder.cs
// Purpose: PCSX2 .pnach text builder and filename guess helpers.
// Notes:
//  • Split from CollectorForm.Pnach.Export.cs during cleanup pass 6.
//  • Behavior/output intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        /// <summary>
        /// Build a minimal but compatible PCSX2 .pnach from collected entries.
        /// Header mirrors the savepatch style with '//' comments. Body emits existing patch= lines,
        /// or converts AAAAAAAA BBBBBBBB pairs into patch=1,EE,AAAAAAAA,extended,BBBBBBBB.
        /// Also tries to include Original File Name, Game Name, HASH, Credits when present in the text.
        /// </summary>
        private string BuildTempPnach(IReadOnlyList<KeyValuePair<string, string>> entries, out string contentOut)
        {
            // Try to glean meta from collected bodies
            var rxFile = PnachMetaFileRegex;
            var rxHash = PnachMetaHashRegex;
            var rxName = PnachMetaNameRegex;
            var rxCredits = PnachCreditsRegex;

            string? topFile = null, topName = null, topHash = null;
            var creditsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in entries)
            {
                // Include sidecar author= metadata in top credits line (if present)
                if (TryGetMeta(kv.Key, out var meta) && !string.IsNullOrWhiteSpace(meta.Author))
                {
                    foreach (var who in SplitPeople(meta.Author))
                        creditsSet.Add(who);
                }

                using var sr = new StringReader(kv.Value ?? string.Empty);
                for (string? line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    var mF = rxFile.Match(line);    if (mF.Success && topFile == null) topFile = mF.Groups[1].Value.Trim();
                    var mH = rxHash.Match(line);    if (mH.Success && topHash == null) topHash = mH.Groups[1].Value.Trim().ToUpperInvariant();
                    var mN = rxName.Match(line);    if (mN.Success && topName == null) topName = mN.Groups[1].Value.Trim();
                    var mC = rxCredits.Match(line); if (mC.Success)
                    {
                        foreach (var who in mC.Groups[1].Value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                            creditsSet.Add(who.Trim());
                    }
                }
            }

            // Build content
            var sb = new StringBuilder();
            sb.AppendLine("// Built by CMP Collector");
            if (!string.IsNullOrWhiteSpace(topFile))
            {
                sb.AppendLine($"// Original File Name: {topFile}");
                sb.AppendLine($"// Game Name: {topName ?? "UNKNOWN"}");
            }
            if (!string.IsNullOrWhiteSpace(topHash))
                sb.AppendLine($"// HASH: {topHash}");
            if (creditsSet.Count > 0)
                sb.AppendLine($"// Credits: {string.Join(", ", creditsSet)}");
            sb.AppendLine();

            // Body: either keep patch= lines or convert pairs
            var rxPatch = PnachPatchLineRegex;
            var rxPair = PnachPairLineRegex;
            foreach (var kv in entries)
            {
                var title = kv.Key ?? "PATCH";
                sb.AppendLine("[" + title + "]");

                // Optional metadata (not shown in Collector UI)
                var author = GetPnachAuthorForEntry(title);
                if (!string.IsNullOrWhiteSpace(author)) sb.AppendLine("author=" + author);

                var desc = GetPnachDescriptionForEntry(title);
                if (!string.IsNullOrWhiteSpace(desc)) sb.AppendLine("description=" + desc);

                using var sr = new StringReader(kv.Value ?? string.Empty);
                for (string? line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    var raw = line.Trim();
                    if (raw.Length == 0) continue;
                    if (raw.StartsWith(";") || raw.StartsWith("//") || raw.StartsWith("[INFO:", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine("; " + raw.TrimStart(';', '/').Trim());
                        continue;
                    }
                    if (rxPatch.IsMatch(raw))
                    {
                        sb.AppendLine(raw); // already a patch= line
                        continue;
                    }
                    var mp = rxPair.Match(raw);
                    if (mp.Success)
                    {
                        var addr = mp.Groups[1].Value.ToUpperInvariant();
                        var val  = mp.Groups[2].Value.ToUpperInvariant();
                        sb.AppendLine($"patch=1,EE,{addr},extended,{val}");
                    }
                }
                sb.AppendLine();
            }

            var content = sb.ToString().TrimEnd() + Environment.NewLine;
            contentOut = content;

            // Write a temp file for preview/export
            var dir = Path.Combine(Path.GetTempPath(), "CMPCollector");
            Directory.CreateDirectory(dir);
            var name = GetPreferredPnachDefaultFileName_META() 
			?? GuessPnachFileName(entries) 
			?? "CollectorPatch_TMP.pnach";
            var outPath = Path.Combine(dir, name);
            File.WriteAllText(outPath, content, new UTF8Encoding(false));
            return outPath;
        }

        /// <summary>
        /// Guess a default .pnach filename from collected meta (^1=HASH).
        /// </summary>
        private string? GuessPnachFileName(IReadOnlyList<KeyValuePair<string,string>> entries)
        {
            var rxHash = PnachMetaHashRegex;
            foreach (var kv in entries)
            {
                using var sr = new StringReader(kv.Value ?? string.Empty);
                for (string? line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    var m = rxHash.Match(line);
                    if (m.Success) return m.Groups[1].Value.ToUpperInvariant() + ".pnach";
                }
            }
            return null;
        }

    }
}
