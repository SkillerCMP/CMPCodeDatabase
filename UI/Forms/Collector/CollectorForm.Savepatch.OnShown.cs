// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorControl.Host.cs
// Purpose: Host integration for CollectorControl.
// Notes:
//  • CollectorControl can be hosted either in a dedicated CollectorForm window,
//    or embedded in MainForm (tabbed layout).
//  • Anything that is Form-specific (KeyPreview, window sizing, activation) must
//    be done via the host Form.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        public enum CollectorHostMode
        {
            Windowed,
            Tabbed
        }

        private Form? _hostForm;
        private CollectorHostMode _hostMode = CollectorHostMode.Windowed;

        /// <summary>
        /// Attach CollectorControl to its host Form (CollectorForm window or MainForm tab host).
        /// Call this once after the host is created (e.g., in Form.Shown or after adding to tabs).
        /// </summary>
        public void AttachHost(Form host, CollectorHostMode mode)
        {
            _hostForm = host;
            _hostMode = mode;

            // Form-specific wiring that CollectorControl needs.
            try { EnsureShortcuts_SHORT(host); } catch { }
            try { EnsureOpsMenu_MENU(); } catch { }
            try { EnsureCollectorTools_ELFCRC(); } catch { }

            // Only enforce fixed sizing when hosted in a dedicated window.
            if (mode == CollectorHostMode.Windowed)
            {
                try { ApplyFixedCollectorSizing(host); } catch { }
            }
        }

        internal Form? TryGetHostForm()
        {
            return _hostForm ?? this.FindForm();
        }

        internal CollectorHostMode GetHostMode() => _hostMode;
    }
}
