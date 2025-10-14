// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.LogUI.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Adds a bottom-docked live log panel to the Collector and relayouts buttons:
    /// - Moves "Copy Checked / Copy All / Clear" to top-right beside Select All/None
    /// - Hides "Invert"
    /// - Provides AppendLog/ClearLog helpers
    /// - Rewires "Run Patch (Checked/All)" to a streaming runner
    /// </summary>
    public partial class CollectorForm : Form
    {
        private Panel? _logPanel;
        private RichTextBox? _rtbLog;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
                        this.Padding = new Padding(0);
EnsureLogPanel();
            RelayoutButtons();
            WireStreamingRunButtons();
            try { MoveRunAndPatcherBarsIntoLogPanel(); } catch { }
            try { RenameRunButtons(); } catch { }
        
            try { TryInitPatchUI(); } catch { }
}

        private void EnsureLogPanel()
        {
            if (_logPanel != null && !_logPanel.IsDisposed) return;

            _logPanel = new Panel { Dock = DockStyle.Bottom, Height = 200, Padding = new Padding(8, 6, 8, 0) , Margin = new Padding(0)};
            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 5 , Margin = new Padding(0)};
            var btnClearLog = new Button { Text = "Clear Log", Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Width = 100, Height = 24 };
            _rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = false,
                DetectUrls = false,
                Font = new Font("Consolas", 9.0f),
                BackColor = Color.White
            };

            bottomBar.Controls.Add(btnClearLog);
            btnClearLog.Top = 3;
            bottomBar.Resize += (s, _) => btnClearLog.Left = bottomBar.Width - btnClearLog.Width - 8;
            btnClearLog.Left = bottomBar.Width - btnClearLog.Width - 8;
            btnClearLog.Click += (s, _) => ClearLog();

            _logPanel.Controls.Add(_rtbLog);
            _logPanel.Controls.Add(bottomBar);
            Controls.Add(_logPanel);
                        // _logPanel.BringToFront(); // removed to prevent overlay over Fill
// _logPanel.BringToFront(); // removed to prevent overlay over Fill
        }

        private void RelayoutButtons()
        {
            // Find the ops panel with Select All/None/Invert
            Panel? opsPanel = Controls.OfType<Panel>().FirstOrDefault(p =>
                p.Controls.OfType<Button>().Any(b => b.Text == "Select All") &&
                p.Controls.OfType<Button>().Any(b => b.Text == "Select None"));

            // Find the bottom button flow with Copy/Clear
            Panel? bottomButtons = Controls.OfType<Panel>().FirstOrDefault(p =>
                p.Controls.OfType<Button>().Any(b => b.Text == "Copy Checked") &&
                p.Controls.OfType<Button>().Any(b => b.Text == "Copy All"));

            if (opsPanel != null)
            {
                // Hide "Invert"
                var invert = opsPanel.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Invert");
                if (invert != null) invert.Visible = false;

                if (bottomButtons != null)
                {
                    var moving = bottomButtons.Controls.OfType<Button>()
                        .Where(b => b.Text is "Copy Checked" or "Copy All" or "Clear").ToList();

                    foreach (var b in moving)
                    {
                        bottomButtons.Controls.Remove(b);
                        b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                        opsPanel.Controls.Add(b);
                    }

                    bottomButtons.Visible = false;
                    bottomButtons.Height = 0;
                }

                // Right-align on ops panel
                var order = new[] { "Select All", "Select None", "Copy Checked", "Copy All", "Clear" };
                var btns = opsPanel.Controls.OfType<Button>()
                    .Where(b => order.Contains(b.Text)).ToList();

                opsPanel.Resize += (s, _) => LayoutButtonsRightAligned(opsPanel, btns, order);
                LayoutButtonsRightAligned(opsPanel, btns, order);
            }
        }

        private static void LayoutButtonsRightAligned(Panel opsPanel, System.Collections.Generic.List<Button> btns, string[] order)
        {
            int x = opsPanel.Width - 8;
            const int gap = 6;
            foreach (var text in order.Reverse())
            {
                var b = btns.FirstOrDefault(bb => bb.Text == text);
                if (b == null) continue;
                b.Top = 6;
                b.Width = Math.Max(90, b.PreferredSize.Width + 10);
                x -= b.Width; b.Left = x;
                x -= gap;
            }
        }

        private void WireStreamingRunButtons()
        {
            // Replace the original buttons so we remove old anonymous handlers
            ReplaceButtonByText("Run Patch (Checked)", (s, e) => RunPatchStreaming(true));
            ReplaceButtonByText("Run Patch (All)", (s, e) => RunPatchStreaming(false));
        }

        private void ReplaceButtonByText(string text, EventHandler handler)
        {
            var btn = FindControlByText<Button>(this, text);
            if (btn == null || btn.Parent == null) return;

            var parent = btn.Parent;
            var bounds = btn.Bounds;
            var anchor = btn.Anchor;
            var idx = parent.Controls.GetChildIndex(btn);

            // Remove old and insert new
            parent.Controls.Remove(btn);
            btn.Dispose();

            var b2 = new Button { Text = text, Bounds = bounds, Anchor = anchor };
            b2.Click += handler;
            parent.Controls.Add(b2);
            parent.Controls.SetChildIndex(b2, idx);
        }

        private static T? FindControlByText<T>(Control root, string text) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c is T t && t.Text == text) return t;
                var nested = FindControlByText<T>(c, text);
                if (nested != null) return nested;
            }
            return null;
        }

        private void AppendLog(string text)
        {{
    if (_rtbLog == null || _rtbLog.IsDisposed) return;
    if (InvokeRequired) { BeginInvoke((Action)(() => AppendLog(text))); return; }

    // Choose color + update status purely from log text (no OK handling)
    var color = ClassifyLogAndUpdateStatus(text);

    // append with color
    int start = _rtbLog.TextLength;
    _rtbLog.SelectionStart = start;
    _rtbLog.SelectionLength = 0;
    _rtbLog.SelectionColor = color;

    _rtbLog.AppendText(text);
    if (!text.EndsWith(Environment.NewLine)) _rtbLog.AppendText(Environment.NewLine);

    // cap to last ~5000 lines
    if (_rtbLog.Lines.Length > 5000)
    {
        var keep = _rtbLog.Lines.Skip(_rtbLog.Lines.Length - 5000).ToArray();
        _rtbLog.Lines = keep;
    }

    _rtbLog.SelectionStart = _rtbLog.TextLength;
    _rtbLog.SelectionColor = SystemColors.WindowText; // reset
    _rtbLog.ScrollToCaret();
}


        }

        private void ClearLog()
        {
            if (_rtbLog == null || _rtbLog.IsDisposed) return;
            if (InvokeRequired) { BeginInvoke((Action)ClearLog); return; }
            _rtbLog.Clear();
        }
    }
}