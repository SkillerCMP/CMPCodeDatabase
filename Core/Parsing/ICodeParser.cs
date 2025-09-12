// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Parsing/ICodeParser.cs
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
using CMPCodeDatabase.Core.Models;

namespace CMPCodeDatabase.Core.Parsing
{
    public interface ICodeParser
    {
        Game ParseGame(string gameId, string gameName, string folderPath, IEnumerable<(string fileName, string text)> files);
    }
}
