// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Patching/IPatchRunner.cs
// Purpose: Patching and process/runner logic.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System.Threading; using System.Threading.Tasks; using System.Windows.Forms; namespace CMPCodeDatabase.Patching { public interface IPatchRunner { Task RunAsync(string p,int c,IWin32Window o); Task RunAsync(string p,int c,IWin32Window o, IPatchLogSink l, CancellationToken ct = default); } }