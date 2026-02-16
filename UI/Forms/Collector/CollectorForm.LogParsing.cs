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

        [GeneratedRegex(@"Applying\s*\[(.*?)\]", RegexOptions.IgnoreCase)]
        private static partial Regex Rx_rxApplying_Generated();
        private static readonly Regex rxApplying =
            Rx_rxApplying_Generated();

        [GeneratedRegex(@"(ERROR!|^[-\s]*ERROR\b|Error:|Can't\s+load|SEARCH\s+PATTERN\s+NOT\s+FOUND)", RegexOptions.IgnoreCase)]
        private static partial Regex Rx_rxError_Generated();
        private static readonly Regex rxError =
            Rx_rxError_Generated();

        [GeneratedRegex(@"\b(WARN|WARNING)\b", RegexOptions.IgnoreCase)]
        private static partial Regex Rx_rxWarn_Generated();
        private static readonly Regex rxWarn =
            Rx_rxWarn_Generated();

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