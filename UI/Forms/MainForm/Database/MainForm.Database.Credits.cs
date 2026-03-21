using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        private static readonly Regex RxCodeCredits =
            new Regex(@"^\s*(?:%|#)\s*Credits\s*:\s*(.+?)\s*$",
                      RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Try parse a code-level credits line (e.g. "%Credits: Someone").
        /// Only intended to be used while a code node is active.
        /// </summary>
        private static bool TryParseCodeCreditsLine(string line, out string credits)
        {
            credits = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;
            var m = RxCodeCredits.Match(line);
            if (!m.Success) return false;
            credits = (m.Groups[1].Value ?? string.Empty).Trim();
            return credits.Length > 0;
        }

        /// <summary>
        /// Merge credits for a specific code node. Splits on ',' and ';' and de-dupes.
        /// </summary>
        private void AddCreditsForNode(TreeNode codeNode, string creditsLine)
        {
            if (codeNode == null) return;
            if (string.IsNullOrWhiteSpace(creditsLine)) return;

            var parts = creditsLine
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            if (parts.Count == 0) return;

            // Merge with existing (case-insensitive)
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (nodeCredits.TryGetValue(codeNode, out var existing) && !string.IsNullOrWhiteSpace(existing))
            {
                foreach (var p in existing.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var s = p.Trim();
                    if (s.Length > 0) set.Add(s);
                }
            }

            foreach (var p in parts) set.Add(p);

            nodeCredits[codeNode] = string.Join(", ", set);
        }
    }
}
