// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: GlobalUsings.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

// Applies to the whole project
global using System.Text.RegularExpressions; // Regex
global using System.Numerics;               // BigInteger
global using System.Globalization;          // CultureInfo, NumberStyles
