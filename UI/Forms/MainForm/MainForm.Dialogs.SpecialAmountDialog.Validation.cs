// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.Dialogs.SpecialAmountDialog.Validation.cs
// Purpose: Special Amount dialog live validation and preview formatting helpers.
// Notes:
//  • Split from MainForm.Dialogs.SpecialAmountDialog.Conversion.cs during cleanup pass 23.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        internal partial class SpecialAmountDialog
        {
                private static string GroupHexPairs(string hex)
                {
                    if (string.IsNullOrWhiteSpace(hex)) return "";
                    var clean = System.Text.RegularExpressions.Regex.Replace(hex, "[^0-9A-Fa-f]", "").ToUpperInvariant();
                    if (clean.Length % 2 != 0) clean = "0" + clean;
                    var parts = new System.Collections.Generic.List<string>(clean.Length / 2);
                    for (int i = 0; i < clean.Length; i += 2)
                        parts.Add(clean.Substring(i, 2));
                    return string.Join(" ", parts);
                }

                private void ValidateLive()
                {
                    if (_internalChange) return;

                    string input = txtInput.Text?.Trim() ?? "";
                    try
                    {
                        string hex = ComputeHexFromInput(input);
                        lblStatus.Text = "Value is valid";
                        lblStatus.ForeColor = System.Drawing.Color.ForestGreen;
                        lblPreview.Text = "Hex: " + GroupHexPairs(hex);
                        btnOK.Enabled = true;
                        _lastValidInput = input;
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message ?? "Invalid value.";
                        lblStatus.Text = msg.TrimStart('[').Replace("RANGE]", "Range");
                        lblStatus.ForeColor = System.Drawing.Color.Firebrick;
                        lblPreview.Text = "Hex: —";
                        btnOK.Enabled = false;

                        // Only auto-revert for hard RANGE violations (over size) while user is typing a DEC/float
                        bool isRange = msg.StartsWith("[RANGE]");
                        if (isRange && _lastValidInput is not null)
                        {
                            _internalChange = true;
                            try
                            {
                                int caret = txtInput.SelectionStart;
                                txtInput.Text = _lastValidInput;
                                txtInput.SelectionStart = Math.Min(_lastValidInput.Length, Math.Max(0, caret - 1));
                                txtInput.SelectionLength = 0;
                                try { System.Media.SystemSounds.Beep.Play(); } catch { }
                            }
                            finally
                            {
                                _internalChange = false;
                            }
                        }
                    }
                }
        }
    }
}
