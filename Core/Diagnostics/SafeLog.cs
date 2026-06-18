// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Diagnostics/SafeLog.cs
// Purpose: Best-effort diagnostic logging for non-fatal cleanup/ignored errors.
// Notes:
//  • This helper must never throw back to callers.
//  • Intended for catch blocks that previously swallowed exceptions silently.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;

namespace CMPCodeDatabase.Core.Diagnostics
{
    internal static class SafeLog
    {
        private static readonly object Gate = new();

        public static void Write(string area, Exception? exception = null, string? message = null)
        {
            try
            {
                var logDir = Path.Combine(AppContext.BaseDirectory, "Files", "Logs");
                Directory.CreateDirectory(logDir);

                var file = Path.Combine(logDir, "CMPCodeDatabase_Diagnostics.log");
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {area}";
                if (!string.IsNullOrWhiteSpace(message))
                    line += $" - {message}";

                if (exception != null)
                    line += Environment.NewLine + exception;

                line += Environment.NewLine + "-----------------------------" + Environment.NewLine;

                lock (Gate)
                    File.AppendAllText(file, line);
            }
            catch
            {
                // Diagnostics must never break normal app flow.
            }
        }
    }
}
