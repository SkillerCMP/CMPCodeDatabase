// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/UI/MainForm.UI.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────


namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void InitializeComponent()
                {
                    Text = CMPCodeDatabase.Util.VersionUtil.BuildWindowTitle();
                    StartPosition = FormStartPosition.CenterScreen;

                    // MenuStrip
                    var menu = new MenuStrip();
                    this.MainMenuStrip = menu;

                    // View menu with Collector + Calculator
                    var viewMenu = new ToolStripMenuItem("View");
var dbStatsItem = new ToolStripMenuItem("Database Stats…") { Name = "menuDatabaseStats" };
dbStatsItem.Click += (s, e) => ShowDatabaseStatsWindow();
viewMenu.DropDownItems.Add(dbStatsItem);

                    var collectorItem = new ToolStripMenuItem("Collector");
                    collectorItem.ShortcutKeys = Keys.Control | Keys.L;
                    collectorItem.Click += (s, e) => ToggleCollectorWindow();
                    viewMenu.DropDownItems.Add(collectorItem);

                    var calcItem = new ToolStripMenuItem("Calculator");
                    calcItem.ShortcutKeys = Keys.Control | Keys.K;
                    calcItem.Click += (s, e) => ToggleCalculatorWindow();
                    viewMenu.DropDownItems.Add(calcItem);

                    menu.Items.Add(viewMenu);
            
                    // Help menu
        			var helpMenu = new ToolStripMenuItem("Help");

        			var helpOpen = new ToolStripMenuItem("Open Help");
        			helpOpen.ShortcutKeys = Keys.F1;
        			helpOpen.Click += (s, e) =>
        			{
            using (var f = new HelpForm())
                f.ShowDialog(this);
        };
        helpMenu.DropDownItems.Add(helpOpen);

        menu.Items.Add(helpMenu);
        Controls.Add(menu);
                    menu.Dock = DockStyle.Top;

                    // Left: Games
                    Label lblGames = new Label() { Left = 10, Top = 25, Width = 100, Text = "Games" };
                    dbSelector = new ComboBox() { Left = 10, Top = 50, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
                    dbSelector.SelectedIndexChanged += DbSelector_SelectedIndexChanged;
                    treeGames = new TreeView() { Left = 10, Top = 80, Width = 250, Height = 520, HideSelection = false };
                    treeGames.AfterSelect += TreeGames_AfterSelect;

                   // Middle: Codes
Label lblCodes = new Label() { Left = 270, Top = 25, Width = 100, Text = "Codes" };

treeCodes = new TreeView()
{
    Left = 270,
    Top = 50,
    Width = 500,     // your widened width
    Height = 520,
    CheckBoxes = true,
    LabelEdit = false,
	HideSelection = false
};

treeCodes.AfterSelect += TreeCodes_AfterSelect;
treeCodes.NodeMouseDoubleClick += TreeCodes_NodeMouseDoubleClick;
treeCodes.KeyDown += TreeCodes_KeyDown;
treeCodes.DrawMode = TreeViewDrawMode.OwnerDrawText;
treeCodes.DrawNode += TreeCodes_DrawNode_WithRightPad;
// Bold styling for groups/subgroups
                    _boldNodeFont = new Font(treeCodes.Font, FontStyle.Bold);
//  after Controls.Add(treeCodes); so the control has a handle by Shown)
this.Shown        += (_, __) => TreeViewExtent.UpdateHorizontalExtent(treeCodes);
this.ResizeEnd    += (_, __) => TreeViewExtent.UpdateHorizontalExtent(treeCodes);

treeCodes.FontChanged    += (_, __) => TreeViewExtent.UpdateHorizontalExtent(treeCodes);
// NEW: auto-open group {{...}} notes on expand
treeCodes.AfterExpand += TreeCodes_AfterExpand_ShowGroupNote;
treeCodes.AfterExpand    += (_, __) => TreeViewExtent.UpdateHorizontalExtent(treeCodes);
treeCodes.AfterCollapse  += (_, __) => TreeViewExtent.UpdateHorizontalExtent(treeCodes);



// Right: Code preview (monospace, no wrap)
                    Label lblPreview = new Label() { Left = 780, Top = 25, Width = 120, Text = "Code Preview" };
                    txtCodePreview = new TextBox()
                    {
                        Left = 780,
                        Top = 50,
                        Height = 520,
                        Multiline = true,
                        WordWrap = false,
                        ScrollBars = ScrollBars.Both,
                        Font = new Font("Consolas", 10f)
                    };

                    int charsDesired = "00000000 00000000 00000000 00000000".Length;
                    Size size = TextRenderer.MeasureText(new string('0', charsDesired), txtCodePreview.Font);
                    txtCodePreview.Width = Math.Max(360, size.Width + 20);

                    btnRefresh = new Button() { Left = 270, Top = 580, Width = 100, Text = "ReloadDB" };
                    btnRefresh.Click += BtnRefresh_Click;

                    Controls.AddRange(new Control[]
                    {
                        lblGames, dbSelector, treeGames,
                        lblCodes, treeCodes, lblPreview, txtCodePreview, btnRefresh
                    });

                    int maxRight = Controls.Cast<Control>().Max(c => c.Left + c.Width);
                    int maxBottom = Controls.Cast<Control>().Max(c => c.Top + c.Height);
                    this.ClientSize = new Size(maxRight + 10, maxBottom + 10);
                    KeyPreview = true;
                }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
                {
                    if (keyData == (Keys.Control | Keys.K)) { ToggleCalculatorWindow(); return true; }
                    if (keyData == (Keys.Control | Keys.L)) { ToggleCollectorWindow(); return true; }
                    if (keyData == (Keys.Control | Keys.F)) { PromptFind(); return true; }
                    return base.ProcessCmdKey(ref msg, keyData);
                }

	// Owner-draw text with a tiny left inset and a right "tail" so bold endings aren't clipped
private void TreeCodes_DrawNode_WithRightPad(object? sender, DrawTreeNodeEventArgs e)
{
    e.DrawDefault = false;                         // we'll paint the label area ourselves

    var tv   = (TreeView)sender!;
    var font = e.Node.NodeFont ?? tv.Font;

    const int LeftPad  = 6;                       // gap between checkbox column and text
    const int MaxExtra = 64;                      // safety cap for extra width
    var flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

    // How wide is the text really (in this font)?
    int textW   = TextRenderer.MeasureText(
                    e.Graphics, e.Node.Text ?? string.Empty, font,
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
    TextRenderer.DrawText(e.Graphics, e.Node.Text ?? string.Empty, font, rectText, fore, flags);

    // Optional: focus rectangle matching the painted label region
    if (selected && tv.Focused)
        ControlPaint.DrawFocusRectangle(e.Graphics, rectText);
}

        }
    }