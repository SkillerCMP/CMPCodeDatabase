// Clean replacement to fix CS1519 and keep buttons DPI/TextSize-safe.
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class TextAmountDialog : Form
    {
        private readonly Encoding _encoding;
        private readonly int _maxBytes;
        private readonly string _encodingToken;

        private readonly Label lblPrompt = new Label();
        private readonly TextBox txtInput = new TextBox();
        private readonly Label lblCount = new Label();
        private readonly Button btnCancel = new Button();
        private readonly Button btnOK = new Button();
        private readonly Button btnUseDefault = new Button();

        private readonly string defaultTextForUseButton = string.Empty;

        public string ResultText { get; private set; } = string.Empty;

        // Primary ctor used by callers
        public TextAmountDialog(string encToken, int maxBytes)
        {
            _encodingToken = NormalizeEncodingToken(encToken);
            _encoding = MapEncodingToken(_encodingToken);
            _maxBytes = Math.Max(0, maxBytes);
            InitializeUi();
        }

        // Overload with default-text button
        public TextAmountDialog(string encToken, int maxBytes, string defaultText)
            : this(encToken, maxBytes)
        {
            defaultTextForUseButton = defaultText ?? string.Empty;

            // Keep button label reasonable; show full default text only if it is short.
            var preview = defaultTextForUseButton.Trim();
            if (preview.Length > 24) preview = preview.Substring(0, 24) + "…";

            btnUseDefault.Text = string.IsNullOrWhiteSpace(defaultTextForUseButton)
                ? "Use Default"
                : $"Use Default ({preview})";

            btnUseDefault.Enabled = !string.IsNullOrEmpty(defaultTextForUseButton);
        }

        private void InitializeUi()
        {
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;

            ClientSize = new Size(720, 220);
            MinimumSize = new Size(720, 220);

            lblPrompt.AutoSize = true;
            lblPrompt.Text = $"Enter text ({_encodingToken}), max {_maxBytes} bytes";
            lblPrompt.Location = new Point(12, 12);

            txtInput.Location = new Point(12, 36);
            txtInput.Width = 492;
            txtInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInput.TextChanged += (_, __) => UpdateCounterAndValidate();

            lblCount.AutoSize = true;
            lblCount.Location = new Point(12, 66);
            lblCount.ForeColor = Color.Black;

            // Buttons row (AutoSize so Accessibility "Text size" won't clip)
            btnCancel.Text = "Cancel";
            btnCancel.AutoSize = true;
            btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCancel.Padding = new Padding(14, 4, 14, 4);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancel.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            btnOK.Text = "OK";
            btnOK.AutoSize = true;
            btnOK.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnOK.Padding = new Padding(14, 4, 14, 4);
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnOK.Click += (_, __) =>
            {
                ResultText = txtInput.Text ?? string.Empty;
                DialogResult = DialogResult.OK;
                Close();
            };

            btnUseDefault.Text = "Use Default";
            btnUseDefault.AutoSize = true;
            btnUseDefault.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnUseDefault.Padding = new Padding(14, 4, 14, 4);
            btnUseDefault.AutoEllipsis = true;
            btnUseDefault.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnUseDefault.Click += (_, __) =>
            {
                if (!string.IsNullOrEmpty(defaultTextForUseButton))
                {
                    ResultText = defaultTextForUseButton;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            Controls.Add(lblPrompt);
            Controls.Add(txtInput);
            Controls.Add(lblCount);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(btnUseDefault);

            // Position once after layout (handles high Text size / DPI)
            Shown += (_, __) => RepositionButtonsForDpi();
            Resize += (_, __) => RepositionButtonsForDpi();

            UpdateCounterAndValidate();
        }

        private void RepositionButtonsForDpi()
        {
            int pad = 12;

            // Bottom row Y
            int y = ClientSize.Height - btnOK.Height - pad;

            btnCancel.Top = y;
            btnOK.Top = y;
            btnUseDefault.Top = y;

            // Right-align: [Use Default] [OK] [Cancel]
            int x = ClientSize.Width - pad;

            btnCancel.Left = x - btnCancel.Width;
            x = btnCancel.Left - 8;

            btnOK.Left = x - btnOK.Width;
            x = btnOK.Left - 8;

            // Give Use Default any remaining space but keep a minimum left padding.
            btnUseDefault.Left = Math.Max(pad, x - btnUseDefault.Width);

            // Expand input width to match dialog width (keeps it neat when dialog is wider due to font scaling)
            txtInput.Width = ClientSize.Width - 24;
        }

        private void UpdateCounterAndValidate()
        {
            var text = txtInput.Text ?? string.Empty;
            int bytes = _encoding.GetByteCount(text);

            lblCount.Text = $"{bytes} / {_maxBytes} bytes";
            bool tooLong = bytes > _maxBytes && _maxBytes > 0;

            lblCount.ForeColor = tooLong ? Color.DarkRed : Color.Black;
            btnOK.Enabled = !tooLong;
        }

        private static string NormalizeEncodingToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "UTF08";
            return token.Trim().ToUpperInvariant().Replace("-", "");
        }

        private static Encoding MapEncodingToken(string token)
        {
            switch (token)
            {
                case "UTF08":
                case "UTF8":
                    return Encoding.UTF8;
                case "UTF16":
                case "UTF16LE":
                    return Encoding.Unicode;
                case "UTF16BE":
                    return Encoding.BigEndianUnicode;
                case "ASCII":
                    return Encoding.ASCII;
                default:
                    return Encoding.UTF8;
            }
        }
    }
}
