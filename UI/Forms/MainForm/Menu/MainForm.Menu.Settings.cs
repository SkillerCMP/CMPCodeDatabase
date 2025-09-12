// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Menu/MainForm.Menu.Settings.cs
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
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void EnsureSettingsMenu()
        {
            var menu = this.Controls.OfType<MenuStrip>().FirstOrDefault()
                   ?? this.Controls.Find("menuStrip1", true).OfType<MenuStrip>().FirstOrDefault();

            if (menu == null)
            {
                menu = new MenuStrip { Name = "menuStrip1", Dock = DockStyle.Top };
                this.MainMenuStrip = menu;
                this.Controls.Add(menu);
                menu.BringToFront();
            }

            var settingsTop = menu.Items.OfType<ToolStripMenuItem>().FirstOrDefault(i => string.Equals(i.Text, "Settings", StringComparison.OrdinalIgnoreCase));
            if (settingsTop == null)
            {
                settingsTop = new ToolStripMenuItem("Settings") { Name = "menuSettings" };
                var items = menu.Items.OfType<ToolStripMenuItem>().ToList();
                int helpIdx = items.FindIndex(i => string.Equals(i.Text, "Help", StringComparison.OrdinalIgnoreCase));
                int viewIdx = items.FindIndex(i => string.Equals(i.Text, "View", StringComparison.OrdinalIgnoreCase));
                int insertIdx = Math.Max(helpIdx, viewIdx);
                if (insertIdx >= 0) menu.Items.Insert(insertIdx, settingsTop);
                else menu.Items.Add(settingsTop);
            }

            if (!settingsTop.DropDownItems.OfType<ToolStripMenuItem>().Any(i => i.Name == "menuSettingsOpen"))
            {
                var open = new ToolStripMenuItem("Open...") { Name = "menuSettingsOpen" };
                open.Click += (s, e) => { using var dlg = new SettingsForm(); dlg.ShowDialog(this); };
                settingsTop.DropDownItems.Add(open);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            EnsureSettingsMenu();
            EnsureStartupChecks();
        
            try { TryInitGameSearchUI(); } catch { }
        }
    }
}
