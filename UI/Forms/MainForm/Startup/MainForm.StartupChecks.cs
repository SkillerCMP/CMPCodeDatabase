// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Startup/MainForm.StartupChecks.cs
// Purpose: App startup checks and first-run setup.
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
using CMPCodeDatabase.UI.Dialogs;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void EnsureStartupChecks()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string filesRoot = Path.Combine(baseDir, "Files");
                string dbDir = Path.Combine(filesRoot, "Database");
                string toolsDir = Path.Combine(filesRoot, "Tools");

                bool missingFilesRoot = !Directory.Exists(filesRoot);
                bool missingDatabase = !Directory.Exists(dbDir);
                bool missingTools = !Directory.Exists(toolsDir);

                // Create anything missing
                Directory.CreateDirectory(filesRoot);
                Directory.CreateDirectory(dbDir);
                Directory.CreateDirectory(toolsDir);

                if (missingFilesRoot || missingDatabase || missingTools)
                {
                    using (var dlg = new MissingFilesDialog(missingFilesRoot, missingDatabase, missingTools))
                        dlg.ShowDialog(this);
                }
            }
            catch
            {
                // swallow; avoid blocking app startup on first-run setup
            }
        }
    }
}
