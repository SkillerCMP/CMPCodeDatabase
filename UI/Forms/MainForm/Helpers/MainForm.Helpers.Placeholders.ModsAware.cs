// Drop-in replacement: ModsAware with single-arg overload that infers mod names from the code.
// Self-contained (no external helper deps) and field-less (avoids CS0102).
// Fixes CS7036 in Wrappers.cs by providing HasUnresolvedPlaceholders_ModsAware(string).

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>
        /// Normalize the base "name" of a placeholder by removing any &lt;...&gt; payloads and trimming.
        /// Example: "Item&lt;Wanted&gt;" -> "Item"
        /// </summary>
        private static string NormalizePlaceholderBaseLocal(string? raw)
            => string.IsNullOrWhiteSpace(raw) ? string.Empty : Regex.Replace(raw, "<[^>]*>", string.Empty).Trim();

        /// <summary>
        /// Infer available MOD names from the code text.
        /// Priority:
        ///  1) Curly blocks in a [MODS:] region, e.g. {MODNAME} ... {\MODNAME}
        ///  2) Fallback: placeholder base names [Name...] (excluding Amount)
        /// </summary>
        private static ISet<string> InferModNamesFromCode(string? codeText)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(codeText)) return names;

            // Collect curly block names like {MOD} ... {\MOD}
            var rxCurlyStart = new Regex(@"(?m)^\{(?<name>[A-Za-z0-9_]+)\}\s*$", RegexOptions.Compiled);
            foreach (Match m in rxCurlyStart.Matches(codeText))
            {
                var n = m.Groups["name"]?.Value;
                if (!string.IsNullOrWhiteSpace(n)) names.Add(n.Trim());
            }

            // Fallback: collect base names from [Name], [Name<...>], [Name:...]
            if (names.Count == 0)
            {
                var rxPlaceholder = new Regex(@"\[(?<name>[^\[\]\:<]+)(?:<[^>\]]+>)?(?::[^\]]+)?\]", RegexOptions.Compiled);
                foreach (Match p in rxPlaceholder.Matches(codeText))
                {
                    var baseName = NormalizePlaceholderBaseLocal(p.Groups["name"]?.Value);
                    if (string.IsNullOrEmpty(baseName)) continue;
                    if (string.Equals(baseName, "Amount", StringComparison.OrdinalIgnoreCase)) continue;
                    names.Add(baseName);
                }
            }

            return names;
        }

        /// <summary>
        /// Return true if any line contains a blocking MOD placeholder.
        /// Angle-bracket payloads are ignored for base-name matching: [Item&lt;X&gt;] == [Item].
        /// </summary>
        private static bool LineHasBlockingMods_Normalized(string? line, ISet<string> modNames)
        {
            if (string.IsNullOrEmpty(line) || modNames == null || modNames.Count == 0) return false;

            // Local regex (no class field => avoids duplicates across partials)
            var rxInsideBracket = new Regex(@"\[(?<inside>[^\]]+)\]", RegexOptions.Compiled);

            foreach (Match m in rxInsideBracket.Matches(line))
            {
                var inside = m.Groups["inside"]?.Value ?? string.Empty;
                var parts = inside.Split(':');
                var first = parts.Length > 0 ? parts[0] : string.Empty;

                // Normalize base name: "Item<Thing>" -> "Item"
                var baseName = NormalizePlaceholderBaseLocal(first);

                // Special Amount: [Amount:VALUE:NAME:TYPE] is always blocking
                if (parts.Length == 4 &&
                    string.Equals(NormalizePlaceholderBaseLocal(first), "Amount", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!string.IsNullOrEmpty(baseName) && modNames.Contains(baseName))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determine if a block of code text has unresolved MOD placeholders (blocking).
        /// Two-argument form (explicit mod name set).
        /// </summary>
        private static bool HasUnresolvedPlaceholders_ModsAware(string? codeText, ISet<string> modNames)
        {
            if (string.IsNullOrEmpty(codeText)) return false;
            var lines = codeText.Replace("\r\n", "\n").Split('\n');
            foreach (var line in lines)
            {
                if (LineHasBlockingMods_Normalized(line, modNames))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Single-argument convenience overload (legacy call sites).
        /// Infers mod names from code text (prefers {MOD} blocks; falls back to placeholder names).
        /// </summary>
        private static bool HasUnresolvedPlaceholders_ModsAware(string? codeText)
        {
            var inferred = InferModNamesFromCode(codeText);
            return HasUnresolvedPlaceholders_ModsAware(codeText, inferred);
        }
    }
}
