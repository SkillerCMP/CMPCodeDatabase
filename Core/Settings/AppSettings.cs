// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Settings/AppSettings.cs
// Purpose: Settings model and UI for persisted configuration.
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
using System.Text.Json;

namespace CMPCodeDatabase.Core.Settings
{
    public sealed class AppSettings
    {
        // Existing
        public string? PatchToolPath { get; set; }
        public bool ShowPatchLogByDefault { get; set; }
        public bool OpenCollectorOnAdd { get; set; }
        public bool UseTabbedPreviewCollector { get; set; } = false; // tab "Code Preview" + "Collector" in MainForm

        // UX: double-click MOD codes -> prompt for mods then auto-add to Collector + reset
        public bool DoubleClickResolveModsThenAddToCollector { get; set; } = false;

        // Save Wizard integration (Collector export)
        public string? SwGameListPath { get; set; }            // path to Save Wizard gamelist.xml
        public string? SwLastGameId { get; set; }              // last exported title id (CUSA/NPUB/etc)
        public string? SwLastUserCheatsPath { get; set; }      // last swusercheats.xml output path

        // Default-baked links (user can override in Settings)
        public string DatabaseDownloadUrl { get; set; } =
            "https://drive.google.com/drive/folders/1MoOYhItCwsTypEkn8a98TY3O32t8WnIe";
        public string ToolsDownloadUrl { get; set; } =
            "https://github.com/bucanero/apollo-lib/releases";

        public static string CompanyFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CMPCodeDatabase");

        public static string SettingsPath => Path.Combine(CompanyFolder, "settings.json");

        private static AppSettings? _instance;
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    try
                    {
                        Directory.CreateDirectory(CompanyFolder);
                        if (File.Exists(SettingsPath))
                        {
                            var json = File.ReadAllText(SettingsPath);
                            _instance = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                        }
                    }
                    catch { /* ignore and fallback */ }
                    _instance ??= new AppSettings();
                }
                return _instance;
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(CompanyFolder);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* ignore */ }
        }
    }
}