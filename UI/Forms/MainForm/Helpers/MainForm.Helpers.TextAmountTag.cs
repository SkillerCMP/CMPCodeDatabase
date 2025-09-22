using System;

namespace CMPCodeDatabase
{
    public partial class MainForm
    {
        /// <summary>
        /// Parses a text-amount tag of the form:
        ///   [Amount:&lt;baseText&gt;:&lt;encToken&gt;:TXT]
        /// or   [Amount:&lt;baseText&gt;:&lt;encToken&gt;:TXT&lt;...&gt;]
        /// Returns true and sets baseText/encToken if recognized.
        /// </summary>
        private bool TryParseTextAmountTag(string token, out string baseText, out string encToken)
        {
            baseText = string.Empty;
            encToken = "UTF08";

            if (string.IsNullOrWhiteSpace(token))
                return false;

            string core = token.Trim();
            if (core.StartsWith("[" ) && core.EndsWith("]"))
                core = core.Substring(1, core.Length - 2);

            if (!core.StartsWith("Amount:", StringComparison.OrdinalIgnoreCase))
                return false;

            var parts = core.Split(':');
            if (parts.Length < 4)
                return false;

            // mode may be "TXT" or "TXT<...>"
            var mode = parts[3] ?? string.Empty;
            int lt = mode.IndexOf('<');
            if (lt >= 0) mode = mode.Substring(0, lt).Trim();

            if (!mode.Equals("TXT", StringComparison.OrdinalIgnoreCase))
                return false;

            baseText = (parts[1] ?? string.Empty).Trim();
            encToken = (parts[2] ?? "UTF08").Trim();
            return true;
        }
    }
}
