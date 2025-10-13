using System;
using System.IO;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        // Remember the last target file we saw (normalized)
        private string? _activeTargetPathNorm;
        private DateTime? _activeTargetLastWriteUtc; // (optional) if you want to also react to content changes

        // Normalize paths consistently
        private static string NormalizePath(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return string.Empty;
            try { return Path.GetFullPath(p).Trim().ToLowerInvariant(); }
            catch { return p.Trim().ToLowerInvariant(); }
        }

        /// <summary>
        /// Call this ANYTIME the target file is switched by the user.
        /// If the path changed, we clear error/warn markers so codes can be re-tested cleanly.
        /// </summary>
        public void OnTargetFileChanged(string? newPath)
        {
            var norm = NormalizePath(newPath);
            if (string.Equals(_activeTargetPathNorm, norm, StringComparison.Ordinal))
                return; // nothing changed, keep markers

            _activeTargetPathNorm = norm;

            // (optional) capture file timestamp if it exists
            try
            {
                _activeTargetLastWriteUtc = (!string.IsNullOrEmpty(newPath) && File.Exists(newPath))
                    ? File.GetLastWriteTimeUtc(newPath)
                    : (DateTime?)null;
            }
            catch { _activeTargetLastWriteUtc = null; }

            // --- Reset run-specific state ---
            _currentApplyingName = null;   // from LogParsing partial
            ClearStatuses();               // from Status partial (removes ✖/⚠ and clears dictionary)

            // If you also want to clear the log, uncomment:
            // ClearLog();
        }

        /// <summary>
        /// (Optional) If you reuse the *same* path but the file content on disk changes,
        /// you can call this to auto-clear markers. For example from a FileSystemWatcher or a "Reload" action.
        /// </summary>
        public void OnTargetFileMaybeUpdatedOnDisk()
        {
            if (string.IsNullOrEmpty(_activeTargetPathNorm)) return;

            try
            {
                var currentUtc = File.GetLastWriteTimeUtc(_activeTargetPathNorm);
                if (_activeTargetLastWriteUtc == null || currentUtc != _activeTargetLastWriteUtc.Value)
                {
                    _activeTargetLastWriteUtc = currentUtc;
                    _currentApplyingName = null;
                    ClearStatuses();
                    // ClearLog();
                }
            }
            catch
            {
                // If the file went missing, treat it as a change
                _activeTargetLastWriteUtc = null;
                _currentApplyingName = null;
                ClearStatuses();
            }
        }
    }
}
