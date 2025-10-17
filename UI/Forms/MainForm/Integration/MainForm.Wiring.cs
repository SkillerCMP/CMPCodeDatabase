// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/MainForm/Integration/MainForm.Wiring.cs (MERGED)
// Purpose: Preserve original startup wiring (TryWire* calls) and add Joker context wiring.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.ComponentModel; // CancelEventArgs
using System.Linq;           // Any()
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Unified wiring point. Preserves original OnHandleCreated TryWire* calls and
    /// adds InitializeJokerWiring() so the editor gets the "Open Joker Controller…" item.
    /// </summary>
    public partial class MainForm : Form
    {
        // === Added fields for Joker wiring ===
        private ToolStripMenuItem _ctxOpenJokerItem;
        private ContextMenuStrip _jokerMenuBoundTo;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Original behavior — keep these intact
            try { TryWireCollectorSync(); } catch { /* optional: not present in all builds */ }
            try { TryWireDatabaseRootSwitch(); } catch { /* optional: not present in all builds */ }
            try { TryWireSeeInfo(); } catch { /* must be present from this patch */ }
            // try { TryWireAutoSend(); } catch { /* if/when you re-enable Step 5 */ }

            // Added: ensure Joker context is available
            try { InitializeJokerWiring(); } catch { /* defensive */ }
        }

        /// <summary>
        /// Call once per active editor to wire the Joker context item
        /// </summary>
        private void InitializeJokerWiring()
        {
            var rtb = GetActiveEditor();
            if (rtb == null) return;

            EnsureMenuOnEditor(rtb);
            EnsureJokerMenuItem(rtb.ContextMenuStrip);
        }

        /// <summary>
        /// Use this when your UI switches tabs/editors.
        /// </summary>
        private void InitializeJokerWiringFor(RichTextBox editor)
        {
            if (editor == null) return;
            EnsureMenuOnEditor(editor);
            EnsureJokerMenuItem(editor.ContextMenuStrip);
        }

        private void EnsureMenuOnEditor(RichTextBox rtb)
        {
            if (rtb.ContextMenuStrip == null)
                rtb.ContextMenuStrip = new ContextMenuStrip();
        }

        private void EnsureJokerMenuItem(ContextMenuStrip menu)
        {
            if (menu == null) return;

            if (_jokerMenuBoundTo == menu && _ctxOpenJokerItem != null)
                return; // already wired to this menu

            // Remove from previous menu when switching editors
            if (_jokerMenuBoundTo != null && _jokerMenuBoundTo != menu && _ctxOpenJokerItem != null)
            {
                try
                {
                    var idx = _jokerMenuBoundTo.Items.IndexOf(_ctxOpenJokerItem);
                    if (idx > 0 && _jokerMenuBoundTo.Items[idx - 1] is ToolStripSeparator)
                        _jokerMenuBoundTo.Items.RemoveAt(idx - 1);
                    if (idx >= 0)
                        _jokerMenuBoundTo.Items.RemoveAt(idx);
                }
                catch { /* ignore */ }
            }

            if (_ctxOpenJokerItem == null)
            {
                _ctxOpenJokerItem = new ToolStripMenuItem("Open Joker Controller…");
                _ctxOpenJokerItem.Click += (s, e) =>
                {
                    var rtb = GetActiveEditor();
                    if (rtb == null) return;
                    CMPCodeDatabase.SpecialMods.JokerMod.ResolveTokenAtCaret(this, rtb, keepTokenAppend: false);
                };
            }

            if (!menu.Items.Contains(_ctxOpenJokerItem))
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(_ctxOpenJokerItem);
            }

            menu.Opening -= ContextMenuStrip_Opening_ForJoker;
            menu.Opening += ContextMenuStrip_Opening_ForJoker;

            _jokerMenuBoundTo = menu;
        }

        private void ContextMenuStrip_Opening_ForJoker(object sender, CancelEventArgs e)
        {
            var rtb = GetActiveEditor();
            bool hasToken = false;
            if (rtb != null)
            {
                var text = rtb.Text;
                hasToken = CMPCodeDatabase.SpecialMods.JokerMod.FindTokens(text).Any();
            }
            if (_ctxOpenJokerItem != null)
                _ctxOpenJokerItem.Enabled = hasToken;
        }

        // Replace with your actual accessor if your editor is wrapped
        private RichTextBox GetActiveEditor()
        {
            return this.ActiveControl as RichTextBox;
        }
    }
}
