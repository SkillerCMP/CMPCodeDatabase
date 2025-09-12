// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Export/SavepatchExportService.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System.IO;
using System.Threading.Tasks;

namespace CMPCodeDatabase.Core.Export
{
    public sealed class SavepatchExportService : IExportService
    {
        public Task<string> ExportCollectorAsync(string destinationFile)
        {
            File.WriteAllText(destinationFile, "// export placeholder");
            return Task.FromResult(destinationFile);
        }
    }
}
