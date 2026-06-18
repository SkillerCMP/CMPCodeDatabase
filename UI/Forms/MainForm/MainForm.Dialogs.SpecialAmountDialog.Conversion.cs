// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.Dialogs.SpecialAmountDialog.Conversion.cs
// Purpose: Amount dialog conversion, validation, and formatting helpers.
// Notes:
//  • Split from MainForm.Dialogs.SpecialAmountDialog.cs during cleanup pass 12.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        internal partial class SpecialAmountDialog
        {
                /// <summary>
                /// Shows the default value in DEC for readability.
                /// For FLOAT types, returns the numeric value (not hex bits).
                /// </summary>
                private string DisplayDefaultDec()
                {
                    if (string.IsNullOrEmpty(defaultRawHex))
                        return "0";

                    bool wantBig = (endian == "BIG" || endian == "BE");
                    byte[] bytes = HexToBytes(defaultRawHex);

                    switch (type)
                    {
                        case "FLOAT":
                        case "FLOAT32":
                        {
                            bytes = NormalizeSize(bytes, 4, wantBig);
                            byte[] le = (byte[])bytes.Clone();
                            if (wantBig) Array.Reverse(le);
                            float f = BitConverter.ToSingle(le, 0);
                            return FormatFloatLabel(f);
                        }
                        case "DOUBLE":
                        case "FLOAT64":
                        {
                            bytes = NormalizeSize(bytes, 8, wantBig);
                            byte[] le = (byte[])bytes.Clone();
                            if (wantBig) Array.Reverse(le);
                            double d = BitConverter.ToDouble(le, 0);
                            return FormatFloatLabel(d);
                        }
                        case "INT":
                        {
                            var u = BytesToUnsignedBigInteger(bytes, wantBig);
                            int bits = Math.Max(8, byteSize * 8);
                            var twoPow = System.Numerics.BigInteger.One << bits;
                            var signBit = System.Numerics.BigInteger.One << (bits - 1);
                            var signed = (u & signBit) != 0 ? u - twoPow : u;
                            return signed.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        case "UINT":
                        case "DEC":
                        case "HEX":
                        default:
                        {
                            var u = BytesToUnsignedBigInteger(bytes, wantBig);
                            return u.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                }

                /// <summary>
                /// Central encoder. Accepts DEC or HEX for integer modes; DEC for float modes.
                /// Produces uppercase hex respecting endian and target byte size.
                /// Throws "[RANGE]" exceptions when value exceeds byte size range to drive auto-revert.
                /// </summary>
                private string ComputeHexFromInput(string input)
                {
                    byte[] bytes;
                    bool wantBig = (endian == "BIG" || endian == "BE");
                    string raw = (input ?? "").Trim();

                    switch (type)
                    {
                        case "HEX":
                        {
                            // Allow DEC here too if user types digits only (no 0x, no A-F)
                            bool looksHex = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                                          || System.Text.RegularExpressions.Regex.IsMatch(raw, "[A-Fa-f]");

                            if (!looksHex)
                            {
                                if (!System.Numerics.BigInteger.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var biDec))
                                    throw new Exception("Enter a valid integer.");
                                // Unsigned bounds for HEX pathway
                                EnsureWithinBounds(biDec, signed:false);
                                bytes = biDec.ToByteArray(isUnsigned: true, isBigEndian: wantBig);
                                break;
                            }

                            var h = System.Text.RegularExpressions.Regex.Replace(raw, "[^0-9A-Fa-f]", "");
                            if (h.Length == 0) h = "0";
                            if (h.Length % 2 != 0) h = "0" + h;
                            bytes = HexToBytes(h); // big-endian
                            if (!wantBig) Array.Reverse(bytes);
                            // Also enforce max byte size for pure hex by truncation rules: if longer than allowed, treat as range error
                            if (byteSize > 0 && bytes.Length > byteSize)
                                throw new Exception($"[RANGE] Hex value exceeds {byteSize} bytes.");
                            break;
                        }

                        case "INT":
                        {
                            if (!System.Numerics.BigInteger.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var bi))
                                throw new Exception("Enter a valid integer.");
                            EnsureWithinBounds(bi, signed:true);
                            bytes = bi.ToByteArray(isUnsigned: false, isBigEndian: wantBig);
                            break;
                        }
                        case "DEC":
                        case "UINT":
                        {
                            if (!System.Numerics.BigInteger.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var bi))
                                throw new Exception("Enter a valid integer.");
                            if (bi < 0) throw new Exception("Value must be non-negative.");
                            EnsureWithinBounds(bi, signed:false);
                            bytes = bi.ToByteArray(isUnsigned: true, isBigEndian: wantBig);
                            break;
                        }

                        case "FLOAT":
                        case "FLOAT32":
                        {
                            if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fd32))
                                throw new Exception("Enter a valid float.");

                            // Range guard: if casting to float overflows to Infinity, block.
                            float f = (float)fd32;
                            if (float.IsInfinity(f))
                                throw new Exception("[RANGE] Exceeds float32 range.");
                            bytes = BitConverter.GetBytes(f); // LE
                            if (wantBig) Array.Reverse(bytes);
                            break;
                        }
                        case "DOUBLE":
                        case "FLOAT64":
                        {
                            if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fd64))
                                throw new Exception("Enter a valid double.");
                            // double range is enormous; overflow to Infinity check
                            double d = fd64;
                            if (double.IsInfinity(d))
                                throw new Exception("[RANGE] Exceeds float64 range.");
                            bytes = BitConverter.GetBytes(d); // LE
                            if (wantBig) Array.Reverse(bytes);
                            break;
                        }

                        default:
                        {
                            // Fallback: treat as HEX
                            var hx = System.Text.RegularExpressions.Regex.Replace(raw, "[^0-9A-Fa-f]", "");
                            if (hx.Length % 2 != 0) hx = "0" + hx;
                            bytes = HexToBytes(hx);
                            if (!wantBig) Array.Reverse(bytes);
                            if (byteSize > 0 && bytes.Length > byteSize)
                                throw new Exception($"[RANGE] Hex value exceeds {byteSize} bytes.");
                            break;
                        }
                    }

                    // Normalize to declared byte size (pad/truncate)
                    if (byteSize > 0)
                        bytes = NormalizeSize(bytes, byteSize, wantBig);

                    return BytesToHex(bytes);
                }

                private void EnsureWithinBounds(System.Numerics.BigInteger value, bool signed)
                {
                    GetIntegerBounds(byteSize, signed, out var min, out var max);
                    if (value < min || value > max)
                        throw new Exception($"[RANGE] Value exceeds allowed range {min}..{max} for {byteSize} bytes.");
                }

                private static void GetIntegerBounds(int byteSize, bool signed, out System.Numerics.BigInteger min, out System.Numerics.BigInteger max)
                {
                    int bits = Math.Max(1, byteSize) * 8;
                    var one = System.Numerics.BigInteger.One;
                    if (signed)
                    {
                        max = (one << (bits - 1)) - one;
                        min = - (one << (bits - 1));
                    }
                    else
                    {
                        min = System.Numerics.BigInteger.Zero;
                        max = (one << bits) - one;
                    }
                }

        }
    }
}
