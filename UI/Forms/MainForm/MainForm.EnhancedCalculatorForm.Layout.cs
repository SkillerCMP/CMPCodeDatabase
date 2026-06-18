// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.EnhancedCalculatorForm.Layout.cs
// Purpose: Layout and UI construction for EnhancedCalculatorForm.
// Notes:
//  • Split from MainForm.EnhancedCalculatorForm.cs during cleanup pass 20.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        public partial class EnhancedCalculatorForm
        {
                private void InitializeEnhancedCalculatorLayout()
                {
                    // Main layout (two columns). Dock=Top + AutoSize allows AutoScroll when fonts grow.
                    var main = new TableLayoutPanel()
                    {
                        Dock = DockStyle.Top,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        ColumnCount = 2,
                        RowCount = 1,
                        Padding = new Padding(10),
                        GrowStyle = TableLayoutPanelGrowStyle.FixedSize
                    };
                    main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                    main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    // LEFT column
                    var left = new TableLayoutPanel()
                    {
                        Dock = DockStyle.Fill,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        ColumnCount = 1,
                        RowCount = 1,
                        GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                        Margin = new Padding(0, 0, 8, 0)
                    };
                    left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                    Label lblIn = new Label()
                    {
                        AutoSize = true,
                        Text = "Input prefixes: 0x hex int, Fx float (8/16 hex chars) or decimal; inf/-inf/nan",
                        Margin = new Padding(0, 0, 0, 6)
                    };
                    txtInput = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 0, 6) };

                    cmbEndian = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
                    cmbEndian.Items.AddRange(new object[] { "Big-endian", "Little-endian" });
                    cmbEndian.SelectedIndex = 0;
                    cmbEndian.SelectedIndexChanged += (s, e) => { DoConvert(); DoFloatQuick(); };

                    btnSwapEndian = new Button() { AutoSize = true, Text = "Swap Endian" };
                    btnSwapEndian.Click += (s, e) => { cmbEndian.SelectedIndex = 1 - cmbEndian.SelectedIndex; DoConvert(); DoFloatQuick(); };

                    var pnlEndian = new FlowLayoutPanel()
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = false,
                        Margin = new Padding(0, 0, 0, 6)
                    };
                    pnlEndian.Controls.Add(cmbEndian);
                    pnlEndian.Controls.Add(btnSwapEndian);

                    btnConvert = new Button() { AutoSize = true, Text = "Convert" };
                    btnConvert.Click += (s, e) => DoConvert();
                    btnClear = new Button() { AutoSize = true, Text = "Clear" };
                    btnClear.Click += (s, e) => { txtInput.Clear(); txtOutDec.Clear(); txtOutHex.Clear(); txtOutF32.Clear(); txtOutF64.Clear(); };

                    var pnlButtons = new FlowLayoutPanel()
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = false,
                        Margin = new Padding(0, 0, 0, 10)
                    };
                    pnlButtons.Controls.Add(btnConvert);
                    pnlButtons.Controls.Add(btnClear);

                    // Outputs
                    Label lblDec = new Label() { AutoSize = true, Text = "Dec >", Margin = new Padding(0, 0, 0, 2) };
                    txtOutDec = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, Margin = new Padding(0, 0, 0, 8) };

                    Label lblHex = new Label() { AutoSize = true, Text = "Hex > (no 0x, endian-aware)", Margin = new Padding(0, 0, 0, 2) };
                    txtOutHex = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, Margin = new Padding(0, 0, 0, 8) };

                    Label lblF32 = new Label() { AutoSize = true, Text = "Float (32-bit) >  value | HEX", Margin = new Padding(0, 0, 0, 2) };
                    txtOutF32 = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, Margin = new Padding(0, 0, 0, 8) };

                    Label lblF64 = new Label() { AutoSize = true, Text = "Double (64-bit) >  value | HEX", Margin = new Padding(0, 0, 0, 2) };
                    txtOutF64 = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, Margin = new Padding(0, 0, 0, 0) };

                    left.Controls.Add(lblIn);
                    left.Controls.Add(txtInput);
                    left.Controls.Add(pnlEndian);
                    left.Controls.Add(pnlButtons);
                    left.Controls.Add(lblDec);
                    left.Controls.Add(txtOutDec);
                    left.Controls.Add(lblHex);
                    left.Controls.Add(txtOutHex);
                    left.Controls.Add(lblF32);
                    left.Controls.Add(txtOutF32);
                    left.Controls.Add(lblF64);
                    left.Controls.Add(txtOutF64);

                    // RIGHT column (Float arithmetic quick)
                    var right = new TableLayoutPanel()
                    {
                        Dock = DockStyle.Fill,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        ColumnCount = 1,
                        RowCount = 1,
                        GrowStyle = TableLayoutPanelGrowStyle.AddRows,
                        Margin = new Padding(8, 0, 0, 0)
                    };
                    right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

                    Label lblFloat = new Label() { AutoSize = true, Text = "Float arithmetic (quick)", Margin = new Padding(0, 0, 0, 6) };

                    txtFloatA = new TextBox() { Width = 140 };
                    cmbOp = new ComboBox() { Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
                    cmbOp.Items.AddRange(new object[] { "+", "-", "*", "/" });
                    cmbOp.SelectedIndex = 0;
                    txtFloatB = new TextBox() { Width = 140 };

                    var pnlQuick = new FlowLayoutPanel()
                    {
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        FlowDirection = FlowDirection.LeftToRight,
                        WrapContents = false,
                        Margin = new Padding(0, 0, 0, 6)
                    };
                    pnlQuick.Controls.Add(txtFloatA);
                    pnlQuick.Controls.Add(cmbOp);
                    pnlQuick.Controls.Add(txtFloatB);

                    btnFloatCalc = new Button() { AutoSize = true, Text = "=" };
                    btnFloatCalc.Click += (s, e) => DoFloatQuick();

                    Label lblQF32 = new Label() { AutoSize = true, Text = "Result Float32 >  value | HEX", Margin = new Padding(0, 10, 0, 2) };
                    txtFloatQuickF32 = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, Margin = new Padding(0, 0, 0, 8) };

                    Label lblQF64 = new Label() { AutoSize = true, Text = "Result Float64 >  value | HEX", Margin = new Padding(0, 0, 0, 2) };
                    txtFloatQuickF64 = new TextBox() { Anchor = AnchorStyles.Left | AnchorStyles.Right, ReadOnly = true, Margin = new Padding(0, 0, 0, 0) };

                    right.Controls.Add(lblFloat);
                    right.Controls.Add(pnlQuick);
                    right.Controls.Add(btnFloatCalc);
                    right.Controls.Add(lblQF32);
                    right.Controls.Add(txtFloatQuickF32);
                    right.Controls.Add(lblQF64);
                    right.Controls.Add(txtFloatQuickF64);

                    main.Controls.Add(left, 0, 0);
                    main.Controls.Add(right, 1, 0);
                    Controls.Add(main);

                    // Improve label wrapping when resizing
                    void UpdateWrap()
                    {
                        int innerW = Math.Max(240, (ClientSize.Width - main.Padding.Horizontal - 24) / 2);
                        lblIn.MaximumSize = new System.Drawing.Size(innerW, 0);
                        lblFloat.MaximumSize = new System.Drawing.Size(innerW, 0);
                    }
                    Shown += (s, e) => UpdateWrap();
                    SizeChanged += (s, e) => UpdateWrap();

                }
        }
    }
}
