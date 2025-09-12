// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Models/Metadata.cs
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
    public sealed class Metadata
    {
        public string Kind { get; } // "Hash" or "GameID"
        public string Value { get; }
        public Metadata(string kind, string value) { Kind = kind; Value = value; }
    }
}
