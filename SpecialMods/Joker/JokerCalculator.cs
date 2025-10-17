using System;
using System.Collections.Generic;
using System.Linq;

namespace CMPCodeDatabase.SpecialMods
{
    internal static class JokerCalculator
    {
        private static string Hex(int n) => n.ToString("X");
        private static string Pad4(string s) => (s ?? "0").PadLeft(4, '0').ToUpperInvariant();

        public static string ReverseMask(string mask4)
        {
            if (string.IsNullOrWhiteSpace(mask4)) mask4 = "0000";
            mask4 = Pad4(mask4);
            char[] arr = new char[4];
            for (int i = 0; i < 4; i++)
            {
                int v = Convert.ToInt32(mask4[i].ToString(), 16);
                arr[i] = Hex(0xF - v)[0];
            }
            return new string(arr);
        }

        // ===================== PS2 =====================
        // Nibbles: [Face][Shoulders][D-Pad][System]
        public static string MaskPS2(ISet<string> p, bool reverse = false)
        {
            int ny0 = (p.Contains("SELECT") ? 1 : 0) + (p.Contains("L3") ? 2 : 0) + (p.Contains("R3") ? 4 : 0) + (p.Contains("START") ? 8 : 0);
            int ny1 = (p.Contains("UP") ? 1 : 0) + (p.Contains("RIGHT") ? 2 : 0) + (p.Contains("DOWN") ? 4 : 0) + (p.Contains("LEFT") ? 8 : 0);
            int ny2 = (p.Contains("L2") ? 1 : 0) + (p.Contains("R2") ? 2 : 0) + (p.Contains("L1") ? 4 : 0) + (p.Contains("R1") ? 8 : 0);
            int ny3 = (p.Contains("TRIANGLE") ? 1 : 0) + (p.Contains("CIRCLE") ? 2 : 0) + (p.Contains("X") ? 4 : 0) + (p.Contains("SQUARE") ? 8 : 0);
            var s = $"{Hex(ny3)}{Hex(ny2)}{Hex(ny1)}{Hex(ny0)}";
            return reverse ? ReverseMask(s) : s;
        }

        // ===================== GameCube =====================
        // Big-endian nibbles: [Start][A/B/X/Y][Z/R/L][D-Pad]
        public static string MaskGC_BE(ISet<string> p)
        {
            int ny0 = (p.Contains("LEFT") ? 1 : 0) + (p.Contains("RIGHT") ? 2 : 0) + (p.Contains("DOWN") ? 4 : 0) + (p.Contains("UP") ? 8 : 0);
            int ny1 = (p.Contains("Z") ? 1 : 0) + (p.Contains("R") ? 2 : 0) + (p.Contains("L") ? 4 : 0);
            int ny2 = (p.Contains("A") ? 1 : 0) + (p.Contains("B") ? 2 : 0) + (p.Contains("X") ? 4 : 0) + (p.Contains("Y") ? 8 : 0);
            int ny3 = (p.Contains("START") ? 1 : 0);
            return $"{Hex(ny3)}{Hex(ny2)}{Hex(ny1)}{Hex(ny0)}";
        }
        public static string MaskGC_LE(ISet<string> p)
        {
            var be = MaskGC_BE(p);
            return be.Substring(2, 2) + be.Substring(0, 2);
        }

        // ===================== Wii =====================
        // Wiimote (+Nunchuk):
        // Nibbles: [Home/C/Z/Minus][A/B/One/Two][Plus][D-Pad(L/R/D/U)]
        public static string MaskWii_Wiimote(ISet<string> p, bool reverse = false)
        {
            int ny0 = (p.Contains("LEFT") ? 1 : 0) + (p.Contains("RIGHT") ? 2 : 0) + (p.Contains("DOWN") ? 4 : 0) + (p.Contains("UP") ? 8 : 0);
            int ny1 = (p.Contains("PLUS") ? 1 : 0);
            int ny2 = (p.Contains("A") ? 8 : 0) + (p.Contains("B") ? 4 : 0) + (p.Contains("ONE") ? 2 : 0) + (p.Contains("TWO") ? 1 : 0);
            int ny3 = (p.Contains("HOME") ? 8 : 0) + (p.Contains("C") ? 4 : 0) + (p.Contains("Z") ? 2 : 0) + (p.Contains("MINUS") ? 1 : 0);
            var s = $"{Hex(ny3)}{Hex(ny2)}{Hex(ny1)}{Hex(ny0)}";
            return reverse ? ReverseMask(s) : s;
        }

        // Wii Classic:
        // Nibbles: [Sub/L/Down/Right][R/Plus][A/Y/B/ZL][Up/Left/ZR/X]
        public static string MaskWii_Classic(ISet<string> p, bool reverse = false)
        {
            int ny0 = (p.Contains("UP") ? 1 : 0) + (p.Contains("LEFT") ? 2 : 0) + (p.Contains("ZR") ? 4 : 0) + (p.Contains("X") ? 8 : 0);
            int ny1 = (p.Contains("A") ? 1 : 0) + (p.Contains("Y") ? 2 : 0) + (p.Contains("B") ? 4 : 0) + (p.Contains("ZL") ? 8 : 0);
            int ny2 = (p.Contains("R") ? 2 : 0) + (p.Contains("PLUS") ? 4 : 0);
            int ny3 = (p.Contains("SUB") ? 1 : 0) + (p.Contains("L") ? 2 : 0) + (p.Contains("DOWN") ? 4 : 0) + (p.Contains("RIGHT") ? 8 : 0);
            var s = $"{Hex(ny3)}{Hex(ny2)}{Hex(ny1)}{Hex(ny0)}";
            return reverse ? ReverseMask(s) : s;
        }

        // ===================== GBA =====================
        // Keep ONLY the final 4 hex digits (mask); drop address/type words.
        // Nibbles: [0][R/L][Right/Left/Up/Down][A/B/Select/Start]
        public static string MaskGBA(ISet<string> p)
        {
            int ny0 = (p.Contains("A") ? 1 : 0) + (p.Contains("B") ? 2 : 0) + (p.Contains("SELECT") ? 4 : 0) + (p.Contains("START") ? 8 : 0);
            int ny1 = (p.Contains("RIGHT") ? 1 : 0) + (p.Contains("LEFT") ? 2 : 0) + (p.Contains("UP") ? 4 : 0) + (p.Contains("DOWN") ? 8 : 0);
            int ny2 = (p.Contains("R") ? 1 : 0) + (p.Contains("L") ? 2 : 0);
            int ny3 = 0;
            return $"{Hex(ny3)}{Hex(ny2)}{Hex(ny1)}{Hex(ny0)}";
        }
    }
}
