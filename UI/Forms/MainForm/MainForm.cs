using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/MainForm.cs
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
        // Tracks applied MOD labels per node, keyed by a "title" (text before ':') or the full label if no ':'.
                // This lets us replace values for the same MOD title (e.g., "HP: 9999" overwrites "HP: Max") and avoid duplicates.
                private readonly Dictionary<TreeNode, Dictionary<string, string>> appliedModLabelMap =
                    new Dictionary<TreeNode, Dictionary<string, string>>();

                private static string ExtractModKey(string label)
                {
                    if (string.IsNullOrWhiteSpace(label)) return string.Empty;
                    int idx = label.IndexOf(':');
                    if (idx > 0) return label.Substring(0, idx).Trim();
                    return label.Trim().ToUpperInvariant();
                }

                private void ClearAppliedModNames(TreeNode node)
                {
                    if (node == null) return;
                    appliedModNames.Remove(node);
                    appliedModLabelMap.Remove(node);
                }

                private string codeDirectory = null!;

                private readonly Dictionary<string, List<(string Value, string Name)>> modDefinitions =
                    new Dictionary<string, List<(string Value, string Name)>>();

                // Enhanced: headered tables for MODs: Tag -> headers + rows
                private readonly Dictionary<string, List<string>> modHeaders = new();
                private readonly Dictionary<string, List<string[]>> modRows = new();

                private readonly Dictionary<TreeNode, string> originalCodeTemplates = new();
                private readonly Dictionary<TreeNode, string> originalNodeNames = new();
                private readonly Dictionary<TreeNode, string> nodeNotes = new();

                // Popup notes per node (double-brace {{...}}) and session suppression
                private readonly Dictionary<TreeNode, List<string>> nodePopupNotes = new();
                private readonly HashSet<string> suppressedPopupNotes = new();

        // Last highlighted MOD range in the preview (start, length)
        private (int start, int length)? _lastModHighlight = null;
                private readonly HashSet<TreeNode> nodeHasMod = new();
                private readonly Dictionary<TreeNode, string> appliedModNames = new();

                private ContextMenuStrip codesContextMenu = null!;
                // Bold font reused for group/subgroup nodes
                private Font _boldNodeFont = null!;

                // UI controls
                private ComboBox dbSelector = null!;
                private TreeView treeGames = null!;
                private TreeView treeCodes = null!;
                private TextBox txtCodePreview = null!;
                private Button btnRefresh = null!;

                // Collector & Calculator windows
                private CollectorForm? collectorWindow;
                private EnhancedCalculatorForm? calculatorWindow;

                // Fallback collector storage when collector window is closed
                private readonly Dictionary<string, string> collectorFallback = new();

                public MainForm()
                {
                    InitializeComponent();
                    InitializeContextMenu();
        			GameInfoContextHelper.Attach(treeGames, node => (node.Tag as string) ?? string.Empty);
                    LoadDatabaseSelector();
                }

        // Add inside MainForm



        
        
                // Formats hex payloads into 64-bit (16 hex chars) blocks, one per line.
                private static readonly Regex HexWord = new Regex(@"[0-9A-Fa-f]{2,}", RegexOptions.Compiled);


                // ---------- Small helpers ----------


            // --------- MOD dialogs ---------
                // === Per-occurrence helpers (v6) ===

    
        

    
            // Dialog for Special [MOD] Amount:VAL:TYPE:ENDIAN
    
            // Dialog for Special [MOD] Amount: enforce DEC/floating ranges by byte size and prevent overflow typing
            internal class SpecialAmountDialog : Form
            {
                private readonly string type;
                private readonly string endian;
                private readonly int byteSize;            // number of bytes allowed by the MOD tag
                private readonly string defaultRawHex;    // uppercase, no spaces

                private TextBox txtInput = new TextBox() { Dock = DockStyle.Top };
                private Label lblStatus = new Label() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
                private Label lblPreview = new Label() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
                private Label lblMeta = new Label() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
                private Label lblBoxName;
                private Button btnDefault = new Button() { Text = "Use Default", Dock = DockStyle.Bottom };
                private Button btnOK = new Button() { Text = "OK", Dock = DockStyle.Bottom };
                private Button btnCancel = new Button() { Text = "Cancel", Dock = DockStyle.Bottom };

                public string? ResultHex { get; private set; } // uppercase hex, exact byteSize*2 length
                public string? SelectedHex => ResultHex;


                private string _lastValidInput = "0";      // for auto-revert on overflow
                private bool _internalChange = false;      // guard to prevent recursion during revert

                public SpecialAmountDialog(string title, string defaultHex, string type, string endian, string boxLabel = null)
                {
                    this.type = (type ?? "HEX").Trim().ToUpperInvariant();
                    this.endian = (endian ?? "BIG").Trim().ToUpperInvariant();
                    this.defaultRawHex = System.Text.RegularExpressions.Regex.Replace(defaultHex ?? "", "[^0-9A-Fa-f]", "").ToUpperInvariant();
                    this.byteSize = Math.Max(1, defaultRawHex.Length / 2);

                    
                    // Enforce proper byte size for floating types regardless of default hex length
                    switch (this.type)
                    {
                        case "FLOAT":
                        case "FLOAT32":
                            this.byteSize = 4;
                            break;
                        case "DOUBLE":
                        case "FLOAT64":
                            this.byteSize = 8;
                            break;
                    }
var label = (boxLabel ?? string.Empty).Trim('<','>',' '); // safety: remove <>
var caption = string.IsNullOrWhiteSpace(title) ? "Amount" : title;
if (!string.IsNullOrWhiteSpace(label)) caption += " " + label;
Text = caption;
                    Width = 520; Height = 260; StartPosition = FormStartPosition.CenterParent;

                    var panel = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(8) };
                    Controls.Add(panel);
                    panel.Controls.Add(txtInput);
                    panel.Controls.Add(lblStatus);
                    panel.Controls.Add(lblPreview);
                    panel.Controls.Add(lblMeta);
                    Controls.Add(btnCancel);
                    Controls.Add(btnOK);
                    Controls.Add(btnDefault);

                    lblMeta.Text = $"Type: {this.type}   Endian: {this.endian}   Size: {byteSize} bytes";

                    // Default in DEC for all types (including floats)
                    string defDisplay = DisplayDefaultDec();
                    _lastValidInput = defDisplay;
                    txtInput.Text = defDisplay;
                    txtInput.SelectAll();

                    txtInput.TextChanged += (s, e) => ValidateLive();
                    ValidateLive();

                    btnDefault.Text = $"Use Default ({defDisplay})";
                    btnDefault.Click += (s, e) => { txtInput.Text = defDisplay; txtInput.SelectAll(); };

                    btnOK.Click += (s, e) => { 
                        try { 
                            var hex = ComputeHexFromInput(txtInput.Text?.Trim() ?? "");
                            ResultHex = hex;
                            DialogResult = DialogResult.OK; 
                        } catch (Exception ex) { MessageBox.Show(this, ex.Message.TrimStart('[').Replace("RANGE]", "Range"), "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    };
                    btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
                }

                /// <summary>
                /// Shows the default value in DEC for readability.
                /// For FLOAT types, returns the numeric value (not hex bits).
                /// </summary>
                private string DisplayDefaultDec()
                {
                    if (string.IsNullOrEmpty(defaultRawHex))
                        return "0";

                    bool wantBig = (endian == "BIG" || endian == "BE");
                    byte[] bytes = HexToBytes(defaultRawHex);

                    switch (type)
                    {
                        case "FLOAT":
                        case "FLOAT32":
                        {
                            bytes = NormalizeSize(bytes, 4, wantBig);
                            byte[] le = (byte[])bytes.Clone();
                            if (wantBig) Array.Reverse(le);
                            float f = BitConverter.ToSingle(le, 0);
                            return FormatFloatLabel(f);
                        }
                        case "DOUBLE":
                        case "FLOAT64":
                        {
                            bytes = NormalizeSize(bytes, 8, wantBig);
                            byte[] le = (byte[])bytes.Clone();
                            if (wantBig) Array.Reverse(le);
                            double d = BitConverter.ToDouble(le, 0);
                            return FormatFloatLabel(d);
                        }
                        case "INT":
                        {
                            var u = BytesToUnsignedBigInteger(bytes, wantBig);
                            int bits = Math.Max(8, byteSize * 8);
                            var twoPow = System.Numerics.BigInteger.One << bits;
                            var signBit = System.Numerics.BigInteger.One << (bits - 1);
                            var signed = (u & signBit) != 0 ? u - twoPow : u;
                            return signed.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        case "UINT":
                        case "DEC":
                        case "HEX":
                        default:
                        {
                            var u = BytesToUnsignedBigInteger(bytes, wantBig);
                            return u.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                }

                /// <summary>
                /// Central encoder. Accepts DEC or HEX for integer modes; DEC for float modes.
                /// Produces uppercase hex respecting endian and target byte size.
                /// Throws "[RANGE]" exceptions when value exceeds byte size range to drive auto-revert.
                /// </summary>
                private string ComputeHexFromInput(string input)
                {
                    byte[] bytes;
                    bool wantBig = (endian == "BIG" || endian == "BE");
                    string raw = (input ?? "").Trim();

                    switch (type)
                    {
                        case "HEX":
                        {
                            // Allow DEC here too if user types digits only (no 0x, no A-F)
                            bool looksHex = raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                                          || System.Text.RegularExpressions.Regex.IsMatch(raw, "[A-Fa-f]");

                            if (!looksHex)
                            {
                                if (!System.Numerics.BigInteger.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var biDec))
                                    throw new Exception("Enter a valid integer.");
                                // Unsigned bounds for HEX pathway
                                EnsureWithinBounds(biDec, signed:false);
                                bytes = biDec.ToByteArray(isUnsigned: true, isBigEndian: wantBig);
                                break;
                            }

                            var h = System.Text.RegularExpressions.Regex.Replace(raw, "[^0-9A-Fa-f]", "");
                            if (h.Length == 0) h = "0";
                            if (h.Length % 2 != 0) h = "0" + h;
                            bytes = HexToBytes(h); // big-endian
                            if (!wantBig) Array.Reverse(bytes);
                            // Also enforce max byte size for pure hex by truncation rules: if longer than allowed, treat as range error
                            if (byteSize > 0 && bytes.Length > byteSize)
                                throw new Exception($"[RANGE] Hex value exceeds {byteSize} bytes.");
                            break;
                        }

                        case "INT":
                        {
                            if (!System.Numerics.BigInteger.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var bi))
                                throw new Exception("Enter a valid integer.");
                            EnsureWithinBounds(bi, signed:true);
                            bytes = bi.ToByteArray(isUnsigned: false, isBigEndian: wantBig);
                            break;
                        }
                        case "DEC":
                        case "UINT":
                        {
                            if (!System.Numerics.BigInteger.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var bi))
                                throw new Exception("Enter a valid integer.");
                            if (bi < 0) throw new Exception("Value must be non-negative.");
                            EnsureWithinBounds(bi, signed:false);
                            bytes = bi.ToByteArray(isUnsigned: true, isBigEndian: wantBig);
                            break;
                        }

                        case "FLOAT":
                        case "FLOAT32":
                        {
                            if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fd32))
                                throw new Exception("Enter a valid float.");

                            // Range guard: if casting to float overflows to Infinity, block.
                            float f = (float)fd32;
                            if (float.IsInfinity(f))
                                throw new Exception("[RANGE] Exceeds float32 range.");
                            bytes = BitConverter.GetBytes(f); // LE
                            if (wantBig) Array.Reverse(bytes);
                            break;
                        }
                        case "DOUBLE":
                        case "FLOAT64":
                        {
                            if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fd64))
                                throw new Exception("Enter a valid double.");
                            // double range is enormous; overflow to Infinity check
                            double d = fd64;
                            if (double.IsInfinity(d))
                                throw new Exception("[RANGE] Exceeds float64 range.");
                            bytes = BitConverter.GetBytes(d); // LE
                            if (wantBig) Array.Reverse(bytes);
                            break;
                        }

                        default:
                        {
                            // Fallback: treat as HEX
                            var hx = System.Text.RegularExpressions.Regex.Replace(raw, "[^0-9A-Fa-f]", "");
                            if (hx.Length % 2 != 0) hx = "0" + hx;
                            bytes = HexToBytes(hx);
                            if (!wantBig) Array.Reverse(bytes);
                            if (byteSize > 0 && bytes.Length > byteSize)
                                throw new Exception($"[RANGE] Hex value exceeds {byteSize} bytes.");
                            break;
                        }
                    }

                    // Normalize to declared byte size (pad/truncate)
                    if (byteSize > 0)
                        bytes = NormalizeSize(bytes, byteSize, wantBig);

                    return BytesToHex(bytes);
                }

                private void EnsureWithinBounds(System.Numerics.BigInteger value, bool signed)
                {
                    GetIntegerBounds(byteSize, signed, out var min, out var max);
                    if (value < min || value > max)
                        throw new Exception($"[RANGE] Value exceeds allowed range {min}..{max} for {byteSize} bytes.");
                }

                private static void GetIntegerBounds(int byteSize, bool signed, out System.Numerics.BigInteger min, out System.Numerics.BigInteger max)
                {
                    int bits = Math.Max(1, byteSize) * 8;
                    var one = System.Numerics.BigInteger.One;
                    if (signed)
                    {
                        max = (one << (bits - 1)) - one;
                        min = - (one << (bits - 1));
                    }
                    else
                    {
                        min = System.Numerics.BigInteger.Zero;
                        max = (one << bits) - one;
                    }
                }

                private static string GroupHexPairs(string hex)
                {
                    if (string.IsNullOrWhiteSpace(hex)) return "";
                    var clean = System.Text.RegularExpressions.Regex.Replace(hex, "[^0-9A-Fa-f]", "").ToUpperInvariant();
                    if (clean.Length % 2 != 0) clean = "0" + clean;
                    var parts = new System.Collections.Generic.List<string>(clean.Length / 2);
                    for (int i = 0; i < clean.Length; i += 2)
                        parts.Add(clean.Substring(i, 2));
                    return string.Join(" ", parts);
                }

                private void ValidateLive()
                {
                    if (_internalChange) return;

                    string input = txtInput.Text?.Trim() ?? "";
                    try
                    {
                        string hex = ComputeHexFromInput(input);
                        lblStatus.Text = "Value is valid";
                        lblStatus.ForeColor = System.Drawing.Color.ForestGreen;
                        lblPreview.Text = "Hex: " + GroupHexPairs(hex);
                        btnOK.Enabled = true;
                        _lastValidInput = input;
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message ?? "Invalid value.";
                        lblStatus.Text = msg.TrimStart('[').Replace("RANGE]", "Range");
                        lblStatus.ForeColor = System.Drawing.Color.Firebrick;
                        lblPreview.Text = "Hex: —";
                        btnOK.Enabled = false;

                        // Only auto-revert for hard RANGE violations (over size) while user is typing a DEC/float
                        bool isRange = msg.StartsWith("[RANGE]");
                        if (isRange && _lastValidInput is not null)
                        {
                            _internalChange = true;
                            try
                            {
                                int caret = txtInput.SelectionStart;
                                txtInput.Text = _lastValidInput;
                                txtInput.SelectionStart = Math.Min(_lastValidInput.Length, Math.Max(0, caret - 1));
                                txtInput.SelectionLength = 0;
                                try { System.Media.SystemSounds.Beep.Play(); } catch { }
                            }
                            finally
                            {
                                _internalChange = false;
                            }
                        }
                    }
                }

                private static byte[] NormalizeSize(byte[] bytes, int targetSize, bool bigEndian)
                {
                    if (bytes == null) return Array.Empty<byte>();
                    if (targetSize <= 0) return bytes;
                    if (bytes.Length == targetSize) return bytes;

                    if (bytes.Length < targetSize)
                    {
                        var pad = new byte[targetSize];
                        if (bigEndian)
                            Array.Copy(bytes, 0, pad, targetSize - bytes.Length, bytes.Length); // left-pad
                        else
                            Array.Copy(bytes, 0, pad, 0, bytes.Length); // right-pad
                        return pad;
                    }
                    else
                    {
                        var trimmed = new byte[targetSize];
                        if (bigEndian)
                            Array.Copy(bytes, bytes.Length - targetSize, trimmed, 0, targetSize); // keep tail
                        else
                            Array.Copy(bytes, 0, trimmed, 0, targetSize); // keep head
                        return trimmed;
                    }
                }

                private static byte[] HexToBytes(string hex)
                {
                    if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
                    var clean = System.Text.RegularExpressions.Regex.Replace(hex, "[^0-9A-Fa-f]", "");
                    if (clean.Length % 2 != 0) clean = "0" + clean;
                    var bytes = new byte[clean.Length / 2];
                    for (int i = 0; i < bytes.Length; i++)
                        bytes[i] = byte.Parse(clean.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                    return bytes;
                }

                private static string BytesToHex(byte[] data)
                {
                    var sb = new System.Text.StringBuilder(data.Length * 2);
                    for (int i = 0; i < data.Length; i++) sb.Append(data[i].ToString("X2"));
                    return sb.ToString();
                }

                private static System.Numerics.BigInteger BytesToUnsignedBigInteger(byte[] bytes, bool bigEndian)
                {
                    var bi = System.Numerics.BigInteger.Zero;
                    if (bytes == null || bytes.Length == 0) return bi;
                    if (bigEndian)
                    {
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bi = (bi << 8) + bytes[i];
                        }
                    }
                    else
                    {
                        System.Numerics.BigInteger factor = 1;
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bi += (System.Numerics.BigInteger)bytes[i] * factor;
                            factor <<= 8;
                        }
                    }
                    return bi;
                }

                private static string FormatFloatLabel(double d)
                {
                    if (double.IsPositiveInfinity(d)) return "+Infinity";
                    if (double.IsNegativeInfinity(d)) return "-Infinity";
                    if (double.IsNaN(d)) return "NaN";
                    return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                private static string FormatFloatLabel(float f)
                {
                    if (float.IsPositiveInfinity(f)) return "+Infinity";
                    if (float.IsNegativeInfinity(f)) return "-Infinity";
                    if (float.IsNaN(f)) return "NaN";
                    return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }

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
                private DataGridView grid = new DataGridView() { Dock = DockStyle.Fill, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                private Button ok = new Button() { Text = "OK", Dock = DockStyle.Bottom };
                private CheckBox chkSwapBytes = new CheckBox() { Text = "Swap bytes (e.g., 2710 → 1027)", Dock = DockStyle.Bottom, AutoSize = true };
				private List<string[]> allRows;
                private List<string> headers;
                public string? SelectedValue { get; private set; }
                public string? SelectedDisplay { get; private set; }

        
				public ModGridDialog(string tag, List<string> headers, List<string[]> rows, bool initialSwap = false)
                {
                    Text = $"Select {tag}";
                    Width = 680; Height = 420; StartPosition = FormStartPosition.CenterParent;
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

            // --------- Enhanced Calculator ---------

    
        
    



        
    
        public class EnhancedCalculatorForm : Form
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

                    int xL = 10, xR = 410, w = 380;

                    Label lblIn = new Label() { Left = xL, Top = 10, Width = w, Text = "Input prefixes: 0x hex int, Fx float (8/16 hex chars) or decimal; inf/-inf/nan" };
                    txtInput = new TextBox() { Left = xL, Top = 30, Width = w };

                    cmbEndian = new ComboBox() { Left = xL, Top = 60, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
                    cmbEndian.Items.AddRange(new object[] { "Big-endian", "Little-endian" });
                    cmbEndian.SelectedIndex = 0;
                    cmbEndian.SelectedIndexChanged += (s, e) => { DoConvert(); DoFloatQuick(); };

                    btnSwapEndian = new Button() { Left = xL + 170, Top = 60, Width = 120, Text = "Swap Endian" };
                    btnSwapEndian.Click += (s, e) => { cmbEndian.SelectedIndex = 1 - cmbEndian.SelectedIndex; DoConvert(); DoFloatQuick(); };

                    btnConvert = new Button() { Left = xL, Top = 90, Width = 120, Text = "Convert" };
                    btnConvert.Click += (s, e) => DoConvert();

                    btnClear = new Button() { Left = xL + 130, Top = 90, Width = 120, Text = "Clear" };
                    btnClear.Click += (s, e) => { txtInput.Clear(); txtOutDec.Clear(); txtOutHex.Clear(); txtOutF32.Clear(); txtOutF64.Clear(); };

                    // Outputs
                    Label lblDec = new Label() { Left = xL, Top = 130, Width = w, Text = "Dec >" };
                    txtOutDec = new TextBox() { Left = xL, Top = 150, Width = w, ReadOnly = true };

                    Label lblHex = new Label() { Left = xL, Top = 180, Width = w, Text = "Hex > (no 0x, endian-aware)" };
                    txtOutHex = new TextBox() { Left = xL, Top = 200, Width = w, ReadOnly = true };

                    Label lblF32 = new Label() { Left = xL, Top = 230, Width = w, Text = "Float (32-bit) >  value | HEX" };
                    txtOutF32 = new TextBox() { Left = xL, Top = 250, Width = w, ReadOnly = true };

                    Label lblF64 = new Label() { Left = xL, Top = 280, Width = w, Text = "Double (64-bit) >  value | HEX" };
                    txtOutF64 = new TextBox() { Left = xL, Top = 300, Width = w, ReadOnly = true };

                    // Float arithmetic quick
                    Label lblFloat = new Label() { Left = xR, Top = 10, Width = w, Text = "Float arithmetic (quick)" };
                    txtFloatA = new TextBox() { Left = xR, Top = 30, Width = 140 };
                    cmbOp = new ComboBox() { Left = xR + 150, Top = 30, Width = 60, DropDownStyle = ComboBoxStyle.DropDownList };
                    cmbOp.Items.AddRange(new object[] { "+", "-", "*", "/" }); cmbOp.SelectedIndex = 0;
                    txtFloatB = new TextBox() { Left = xR + 220, Top = 30, Width = 140 };
                    btnFloatCalc = new Button() { Left = xR, Top = 60, Width = 120, Text = "=" };
                    btnFloatCalc.Click += (s, e) => DoFloatQuick();

                    Label lblQF32 = new Label() { Left = xR, Top = 100, Width = w, Text = "Result Float32 >  value | HEX" };
                    txtFloatQuickF32 = new TextBox() { Left = xR, Top = 120, Width = w, ReadOnly = true };

                    Label lblQF64 = new Label() { Left = xR, Top = 150, Width = w, Text = "Result Float64 >  value | HEX" };
                    txtFloatQuickF64 = new TextBox() { Left = xR, Top = 170, Width = w, ReadOnly = true };

                    Controls.AddRange(new Control[] {
                        lblIn, txtInput, cmbEndian, btnSwapEndian, btnConvert, btnClear,
                        lblDec, txtOutDec, lblHex, txtOutHex, lblF32, txtOutF32, lblF64, txtOutF64,
                        lblFloat, txtFloatA, cmbOp, txtFloatB, btnFloatCalc, lblQF32, txtFloatQuickF32, lblQF64, txtFloatQuickF64
                    });

                    KeyPreview = true;
                }

                protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
                {
                    if (keyData == (Keys.Control | Keys.V)) { if (Clipboard.ContainsText()) txtInput.Text = Clipboard.GetText(); return true; }
                    if (keyData == (Keys.Control | Keys.C)) { if (this.ActiveControl is TextBox tb) Clipboard.SetText(tb.Text ?? ""); return true; }
                    return base.ProcessCmdKey(ref msg, keyData);
                }

                private void ClearOutputs()
                {
                    txtOutDec.Clear(); txtOutHex.Clear(); txtOutF32.Clear(); txtOutF64.Clear();
                }

                private static bool IsHexString(string s)
                {
                    if (string.IsNullOrEmpty(s)) return false;
                    foreach (char c in s) if (!Uri.IsHexDigit(c)) return false;
                    return true;
                }

                private static byte[] TrimTo(byte[] bytes, int n)
                {
                    if (bytes == null) return Array.Empty<byte>();
                    if (n <= 0) return Array.Empty<byte>();
                    if (bytes.Length <= n) return bytes;
                    var trimmed = new byte[n];
                    Array.Copy(bytes, trimmed, n);
                    return trimmed;
                }

                private static string Float32Hex(float f, bool bigEndian)
                {
                    var b = BitConverter.GetBytes(f);
                    if (bigEndian) Array.Reverse(b);
                    return BitConverter.ToString(b).Replace("-", "");
                }

                private static string Float64Hex(double d, bool bigEndian)
                {
                    var b = BitConverter.GetBytes(d);
                    if (bigEndian) Array.Reverse(b);
                    return BitConverter.ToString(b).Replace("-", "");
                }

                private static string FormatFloatLabel(double d)
                {
                    if (double.IsPositiveInfinity(d)) return "+Infinity";
                    if (double.IsNegativeInfinity(d)) return "-Infinity";
                    if (double.IsNaN(d)) return "NaN";
                    return d.ToString(CultureInfo.InvariantCulture);
                }
                private static string FormatFloatLabel(float f)
                {
                    if (float.IsPositiveInfinity(f)) return "+Infinity";
                    if (float.IsNegativeInfinity(f)) return "-Infinity";
                    if (float.IsNaN(f)) return "NaN";
                    return f.ToString(CultureInfo.InvariantCulture);
                }

                private static bool TryParseBigInteger(string text, out BigInteger value)
                {
                    text = text.Trim();

                    if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        string hex = text.Substring(2).Replace("_", "").Replace(" ", "");
                        if (hex.Length == 0) { value = BigInteger.Zero; return true; }
                        foreach (char c in hex) { if (!Uri.IsHexDigit(c)) { value = BigInteger.Zero; return false; } }
                        if (hex.Length % 2 == 1) hex = "0" + hex;
                        int len = hex.Length / 2;
                        var bytes = new byte[len + 1];
                        for (int i = 0; i < len; i++)
                            bytes[i] = byte.Parse(hex.Substring(hex.Length - (i + 1) * 2, 2), NumberStyles.HexNumber);
                        value = new BigInteger(bytes);
                        return true;
                    }

                    if (BigInteger.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                        return true;

                    value = BigInteger.Zero;
                    return false;
                }

                private static string FormatHex(BigInteger unsignedVal, bool bigEndian)
                {
                    if (unsignedVal < 0) unsignedVal = -unsignedVal;
                    byte[] bytes;
                    if (unsignedVal.IsZero)
                    {
                        bytes = new byte[] { 0x00 };
                    }
                    else
                    {
                        var list = new List<byte>();
                        var temp = unsignedVal;
                        while (temp > 0)
                        {
                            list.Add((byte)(temp & 0xFF));
                            temp >>= 8;
                        }
                        bytes = list.ToArray(); // little-endian
                    }

                    if (bytes.Length > 16) bytes = TrimTo(bytes, 16);

                    if (bigEndian)
                    {
                        var be = (byte[])bytes.Clone();
                        Array.Reverse(be);
                        return BitConverter.ToString(be).Replace("-", "");
                    }
                    else
                    {
                        return BitConverter.ToString(bytes).Replace("-", "");
                    }
                }

                private void DoFloatQuick()
                {

                    // Gracefully handle empty/invalid inputs; no popups on auto-calls (endian swap)
                    string aTxt = (txtFloatA.Text ?? "").Trim();
                    string bTxt = (txtFloatB.Text ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(aTxt) || string.IsNullOrWhiteSpace(bTxt))
                    {
                        // No inputs -> clear quick outputs and return
                        txtFloatQuickF32.Text = string.Empty;
                        txtFloatQuickF64.Text = string.Empty;
                        return;
                    }

                    if (!double.TryParse(aTxt, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double a) ||
                        !double.TryParse(bTxt, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double b))
                    {
                        // Invalid numbers -> clear quick outputs and return quietly
                        txtFloatQuickF32.Text = string.Empty;
                        txtFloatQuickF64.Text = string.Empty;
                        return;
                    }

                    double res = 0.0;
                    var op = cmbOp.SelectedItem?.ToString();
                    if (op == "+") res = a + b;
                    else if (op == "-") res = a - b;
                    else if (op == "*") res = a * b;
                    else if (op == "/") res = a / b;
                    else res = a + b;

                    bool bigEndian = string.Equals(cmbEndian.SelectedItem?.ToString(), "Big-endian", StringComparison.Ordinal);
                    float f32 = (float)res;
                    // Keep decimal + HEX on quick results
                    txtFloatQuickF32.Text = $"{FormatFloatLabel(f32)} | {Float32Hex(f32, bigEndian)}";
                    txtFloatQuickF64.Text = $"{FormatFloatLabel(res)} | {Float64Hex(res, bigEndian)}";

                }


                private void DoConvert()
                {
                    string input = txtInput.Text?.Trim() ?? "";
                    bool bigEndian = string.Equals(cmbEndian.SelectedItem?.ToString(), "Big-endian", StringComparison.Ordinal);
                    if (string.IsNullOrEmpty(input)) { ClearOutputs(); return; }

                    // Fx => float input
                    if (input.StartsWith("Fx", StringComparison.OrdinalIgnoreCase))
                    {
                        string body = input.Substring(2).Trim();
                        if (body.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) body = body.Substring(2);
                        body = body.Replace("_", "").Replace(" ", "");

                        if (IsHexString(body))
                        {
                            if (body.Length <= 8)
                            {
                                body = body.PadLeft(8, '0');
                                byte[] be = new byte[4];
                                for (int i = 0; i < 4; i++) be[i] = byte.Parse(body.Substring(i * 2, 2), NumberStyles.HexNumber);
                                byte[] le = (byte[])be.Clone(); Array.Reverse(le);
                                float f32 = BitConverter.ToSingle(le, 0);
                                double f64v = f32;
                                txtOutDec.Text = FormatFloatLabel(f32);
                                txtOutHex.Text = Float32Hex(f32, bigEndian);
                                txtOutF32.Text = Float32Hex(f32, bigEndian);
                                txtOutF64.Text = Float64Hex(f64v, bigEndian);
                                return;
                            }
                            else
                            {
                                body = body.PadLeft(16, '0');
                                byte[] be = new byte[8];
                                for (int i = 0; i < 8; i++) be[i] = byte.Parse(body.Substring(i * 2, 2), NumberStyles.HexNumber);
                                byte[] le = (byte[])be.Clone(); Array.Reverse(le);
                                double f64 = BitConverter.ToDouble(le, 0);
        float f32v = (float)f64;
        txtOutDec.Text = FormatFloatLabel(f64);
                                txtOutHex.Text = Float64Hex(f64, bigEndian);
                                txtOutF32.Text = Float32Hex(f32v, bigEndian);
                                txtOutF64.Text = Float64Hex(f64, bigEndian);
                                return;
                            }
                        }
                        else
                        {
                            // decimal float (allow inf, -inf, nan too)
                            if (TryParseSpecialFloat(body, out var spec))
                            {
                                float f32 = (float)spec;
                                txtOutDec.Text = FormatFloatLabel(spec);
                                txtOutHex.Text = Float64Hex(spec, bigEndian);
                                txtOutF32.Text = Float32Hex(f32, bigEndian);
                                txtOutF64.Text = Float64Hex(spec, bigEndian);
                                return;
                            }
                            if (double.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out var dval))
                            {
                                float f32 = (float)dval;
                                txtOutDec.Text = FormatFloatLabel(dval);
                                txtOutHex.Text = Float64Hex(dval, bigEndian);
                                txtOutF32.Text = Float32Hex(f32, bigEndian);
                                txtOutF64.Text = Float64Hex(dval, bigEndian);
                                return;
                            }
                        }
                    }

                    // Special values top-level
                    if (TryParseSpecialFloat(input, out var sVal))
                    {
                        float f32 = (float)sVal;
                        txtOutDec.Text = FormatFloatLabel(sVal);
                        txtOutHex.Text = Float64Hex(sVal, bigEndian);
                        txtOutF32.Text = Float32Hex(f32, bigEndian);
                        txtOutF64.Text = Float64Hex(sVal, bigEndian);
                        return;
                    }

                    // Hex integer or decimal integer
                    if (TryParseBigInteger(input, out var bigint))
                    {
                        var signedVal = bigint;

                        // Unsigned modulo 2^128 for Hex (two's-complement view)
                        var mod = (BigInteger.One << 128);
                        var unsigned128 = signedVal & (mod - 1);

                        txtOutDec.Text = signedVal.ToString(CultureInfo.InvariantCulture);
                        txtOutHex.Text = FormatHex(unsigned128, bigEndian);

                        // Floats from signed integer
                        float f32 = (float)(double)(decimal)signedVal;
                        double f64 = (double)signedVal;
        txtOutF32.Text = Float32Hex(f32, bigEndian);
                        txtOutF64.Text = Float64Hex(f64, bigEndian);
                        return;
                    }

                    // Otherwise parse as double
                    if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    {
                        txtOutDec.Text = FormatFloatLabel(d);
                        txtOutHex.Text = Float64Hex(d, bigEndian);
                        float f32 = (float)d;
                        txtOutF32.Text = Float32Hex(f32, bigEndian);
                        txtOutF64.Text = Float64Hex(d, bigEndian);
                        return;
                    }

                    ClearOutputs();
                }

                private static bool TryParseSpecialFloat(string text, out double dval)
                {
                    dval = 0;
                    string t = (text ?? "").Trim().ToLowerInvariant();
                    if (t == "inf" || t == "+inf" || t == "infinity" || t == "+infinity")
                    {
                        dval = double.PositiveInfinity; return true;
                    }
                    if (t == "-inf" || t == "-infinity")
                    {
                        dval = double.NegativeInfinity; return true;
                    }
                    if (t == "nan")
                    {
                        dval = double.NaN; return true;
                    }
                    return false;
                }
            }

    

        private void AppendAppliedModName(TreeNode node, string displayName)

        {
            if (node == null) return;
            if (string.IsNullOrWhiteSpace(displayName)) return;

            if (!appliedModLabelMap.TryGetValue(node, out var map))
            {
                map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                appliedModLabelMap[node] = map;
            }

            string key = ExtractModKey(displayName);
            if (string.IsNullOrEmpty(key)) key = displayName.Trim().ToUpperInvariant();

            map[key] = displayName.Trim();

            appliedModNames[node] = string.Join(", ", map.Values);
        }

        // --- Note UI helpers (popup-aware) ---
        private bool HasPopupNote(TreeNode node) =>
            node != null && nodePopupNotes.TryGetValue(node, out var list) && list != null && list.Count > 0;

        private void ShowNoteOrPopupForNode(TreeNode node)
        {
            if (node == null) return;
            if (HasPopupNote(node))
            {
                // Show the popup dialog (non-gating here)
                string html = string.Join("<hr/>", nodePopupNotes[node].Select(UnescapeNote));
                using (var dlg = new NotePopupDialog(GetCopyName(node), html))
                {
                    dlg.ShowDialog(this);
                }
                return;
            }
            ShowNoteForNode(node);
        }

        // Gate actions (Select MOD / Add to Collector). Return true if user chooses Continue.
        private bool GateOnAction(TreeNode node)
        {
            if (node == null) return true;
            // If suppressed for the session, allow through
            string key = GetCopyName(node);
            if (string.IsNullOrWhiteSpace(key)) key = node.Text ?? string.Empty;
            if (suppressedPopupNotes.Contains(key)) return true;

            if (!nodePopupNotes.TryGetValue(node, out var list) || list == null || list.Count == 0) return true;

            string html = string.Join("<hr/>", list.Select(UnescapeNote));
            using (var dlg = new NotePopupDialog(GetCopyName(node), html))
            {
                var result = dlg.ShowDialog(this);
                if (dlg.Suppress) suppressedPopupNotes.Add(key);
                return result == DialogResult.OK; // Continue => true; Don't Use => false
            }
        }

}
}
