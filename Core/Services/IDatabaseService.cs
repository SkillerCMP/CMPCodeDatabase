// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Services/IDatabaseService.cs
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
using System.Threading.Tasks;
using CMPCodeDatabase.Core.Models;

namespace CMPCodeDatabase.Core.Services
{
    public interface IDatabaseService
    {
        Task<IReadOnlyList<Game>> LoadGamesAsync(string rootFolder);
        Task RefreshAsync();
        event EventHandler? DatabaseChanged;
    }
}
