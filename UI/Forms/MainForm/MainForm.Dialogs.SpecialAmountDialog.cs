using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
            internal partial class SpecialAmountDialog : Form
            {
                private readonly string type;
                private readonly string endian;
                private readonly int byteSize;            // number of bytes allowed by the MOD tag
                private readonly string defaultRawHex;    // uppercase, no spaces

                private TextBox txtInput = new TextBox() { Dock = DockStyle.Top };
                private Label lblStatus = new Label() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
                private Label lblPreview = new Label() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
                private Label lblMeta = new Label() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
                private Button btnDefault = new Button() { Text = "Use Default", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 4, 14, 4) };
                private Button btnOK = new Button() { Text = "OK", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 4, 14, 4) };
                private Button btnCancel = new Button() { Text = "Cancel", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 4, 14, 4) };

                public string? ResultHex { get; private set; } // uppercase hex, exact byteSize*2 length
                public string? SelectedHex => ResultHex;


                private string _lastValidInput = "0";      // for auto-revert on overflow
                private bool _internalChange = false;      // guard to prevent recursion during revert

                public SpecialAmountDialog(string title, string defaultHex, string type, string endian, string? boxLabel = null)
                {
                    this.type = (type ?? "HEX").Trim().ToUpperInvariant();
                    this.endian = (endian ?? "BIG").Trim().ToUpperInvariant();
                    this.defaultRawHex = System.Text.RegularExpressions.Regex.Replace(defaultHex ?? "", "[^0-9A-Fa-f]", "").ToUpperInvariant();
                    this.byteSize = Math.Max(1, defaultRawHex.Length / 2);

                    
                    // Enforce proper byte size for floating types regardless of default hex length
                    switch (this.type)
                    {
                        case "FLOAT":
                        case "FLOAT32":
                            this.byteSize = 4;
                            break;
                        case "DOUBLE":
                        case "FLOAT64":
                            this.byteSize = 8;
                            break;
                    }
var label = (boxLabel ?? string.Empty).Trim('<','>',' '); // safety: remove <>
var caption = string.IsNullOrWhiteSpace(title) ? "Amount" : title;
if (!string.IsNullOrWhiteSpace(label)) caption += " " + label;
Text = caption;
                    InitializeSpecialAmountDialogLayout();

                    lblMeta.Text = $"Type: {this.type}   Endian: {this.endian}   Size: {byteSize} bytes";

                    // Default in DEC for all types (including floats)
                    string defDisplay = DisplayDefaultDec();
                    _lastValidInput = defDisplay;
                    txtInput.Text = defDisplay;
                    txtInput.SelectAll();

                    txtInput.TextChanged += (s, e) => ValidateLive();
                    ValidateLive();

                    btnDefault.Text = $"Use Default ({defDisplay})";
                    btnDefault.Click += (s, e) => { txtInput.Text = defDisplay; txtInput.SelectAll(); };

                    btnOK.Click += (s, e) => { 
                        try { 
                            var hex = ComputeHexFromInput(txtInput.Text?.Trim() ?? "");
                            ResultHex = hex;
                            DialogResult = DialogResult.OK; 
                        } catch (Exception ex) { MessageBox.Show(this, ex.Message.TrimStart('[').Replace("RANGE]", "Range"), "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    };
                    btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
                }
}
    }
}
