// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.EnhancedCalculatorForm.Encoding.cs
// Purpose: Parsing, endian, and hex/float formatting helpers for EnhancedCalculatorForm.
// Notes:
//  • Split from MainForm.EnhancedCalculatorForm.Conversion.cs during cleanup pass 19.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        public partial class EnhancedCalculatorForm
        {
                private static bool IsHexString(string s)
                {
                    if (string.IsNullOrEmpty(s)) return false;
                    foreach (char c in s) if (!Uri.IsHexDigit(c)) return false;
                    return true;
                }

                private static byte[] TrimTo(byte[] bytes, int n)
                {
                    if (bytes == null) return Array.Empty<byte>();
                    if (n <= 0) return Array.Empty<byte>();
                    if (bytes.Length <= n) return bytes;
                    var trimmed = new byte[n];
                    Array.Copy(bytes, trimmed, n);
                    return trimmed;
                }

                private static string Float32Hex(float f, bool bigEndian)
                {
                    var b = BitConverter.GetBytes(f);
                    if (bigEndian) Array.Reverse(b);
                    return BitConverter.ToString(b).Replace("-", "");
                }

                private static string Float64Hex(double d, bool bigEndian)
                {
                    var b = BitConverter.GetBytes(d);
                    if (bigEndian) Array.Reverse(b);
                    return BitConverter.ToString(b).Replace("-", "");
                }

                private static string FormatFloatLabel(double d)
                {
                    if (double.IsPositiveInfinity(d)) return "+Infinity";
                    if (double.IsNegativeInfinity(d)) return "-Infinity";
                    if (double.IsNaN(d)) return "NaN";
                    return d.ToString(CultureInfo.InvariantCulture);
                }
                private static string FormatFloatLabel(float f)
                {
                    if (float.IsPositiveInfinity(f)) return "+Infinity";
                    if (float.IsNegativeInfinity(f)) return "-Infinity";
                    if (float.IsNaN(f)) return "NaN";
                    return f.ToString(CultureInfo.InvariantCulture);
                }

                private static bool TryParseBigInteger(string text, out BigInteger value)
                {
                    text = text.Trim();

                    if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        string hex = text.Substring(2).Replace("_", "").Replace(" ", "");
                        if (hex.Length == 0) { value = BigInteger.Zero; return true; }
                        foreach (char c in hex) { if (!Uri.IsHexDigit(c)) { value = BigInteger.Zero; return false; } }
                        if (hex.Length % 2 == 1) hex = "0" + hex;
                        int len = hex.Length / 2;
                        var bytes = new byte[len + 1];
                        for (int i = 0; i < len; i++)
                            bytes[i] = byte.Parse(hex.Substring(hex.Length - (i + 1) * 2, 2), NumberStyles.HexNumber);
                        value = new BigInteger(bytes);
                        return true;
                    }

                    if (BigInteger.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                        return true;

                    value = BigInteger.Zero;
                    return false;
                }

                private static string FormatHex(BigInteger unsignedVal, bool bigEndian)
                {
                    if (unsignedVal < 0) unsignedVal = -unsignedVal;
                    byte[] bytes;
                    if (unsignedVal.IsZero)
                    {
                        bytes = new byte[] { 0x00 };
                    }
                    else
                    {
                        List<byte> list = [];
                        var temp = unsignedVal;
                        while (temp > 0)
                        {
                            list.Add((byte)(temp & 0xFF));
                            temp >>= 8;
                        }
                        bytes = list.ToArray(); // little-endian
                    }

                    if (bytes.Length > 16) bytes = TrimTo(bytes, 16);

                    if (bigEndian)
                    {
                        var be = (byte[])bytes.Clone();
                        Array.Reverse(be);
                        return BitConverter.ToString(be).Replace("-", "");
                    }
                    else
                    {
                        return BitConverter.ToString(bytes).Replace("-", "");
                    }
                }

        }
    }
}
