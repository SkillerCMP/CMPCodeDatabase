using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase.SpecialMods
{
    internal static class JokerMod
    {
        // Allow ALL as a platform to expose the dropdown; otherwise lock the dialog to that platform
        private static readonly Regex JokerToken = new(@"\[(?:Joker|JKR):(?<plat>PS2|GC|Wii|GBA|ALL)(?::(?<mods>[^\]]+))?\]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Finds all Joker tokens in a string.</summary>
        public static IEnumerable<Match> FindTokens(string text)
            => JokerToken.Matches(text ?? string.Empty).Cast<Match>();

        /// <summary>Resolve every [Joker:*] token in the RichTextBox.</summary>
        public static void BatchResolveRichTextBox(IWin32Window owner, RichTextBox rtb, bool keepTokenAppend = false)
        {
            if (rtb == null) return;
            string text = rtb.Text;
            var matches = FindTokens(text).ToList();
            if (matches.Count == 0)
            {
                MessageBox.Show(owner, "No [Joker:*] tokens found.", "Joker", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Replace from the end so indices remain valid
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var m = matches[i];
                ResolveOne(owner, rtb, m.Index, m.Length, m.Value,
                           m.Groups["plat"].Value.ToUpperInvariant(),
                           (m.Groups["mods"]?.Value ?? ""), keepTokenAppend);
            }
        }

        /// <summary>Resolve the [Joker:*] token nearest the caret.</summary>
        public static void ResolveTokenAtCaret(IWin32Window owner, RichTextBox rtb, bool keepTokenAppend = false)
        {
            if (rtb == null) return;
            int caret = rtb.SelectionStart;
            string text = rtb.Text;

            var tokens = FindTokens(text).ToList();
            if (tokens.Count == 0)
            {
                MessageBox.Show(owner, "No [Joker:*] token found.", "Joker", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var nearest = tokens
                .Select(mt => new { M = mt, Center = mt.Index + mt.Length / 2, Dist = Math.Abs((mt.Index + mt.Length / 2) - caret) })
                .OrderBy(x => x.Dist)
                .First().M;

            ResolveOne(owner, rtb, nearest.Index, nearest.Length, nearest.Value,
                       nearest.Groups["plat"].Value.ToUpperInvariant(),
                       (nearest.Groups["mods"]?.Value ?? ""), keepTokenAppend);
        }

        private static void ResolveOne(IWin32Window owner, RichTextBox rtb, int index, int length, string rawToken, string plat, string modsRaw, bool keepTokenAppend)
        {
            var mods = new HashSet<string>(modsRaw.Split(':', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpperInvariant()));

            using (var dlg = new JokerDialog(plat, mods))
            {
                var dr = dlg.ShowDialog(owner);
                if (dr != DialogResult.OK) return;

                string hex = dlg.ResultHex.ToUpperInvariant();
                string label = dlg.ResultPressLabel ?? "Press Buttons"; // e.g., "Press Select+Start"

                // Build replacement text
                string replacement = keepTokenAppend ? $"{rawToken}={hex}" : hex;
                // Append the friendly comment with buttons
                replacement += $" /* {label} */";

                string text = rtb.Text;
                if (index >= 0 && index + length <= text.Length)
                    rtb.Text = text.Remove(index, length).Insert(index, replacement);
            }
        }
    }
}
