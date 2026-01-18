using System;
using System.Drawing;
using System.Windows.Forms;

namespace CMPCodeDatabase.SpecialMods
{
    internal class TimeStampDialog : Form
    {
        private DateTimePicker dtp;
        private CheckBox chk64;
        private Label lblLocal;
        private Label lblUtc;
        private Label lblDec;
        private Label lblHex;

        private Button btnNow;
        private Button btnCopyDec;
        private Button btnCopyHex;
        private Button btnOK;
        private Button btnCancel;

        public long ResultUnixSeconds { get; private set; }
        public string ResultHex { get; private set; } = string.Empty;
        public bool ResultIs64Bit { get; private set; }
        public string ResultLabel => ResultIs64Bit ? "Epoch 64" : "Epoch 32";

        public TimeStampDialog(bool start64Bit = false)
        {
            Text = "Epoch Timestamp → Hex";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 210);

            lblLocal = new Label { Left = 12, Top = 12, Width = 490, Text = "Local time:" };
            dtp = new DateTimePicker
            {
                Left = 12,
                Top = 34,
                Width = 380,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dddd, MMMM d, yyyy h:mm:ss tt",
                Value = DateTime.Now
            };

            btnNow = new Button { Left = 402, Top = 33, Width = 90, Text = "Now" };
            btnNow.Click += (s, e) => { dtp.Value = DateTime.Now; Recompute(); };

            chk64 = new CheckBox { Left = 12, Top = 66, Width = 200, Text = "64-bit (X16)", Checked = start64Bit };

            lblUtc = new Label { Left = 12, Top = 92, Width = 490, Text = "UTC: -" };
            lblDec = new Label { Left = 12, Top = 114, Width = 490, Text = "Epoch (dec): -" };
            lblHex = new Label { Left = 12, Top = 136, Width = 490, Text = "Hex: -" , Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold) };

            btnCopyDec = new Button { Left = 12, Top = 164, Width = 110, Text = "Copy Dec" };
            btnCopyHex = new Button { Left = 128, Top = 164, Width = 110, Text = "Copy Hex" };

            btnOK = new Button { Left = 322, Top = 164, Width = 80, Text = "OK" };
            btnCancel = new Button { Left = 412, Top = 164, Width = 80, Text = "Cancel" };

            btnCopyDec.Click += (s, e) =>
            {
                try { Clipboard.SetText(ResultUnixSeconds.ToString()); } catch { }
            };

            btnCopyHex.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(ResultHex))
                {
                    try { Clipboard.SetText(ResultHex); } catch { }
                }
            };

            btnOK.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            dtp.ValueChanged += (s, e) => Recompute();
            chk64.CheckedChanged += (s, e) => Recompute();

            Controls.AddRange(new Control[]
            {
                lblLocal, dtp, btnNow, chk64, lblUtc, lblDec, lblHex,
                btnCopyDec, btnCopyHex, btnOK, btnCancel
            });

            Recompute();
        }

        private void Recompute()
        {
            ResultIs64Bit = chk64.Checked;

            // Interpret DateTimePicker's value as LOCAL time and convert to epoch seconds
            var local = dtp.Value;
            var dtoLocal = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local));
            var dtoUtc = dtoLocal.ToUniversalTime();

            ResultUnixSeconds = dtoUtc.ToUnixTimeSeconds();

            lblUtc.Text = $"UTC: {dtoUtc:dddd, MMMM d, yyyy h:mm:ss tt}";
            lblDec.Text = $"Epoch (dec): {ResultUnixSeconds}";

            if (EpochUtil.TryFormatHexSeconds(ResultUnixSeconds, ResultIs64Bit, out var hex, out var err))
            {
                ResultHex = hex;
                lblHex.Text = $"Hex: {ResultHex}";
                btnOK.Enabled = true;
            }
            else
            {
                ResultHex = string.Empty;
                lblHex.Text = $"Hex: (error) {err}";
                btnOK.Enabled = false;
            }
        }
    }
}
