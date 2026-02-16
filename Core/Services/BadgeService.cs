// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Services/BadgeService.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System.Text.RegularExpressions;
using CMPCodeDatabase.Core.Models;

namespace CMPCodeDatabase.Core.Services
{
    public sealed partial class BadgeService : IBadgeService
    {
        [GeneratedRegex(@"\{MOD\}|\[Amount:", RegexOptions.IgnoreCase)]
        private static partial Regex HasModBlockRx();

        [GeneratedRegex(@"\{[^}]+\}", RegexOptions.Singleline)]
        private static partial Regex HasNoteRx();

        public Badges GetBadgesFor(CodeEntry code)
        {
            var flags = Badges.None;
            if (HasModBlockRx().IsMatch(code.Raw)) flags |= Badges.HasMods;
            if (!string.IsNullOrEmpty(code.NoteHtml) || HasNoteRx().IsMatch(code.Raw)) flags |= Badges.HasNote;
            return flags;
        }
    }
}
