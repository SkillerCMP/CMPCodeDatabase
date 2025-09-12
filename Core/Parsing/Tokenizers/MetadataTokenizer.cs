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
    public static class MetadataTokenizer
    {
        static readonly Regex HashLine   = new(@"^\s*\^1\s*(?:=\s*Hash:)?\s*(.+)$", RegexOptions.IgnoreCase);
        static readonly Regex GameIdLine = new(@"^\s*\^2\s*(?:=\s*GameID:)?\s*(.+)$", RegexOptions.IgnoreCase);
        static readonly Regex BareHash   = new(@"^\s*Hash\s*:\s*(.+)$", RegexOptions.IgnoreCase);
        static readonly Regex BareGameId = new(@"^\s*GameID\s*:\s*(.+)$", RegexOptions.IgnoreCase);

        public static bool TryParseMetadata(string line, out Metadata meta)
        {
            meta = null!;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var m = HashLine.Match(line);
            if (m.Success) { meta = new Metadata("Hash", m.Groups[1].Value.Trim()); return true; }

            m = GameIdLine.Match(line);
            if (m.Success) { meta = new Metadata("GameID", m.Groups[1].Value.Trim()); return true; }

            m = BareHash.Match(line);
            if (m.Success) { meta = new Metadata("Hash", m.Groups[1].Value.Trim()); return true; }

            m = BareGameId.Match(line);
            if (m.Success) { meta = new Metadata("GameID", m.Groups[1].Value.Trim()); return true; }

            return false;
        }
    }
}
