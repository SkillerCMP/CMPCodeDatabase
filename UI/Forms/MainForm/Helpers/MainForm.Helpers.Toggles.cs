// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Toggles.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────


namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void ToggleCollectorWindow()
                        {
                            if (collectorWindow == null || collectorWindow.IsDisposed)
                            {
                                collectorWindow = new CollectorForm();
                                foreach (var kv in collectorFallback) collectorWindow.AddItem(kv.Key, kv.Value);
                            }
                            if (collectorWindow.Visible) collectorWindow.Hide(); else collectorWindow.Show(this);
                        }

        private void ToggleCalculatorWindow()
        {
            if (calculatorWindow == null || calculatorWindow.IsDisposed)
            {
                calculatorWindow = new EnhancedCalculatorForm();
            }
            if (calculatorWindow.Visible) calculatorWindow.Hide(); else calculatorWindow.Show(this);
        }

        private void ShowDatabaseStatsWindow()
        {
            if (databaseStatsWindow == null || databaseStatsWindow.IsDisposed)
{
    var rootPath = _dbSelectedPath ?? string.Empty;   // from FilesRoot partial
    databaseStatsWindow = new DatabaseStatsForm(rootPath);
}

            if (!databaseStatsWindow.Visible) databaseStatsWindow.Show(this);
            if (databaseStatsWindow.WindowState == FormWindowState.Minimized)
                databaseStatsWindow.WindowState = FormWindowState.Normal;
            databaseStatsWindow.BringToFront();
        }
    }   // ← closes class MainForm
}       // ← closes namespace