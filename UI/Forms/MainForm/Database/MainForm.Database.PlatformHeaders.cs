// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.PlatformHeaders.cs
// Purpose: Special MODS-section platform header support for code preview/collector.
// Notes:
//  • Pass 27 targeted feature: [PS1HEADER], [PS2HEADER], [PSPHEADER] blocks under ^6 = MODS:.
//  • Header lines are prepended to each parsed code for display and Collector transfer.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private static readonly Regex PlatformHeaderOpenRegex = new(
            @"^\[(?<tag>PS[0-9A-Z]+HEADER)\]\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PlatformHeaderCloseRegex = new(
            @"^\[/\s*(?<tag>PS[0-9A-Z]+HEADER)\s*\]\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PlatformTokenRegex = new(
            @"\[(?<platform>PS1|PS2|PSP)\]|\b(?<platform>PS1|PS2|PSP)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private bool TryHandlePlatformHeaderModsLine(
            string line,
            Dictionary<string, List<string>> platformHeaders,
            ref string? currentPlatformHeaderTag,
            ref List<string>? currentPlatformHeaderLines)
        {
            var trimmed = (line ?? string.Empty).Trim();

            if (!string.IsNullOrEmpty(currentPlatformHeaderTag))
            {
                var close = PlatformHeaderCloseRegex.Match(trimmed);
                if (close.Success && string.Equals(close.Groups["tag"].Value, currentPlatformHeaderTag, StringComparison.OrdinalIgnoreCase))
                {
                    if (currentPlatformHeaderLines != null && currentPlatformHeaderLines.Count > 0)
                        platformHeaders[currentPlatformHeaderTag] = new List<string>(currentPlatformHeaderLines);

                    currentPlatformHeaderTag = null;
                    currentPlatformHeaderLines = null;
                    return true;
                }

                var headerLine = NormalizePlatformHeaderCodeLine(line);
                if (!string.IsNullOrWhiteSpace(headerLine))
                    currentPlatformHeaderLines!.Add(headerLine);

                return true;
            }

            var open = PlatformHeaderOpenRegex.Match(trimmed);
            if (!open.Success)
                return false;

            currentPlatformHeaderTag = open.Groups["tag"].Value.ToUpperInvariant();
            currentPlatformHeaderLines = new List<string>();
            return true;
        }

        private static string NormalizePlatformHeaderCodeLine(string? line)
        {
            var text = (line ?? string.Empty).Trim();

            // Code lines are stored internally without a leading '$'. Keep platform headers consistent
            // with normal parsed CMP code lines so preview, Collector, and exports all see the same shape.
            if (text.StartsWith("$", StringComparison.Ordinal))
                text = text.Substring(1).TrimStart();

            return text.TrimEnd();
        }

        private void ApplyPlatformHeaderToCodeNodes(
            string filePath,
            Dictionary<string, List<string>> platformHeaders,
            List<TreeNode> fileCodeNodes)
        {
            if (platformHeaders.Count == 0 || fileCodeNodes.Count == 0)
                return;

            if (!TryResolvePlatformHeaderForFile(filePath, platformHeaders, out var headerLines))
                return;

            var headerBlock = string.Join(Environment.NewLine, headerLines.Where(l => !string.IsNullOrWhiteSpace(l)));
            if (string.IsNullOrWhiteSpace(headerBlock))
                return;

            foreach (var node in fileCodeNodes)
            {
                if (node == null)
                    continue;

                if (originalCodeTemplates.TryGetValue(node, out var template))
                    originalCodeTemplates[node] = CombinePlatformHeaderAndCode(headerBlock, template);

                var working = node.Tag as string ?? node.Tag?.ToString() ?? string.Empty;
                node.Tag = CombinePlatformHeaderAndCode(headerBlock, working);
            }
        }

        private static bool TryResolvePlatformHeaderForFile(
            string filePath,
            Dictionary<string, List<string>> platformHeaders,
            out List<string> headerLines)
        {
            headerLines = new List<string>();

            var preferredTag = InferPlatformHeaderTag(filePath);
            if (!string.IsNullOrWhiteSpace(preferredTag) && platformHeaders.TryGetValue(preferredTag, out var matched))
            {
                headerLines = matched;
                return headerLines.Count > 0;
            }

            // If the file declares exactly one platform header, use it even when the filename does not
            // expose a platform token. This keeps single-header CMP files simple.
            if (platformHeaders.Count == 1)
            {
                headerLines = platformHeaders.Values.First();
                return headerLines.Count > 0;
            }

            return false;
        }

        private static string? InferPlatformHeaderTag(string filePath)
        {
            var fileName = Path.GetFileName(filePath) ?? string.Empty;
            var m = PlatformTokenRegex.Match(fileName);
            if (!m.Success)
                m = PlatformTokenRegex.Match(filePath ?? string.Empty);

            if (!m.Success)
                return null;

            var platform = m.Groups["platform"].Value.ToUpperInvariant();
            return platform.Length == 0 ? null : platform + "HEADER";
        }

        private static string CombinePlatformHeaderAndCode(string headerBlock, string? codeBody)
        {
            var header = (headerBlock ?? string.Empty).Trim();
            var body = (codeBody ?? string.Empty).Trim();

            if (header.Length == 0)
                return body;

            if (body.Length == 0)
                return header;

            // Avoid doubling the header if a caller re-applies during reload/refresh flow.
            if (body.StartsWith(header, StringComparison.OrdinalIgnoreCase))
                return body;

            return header + Environment.NewLine + body;
        }
    }
}
