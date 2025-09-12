// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Services/FileDatabaseService.cs
// Purpose: Database discovery, selector, and tree building.
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMPCodeDatabase.Core.Models;
using CMPCodeDatabase.Core.Parsing;

namespace CMPCodeDatabase.Core.Services
{
    public sealed class FileDatabaseService : IDatabaseService
    {
        private readonly ICodeParser _parser;
        private List<Game> _games = new();

        public FileDatabaseService() : this(new CodeTextParser()) { }
        public FileDatabaseService(ICodeParser parser) { _parser = parser; }

        public event EventHandler? DatabaseChanged;

        public Task<IReadOnlyList<Game>> LoadGamesAsync(string rootFolder)
        {
            if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
                return Task.FromResult((IReadOnlyList<Game>)Array.Empty<Game>());

            var games = new List<Game>();
            // Any folder with *.txt is a game folder; recurse to support vendor/category levels.
            foreach (var dir in Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories))
            {
                var txts = Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly).ToList();
                if (txts.Count == 0) continue;
                var name = Path.GetFileName(dir);
                var files = txts.Select(p => (Path.GetFileName(p), File.ReadAllText(p)));
                games.Add(_parser.ParseGame(name, name, dir, files));
            }
            // Fallback to one level (legacy)
            if (games.Count == 0)
            {
                foreach (var dir in Directory.GetDirectories(rootFolder))
                {
                    var name = Path.GetFileName(dir);
                    var files = Directory.EnumerateFiles(dir, "*.txt").Select(p => (Path.GetFileName(p), File.ReadAllText(p)));
                    if (!files.Any()) continue;
                    games.Add(_parser.ParseGame(name, name, dir, files));
                }
            }
            _games = games.OrderBy(g => g.Name, System.StringComparer.OrdinalIgnoreCase).ToList();
            DatabaseChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult((IReadOnlyList<Game>)_games);
        }

        public Task RefreshAsync()
        {
            DatabaseChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }
}
