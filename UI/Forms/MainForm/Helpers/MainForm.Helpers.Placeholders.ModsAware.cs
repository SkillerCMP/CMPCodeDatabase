// Drop-in: ModsAware v4 (angle-safe, declared-mod aware)
// Purpose:
// - Treat [NAME] and [NAME<...>] as the SAME placeholder (base NAME).
// - When a set of declared mods is provided (from ^6 = MODS:), only those names count
//   for "-M-" gating. [AMOUNT:...:...:...] remains always blocking.
// - Ignore human-readable headers like "[Item Swap (...)]".
//
// This file is self-contained and uses no class fields.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>Strip any &lt;...&gt; payload and trim.</summary>
        private static string PlaceholderBase(string? raw)
            => string.IsNullOrWhiteSpace(raw) ? string.Empty : Regex.Replace(raw, "<[^>]*>", string.Empty).Trim();

        /// <summary>True for clean identifier (A-Za-z0-9_ only).</summary>
        private static bool IsIdentifier(string s) => !string.IsNullOrEmpty(s) && Regex.IsMatch(s, @"^[A-Za-z0-9_]+$");

        /// <summary>Tokens like "[Item Swap (...)]" are NOT placeholders.</summary>
        private static bool LooksLikePlaceholder(string inside)
        {
            if (string.IsNullOrWhiteSpace(inside)) return false;
            if (inside.IndexOf('<') >= 0 || inside.IndexOf(':') >= 0) return true; // [NAME<...>] or [NAME:...]
            return IsIdentifier(inside.Trim()); // bare [NAME]
        }

        /// <summary>
        /// Angle-safe unresolved check using a DECLARED mod set. Base name only.
        /// Examples that BLOCK:
        ///   [Item], [Item<Thing>], [Size:...], [Amount:V:N:T]
        /// Only counts when base name exists in 'declared' (except Amount).
        /// </summary>
        private static bool HasUnresolvedPlaceholders_ModsAware(string? codeText, ISet<string> declared)
        {
            if (string.IsNullOrEmpty(codeText)) return false;
            declared ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var rx = new Regex(@"\[(?<inside>[^\]]+)\]");
            foreach (Match m in rx.Matches(codeText))
            {
                var inside = (m.Groups["inside"]?.Value ?? string.Empty).Trim();
                if (!LooksLikePlaceholder(inside)) continue;

                var parts = inside.Split(':');
                var first = parts.Length > 0 ? parts[0] : string.Empty;
                var baseName = PlaceholderBase(first);

                // Special MODs that are ALWAYS blocking (regardless of declared set)
                if (string.Equals(baseName, "Amount", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(baseName, "Joker", StringComparison.OrdinalIgnoreCase))  return true;
                if (string.Equals(baseName, "STAR", StringComparison.OrdinalIgnoreCase))   return true;

                if (!IsIdentifier(baseName)) continue;

                // Only unresolved if declared set contains the base name
                if (declared.Contains(baseName)) return true;
            }
            return false;
        }

        /// <summary>
        /// Legacy single-arg overload: try to infer mod names from {MOD} blocks, else block on any placeholder.
        /// </summary>
        private static bool HasUnresolvedPlaceholders_ModsAware(string? codeText)
        {
            var names = InferDeclaredModNames(codeText);
            if (names.Count == 0) names.Add("__ANY__"); // sentinel
            return HasUnresolvedPlaceholders_ModsAware(codeText, names.Contains("__ANY__") ? new HashSet<string>() : names);
        }

        /// <summary>Infer declared mod names from a MODS section: [Name] ... [/Name] or {NAME} ... {\NAME}.</summary>
        private static ISet<string> InferDeclaredModNames(string? codeText)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(codeText)) return names;

            // [Name] ... [/Name]
            var rxSquare = new Regex(@"\[(?<name>[A-Za-z0-9_]+)\]\s*(?:.|\n)*?\[\/\k<name>\]", RegexOptions.Compiled);
            foreach (Match m in rxSquare.Matches(codeText))
            {
                var n = m.Groups["name"]?.Value;
                if (!string.IsNullOrWhiteSpace(n)) names.Add(n.Trim());
            }

            // {NAME} ... {\NAME}
            var rxCurly = new Regex(@"\{(?<name>[A-Za-z0-9_]+)\}(?:.|\n)*?\{\\\k<name>\}", RegexOptions.Compiled);
            foreach (Match m in rxCurly.Matches(codeText))
            {
                var n = m.Groups["name"]?.Value;
                if (!string.IsNullOrWhiteSpace(n)) names.Add(n.Trim());
            }

            return names;
        }
    }
}
