// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Infrastructure/AppBootstrapper.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using CMPCodeDatabase.Core.Export;
using CMPCodeDatabase.Core.Infrastructure;
using CMPCodeDatabase.Core.Parsing;
using CMPCodeDatabase.Core.Services;

namespace CMPCodeDatabase.Core.Infrastructure
{
    public static class AppBootstrapper
    {
        public static SimpleContainer Build()
        {
            var c = new SimpleContainer();
            c.RegisterSingleton(() => new Parsing.CodeTextParser());
            c.RegisterSingleton<IDatabaseService>(() => new FileDatabaseService(c.Resolve<Parsing.CodeTextParser>()));
            c.RegisterSingleton<IBadgeService>(() => new BadgeService());
            c.RegisterSingleton<IHelpContentService>(() => new HelpContentService());
            c.RegisterSingleton<IExportService>(() => new SavepatchExportService());
            return c;
        }
    }
}
