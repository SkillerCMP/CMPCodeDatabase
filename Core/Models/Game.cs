// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Models/Game.cs
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

namespace CMPCodeDatabase.Core.Models
{
    public sealed class Game
    {
        public string Id { get; }
        public string Name { get; }
        public string FolderPath { get; }
        public List<CodeGroup> Groups { get; } = new();
        public List<Metadata> Metadata { get; } = new();
        public List<Credit> Credits { get; } = new();
        public string? TopNoteHtml { get; set; }

        public Game(string id, string name, string folderPath)
        {
            Id = id;
            Name = name;
            FolderPath = folderPath;
        }
    }
}
