
// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — Patch: UI/Forms/SeeInfoForm/SeeInfoForm.CreditsRoles.cs
// Purpose: Role-aware Credits population (no GameInfo dependencies).
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class SeeInfoForm : Form
    {
        private void PopulateCreditsFromTextWithRoles(string text)
        {
            var counts = ParseCredits(text);
            var rolesMap = ParseCreditsRolesMap(text);
            var normalized = NormalizeCreditsCounts(counts);
            PopulateCredits(normalized, rolesMap);
        }

        private Dictionary<string, string> ParseCreditsRolesMap(string text)
        {
            var sep = new[] { '>', ',', ';', '|' };
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in Regex.Matches(text ?? string.Empty, @"^\s*%Credits:\s*(.+)$", RegexOptions.Multiline))
            {
                var payload = m.Groups[1].Value;
                var tokens = payload.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Where(s => s.Length > 0);

                foreach (var tok in tokens)
                {
                    string name = tok;
                    string? role = null;

                    var colon = tok.IndexOf(':');
                    if (colon > 0)
                    {
                        name = tok.Substring(0, colon).Trim();
                        role = tok.Substring(colon + 1).Trim();
                    }
                    else
                    {
                        var pm = Regex.Match(tok, @"^(?<n>.+?)\s*\((?<r>.+?)\)\s*$");
                        if (pm.Success)
                        {
                            name = pm.Groups["n"].Value.Trim();
                            role = pm.Groups["r"].Value.Trim();
                        }
                    }

                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (!map.TryGetValue(name, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        map[name] = set;
                    }
                    if (!string.IsNullOrWhiteSpace(role) && !role.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                        set.Add(role);
                }
            }

            return map.ToDictionary(kv => kv.Key, kv => string.Join(", ", kv.Value.OrderBy(v => v, StringComparer.OrdinalIgnoreCase)));
        }

        
        // Merge keys like "Name:Role" or "Name(Role)" into "Name" and sum counts.
        private Dictionary<string, int> NormalizeCreditsCounts(Dictionary<string, int> counts)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in counts ?? new Dictionary<string, int>())
            {
                var (name, _) = SplitNameRole(kv.Key);
                if (string.IsNullOrWhiteSpace(name)) continue;
                result[name] = (result.TryGetValue(name, out var c) ? c : 0) + kv.Value;
            }
            return result;
        }

        // Shared splitter used by normalizer and role parser.
        private (string name, string? role) SplitNameRole(string token)
        {
            token = (token ?? string.Empty).Trim();
            if (token.Length == 0) return (string.Empty, null);

            var colon = token.IndexOf(':');
            if (colon > 0)
            {
                var n = token.Substring(0, colon).Trim();
                var r = token.Substring(colon + 1).Trim();
                return (n, string.IsNullOrWhiteSpace(r) ? null : r);
            }
            var pm = System.Text.RegularExpressions.Regex.Match(token, @"^(?<n>.+?)\s*\((?<r>.+?)\)\s*$");
            if (pm.Success)
            {
                var n = pm.Groups["n"].Value.Trim();
                var r = pm.Groups["r"].Value.Trim();
                return (n, string.IsNullOrWhiteSpace(r) ? null : r);
            }
            return (token, null);
        }
    private void PopulateCredits(Dictionary<string, int> counts, Dictionary<string, string> rolesMap)
        {
            listCredits.BeginUpdate();
            listCredits.Items.Clear();
            foreach (var kvp in counts.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var lvi = new ListViewItem(kvp.Key);
                lvi.SubItems.Add(kvp.Value.ToString());
                rolesMap.TryGetValue(kvp.Key, out var roles);
                lvi.SubItems.Add(roles ?? string.Empty);
                listCredits.Items.Add(lvi);
            }
            listCredits.ListViewItemSorter = new ListViewItemComparer(1, false);
            listCredits.Tag = Tuple.Create(1, false);
            listCredits.Sort();
            listCredits.EndUpdate();
        }
    }
}
