// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Infrastructure/AppServices.cs
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
using CMPCodeDatabase.Core.Infrastructure;
using CMPCodeDatabase.Core.Services;

namespace CMPCodeDatabase.Core.Infrastructure
{
    /// <summary>
    /// Provides lazy access to modular services without touching UI.
    /// Your existing forms can call: AppServices.Get<IDatabaseService>();
    /// </summary>
    public static class AppServices
    {
        private static readonly Lazy<SimpleContainer> _container = new(() => AppBootstrapper.Build());
        public static SimpleContainer Container => _container.Value;
        public static T Get<T>() where T : class => Container.Resolve<T>();
    }
}
