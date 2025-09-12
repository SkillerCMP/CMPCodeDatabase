// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Models/CodeEntry.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

namespace CMPCodeDatabase.Core.Models
{
    public sealed class CodeEntry
    {
        public string Name { get; }
        public string Raw { get; internal set; }
        public string? NoteHtml { get; set; }
        public Badges BadgeFlags { get; set; } = Badges.None;

        public CodeEntry(string name, string raw)
        {
            Name = name ?? string.Empty;
            Raw = raw ?? string.Empty;
        }
    }
}
