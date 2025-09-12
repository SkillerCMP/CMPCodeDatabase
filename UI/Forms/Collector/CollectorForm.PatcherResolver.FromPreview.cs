// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.PatcherResolver.FromPreview.cs
// Purpose: Helpers to resolve filesystem paths and database roots.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        internal string? TryReadDatabaseNameFromSavepatch(string? savepatchPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(savepatchPath) || !File.Exists(savepatchPath)) return null;
                int scanned = 0;
                foreach (var raw in File.ReadLines(savepatchPath))
                {
                    if (raw == null) continue;
                    var line = raw.Trim();
                    if (line.Length == 0) { scanned++; if (scanned > 10) break; continue; }

                    if (line.StartsWith("; Database:", StringComparison.OrdinalIgnoreCase))
                        return line.Substring(line.IndexOf(':') + 1).Trim();

                    if (line.StartsWith(";", StringComparison.Ordinal))
                    {
                        var s = line.TrimStart(';', ' ');
                        int ix = s.IndexOf("CMP", StringComparison.OrdinalIgnoreCase);
                        if (ix > 0) return s.Substring(0, ix).Trim();
                    }

                    scanned++; if (scanned > 10) break;
                }
            }
            catch { }
            return null;
        }
    }
}
