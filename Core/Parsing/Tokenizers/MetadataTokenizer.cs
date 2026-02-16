// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Parsing/Tokenizers/MetadataTokenizer.cs
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

namespace CMPCodeDatabase.Core.Parsing.Tokenizers
{
    public static partial class MetadataTokenizer
    {
        [GeneratedRegex(@"^\s*\^1\s*(?:=\s*Hash:)?\s*(.+)$", RegexOptions.IgnoreCase)]
        private static partial Regex HashLineRx();

        [GeneratedRegex(@"^\s*\^2\s*(?:=\s*GameID:)?\s*(.+)$", RegexOptions.IgnoreCase)]
        private static partial Regex GameIdLineRx();

        [GeneratedRegex(@"^\s*Hash\s*:\s*(.+)$", RegexOptions.IgnoreCase)]
        private static partial Regex BareHashRx();

        [GeneratedRegex(@"^\s*GameID\s*:\s*(.+)$", RegexOptions.IgnoreCase)]
        private static partial Regex BareGameIdRx();

        public static bool TryParseMetadata(string line, out Metadata meta)
        {
            meta = null!;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var m = HashLineRx().Match(line);
            if (m.Success) { meta = new Metadata("Hash", m.Groups[1].Value.Trim()); return true; }

            m = GameIdLineRx().Match(line);
            if (m.Success) { meta = new Metadata("GameID", m.Groups[1].Value.Trim()); return true; }

            m = BareHashRx().Match(line);
            if (m.Success) { meta = new Metadata("Hash", m.Groups[1].Value.Trim()); return true; }

            m = BareGameIdRx().Match(line);
            if (m.Success) { meta = new Metadata("GameID", m.Groups[1].Value.Trim()); return true; }

            return false;
        }
    }
}
