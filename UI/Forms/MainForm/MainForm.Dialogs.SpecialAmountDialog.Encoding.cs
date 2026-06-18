// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.Dialogs.SpecialAmountDialog.Encoding.cs
// Purpose: Byte/hex/float formatting helpers for SpecialAmountDialog.
// Notes:
//  • Split from MainForm.Dialogs.SpecialAmountDialog.Conversion.cs during cleanup pass 17.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        internal partial class SpecialAmountDialog
        {
                private static byte[] NormalizeSize(byte[] bytes, int targetSize, bool bigEndian)
                {
                    if (bytes == null) return Array.Empty<byte>();
                    if (targetSize <= 0) return bytes;
                    if (bytes.Length == targetSize) return bytes;

                    if (bytes.Length < targetSize)
                    {
                        var pad = new byte[targetSize];
                        if (bigEndian)
                            Array.Copy(bytes, 0, pad, targetSize - bytes.Length, bytes.Length); // left-pad
                        else
                            Array.Copy(bytes, 0, pad, 0, bytes.Length); // right-pad
                        return pad;
                    }
                    else
                    {
                        var trimmed = new byte[targetSize];
                        if (bigEndian)
                            Array.Copy(bytes, bytes.Length - targetSize, trimmed, 0, targetSize); // keep tail
                        else
                            Array.Copy(bytes, 0, trimmed, 0, targetSize); // keep head
                        return trimmed;
                    }
                }

                private static byte[] HexToBytes(string hex)
                {
                    if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
                    var clean = System.Text.RegularExpressions.Regex.Replace(hex, "[^0-9A-Fa-f]", "");
                    if (clean.Length % 2 != 0) clean = "0" + clean;
                    var bytes = new byte[clean.Length / 2];
                    for (int i = 0; i < bytes.Length; i++)
                        bytes[i] = byte.Parse(clean.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                    return bytes;
                }

                private static string BytesToHex(byte[] data)
                {
                    var sb = new System.Text.StringBuilder(data.Length * 2);
                    for (int i = 0; i < data.Length; i++) sb.Append(data[i].ToString("X2"));
                    return sb.ToString();
                }

                private static System.Numerics.BigInteger BytesToUnsignedBigInteger(byte[] bytes, bool bigEndian)
                {
                    var bi = System.Numerics.BigInteger.Zero;
                    if (bytes == null || bytes.Length == 0) return bi;
                    if (bigEndian)
                    {
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bi = (bi << 8) + bytes[i];
                        }
                    }
                    else
                    {
                        System.Numerics.BigInteger factor = 1;
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bi += (System.Numerics.BigInteger)bytes[i] * factor;
                            factor <<= 8;
                        }
                    }
                    return bi;
                }

                private static string FormatFloatLabel(double d)
                {
                    if (double.IsPositiveInfinity(d)) return "+Infinity";
                    if (double.IsNegativeInfinity(d)) return "-Infinity";
                    if (double.IsNaN(d)) return "NaN";
                    return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                private static string FormatFloatLabel(float f)
                {
                    if (float.IsPositiveInfinity(f)) return "+Infinity";
                    if (float.IsNegativeInfinity(f)) return "-Infinity";
                    if (float.IsNaN(f)) return "NaN";
                    return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

        }
    }
}
