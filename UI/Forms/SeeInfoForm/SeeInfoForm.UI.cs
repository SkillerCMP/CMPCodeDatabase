// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/SeeInfoForm/SeeInfoForm.UI.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class SeeInfoForm : Form
    {
        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(8)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            Controls.Add(root);

            // NOTE (top)
            grpNote = new GroupBox { Text = "Game Note", Dock = DockStyle.Fill, Padding = new Padding(8) };
            noteBrowser = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
            grpNote.Controls.Add(noteBrowser);
            root.Controls.Add(grpNote, 0, 0);
            root.SetColumnSpan(grpNote, 2);

            // IDs (bottom-left)
            grpIds = new GroupBox { Text = "Game ID  >  Hash", Dock = DockStyle.Fill, Padding = new Padding(8) };
            gridIds = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
            };
            grpIds.Controls.Add(gridIds);
            root.Controls.Add(grpIds, 0, 1);

            // CREDITS (bottom-right)
            grpCredits = new GroupBox { Text = "Credits", Dock = DockStyle.Fill, Padding = new Padding(8) };
            listCredits = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            listCredits.Columns.Add("Name", 220);
            listCredits.Columns.Add("Count", 80, HorizontalAlignment.Right);
            listCredits.Columns.Add("Role", 160);

            // Toggle sort without depending on comparer properties; stash state in Tag
            listCredits.ColumnClick += (s, e) =>
            {
                bool newAsc = true;
                if (listCredits.Tag is Tuple<int, bool> last && last.Item1 == e.Column)
                    newAsc = !last.Item2;
                listCredits.Tag = Tuple.Create(e.Column, newAsc);

                listCredits.ListViewItemSorter = new ListViewItemComparer(e.Column, newAsc);
                listCredits.Sort();
            };

            grpCredits.Controls.Add(listCredits);
            root.Controls.Add(grpCredits, 1, 1);
        }

        private void EnsureIdGridColumns()
        {
            if (gridIds == null) return;
            if (gridIds.Columns.Count >= 2)
            {
                gridIds.Columns[0].HeaderText = "GameID";
                gridIds.Columns[1].HeaderText = "Hash";
                gridIds.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                gridIds.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }
    }
}
