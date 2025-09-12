// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Services/IBadgeService.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using CMPCodeDatabase.Core.Models;

namespace CMPCodeDatabase.Core.Services
{
    public interface IBadgeService
    {
        Badges GetBadgesFor(CodeEntry code);
    }
}
