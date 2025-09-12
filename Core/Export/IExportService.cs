// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Export/IExportService.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System.Threading.Tasks;

namespace CMPCodeDatabase.Core.Export
{
    public interface IExportService
    {
        Task<string> ExportCollectorAsync(string destinationFile);
    }
}
