using System;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase.SpecialMods
{
    internal static class TimeStampMod
    {
        internal enum EpochMode { Current, Select }
        internal enum EpochEndian { Big, Little }

        internal sealed class EpochTag
        {
            public EpochMode Mode { get; set; }
            public bool Is64Bit { get; set; }
            public EpochEndian Endian { get; set; } = EpochEndian.Big;
        }

        /// <summary>
        /// Accepts:
        ///   EPOCH:CURRENT:HEX:BIG
        ///   EPOCH:CURRENT:HEX:BIG:64
        ///   EPOCH:CURRENT:HEX:LITTLE
        ///   EPOCH:CURRENT:HEX:LITTLE:64
        ///   EPOCH:SELECT:HEX:BIG
        ///   EPOCH:SELECT:HEX:BIG:64
        ///   EPOCH:SELECT:HEX:LITTLE
        ///   EPOCH:SELECT:HEX:LITTLE:64
        /// 32-bit default unless ":64" present.
        /// </summary>
        public static bool TryParseEpochTag(string token, out EpochTag tag)
        {
            tag = null!;
            if (string.IsNullOrWhiteSpace(token)) return false;

            // Strip any <Label> payload if caller passed raw token
            int lt = token.IndexOf('<');
            if (lt >= 0) token = token.Substring(0, lt);

            var parts = token.Split(':', StringSplitOptions.RemoveEmptyEntries)
                             .Select(p => p.Trim())
                             .ToArray();

            if (parts.Length < 2) return false;
            if (!parts[0].Equals("EPOCH", StringComparison.OrdinalIgnoreCase)) return false;

            EpochMode mode;
            if (parts[1].Equals("CURRENT", StringComparison.OrdinalIgnoreCase)) mode = EpochMode.Current;
            else if (parts[1].Equals("SELECT", StringComparison.OrdinalIgnoreCase)) mode = EpochMode.Select;
            else return false;

            // Optional strictness: if user included extra fields, validate HEX + endian
            var endian = EpochEndian.Big;
            if (parts.Length >= 4)
            {
                if (!parts[2].Equals("HEX", StringComparison.OrdinalIgnoreCase)) return false;

                if (parts[3].Equals("BIG", StringComparison.OrdinalIgnoreCase) ||
                    parts[3].Equals("BE", StringComparison.OrdinalIgnoreCase))
                {
                    endian = EpochEndian.Big;
                }
                else if (parts[3].Equals("LITTLE", StringComparison.OrdinalIgnoreCase) ||
                         parts[3].Equals("LE", StringComparison.OrdinalIgnoreCase))
                {
                    endian = EpochEndian.Little;
                }
                else return false;
            }

            bool is64 = parts.Any(p => p.Equals("64", StringComparison.OrdinalIgnoreCase) ||
                                       p.Equals("64BIT", StringComparison.OrdinalIgnoreCase) ||
                                       p.Equals("QWORD", StringComparison.OrdinalIgnoreCase));

            tag = new EpochTag { Mode = mode, Is64Bit = is64, Endian = endian };
            return true;
        }

        /// <summary>
        /// Resolves EPOCH tags into hex. SELECT mode opens a dialog.
        /// Returns true if token is recognized (even if user cancels).
        /// </summary>
        public static bool TryResolveEpoch(IWin32Window owner, string token, out string hex, out string appliedLabel)
        {
            hex = string.Empty;
            appliedLabel = string.Empty;

            if (!TryParseEpochTag(token, out var tag)) return false;

            if (tag.Mode == EpochMode.Current)
            {
                long sec = EpochUtil.NowUnixSeconds();
                bool le = tag.Endian == EpochEndian.Little;
                if (!EpochUtil.TryFormatHexSeconds(sec, tag.Is64Bit, le, out var h, out var err))
                {
                    MessageBox.Show(owner, err, "Epoch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return true;
                }

                hex = h;
                appliedLabel = (tag.Is64Bit ? "Epoch 64" : "Epoch 32") + (le ? " LE" : "");
                return true;
            }

            // SELECT mode
            using (var dlg = new TimeStampDialog(tag.Is64Bit))
            {
                if (dlg.ShowDialog(owner) != DialogResult.OK) return true; // recognized, but canceled

                bool le = tag.Endian == EpochEndian.Little;
                if (!EpochUtil.TryFormatHexSeconds(dlg.ResultUnixSeconds, dlg.ResultIs64Bit, le, out var h2, out var err2))
                {
                    MessageBox.Show(owner, err2, "Epoch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return true;
                }

                hex = (h2 ?? string.Empty).ToUpperInvariant();
                appliedLabel = dlg.ResultLabel + (le ? " LE" : "");
                return true;
            }
        }
    }
}
