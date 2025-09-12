// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/SeeInfoForm/SeeInfoForm.cs
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
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class SeeInfoForm : Form
    {
        // Accept either a folder or a single .txt file
        private readonly string _path;
        private readonly string? _singleFile;

        private GroupBox grpNote = null!, grpIds = null!, grpCredits = null!;
        private WebBrowser noteBrowser = null!;
        private DataGridView gridIds = null!;
        private ListView listCredits = null!;

        public SeeInfoForm(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Invalid path.", nameof(path));

            if (Directory.Exists(path))
            {
                _path = path;
                _singleFile = null;
            }
            else if (File.Exists(path) && string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
            {
                _path = Path.GetDirectoryName(path)!;
                _singleFile = path;
            }
            else
            {
                throw new ArgumentException("Path must be an existing folder or .txt file.", nameof(path));
            }

            Text = "Game Info";
            Width = 900;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;

            BuildUi();
            LoadData();
        }

        // Simple column sorter for the ListView
        private sealed class ListViewItemComparer : System.Collections.IComparer
        {
            private readonly int _column;
            private readonly bool _ascending;

            public ListViewItemComparer(int column, bool ascending)
            {
                _column = column;
                _ascending = ascending;
            }

            public int Compare(object? x, object? y)
            {
                var lx = x as ListViewItem;
                var ly = y as ListViewItem;

                if (ReferenceEquals(lx, ly)) return 0;
                if (lx is null) return _ascending ? -1 : 1;
                if (ly is null) return _ascending ? 1 : -1;

                string sx = _column < lx.SubItems.Count ? lx.SubItems[_column].Text : string.Empty;
                string sy = _column < ly.SubItems.Count ? ly.SubItems[_column].Text : string.Empty;

                // numeric-aware compare for Count column
                if (int.TryParse(sx, out var nx) && int.TryParse(sy, out var ny))
                {
                    int cmp = nx.CompareTo(ny);
                    return _ascending ? cmp : -cmp;
                }

                int r = string.Compare(sx, sy, StringComparison.OrdinalIgnoreCase);
                return _ascending ? r : -r;
            }
        }
    }
}
