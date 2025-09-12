// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.PreviewHighlight.cs
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
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private (int start, int length)? _lastModHighlight;

        private void HighlightModRange(int start, int length)
        {
            if (txtCodePreview == null) return;
            if (start < 0 || length <= 0 || start + length > (txtCodePreview.Text?.Length ?? 0)) return;
            try
            {
                txtCodePreview.HideSelection = false;
                txtCodePreview.Select(start, length);
                txtCodePreview.ScrollToCaret();
                _lastModHighlight = (start, length);
            }
            catch { }
        }

        private void ClearPreviewHighlight()
        {
            if (txtCodePreview == null) return;
            if (_lastModHighlight.HasValue)
            {
                try
                {
                    int pos = _lastModHighlight.Value.start + _lastModHighlight.Value.length;
                    txtCodePreview.Select(Math.Min(pos, txtCodePreview.Text.Length), 0);
                }
                catch { }
                _lastModHighlight = null;
            }
        }
    }
}
