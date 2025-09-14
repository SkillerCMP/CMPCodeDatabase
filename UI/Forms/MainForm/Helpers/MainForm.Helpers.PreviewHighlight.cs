
// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.PreviewHighlight.cs
// Purpose: Code Preview helpers: keep preview text in sync and highlight current MOD token.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Update the preview text safely (no selection flicker).
        /// </summary>
        private void UpdateCodePreview(string text)
        {
            if (txtCodePreview == null) return;
            try
            {
                txtCodePreview.SuspendLayout();
                txtCodePreview.Text = text ?? string.Empty;
                txtCodePreview.SelectionStart = txtCodePreview.TextLength;
                txtCodePreview.SelectionLength = 0;
            }
            finally
            {
                txtCodePreview.ResumeLayout();
            }
        }

        /// <summary>
        /// Highlight a range (start, length) in the preview. Scrolls caret into view.
        /// Pass a negative length or out-of-bounds start to clear the highlight.
        /// </summary>
        private void HighlightModRange(int start, int length)
        {
            if (txtCodePreview == null) return;

            // Clear when invalid
            if (start < 0 || length <= 0 || start >= txtCodePreview.TextLength)
            {
                ClearModHighlight();
                return;
            }

            int maxLen = txtCodePreview.TextLength - start;
            int len = Math.Min(length, Math.Max(0, maxLen));

            try
            {
                txtCodePreview.SuspendLayout();
                _lastModHighlight = (start, len);
                txtCodePreview.Select(start, len);
                txtCodePreview.ScrollToCaret();
            }
            finally
            {
                txtCodePreview.ResumeLayout();
            }
        }

        /// <summary>
        /// Clear the temporary highlight and collapse selection to after the last range.
        /// </summary>
        private void ClearModHighlight()
        {
            if (txtCodePreview == null) return;
            if (_lastModHighlight.HasValue)
            {
                try
                {
                    int pos = _lastModHighlight.Value.start + _lastModHighlight.Value.length;
                    if (pos < 0) pos = 0;
                    if (pos > txtCodePreview.TextLength) pos = txtCodePreview.TextLength;
                    txtCodePreview.Select(pos, 0);
                }
                catch { /* ignore */ }
                _lastModHighlight = null;
            }
        }
    }
}
