// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.cs
// Purpose: Database discovery, selector, and tree building.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────


using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void ParseCodeFilesInFolder(string[] txtFiles, TreeNode parentNode)
        {
            foreach (var file in txtFiles)
            {
                bool inModsSection = false;

                TreeNode? currentFileGroup = null;
                TreeNode? currentGroup = null;
                TreeNode? currentCodeNode = null;
                string? currentModTag = null;
                List<(string Value, string Name)>? currentModList = null;
                bool headerCollected = false;
                var fileCodeNodes = new List<TreeNode>();
                var groupHeaderLines = new Dictionary<TreeNode, List<string>>();
                var groupsWithParsedCodes = new HashSet<TreeNode>();
                var platformHeaders = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);
                string? currentPlatformHeaderTag = null;
                List<string>? currentPlatformHeaderLines = null;

                foreach (var raw in File.ReadLines(file))
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    if (raw.StartsWith("^1") || raw.StartsWith("^2")) continue;

                    inModsSection = UpdateModsSectionState(raw, inModsSection);

                    string line = (raw ?? string.Empty).TrimEnd();

                    if (TryHandleGameNoteLine(line, inModsSection, currentFileGroup, parentNode))
                        continue;

                    if (TryHandleCodeFileHeaderLine(line, parentNode, ref currentFileGroup, ref currentGroup))
                    {
                        currentCodeNode = null;
                        continue;
                    }

                    if (line.StartsWith("+"))
                    {
                        var inheritedGroupHeaderBlock = BuildInheritedGroupHeaderBlock(currentGroup, groupHeaderLines);
                        currentCodeNode = CreateCodeNodeFromPlusLine(line, currentGroup, currentFileGroup, parentNode, inheritedGroupHeaderBlock);
                        MarkGroupScopesWithParsedCode(currentGroup, currentFileGroup, parentNode, groupsWithParsedCodes);
                        fileCodeNodes.Add(currentCodeNode);
                    }
                    else if (!inModsSection && line.StartsWith("!"))
                    {
                        currentCodeNode = null;
                        HandleGroupLine(line, currentFileGroup, parentNode, ref currentGroup);
                    }
                    else if (inModsSection && TryHandlePlatformHeaderModsLine(line, platformHeaders, ref currentPlatformHeaderTag, ref currentPlatformHeaderLines))
                    {
                        continue;
                    }
                    else if (line.StartsWith("["))
                    {
                        HandleModBlockBoundaryLine(line, ref currentModTag, ref currentModList, ref headerCollected);
                    }
                    else if (currentCodeNode != null && TryParseCodeCreditsLine(line, out var creditsLine))
                    {
                        // Code-level credits only (ignore any global credits outside a code block)
                        AddCreditsForNode(currentCodeNode, creditsLine);
                    }
                    else if (line.StartsWith("$"))
                    {
                        if (!inModsSection && TryAddGroupHeaderLine(line, currentCodeNode, currentGroup, currentFileGroup, parentNode, groupHeaderLines, groupsWithParsedCodes))
                            continue;

                        AddCodeLineToNode(line, currentCodeNode);
                    }
                    else if (currentModTag != null)
                    {
                        HandleCurrentModLine(line, currentModTag, currentModList, ref headerCollected);
                    }
                }

                ApplyPlatformHeaderToCodeNodes(file, platformHeaders, fileCodeNodes);
                RefreshModBadgesForTree(parentNode);
            }
        }
    }
}
