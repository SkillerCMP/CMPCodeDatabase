// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.Dialogs.SpecialAmountDialog.Layout.cs
// Purpose: Layout and accessibility sizing helpers for SpecialAmountDialog.
// Notes:
//  • Split from MainForm.Dialogs.SpecialAmountDialog.cs during cleanup pass 21.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        internal partial class SpecialAmountDialog
        {
                private void InitializeSpecialAmountDialogLayout()
                {
                    StartPosition = FormStartPosition.CenterParent;
                    FormBorderStyle = FormBorderStyle.FixedDialog;
                    MinimizeBox = false;
                    MaximizeBox = false;
                    AutoScaleMode = AutoScaleMode.Font;
                    AutoScroll = true;
                    Width = 520; Height = 260;
                    MinimumSize = new System.Drawing.Size(520, 260);

                    var panel = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(8) };

                    var buttonRow = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Bottom,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        FlowDirection = FlowDirection.RightToLeft,
                        WrapContents = false,
                        Padding = new Padding(8),
                    };
                    Controls.Add(panel);
                    panel.Controls.Add(txtInput);
                    panel.Controls.Add(lblStatus);
                    panel.Controls.Add(lblPreview);
                    panel.Controls.Add(lblMeta);
                    buttonRow.Controls.Add(btnCancel);
                    buttonRow.Controls.Add(btnOK);
                    buttonRow.Controls.Add(btnDefault);
                    Controls.Add(buttonRow);

                    AcceptButton = btnOK;
                    CancelButton = btnCancel;

                    Shown += (_, __) => EnsureFitsTextSize(panel, buttonRow);
                    FontChanged += (_, __) => EnsureFitsTextSize(panel, buttonRow);
                    Resize += (_, __) => EnsureFitsTextSize(panel, buttonRow);
                }

                private void EnsureFitsTextSize(Control contentPanel, Control buttonRow)
                {
                    try
                    {
                        // Force layout so heights are correct under Accessibility "Text size"
                        PerformLayout();
                        contentPanel.PerformLayout();
                        buttonRow.PerformLayout();

                        int contentH = 0;
                        foreach (Control c in contentPanel.Controls)
                        {
                            if (!c.Visible) continue;
                            contentH += c.Margin.Vertical + c.Height;
                        }

                        if (contentPanel is Panel p) contentH += p.Padding.Vertical;

                        int requiredClientH = contentH + buttonRow.Height + 24;
                        int requiredW = Math.Max(Width, buttonRow.PreferredSize.Width + 32);

                        var wa = Screen.FromControl(this).WorkingArea;
                        int maxW = Math.Max(320, wa.Width - 80);
                        int maxClientH = Math.Max(240, wa.Height - 80);

                        int newW = Math.Min(maxW, requiredW);
                        int newClientH = Math.Min(maxClientH, Math.Max(ClientSize.Height, requiredClientH));

                        if (Width < newW) Width = newW;
                        if (ClientSize.Height < newClientH) ClientSize = new System.Drawing.Size(ClientSize.Width, newClientH);
                    }
                    catch { }
                }
        }
    }
}
