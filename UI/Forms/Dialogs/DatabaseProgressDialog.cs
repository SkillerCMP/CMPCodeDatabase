using System;
using System.Drawing;
using System.Windows.Forms;

namespace CMPCodeDatabase.UI.Dialogs;

public sealed class DatabaseProgressDialog : Form
{
    private readonly Label _lblTitle;
    private readonly Label _lblDetail;
    private readonly ProgressBar _bar;
    private readonly Button _btnCancel;
    private readonly FlowLayoutPanel _buttonPanel;

    public event Action? CancelRequested;
    public bool IsCancellationRequested { get; private set; }

    public DatabaseProgressDialog()
    {
        Text = "Database Download";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;

        _lblTitle = new Label { AutoSize = true, Text = "Preparing…", Dock = DockStyle.Fill };
        _lblDetail = new Label { AutoSize = true, Text = "", Dock = DockStyle.Fill };
        _bar = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 0 };

        _btnCancel = new Button { AutoSize = true, Text = "Cancel" };
        _btnCancel.Click += (_, _) => RequestCancel();

        _buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0),
        };
        _buttonPanel.Controls.Add(_btnCancel);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(_lblTitle, 0, 0);
        layout.Controls.Add(_lblDetail, 0, 1);
        layout.Controls.Add(_bar, 0, 2);
        layout.Controls.Add(_buttonPanel, 0, 3);

        Controls.Add(layout);
        ClientSize = new Size(560, 130);

        CancelButton = _btnCancel;
    }

    private void RequestCancel()
    {
        if (IsCancellationRequested)
            return;

        IsCancellationRequested = true;
        _btnCancel.Enabled = false;
        CancelRequested?.Invoke();

        // Give immediate feedback to the user.
        SetStatus("Cancelling…");
    }

    private void SetStatus(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(message)));
            return;
        }

        _lblTitle.Text = message;
    }

    public void SetProgress(string dbName, int done, int total, string file)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetProgress(dbName, done, total, file)));
            return;
        }

        _lblTitle.Text = $"Downloading: {dbName}";
        _lblDetail.Text = $"{done}/{Math.Max(total, 1)} – {file}";

        var pct = total <= 0 ? 0 : (int)Math.Round(done * 100.0 / total);
        pct = Math.Clamp(pct, 0, 100);
        _bar.Value = pct;
    }

    public void SetDone(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetDone(message)));
            return;
        }

        _lblTitle.Text = message;
        _lblDetail.Text = "";
        _bar.Value = 100;
    }
}
