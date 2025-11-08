using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private static bool _shownPnachHelpThisSession;

        private static string PnachHelpPrefsDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CMPCodeDatabase");
        private static string PnachHelpFlagFile =>
            Path.Combine(PnachHelpPrefsDir, "hide_pnach_help.flag");

        private static bool IsPnachHelpHiddenForever()
        {
            try { return File.Exists(PnachHelpFlagFile); } catch { return false; }
        }

        private static void SetPnachHelpHiddenForever(bool hide)
        {
            try
            {
                if (hide)
                {
                    Directory.CreateDirectory(PnachHelpPrefsDir);
                    File.WriteAllText(PnachHelpFlagFile, DateTime.UtcNow.ToString("o"));
                }
                else
                {
                    if (File.Exists(PnachHelpFlagFile)) File.Delete(PnachHelpFlagFile);
                }
            }
            catch { /* best-effort only */ }
        }

        /// <summary>
        /// Shows a friendly HTML pop-up explaining how to create/use a PCSX2 .pnach file.
        /// Shown once per app session by default. If the user opted 'Don't ever show again',
        /// it will never be shown unless the flag file is deleted.
        /// </summary>
        private void EnsurePnachHelpOnce()
        {
            if (IsPnachHelpHiddenForever()) return;       // persistent opt-out
            if (_shownPnachHelpThisSession) return;       // session opt-out
            _shownPnachHelpThisSession = true;
            ShowPnachHelpHtml();                          // show now
        }

        /// <summary>
        /// Always show the help (ignores session flag, but still respects the 'ever' flag).
        /// </summary>
        private void ShowPnachHelpHtml()
        {
            if (IsPnachHelpHiddenForever()) return;       // respect persistent opt-out

            // Basic WinForms HTML host using WebBrowser for broad compatibility.
            var html = GetPnachHelpHtml();
            using (var dlg = new Form())
            using (var wb = new WebBrowser())
            using (var ok = new Button())
            using (var chkSession = new CheckBox())
            using (var chkForever = new CheckBox())
            {
                dlg.Text = "PCSX2 .pnach — How to create and use cheats";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = true;
                dlg.ShowIcon = false;
                dlg.TopMost = false;
                dlg.FormBorderStyle = FormBorderStyle.Sizable;
                dlg.ClientSize = new Size(820, 640);

                wb.AllowWebBrowserDrop = false;
                wb.IsWebBrowserContextMenuEnabled = false;
                wb.WebBrowserShortcutsEnabled = true;
                wb.ScriptErrorsSuppressed = true;
                wb.Dock = DockStyle.Fill;

                ok.Text = "OK";
                ok.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                ok.Size = new Size(100, 28);
                ok.Location = new Point(dlg.ClientSize.Width - ok.Width - 16, dlg.ClientSize.Height - ok.Height - 12);
                ok.Click += (s, e) =>
                {
                    // Apply user choices on close
                    if (chkForever.Checked) SetPnachHelpHiddenForever(true);
                    _shownPnachHelpThisSession = chkSession.Checked || chkForever.Checked;
                    dlg.DialogResult = DialogResult.OK;
                };

                chkSession.Text = "Don’t show again this session";
                chkSession.AutoSize = true;
                chkSession.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
                chkSession.Location = new Point(12, dlg.ClientSize.Height - ok.Height - 10);

                chkForever.Text = "Don’t ever show again";
                chkForever.AutoSize = true;
                chkForever.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
                chkForever.Location = new Point(12, dlg.ClientSize.Height - ok.Height - 32);

                // Layout panel for bottom row
                var bottom = new Panel { Dock = DockStyle.Bottom, Height = 52 };
                bottom.Controls.Add(ok);
                bottom.Controls.Add(chkSession);
                bottom.Controls.Add(chkForever);

                dlg.Controls.Add(wb);
                dlg.Controls.Add(bottom);

                // Load HTML
                try { wb.DocumentText = html; } catch { /* ignore */ }

                dlg.ShowDialog(this);
            }
        }

        private string GetPnachHelpHtml()
        {
            // Inline CSS kept simple for legacy IE engine used by WebBrowser.
            return @"<!doctype html><html><head><meta charset='utf-8'>
<style>
body{font-family:Segoe UI,Arial,sans-serif;margin:16px;line-height:1.45}
h1{font-size:20px;margin:0 0 8px 0}
h2{font-size:16px;margin:18px 0 8px 0}
ol,ul{margin:6px 0 10px 20px}
code,pre{font-family:Consolas,monospace;background:#f5f5f5;padding:2px 4px;border-radius:3px}
.tip{background:#eef9ff;border:1px solid #cfefff;padding:8px;border-radius:6px;margin:10px 0}
.small{color:#555;font-size:12px}
hr{border:none;border-top:1px solid #ddd;margin:14px 0}
</style></head><body>
  <h1>PCSX2 <code>.pnach</code> — How to create &amp; use cheats</h1>
  <p>This guide explains the easiest way to create the correct <code>.pnach</code> file and enable cheats in PCSX2.</p>

  <h2>Step 1 — Let PCSX2 create the file for your game</h2>
  <ol>
    <li>Open <strong>PCSX2</strong> and <strong>start the game</strong> you want to add cheats to.</li>
    <li>Open <strong>Tools → Edit Cheats</strong> (or from the Library: right‑click the game → <strong>Properties</strong> → <strong>Cheats</strong> → <strong>Edit/Open</strong>).</li>
    <li>PCSX2 will prompt to create a cheat file for this game. Click <em>Create</em>. This ensures the filename and location match what PCSX2 expects.</li>
  </ol>

  <div class='tip'><strong>Tip:</strong> You can either rename a file you exported from the Collector to match the PCSX2 one, or simply <strong>copy your cheat lines</strong> into the file PCSX2 created, then <strong>save</strong>.</div>

  <h2>Step 2 — Add your codes</h2>
  <ul>
    <li><strong>Copy/Paste:</strong> From the Collector, copy the <code>patch=1,EE,...</code> lines (or the converted address/value pairs) and paste into the PCSX2 cheat file.</li>
    <li><strong>Or Export:</strong> Use <em>Options → Export To → PCSX2 → .pnach</em> to save a file, then replace the one PCSX2 created.</li>
  </ul>

  <h2>Step 3 — Enable cheats</h2>
  <ul>
    <li>From the Library, right‑click the game → <strong>Properties</strong> → <strong>Cheats</strong> → check <strong>Enable Cheats</strong>.</li>
    <li>Depending on version, you may also see a global toggle under <strong>System → Enable Cheats</strong>.</li>
  </ul>

  <h2>Step 4 — Toggle individual cheats (optional)</h2>
  <p>PCSX2 will list the cheats it loaded from your file; you can switch them on/off inside the app.</p>

  <h2>Notes</h2>
  <ul class='small'>
    <li>Each line uses the format <code>patch=1,EE,AAAAAAAA,extended,BBBBBBBB</code>. Comment lines can start with <code>//</code>.</li>
    <li>If you edit the file while the game is running, you may need to reload cheats or restart the game.</li>
  </ul>

  <hr>
  <p class='small'>This message appears before <strong>Preview .pnach</strong> or <strong>Export → PCSX2</strong>. You can hide it for the rest of this session or permanently.</p>
</body></html>";
        }
    }
}
