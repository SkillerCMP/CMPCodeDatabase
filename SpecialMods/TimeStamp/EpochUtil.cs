using System;

namespace CMPCodeDatabase.SpecialMods
{
    internal static class EpochUtil
    {
        public static uint Swap32(uint x)
        {
            return (x >> 24) |
                   ((x >> 8) & 0x0000FF00u) |
                   ((x << 8) & 0x00FF0000u) |
                   (x << 24);
        }

        public static ulong Swap64(ulong x)
        {
            // Bit-twiddling swap, no allocations
            x = (x >> 32) | (x << 32);
            x = ((x & 0xFFFF0000FFFF0000UL) >> 16) | ((x & 0x0000FFFF0000FFFFUL) << 16);
            x = ((x & 0xFF00FF00FF00FF00UL) >> 8)  | ((x & 0x00FF00FF00FF00FFUL) << 8);
            return x;
        }

        public static long NowUnixSeconds()
            => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Treat the provided DateTime as LOCAL time (DateTimePicker.Value is usually Kind=Unspecified).
        /// </summary>
        public static long ToUnixSecondsFromLocal(DateTime localLike)
        {
            var local = DateTime.SpecifyKind(localLike, DateTimeKind.Local);
            return new DateTimeOffset(local).ToUnixTimeSeconds();
        }

        public static bool TryFormatHexSeconds(long unixSeconds, bool is64Bit, out string hex, out string error)
            => TryFormatHexSeconds(unixSeconds, is64Bit, littleEndian: false, out hex, out error);

        public static bool TryFormatHexSeconds(long unixSeconds, bool is64Bit, bool littleEndian, out string hex, out string error)
        {
            hex = string.Empty;
            error = string.Empty;

            if (is64Bit)
            {
                // 64-bit (X16)
                unchecked
                {
                    ulong v = (ulong)unixSeconds;
                    if (littleEndian) v = Swap64(v);
                    hex = v.ToString("X16");
                    return true;
                }
            }

            // 32-bit (X8) — enforce 0..0xFFFFFFFF (unsigned range)
            if (unixSeconds < 0 || unixSeconds > uint.MaxValue)
            {
                error = "Epoch seconds out of 32-bit range (0..0xFFFFFFFF). Use 64-bit.";
                return false;
            }

            uint u = (uint)unixSeconds;
            if (littleEndian) u = Swap32(u);
            hex = u.ToString("X8");
            return true;
        }
    }
}
