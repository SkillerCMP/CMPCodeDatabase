// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Helpers/MainForm.Helpers.Tags.cs
// Purpose: UI composition, menus, and layout for the MainForm.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────


namespace CMPCodeDatabase
{
    public partial class MainForm : Form
    {
// Returns the tag name without label in angle brackets, e.g., "Item<HP>" -> "Item"
private static string TagCore(string tag)
{
    if (string.IsNullOrWhiteSpace(tag)) return string.Empty;
    int lt = tag.IndexOf('<');
    int gt = tag.LastIndexOf('>');
    if (lt >= 0 && gt > lt) return tag.Remove(lt, gt - lt + 1).Trim();
    return tag.Trim();
}

// Back-compat alias for older call sites that used lowercase method name
private static string tagCore(string tag) => TagCore(tag);
private int CountOccurrencesInText(string text, string token)
                        {
                            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return 0;
                            int count = 0, start = 0;
                            while (true)
                            {
                                int pos = text.IndexOf(token, start, StringComparison.Ordinal);
                                if (pos < 0) break;
                                count++;
                                start = pos + token.Length;
                            }
                            return count;
                        }

        private bool IsCaretInsideTag(string text, int caret, string tag)
                        {
                            if (caret < 0 || caret > text.Length) return false;
                            int lsq = text.LastIndexOf('[', Math.Min(caret, Math.Max(0, text.Length - 1)));
                            if (lsq < 0) return false;
                            int rsq = text.IndexOf(']', lsq + 1);
                            if (rsq < 0) return false;
                            string inside = text.Substring(lsq + 1, rsq - lsq - 1).Trim();
                            return string.Equals(inside, tag, StringComparison.OrdinalIgnoreCase) || string.Equals(TagCore(inside), TagCore(tag), StringComparison.OrdinalIgnoreCase);
                        }

        private string ReplaceOneOccurrenceAtIndex(string tpl, int startIndexOfBracket, string newValue)
                        {
                            int lsq = startIndexOfBracket;
                            if (lsq >= 0 && lsq < tpl.Length && tpl[lsq] == '[')
                            {
                                int rsq = tpl.IndexOf(']', lsq + 1);
                                if (rsq > lsq)
                                    return tpl.Substring(0, lsq) + newValue + tpl.Substring(rsq + 1);
                            }
                            return tpl;
                        }

        private System.Collections.Generic.List<(int Start, int End)> FindAllTagOccurrences(string tpl, string tag)
                        {
                            var list = new System.Collections.Generic.List<(int, int)>();
                            if (string.IsNullOrEmpty(tpl)) return list;
                            int i = 0;
                            while (i < tpl.Length)
                            {
                                int lsq = tpl.IndexOf('[', i);
                                if (lsq < 0) break;
                                int rsq = tpl.IndexOf(']', lsq + 1);
                                if (rsq < 0) break;
                                string inside = tpl.Substring(lsq + 1, rsq - lsq - 1).Trim();
                                if (string.Equals(inside, tag, StringComparison.OrdinalIgnoreCase) || string.Equals(TagCore(inside), TagCore(tag), StringComparison.OrdinalIgnoreCase))
                                    list.Add((lsq, rsq));
                                i = rsq + 1;
                            }
                            return list;
                        }

        private int ComputeCaretIndex(string tpl, string tag, int caret)
                        {
                            var occ = FindAllTagOccurrences(tpl, tag);
                            for (int i = 0; i < occ.Count; i++)
                            {
                                var o = occ[i];
                                if (caret >= o.Start && caret <= o.End) return i + 1;
                            }
                            return 1;
                        }

        private static bool TryParseSpecialAmountTag(string tag, out string title, out string defHex, out string type, out string endian)
                        {
                            title = "Amount"; defHex = ""; type = "HEX"; endian = "BIG";
                            if (string.IsNullOrWhiteSpace(tag)) return FalseAlias();
                            // Expected: Amount:VAL:TYPE:ENDIAN
                            if (!tag.StartsWith("Amount:", StringComparison.OrdinalIgnoreCase)) return FalseAlias();
                            var parts = tag.Split(':');
                            if (parts.Length < 4) return FalseAlias();
                            title = parts[0];
                            defHex = parts[1];
                            type = parts[2];
                            endian = parts[3];
                            return true;

                            static bool FalseAlias() => false;
                        }
    }
}
