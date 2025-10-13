using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CMPCodeDatabase
{
    // Prefix style: items get "✖ " (error) or "⚠ " (warn) added to the item text.
    // Owner-draw paints ONLY the prefix in color (✖ red, ⚠ orange); the rest stays default.
    public partial class CollectorForm : Form
    {
        private enum CodeStatus { None, Warn, Error }
        private readonly Dictionary<int, CodeStatus> _statusByIndex = new();

        private static string Normalize(string s)
            => string.IsNullOrWhiteSpace(s) ? string.Empty : Regex.Replace(s.Trim(), @"\s+", " ");

        // Call this once after clbCollector is created (e.g., in UI.cs right after the initializer)
        private void InitCollectorOwnerDraw()
        {
            if (clbCollector == null || clbCollector.IsDisposed) return;
            clbCollector.DrawMode = DrawMode.OwnerDrawFixed;
            if (clbCollector.ItemHeight < 18) clbCollector.ItemHeight = 18;
            clbCollector.DrawItem -= clbCollector_DrawItem;   // idempotent
            clbCollector.DrawItem += clbCollector_DrawItem;
            clbCollector.Invalidate();
        }

        private void clbCollector_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            // Layout: checkbox at left, then text
            int box = 14;
            var checkPt  = new Point(e.Bounds.Left + 2, e.Bounds.Top + (e.Bounds.Height - box) / 2);
            var textRect = new Rectangle(e.Bounds.Left + 22, e.Bounds.Top, e.Bounds.Width - 22, e.Bounds.Height);

            // Draw the checkbox itself
            bool isChecked = clbCollector.GetItemChecked(e.Index);
            var cbState = isChecked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
            CheckBoxRenderer.DrawCheckBox(e.Graphics, checkPt, cbState);

            // Prepare text + optional colored prefix
            string raw = clbCollector.Items[e.Index]?.ToString() ?? string.Empty;
            string prefix = raw.StartsWith("✖ ") ? "✖ "
                          : raw.StartsWith("⚠ ") ? "⚠ "
                          : string.Empty;
            string rest = prefix.Length > 0 ? raw.Substring(prefix.Length) : raw;

            var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;

            int x = textRect.Left;

            // Draw colored prefix if present
            if (prefix.Length > 0)
            {
                var color = (prefix[0] == '✖') ? Color.Red : Color.DarkOrange;
                // Measure prefix to advance x correctly
                var sz = TextRenderer.MeasureText(e.Graphics, prefix, e.Font, new Size(int.MaxValue, textRect.Height), flags);
                TextRenderer.DrawText(e.Graphics, prefix, e.Font, new Point(x, textRect.Top), color, flags);
                x += Math.Max(0, sz.Width);
            }

            // Draw the remainder in default color
            TextRenderer.DrawText(e.Graphics, rest, e.Font, new Point(x, textRect.Top), e.ForeColor, flags);

            e.DrawFocusRectangle();
        }

        /// <summary>
        /// Mark a code by its visible name (adds prefix and records status).
        /// Use "error" for ✖, "warn" for ⚠, anything else clears the prefix.
        /// </summary>
        public void MarkCodeStatusByName(string displayName, string statusText)
        {
            var status =
                (statusText?.Equals("error", StringComparison.OrdinalIgnoreCase) == true) ? CodeStatus.Error :
                (statusText?.Equals("warn",  StringComparison.OrdinalIgnoreCase) == true) ? CodeStatus.Warn  :
                CodeStatus.None;

            if (clbCollector == null) return;

            string needle = Normalize(displayName);

            // exact match first
            int hit = -1;
            for (int i = 0; i < clbCollector.Items.Count; i++)
            {
                if (Normalize(clbCollector.Items[i]?.ToString() ?? "")
                    .Equals(needle, StringComparison.OrdinalIgnoreCase)) { hit = i; break; }
            }
            // then contains (robust to any prefixes/spacing changes)
            if (hit < 0)
            {
                for (int i = 0; i < clbCollector.Items.Count; i++)
                {
                    if (Normalize(clbCollector.Items[i]?.ToString() ?? "")
                        .IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) { hit = i; break; }
                }
            }
            if (hit < 0) return;

            // (Re)apply the visible prefix
            string current = clbCollector.Items[hit]?.ToString() ?? string.Empty;
            // remove any old prefix first
            if (current.StartsWith("✖ ") || current.StartsWith("⚠ "))
                current = current.Substring(2);

            string withPrefix = status == CodeStatus.Error ? "✖ " + current
                              : status == CodeStatus.Warn  ? "⚠ " + current
                              : current;

            if (!string.Equals(clbCollector.Items[hit]?.ToString(), withPrefix, StringComparison.Ordinal))
                clbCollector.Items[hit] = withPrefix;

            _statusByIndex[hit] = status;
            clbCollector.Invalidate(clbCollector.GetItemRectangle(hit));
        }

        /// <summary>
        /// Clears all error/warn marks in the list.
        /// Safe for both prefix style and overlay-only style.
        /// </summary>
        private void ClearStatuses()
        {
            if (clbCollector == null) return;

            _statusByIndex.Clear();

            // Strip any "✖ " or "⚠ " that may exist from previous runs
            for (int i = 0; i < clbCollector.Items.Count; i++)
            {
                var s = clbCollector.Items[i]?.ToString() ?? string.Empty;
                if (s.StartsWith("✖ ") || s.StartsWith("⚠ "))
                    clbCollector.Items[i] = s.Substring(2);
            }

            clbCollector.Invalidate();
        }
    }
}
