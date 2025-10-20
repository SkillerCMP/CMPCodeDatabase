using System;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>
        /// Parses a text-amount tag that requests ASCII bytes output as hex:
        ///   [Amount:<baseText>:ASCII:BIG] or [Amount:<baseText>:ASCII:LITTLE]
        /// Accepts BIG/BE or LITTLE/LE, and supports an optional label in angle brackets on the 4th field:
        ///   [Amount:<baseText>:ASCII:BIG<MyTag>]
        /// </summary>
        private bool TryParseTextAmountTagHex(string token, out string baseText, out string endian, out string label)
        {
            baseText = string.Empty;
            endian = "BIG";
            label = string.Empty;
            if (string.IsNullOrWhiteSpace(token)) return false;

            // token may be the whole [Amount:...:...:...] or just the inner core
            var t = token.Trim();
            if (t.StartsWith("[") && t.EndsWith("]")) t = t.Substring(1, t.Length - 2);

            var parts = t.Split(':');
            if (parts.Length < 4) return false;
            if (!parts[0].Equals("Amount", StringComparison.OrdinalIgnoreCase)) return false;

            var enc = (parts[2] ?? "").Trim();
            var mode = (parts[3] ?? "").Trim();

            // Extract optional <label> from mode
            int lt = mode.IndexOf('<');
            if (lt >= 0)
            {
                int gt = mode.LastIndexOf('>');
                if (gt > lt) label = mode.Substring(lt + 1, gt - lt - 1).Trim();
                mode = mode.Substring(0, lt).Trim();
            }

            // Only for ASCII encoding
            if (!enc.Equals("ASCII", StringComparison.OrdinalIgnoreCase)) return false;

            // Endian must be BIG/BE or LITTLE/LE
            if (mode.Equals("BIG", StringComparison.OrdinalIgnoreCase) || mode.Equals("BE", StringComparison.OrdinalIgnoreCase))
                endian = "BIG";
            else if (mode.Equals("LITTLE", StringComparison.OrdinalIgnoreCase) || mode.Equals("LE", StringComparison.OrdinalIgnoreCase))
                endian = "LITTLE";
            else
                return false;

            baseText = (parts[1] ?? string.Empty).Trim();
            return true;
        }
    }
}
