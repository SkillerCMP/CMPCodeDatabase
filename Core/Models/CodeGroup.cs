// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Models/CodeGroup.cs
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

namespace CMPCodeDatabase.Core.Models
{
    public sealed class CodeGroup
    {
        public string Name { get; }
        public bool IsSubGroup { get; }
        public List<CodeEntry> Codes { get; } = new();

        public CodeGroup(string name, bool isSubGroup = false)
        {
            Name = name;
            IsSubGroup = isSubGroup;
        }
    }
}
