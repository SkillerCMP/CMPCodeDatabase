// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Patching/PatchBuilder.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CMPCodeDatabase.Formatters; // SwApolloFormatter

namespace CMPCodeDatabase.Patching
{
    /// <summary>
    /// Composes Apollo-style .savepatch text and writes it to a temp or target path.
    /// Each block is:
    /// [Name]
    /// XXXXXXXX YYYYYYYY
    /// XXXXXXXX YYYYYYYY
    /// ...
    /// Mixed/script lines (search/Insert Next/JSON/[Amount:...]) are kept verbatim.
    /// Hex-only lines are normalized to SW pairs with padding on a trailing odd token.
    /// </summary>
    public static class PatchBuilder
    {
        /// <summary>
        /// Build a .savepatch file from a dictionary of name -> code.
        /// Returns the full path to the written file.
        /// </summary>
        public static string BuildSavePatch(IDictionary<string, string> nameToCode, bool padPairs = true, string? targetPath = null)
            => BuildSavePatch((IEnumerable<KeyValuePair<string, string>>)nameToCode, padPairs, targetPath);

        /// <summary>
        /// Build a .savepatch file from a set of name/code pairs.
        /// Returns the full path to the written file.
        /// </summary>
        public static string BuildSavePatch(IEnumerable<KeyValuePair<string, string>> items, bool padPairs = true, string? targetPath = null)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            var text = ComposeSavePatchText(items, padPairs);
            return WriteSavePatch(text, targetPath);
        }

        /// <summary>
        /// Build a .savepatch file from tuples.
        /// </summary>
        public static string BuildSavePatch(IEnumerable<(string Name, string Code)> items, bool padPairs = true, string? targetPath = null)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            return BuildSavePatch(items.Select(t => new KeyValuePair<string, string>(t.Name, t.Code)), padPairs, targetPath);
        }

        /// <summary>
        /// Compose the .savepatch content (without writing to disk).
        /// </summary>
        public static string ComposeSavePatchText(IEnumerable<KeyValuePair<string, string>> items, bool padPairs = true)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            var sb = new StringBuilder(4096);
            foreach (var kv in items)
            {
                var name = (kv.Key ?? string.Empty).Trim();
                var raw  = kv.Value ?? string.Empty;

                // Normalize via Apollo SW to ensure pairs and padding for hex-only lines.
                var normalized = SwApolloFormatter.NormalizeSwBlocksForCollector(raw);

                // Note: if padPairs == false, we could strip trailing " 00000000" produced by padding.
                // Apollo tools typically expect padding, so we keep it.
                sb.Append('[').Append(name).Append(']').AppendLine();
                sb.AppendLine(normalized);
                sb.AppendLine(); // blank line between blocks
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Write text to a .savepatch file. If targetPath is null, creates a file in %TEMP%.
        /// </summary>
        public static string WriteSavePatch(string content, string? targetPath = null)
        {
            if (string.IsNullOrEmpty(content)) throw new ArgumentException("No content to write.", nameof(content));

            string path = targetPath ?? Path.Combine(Path.GetTempPath(), $"cmp_{DateTime.Now:yyyyMMdd_HHmmss_fff}.savepatch");
            // UTF-8 without BOM; Apollo tools accept this for ASCII/hex content.
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(path, content, utf8NoBom);
            return path;
        }
    }
}
