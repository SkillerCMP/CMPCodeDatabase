
// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — Patch file: UI/Controls/CreditsGrid.cs
// Purpose: Editable grid for Credits with Role; compatible with Core.Models.Credit
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Models;

namespace CMPCodeDatabase.UI.Controls
{
    public sealed class CreditsGrid : UserControl
    {
        private sealed class Entry
        {
            public string Name { get; set; } = string.Empty;
            public string Role { get; set; } = "N/A";
        }

        private readonly DataGridView _grid = new DataGridView();
        private readonly BindingList<Entry> _binding = new BindingList<Entry>();
        private readonly ContextMenuStrip _menu = new ContextMenuStrip();

        public CreditsGrid()
        {
            Dock = DockStyle.Fill;
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = true;
            _grid.AllowUserToDeleteRows = true;
            _grid.AutoGenerateColumns = false;
            _grid.DataSource = _binding;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            var colName = new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                DataPropertyName = nameof(Entry.Name),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 60
            };
            var colRole = new DataGridViewComboBoxColumn
            {
                HeaderText = "Role",
                DataPropertyName = nameof(Entry.Role),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 40,
                FlatStyle = FlatStyle.Flat
            };

            colRole.Items.AddRange(CommonRoles.Cast<object>().ToArray());
            _grid.Columns.Add(colName);
            _grid.Columns.Add(colRole);

            _grid.EditingControlShowing += (s, e) =>
            {
                if (e.Control is ComboBox cb)
                {
                    cb.DropDownStyle = ComboBoxStyle.DropDown; // allow free text
                    cb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    cb.AutoCompleteSource = AutoCompleteSource.ListItems;
                }
            };

            // Context menu
            _menu.Items.Add("Add row", null, (s, e) => _binding.Add(new Entry()));
            _menu.Items.Add("Delete selected", null, (s, e) => DeleteSelected());
            _menu.Items.Add(new ToolStripSeparator());
            var quick = new ToolStripMenuItem("Quick set role");
            foreach (var role in CommonRoles)
                quick.DropDownItems.Add(role, null, (s, e) => SetRoleForSelected(((ToolStripItem)s).Text));
            _menu.Items.Add(quick);
            _grid.ContextMenuStrip = _menu;

            Controls.Add(_grid);
        }

        public IEnumerable<string> CommonRoles { get; set; } = new[]
        {
            "Codes", "IDS", "Porting", "Testing", "Docs", "Research", "Tools", "Maintainer", "Translation"
        };

        public void SetCredits(IEnumerable<Credit> credits)
        {
            _binding.RaiseListChangedEvents = false;
            _binding.Clear();
            foreach (var c in credits ?? Enumerable.Empty<Credit>())
                _binding.Add(new Entry { Name = c?.Name ?? string.Empty, Role = string.IsNullOrWhiteSpace(c?.Role) ? "N/A" : c.Role! });
            _binding.RaiseListChangedEvents = true;
            _binding.ResetBindings();
        }

        public List<Credit> GetCredits()
        {
            return _binding
                .Where(e => !string.IsNullOrWhiteSpace(e.Name))
                .Select(e => new Credit(e.Name.Trim(), NormalizeRole(e.Role)))
                .ToList();
        }

        private static string? NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return null;
            role = role.Trim();
            return role.Equals("N/A", StringComparison.OrdinalIgnoreCase) ? null : role;
        }

        private void DeleteSelected()
        {
            foreach (DataGridViewRow row in _grid.SelectedRows)
                if (!row.IsNewRow && row.DataBoundItem is Entry e) _binding.Remove(e);
        }

        private void SetRoleForSelected(string role)
        {
            foreach (DataGridViewRow row in _grid.SelectedRows)
                if (!row.IsNewRow && row.DataBoundItem is Entry e) e.Role = role;
            _binding.ResetBindings();
        }
    }
}
