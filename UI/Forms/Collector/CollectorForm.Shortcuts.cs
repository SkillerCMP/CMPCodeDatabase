using System;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private bool _shortcutsReady_SHORT;

        /// <summary>
        /// Call once (e.g., in CollectorForm.OnShown): try { EnsureShortcuts_SHORT(); } catch {}
        /// </summary>
        private void EnsureShortcuts_SHORT()
        {
            if (_shortcutsReady_SHORT) return;
            _shortcutsReady_SHORT = true;

            try { this.KeyPreview = true; } catch { }
            this.KeyDown += CollectorForm_KeyDown_SHORT;
        }

        private void CollectorForm_KeyDown_SHORT(object sender, KeyEventArgs e)
        {
            // Target file
            if (e.Control && e.KeyCode == Keys.O) { ClickByTextContains_SHORT("Browse"); e.SuppressKeyPress = true; return; }
            if (e.Control && e.KeyCode == Keys.L) { ClickTopClear_SHORT(); e.SuppressKeyPress = true; return; }

            // Selection / copy row
            if (e.Control && !e.Shift && e.KeyCode == Keys.A) { ClickByTextExact_SHORT("Select All"); e.SuppressKeyPress = true; return; }
            if (e.Control &&  e.Shift && e.KeyCode == Keys.A) { ClickByTextExact_SHORT("Select None"); e.SuppressKeyPress = true; return; }
            if (e.Control && !e.Shift && e.KeyCode == Keys.C) { ClickByTextExact_SHORT("Copy Checked"); e.SuppressKeyPress = true; return; }
            if (e.Control &&  e.Shift && e.KeyCode == Keys.C) { ClickByTextExact_SHORT("Copy All"); e.SuppressKeyPress = true; return; }
            if (e.Control && e.KeyCode == Keys.K) { ClickOpsClear_SHORT(); e.SuppressKeyPress = true; return; }

            // Patch row
            if (!e.Control && !e.Shift && e.KeyCode == Keys.F5) { ClickByTextExact_SHORT("Run Patch"); e.SuppressKeyPress = true; return; }
            if ( e.Control && !e.Shift && e.KeyCode == Keys.F5) { ClickByTextExact_SHORT("Run Patch (All)"); e.SuppressKeyPress = true; return; }
            if (!e.Control && !e.Shift && e.KeyCode == Keys.F6) { ClickByTextExact_SHORT("Preview"); e.SuppressKeyPress = true; return; }

            // Backups
            if (e.Control && !e.Shift && e.KeyCode == Keys.B) { ClickByTextExact_SHORT("Open Backups"); e.SuppressKeyPress = true; return; }
            if (e.Control &&  e.Shift && e.KeyCode == Keys.B) { ClickByTextExact_SHORT("Restore Backup"); e.SuppressKeyPress = true; return; }

            // Help
            if (e.KeyCode == Keys.F1) { ShowShortcutHelp_SHORT(); e.SuppressKeyPress = true; return; }
        }

        // ---------- helpers (unique *_SHORT names to avoid collisions) ----------

        private static string Norm_SHORT(string s) =>
            (s ?? string.Empty).Replace("&", "").Trim().TrimEnd('.');

        private Button FindButtonByText_SHORT(string text)
        {
            var want = Norm_SHORT(text).ToLowerInvariant();
            return FindDeep_SHORT<Button>(this, b => Norm_SHORT(b.Text).Equals(want, StringComparison.OrdinalIgnoreCase));
        }

        private Button FindButtonByTextContains_SHORT(string part)
        {
            var want = Norm_SHORT(part).ToLowerInvariant();
            return FindDeep_SHORT<Button>(this, b => Norm_SHORT(b.Text).ToLowerInvariant().Contains(want));
        }

        private void ClickByTextExact_SHORT(string text)
        {
            var b = FindButtonByText_SHORT(text);
            if (b != null && b.Enabled && b.Visible) b.PerformClick();
        }

        private void ClickByTextContains_SHORT(string part)
        {
            var b = FindButtonByTextContains_SHORT(part);
            if (b != null && b.Enabled && b.Visible) b.PerformClick();
        }

        private void ClickTopClear_SHORT()
        {
            // Prefer the Clear next to Target file (usually in the same panel as the Browse button).
            var browse = FindButtonByTextContains_SHORT("Browse");
            if (browse?.Parent != null)
            {
                var cand = browse.Parent.Controls.OfType<Button>()
                    .FirstOrDefault(x => Norm_SHORT(x.Text).Equals("Clear", StringComparison.OrdinalIgnoreCase));
                if (cand != null && cand.Enabled && cand.Visible) { cand.PerformClick(); return; }
            }
            ClickByTextExact_SHORT("Clear");
        }

        private void ClickOpsClear_SHORT()
        {
            // Prefer the Clear that lives with Select All / Select None / Copy…
            var selAll = FindButtonByText_SHORT("Select All");
            if (selAll?.Parent != null)
            {
                var cand = selAll.Parent.Controls.OfType<Button>()
                    .FirstOrDefault(x => Norm_SHORT(x.Text).Equals("Clear", StringComparison.OrdinalIgnoreCase));
                if (cand != null && cand.Enabled && cand.Visible) { cand.PerformClick(); return; }
            }
            ClickByTextExact_SHORT("Clear");
        }

        private static T FindDeep_SHORT<T>(Control root, Func<T, bool> pred) where T : Control
        {
            foreach (Control c in root.Controls)
            {
                if (c is T t && (pred?.Invoke(t) ?? true)) return t;
                var sub = FindDeep_SHORT(c, pred);
                if (sub != null) return sub;
            }
            return null;
        }

        private void ShowShortcutHelp_SHORT()
        {
            var msg =
@"Code Collector — Shortcut Keys

Ctrl+O         Browse target file
Ctrl+L         Clear target file
Ctrl+A         Select All
Ctrl+Shift+A   Select None
Ctrl+C         Copy Checked
Ctrl+Shift+C   Copy All
Ctrl+K         Clear (ops row)
F5             Run Patch
Ctrl+F5        Run Patch (All)
F6             Preview
Ctrl+B         Open Backups
Ctrl+Shift+B   Restore Backup
F1             Show this help";
            MessageBox.Show(this, msg, "Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
