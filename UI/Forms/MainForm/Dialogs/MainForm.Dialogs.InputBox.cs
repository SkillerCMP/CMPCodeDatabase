// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Dialogs/MainForm.Dialogs.InputBox.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────


namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private class InputBox : Form
                        {
                            private TextBox tb = new TextBox() { Dock = DockStyle.Top };
                            public string Value => tb.Text;
                            public InputBox(string title)
                            {
                                Text = title;
                                Width = 420; Height = 120; StartPosition = FormStartPosition.CenterParent;
                                var ok = new Button() { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
                                Controls.Add(tb);
                                Controls.Add(ok);
                                AcceptButton = ok;
                            }
                        }
    }
}
