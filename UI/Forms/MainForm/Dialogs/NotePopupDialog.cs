
using System;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public sealed class NotePopupDialog : Form
    {
        private readonly WebBrowser browser = new WebBrowser();
        private readonly CheckBox chkSuppress = new CheckBox();
        private readonly Button btnContinue = new Button();
        private readonly Button btnDontUse = new Button();

        public bool Suppress => chkSuppress.Checked;

        public NotePopupDialog(string title, string htmlFragment)
        {
            Text = string.IsNullOrWhiteSpace(title) ? "Note" : $"Note — {title}";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = MaximizeBox = false;
            ShowInTaskbar = false;
            Width = 760; Height = 520;

            browser.Dock = DockStyle.Fill;
            browser.AllowWebBrowserDrop = false;
            browser.IsWebBrowserContextMenuEnabled = true;
            browser.ScriptErrorsSuppressed = true;
            browser.DocumentText = WrapHtml(htmlFragment ?? string.Empty);

            chkSuppress.Text = "Don't show this note again (this session)";
            chkSuppress.AutoSize = true;
            chkSuppress.Dock = DockStyle.Top;
            chkSuppress.Padding = new Padding(10,6,10,6);

            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10),
                AutoSize = true
            };

            btnContinue.Text = "Continue";
            btnContinue.DialogResult = DialogResult.OK;
            btnDontUse.Text = "Don't Use";
            btnDontUse.DialogResult = DialogResult.Cancel;

            panelButtons.Controls.Add(btnContinue);
            panelButtons.Controls.Add(btnDontUse);

            Controls.Add(browser);
            Controls.Add(panelButtons);
            Controls.Add(chkSuppress);

            AcceptButton = btnContinue;
            CancelButton = btnDontUse;
        }

        private static string WrapHtml(string inner)
        {
            if (string.IsNullOrWhiteSpace(inner)) inner = "(empty)";
            if (inner.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0) return inner;
            string css = @"<style>
                body { font-family: Segoe UI, Arial, sans-serif; font-size: 14px; padding: 16px; color: #222; background:#fff; }
                h1,h2,h3 { margin: 12px 0 6px; }
                p { margin: 6px 0; }
                code, pre { background: #f7f7f7; padding: 2px 4px; border-radius: 4px; }
                hr { border: 0; border-top: 1px solid #ddd; margin: 12px 0; }
            </style>";
            return "<html><head>" + css + "</head><body>" + inner + "</body></html>";
        }
    }
}
