// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/PatchLogForm.cs
// Purpose: Small log window used by the patch runner to display stdout/stderr.
// Notes:
//  • Formatting/cleanup only (no behavioral changes).
// Added: 2026-01-28
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Windows.Forms;
using CMPCodeDatabase.Patching;

namespace CMPCodeDatabase
{
    public partial class PatchLogForm : Form, IPatchLogSink
    {
        private readonly TextBox _txt = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            WordWrap = false
        };

        public PatchLogForm()
        {
            Text = "Patch Log";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(6),
                FlowDirection = FlowDirection.LeftToRight
            };

            var btnClear = new Button { Text = "Clear" };
            var btnSave = new Button { Text = "Save..." };

            btnClear.Click += (_, _) => _txt.Clear();
            btnSave.Click += (_, _) =>
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = "patch-log.txt"
                };

                if (sfd.ShowDialog(this) == DialogResult.OK)
                    File.WriteAllText(sfd.FileName, _txt.Text);
            };

            panel.Controls.Add(btnClear);
            panel.Controls.Add(btnSave);

            Controls.Add(_txt);
            Controls.Add(panel);
        }

        public void Write(string text)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Write), text);
                return;
            }

            _txt.AppendText(text);
        }

        public void WriteLine(string text)
        {
            Write(text);
            Write(Environment.NewLine);
        }
    }
}
