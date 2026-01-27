using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    internal class AsciiHexAmountDialog : Form
    {
        private readonly bool _littleEndian;
        private readonly int? _maxChars;
        private TextBox txtInput;
        private Label lblInfo;
        private Label lblResult;
        private Button btnOK, btnCancel;

        public string ResultHex { get; private set; } = string.Empty;

        /// <param name="littleEndian">true for LITTLE (reverse bytes), false for BIG (no reversal)</param>
        /// <param name="titleLabel">Optional label to show in the title (e.g., "ASCII BIG" or "ASCII LITTLE")</param>
        /// <param name="initialText">Optional initial input text; if null/empty or ".....", starts blank</param>
        /// <param name="maxChars">Optional cap for number of characters; if null or <=0, unlimited</param>
        public AsciiHexAmountDialog(bool littleEndian, string titleLabel = null, string initialText = null, int? maxChars = null)
        {
            _littleEndian = littleEndian;
            _maxChars = maxChars;

            string label = titleLabel ?? (littleEndian ? "ASCII LITTLE" : "ASCII BIG");
            Text = $"Amount — {label}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(640, 220);
            MinimumSize = new Size(640, 220);

            lblInfo = new Label { Left = 12, Top = 14, AutoSize = true, Text = $"Input (text) → {label} hex bytes" };
            txtInput = new TextBox { Left = 12, Top = 40, Width = ClientSize.Width - 24, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            lblResult = new Label { Left = 12, Top = 75, AutoSize = true, Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold), Text = "Result: " };

            btnOK = new Button { Left = 292, Top = 115, Text = "OK", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12,4,12,4) };
            btnCancel = new Button { Left = 382, Top = 115, Text = "Cancel", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12,4,12,4) };

            btnOK.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            txtInput.TextChanged += (s, e) => Recompute();

            Controls.AddRange(new Control[] { lblInfo, txtInput, lblResult, btnOK, btnCancel });

            if (_maxChars.HasValue && _maxChars.Value > 0)
            {
                try { txtInput.MaxLength = _maxChars.Value; } catch { }
            }

            if (!string.IsNullOrWhiteSpace(initialText) && initialText != ".....")
            {
                txtInput.Text = initialText;
            }

            Recompute();
        }

        private void Recompute()
        {
            var s = txtInput.Text ?? string.Empty;
            var bytes = Encoding.ASCII.GetBytes(s);
            if (_littleEndian)
                bytes = bytes.Reverse().ToArray();
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("X2"));
            ResultHex = sb.ToString();
            lblResult.Text = $"Result: {ResultHex}";
        }
    }
}
