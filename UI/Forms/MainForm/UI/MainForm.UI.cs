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
                    treeGames = new TreeView() { Left = 10, Top = 80, Width = 250, Height = 520 };
                    treeGames.AfterSelect += TreeGames_AfterSelect;

                    // Middle: Codes
                    Label lblCodes = new Label() { Left = 270, Top = 25, Width = 100, Text = "Codes" };
                    treeCodes = new TreeView() { Left = 270, Top = 50, Width = 400, Height = 520, CheckBoxes = true, LabelEdit = true };
                    treeCodes.AfterSelect += TreeCodes_AfterSelect;
                    treeCodes.NodeMouseDoubleClick += TreeCodes_NodeMouseDoubleClick;
                    treeCodes.KeyDown += TreeCodes_KeyDown;

                    // Bold styling for groups/subgroups
                    _boldNodeFont = new Font(treeCodes.Font, FontStyle.Bold);
// Right: Code preview (monospace, no wrap)
                    Label lblPreview = new Label() { Left = 680, Top = 25, Width = 120, Text = "Code Preview" };
                    txtCodePreview = new TextBox()
                    {
                        Left = 680,
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
                    if (keyData == (Keys.Control | Keys.R)) { if (dbSelector.SelectedItem != null) LoadGames(); return true; }
                    if (keyData == (Keys.Control | Keys.K)) { ToggleCalculatorWindow(); return true; }
                    if (keyData == (Keys.Control | Keys.L)) { ToggleCollectorWindow(); return true; }
                    if (keyData == (Keys.Control | Keys.F)) { PromptFind(); return true; }
                    return base.ProcessCmdKey(ref msg, keyData);
                }

    }
}
