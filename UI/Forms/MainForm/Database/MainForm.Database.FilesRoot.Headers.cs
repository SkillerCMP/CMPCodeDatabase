// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.FilesRoot.Headers.cs
// Purpose: Header/name/GameID parsing helpers for Files\Database top-level .txt games.
// Notes:
//  • Split from MainForm.Database.FilesRoot.cs during cleanup pass 18.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private static readonly Regex HeaderGameIdRegex = new(@"\b[A-Za-z]{4}\d{5}\b", RegexOptions.Compiled);
        private static readonly Regex HeaderNameRegex = new(@"^\^3\s*=\s*NAME\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string? TryReadGameIdFromTxt(string txtPath)
        {
            var ids = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in System.IO.File.ReadLines(txtPath))
            {
                var s = line.Trim();
                if (s.StartsWith("^2", StringComparison.Ordinal) || s.StartsWith(";", StringComparison.Ordinal))
                {
                    var idx = s.IndexOf("GameID:", StringComparison.OrdinalIgnoreCase);
                    var tokenLen = "GameID:".Length;
                    if (idx < 0)
                    {
                        idx = s.IndexOf("Game ID:", StringComparison.OrdinalIgnoreCase);
                        tokenLen = "Game ID:".Length;
                    }
                    if (idx >= 0)
                    {
                        var val = s.Substring(idx + tokenLen);
                        foreach (Match m in HeaderGameIdRegex.Matches(val))
                        {
                            var id = m.Value.Trim().ToUpperInvariant();
                            if (seen.Add(id)) ids.Add(id);
                        }
                    }
                }
                else if (ids.Count > 0 && !s.StartsWith("^", StringComparison.Ordinal))
                {
                    // stop once we've captured IDs and header markers have ended
                    break;
                }
            }

            return ids.Count == 0 ? null : string.Join(",", ids);
        }

        private static (string? Name, string? GameId) TryReadHeader(string txtPath)
        {
            string? name = null;
            var ids = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in System.IO.File.ReadLines(txtPath))
            {
                var s = line.Trim();
                if (s.StartsWith("^3", StringComparison.Ordinal))
                {
                    var ix = s.IndexOf("NAME:", StringComparison.OrdinalIgnoreCase);
                    if (ix >= 0) name = s.Substring(ix + "NAME:".Length).Trim().Trim('"');
                }
                else if (s.StartsWith("^2", StringComparison.Ordinal))
                {
                    var ix = s.IndexOf("GameID:", StringComparison.OrdinalIgnoreCase);
                    if (ix >= 0)
                    {
                        var val = s.Substring(ix + "GameID:".Length);
                        foreach (Match m in HeaderGameIdRegex.Matches(val))
                        {
                            var id = m.Value.Trim().ToUpperInvariant();
                            if (seen.Add(id)) ids.Add(id);
                        }
                    }
                }

                // Header ends when we hit a non-meta line (or first group)
                if ((name != null || ids.Count > 0) && (s.StartsWith("[", StringComparison.Ordinal) || (!s.StartsWith("^", StringComparison.Ordinal) && s.Length > 0)))
                    break;
            }

            var idCsv = ids.Count == 0 ? null : string.Join(",", ids);
            return (name, idCsv);
        }

        private static string CleanDisplayNameFromFile(string filePath)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            if (name.EndsWith(".CMP", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);
            return name.Trim();
        }

        /// <summary>
        /// Scan a .txt for '^3 = NAME: ...' and return the value if present; otherwise null.
        /// </summary>
        private string? TryReadGameNameFromTxt(string file)
        {
            try
            {
                foreach (var raw in File.ReadLines(file))
                {
                    // Trim and skip empties quickly
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var line = raw.Trim();

                    // Support variations with/without spaces around '=' and case-insensitive NAME
                    // Examples:
                    //   ^3 = NAME: Lies of P
                    //   ^3=NAME: Elden Ring
                    //   ^3 = name: Something
                    var m = HeaderNameRegex.Match(line);
                    if (m.Success)
                    {
                        var name = m.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(name)) return name;
                    }

                    // Stop early if file has moved past headings (optional optimization)
                    if (line.StartsWith("^") && !line.StartsWith("^3", StringComparison.Ordinal))
                        break;
                }
            }
            catch { /* ignore and fall back */ }
            return null;
        }

    }
}
