// Clean replacement to fix CS1520 and add "Use Default" support for TXT dialog.
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
            btnUseDefault.Text = string.IsNullOrWhiteSpace(defaultTextForUseButton)
                ? "Use Default"
                : $"Use Default ({defaultTextForUseButton})";
            btnUseDefault.Enabled = !string.IsNullOrEmpty(defaultTextForUseButton);
        }

        private void InitializeUi()
        {
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(520, 170);

            lblPrompt.AutoSize = true;
            lblPrompt.Text = $"Enter text ({_encodingToken}), max {_maxBytes} bytes";
            lblPrompt.Location = new Point(12, 12);

            txtInput.Location = new Point(12, 36);
            txtInput.Width = 492;
            txtInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInput.TextChanged += (s, e) => UpdateCounterAndValidate();

            lblCount.AutoSize = true;
            lblCount.Location = new Point(12, 66);
            lblCount.ForeColor = Color.Black;

            // Buttons row
            btnCancel.Text = "Cancel";
            btnCancel.Width = 120;
            btnCancel.Location = new Point(12, 110);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            btnOK.Text = "OK";
            btnOK.Width = 120;
            btnOK.Location = new Point(142, 110);
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnOK.Click += (s, e) =>
            {
                this.ResultText = txtInput.Text ?? string.Empty;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            btnUseDefault.Text = "Use Default";
            btnUseDefault.Width = 240;
            btnUseDefault.Location = new Point(272, 110);
            btnUseDefault.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnUseDefault.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(defaultTextForUseButton))
                {
                    this.ResultText = defaultTextForUseButton;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            this.Controls.Add(lblPrompt);
            this.Controls.Add(txtInput);
            this.Controls.Add(lblCount);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnUseDefault);

            UpdateCounterAndValidate();
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
