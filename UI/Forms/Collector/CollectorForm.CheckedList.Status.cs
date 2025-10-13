using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private enum CodeStatus { None, Warn, Error }
        private readonly Dictionary<int, CodeStatus> _statusByIndex = new();
        private readonly Dictionary<int, string> _originalText = new();

        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return Regex.Replace(s.Trim(), @"\s+", " ");
        }

        private void InitCollectorOwnerDraw()
        {
            if (clbCollector == null || clbCollector.IsDisposed) return;
            clbCollector.DrawMode = DrawMode.OwnerDrawFixed;
            if (clbCollector.ItemHeight < 18) clbCollector.ItemHeight = 18;
            clbCollector.DrawItem -= clbCollector_DrawItem;
            clbCollector.DrawItem += clbCollector_DrawItem;

            _originalText.Clear();
            for (int i = 0; i < clbCollector.Items.Count; i++)
                _originalText[i] = clbCollector.Items[i]?.ToString() ?? string.Empty;

            clbCollector.Invalidate();
        }

        private void clbCollector_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            int box = 14;
            var checkPt = new Point(e.Bounds.Left + 2, e.Bounds.Top + (e.Bounds.Height - box) / 2);
            var textRect = new Rectangle(e.Bounds.Left + 22, e.Bounds.Top, e.Bounds.Width - 22, e.Bounds.Height);

            bool isChecked = clbCollector.GetItemChecked(e.Index);
            var cbState = isChecked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
            CheckBoxRenderer.DrawCheckBox(e.Graphics, checkPt, cbState);

            if (_statusByIndex.TryGetValue(e.Index, out var status) && status == CodeStatus.Error)
            {
                using var pen = new Pen(Color.Red, 2f);
                int pad = 3;
                var r = new Rectangle(checkPt.X, checkPt.Y, box, box);
                e.Graphics.DrawLine(pen, r.Left + pad,  r.Top + pad,  r.Right - pad, r.Bottom - pad);
                e.Graphics.DrawLine(pen, r.Right - pad, r.Top + pad,  r.Left + pad,  r.Bottom - pad);
            }

            Color fore = (_statusByIndex.TryGetValue(e.Index, out var s) && s == CodeStatus.Warn)
                ? Color.DarkOrange : e.ForeColor;

            string text = clbCollector.Items[e.Index]?.ToString() ?? string.Empty;
            TextRenderer.DrawText(e.Graphics, text, e.Font, textRect, fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            e.DrawFocusRectangle();
        }

        public void MarkCodeStatusByName(string displayName, string statusText)
        {
            CodeStatus status =
                (statusText?.Equals("error", StringComparison.OrdinalIgnoreCase) == true) ? CodeStatus.Error :
                (statusText?.Equals("warn",  StringComparison.OrdinalIgnoreCase) == true) ? CodeStatus.Warn  :
                CodeStatus.None;

            if (clbCollector == null) return;
            string needle = Normalize(displayName);

            int hit = -1;
            for (int i = 0; i < clbCollector.Items.Count; i++)
            {
                string txt = Normalize(clbCollector.Items[i]?.ToString() ?? "");
                if (txt.Equals(needle, StringComparison.OrdinalIgnoreCase)) { hit = i; break; }
            }
            if (hit < 0)
            {
                for (int i = 0; i < clbCollector.Items.Count; i++)
                {
                    string txt = Normalize(clbCollector.Items[i]?.ToString() ?? "");
                    if (txt.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) { hit = i; break; }
                }
            }
            if (hit < 0) return;

            _statusByIndex[hit] = status;

            if (!_originalText.TryGetValue(hit, out var original)) original = clbCollector.Items[hit]?.ToString() ?? "";
            string prefix = status == CodeStatus.Error ? "✖ " : (status == CodeStatus.Warn ? "⚠ " : "");
            string want = string.IsNullOrEmpty(prefix) ? original : (original.StartsWith(prefix) ? original : prefix + original);
            if (!string.Equals(clbCollector.Items[hit]?.ToString(), want, StringComparison.Ordinal))
                clbCollector.Items[hit] = want;

            clbCollector.Invalidate(clbCollector.GetItemRectangle(hit));
        }
    }
}
