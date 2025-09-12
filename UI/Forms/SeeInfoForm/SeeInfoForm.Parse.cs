// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/SeeInfoForm/SeeInfoForm.Parse.cs
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
        private DataTable ParseIdHash(string text)
                {
                    var dt = new DataTable();
                    dt.Columns.Add("Game ID");
                    dt.Columns.Add("Hash");

                    var ids = new List<string>();
                    var hashes = new List<string>();

                    foreach (Match m in Regex.Matches(text, @"^\s*\^1\s*=\s*Hash:\s*(.+)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                        hashes.AddRange(SplitCsv(m.Groups[1].Value));
                    foreach (Match m in Regex.Matches(text, @"^\s*\^2\s*=\s*GameID:\s*(.+)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                        ids.AddRange(SplitCsv(m.Groups[1].Value));

                    foreach (Match m in Regex.Matches(text, @"^\s*Hash:\s*(.+)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                        hashes.AddRange(SplitCsv(m.Groups[1].Value));
                    foreach (Match m in Regex.Matches(text, @"^\s*GameID:\s*(.+)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                        ids.AddRange(SplitCsv(m.Groups[1].Value));

                    int n = Math.Max(ids.Count, hashes.Count);
                    for (int i = 0; i < n; i++)
                    {
                        var id = i < ids.Count ? ids[i].Trim() : "";
                        var hash = i < hashes.Count ? hashes[i].Trim() : "";
                        dt.Rows.Add(id, hash);
                    }
                    return dt;
                }

                private Dictionary<string, int> ParseCredits(string text)
                {
                    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (Match m in Regex.Matches(text, @"^\s*%Credits:\s*(.+)$", RegexOptions.Multiline))
                    {
                        var payload = m.Groups[1].Value;
                        var names = payload.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .Where(s => s.Length > 0);
                        foreach (var name in names)
                            counts[name] = counts.TryGetValue(name, out var c) ? c + 1 : 1;
                    }
                    return counts;
                }

                private string? ParseTopGameNoteHtml(string text)
                {
                    var lines = (text ?? "").Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    var buf = new List<string>();
                    bool anyReal = false;

                    foreach (var raw in lines)
                    {
                        var line = raw ?? "";
                        var trimmed = line.Trim();

                        if (trimmed.StartsWith("!") || trimmed.StartsWith("+"))
                            break;

                        // Ignore meta/control lines at top
                        if (trimmed.StartsWith("^") || trimmed.StartsWith("%") || trimmed.StartsWith("$"))
                            continue;

                        if (trimmed.Length == 0)
                        {
                            if (anyReal) buf.Add(""); // keep blank only once we started
                            continue;
                        }

                        anyReal = true;
                        buf.Add(line);
                    }

                    if (!anyReal) return null;

                    var header = string.Join("\n", buf).Trim();
                    if (string.IsNullOrWhiteSpace(header)) return null;

                    // If it looks like HTML, render as-is
                    if (Regex.IsMatch(header, @"</?\w+", RegexOptions.IgnoreCase))
                        return header;

                    return "<pre>" + System.Net.WebUtility.HtmlEncode(header) + "</pre>";
                }
    }
}
