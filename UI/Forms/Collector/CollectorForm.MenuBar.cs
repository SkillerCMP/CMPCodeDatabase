using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class CollectorForm : Form
    {
        private bool _opsMenuReady_MENU;
        private MenuStrip _msOps;

        /// <summary>
        /// Build a top menu for Collector operations and hide old buttons (Select/Copy/Clear).
        /// Call once (e.g., in OnShown): try { EnsureOpsMenu_MENU(); } catch {}
        /// </summary>
        private void EnsureOpsMenu_MENU()
        {
            if (_opsMenuReady_MENU) return;
            _opsMenuReady_MENU = true;

            try
            {
                // Hide the old buttons the user asked to remove
                if (btnSelectAll != null)   btnSelectAll.Visible   = false;
                if (btnSelectNone != null)  btnSelectNone.Visible  = false;
                if (btnInvert != null)      btnInvert.Visible      = false;
                if (btnCopyAll != null)     btnCopyAll.Visible     = false;
                if (btnCopyChecked != null) btnCopyChecked.Visible = false;
                if (btnClear != null)       btnClear.Visible       = false;
            }
            catch { /* missing fields are OK */ }

            // Create menu
            _msOps = new MenuStrip { Dock = DockStyle.Top };
            var miClear   = new ToolStripMenuItem("Clear Codelist");
            var miOptions = new ToolStripMenuItem("Options");

            // Options → Preview
            var miPrev      = new ToolStripMenuItem("Preview");
            var miPrevSav   = new ToolStripMenuItem(".savepatch", null, (s, e) => PreviewPatch(onlyChecked: true));
            var miPrevPnach = new ToolStripMenuItem(".pnach",     null, (s, e) => PreviewPnach(onlyChecked: true));
            miPrev.DropDownItems.Add(miPrevSav);
            miPrev.DropDownItems.Add(miPrevPnach);

            // Options → Export To
            var miExport      = new ToolStripMenuItem("Export To");
            var miPCSX2       = new ToolStripMenuItem("PCSX2");
            var miPCSX2Chk    = new ToolStripMenuItem(".pnach (Checked)", null, (s, e) => ExportPnach(onlyChecked: true));
            var miPCSX2All    = new ToolStripMenuItem(".pnach (All)",     null, (s, e) => ExportPnach(onlyChecked: false));
            miPCSX2.DropDownItems.Add(miPCSX2Chk);
            miPCSX2.DropDownItems.Add(miPCSX2All);

            var miApollo      = new ToolStripMenuItem("Apollo");
            var miSavChk      = new ToolStripMenuItem(".savepatch (Checked)", null, (s, e) => ExportSavepatch(onlyChecked: true));
            var miSavAll      = new ToolStripMenuItem(".savepatch (All)",     null, (s, e) => ExportSavepatch(onlyChecked: false));
            miApollo.DropDownItems.Add(miSavChk);
            miApollo.DropDownItems.Add(miSavAll);

            miExport.DropDownItems.Add(miPCSX2);
            miExport.DropDownItems.Add(miApollo);

            // Options → Select
            var miSelect  = new ToolStripMenuItem("Select");
            var miSelAll  = new ToolStripMenuItem("All",   null, (s, e) => SetAllChecked(true));
            var miSelNone = new ToolStripMenuItem("None",  null, (s, e) => SetAllChecked(false));
            miSelect.DropDownItems.Add(miSelAll);
            miSelect.DropDownItems.Add(miSelNone);

            // Options → Copy
            var miCopy    = new ToolStripMenuItem("Copy");
            var miCopyAll = new ToolStripMenuItem("All",     null, (s, e) => CopyAll());
            var miCopyChk = new ToolStripMenuItem("Checked", null, (s, e) => CopyChecked());
            miCopy.DropDownItems.Add(miCopyAll);
            miCopy.DropDownItems.Add(miCopyChk);

            // Wire Clear
            miClear.Click += (s, e) =>
            {
                try
                {
                    collectorCodeMap.Clear();
                    clbCollector.Items.Clear();
                }
                catch { }
            };

            // Compose top-level
            _msOps.Items.Add(miClear);
            _msOps.Items.Add(miOptions);

            // Compose Options subtree
            miOptions.DropDownItems.Add(miPrev);
            miOptions.DropDownItems.Add(miExport);
            miOptions.DropDownItems.Add(miSelect);
            miOptions.DropDownItems.Add(miCopy);

            // Insert the menu just above the "Target file" (dataBar) row
            try
            {
                Control dataBarCtl = this.Controls.Cast<Control>().FirstOrDefault(c => c.Name == "dataBar" || (c is FlowLayoutPanel && c.Controls.OfType<Button>().Any(b => b.Text.Contains("Browse"))));
                int insertIndex = 0;
                if (dataBarCtl != null)
                {
                    insertIndex = this.Controls.GetChildIndex(dataBarCtl);
                }
                this.Controls.Add(_msOps);
                if (dataBarCtl != null)
                    this.Controls.SetChildIndex(_msOps, insertIndex);
                else
                    this.Controls.SetChildIndex(_msOps, 0);
            }
            catch
            {
                // Fallback: add normally; Dock=Top will still place it near the top.
                this.Controls.Add(_msOps);
            }
        }
    }
}
