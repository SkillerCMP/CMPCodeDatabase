// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: Core/Services/HelpContentService.cs
// Purpose: Project source file.
// Notes:
//  • Documentation-only header added (no behavioral changes).
//  • Keep UI hooks intact: EnsureDownloadButtons(), EnsureStartupChecks(), EnsureCloudMenu().
//  • Database root resolution is centralized (ResolveDatabasesRoot / helpers).
//  • Startup creates: Files\, Files\Database\, Files\Tools\ (if missing).
//  • 'ReloadDB' clears trees and calls LoadDatabaseSelector().
// Added: 2025-09-12
// ─────────────────────────────────────────────────────────────────────────────

namespace CMPCodeDatabase.Core.Services
{
    public sealed class HelpContentService : IHelpContentService
    {
        public string GetCodeTextLegendHtml() => @"
<h2>Code Text</h2>
<ul>
  <li><b>+Name</b> → start of a code entry.</li>
  <li><b>!Group</b> / <b>!!GroupEnd</b> → group boundaries.</li>
  <li><b>{ ... }</b> → HTML note (supports &lt;b&gt;, &lt;i&gt;, &lt;span style='color:#...'&gt;).</li>
  <li><b>%Credits:</b> Name[:Role]</li>
  <li><b>^1 = Hash:</b> value (multiple allowed)</li>
  <li><b>^2 = GameID:</b> value (multiple allowed)</li>
  <li><b>[Amount:VAL:TYPE:ENDIAN]</b> → dynamic amounts.</li>
  <li>Badges: <b>-M-</b>=mods, <b>-N-</b>=note, <b>-NM-</b>=both.</li>
</ul>";
    }
}
