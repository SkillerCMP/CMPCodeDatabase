using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Unified, button-style version:
    /// - Adds an "SecondLayer" button column (index 0) to the Game ID > Hash grid.
    /// - Values: YES (red), NO (green), NA (gray when header is absent).
    /// - Clicking works only when YES and a {note} exists; then shows the note.
    /// - Reads ^SECONDLAYER only from the single .txt this window opened.
    /// </summary>
    public partial class SeeInfoForm : Form
    {
        // Tri-state flag: true=YES, false=NO, null=NA (header absent)
        private bool?  _secondLayerFlag;
        private string? _secondLayerNote;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                var (has, note) = ExtractSecondLayerInfo_SingleFileOnly();
                _secondLayerFlag = has;          // keep null as NA
                _secondLayerNote = note;
            }
            catch
            {
                _secondLayerFlag = null;         // NA on error
                _secondLayerNote = null;
            }

            try { ApplySecondLayerToIdsGrid(); } catch { /* keep window usable */ }
        }

        private void ApplySecondLayerToIdsGrid()
        {
            if (gridIds == null) return;

            // Ensure the button column exists at index 0
            int targetIndex = 0;
            DataGridViewColumn? encCol = null;
            foreach (DataGridViewColumn c in gridIds.Columns)
                if (string.Equals(c.HeaderText, "SecondLayer", StringComparison.OrdinalIgnoreCase) || c.Name == "SecondLayer")
                    encCol = c;

            if (encCol == null)
            {
                var btnCol = new DataGridViewButtonColumn
                {
                    Name = "SecondLayer",
                    HeaderText = "SecondLayer",
                    UseColumnTextForButtonValue = false,
                    FlatStyle = FlatStyle.Popup,
                    Width = 90,
                    ReadOnly = false,
                    Frozen = true
                };
                gridIds.Columns.Insert(targetIndex, btnCol);
                encCol = btnCol;
            }
            else if (encCol.Index != targetIndex)
            {
                gridIds.Columns.Remove(encCol);
                gridIds.Columns.Insert(targetIndex, encCol);
            }

            // Populate each row
            foreach (DataGridViewRow row in gridIds.Rows)
            {
                if (row.IsNewRow) continue;

                var cell = new DataGridViewButtonCell();
                string text;
                Color back, fore;

                if (!_secondLayerFlag.HasValue)
                {
                    text = "NA";
                    back = Color.Gainsboro;
                    fore = Color.DimGray;
                }
                else if (_secondLayerFlag.Value)
                {
                    text = "YES";
                    back = Color.IndianRed;
                    fore = Color.White;
                }
                else
                {
                    text = "NO";
                    back = Color.SeaGreen;
                    fore = Color.White;
                }

                cell.Value = text;
                // Try to color the button; Popup/Flat style honors BackColor/ForeColor reasonably
                var style = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = back,
                    ForeColor = fore,
                    SelectionBackColor = back,
                    SelectionForeColor = fore
                };
                cell.Style = style;

                row.Cells[targetIndex] = cell;
            }

            gridIds.CellContentClick -= GridIds_CellContentClick_ShowSecondLayerNote;
            gridIds.CellContentClick += GridIds_CellContentClick_ShowSecondLayerNote;
        }

        private void GridIds_CellContentClick_ShowSecondLayerNote(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex != 0) return; // SecondLayer column

            // Only act when: YES + we have a note
            if (_secondLayerFlag == true && !string.IsNullOrWhiteSpace(_secondLayerNote))
            {
                MessageBox.Show(this, _secondLayerNote!, "Second Layer Note",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            // Otherwise: do nothing (button is inert)
        }

        /// <summary>
        /// Strict: only reads ^SECONDLAYER from the single .txt this form opened.
        /// Returns (null, null) when no file or directive present.
        /// </summary>
        private (bool? has, string? note) ExtractSecondLayerInfo_SingleFileOnly()
        {
            if (string.IsNullOrWhiteSpace(_singleFile) || !File.Exists(_singleFile))
                return (null, null);

            var rx = new Regex(@"^\^SECONDLAYER:\s*(YES|NO)\s*(\{(?<note>[^}]*)\})?\s*$",
                               RegexOptions.IgnoreCase);

            return ScanOneFile(_singleFile!, rx);
        }

        private static (bool? has, string? note) ScanOneFile(string file, Regex rx)
        {
            try
            {
                foreach (var raw in File.ReadLines(file))
                {
                    var line = raw.Trim();
                    var m = rx.Match(line);
                    if (m.Success)
                    {
                        bool has = string.Equals(m.Groups[1].Value, "YES", StringComparison.OrdinalIgnoreCase);
                        string? note = m.Groups["note"].Success ? m.Groups["note"].Value.Trim() : null;
                        return (has, note);
                    }
                    // stop at codes
                    if (line.StartsWith("+") || line.StartsWith("!")) break;
                }
            }
            catch { }
            return (null, null);
        }
    }
}
