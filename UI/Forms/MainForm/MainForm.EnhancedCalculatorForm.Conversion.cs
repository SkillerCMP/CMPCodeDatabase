// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.EnhancedCalculatorForm.Conversion.cs
// Purpose: Conversion and formatting logic for EnhancedCalculatorForm.
// Notes:
//  • Split from MainForm.EnhancedCalculatorForm.cs during cleanup pass 11.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        public partial class EnhancedCalculatorForm : Form
        {
                private void ClearOutputs()
                {
                    txtOutDec.Clear(); txtOutHex.Clear(); txtOutF32.Clear(); txtOutF64.Clear();
                }

                private void DoFloatQuick()
                {

                    // Gracefully handle empty/invalid inputs; no popups on auto-calls (endian swap)
                    string aTxt = (txtFloatA.Text ?? "").Trim();
                    string bTxt = (txtFloatB.Text ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(aTxt) || string.IsNullOrWhiteSpace(bTxt))
                    {
                        // No inputs -> clear quick outputs and return
                        txtFloatQuickF32.Text = string.Empty;
                        txtFloatQuickF64.Text = string.Empty;
                        return;
                    }

                    if (!double.TryParse(aTxt, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double a) ||
                        !double.TryParse(bTxt, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double b))
                    {
                        // Invalid numbers -> clear quick outputs and return quietly
                        txtFloatQuickF32.Text = string.Empty;
                        txtFloatQuickF64.Text = string.Empty;
                        return;
                    }

                    double res = 0.0;
                    var op = cmbOp.SelectedItem?.ToString();
                    if (op == "+") res = a + b;
                    else if (op == "-") res = a - b;
                    else if (op == "*") res = a * b;
                    else if (op == "/") res = a / b;
                    else res = a + b;

                    bool bigEndian = string.Equals(cmbEndian.SelectedItem?.ToString(), "Big-endian", StringComparison.Ordinal);
                    float f32 = (float)res;
                    // Keep decimal + HEX on quick results
                    txtFloatQuickF32.Text = $"{FormatFloatLabel(f32)} | {Float32Hex(f32, bigEndian)}";
                    txtFloatQuickF64.Text = $"{FormatFloatLabel(res)} | {Float64Hex(res, bigEndian)}";

                }


                private void DoConvert()
                {
                    string input = txtInput.Text?.Trim() ?? "";
                    bool bigEndian = string.Equals(cmbEndian.SelectedItem?.ToString(), "Big-endian", StringComparison.Ordinal);
                    if (string.IsNullOrEmpty(input)) { ClearOutputs(); return; }

                    // Fx => float input
                    if (input.StartsWith("Fx", StringComparison.OrdinalIgnoreCase))
                    {
                        string body = input.Substring(2).Trim();
                        if (body.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) body = body.Substring(2);
                        body = body.Replace("_", "").Replace(" ", "");

                        if (IsHexString(body))
                        {
                            if (body.Length <= 8)
                            {
                                body = body.PadLeft(8, '0');
                                byte[] be = new byte[4];
                                for (int i = 0; i < 4; i++) be[i] = byte.Parse(body.Substring(i * 2, 2), NumberStyles.HexNumber);
                                byte[] le = (byte[])be.Clone(); Array.Reverse(le);
                                float f32 = BitConverter.ToSingle(le, 0);
                                double f64v = f32;
                                txtOutDec.Text = FormatFloatLabel(f32);
                                txtOutHex.Text = Float32Hex(f32, bigEndian);
                                txtOutF32.Text = Float32Hex(f32, bigEndian);
                                txtOutF64.Text = Float64Hex(f64v, bigEndian);
                                return;
                            }
                            else
                            {
                                body = body.PadLeft(16, '0');
                                byte[] be = new byte[8];
                                for (int i = 0; i < 8; i++) be[i] = byte.Parse(body.Substring(i * 2, 2), NumberStyles.HexNumber);
                                byte[] le = (byte[])be.Clone(); Array.Reverse(le);
                                double f64 = BitConverter.ToDouble(le, 0);
        float f32v = (float)f64;
        txtOutDec.Text = FormatFloatLabel(f64);
                                txtOutHex.Text = Float64Hex(f64, bigEndian);
                                txtOutF32.Text = Float32Hex(f32v, bigEndian);
                                txtOutF64.Text = Float64Hex(f64, bigEndian);
                                return;
                            }
                        }
                        else
                        {
                            // decimal float (allow inf, -inf, nan too)
                            if (TryParseSpecialFloat(body, out var spec))
                            {
                                float f32 = (float)spec;
                                txtOutDec.Text = FormatFloatLabel(spec);
                                txtOutHex.Text = Float64Hex(spec, bigEndian);
                                txtOutF32.Text = Float32Hex(f32, bigEndian);
                                txtOutF64.Text = Float64Hex(spec, bigEndian);
                                return;
                            }
                            if (double.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out var dval))
                            {
                                float f32 = (float)dval;
                                txtOutDec.Text = FormatFloatLabel(dval);
                                txtOutHex.Text = Float64Hex(dval, bigEndian);
                                txtOutF32.Text = Float32Hex(f32, bigEndian);
                                txtOutF64.Text = Float64Hex(dval, bigEndian);
                                return;
                            }
                        }
                    }

                    // Special values top-level
                    if (TryParseSpecialFloat(input, out var sVal))
                    {
                        float f32 = (float)sVal;
                        txtOutDec.Text = FormatFloatLabel(sVal);
                        txtOutHex.Text = Float64Hex(sVal, bigEndian);
                        txtOutF32.Text = Float32Hex(f32, bigEndian);
                        txtOutF64.Text = Float64Hex(sVal, bigEndian);
                        return;
                    }

                    // Hex integer or decimal integer
                    if (TryParseBigInteger(input, out var bigint))
                    {
                        var signedVal = bigint;

                        // Unsigned modulo 2^128 for Hex (two's-complement view)
                        var mod = (BigInteger.One << 128);
                        var unsigned128 = signedVal & (mod - 1);

                        txtOutDec.Text = signedVal.ToString(CultureInfo.InvariantCulture);
                        txtOutHex.Text = FormatHex(unsigned128, bigEndian);

                        // Floats from signed integer
                        float f32 = (float)(double)(decimal)signedVal;
                        double f64 = (double)signedVal;
        txtOutF32.Text = Float32Hex(f32, bigEndian);
                        txtOutF64.Text = Float64Hex(f64, bigEndian);
                        return;
                    }

                    // Otherwise parse as double
                    if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    {
                        txtOutDec.Text = FormatFloatLabel(d);
                        txtOutHex.Text = Float64Hex(d, bigEndian);
                        float f32 = (float)d;
                        txtOutF32.Text = Float32Hex(f32, bigEndian);
                        txtOutF64.Text = Float64Hex(d, bigEndian);
                        return;
                    }

                    ClearOutputs();
                }

                private static bool TryParseSpecialFloat(string text, out double dval)
                {
                    dval = 0;
                    string t = (text ?? "").Trim().ToLowerInvariant();
                    if (t == "inf" || t == "+inf" || t == "infinity" || t == "+infinity")
                    {
                        dval = double.PositiveInfinity; return true;
                    }
                    if (t == "-inf" || t == "-infinity")
                    {
                        dval = double.NegativeInfinity; return true;
                    }
                    if (t == "nan")
                    {
                        dval = double.NaN; return true;
                    }
                    return false;
                }

        }
    }
}
