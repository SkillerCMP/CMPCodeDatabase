using System;

namespace CMPCodeDatabase.SpecialMods
{
    /// <summary>
    /// STAR encoders for Star Ocean: Till the End of Time (PS2).
    /// H4V (32-bit): 0x7DE3F7E3 ^ value
    /// CH  (32-bit): h = H4V(value); (h << 1) ^ h
    /// LVL (16-bit): 0x7E93 ^ level
    /// </summary>
    internal static class StarCalculator
    {
        private const uint H4V_Xor = 0x7DE3F7E3u;
        private const ushort LVL_Xor = 0x7E93;

        private static string Hex32(uint v) => v.ToString("X8").ToUpperInvariant();
        private static string Hex16(ushort v) => v.ToString("X4").ToUpperInvariant();

        public static string Encode(string type, uint dec32)
        {
            type = (type ?? "H4V").ToUpperInvariant();
            if (type == "CH")
            {
                unchecked
                {
                    uint h = H4_XOR(dec32);
                    uint ch = (h << 1) ^ h;
                    return Hex32(ch);
                }
            }
            else if (type == "LVL")
            {
                ushort lvl = (ushort)(dec32 & 0xFFFF);
                ushort enc = (ushort)(LVL_Xor ^ lvl);
                return Hex16(enc);
            }
            else // H4V
            {
                uint v = H4_XOR(dec32);
                return Hex32(v);
            }
        }

        private static uint H4_XOR(uint v) => H4V_Xor ^ v;
    }
}
