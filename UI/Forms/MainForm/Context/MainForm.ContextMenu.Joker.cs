// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Context/MainForm.ContextMenu.Joker.cs
// Adds a "Joker Controller…" item to the code context menu and resolves [Joker:*] at caret.
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void TryWireJokerContext()
        {
            try
            {
                if (codesContextMenu == null) return;

                // Avoid duplicates
                foreach (ToolStripItem it in codesContextMenu.Items)
                    if (string.Equals(it?.Name, "miJokerController", StringComparison.Ordinal)) return;

                var sep = new ToolStripSeparator();
                var mi = new ToolStripMenuItem("Open Joker Controller…");
                mi.Name = "miJokerController";
                mi.Click += (s, e) => ResolveJokerAtCaretOrFirst();

                codesContextMenu.Items.Add(sep);
                codesContextMenu.Items.Add(mi);

                // Enable only when a [Joker:*] tag exists in current code
                codesContextMenu.Opening += (s, e) =>
                {
                    var node = treeCodes?.SelectedNode;
                    bool enabled = false;
                    if (node?.Tag is string tpl && !string.IsNullOrEmpty(tpl))
                    {
                        enabled = Regex.IsMatch(tpl, @"\[(?:Joker|JKR):(?:(?:PS2|GC|Wii|GBA|ALL))(?:\:[^\]]+)?\]", RegexOptions.IgnoreCase);
                    }
                    mi.Enabled = enabled;
                };
            }
            catch { /* noop */ }
        }

        private void ResolveJokerAtCaretOrFirst()
        {
            var node = treeCodes?.SelectedNode;
            if (node?.Tag is not string tpl || string.IsNullOrEmpty(tpl)) return;

            int caret = txtCodePreview?.SelectionStart ?? 0;

            // Find nearest [Joker:*] around caret; else first occurrence
            var rx = new Regex(@"\[(?:Joker|JKR):(?<plat>PS2|GC|Wii|GBA|ALL)(?::(?<mods>[^\]]+))?\]", RegexOptions.IgnoreCase);
            var matches = rx.Matches(tpl).Cast<Match>().ToList();
            if (matches.Count == 0) return;

            Match target = matches[0];
            foreach (var m in matches)
            {
                int mid = m.Index + m.Length / 2;
                if (Math.Abs(mid - caret) < Math.Abs(target.Index + target.Length/2 - caret))
                    target = m;
            }

            string plat = target.Groups["plat"].Value.ToUpperInvariant();
            string modsRaw = target.Groups["mods"]?.Value ?? string.Empty;
            var mods = new System.Collections.Generic.HashSet<string>(
                (modsRaw ?? string.Empty).Split(':', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpperInvariant())
            );

            using (var dlg = new CMPCodeDatabase.SpecialMods.JokerDialog(plat, mods))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string hex = dlg.ResultHex?.ToUpperInvariant() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(hex)) return;

                // Replace token
                tpl = tpl.Remove(target.Index, target.Length).Insert(target.Index, hex);
                node.Tag = tpl;
                txtCodePreview.Text = tpl;
                try { txtCodePreview.HideSelection = false; txtCodePreview.Select(target.Index, hex.Length); txtCodePreview.ScrollToCaret(); txtCodePreview.Update(); } catch { }

                AppendAppliedModName(node, "Joker");
                node.Text = GetDisplayName(node);
            }
        }
    }
}
