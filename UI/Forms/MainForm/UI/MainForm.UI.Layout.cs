// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/UI/MainForm.UI.Layout.cs
// Purpose: MainForm responsive layout and owner-draw helper methods.
// Notes:
//  • Split from MainForm.UI.cs during cleanup pass 13.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
// Responsive layout (keeps original look, but fixes large Text Size / DPI and allows resizing).
private bool _mainLayoutCaptured;
private int _baseClientW, _baseClientH;
private int _baseGamesW, _baseCodesW, _basePreviewW;
private int _gapCols = 10;
private MenuStrip? _mainMenu;

private void CaptureMainLayout(int gamesW, int codesW, int prevW)
{
    if (_mainLayoutCaptured) return;
    _baseClientW = ClientSize.Width;
    _baseClientH = ClientSize.Height;
    _baseGamesW = gamesW;
    _baseCodesW = codesW;
    _basePreviewW = prevW;
    _mainLayoutCaptured = true;
}

private void ApplyMainResponsiveLayout(Label lblGames, Label lblCodes, Label lblPreview)
{
    if (treeGames == null || treeCodes == null || txtCodePreview == null || dbSelector == null || btnRefresh == null)
        return;

    TryInitGameSearchUI();
    // Top baseline: always below the MenuStrip (prevents headers being hidden at large Text Size)
    int menuH = _mainMenu?.Height ?? 0;
    int yLabel = menuH + 8;
    int yCombo = yLabel + lblGames.Height + 6;
    int yTreeTop = yCombo + dbSelector.Height + 8;

    // Bottom area (Reload + optional toggle) reserve
    int bottomPad = 10;
    int btnRowH = btnRefresh.Height + 8;

    // Extra width distributed equally
    int deltaW = ClientSize.Width - _baseClientW;
    int per = deltaW / 3;
    int rem = deltaW - per * 3;

    int wGames = Math.Max(_baseGamesW, _baseGamesW + per + (rem > 0 ? 1 : 0));
    int wCodes = Math.Max(_baseCodesW, _baseCodesW + per + (rem > 1 ? 1 : 0));
    int wPrev  = Math.Max(_basePreviewW, _basePreviewW + per);

    int xGames = 10;
    int xCodes = xGames + wGames + _gapCols;
    int xPrev  = xCodes + wCodes + _gapCols;

    // Labels
    lblGames.Left = xGames;  lblGames.Top = yLabel;
    lblCodes.Left = xCodes;  lblCodes.Top = yLabel;
    lblPreview.Left = xPrev; lblPreview.Top = yLabel;

    // Games controls
    dbSelector.Left = xGames;
    dbSelector.Top = yCombo;
    dbSelector.Width = Math.Max(200, wGames);
    if (_txtGameSearch is { IsDisposed: false })
    {
        int tbH = Math.Max(23, _txtGameSearch.PreferredHeight);
        _txtGameSearch.Left = xGames;
        _txtGameSearch.Top = dbSelector.Bottom + 6;
        _txtGameSearch.Width = Math.Max(200, wGames);
        _txtGameSearch.Height = tbH;
        _txtGameSearch.BringToFront();

        yTreeTop = _txtGameSearch.Bottom + 6;
    }


    treeGames.Left = xGames;
    treeGames.Top = yTreeTop;
    treeGames.Width = wGames;

    // Codes tree
    treeCodes.Left = xCodes;
    treeCodes.Top = yCombo;
    treeCodes.Width = wCodes;

    // Preview (either the classic TextBox, or the tab host if enabled)
    Control previewHost = (tabPreviewCollector != null && !tabPreviewCollector.IsDisposed && tabPreviewCollector.Parent == this)
        ? (Control)tabPreviewCollector
        : (Control)txtCodePreview;

    previewHost.Left = xPrev;
    previewHost.Top = yCombo;
    previewHost.Width = wPrev;

    // Heights grow with window, but never smaller than current content baseline
    int usableH = ClientSize.Height - yTreeTop - btnRowH - bottomPad;
    int minTreeH = 220;
    int hTrees = Math.Max(minTreeH, usableH);

    treeGames.Height = hTrees;

    // Codes + Preview start higher (no search box), so give them extra height so bottoms align with Games.
    int codesExtraH = Math.Max(0, yTreeTop - yCombo);
    int hCodes = hTrees + codesExtraH;
    treeCodes.Height = hCodes;
    previewHost.Height = hCodes;

    // Reload row stays aligned under Codes column
    btnRefresh.Left = xCodes;
    btnRefresh.Top = yTreeTop + hTrees + 8;

    // If the expand toggle exists, keep it beside Reload
    try
    {
        var toggle = Controls.Find("btnExpandCollapseToggle", true).FirstOrDefault() as Button;
        if (toggle != null && !toggle.IsDisposed)
        {
            toggle.Top = btnRefresh.Top;
            toggle.Left = btnRefresh.Right + 8;
            toggle.Height = btnRefresh.Height;
        }
    }
    catch { }
}

	// Owner-draw text with a tiny left inset and a right "tail" so bold endings aren't clipped
private void TreeCodes_DrawNode_WithRightPad(object? sender, DrawTreeNodeEventArgs e)
{
    e.DrawDefault = false;                         // we'll paint the label area ourselves

    if (sender is not TreeView tv) { e.DrawDefault = true; return; }

    var font = e.Node?.NodeFont ?? tv.Font;

    const int LeftPad  = 6;                       // gap between checkbox column and text
    const int MaxExtra = 64;                      // safety cap for extra width
    var flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

    // How wide is the text really (in this font)?
    int textW   = TextRenderer.MeasureText(
                    e.Graphics, e.Node?.Text ?? string.Empty, font,
                    new Size(int.MaxValue, int.MaxValue), flags).Width;

    // Base tail at ~12px (DPI-adjusted), plus any spill beyond default bounds
    int basePad = (int)Math.Round(12 * (tv.DeviceDpi / 96.0));
    int spill   = Math.Max(0, textW - e.Bounds.Width);
    int tailPad = Math.Min(MaxExtra, basePad + spill);

    bool selected = (e.State & TreeNodeStates.Selected) != 0;

    // Label rectangle: start after checkbox column, and extend into the tail
    var rectText = e.Bounds;
    rectText.X     += LeftPad;
    rectText.Width += Math.Max(0, tailPad - LeftPad);

    // Background only under the label (so we don't touch the checkbox column)
    var back = selected ? SystemColors.Highlight : tv.BackColor;
    using (var br = new SolidBrush(back))
        e.Graphics.FillRectangle(br, rectText);

    // Draw the text into that same area
    var fore = selected ? SystemColors.HighlightText : tv.ForeColor;
    TextRenderer.DrawText(e.Graphics, e.Node?.Text ?? string.Empty, font, rectText, fore, flags);

    // Optional: focus rectangle matching the painted label region
    if (selected && tv.Focused)
        ControlPaint.DrawFocusRectangle(e.Graphics, rectText);
}

    }
}
