// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorControl.FixedUI.cs
// Purpose: Collector sizing heuristics (window-hosted only) + log/scrollbar setup.
// Notes:
//  • When embedded in MainForm (tabbed layout), we must NOT resize the MainForm.
//  • The collector itself can still compute a "desired minimum" width for its list.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        // Width probe — window width will be sized to fit this string.
        private const string __WidthProbe = "Mod Item Quantity (Player Inventory, A cure no more effective(21) | Epic_DiscoloredRemedy)ANDME";

        private void ApplyFixedCollectorSizing(Form host)
        {
            try
            {
                try { EnsureLogPanel(); } catch { }

                // Ensure scrollbars
                try { _rtbLog?.ScrollBars = RichTextBoxScrollBars.Both; } catch { }
                try { clbCollector?.HorizontalScrollbar = true; } catch { }

                // Compute a *minimum* width from probe text (do not hard-lock window size).
                var useFont = (clbCollector != null ? clbCollector.Font : host.Font);
                var sz = TextRenderer.MeasureText(__WidthProbe, useFont);
                int minWidth = Math.Max(560, sz.Width + 140); // extra padding for large text/buttons

                // Only grow if needed; leave user free to resize smaller/larger.
                if (host.Width < minWidth) host.Width = minWidth;

                int minHeight = Math.Max(host.MinimumSize.Height, 520);
                host.MinimumSize = new Size(minWidth, minHeight);

                // Keep it resizable so Windows "Text size" doesn't clip fixed-size UI.
                if (host.FormBorderStyle == FormBorderStyle.FixedDialog)
                    host.FormBorderStyle = FormBorderStyle.Sizable;
            }
            catch { }
        }
    }
}
