// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.FixedUI.cs
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
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        // Width probe — window width will be sized to fit this string.
        private const string __WidthProbe = "Mod Item Quantity (Player Inventory, A cure no more effective(21) | Epic_DiscoloredRemedy)ANDME";

        private void ApplyFixedCollectorSizing()
        {
            try
            {
                try { EnsureLogPanel(); } catch { }

                // Ensure scrollbars
                try { if (_rtbLog != null) _rtbLog.ScrollBars = RichTextBoxScrollBars.Both; } catch { }
                try { if (clbCollector != null) clbCollector.HorizontalScrollbar = true; } catch { }

                // Compute a *minimum* width from probe text (do not hard-lock window size).
                var useFont = (clbCollector != null ? clbCollector.Font : this.Font);
                var sz = TextRenderer.MeasureText(__WidthProbe, useFont);
                int minWidth = Math.Max(560, sz.Width + 140); // extra padding for large text/buttons

                // Only grow if needed; leave user free to resize smaller/larger.
                if (this.Width < minWidth) this.Width = minWidth;

                int minHeight = Math.Max(this.MinimumSize.Height, 520);
                this.MinimumSize = new Size(minWidth, minHeight);

                // Keep it resizable so Windows "Text size" doesn't clip fixed-size UI.
                if (this.FormBorderStyle == FormBorderStyle.FixedDialog)
                    this.FormBorderStyle = FormBorderStyle.Sizable;
            }
            catch { }
        }
    }
}
