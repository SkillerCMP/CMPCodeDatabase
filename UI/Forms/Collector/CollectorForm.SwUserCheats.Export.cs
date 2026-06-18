// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.SwUserCheats.Export.cs
// Purpose: Collector export to Save Wizard swusercheats.xml (Quick Mode user cheats).
// Notes:
//  • Uses Save Wizard gamelist (gamelist or gamelist.xml) to resolve containerKey + filename patterns.
//  • Creates skeleton <game id="{id}{containerKey}"> nodes for primary + aliases.
//  • Writes collected entries as <cheat desc="..." comment="..."> with normalized ADDR VALUE pairs.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Export.SaveWizard;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        private static readonly Regex SwGameIdRegex = new(@"\b[A-Za-z]{4}\d{5}\b", RegexOptions.Compiled);
        private static readonly Regex SwLikelyGameIdRegex = new(@"\b(CUSA\d{5}|CUS[AP]\d{5}|NPUB\d{5}|NPEB\d{5}|NPHB\d{5}|BLUS\d{5}|BLES\d{5}|BLJM\d{5}|ULUS\d{5}|ULES\d{5}|ULJM\d{5})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SwPairLineRegex = new(@"^\$?\s*([0-9A-F]{8})\s+([0-9A-F]{8})\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private void ExportSwUserCheatsXml(bool onlyChecked)
        {
            var entries = CollectEntries(onlyChecked);
            if (entries.Count == 0)
            {
                MessageBox.Show(this, "No entries to export.", "Save Wizard swusercheats.xml", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var settings = AppSettings.Instance;

            if (!TryResolveSwGamelistPath(settings, out var gamelistPath))
                return;

            var candidates = BuildSwGameIdCandidates(settings);

            if (!TryResolveSwGameSelection(gamelistPath, candidates, out var gameId, out var game))
                return;

            if (string.IsNullOrWhiteSpace(gameId) || game is null)
                return;

            if (!TryResolveSwContainerAndFiles(game, out var container, out var selectedFiles))
                return;

            if (!TryPickSwUserCheatsOutputPath(settings, out var outPath))
                return;

            // Build / update swusercheats.xml
            int exported = 0;
            int skipped = 0;

            try
            {
                var sw = SwUserCheats.LoadOrCreate(outPath);

                // Best practice: create nodes for primary + aliases
                var idsToCreate = new List<string> { game.PrimaryId };
                idsToCreate.AddRange(game.AliasIds);

                foreach (var id in idsToCreate.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var swGameId = id + container.Key;
                    sw.EnsureGameAndFiles(swGameId, selectedFiles);

                    foreach (var fp in selectedFiles)
                    {
                        foreach (var kv in entries)
                        {
                            var desc = kv.Key ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(desc)) { skipped++; continue; }

                            if (!TryExtractAddrValuePairs(kv.Value ?? string.Empty, out var pairs))
                            {
                                skipped++;
                                continue;
                            }

                            // Save Wizard typically ignores group nesting, but it *does* display backslashes nicely in many builds.
                            var comment = string.Empty;
                            sw.AddOrReplaceCheat(swGameId, fp, desc, comment, pairs);
                            exported++;
                        }
                    }
                }

                sw.Save(outPath);

                // Persist last paths (without breaking older settings.json)
                settings.SwGameListPath = gamelistPath;
                settings.SwLastGameId = gameId;
                settings.SwLastUserCheatsPath = outPath;
                settings.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export swusercheats.xml", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this,
                $"Export complete.\n\n" +
                $"Game: {game.PrimaryId}  (aliases: {game.AliasIds.Count})\n" +
                $"Container key: {container.Key}\n" +
                $"Files: {selectedFiles.Count}\n" +
                $"Cheats written: {exported}\n" +
                (skipped > 0 ? $"Skipped (not pure SW code): {skipped}\n" : ""),
                "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
