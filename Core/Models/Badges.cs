// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Models/Badges.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;

namespace CMPCodeDatabase.Core.Models
{
    [Flags]
    public enum Badges
    {
        None = 0,
        HasMods = 1,
        HasNote = 2
    }
}
