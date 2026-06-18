using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        public partial class EnhancedCalculatorForm : Form
            {
                private TextBox txtInput = new TextBox();
                private ComboBox cmbEndian = new ComboBox();
                private Button btnSwapEndian = new Button();
                private Button btnConvert = new Button();
                private Button btnClear = new Button();

                private TextBox txtOutDec = new TextBox();
                private TextBox txtOutHex = new TextBox();
                private TextBox txtOutF32 = new TextBox();
                private TextBox txtOutF64 = new TextBox();

                private TextBox txtFloatA = new TextBox();
                private ComboBox cmbOp = new ComboBox();
                private TextBox txtFloatB = new TextBox();
                private Button btnFloatCalc = new Button();
                private TextBox txtFloatQuickF32 = new TextBox();
                private TextBox txtFloatQuickF64 = new TextBox();

                public EnhancedCalculatorForm()
                {
                    Text = "Enhanced Converter";
                    Width = 820; Height = 520; StartPosition = FormStartPosition.CenterParent;

                    // Better behavior on high-DPI and Accessibility > Text size
                    AutoScaleMode = AutoScaleMode.Font;
                    AutoScroll = true;

                    InitializeEnhancedCalculatorLayout();

                    KeyPreview = true;
                }

                protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
                {
                    if (keyData == (Keys.Control | Keys.V)) { if (Clipboard.ContainsText()) txtInput.Text = Clipboard.GetText(); return true; }
                    if (keyData == (Keys.Control | Keys.C)) { if (this.ActiveControl is TextBox tb) Clipboard.SetText(tb.Text ?? ""); return true; }
                    return base.ProcessCmdKey(ref msg, keyData);
                }

            }
    }
}
