// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.cs
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
using System.Windows.Forms;


namespace CMPCodeDatabase
{
    public partial class HelpForm : Form
    {
        private readonly TabControl tabs = new TabControl();


                // ---------------- Shared HTML wrapper & styles ----------------


                // ---------------- Code Browser (restored guidance) ----------------


                // ---------------- Notes tab ----------------


                // ---------------- Collector tab ----------------


                // ---------------- Shortcuts tab ----------------

                // ---------------- New: Code Text tab (value-first spec) ----------------

                // ---------------- About tab ----------------

        public HelpForm()
                {
                    Text = "Help";
                    Width = 950;
                    Height = 760;
                    StartPosition = FormStartPosition.CenterParent;

                    tabs.Dock = DockStyle.Fill;
                    Controls.Add(tabs);

                    // ---- Existing/Restored help tabs (you can reorder as you like) ----
                    AddTab("Code Browser", BuildCodeBrowserHtml());
                    AddTab("Notes", BuildNotesHtml());
                    AddTab("Collector", BuildCollectorHtml());
                    AddTab("Shortcuts", BuildShortcutsHtml());
                    // ---- New help tab: Code Text grammar/spec ----
                    AddTab("Code Text", BuildCodeTextLegendHtml());
                    AddTab("About", BuildAboutHtml());



                    // Select the first tab by default
                    if (tabs.TabPages.Count > 0) tabs.SelectedIndex = 0;
                }
    }
}
