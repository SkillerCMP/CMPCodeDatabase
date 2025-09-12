// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Dialogs/MainForm.Dialogs.NotesViewerForm.cs
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
        private class NotesViewerForm : Form
                        {
                            private WebBrowser browser = new WebBrowser();
                            public NotesViewerForm(string title, string htmlFragment)
                            {
                                Text = "Notes - " + (title ?? "");
                                Width = 760;
                                Height = 680;
                                browser.Dock = DockStyle.Fill;
                                Controls.Add(browser);
                                browser.DocumentText = WrapHtml(htmlFragment ?? "");
                            }

                            private string WrapHtml(string inner)
                            {
                                if (!string.IsNullOrEmpty(inner) && inner.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0)
                                    return inner;
                                string css = @"<style>
                                    body { font-family: Segoe UI, Arial; font-size: 14px; padding: 16px; color: #222; background:#fff; }
                                    h1,h2,h3 { margin: 12px 0 6px; }
                                    p { margin: 6px 0; }
                                    code, pre { background: #f7f7f7; padding: 2px 4px; border-radius: 4px; }
                                    hr { border: 0; border-top: 1px solid #ddd; margin: 12px 0; }
                                </style>";
                                return "<html><head>" + css + "</head><body>" + inner + "</body></html>";
                            }
                        }
    }
}
