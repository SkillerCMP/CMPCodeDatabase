using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CMPCodeDatabase.Tools; // requires Pcsx2ElfCrc.cs in project

namespace CMPCodeDatabase
{
    /// <summary>
    /// Adds "Options > Tools > Get ELF CRC..." to the Collector window + stores a preferred default .pnach name.
    /// Example stored flag: "SLES-52641_A1FD63D6" → used as default for .pnach export if present.
    /// Wire once (e.g., CollectorForm.OnShown): try { EnsureCollectorTools_ELFCRC(); } catch {}
    /// Access preferred name in export code via GetPreferredPnachDefaultFileName_META().
    /// </summary>
    public partial class CollectorForm : Form
    {
        // Stored "name flag" (e.g., SLES-52641_A1FD63D6). Null when unset.
        private string _pnachNameHint_META;

        /// <summary>Returns "SLES-52641_A1FD63D6.pnach" if a hint is present, else null.</summary>
        public string GetPreferredPnachDefaultFileName_META()
            => string.IsNullOrWhiteSpace(_pnachNameHint_META) ? null : _pnachNameHint_META + ".pnach";

        /// <summary>Manually accept a precomputed preferred name flag (sans .pnach).</summary>
        public void AcceptPreferredPnachBaseName_META(string baseNameWithoutExt)
        {
            _pnachNameHint_META = string.IsNullOrWhiteSpace(baseNameWithoutExt) ? null : baseNameWithoutExt.Trim();
        }

        /// <summary>
        /// Find (or create) Options > Tools and add "Get ELF CRC..." item.
        /// </summary>
        public void EnsureCollectorTools_ELFCRC()
        {
            // Find a MenuStrip on this form
            var menu = FindDeepMenuStrip(this);
            if (menu == null) return;

            // Find "Options" top-level
            var mOptions = FindMenuItemByText(menu, "options");
            if (mOptions == null) return;

            // Find or create "Tools" under Options
            var mTools = FindInDropDownByText(mOptions, "tools");
            if (mTools == null)
            {
                mTools = new ToolStripMenuItem("Tools") { Name = "mnuOptionsTools" };
                mOptions.DropDownItems.Add(mTools);
            }

            // If already present, don't double-add
            if (FindInDropDownByText(mTools, "get elf crc") != null) return;

            var mGetElfCrc = new ToolStripMenuItem("Get ELF CRC...") { Name = "mnuGetElfCrc" };
            mGetElfCrc.Click += GetElfCrc_Click;
            mTools.DropDownItems.Add(mGetElfCrc);
        }

        private void GetElfCrc_Click(object sender, EventArgs e)
        {
            using (var dlg = new ElfCrcPickerForm(this))
            {
                dlg.ShowDialog(this);
            }
        }

        // -------- menu helpers (fixed overloads) --------

        private static MenuStrip FindDeepMenuStrip(Control root)
        {
            if (root == null) return null;
            foreach (Control c in root.Controls)
            {
                if (c is MenuStrip ms) return ms;
                var deep = FindDeepMenuStrip(c);
                if (deep != null) return deep;
            }
            return null;
        }

        private static ToolStripMenuItem FindMenuItemByText(MenuStrip menu, string containsLower)
        {
            if (menu == null) return null;
            containsLower = (containsLower ?? "").ToLowerInvariant();
            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem mi)
                {
                    var txt = (mi.Text ?? "").ToLowerInvariant();
                    if (txt.Contains(containsLower)) return mi;
                    var deep = FindInDropDownByText(mi, containsLower);
                    if (deep != null) return deep;
                }
            }
            return null;
        }

        private static ToolStripMenuItem FindInDropDownByText(ToolStripMenuItem owner, string containsLower)
        {
            if (owner == null) return null;
            containsLower = (containsLower ?? "").ToLowerInvariant();
            if (owner.DropDownItems == null) return null;
            foreach (ToolStripItem item in owner.DropDownItems)
            {
                if (item is ToolStripMenuItem mi)
                {
                    var txt = (mi.Text ?? "").ToLowerInvariant();
                    if (txt.Contains(containsLower)) return mi;
                    var deep = FindInDropDownByText(mi, containsLower);
                    if (deep != null) return deep;
                }
            }
            return null;
        }
    }
}
