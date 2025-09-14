// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — Helpers: tag label parsing (<…>)
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
        /// <summary>Return tag core without any trailing &lt;Label&gt;, e.g. "Item&lt;L&gt;" → "Item".</summary>
        private static string StripTagLabel(string tagOrCore)
        {
            if (string.IsNullOrEmpty(tagOrCore)) return tagOrCore ?? string.Empty;
            int i = tagOrCore.IndexOf('<');
            return (i >= 0) ? tagOrCore.Substring(0, i) : tagOrCore;
        }

        /// <summary>Return the optional &lt;Label&gt; part, or null if absent.</summary>
        private static string ExtractTagLabel(string tagOrCore)
        {
            if (string.IsNullOrEmpty(tagOrCore)) return null;
            int i = tagOrCore.IndexOf('<');
            if (i < 0) return null;
            int j = tagOrCore.LastIndexOf('>');
            if (j > i) return tagOrCore.Substring(i + 1, j - i - 1).Trim();
            return null;
        }
    }
}
