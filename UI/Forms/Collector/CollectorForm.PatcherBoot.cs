// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.PatcherBoot.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Ensures CollectorForm has a valid PatchProgramExePath as soon as the form is created,
    /// using AppSettings.PatchToolPath or the default %ROOT%\Files\Tools\patcher.exe.
    /// If a valid path is discovered and AppSettings is empty, it is persisted.
    /// </summary>
    public partial class CollectorForm : Form
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                // If already set (by caller or previous run), don't override
                if (!string.IsNullOrWhiteSpace(PatchProgramExePath) && File.Exists(PatchProgramExePath))
                    return;

                var s = AppSettings.Instance;
                var resolved = PatcherPathResolver.Resolve(s);

                if (string.IsNullOrWhiteSpace(resolved))
                {
                    // If settings are empty and default exists, use default and persist
                    var def = PatcherPathResolver.GetDefaultPath();
                    if (File.Exists(def))
                    {
                        PatchProgramExePath = def;
                        if (string.IsNullOrWhiteSpace(s.PatchToolPath))
                        {
                            s.PatchToolPath = def;
                            s.Save();
                        }
                    }
                    return;
                }

                // Use resolved path and persist if settings were empty or different
                PatchProgramExePath = resolved;
                if (!string.Equals(s.PatchToolPath, resolved, StringComparison.OrdinalIgnoreCase))
                {
                    s.PatchToolPath = resolved;
                    s.Save();
                }
            }
            catch
            {
                // Non-fatal: let the existing patch-run logic display its usual prompt if needed
            }
        }
    }
}