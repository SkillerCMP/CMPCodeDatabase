// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — Helpers: special [Joker:<plat>[:mods...]] parsing
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Parses a Joker tag of the form:
        ///   [Joker:PS2]
        ///   [Joker:GC]
        ///   [Joker:Wii]
        ///   [Joker:GBA]
        /// Optional mods: :Reverse, :LE, :Wiimote, :Classic (case-insensitive).
        /// Returns true if recognized.
        /// </summary>
        private static bool TryParseJokerTag(string token, out string platform, out string[] mods)
        {
            platform = string.Empty; mods = Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(token)) return false;
            if (!token.StartsWith("Joker:", StringComparison.OrdinalIgnoreCase)) return false;

            var rest = token.Substring(6).Trim();
            var parts = rest.Split(':', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).ToArray();
            if (parts.Length == 0) return false;

            var p0 = parts[0].ToUpperInvariant();
            if (p0 != "PS2" && p0 != "GC" && p0 != "WII" && p0 != "GBA") return false;

            platform = p0;
            mods = parts.Skip(1).Select(s => s.ToUpperInvariant()).ToArray();
            return true;
        }
    }
}
