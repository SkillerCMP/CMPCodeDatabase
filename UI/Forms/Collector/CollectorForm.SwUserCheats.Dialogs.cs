// ─────────────────────────────────────────────────────────────────────────────
// CMPCodeDatabase — File: UI/Forms/Collector/CollectorForm.SwUserCheats.Dialogs.cs
// Purpose: Save Wizard swusercheats.xml export dialog helpers.
// Notes:
//  • Split from CollectorForm.SwUserCheats.Export.cs during cleanup pass 5.
//  • Behavior intentionally unchanged.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CMPCodeDatabase.Core.Export.SaveWizard;

namespace CMPCodeDatabase
{
    public partial class CollectorControl : UserControl
    {
        private static bool PromptText(IWin32Window owner, string title, string label, string initial, out string value)
        {
            value = initial ?? string.Empty;

            using var f = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Width = 520,
                Height = 160
            };

            var lbl = new Label { Left = 12, Top = 12, Width = 480, Height = 18, Text = label };
            var tb = new TextBox { Left = 12, Top = 34, Width = 480, Text = value };

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 332, Width = 76, Top = 70 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 416, Width = 76, Top = 70 };

            f.Controls.Add(lbl);
            f.Controls.Add(tb);
            f.Controls.Add(ok);
            f.Controls.Add(cancel);
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            var res = f.ShowDialog(owner);
            if (res != DialogResult.OK)
                return false;

            value = tb.Text.Trim();
            return true;
        }

        private static bool PickContainer(IWin32Window owner, GameListGame game, out int containerIndex)
        {
            containerIndex = 0;

            using var f = new Form
            {
                Text = "Select Save Wizard Container",
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Width = 720,
                Height = 360
            };

            var list = new ListBox { Left = 12, Top = 12, Width = 680, Height = 260 };
            for (int i = 0; i < game.Containers.Count; i++)
            {
                var c = game.Containers[i];
                var pfs = string.IsNullOrWhiteSpace(c.Pfs) ? "" : $"  pfs: {c.Pfs}";
                list.Items.Add($"[{i}] key: {c.Key}  files: {c.FilePatterns.Count}{pfs}");
            }
            list.SelectedIndex = 0;

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 532, Width = 76, Top = 282 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 616, Width = 76, Top = 282 };

            f.Controls.Add(list);
            f.Controls.Add(ok);
            f.Controls.Add(cancel);
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            var res = f.ShowDialog(owner);
            if (res != DialogResult.OK)
                return false;

            containerIndex = Math.Max(0, list.SelectedIndex);
            return true;
        }

        private static bool PickFiles(IWin32Window owner, IReadOnlyList<string> filePatterns, out List<string> selected)
        {
            selected = new List<string>();

            using var f = new Form
            {
                Text = "Select Save Files (filename patterns)",
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Width = 720,
                Height = 420
            };

            var clb = new CheckedListBox { Left = 12, Top = 12, Width = 680, Height = 300, CheckOnClick = true };
            foreach (var p in filePatterns)
                clb.Items.Add(p, true); // default: all checked

            var btnAll = new Button { Text = "All", Left = 12, Top = 320, Width = 70 };
            var btnNone = new Button { Text = "None", Left = 88, Top = 320, Width = 70 };
            btnAll.Click += (_, __) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, true); };
            btnNone.Click += (_, __) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, false); };

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 532, Width = 76, Top = 320 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 616, Width = 76, Top = 320 };

            f.Controls.Add(clb);
            f.Controls.Add(btnAll);
            f.Controls.Add(btnNone);
            f.Controls.Add(ok);
            f.Controls.Add(cancel);
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            var res = f.ShowDialog(owner);
            if (res != DialogResult.OK)
                return false;

            for (int i = 0; i < clb.Items.Count; i++)
                if (clb.GetItemChecked(i))
                    selected.Add(clb.Items[i]?.ToString() ?? string.Empty);

            selected = selected.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.Ordinal).ToList();
            return true;
        }

    }
}
