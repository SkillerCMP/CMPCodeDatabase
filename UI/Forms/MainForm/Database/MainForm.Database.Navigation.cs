// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Database/MainForm.Database.Navigation.cs
// Purpose: Database selector, game tree loading, and code tree navigation helpers.
// Notes:
//  • Split from MainForm.Database.cs during cleanup pass 10.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Diagnostics;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        private void LoadDatabaseSelector()
                {
                    string databasesRoot = GetDatabasesRootPath();
                    dbSelector.Items.Clear();
                    if (!Directory.Exists(databasesRoot)) return;

                    foreach (var folder in Directory.GetDirectories(databasesRoot))
                        dbSelector.Items.Add(Path.GetFileName(folder));

                    if (dbSelector.Items.Count > 0)
                        dbSelector.SelectedIndex = 0;
                }

        private void DbSelector_SelectedIndexChanged(object? sender, EventArgs e)
                {
                    if (dbSelector.SelectedItem == null) return;
                    var root = GetDatabasesRootPath();
                    codeDirectory = Path.Combine(root, dbSelector.SelectedItem?.ToString() ?? string.Empty);
                    LoadGames();
                }

        private void LoadGames()
                {
                    treeGames.BeginUpdate();
                    try
                    {
                        treeGames.Nodes.Clear();
                        if (string.IsNullOrEmpty(codeDirectory) || !Directory.Exists(codeDirectory)) return;

                        foreach (var folder in Directory.GetDirectories(codeDirectory))
                            treeGames.Nodes.Add(new TreeNode(Path.GetFileName(folder)) { Tag = folder });

                        foreach (TreeNode n in treeGames.Nodes) n.Collapse();
                    }
                    finally
                    {
                        treeGames.EndUpdate();
                    }

                    treeCodes.BeginUpdate();
            treeCodes.Nodes.Clear();
                    txtCodePreview.Clear();
					treeCodes.EndUpdate();
TreeViewExtent.UpdateHorizontalExtent(treeCodes);
try { NotifyCodesTreeRebuilt_REFRESH(); } catch (Exception ex) { SafeLog.Write("MainForm.NotifyCodesTreeRebuilt", ex); }
                }

        private void TreeGames_AfterSelect(object? sender, TreeViewEventArgs e)
                {
                    if (e.Node?.Tag == null) return;
                    LoadCodes(e.Node.Tag.ToString()!);
                }

        private void LoadCodes(string folder)
                {
                    treeCodes.BeginUpdate();
					treeCodes.Nodes.Clear();
                    CurrentGameIdsCsv = null;

                    originalCodeTemplates.Clear();
                    originalNodeNames.Clear();
                    nodeNotes.Clear();
                    nodeHasMod.Clear();
                    appliedModNames.Clear();
                    modDefinitions.Clear();
                    modHeaders.Clear();
                    modRows.Clear();

                    BuildTreeFromFolder(folder, null);

            // Compose right root caption from ^3 and ^2 of a representative file (if any)
            try
            {
                string folderName = Path.GetFileName(folder);
                string? firstTxt = Directory.EnumerateFiles(folder, "*.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (treeCodes.Nodes.Count > 0)
                {
                    string rootCaptionForFolder = folderName;
                    if (firstTxt != null)
                    {
                        var header = TryReadHeader(firstTxt);
                        var _name = header.Name ?? folderName;
                        rootCaptionForFolder = !string.IsNullOrEmpty(header.GameId) ? $"{_name} - {header.GameId}" : _name;
                        CurrentGameIdsCsv = string.IsNullOrWhiteSpace(header.GameId) ? null : header.GameId.Trim();
                    }
                    treeCodes.Nodes[0].Text = rootCaptionForFolder;
                    originalNodeNames[treeCodes.Nodes[0]] = rootCaptionForFolder;
                }
            }
            catch (Exception ex)
            {
                SafeLog.Write("MainForm.LoadCodes.HeaderCaption", ex);
            }

                    ApplyBoldStyling(treeCodes.Nodes);

                    foreach (TreeNode n in treeCodes.Nodes) n.Collapse();
                    txtCodePreview.Clear();
					treeCodes.EndUpdate();
    TreeViewExtent.UpdateHorizontalExtent(treeCodes);
	try { NotifyCodesTreeRebuilt_REFRESH(); } catch (Exception ex) { SafeLog.Write("MainForm.NotifyCodesTreeRebuilt", ex); }
                }

        private void BuildTreeFromFolder(string currentFolder, TreeNode? parentNode)
                {
                    var __folderName = Path.GetFileName(currentFolder);
                    var folderNode = new TreeNode(__folderName);
                    originalNodeNames[folderNode] = __folderName;

                    foreach (var sub in Directory.EnumerateDirectories(currentFolder))
                        BuildTreeFromFolder(sub, folderNode);

                    var txtFiles = Directory.EnumerateFiles(currentFolder, "*.txt").ToArray();
                    if (txtFiles.Length > 0)
                        ParseCodeFilesInFolder(txtFiles, folderNode);

                    if (folderNode.Nodes.Count > 0 || txtFiles.Length > 0)
                    {
                        if (parentNode == null) treeCodes.Nodes.Add(folderNode);
                        else parentNode.Nodes.Add(folderNode);
                    }
                }

        private string GetDatabasesRootPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                System.IO.Path.Combine(baseDir, "Database"),
                System.IO.Path.Combine(baseDir, "File", "Database"),
                System.IO.Path.Combine(baseDir, "Files", "Database")
            };
            foreach (var p in candidates)
                if (System.IO.Directory.Exists(p)) return p;
            return candidates[0];
        }

    }
}
