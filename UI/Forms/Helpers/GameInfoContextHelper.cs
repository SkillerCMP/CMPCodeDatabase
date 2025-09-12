// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Helpers/GameInfoContextHelper.cs
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
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Adds a right-click context menu to the Games TreeView:
    ///  - See info     -> opens SeeInfoForm for either a folder or a single .txt file
    ///  - Open file... -> opens the single .txt if available (or the folder if multiple)
    /// </summary>
    public static class GameInfoContextHelper
    {
        /// <param name="gamesTree">The Games TreeView control.</param>
        /// <param name="resolvePathForNode">
        /// Return an absolute folder path OR a single .txt file path for a clicked node,
        /// or null if that node shouldn't show the menu.
        /// </param>
        public static void Attach(TreeView gamesTree, Func<TreeNode, string?> resolvePathForNode)
        {
            if (gamesTree == null) throw new ArgumentNullException(nameof(gamesTree));
            if (resolvePathForNode == null) throw new ArgumentNullException(nameof(resolvePathForNode));

            var cm = new ContextMenuStrip();
            var seeInfo = new ToolStripMenuItem("See info");
            var openFile = new ToolStripMenuItem("Open file...");
            cm.Items.AddRange(new ToolStripItem[] { seeInfo, openFile });

            gamesTree.NodeMouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                gamesTree.SelectedNode = e.Node;

                var path = resolvePathForNode(e.Node);
                if (string.IsNullOrWhiteSpace(path))
                    return;

                bool canShow = false;

                if (File.Exists(path) && string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    cm.Tag = path;
                    canShow = true;
                }
                else if (Directory.Exists(path))
                {
                    bool hasTxt = Directory.EnumerateFiles(path, "*.txt", SearchOption.TopDirectoryOnly).Any();
                    if (hasTxt)
                    {
                        cm.Tag = path;
                        canShow = true;
                    }
                }

                if (canShow)
                    cm.Show(gamesTree, e.Location);
            };

            seeInfo.Click += (s, e) =>
            {
                var path = cm.Tag as string;
                if (string.IsNullOrWhiteSpace(path)) return;

                using (var f = new SeeInfoForm(path))
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(gamesTree.FindForm());
                }
            };

            openFile.Click += (s, e) =>
            {
                var path = cm.Tag as string;
                if (string.IsNullOrWhiteSpace(path)) return;

                try
                {
                    if (File.Exists(path) && string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        });
                        return;
                    }

                    if (Directory.Exists(path))
                    {
                        var txts = Directory.EnumerateFiles(path, "*.txt", SearchOption.TopDirectoryOnly).ToList();
                        if (txts.Count == 1)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = txts[0],
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = path,
                                UseShellExecute = true
                            });
                        }
                    }
                }
                catch
                {
                    // swallow – nothing critical if shell open fails
                }
            };
        }
    }
}
