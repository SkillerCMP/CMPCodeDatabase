using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMPCodeDatabase.UI.Dialogs;

public sealed class DatabasePickerDialog : Form
{
    private readonly TextBox _txtFilter;
    private readonly ListView _list;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;
    private readonly Label _lblStatus;

    private DatabaseManager.ManifestRoot? _manifest;

    public DatabaseManager.ManifestDatabase? SelectedDatabase { get; private set; }
    public DatabaseManager.ManifestDatabase[] SelectedDatabases { get; private set; } = Array.Empty<DatabaseManager.ManifestDatabase>();

    public DatabasePickerDialog()
    {
        Text = "Download Database…";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;

        _txtFilter = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Filter…" };
        _txtFilter.TextChanged += (s, e) => RefreshList();

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = true
        };
        _list.Columns.Add("Database", 360);
        _list.Columns.Add("Files", 80);
        _list.DoubleClick += (s, e) => AcceptSelection();

        _lblStatus = new Label { Dock = DockStyle.Fill, AutoSize = true, Text = "Loading manifest…", ForeColor = SystemColors.GrayText };

        _btnOk = new Button { Text = "Download Selected", DialogResult = DialogResult.OK, AutoSize = true };
        _btnOk.Click += (s, e) => { if (!AcceptSelection()) DialogResult = DialogResult.None; };

        _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false
        };
        buttons.Controls.Add(_btnCancel);
        buttons.Controls.Add(_btnOk);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 4,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(_txtFilter, 0, 0);
        layout.Controls.Add(_lblStatus, 0, 1);
        layout.Controls.Add(_list, 0, 2);
        layout.Controls.Add(buttons, 0, 3);

        Controls.Add(layout);
        ClientSize = new Size(560, 520);

        Shown += async (s, e) => await LoadManifestAsync();
    }

    private async Task LoadManifestAsync()
    {
        try
        {
            _lblStatus.Text = "Loading manifest…";
            _manifest = await DatabaseManager.GetRemoteManifestAsync(CancellationToken.None);
            _lblStatus.Text = $"Loaded {_manifest.Databases.Count} databases.";
            RefreshList();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Failed to load manifest.";
            MessageBox.Show(this, "Failed to load manifest:\n\n" + ex.Message, "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();

        if (_manifest is null)
        {
            _list.EndUpdate();
            return;
        }

        var filter = _txtFilter.Text.Trim();
        var dbs = _manifest.Databases.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter))
            dbs = dbs.Where(d => d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

        foreach (var db in dbs.OrderBy(d => d.Name))
        {
            var item = new ListViewItem(db.Name) { Tag = db };
            item.SubItems.Add(db.FileCount.ToString());
            _list.Items.Add(item);
        }

        if (_list.Items.Count > 0 && _list.SelectedItems.Count == 0)
            _list.Items[0].Selected = true;

        _list.EndUpdate();
    }

    private bool AcceptSelection()
    {
        var selected = _list.SelectedItems
            .Cast<ListViewItem>()
            .Select(i => i.Tag as DatabaseManager.ManifestDatabase)
            .Where(d => d is not null)
            .Cast<DatabaseManager.ManifestDatabase>()
            .ToArray();

        SelectedDatabases = selected;
        SelectedDatabase = selected.FirstOrDefault();

        if (selected.Length == 0)
        {
            MessageBox.Show(this, "Nothing selected.", "CMPCodeDatabase", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        return true;
    }
}
