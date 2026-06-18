using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        internal class SimpleModDialog : Form
            {
                private ComboBox cmb = new ComboBox() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
                private Button ok = new Button() { Text = "OK", Dock = DockStyle.Bottom };
                public string? SelectedValue { get; private set; }
                public string? SelectedName { get; private set; }
                public SimpleModDialog(string tag, List<(string Value, string Name)> items)
                {
                    Text = $"Select {tag}";
                    Width = 420; Height = 140; StartPosition = FormStartPosition.CenterParent;
                    foreach (var m in items) cmb.Items.Add($"{m.Value} = {m.Name}");
                    if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
                    ok.Click += (s, e) =>
                    {
                        if (cmb.SelectedIndex >= 0 && cmb.SelectedIndex < items.Count)
                        {
                            SelectedValue = items[cmb.SelectedIndex].Value;
                            SelectedName = items[cmb.SelectedIndex].Name;
                        }
                        DialogResult = DialogResult.OK;
                    };
                    Controls.Add(cmb);
                    Controls.Add(ok);
                    AcceptButton = ok;
                }
                public SimpleModDialog(string tag, List<(string Value, string Name)> items, string titleOverride)
                    : this(tag, items)
                {
                    if (!string.IsNullOrWhiteSpace(titleOverride)) this.Text = titleOverride;
                }
    
            }

                internal class ScopeChoiceDialog : Form
                {
                    private RadioButton rbAll = new RadioButton() { Text = "Apply to ALL occurrences", Dock = DockStyle.Top };
                    private RadioButton rbOne = new RadioButton() { Text = "Choose a SINGLE occurrence", Dock = DockStyle.Top };
                    private RadioButton rbEach = new RadioButton() { Text = "Step through EACH occurrence (choose different values)", Dock = DockStyle.Top };
                    private Button ok = new Button() { Text = "OK", Dock = DockStyle.Bottom };
                    public string Choice { get; private set; } = "all"; // "all", "one", "each"
                    public ScopeChoiceDialog(string tag, int count)
                    {
                        Text = $"Apply MOD for [{tag}]";
                        Width = 520; Height = 180; StartPosition = FormStartPosition.CenterParent;
                        var lbl = new Label() { Text = $"Found {count} occurrences of [{tag}]. How would you like to apply?", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
                        rbAll.Checked = true;
                        ok.Click += (s, e) =>
                        {
                            Choice = rbAll.Checked ? "all" : rbOne.Checked ? "one" : "each";
                            DialogResult = DialogResult.OK;
                        };
                        var panel = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(8) };
                        panel.Controls.Add(rbEach);
                        panel.Controls.Add(rbOne);
                        panel.Controls.Add(rbAll);
                        Controls.Add(panel);
                        Controls.Add(lbl);
                        Controls.Add(ok);
                    }
                }

            internal class ModGridDialog : Form
            {
                private TextBox txtFilter = new TextBox() { Dock = DockStyle.Top, PlaceholderText = "Filter..." };
                private DataGridView grid = new DataGridView() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize };
                private Button ok = new Button() { Text = "OK", Dock = DockStyle.Bottom, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14, 4, 14, 4) };
                private CheckBox chkSwapBytes = new CheckBox() { Text = "Swap bytes (e.g., 2710 → 1027)", Dock = DockStyle.Bottom, AutoSize = true };
				private List<string[]> allRows;
                private List<string> headers;
                public string? SelectedValue { get; private set; }
                public string? SelectedDisplay { get; private set; }

        
				public ModGridDialog(string tag, List<string> headers, List<string[]> rows, bool initialSwap = false)
                {
                    Text = $"Select {tag}";
                    Width = 680; Height = 420; StartPosition = FormStartPosition.CenterParent;
                    AutoScaleMode = AutoScaleMode.Font;
                    AutoScroll = true;
                    FormBorderStyle = FormBorderStyle.FixedDialog;
                    MinimizeBox = false;
                    MaximizeBox = false;
                    MinimumSize = new System.Drawing.Size(680, 420);
                    this.headers = headers;
                    this.allRows = rows;

                    grid.AllowUserToAddRows = false;
                    grid.AllowUserToDeleteRows = false;
                    grid.RowHeadersVisible = false;
                    grid.Columns.Clear();
                    foreach (var h in headers) grid.Columns.Add(h, h);

                    Populate(rows);

                    txtFilter.TextChanged += (s, e) => ApplyFilter();
                    ok.Click += (s, e) => { TakeSelection(); };
                    grid.CellDoubleClick += (s, e) => { TakeSelection(); };

                    Shown += (_, __) => { try { grid.AutoResizeColumnHeadersHeight(); } catch { } };

                    Controls.Add(grid);
					Controls.Add(chkSwapBytes);                  // NEW: For ByteSwap
                    Controls.Add(ok);
                    Controls.Add(txtFilter);
                    AcceptButton = ok;
					chkSwapBytes.Checked = initialSwap;   // auto-check from <*...> rule
                }

                public ModGridDialog(string tag, List<string> headers, List<string[]> rows, string titleOverride)
                    : this(tag, headers, rows)
                {
                    if (!string.IsNullOrWhiteSpace(titleOverride)) this.Text = titleOverride;
                }
    

                private void Populate(List<string[]> rows)
                {
                    grid.Rows.Clear();
                    foreach (var r in rows)
                    {
                        var cells = new object[headers.Count];
                        for (int i = 0; i < headers.Count; i++) cells[i] = (i < r.Length ? r[i] : "");
                        grid.Rows.Add(cells);
                    }
                }

                private void ApplyFilter()
                {
                    string f = txtFilter.Text.Trim();
                    if (f.Length == 0) { Populate(allRows); return; }
                    var filtered = allRows.Where(r => r.Any(x => x != null && x.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                    Populate(filtered);
                }

                private void TakeSelection()
                {
                    if (grid.CurrentRow != null)
                    {
                        var parts = new string[headers.Count];
                        for (int i = 0; i < headers.Count; i++) parts[i] = grid.CurrentRow.Cells[i].Value?.ToString() ?? "";
                        // First column acts as VALUE; Display is concatenation "VAL | NAME | ..."
                        SelectedValue = parts.Length > 0 ? parts[0] : "";
						if (chkSwapBytes.Checked)
    SelectedValue = SwapBytes(SelectedValue ?? string.Empty);
                        SelectedDisplay = string.Join(" | ", parts.Skip(1).Where(s => !string.IsNullOrWhiteSpace(s)));
                        DialogResult = DialogResult.OK;
                    }
                }
				private static string SwapBytes(string hex)
{
    if (string.IsNullOrWhiteSpace(hex)) return string.Empty;
    // remove spaces
    var clean = System.Text.RegularExpressions.Regex.Replace(hex, @"\s+", "");
    // pad to even length
    if ((clean.Length & 1) == 1) clean = "0" + clean;
    // split into bytes and reverse order
    var bytes = new string[clean.Length / 2];
    for (int i = 0; i < bytes.Length; i++)
        bytes[i] = clean.Substring(i * 2, 2);
    System.Array.Reverse(bytes);
    return string.Concat(bytes);
}
            }
    }
}
