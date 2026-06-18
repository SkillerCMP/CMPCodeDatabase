// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.SwUserCheats.Export.Resolve.cs
// Purpose: Save Wizard swusercheats.xml export path/game/container selection helpers.
// Notes:
//  • Split from CollectorForm.SwUserCheats.Export.cs during cleanup pass 24.
//  • Behavior intentionally unchanged; this file only groups export setup steps.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Export.SaveWizard;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        private bool TryResolveSwGamelistPath(AppSettings settings, out string gamelistPath)
        {
            gamelistPath = string.Empty;

            // Preferred default is: %EXE%\Files\Tools\gamelist
            // If none is set, we try to discover it from %TEMP%/%TMP% and copy it into Files\Tools\.
            string? resolvedPath = null;
            if (!TryResolveOrAcquireGamelistXml(settings, out resolvedPath, out var acquiredNote))
            {
                // Fallback: let user browse, then copy into Files\Tools\gamelist for future runs
                using var ofd = new OpenFileDialog
                {
                    Title = "Select Save Wizard gamelist (gamelist or gamelist.xml)",
                    Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return false;

                try
                {
                    resolvedPath = CopyGamelistIntoTools(ofd.FileName, settings);
                    settings.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                MessageBox.Show(this, "gamelist not found. Please export or locate it again.", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            gamelistPath = resolvedPath;
            return true;
        }

        private List<string> BuildSwGameIdCandidates(AppSettings settings)
        {
            // Determine Game ID automatically from the currently selected game's ^2 = GameID metadata.
            // Falls back to prompting only if auto-detection fails.
            string? lastGame = settings.SwLastGameId;

            // Candidate IDs: current game's metadata first, then last used, then heuristic guess.
            var candidates = new List<string>();
            candidates.AddRange(TryGetActiveGameIdsFromMainForm());
            if (!string.IsNullOrWhiteSpace(lastGame)) candidates.Add(lastGame!.Trim());
            var heuristic = GuessLikelyGameId(_activeGameKey);
            if (!string.IsNullOrWhiteSpace(heuristic)) candidates.Add(heuristic.Trim());

            // Normalize + de-dupe
            return candidates.Where(s => !string.IsNullOrWhiteSpace(s))
                             .Select(s => s.Trim().ToUpperInvariant())
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .ToList();
        }

        private bool TryResolveSwGameSelection(
            string gamelistPath,
            List<string> candidates,
            out string? gameId,
            out GameListGame? game)
        {
            gameId = null;
            game = null;

            try
            {
                var games = SwGameListParser.Load(gamelistPath);

                // Try candidates first (no prompt if we can resolve automatically)
                foreach (var cand in candidates)
                {
                    var g = SwGameListParser.FindGameByAnyId(games, cand);
                    if (g != null)
                    {
                        gameId = cand;
                        game = g;
                        break;
                    }
                }

                // If still unknown, prompt (default to last/heuristic)
                if (gameId == null)
                {
                    string guess = candidates.Count > 0 ? candidates[0] : string.Empty;
                    if (!PromptText(this, "Save Wizard Export", "Game ID (e.g., CUSA12345):", guess, out var entered))
                        return false;

                    entered = (entered ?? string.Empty).Trim();
                    if (entered.Length == 0)
                        return false;

                    gameId = entered.ToUpperInvariant();
                    game = SwGameListParser.FindGameByAnyId(games, gameId);
                }
                else
                {
                    // game already set in loop
                    if (game == null)
                        game = SwGameListParser.FindGameByAnyId(games, gameId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Read gamelist", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(gameId))
                return false;

            if (game is null)
            {
                MessageBox.Show(this, $"Game not found in gamelist: {gameId}\n\nTip: try a different ID (primary or alias).", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (game.Containers.Count == 0)
            {
                MessageBox.Show(this, "Game found but it has no <container> entries in gamelist.", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool TryResolveSwContainerAndFiles(
            GameListGame game,
            out GameListContainer container,
            out List<string> selectedFiles)
        {
            selectedFiles = new List<string>();

            int containerIndex = 0;
            if (game.Containers.Count > 1)
            {
                if (!PickContainer(this, game, out containerIndex))
                {
                    container = game.Containers[0];
                    return false;
                }
            }

            container = game.Containers[containerIndex];
            if (container.FilePatterns.Count == 0)
            {
                MessageBox.Show(this, "Selected container has no <filename> patterns.", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!PickFiles(this, container.FilePatterns, out selectedFiles))
                return false;

            if (selectedFiles.Count == 0)
            {
                MessageBox.Show(this, "No files selected.", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            return true;
        }

        private bool TryPickSwUserCheatsOutputPath(AppSettings settings, out string outPath)
        {
            outPath = string.Empty;
            string? lastOut = settings.SwLastUserCheatsPath;
            using var sfd = new SaveFileDialog
            {
                Title = "Export Save Wizard swusercheats.xml",
                Filter = "Save Wizard user cheats (swusercheats.xml)|swusercheats.xml|XML files (*.xml)|*.xml|All files (*.*)|*.*",
                FileName = string.IsNullOrWhiteSpace(lastOut) ? "swusercheats.xml" : Path.GetFileName(lastOut)
            };
            if (!string.IsNullOrWhiteSpace(lastOut))
            {
                try
                {
                    var dir = Path.GetDirectoryName(lastOut);
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        sfd.InitialDirectory = dir;
                }
                catch { }
            }

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return false;

            outPath = sfd.FileName;
            return true;
        }
    }
}
