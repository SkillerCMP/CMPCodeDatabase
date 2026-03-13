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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Export.SaveWizard;
using CMPCodeDatabase.Core.Settings;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        private void ExportSwUserCheatsXml(bool onlyChecked)
        {
            var entries = CollectEntries(onlyChecked);
            if (entries.Count == 0)
            {
                MessageBox.Show(this, "No entries to export.", "Save Wizard swusercheats.xml", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 1) Pick / resolve gamelist (gamelist or gamelist.xml)
            var settings = AppSettings.Instance;

            // Preferred default is: %EXE%\Files\Tools\gamelist
            // If none is set, we try to discover it from %TEMP%/%TMP% and copy it into Files\Tools\.
            string? gamelistPath = null;
            if (!TryResolveOrAcquireGamelistXml(settings, out gamelistPath, out var acquiredNote))
            {
                // Fallback: let user browse, then copy into Files\Tools\gamelist for future runs
                using var ofd = new OpenFileDialog
                {
                    Title = "Select Save Wizard gamelist (gamelist or gamelist.xml)",
                    Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                    CheckFileExists = true
                };
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    gamelistPath = CopyGamelistIntoTools(ofd.FileName, settings);
                    settings.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(gamelistPath) || !File.Exists(gamelistPath))
            {
                MessageBox.Show(this, "gamelist not found. Please export or locate it again.", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2) Determine Game ID automatically from the currently selected game's ^2 = GameID metadata.
//    Falls back to prompting only if auto-detection fails.
            string? lastGame = settings.SwLastGameId;

            // Candidate IDs: current game's metadata first, then last used, then heuristic guess.
            var candidates = new List<string>();
            candidates.AddRange(TryGetActiveGameIdsFromMainForm());
            if (!string.IsNullOrWhiteSpace(lastGame)) candidates.Add(lastGame!.Trim());
            var heuristic = GuessLikelyGameId(_activeGameKey);
            if (!string.IsNullOrWhiteSpace(heuristic)) candidates.Add(heuristic.Trim());

            // Normalize + de-dupe
            candidates = candidates.Where(s => !string.IsNullOrWhiteSpace(s))
                                   .Select(s => s.Trim().ToUpperInvariant())
                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                   .ToList();

            string? gameId = null;
// 3) Load gamelist + resolve game/containers
            List<GameListGame> games;
            GameListGame? game = null;

            try
            {
                games = SwGameListParser.Load(gamelistPath);

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
                        return;

                    entered = (entered ?? string.Empty).Trim();
                    if (entered.Length == 0)
                        return;

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
                return;
            }

            if (string.IsNullOrWhiteSpace(gameId))
                return;
if (game is null)
            {
                MessageBox.Show(this, $"Game not found in gamelist: {gameId}\n\nTip: try a different ID (primary or alias).", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (game.Containers.Count == 0)
            {
                MessageBox.Show(this, "Game found but it has no <container> entries in gamelist.", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int containerIndex = 0;
            if (game.Containers.Count > 1)
            {
                if (!PickContainer(this, game, out containerIndex))
                    return;
            }

            var container = game.Containers[containerIndex];
            if (container.FilePatterns.Count == 0)
            {
                MessageBox.Show(this, "Selected container has no <filename> patterns.", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!PickFiles(this, container.FilePatterns, out var selectedFiles))
                return;
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show(this, "No files selected.", "Save Wizard Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 4) Choose output swusercheats.xml
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
                return;

            var outPath = sfd.FileName;

            // 5) Build / update swusercheats.xml
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

        
        private static List<string> TryGetActiveGameIdsFromMainForm()
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                    if (f is MainForm mf)
                        return SplitGameIds(mf.CurrentGameIdsCsv);
            }
            catch { /* ignore */ }
            return new List<string>();
        }

        private static List<string> SplitGameIds(string? csvOrText)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(csvOrText)) return list;

            // Accept "CUSA12345,CUSA23456" or whitespace separated; also tolerate quotes.
            foreach (Match m in Regex.Matches(csvOrText, @"\b[A-Za-z]{4}\d{5}\b"))
            {
                var id = m.Value.Trim().ToUpperInvariant();
                if (!list.Contains(id))
                    list.Add(id);
            }
            return list;
        }

private static string GuessLikelyGameId(string? activeKey)
        {
            if (string.IsNullOrWhiteSpace(activeKey))
                return string.Empty;

            // Try to find common title-id patterns.
            var m = Regex.Match(activeKey,
                @"\b(CUSA\d{5}|CUS[AP]\d{5}|NPUB\d{5}|NPEB\d{5}|NPHB\d{5}|BLUS\d{5}|BLES\d{5}|BLJM\d{5}|ULUS\d{5}|ULES\d{5}|ULJM\d{5})\b",
                RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.ToUpperInvariant() : string.Empty;
        }

        private static bool TryExtractAddrValuePairs(string input, out string pairs)
        {
            // STRICT Save Wizard format:
            //   Every non-empty line must be exactly "8HEX 8HEX" (optionally prefixed with '$' for CMP layouts).
            //   If *any* other directive/text line exists (e.g., Apollo "delete next/insert next"), we reject the cheat.
            //
            // This prevents exporting mixed SW+Apollo blocks into swusercheats.xml.
            pairs = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var rxLine = new Regex(@"^\$?\s*([0-9A-F]{8})\s+([0-9A-F]{8})\s*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var tokens = new List<string>();

            // Normalize line endings and validate line-by-line.
            var lines = input.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            foreach (var raw in lines)
            {
                var line = (raw ?? string.Empty).Trim();
                if (line.Length == 0)
                    continue;

                var mm = rxLine.Match(line);
                if (!mm.Success)
                {
                    // Any non-matching line means this is not pure SW code.
                    pairs = string.Empty;
                    return false;
                }

                tokens.Add(mm.Groups[1].Value.ToUpperInvariant());
                tokens.Add(mm.Groups[2].Value.ToUpperInvariant());
            }

            if (tokens.Count == 0)
                return false;

            var joined = string.Join(' ', tokens);
            joined = SwCodeNormalize.NormalizePairs(joined);

            if (!SwCodeNormalize.HasEvenTokenCount(joined))
                return false;

            pairs = joined;
            return true;
        }



        private static bool PromptText(IWin32Window owner, string title, string label, string initial, out string value)
        {
            value = initial ?? string.Empty;

            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Width = 520,
                Height = 160
            };

            var lbl = new Label { Left = 12, Top = 12, Width = 480, Height = 18, Text = label };
            var tb = new TextBox { Left = 12, Top = 34, Width = 480, Text = value };

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 332, Width = 76, Top = 70 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 416, Width = 76, Top = 70 };

            f.Controls.Add(lbl);
            f.Controls.Add(tb);
            f.Controls.Add(ok);
            f.Controls.Add(cancel);
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            var res = f.ShowDialog(owner);
            if (res != DialogResult.OK)
                return false;

            value = tb.Text.Trim();
            return true;
        }

        private static bool PickContainer(IWin32Window owner, GameListGame game, out int containerIndex)
        {
            containerIndex = 0;

            using var f = new Form
            {
                Text = "Select Save Wizard Container",
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Width = 720,
                Height = 360
            };

            var list = new ListBox { Left = 12, Top = 12, Width = 680, Height = 260 };
            for (int i = 0; i < game.Containers.Count; i++)
            {
                var c = game.Containers[i];
                var pfs = string.IsNullOrWhiteSpace(c.Pfs) ? "" : $"  pfs: {c.Pfs}";
                list.Items.Add($"[{i}] key: {c.Key}  files: {c.FilePatterns.Count}{pfs}");
            }
            list.SelectedIndex = 0;

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 532, Width = 76, Top = 282 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 616, Width = 76, Top = 282 };

            f.Controls.Add(list);
            f.Controls.Add(ok);
            f.Controls.Add(cancel);
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            var res = f.ShowDialog(owner);
            if (res != DialogResult.OK)
                return false;

            containerIndex = Math.Max(0, list.SelectedIndex);
            return true;
        }

        private static bool PickFiles(IWin32Window owner, IReadOnlyList<string> filePatterns, out List<string> selected)
        {
            selected = new List<string>();

            using var f = new Form
            {
                Text = "Select Save Files (filename patterns)",
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Width = 720,
                Height = 420
            };

            var clb = new CheckedListBox { Left = 12, Top = 12, Width = 680, Height = 300, CheckOnClick = true };
            foreach (var p in filePatterns)
                clb.Items.Add(p, true); // default: all checked

            var btnAll = new Button { Text = "All", Left = 12, Top = 320, Width = 70 };
            var btnNone = new Button { Text = "None", Left = 88, Top = 320, Width = 70 };
            btnAll.Click += (_, __) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, true); };
            btnNone.Click += (_, __) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, false); };

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 532, Width = 76, Top = 320 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 616, Width = 76, Top = 320 };

            f.Controls.Add(clb);
            f.Controls.Add(btnAll);
            f.Controls.Add(btnNone);
            f.Controls.Add(ok);
            f.Controls.Add(cancel);
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            var res = f.ShowDialog(owner);
            if (res != DialogResult.OK)
                return false;

            for (int i = 0; i < clb.Items.Count; i++)
                if (clb.GetItemChecked(i))
                    selected.Add(clb.Items[i]?.ToString() ?? string.Empty);

            selected = selected.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.Ordinal).ToList();
            return true;
        }

        // ────────────────────────────────────────────────────────────────────
        // Save Wizard gamelist auto-discovery
        // ────────────────────────────────────────────────────────────────────

        private static string GetToolsDir()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "Tools");
        }

        private static string GetDefaultGamelistPath()
        {
            // Default location (no extension) because some SW dumps name it just "gamelist".
            return Path.Combine(GetToolsDir(), "gamelist");
        }

        private static string GetDefaultGamelistXmlPath()
        {
            // Back-compat / alternate naming.
            return Path.Combine(GetToolsDir(), "gamelist.xml");
        }

        /// <summary>
        /// Attempts to resolve a usable gamelist path.
        /// If none is configured, scans %TEMP%/%TMP% for "gamelist" or gamelist*.xml, copies it to Files\Tools\gamelist,
        /// and sets settings.SwGameListPath to that default.
        /// If already configured to Files\Tools\gamelist (or gamelist.xml), does a quick scan of %TEMP% for a newer one and updates the copy.
        /// </summary>
        private static bool TryResolveOrAcquireGamelistXml(AppSettings settings, out string? gamelistPath, out string? note)
        {
            gamelistPath = null;
            note = null;

            Directory.CreateDirectory(GetToolsDir());
            var toolsGamelist = GetDefaultGamelistPath();
            var toolsGamelistXml = GetDefaultGamelistXmlPath();

            // If settings points somewhere else but exists, copy it into Files\Tools and switch to default.
            var configured = settings.SwGameListPath;
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            {
                try
                {
                    gamelistPath = CopyGamelistIntoTools(configured!, settings);
                    note = "Copied configured gamelist into Files\\Tools\\.";
                    return true;
                }
                catch { /* fall through */ }
            }

            // If Files\Tools\gamelist (or gamelist.xml) already exists, use it.
            if (File.Exists(toolsGamelist) || File.Exists(toolsGamelistXml))
            {
                gamelistPath = File.Exists(toolsGamelist) ? toolsGamelist : toolsGamelistXml;

                // Quick check in %TEMP%: if a newer gamelist exists, refresh our copy.
                var tmpCandidate = FindBestGamelistInTemp(gamelistPath);
                if (tmpCandidate != null)
                {
                    try
                    {
                        gamelistPath = CopyGamelistIntoTools(tmpCandidate!, settings);
                        note = "Updated Files\\Tools\\gamelist from a newer TEMP copy.";
                    }
                    catch { }
                }
                return true;
            }

            // Otherwise: scan TEMP/TMP and copy best candidate into Files\Tools
            var candidate = FindBestGamelistInTemp(existingToolsPath: null);
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                try
                {
                    gamelistPath = CopyGamelistIntoTools(candidate!, settings);
                    note = "Acquired gamelist from TEMP and stored it in Files\\Tools\\.";
                    return true;
                }
                catch { /* ignore */ }
            }

            return false;
        }

        /// <summary>Copies src gamelist (or gamelist.xml) into Files\Tools\gamelist, updates settings path, and returns the destination.</summary>
        private static string CopyGamelistIntoTools(string srcPath, AppSettings settings)
        {
            Directory.CreateDirectory(GetToolsDir());
            var dest = GetDefaultGamelistPath();

            File.Copy(srcPath, dest, overwrite: true);

            // Preserve timestamp so "newer" checks work sensibly
            try { File.SetLastWriteTimeUtc(dest, File.GetLastWriteTimeUtc(srcPath)); } catch { }

            settings.SwGameListPath = dest;
            return dest;
        }

        /// <summary>
        /// Finds the best candidate gamelist*.xml under %TEMP%/%TMP%.
        /// If existingToolsPath is provided, only returns a candidate that appears newer than that file.
        /// Search is intentionally capped to stay "quick".
        /// </summary>
        private static string? FindBestGamelistInTemp(string? existingToolsPath)
        {
            var tempRoot = Path.GetTempPath();
            DateTime existingUtc = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(existingToolsPath) && File.Exists(existingToolsPath))
            {
                try { existingUtc = File.GetLastWriteTimeUtc(existingToolsPath); } catch { }
            }

            string? best = null;
            DateTime bestUtc = DateTime.MinValue;
            long bestLen = -1;

            // Some SW dumps name it "gamelist" (no extension); others use "gamelist.xml".
            foreach (var pattern in new[] { "gamelist", "gamelist*.xml" })
            {
                foreach (var file in EnumerateFilesCapped(tempRoot, pattern, maxDepth: 6, maxDirs: 2000, maxFiles: pattern.EndsWith(".xml") ? 4000 : 2000))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        if (!fi.Exists) continue;

                        // Filter out tiny junk copies; real gamelist is typically large.
                        if (fi.Length < 64 * 1024) continue;

                        var utc = fi.LastWriteTimeUtc;
                        if (utc < existingUtc) continue;

                        // Prefer newest; tie-break on size.
                        if (best == null || utc > bestUtc || (utc == bestUtc && fi.Length > bestLen))
                        {
                            best = fi.FullName;
                            bestUtc = utc;
                            bestLen = fi.Length;
                        }
                    }
                    catch { }
                }
            }

            return best;
        }

        private static IEnumerable<string> EnumerateFilesCapped(string root, string pattern, int maxDepth, int maxDirs, int maxFiles)
        {
            var stack = new Stack<(string dir, int depth)>();
            stack.Push((root, 0));

            int dirCount = 0;
            int fileCount = 0;

            while (stack.Count > 0 && dirCount < maxDirs && fileCount < maxFiles)
            {
                var (dir, depth) = stack.Pop();
                dirCount++;

                IEnumerable<string>? files = null;
                try { files = Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly); } catch { }

                if (files != null)
                {
                    foreach (var f in files)
                    {
                        yield return f;
                        fileCount++;
                        if (fileCount >= maxFiles) yield break;
                    }
                }

                if (depth >= maxDepth) continue;

                IEnumerable<string>? dirs = null;
                try { dirs = Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly); } catch { }
                if (dirs != null)
                {
                    foreach (var d in dirs)
                    {
                        stack.Push((d, depth + 1));
                        if (stack.Count > maxDirs * 2) break; // soft cap to avoid runaway
                    }
                }
            }
        }

    }
}
