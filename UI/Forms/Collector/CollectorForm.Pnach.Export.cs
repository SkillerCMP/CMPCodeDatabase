using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        /// <summary>
        /// Preview PCSX2 .pnach text in Notepad (quick and reliable).
        /// </summary>
        private void PreviewPnach(bool onlyChecked)
        {
            // Friendly guide (respects "don't show again" flags)
            try { EnsurePnachHelpOnce(); } catch { }

            var entries = CollectEntries(onlyChecked);
            if (entries.Count == 0)
            {
                MessageBox.Show(this, "No entries to preview.", "PCSX2 .pnach", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string content;
            string path = BuildTempPnach(entries, out content);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = "\"" + path + "\"",
                    UseShellExecute = false
                });
            }
            catch
            {
                // Fallback: show a simple dialog if Notepad cannot be launched
                MessageBox.Show(this, content, "PCSX2 .pnach Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Export PCSX2 .pnach via SaveFileDialog.
        /// </summary>
        private void ExportPnach(bool onlyChecked)
        {
            // Friendly guide (respects "don't show again" flags)
            try { EnsurePnachHelpOnce(); } catch { }

            var entries = CollectEntries(onlyChecked);
            if (entries.Count == 0)
            {
                MessageBox.Show(this, "No entries to export.", "PCSX2 .pnach", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string content;
            string _ = BuildTempPnach(entries, out content); // path not needed here

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Export PCSX2 .pnach";
                sfd.Filter = "PCSX2 Patch (*.pnach)|*.pnach|All files (*.*)|*.*";
                var prefer = GetPreferredPnachDefaultFileName_META(); // from CollectorForm.Tools.ELFCRC.cs
				sfd.FileName = prefer ?? GuessPnachFileName(entries) ?? "CMPDBCollector.pnach";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try { File.WriteAllText(sfd.FileName, content, new UTF8Encoding(false)); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export .pnach", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }

        /// <summary>
        /// Export Apollo .savepatch (Checked/All) using existing savepatch builder.
        /// </summary>
        private void ExportSavepatch(bool onlyChecked)
        {
            var entries = CollectEntries(onlyChecked);
            if (entries.Count == 0)
            {
                MessageBox.Show(this, "No entries to export.", "Apollo .savepatch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string content;
            string _ = BuildTempSavepatch(entries, out content); // defined in your Savepatch builder partial

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Export Apollo .savepatch";
                sfd.Filter = "Apollo Savepatch (*.savepatch)|*.savepatch|All files (*.*)|*.*";
                sfd.FileName = "CMPDBCollector.savepatch";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try { File.WriteAllText(sfd.FileName, content, new UTF8Encoding(false)); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export .savepatch", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }

        /// <summary>
        /// Build a minimal but compatible PCSX2 .pnach from collected entries.
        /// Header mirrors the savepatch style with '//' comments. Body emits existing patch= lines,
        /// or converts AAAAAAAA BBBBBBBB pairs into patch=1,EE,AAAAAAAA,extended,BBBBBBBB.
        /// Also tries to include Original File Name, Game Name, HASH, Credits when present in the text.
        /// </summary>
        private string BuildTempPnach(IReadOnlyList<KeyValuePair<string, string>> entries, out string contentOut)
        {
            // Try to glean meta from collected bodies
            var rxFile    = new Regex(@"(?mi)^\s*(?:\^4\s*=\s*FILE|Original\s+File\s+Name)\s*:\s*(.+?)\s*$");
            var rxHash    = new Regex(@"(?mi)^\s*(?:\^1\s*=\s*HASH|HASH|CRC)\s*:\s*([0-9A-F]{8})\b.*$");
            var rxName    = new Regex(@"(?mi)^\s*(?:\^3\s*=\s*NAME|NAME)\s*:\s*(.+?)\s*$");
            var rxCredits = new Regex(@"(?mi)^[%#]\s*Credits\s*:\s*(.+)$");

            string topFile = null, topName = null, topHash = null;
            var creditsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in entries)
            {
                using var sr = new StringReader(kv.Value ?? string.Empty);
                for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
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
            var rxPatch = new Regex(@"(?mi)^\s*patch\s*=\s*1\s*,\s*EE\s*,", RegexOptions.IgnoreCase);
            var rxPair  = new Regex(@"(?mi)^\s*([0-9A-F]{8})\s+([0-9A-F]{8})\s*$");
            foreach (var kv in entries)
            {
                var title = kv.Key ?? "PATCH";
                sb.AppendLine("[" + title + "]");
                using var sr = new StringReader(kv.Value ?? string.Empty);
                for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
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
        private string GuessPnachFileName(IReadOnlyList<KeyValuePair<string,string>> entries)
        {
            var rxHash = new Regex(@"(?mi)^\s*(?:\^1\s*=\s*HASH|HASH|CRC)\s*:\s*([0-9A-F]{8})\b.*$");
            foreach (var kv in entries)
            {
                using var sr = new StringReader(kv.Value ?? string.Empty);
                for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    var m = rxHash.Match(line);
                    if (m.Success) return m.Groups[1].Value.ToUpperInvariant() + ".pnach";
                }
            }
            return null;
        }
    }
}
