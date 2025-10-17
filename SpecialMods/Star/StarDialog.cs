using System;
using System.Drawing;
using System.Windows.Forms;

namespace CMPCodeDatabase.SpecialMods
{
    /// <summary>
    /// Minimal STAR dialog: subtype (from tag), one decimal input, and live hex preview.
    /// </summary>
    internal class StarDialog : Form
    {
        private readonly string _type;
        private Label lblType;
        private NumericUpDown numDec;
        private Label lblResult;
        private Button btnOK, btnCancel;

        public string ResultHex { get; private set; } = "00000000";
        public string ResultLabel { get; private set; } = "H4V 0";

        public StarDialog(string type)
        {
            _type = string.IsNullOrWhiteSpace(type) ? "H4V" : type.ToUpperInvariant();

            Text = "STAR — Encode Value";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 150);

            lblType = new Label { Left = 12, Top = 14, Width = 180, Text = $"Type: {_type}" };
            numDec = new NumericUpDown { Left = 12, Top = 40, Width = 160, Minimum = 0, Maximum = 4294967295, DecimalPlaces = 0 };
            if (_type == "LVL") numDec.Maximum = 65535;
            lblResult = new Label { Left = 190, Top = 42, Width = 170, Text = "Result: 00000000", Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold) };

            btnOK = new Button { Left = 182, Top = 90, Width = 80, Text = "OK" };
            btnCancel = new Button { Left = 272, Top = 90, Width = 80, Text = "Cancel" };

            btnOK.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            numDec.ValueChanged += (s, e) => Recompute();

            Controls.AddRange(new Control[] { lblType, numDec, lblResult, btnOK, btnCancel });

            numDec.Value = _type == "LVL" ? 1 : 120;
            Recompute();
        }

        private void Recompute()
        {
            uint dec = (uint)numDec.Value;
            ResultHex = StarCalculator.Encode(_type, dec);
            ResultLabel = $"{_type} {dec}";
            lblResult.Text = $"Result: {ResultHex}";
        }
    }
}
