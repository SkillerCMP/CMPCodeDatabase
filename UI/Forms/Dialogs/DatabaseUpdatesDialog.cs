using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMPCodeDatabase.UI.Dialogs;

public sealed class DatabaseUpdatesDialog : Form
{
    private readonly ListView _list;
    private readonly TextBox _txtDetails;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;
    private readonly Label _lblStatus;

    private DatabaseManager.ManifestRoot? _manifest;
    private readonly Dictionary<string, DatabaseManager.UpdateInfo> _updates = new(StringComparer.OrdinalIgnoreCase);

    public DatabaseManager.ManifestDatabase[] SelectedDatabasesToUpdate { get; private set; } = Array.Empty<DatabaseManager.ManifestDatabase>();

    public DatabaseUpdatesDialog()
    {
        Text = "Database Updates…";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;

        _lblStatus = new Label { Dock = DockStyle.Fill, AutoSize = true, Text = "Checking…", ForeColor = SystemColors.GrayText };

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            CheckBoxes = true
        };
        _list.Columns.Add("Database", 340);
        _list.Columns.Add("Changes", 120);
        _list.SelectedIndexChanged += (s, e) => UpdateDetails();

        _txtDetails = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false
        };

        _btnOk = new Button { Text = "Download Selected", DialogResult = DialogResult.OK, AutoSize = true };
        _btnOk.Click += (s, e) => { if (!AcceptSelection()) DialogResult = DialogResult.None; };

        _btnCancel = new Button { Text = "Close", DialogResult = DialogResult.Cancel, AutoSize = true };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false
        };
        buttons.Controls.Add(_btnCancel);
        buttons.Controls.Add(_btnOk);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 260,
            Panel1MinSize = 140,
            Panel2MinSize = 140,
        };
        split.Panel1.Controls.Add(_list);
        split.Panel2.Controls.Add(_txtDetails);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(_lblStatus, 0, 0);
        layout.Controls.Add(split, 0, 1);
        layout.Controls.Add(buttons, 0, 2);

        Controls.Add(layout);
        ClientSize = new Size(640, 620);

        Shown += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _lblStatus.Text = "Loading manifest…";
            _manifest = await DatabaseManager.GetRemoteManifestAsync(CancellationToken.None);

            _lblStatus.Text = "Comparing against local databases…";
            var updates = await DatabaseManager.CheckForUpdatesAsync(CancellationToken.None);

            _updates.Clear();
            foreach (var u in updates)
                _updates[u.DatabaseName] = u;

            PopulateList();
            _lblStatus.Text = updates.Length == 0
                ? "No updates found for your currently installed databases."
                : $"Updates available for {updates.Length} database(s).";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Update check failed.";
            MessageBox.Show(this, "Update check failed:\n\n" + ex.Message, "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();

        if (_manifest is null)
        {
            _list.EndUpdate();
            return;
        }

        foreach (var db in _manifest.Databases.OrderBy(d => d.Name))
        {
            if (!_updates.TryGetValue(db.Name, out var u)) continue;

            var item = new ListViewItem(db.Name) { Tag = db, Checked = true };
            item.SubItems.Add($"{u.ChangedFileCount} file(s)");
            _list.Items.Add(item);
        }

        if (_list.Items.Count > 0)
            _list.Items[0].Selected = true;

        _list.EndUpdate();

        UpdateDetails();
    }

    private void UpdateDetails()
    {
        if (_list.SelectedItems.Count == 0)
        {
            _txtDetails.Text = "";
            return;
        }

        var name = _list.SelectedItems[0].Text;
        if (!_updates.TryGetValue(name, out var u))
        {
            _txtDetails.Text = "";
            return;
        }

        _txtDetails.Text = string.Join(Environment.NewLine, u.ChangedFiles);
    }

    private bool AcceptSelection()
    {
        if (_manifest is null) return false;

        var selected = _list.Items.Cast<ListViewItem>()
            .Where(i => i.Checked)
            .Select(i => i.Tag as DatabaseManager.ManifestDatabase)
            .Where(d => d is not null)
            .Cast<DatabaseManager.ManifestDatabase>()
            .ToArray();

        SelectedDatabasesToUpdate = selected;

        if (selected.Length == 0)
        {
            MessageBox.Show(this, "Nothing selected.", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        return true;
    }
}
