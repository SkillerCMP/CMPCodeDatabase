// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Infrastructure/LegacyCompat.cs
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
using System.Threading.Tasks;
using CMPCodeDatabase.Core.Export;
using CMPCodeDatabase.Core.Models;
using CMPCodeDatabase.Core.Services;

namespace CMPCodeDatabase.Core.Infrastructure
{
    /// <summary>
    /// Optional convenience layer so old code can use a few simple APIs immediately.
    /// </summary>
    public static class LegacyCompat
    {
        public static Task<IReadOnlyList<Game>> LoadGamesAsync(string root)
            => AppServices.Get<IDatabaseService>().LoadGamesAsync(root);

        public static Badges GetBadgesFor(CodeEntry code)
            => AppServices.Get<IBadgeService>().GetBadgesFor(code);

        public static string GetLegendHtml()
            => AppServices.Get<IHelpContentService>().GetCodeTextLegendHtml();

        public static Task<string> ExportCollectorAsync(string destinationFile)
            => AppServices.Get<IExportService>().ExportCollectorAsync(destinationFile);
    }
}
