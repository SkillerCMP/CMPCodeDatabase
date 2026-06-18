using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        private static readonly Regex PnachMetaFileRegex = new(@"^\s*(?:\^4\s*=\s*FILE|Original\s+File\s+Name)\s*:\s*(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex PnachMetaHashRegex = new(@"^\s*(?:\^1\s*=\s*HASH|HASH|CRC)\s*:\s*([0-9A-F]{8})\b.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex PnachMetaNameRegex = new(@"^\s*(?:\^3\s*=\s*NAME|NAME)\s*:\s*(.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex PnachCreditsRegex = new(@"^[%#]\s*Credits\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex PnachPatchLineRegex = new(@"^\s*patch\s*=\s*1\s*,\s*EE\s*,", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PnachPairLineRegex = new(@"^\s*([0-9A-F]{8})\s+([0-9A-F]{8})\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                var prefer = GetPreferredPnachDefaultFileName_META(); // from CollectorControl.Tools.ELFCRC.cs
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

    }
}
