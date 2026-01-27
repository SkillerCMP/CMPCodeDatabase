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
using System.Drawing;
using System.Collections.Generic;

namespace CMPCodeDatabase
{
    internal static class Program
    {
        private static Icon _appIcon = SystemIcons.Application;
        private static readonly HashSet<IntPtr> _iconApplied = new();
        private static readonly HashSet<IntPtr> _shownHooked = new();
        private static readonly HashSet<IntPtr> _clamped = new();

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

                // Best behavior for high DPI + mixed monitors (incl. 200% scaling / touch devices)
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                ApplicationConfiguration.Initialize();

                // Grab the icon embedded in the EXE (from <ApplicationIcon> / VS setting)
                try
                {
                    _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
                }
                catch { _appIcon = SystemIcons.Application; }

                // Apply icon + clamp bounds for all open forms (after layout/shown)
                Application.Idle += (s, e) => ApplyUiFixesToOpenForms();

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                LogAndShow("Main", ex);
            }
        }

        private static void ApplyUiFixesToOpenForms()
        {
            foreach (Form f in Application.OpenForms)
            {
                try
                {
                    if (f.IsDisposed) continue;
                    if (!f.IsHandleCreated) continue;

                    var h = f.Handle;

                    // 1) Icon (once)
                    if (!_iconApplied.Contains(h))
                    {
                        f.Icon = _appIcon;
                        f.ShowIcon = true;
                        _iconApplied.Add(h);
                    }

                    // 2) Clamp to working area (once, but after layout/shown)
                    if (!_shownHooked.Contains(h))
                    {
                        _shownHooked.Add(h);
                        f.Shown += (_, __) =>
                        {
                            try
                            {
                                if (f.IsDisposed) return;
                                EnsureFormFullyVisible(f);
                                if (f.IsHandleCreated) _clamped.Add(f.Handle);
                            }
                            catch { }
                        };
                    }

                    // If already visible (dialogs can be visible immediately), clamp now once
                    if (f.Visible && !_clamped.Contains(h))
                    {
                        EnsureFormFullyVisible(f);
                        _clamped.Add(h);
                    }
                }
                catch { /* ignore */ }
            }
        }

        private static void EnsureFormFullyVisible(Form f)
        {
            // Don't fight the OS when maximized/minimized
            if (f.WindowState != FormWindowState.Normal) return;

            var wa = Screen.FromControl(f).WorkingArea;
            var b = f.Bounds;

            int w = Math.Min(b.Width, wa.Width);
            int h = Math.Min(b.Height, wa.Height);

            int x = b.Left;
            int y = b.Top;

            if (x < wa.Left) x = wa.Left;
            if (y < wa.Top) y = wa.Top;
            if (x + w > wa.Right) x = wa.Right - w;
            if (y + h > wa.Bottom) y = wa.Bottom - h;

            var nb = new Rectangle(x, y, w, h);
            if (nb != b) f.Bounds = nb;
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
