// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Models/AmountToken.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

namespace CMPCodeDatabase.Core.Models
{
    public enum AmountEndian { LE, BE }
    public sealed class AmountToken
    {
        public string Value { get; }
        public string Type { get; }
        public AmountEndian Endian { get; }
        public AmountToken(string value, string type, AmountEndian endian)
        {
            Value = value; Type = type; Endian = endian;
        }
    }
}
