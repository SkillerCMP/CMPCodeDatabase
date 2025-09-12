// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/SeeInfoForm/SeeInfoForm.Utils.cs
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
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Utility helpers for SeeInfoForm parsing (kept separate to avoid touching other partials).
    /// </summary>
    public partial class SeeInfoForm : Form
    {
        /// <summary>
        /// Splits a CSV-like string into trimmed tokens. Commas are the primary separator;
        /// surrounding whitespace is ignored. Empty tokens are dropped.
        /// Examples:
        ///  "A,B,C"      -> A B C
        ///  "A,  B ,C "  -> A B C
        ///  "A"          -> A
        /// </summary>
        private static IEnumerable<string> SplitCsv(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                yield break;

            // split on commas; preserve values that may contain internal spaces (rare for IDs/Hashes)
            foreach (var part in Regex.Split(s, @",\s*"))
            {
                var t = part.Trim();
                if (t.Length > 0)
                    yield return t;
            }
        }
    }
}
