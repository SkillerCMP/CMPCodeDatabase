using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    // Modeless stats window (computes on open and on Refresh)
    public sealed class DatabaseStatsForm : Form
    {
        private readonly string _dbRoot;
        private readonly Label _lblTotals = new Label { Dock = DockStyle.Fill, AutoSize = false };
        private readonly Button _btnRefresh = new Button { Text = "Refresh", AutoSize = true };
        private readonly ListView _listCredits = new ListView {
            View = View.Details, Dock = DockStyle.Fill, FullRowSelect = true, GridLines = true
        };

        public DatabaseStatsForm(string dbRootPath)
        {
            _dbRoot = dbRootPath ?? string.Empty;

            Text = "Database Stats";
            StartPosition = FormStartPosition.CenterParent;
            Width = 900;
            Height = 560;

            // Layout: header (totals + refresh) + credits table
            var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 40, ColumnCount = 2 };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Controls.Add(_lblTotals, 0, 0);
            header.Controls.Add(_btnRefresh, 1, 0);

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            panel.Controls.Add(_listCredits);
            Controls.Add(panel);
            Controls.Add(header);

            Load += (_, __) => Populate();
            _btnRefresh.Click += (_, __) => Populate();
        }

        private void Populate()
        {
            // Quick guards
            if (string.IsNullOrWhiteSpace(_dbRoot) || !Directory.Exists(_dbRoot))
            {
                _lblTotals.Text = "No database selected.";
                _listCredits.Items.Clear();
                _listCredits.Columns.Clear();
                _listCredits.Columns.Add("Name", 320);
                _listCredits.Columns.Add("Count", 80, HorizontalAlignment.Right);
                _listCredits.Columns.Add("Role", 160);
                _listCredits.Columns.Add("Count", 80, HorizontalAlignment.Right);
                return;
            }

            int totalCodes = 0;
            var gameNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Totals per-name, and per-role per-name
            var creditCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // Name -> total
            var rolesByName  = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase); // Name -> (Role -> Count)

            foreach (var file in Directory.EnumerateFiles(_dbRoot, "*.txt", SearchOption.AllDirectories))
            {
                bool foundNameInFile = false;
                try
                {
                    foreach (var raw in File.ReadLines(file))
                    {
                        var line = raw?.Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        // Count codes: same rule your parser uses — lines starting with '+'
                        if (line[0] == '+') totalCodes++;

                        // Grab ^3 = NAME: for unique games
                        if (!foundNameInFile && line.StartsWith("^3", StringComparison.Ordinal))
                        {
                            var ix = line.IndexOf("NAME:", StringComparison.OrdinalIgnoreCase);
                            if (ix >= 0)
                            {
                                var gname = line.Substring(ix + "NAME:".Length).Trim().Trim('"');
                                if (gname.Length > 0) { gameNames.Add(gname); foundNameInFile = true; }
                            }
                        }

                        // Aggregate Credits lines
                        var cm = Regex.Match(line, @"^\s*%Credits:\s*(.+)$");
                        if (cm.Success)
                        {
                            var payload = cm.Groups[1].Value;

                            foreach (var tok in payload.Split(new[] { '>', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                var t = tok.Trim();
                                if (t.Length == 0) continue;

                                string name = t;
                                string? role = null;

                                // 1) "Name (Role)"
                                var pm = Regex.Match(t, @"^(?<n>.+?)\s*\((?<r>.+?)\)\s*$");
                                if (pm.Success)
                                {
                                    name = pm.Groups["n"].Value.Trim();
                                    role = pm.Groups["r"].Value.Trim();
                                }
                                else
                                {
                                    // 2) "Name:Role" (split on first ':')
                                    var colon = t.IndexOf(':');
                                    if (colon > 0 && colon < t.Length - 1)
                                    {
                                        name = t.Substring(0, colon).Trim();
                                        role = t.Substring(colon + 1).Trim();
                                    }
                                }

                                // 3) No explicit role -> "Not Set"
                                if (string.IsNullOrEmpty(role)) role = "Not Set";
                                if (name.Length == 0) continue;

                                // Total per person
                                creditCounts[name] = creditCounts.TryGetValue(name, out var c) ? (c + 1) : 1;

                                // Role count per person
                                if (!rolesByName.TryGetValue(name, out var map))
                                {
                                    map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                    rolesByName[name] = map;
                                }
                                map[role] = map.TryGetValue(role, out var rc) ? (rc + 1) : 1;
                            }
                        }
                    }
                }
                catch
                {
                    // ignore unreadable files
                }

                // Fallback: no ^3 NAME found → use the folder name once
                if (!foundNameInFile)
                {
                    var fallback = Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(fallback)) gameNames.Add(fallback);
                }
            }

            // Totals label
            _lblTotals.Text = $"Full Database — Total Codes: {totalCodes:N0}    Total Games: {gameNames.Count:N0}";

            // Determine the maximum number of role-pairs any single person has
            int maxPairs = rolesByName.Count == 0 ? 1 : rolesByName.Max(kv => kv.Value.Count);
            maxPairs = Math.Max(1, maxPairs);

            // Rebuild columns: Name | Count | Role | Count | Role | Count | ...
            _listCredits.BeginUpdate();
            _listCredits.Items.Clear();
            _listCredits.Columns.Clear();

            _listCredits.Columns.Add("Name", 320);
            _listCredits.Columns.Add("Count", 80, HorizontalAlignment.Right);
            for (int i = 0; i < maxPairs; i++)
            {
                _listCredits.Columns.Add("Role", 160);
                _listCredits.Columns.Add("Count", 80, HorizontalAlignment.Right);
            }

            // Rows: for each person, fill pairs with the roles they actually have
            foreach (var kv in creditCounts.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var name = kv.Key;
                var total = kv.Value;

                var lvi = new ListViewItem(name);
                lvi.SubItems.Add(total.ToString());

                // Get roles for this person, sorted by count desc then name
                List<KeyValuePair<string, int>> pairs;
                if (rolesByName.TryGetValue(name, out var map))
                    pairs = map.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase).ToList();
                else
                    pairs = new List<KeyValuePair<string, int>>();

                // Fill role|count pairs across the dynamic columns
                int usedPairs = 0;
                foreach (var pair in pairs)
                {
                    lvi.SubItems.Add(pair.Key);
                    lvi.SubItems.Add(pair.Value.ToString());
                    usedPairs++;
                    if (usedPairs >= maxPairs) break; // cap to columns we created
                }

                // Pad remaining columns for this row
                for (int i = usedPairs; i < maxPairs; i++)
                {
                    lvi.SubItems.Add(string.Empty); // Role
                    lvi.SubItems.Add(string.Empty); // Count
                }

                _listCredits.Items.Add(lvi);
            }

            _listCredits.EndUpdate();
        }
    }
}
