using System;
using System.Text.RegularExpressions;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        private static string ToSingleLineBasic(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            // Collapse whitespace/newlines to a single space
            return Regex.Replace(s.Trim(), @"\s+", " ");
        }

        private CMPCodeDatabase.Core.Models.CollectorItemMeta GetCollectorMetaForNode(System.Windows.Forms.TreeNode node)
        {
            string? author = null;
            string? desc = null;

            if (node != null && nodeCredits.TryGetValue(node, out var credits) && !string.IsNullOrWhiteSpace(credits))
                author = ToSingleLineBasic(credits);

            if (node != null && nodeNotes.TryGetValue(node, out var note) && !string.IsNullOrWhiteSpace(note))
                desc = ToSingleLineBasic(note);

            return new CMPCodeDatabase.Core.Models.CollectorItemMeta(author, desc);
        }
    }
}
