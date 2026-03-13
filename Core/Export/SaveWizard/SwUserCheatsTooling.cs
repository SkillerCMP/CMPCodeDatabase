// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Export/SaveWizard/SwUserCheatsTooling.cs
// Purpose: Save Wizard swusercheats.xml helpers + SW gamelist.xml parser.
// Notes:
//  • Based on user's standalone SwUserCheatsTool (C# 14 / .NET 10).
//  • SW Quick Mode matches by concatenated id: <game id="{gameId}{containerKey}">.
//  • This tooling is used by the Collector export: Export To → Save Wizard → swusercheats.xml
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CMPCodeDatabase.Core.Export.SaveWizard
{
    internal sealed record GameListGame(
        string PrimaryId,
        IReadOnlyList<string> AliasIds,
        IReadOnlyList<GameListContainer> Containers);

    internal sealed record GameListContainer(
        string Key,
        string? Pfs,
        IReadOnlyList<string> FilePatterns);

    internal static class SwGameListParser
    {
        public static List<GameListGame> Load(string path)
        {
            var doc = XDocument.Load(path, LoadOptions.None);
            var games = new List<GameListGame>();

            foreach (var g in doc.Descendants("game"))
            {
                var primaryId = (string?)g.Element("id") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(primaryId))
                    continue;

                var aliases = g.Descendants("aliases").Descendants("alias")
                    .Select(a => (string?)a.Element("id"))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var containers = new List<GameListContainer>();
                foreach (var c in g.Descendants("containers").Elements("container"))
                {
                    var key = (string?)c.Element("key") ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    var pfs = (string?)c.Element("pfs");

                    var filePatterns = c.Descendants("files").Elements("file")
                        .Select(f => (string?)f.Element("filename"))
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Cast<string>()
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    containers.Add(new GameListContainer(key, pfs, filePatterns));
                }

                games.Add(new GameListGame(primaryId, aliases, containers));
            }

            return games;
        }

        public static GameListGame? FindGameByAnyId(List<GameListGame> games, string id)
        {
            return games.FirstOrDefault(g =>
                g.PrimaryId.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                g.AliasIds.Any(a => a.Equals(id, StringComparison.OrdinalIgnoreCase)));
        }
    }

    internal sealed class SwUserCheats
    {
        public XDocument Doc { get; }
        public XElement Root => Doc.Root ?? throw new InvalidOperationException("Missing <usercheats> root.");

        private SwUserCheats(XDocument doc) => Doc = doc;

        public static SwUserCheats LoadOrCreate(string path)
        {
            if (!File.Exists(path))
                return new SwUserCheats(new XDocument(new XElement("usercheats")));

            try
            {
                return new SwUserCheats(XDocument.Load(path, LoadOptions.PreserveWhitespace));
            }
            catch
            {
                // mimic SW's naive recovery for broken '&'
                var xml = File.ReadAllText(path, Encoding.UTF8).Replace("&", "&amp;");
                return new SwUserCheats(XDocument.Parse(xml, LoadOptions.PreserveWhitespace));
            }
        }

        public void EnsureGameAndFiles(string swGameId, IReadOnlyList<string> filePatterns)
        {
            var game = Root.Elements("game")
                .FirstOrDefault(g => ((string?)g.Attribute("id") ?? string.Empty) == swGameId);

            if (game is null)
            {
                game = new XElement("game", new XAttribute("id", swGameId));
                Root.Add(game);
            }

            foreach (var fp in filePatterns)
            {
                var file = game.Elements("file")
                    .FirstOrDefault(f => ((string?)f.Attribute("name") ?? string.Empty) == fp);

                if (file is null)
                    game.Add(new XElement("file", new XAttribute("name", fp)));
            }
        }

        public void AddOrReplaceCheat(string swGameId, string fileName, string desc, string comment, string codePairs)
        {
            EnsureGameAndFiles(swGameId, new[] { fileName });

            var game = Root.Elements("game").First(g => ((string?)g.Attribute("id") ?? string.Empty) == swGameId);
            var file = game.Elements("file").First(f => ((string?)f.Attribute("name") ?? string.Empty) == fileName);

            var existing = file.Elements("cheat")
                .FirstOrDefault(c => string.Equals((string?)c.Attribute("desc"), desc, StringComparison.Ordinal));

            if (existing is null)
            {
                file.Add(new XElement("cheat",
                    new XAttribute("desc", desc),
                    new XAttribute("comment", comment),
                    new XElement("code", codePairs)));
            }
            else
            {
                existing.SetAttributeValue("comment", comment);
                var codeElem = existing.Element("code");
                if (codeElem is null) existing.Add(new XElement("code", codePairs));
                else codeElem.Value = codePairs;
            }
        }

        public void Save(string path)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = true
            };

            using var xw = XmlWriter.Create(path, settings);
            Doc.Save(xw);
        }
    }

    internal static class SwCodeNormalize
    {
        public static string NormalizePairs(string code)
        {
            code = code.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", " ");
            code = System.Text.RegularExpressions.Regex.Replace(code, @"\s+", " ").Trim();
            return code;
        }

        public static bool HasEvenTokenCount(string code)
        {
            var tokens = code.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length > 0 && tokens.Length % 2 == 0;
        }
    }
}
