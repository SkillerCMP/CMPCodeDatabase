using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    // Parse log lines; detect errors/warnings and mark the corresponding code in the list.
    // NOTE: No "OK" (success) handling—per request.
    public partial class CollectorForm : Form
    {
        private string? _currentApplyingName;

        private static readonly Regex rxApplying =
            new Regex(@"Applying\s*\[(.*?)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex rxError =
            new Regex(@"(ERROR!|^[-\s]*ERROR\b|Error:|Can't\s+load|SEARCH\s+PATTERN\s+NOT\s+FOUND)",
                      RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex rxWarn =
            new Regex(@"\b(WARN|WARNING)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private Color ClassifyLogAndUpdateStatus(string text)
        {
            var m = rxApplying.Match(text);
            if (m.Success)
            {
                _currentApplyingName = m.Groups[1].Value.Trim();
                return SystemColors.WindowText;
            }

            if (rxError.IsMatch(text))
            {
                if (!string.IsNullOrEmpty(_currentApplyingName))
                    MarkCodeStatusByName(_currentApplyingName!, "error");
                return Color.Red;
            }

            if (rxWarn.IsMatch(text))
            {
                if (!string.IsNullOrEmpty(_currentApplyingName))
                    MarkCodeStatusByName(_currentApplyingName!, "warn");
                return Color.DarkOrange;
            }

            return SystemColors.WindowText;
        }
    }
}