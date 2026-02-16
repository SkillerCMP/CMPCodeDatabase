// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Parsing/CodeTextParser.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CMPCodeDatabase.Core.Models;
using CMPCodeDatabase.Core.Parsing.Tokenizers;

namespace CMPCodeDatabase.Core.Parsing
{
    public sealed partial class CodeTextParser : ICodeParser
    {
        [GeneratedRegex(@"^\s*!(?<name>.+?)\s*$")]
        private static partial Regex GroupLineRx();

        [GeneratedRegex(@"^\s*!!\s*GroupEnd\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex SubGroupEndRx();

        [GeneratedRegex(@"^\s*\+(?<name>.+?)\s*$")]
        private static partial Regex CodeTitleRx();

        [GeneratedRegex(@"^\s*\{\s*$")]
        private static partial Regex NoteOpenRx();

        [GeneratedRegex(@"^\s*\}\s*$")]
        private static partial Regex NoteCloseRx();

        [GeneratedRegex(@"^\s*%Credits\s*:\s*(?<name>[^:]+?)(?:\s*:\s*(?<role>.+))?\s*$", RegexOptions.IgnoreCase)]
        private static partial Regex CreditLineRx();

        public Game ParseGame(string gameId, string gameName, string folderPath, IEnumerable<(string fileName, string text)> files)
        {
            var game = new Game(gameId, gameName, folderPath);
            var currentGroup = (CodeGroup?)null;
            var currentCode  = (CodeEntry?)null;
            var currentCodeBody = new StringBuilder();
            var inTopNote = true;
            List<string> topNoteBuf = [];
            var inTopNoteBlock = false;
            var inCodeNoteBlock = false;
            List<string> codeNoteBuf = [];

            void SealCurrentCode()
            {
                if (currentCode != null)
                {
                    currentCode.Raw = currentCodeBody.ToString().TrimEnd();
                    if (currentCode.NoteHtml is null && codeNoteBuf.Count > 0)
                        currentCode.NoteHtml = string.Join("\n", codeNoteBuf);
                }
                currentCode = null;
                currentCodeBody.Clear();
                codeNoteBuf.Clear();
                inCodeNoteBlock = false;
            }

            foreach (var (fileName, text) in files)
            {
                foreach (var rawLine in SplitLines(text))
                {
                    var line = rawLine.TrimEnd();

                    if (MetadataTokenizer.TryParseMetadata(line, out var meta))
                        game.Metadata.Add(meta);

                    var mCred = CreditLineRx().Match(line);
                    if (mCred.Success)
                    {
                        var name = mCred.Groups["name"].Value.Trim();
                        var role = mCred.Groups["role"].Success ? mCred.Groups["role"].Value.Trim() : null;
                        if (!string.IsNullOrEmpty(name)) game.Credits.Add(new Credit(name, role));
                        continue;
                    }

                    var mg = GroupLineRx().Match(line);
                    if (mg.Success)
                    {
                        SealCurrentCode();
                        currentGroup = new CodeGroup(mg.Groups["name"].Value);
                        game.Groups.Add(currentGroup);
                        inTopNote = false;
                        continue;
                    }

                    if (SubGroupEndRx().IsMatch(line))
                    {
                        SealCurrentCode();
                        currentGroup = null;
                        continue;
                    }

                    var mc = CodeTitleRx().Match(line);
                    if (mc.Success)
                    {
                        SealCurrentCode();
                        currentCode = new CodeEntry(mc.Groups["name"].Value, raw: string.Empty);
                        currentGroup ??= new CodeGroup("(Ungrouped)");
                        currentGroup.Codes.Add(currentCode);
                        inTopNote = false;
                        continue;
                    }

                    if (inTopNote)
                    {
                        if (NoteOpenRx().IsMatch(line)) { topNoteBuf.Clear(); inTopNoteBlock = true; continue; }
                        if (inTopNoteBlock && NoteCloseRx().IsMatch(line))
                        {
                            game.TopNoteHtml = string.Join("\n", topNoteBuf);
                            inTopNoteBlock = false;
                            continue;
                        }
                        if (inTopNoteBlock) { topNoteBuf.Add(line); continue; }
                    }

                    if (currentCode != null)
                    {
                        currentCodeBody.AppendLine(rawLine);

                        if (currentCode.NoteHtml == null)
                        {
                            if (!inCodeNoteBlock && NoteOpenRx().IsMatch(line))
                            {
                                codeNoteBuf.Clear();
                                inCodeNoteBlock = true;
                                continue;
                            }
                            if (inCodeNoteBlock && NoteCloseRx().IsMatch(line))
                            {
                                currentCode.NoteHtml = string.Join("\n", codeNoteBuf);
                                inCodeNoteBlock = false;
                                continue;
                            }
                            if (inCodeNoteBlock)
                            {
                                codeNoteBuf.Add(line);
                                continue;
                            }
                        }
                    }
                }
                SealCurrentCode();
            }
            return game;
        }

        private static IEnumerable<string> SplitLines(string text)
        {
            using var reader = new StringReader(text ?? string.Empty);
            string? line;
            while ((line = reader.ReadLine()) is not null) yield return line;
        }
    }
}
