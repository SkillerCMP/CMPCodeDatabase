// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.ParseCodeFiles.Mods.cs
// Purpose: MODS-section parser helpers used by ParseCodeFilesInFolder.
// Notes:
//  • Cleanup pass split only; keeps MODS parsing behavior aligned with Pass 21.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private static bool UpdateModsSectionState(string raw, bool inModsSection)
        {
            // --- MODS guard: enter on exact '^6 = MODS:' and leave on any other caret header ---
            var trim = (raw ?? string.Empty).Trim();
            if (trim.StartsWith("^6 = MODS:", StringComparison.Ordinal)) return true;
            if (trim.StartsWith("^", StringComparison.Ordinal) && !trim.StartsWith("^6 = MODS:", StringComparison.Ordinal)) return false;
            return inModsSection;
        }

        private void HandleModBlockBoundaryLine(string line, ref string? currentModTag, ref List<(string Value, string Name)>? currentModList, ref bool headerCollected)
        {
            if (line.StartsWith("[/"))
            {
                if (!string.IsNullOrEmpty(currentModTag) && currentModList != null)
                    modDefinitions[currentModTag] = new List<(string, string)>(currentModList);
                currentModTag = null;
                currentModList = null;
                headerCollected = false;
            }
            else
            {
                currentModTag = line.Trim('[', ']');
                currentModList = new List<(string, string)>();
                headerCollected = false;
            }
        }

        private void HandleCurrentModLine(string line, string currentModTag, List<(string Value, string Name)>? currentModList, ref bool headerCollected)
        {
            // Inside a MOD block
            if (!headerCollected)
            {
                // Detect headers line if it contains '>' but not '='
                if (line.Contains('>') && !line.Contains('='))
                {
                    var headers = line.Split('>').Select(h => h.Trim()).Where(h => h.Length > 0).ToList();
                    if (headers.Count >= 2)
                    {
                        modHeaders[currentModTag] = headers;
                        modRows[currentModTag] = new List<string[]>();
                        headerCollected = true;
                        return;
                    }
                }
                // Otherwise fall through as data for simple pairs
            }

            // Data rows
            if (modHeaders.ContainsKey(currentModTag))
            {
                // headered table
                var headers = modHeaders[currentModTag];
                string[] parts;
                if (line.Contains('\t')) parts = line.Split('\t');
                else if (line.Contains('>')) parts = line.Split('>');
                else if (line.Contains('=')) parts = line.Split('='); // allow VALUE=NAME when exactly 2 headers
                else parts = new[] { line };

                parts = parts.Select(p => p.Trim()).ToArray();

                // Accept rows that have at least VALUE + NAME; INFO is optional.
                // (ModGridDialog already pads missing cells to empty string.)
                if (parts.Length >= 2)
                {
                    modRows[currentModTag].Add(parts);
                    currentModList!.Add((parts[0], parts[1]));
                }
            }
            else
            {
                // simple pair lines: "VAL=NAME" or "VAL<TAB>NAME"
                if (line.Contains('='))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                        currentModList!.Add((parts[0].Trim(), parts[1].Trim()));
                }
                else if (line.Contains('\t'))
                {
                    var parts = line.Split('\t');
                    if (parts.Length >= 2)
                        currentModList!.Add((parts[0].Trim(), parts[1].Trim()));
                }
            }
        }
    }
}
