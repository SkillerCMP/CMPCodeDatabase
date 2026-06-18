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
using CMPCodeDatabase.Core.Diagnostics;

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

        public bool PnachExportNotesAsDescription { get; set; } = false;


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
                    catch (Exception ex)
                    {
                        SafeLog.Write("AppSettings.Load", ex);
                    }
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
                var tempPath = SettingsPath + ".tmp";

                File.WriteAllText(tempPath, json);

                try
                {
                    if (File.Exists(SettingsPath))
                        File.Replace(tempPath, SettingsPath, null, ignoreMetadataErrors: true);
                    else
                        File.Move(tempPath, SettingsPath);
                }
                catch (Exception ex)
                {
                    // Fallback for file systems that do not support File.Replace.
                    SafeLog.Write("AppSettings.Save.ReplaceFallback", ex);
                    File.Copy(tempPath, SettingsPath, overwrite: true);
                    try { File.Delete(tempPath); } catch { /* best effort cleanup */ }
                }
            }
            catch (Exception ex)
            {
                SafeLog.Write("AppSettings.Save", ex);
            }
        }
    }
}