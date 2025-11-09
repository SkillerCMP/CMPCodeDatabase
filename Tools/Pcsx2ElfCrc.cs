using System;
using System.IO;
using System.Text;

namespace CMPCodeDatabase.Tools
{
    /// <summary>
    /// PCSX2 "Game CRC" for .pnach filenames.
    /// NOTE: This is NOT CRC-32. It's the XOR of 32-bit little-endian words across the ENTIRE ELF file,
    /// zero-padding the final partial word if needed.
    /// Example: A1FD63D6 for SLES_526.41.
    /// </summary>
    public static class Pcsx2ElfCrc
    {
        public static string ComputeFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            using var fs = File.OpenRead(path);
            return Compute(fs);
        }

        public static string Compute(Stream stream)
        {
            if (stream == null) return null;
            uint x = 0;
            var buf = new byte[1 << 20]; // 1MB chunks
            int read;
            // rolling word buffer
            int idx = 0;
            Span<byte> word = stackalloc byte[4];
            word.Clear();

            while ((read = stream.Read(buf, 0, buf.Length)) > 0)
            {
                int i = 0;
                while (i < read)
                {
                    word[idx++] = buf[i++];
                    if (idx == 4)
                    {
                        x ^= BitConverter.ToUInt32(word);
                        idx = 0;
                        word.Clear();
                    }
                }
            }
            // pad and XOR last partial word
            if (idx > 0)
                x ^= BitConverter.ToUInt32(word);

            return x.ToString("X8");
        }
    }
}
