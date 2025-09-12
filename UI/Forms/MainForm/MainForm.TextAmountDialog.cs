// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.TextAmountDialog.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System.Text;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        internal class TextAmountDialog : Form
        {
            private readonly int maxBytes;
            private readonly Encoding enc;
            private readonly string encTokenDisplay;
            private readonly Label lbl = new Label() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0,8,0,4) };
            private readonly TextBox txt = new TextBox() { Dock = DockStyle.Top };
            private readonly Label lblCount = new Label() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0,0,0,6) };
            private readonly Button ok = new Button() { Text = "OK", Dock = DockStyle.Right, Width = 80, Margin = new Padding(4) };
            private readonly Button cancel = new Button() { Text = "Cancel", Dock = DockStyle.Right, Width = 80, Margin = new Padding(4) };

            public string? ResultText { get; private set; }

            public TextAmountDialog(string encToken, int maxBytes)
            {
                this.maxBytes = maxBytes;
                this.encTokenDisplay = NormalizeEncodingToken(encToken);
                this.enc = MapEncodingToken(encToken);

                Text = $"Add Item — Amount:{(maxBytes==int.MaxValue ? "NA" : new string('9', maxBytes))}:{encTokenDisplay}:TXT";
                Width = 560; Height = 180; StartPosition = FormStartPosition.CenterParent;

                var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(10) };
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                lbl.Text = maxBytes == int.MaxValue ? $"Enter text ({encTokenDisplay})" : $"Enter text ({encTokenDisplay}), max {maxBytes} bytes";
                panel.Controls.Add(lbl, 0, 0); panel.SetColumnSpan(lbl, 2);

                panel.Controls.Add(txt, 0, 1); panel.SetColumnSpan(txt, 2);

                panel.Controls.Add(lblCount, 0, 2); panel.SetColumnSpan(lblCount, 2);

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
                btnPanel.Controls.Add(cancel); btnPanel.Controls.Add(ok);
                panel.Controls.Add(btnPanel, 0, 3); panel.SetColumnSpan(btnPanel, 2);

                ok.Click += (s,e) => { ResultText = txt.Text ?? string.Empty; DialogResult = DialogResult.OK; };
                cancel.Click += (s,e) => { DialogResult = DialogResult.Cancel; };

                txt.TextChanged += OnTextChangedInternal;
                Shown += (s,e) => { UpdateCountLabel(); txt.SelectionStart = txt.Text.Length; };

                Controls.Add(panel);
                AcceptButton = ok; CancelButton = cancel;
            }

            private void OnTextChangedInternal(object? sender, EventArgs e) => EnforceMaxBytes();

            private void EnforceMaxBytes()
            {
                if (maxBytes == int.MaxValue) { UpdateCountLabel(); return; }
                var t = txt.Text ?? string.Empty;
                var bytes = enc.GetBytes(t);
                if (bytes.Length <= maxBytes) { UpdateCountLabel(bytes.Length); return; }

                int lo = 0, hi = t.Length;
                while (lo < hi)
                {
                    int mid = (lo + hi + 1) / 2;
                    if (enc.GetByteCount(t.Substring(0, mid)) <= maxBytes) lo = mid; else hi = mid - 1;
                }
                var trimmed = t.Substring(0, lo);
                txt.TextChanged -= OnTextChangedInternal;
                txt.Text = trimmed;
                txt.SelectionStart = trimmed.Length;
                txt.SelectionLength = 0;
                txt.TextChanged += OnTextChangedInternal;
                UpdateCountLabel(enc.GetByteCount(trimmed));
            }

            private void UpdateCountLabel(int? currentBytes = null)
            {
                if (currentBytes is null) currentBytes = enc.GetByteCount(txt.Text ?? string.Empty);
                if (maxBytes == int.MaxValue)
                    lblCount.Text = $"{currentBytes} bytes";
                else
                    lblCount.Text = $"{currentBytes} / {maxBytes} bytes";
            }
        }

        private static System.Text.Encoding MapEncodingToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return System.Text.Encoding.UTF8;
            var norm = token.Trim().ToUpperInvariant().Replace("-", "");
            return norm switch
            {
                "UTF08" or "UTF8" => System.Text.Encoding.UTF8,
                "UTF16" or "UTF16LE" => System.Text.Encoding.Unicode,
                "UTF16BE" => System.Text.Encoding.BigEndianUnicode,
                "ASCII" => System.Text.Encoding.ASCII,
                _ => System.Text.Encoding.UTF8,
            };
        }

        private static string NormalizeEncodingToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "UTF08";
            var norm = token.Trim().ToUpperInvariant().Replace("-", "");
            return norm switch
            {
                "UTF8" or "UTF08" => "UTF08",
                "UTF16" or "UTF16LE" => "UTF16LE",
                "UTF16BE" => "UTF16BE",
                "ASCII" => "ASCII",
                _ => token.Trim()
            };
        }
    }
}
