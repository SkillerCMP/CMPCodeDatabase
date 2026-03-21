using System;
using System.IO;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    public class SettingsForm : Form
    {
        private readonly Label lblPatchTool = new Label();
        private readonly Label lblDbUrl = new Label();
        private readonly Label lblToolsUrl = new Label();

        private readonly TextBox txtPatchTool = new TextBox();
        private readonly Button btnBrowsePatchTool = new Button();        private readonly CheckBox chkOpenCollectorWhenAddingCodes = new CheckBox();
        private readonly CheckBox chkUseTabbedPreviewCollector = new CheckBox();
        private readonly CheckBox chkDoubleClickResolveModsThenAddToCollector = new CheckBox();
        private readonly CheckBox chkPnachExportNotesAsDescription = new CheckBox();

        private readonly TextBox txtDbUrl = new TextBox();
        private readonly TextBox txtToolsUrl = new TextBox();

        private readonly Button btnOK = new Button();
        private readonly Button btnCancel = new Button();

        public SettingsForm()
        {
            Text = "Settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;

            // DPI / Accessibility Text Size safe defaults
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;

            // A slightly wider base helps long paths/URLs at higher text sizes.
            ClientSize = new System.Drawing.Size(760, 360);
            MinimumSize = new System.Drawing.Size(760, 360);

            // Labels
            lblPatchTool.AutoSize = true;
            lblPatchTool.Text = "Patch Tool:";

            lblDbUrl.AutoSize = true;
            lblDbUrl.Text = "Database Download URL:";

            lblToolsUrl.AutoSize = true;
            lblToolsUrl.Text = "Tools Download URL:";

            // Inputs
            txtPatchTool.Dock = DockStyle.Fill;
            txtDbUrl.Dock = DockStyle.Fill;
            txtToolsUrl.Dock = DockStyle.Fill;

            btnBrowsePatchTool.Text = "Browse...";
            btnBrowsePatchTool.AutoSize = true;
            btnBrowsePatchTool.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnBrowsePatchTool.Padding = new Padding(10, 4, 10, 4);            chkOpenCollectorWhenAddingCodes.Text = "Open Collector window when adding codes";
            chkOpenCollectorWhenAddingCodes.AutoSize = true;

            chkUseTabbedPreviewCollector.Text = "Use tabbed Preview/Collector panel (restart app to apply)";
            chkUseTabbedPreviewCollector.AutoSize = true;

            chkDoubleClickResolveModsThenAddToCollector.Text = "Double-click MOD codes: prompt for MODs, then auto-add to Collector and Reset";
            chkPnachExportNotesAsDescription.Text = "PNACH export: map Notes to description= (default off)";
            chkDoubleClickResolveModsThenAddToCollector.AutoSize = true;
            chkPnachExportNotesAsDescription.AutoSize = true;

            btnOK.Text = "OK";
            btnOK.AutoSize = true;
            btnOK.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnOK.Padding = new Padding(14, 4, 14, 4);

            btnCancel.Text = "Cancel";
            btnCancel.AutoSize = true;
            btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCancel.Padding = new Padding(14, 4, 14, 4);

            // Layout: 3 columns (label | input | browse button)
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 3,
                RowCount = 9,
                AutoSize = false,
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Patch tool row
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Checkbox row 1
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Checkbox row 2
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Checkbox row 3
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Checkbox row 4 (PNACH notes)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // DB URL
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Tools URL
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Filler
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons
// Row 0: Patch Tool
            layout.Controls.Add(lblPatchTool, 0, 0);
            layout.Controls.Add(txtPatchTool, 1, 0);
            layout.Controls.Add(btnBrowsePatchTool, 2, 0);            // Row 1: checkbox
            layout.Controls.Add(chkOpenCollectorWhenAddingCodes, 1, 1);
            layout.SetColumnSpan(chkOpenCollectorWhenAddingCodes, 2);

            // Row 2: checkbox
            layout.Controls.Add(chkUseTabbedPreviewCollector, 1, 2);
            layout.SetColumnSpan(chkUseTabbedPreviewCollector, 2);

            // Row 3: checkbox
            layout.Controls.Add(chkDoubleClickResolveModsThenAddToCollector, 1, 3);
            layout.SetColumnSpan(chkDoubleClickResolveModsThenAddToCollector, 2);

            // Row 4: checkbox
            layout.Controls.Add(chkPnachExportNotesAsDescription, 1, 4);
            layout.SetColumnSpan(chkPnachExportNotesAsDescription, 2);

            // Row 5: DB URL
            layout.Controls.Add(lblDbUrl, 0, 5);
            layout.Controls.Add(txtDbUrl, 1, 5);
            layout.SetColumnSpan(txtDbUrl, 2);

            // Row 6: Tools URL
            layout.Controls.Add(lblToolsUrl, 0, 6);
            layout.Controls.Add(txtToolsUrl, 1, 6);
            layout.SetColumnSpan(txtToolsUrl, 2);

            // Row 8: Buttons (right aligned)
            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };

            buttonRow.Controls.Add(btnOK);
            buttonRow.Controls.Add(btnCancel);

            layout.Controls.Add(buttonRow, 0, 8);
            layout.SetColumnSpan(buttonRow, 3);

            Controls.Add(layout);

            AcceptButton = btnOK;
            CancelButton = btnCancel;

            LoadSettings();

            // Events
            btnBrowsePatchTool.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
                    Title = "Select patcher.exe"
                };

                if (!string.IsNullOrWhiteSpace(txtPatchTool.Text))
                {
                    try { ofd.InitialDirectory = Path.GetDirectoryName(txtPatchTool.Text); }
                    catch { }
                }

                if (ofd.ShowDialog(this) == DialogResult.OK)
                    txtPatchTool.Text = ofd.FileName;
            };

            btnOK.Click += (s, e) =>
            {
                SaveSettings();
                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
        }

        private void LoadSettings()
        {
            var s = AppSettings.Instance;
            txtPatchTool.Text = s.PatchToolPath ?? string.Empty;            chkOpenCollectorWhenAddingCodes.Checked = s.OpenCollectorOnAdd;
            chkUseTabbedPreviewCollector.Checked = s.UseTabbedPreviewCollector;
            chkDoubleClickResolveModsThenAddToCollector.Checked = s.DoubleClickResolveModsThenAddToCollector;
            chkPnachExportNotesAsDescription.Checked = s.PnachExportNotesAsDescription;
            txtDbUrl.Text = s.DatabaseDownloadUrl ?? string.Empty;
            txtToolsUrl.Text = s.ToolsDownloadUrl ?? string.Empty;
        }

        private bool SaveSettings()
        {
            var s = AppSettings.Instance;
            s.PatchToolPath = txtPatchTool.Text?.Trim();            s.OpenCollectorOnAdd = chkOpenCollectorWhenAddingCodes.Checked;
            s.UseTabbedPreviewCollector = chkUseTabbedPreviewCollector.Checked;
            s.DoubleClickResolveModsThenAddToCollector = chkDoubleClickResolveModsThenAddToCollector.Checked;
                        s.PnachExportNotesAsDescription = chkPnachExportNotesAsDescription.Checked;
s.DatabaseDownloadUrl = txtDbUrl.Text?.Trim() ?? string.Empty;
            s.ToolsDownloadUrl = txtToolsUrl.Text?.Trim() ?? string.Empty;
            s.Save();
            return true;
        }
    }
}
