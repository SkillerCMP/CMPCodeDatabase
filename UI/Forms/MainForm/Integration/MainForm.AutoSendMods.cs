// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Integration/MainForm.AutoSendMods.cs
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
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Auto-sends a code to the Collector when all MOD placeholders are filled.
    /// This version does NOT require AppSettings to declare AutoSendModsToCollector at compile time.
    /// It reads the flag via reflection if present, otherwise falls back to settings.json.
    /// </summary>
    public partial class MainForm : Form
    {
        private bool _autoSendWired;
        private bool _autoSendGuard;
        private string? _lastAutoSentSignature;

        internal void TryWireAutoSend()
        {
            if (_autoSendWired) return;
            if (txtCodePreview == null || treeCodes == null) return;

            txtCodePreview.TextChanged += TxtCodePreview_TextChanged_AutoSend;
            _autoSendWired = true;
        }

        private void TxtCodePreview_TextChanged_AutoSend(object? sender, EventArgs e)
        {
            MaybeAutoSendSelectedNode();
        }

        private void MaybeAutoSendSelectedNode()
        {
            if (_autoSendGuard) return;
            if (!IsAutoSendEnabled()) return;

            var node = treeCodes?.SelectedNode;
            if (node == null) return;

            string raw = node.Tag?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return;
            if (HasUnresolvedPlaceholders(raw)) return;

            string name = GetCopyName(node);
            string code = Apply64BitHexBlocking(raw);

            string sig = name + "\n" + code;
            if (_lastAutoSentSignature == sig) return;

            _autoSendGuard = true;
            try
            {
                var s = AppSettings.Instance;

                if (collectorWindow != null && !collectorWindow.IsDisposed)
                {
                    collectorWindow.AddItem(name, code);
                }
                else
                {
                    collectorFallback[name] = code;
                    if (IsOpenCollectorOnAddEnabled(s)) EnsureCollectorVisible();
                }

                ResetCode(node);
                _lastAutoSentSignature = sig;
            }
            finally
            {
                _autoSendGuard = false;
            }
        }

        // ---------- Settings helpers (reflection + JSON fallback) ----------

        private static bool IsAutoSendEnabled()
        {
            try
            {
                var s = AppSettings.Instance;
                // Try reflection for property "AutoSendModsToCollector"
                var prop = s.GetType().GetProperty("AutoSendModsToCollector");
                if (prop != null && prop.PropertyType == typeof(bool))
                {
                    var val = prop.GetValue(s);
                    if (val is bool b) return b;
                }

                // Fallback: read %APPDATA%\CMPCodeDatabase\settings.json
                if (TryReadAutoSendFromFile(out var enabled)) return enabled;
            }
            catch { /* ignore */ }
            return false; // default off if not found
        }

        private static bool IsOpenCollectorOnAddEnabled(AppSettings s)
        {
            try
            {
                // Prefer declared property (baseline has it)
                var prop = s.GetType().GetProperty("OpenCollectorOnAdd");
                if (prop != null && prop.PropertyType == typeof(bool))
                {
                    var val = prop.GetValue(s);
                    if (val is bool b) return b;
                }

                // Fallback to file
                var path = GetSettingsPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    if (doc.RootElement.TryGetProperty("OpenCollectorOnAdd", out var el) &&
                        (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False))
                        return el.GetBoolean();
                }
            }
            catch { /* ignore */ }
            return true; // sensible default
        }

        private static bool TryReadAutoSendFromFile(out bool enabled)
        {
            enabled = false;
            try
            {
                var path = GetSettingsPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    if (doc.RootElement.TryGetProperty("AutoSendModsToCollector", out var el) &&
                        (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False))
                    {
                        enabled = el.GetBoolean();
                        return true;
                    }
                }
            }
            catch { /* ignore */ }
            return false;
        }

        private static string? GetSettingsPath()
        {
            try
            {
                // Try to resolve via AppSettings if available
                var type = typeof(AppSettings);
                var pathProp = type.GetProperty("SettingsPath");
                if (pathProp != null && pathProp.GetMethod != null && pathProp.GetMethod.IsStatic)
                {
                    var val = pathProp.GetValue(null) as string;
                    if (!string.IsNullOrWhiteSpace(val)) return val;
                }
            }
            catch { /* ignore */ }

            try
            {
                var company = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CMPCodeDatabase");
                return Path.Combine(company, "settings.json");
            }
            catch { /* ignore */ }

            return null;
        }

        private void EnsureCollectorVisible()
        {
            if (collectorWindow == null || collectorWindow.IsDisposed)
            {
                collectorWindow = new CollectorForm();
                foreach (var kv in collectorFallback) collectorWindow.AddItem(kv.Key, kv.Value);
            }
            if (!collectorWindow.Visible) collectorWindow.Show(this); else collectorWindow.BringToFront();
        }
    }
}