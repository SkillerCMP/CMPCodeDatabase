// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — Helpers: special [Amount:VAL:TYPE:ENDIAN<Label>] parsing
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Parse Amount tag and split out optional &lt;Label&gt; on the END.
        /// Returns endian stripped of label, and boxLabel (or null).
        /// </summary>
        private static bool TryParseSpecialAmountTag2(string tag,
                                                      out string title,
                                                      out string defHex,
                                                      out string type,
                                                      out string endian,
                                                      out string boxLabel)
        {
            title = "Amount"; defHex = ""; type = "HEX"; endian = "BIG"; boxLabel = null;
            if (string.IsNullOrWhiteSpace(tag)) return false;
            if (!tag.StartsWith("Amount:", StringComparison.OrdinalIgnoreCase)) return false;

            boxLabel = ExtractTagLabel(tag);
            string core = StripTagLabel(tag); // e.g. Amount:05F5E0FF:HEX:BIG

            var parts = core.Split(':');
            if (parts.Length < 4) return false;

            title  = parts[0].Trim();
            defHex = parts[1].Trim();
            type   = parts[2].Trim();
            endian = parts[3].Trim();
            return true;
        }
    }
}
