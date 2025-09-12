// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/HelpForm/HelpForm.Tabs.CodeText.cs
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
using System.Windows.Forms;


namespace CMPCodeDatabase
{
    public partial class HelpForm : Form
    {
        private string BuildCodeTextLegendHtml()
                {
                    return @"
        <h1>Syntax Legend <span class='small'>(v1.1)</span></h1>

        <div class='block'>
          <div><span class='badge'>^1</span><span class='title'>Hash</span></div>
          <div class='desc'>Metadata line for the game's hash.<br>
        Use:<br>
        <pre>^1 = Hash: 123ABC(can have commas: 123ABC,9999,8888)</pre>
        Supports <b>multiple</b> hash lines; each one is shown in <i>See Info → IDs</i>.<br>
        Example:<br>
        <pre>^1 = Hash: 123ABC,9999,8888</pre></div>
        </div>

        <div class='block'>
          <div><span class='badge'>^2</span><span class='title'>GameID</span></div>
          <div class='desc'>Metadata line for the game ID.<br>
        Use:<br>
        <pre>^2 = GameID: TEST001(can have commas: TEST001,ALT-002</pre>
        Supports <b>multiple</b> GameID lines; each one is shown in <i>See Info → IDs</i>.<br>
        Example:<br>
        <pre>^2 = GameID: TEST001,ALT-002</pre></div>
        </div>

        <div class='block'>
          <div><span class='badge'>!</span><span class='title'>Group start</span></div>
          <div class='desc'>Begins a subgroup under the current group. Example: <span class='code'>! Characters</span></div>
        </div>

        <div class='block'>
          <div><span class='badge'>!!</span><span class='title'>Group end</span></div>
          <div class='desc'>Closes the most recent subgroup. To close multiple levels, put <span class='code'>!!</span> on separate lines twice.</div>
        </div>

        <div class='block'>
          <div><span class='badge'>+</span><span class='title'>Code entry name</span></div>
          <div class='desc'>Creates a code node; can include a note in braces. Example: <span class='code'>+ Infinite Health{Use offline only}</span></div>
        </div>

        <div class='block'>
          <div><span class='badge'>{...}</span><span class='title'>Note</span></div>
          <div class='desc'>HTML allowed: &lt;b&gt;, &lt;i&gt;, &lt;span style='color:#...'&gt;, &lt;br&gt;, etc.</div>
        </div>

        <div class='block'>
          <div><span class='badge'>$</span><span class='title'>Code line</span></div>
          <div class='desc'>Raw code bytes/words. Example: <span class='code'>$20000000 0000ABCD</span></div>
        </div>

        <div class='block'>
          <div><span class='badge'>[TAG] ... [/TAG]</span><span class='title'>Modifier block</span></div>
          <div class='desc'>Defines a table for replacing placeholders (e.g., <span class='code'>[ATK]</span>) in code templates.</div>
        </div>

        <h2>Modifiers (Value‑first)</h2>
        <div class='block'>
          <div class='title'>Header</div>
          <div class='desc'>Always <b>value‑first</b>:</div>
          <div class='code'>Value&gt;Name</div>
          <div class='desc'>You can extend headers: <span class='code'>Value&gt;Name&gt;Type&gt;...</span></div>
        </div>

        <div class='block'>
          <div class='title'>Rows</div>
          <div class='desc'>Use <b>=</b> or <b>TAB</b> as the delimiter (VALUE first):</div>
          <div class='code'>00000001=Small</div>
          <div class='code'>0000000A=Medium</div>
          <div class='code'>00000063=Large</div>
        </div>

        <div class='block'>
          <div class='title'>Replacement</div>
          <div class='desc'>In templates, <span class='code'>[TAG]</span> is replaced with the selected <b>Value</b>; the UI shows the <b>Name</b> and appends it to the node title.</div>
        </div>

        <h2>Mini Example</h2>
        <pre>^1 = Hash: ABC123
        ^2 = GameID: 0456
        !Character:
        !Character 1:{These affect Character 1}
        +Attack Booster{Pick a level}
        $20000000 [ATK]
        $10000004 00000063
        !!
        !!
        [ATK]
        Value>Name
        00000001=Small
        0000000A=Medium
        00000063=Large
        [/ATK]</pre>
        <hr>
        <h2>Credits &amp; Special Placeholders</h2>

        <div class='block'>
          <div><span class='badge'>%</span><span class='title'>Credits</span></div>
          <div class='desc'>
            Add contributor credits using lines that start with <code>%Credits:</code>.<br>
            The parser treats these as metadata (they do not affect patching).<br>
        	Should be stored Undere the code name.<br>
        	If you look at the Game Info By right clicking on the Game name ull see a total for each person credited.<br>
            <i>Examples:</i>
        	<pre></pre>
            <pre>+Code name
        %Credits: Jane Doe,Skiller S</pre>
          </div>
        </div>

        <div class='block'>
          <div><span class='badge'>[Amount:]</span><span class='title'>Numeric entry placeholder</span></div>
          <div class='desc'>
            Prompts for a numeric value and writes it into the code at that position.<br>
            <b>Form:</b> <code>[Amount:&lt;Value&gt;[, &lt;Type&gt;][, &lt;Endian&gt;]]</code><br>
            <b>Types (examples):</b> <code>HEX</code>, <code>Float</code><br>
            <b>Endian (optional):</b> <code>Little</code> (default) or <code>BIG</code><br>
            <b>Note:</b> Value Size Will be bast on Defualt Value Set Originaly<br>
            <i>Examples:</i>
            <pre>$28000020 [Amount:05F5E0FF:HEX:BIG]
        28000020 [Amount:05F5E0FF:FLOAT:BIG]</pre>
          </div>
        </div>
        ";
                }
    }
}
