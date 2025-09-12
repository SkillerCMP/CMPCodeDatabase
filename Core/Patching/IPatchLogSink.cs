// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Patching/IPatchLogSink.cs
// Purpose: Patching and process/runner logic.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

namespace CMPCodeDatabase.Patching { public interface IPatchLogSink { void Write(string t); void WriteLine(string t); } }