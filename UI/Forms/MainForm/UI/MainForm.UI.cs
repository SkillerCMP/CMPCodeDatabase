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
                    AutoScaleMode = AutoScaleMode.Font;
                    FormBorderStyle = FormBorderStyle.Sizable;
                    MaximizeBox = true;
                    MinimizeBox = true;


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


var databaseMenu = new ToolStripMenuItem("Database");

var miVisitSite = new ToolStripMenuItem("Visit Database Site…");
miVisitSite.Click += (s, e) => DatabaseVisitSite();
databaseMenu.DropDownItems.Add(miVisitSite);

var miOpenFolder = new ToolStripMenuItem("Open Local Database Folder…");
miOpenFolder.Click += (s, e) => DatabaseOpenLocalFolder();
databaseMenu.DropDownItems.Add(miOpenFolder);

                    var miExportLocal = new ToolStripMenuItem("Export Local Manifest…");
                    miExportLocal.Click += async (s, e) => await DatabaseExportLocalManifestAsync();
                    databaseMenu.DropDownItems.Add(miExportLocal);


databaseMenu.DropDownItems.Add(new ToolStripSeparator());

var miDownloadOne = new ToolStripMenuItem("Download Database…");
miDownloadOne.Click += async (s, e) => await DatabaseDownloadDatabaseAsync();
databaseMenu.DropDownItems.Add(miDownloadOne);

var miDownloadAll = new ToolStripMenuItem("Download All Databases");
miDownloadAll.Click += async (s, e) => await DatabaseDownloadAllDatabasesAsync();
databaseMenu.DropDownItems.Add(miDownloadAll);

var miCheckUpdates = new ToolStripMenuItem("Check for Database Updates…");
miCheckUpdates.Click += async (s, e) => await DatabaseCheckForUpdatesAsync();
databaseMenu.DropDownItems.Add(miCheckUpdates);

menu.Items.Add(databaseMenu);

            
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
                    _mainMenu = menu;
                    menu.Dock = DockStyle.Top;

                    // Left: Games
                    Label lblGames = new Label() { Left = 10, Top = 25, AutoSize = true, Text = "Games" };
                    dbSelector = new ComboBox() { Left = 10, Top = 50, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
                    dbSelector.SelectedIndexChanged += DbSelector_SelectedIndexChanged;
                    treeGames = new TreeView() { Left = 10, Top = 80, Width = 250, Height = 520, HideSelection = false };
                    treeGames.AfterSelect += TreeGames_AfterSelect;

                   // Middle: Codes
Label lblCodes = new Label() { Left = 270, Top = 25, AutoSize = true, Text = "Codes" };

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
                    bool useTabbedPreviewCollector = CMPCodeDatabase.Core.Settings.AppSettings.Instance.UseTabbedPreviewCollector;

                    Label lblPreview = new Label()
                    {
                        Left = 780,
                        Top = 25,
                        AutoSize = true,
                        Text = useTabbedPreviewCollector ? "Preview / Collector" : "Code Preview"
                    };

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
                    int previewW = Math.Max(360, size.Width + 20);

                    btnRefresh = new Button() { Left = 270, Top = 580, Text = "ReloadDB", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(10, 4, 10, 4) };
                    btnRefresh.Click += BtnRefresh_Click;

                    if (useTabbedPreviewCollector)
                    {
                        // Build the tab host and embed the preview textbox + collector surface.
                        tabPreviewCollector = new TabControl()
                        {
                            Left = 780,
                            Top = 50,
                            Width = previewW,
                            Height = 520
                        };

                        tabPreview = new TabPage("Code Preview");
                        txtCodePreview.Dock = DockStyle.Fill;
                        tabPreview.Controls.Add(txtCodePreview);

                        tabCollector = new TabPage("Collector");
                        collectorTab = new CollectorControl() { Dock = DockStyle.Fill };
                        tabCollector.Controls.Add(collectorTab);

                        tabPreviewCollector.TabPages.Add(tabPreview);
                        tabPreviewCollector.TabPages.Add(tabCollector);

                        Controls.AddRange(new Control[]
                        {
                            lblGames, dbSelector, treeGames,
                            lblCodes, treeCodes, lblPreview, tabPreviewCollector, btnRefresh
                        });

                        // Hook collector to this host form (no window sizing; still enables shortcuts/menu in the tab).
                        this.Shown += (_, __) => collectorTab.AttachHost(this, CollectorControl.CollectorHostMode.Tabbed);
                    }
                    else
                    {
                        // Classic layout: preview textbox + separate collector window.
                        txtCodePreview.Width = previewW;

                        Controls.AddRange(new Control[]
                        {
                            lblGames, dbSelector, treeGames,
                            lblCodes, treeCodes, lblPreview, txtCodePreview, btnRefresh
                        });
                    }
                    int maxRight = Controls.Cast<Control>().Max(c => c.Left + c.Width);
                    int maxBottom = Controls.Cast<Control>().Max(c => c.Top + c.Height);
                    this.ClientSize = new Size(maxRight + 10, maxBottom + 10);
                                        // Capture baseline widths and apply responsive layout (DPI/Text Size safe)
                    CaptureMainLayout(treeGames.Width, treeCodes.Width, (useTabbedPreviewCollector && tabPreviewCollector != null) ? tabPreviewCollector.Width : txtCodePreview.Width);
                    this.Shown += (_, __) => ApplyMainResponsiveLayout(lblGames, lblCodes, lblPreview);
                    this.Resize += (_, __) => ApplyMainResponsiveLayout(lblGames, lblCodes, lblPreview);
                    ApplyMainResponsiveLayout(lblGames, lblCodes, lblPreview);
                    KeyPreview = true;
                }

        
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