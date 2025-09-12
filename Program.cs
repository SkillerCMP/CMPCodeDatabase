// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Program.cs
// Purpose: Project source file.
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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                // Catch UI thread exceptions
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => LogAndShow("UI", e.Exception);

                // Catch non-UI thread exceptions
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                    LogAndShow("AppDomain", e.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception"));

                // Catch unobserved task exceptions
                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    LogAndShow("Task", e.Exception);
                    e.SetObserved();
                };

                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                LogAndShow("Main", ex);
            }
        }

        private static void LogAndShow(string channel, Exception ex)
        {
            try
            {
                var root = AppContext.BaseDirectory;
                var logDir = Path.Combine(root, "Files", "Logs");
                Directory.CreateDirectory(logDir);
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var file = Path.Combine(logDir, $"Crash_{stamp}.log");

                File.AppendAllText(file,
$@"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Channel={channel}
{ex}
-----------------------------
");

                // Show once; avoid recursive crashes on messagebox
                try
                {
                    MessageBox.Show(
                        $"A fatal error occurred (channel: {channel}).\n" +
                        $"A crash log was written to:\n{file}\n\n" +
                        $"{ex.GetType().Name}: {ex.Message}",
                        "CMPCodeDatabase - Unhandled Exception",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { /* ignore UI failures */ }
            }
            catch
            {
                // last resort: swallow
            }
        }
    }
}