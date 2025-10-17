using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CMPCodeDatabase.SpecialMods
{
    internal class JokerDialog : Form
    {
        private ComboBox cmbPlatform;
        private CheckBox chkReverse;
        private Label lblPreview;
        private Panel pnlButtons;
        private GroupBox grpWiiLayout;
        private RadioButton rbWiimote;
        private RadioButton rbClassic;
        private Button btnInsert, btnCancel;

        private HashSet<string> pressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string initialPlat;   // "PS2" | "GC" | "Wii" | "GBA" | "ALL"
        private readonly bool platLocked;      // lock dropdown when not ALL

        public string ResultHex { get; private set; } = "0000";
        public string ResultPressLabel { get; private set; } = "Press Buttons";

        public JokerDialog(string platform, HashSet<string> mods = null)
        {
            initialPlat = string.IsNullOrWhiteSpace(platform) ? "PS2" : platform.ToUpperInvariant();
            platLocked = initialPlat != "ALL";

            Text = "Joker — Pick Buttons";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 440);

            cmbPlatform = new ComboBox { Left = 12, Top = 12, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPlatform.Items.AddRange(new object[] { "PS2", "GC", "Wii", "GBA" });
            cmbPlatform.SelectedIndexChanged += (s, e) => RebuildButtons();

            chkReverse = new CheckBox { Left = 170, Top = 14, Width = 120, Text = "Reverse" };
            chkReverse.CheckedChanged += (s, e) => UpdatePreview();

            lblPreview = new Label { Left = 300, Top = 14, Width = 200, Text = "Value: 0000", Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold) };

            grpWiiLayout = new GroupBox { Left = 12, Top = 44, Width = 220, Height = 52, Text = "Wii Layout" };
            rbWiimote = new RadioButton { Left = 12, Top = 22, Width = 90, Text = "Wiimote", Checked = true };
            rbClassic = new RadioButton { Left = 120, Top = 22, Width = 80, Text = "Classic" };
            rbWiimote.CheckedChanged += (s, e) => { if (cmbPlatform.Text == "Wii") UpdatePreview(); };
            rbClassic.CheckedChanged += (s, e) => { if (cmbPlatform.Text == "Wii") UpdatePreview(); };
            grpWiiLayout.Controls.AddRange(new Control[] { rbWiimote, rbClassic });
            grpWiiLayout.Visible = false;

            pnlButtons = new Panel { Left = 12, Top = 104, Width = 496, Height = 272, BorderStyle = BorderStyle.FixedSingle, AutoScroll = true };

            btnInsert = new Button { Left = 300, Top = 392, Width = 100, Text = "Insert" };
            btnCancel = new Button { Left = 408, Top = 392, Width = 100, Text = "Cancel" };
            btnInsert.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { cmbPlatform, chkReverse, lblPreview, grpWiiLayout, pnlButtons, btnInsert, btnCancel });

            // init selection
            var platToShow = platLocked ? initialPlat : "PS2";
            cmbPlatform.SelectedItem = platToShow;
            cmbPlatform.Enabled = !platLocked; // only ALL lets user change platform

            if (mods != null)
            {
                if (mods.Contains("REVERSE")) chkReverse.Checked = true;
                if (mods.Contains("CLASSIC")) { rbClassic.Checked = true; rbWiimote.Checked = false; }
                if (mods.Contains("WIIMOTE")) { rbWiimote.Checked = true; rbClassic.Checked = false; }
            }

            RebuildButtons();
            UpdatePreview();
        }

        private void RebuildButtons()
        {
            pressed.Clear();
            pnlButtons.Controls.Clear();
            grpWiiLayout.Visible = (cmbPlatform.Text == "Wii");

            string plat = cmbPlatform.Text;
            var groups = GetGroupsFor(plat);
            int col = 0;
            foreach (var grp in groups)
            {
                var gb = new GroupBox { Left = 8 + col * 122, Top = 8, Width = 118, Height = 250, Text = grp.name };
                int y = 20;
                foreach (var b in grp.buttons)
                {
                    var cb = new CheckBox { Left = 10, Top = y, Width = 96, Text = b };
                    cb.CheckedChanged += (s, e) =>
                    {
                        if (cb.Checked) pressed.Add(cb.Text.ToUpperInvariant());
                        else pressed.Remove(cb.Text.ToUpperInvariant());
                        UpdatePreview();
                    };
                    gb.Controls.Add(cb);
                    y += 22;
                }
                pnlButtons.Controls.Add(gb);
                col++;
            }
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            string plat = cmbPlatform.Text;
            bool reverse = chkReverse.Checked;
            string value = "0000";

            if (plat == "PS2")
                value = JokerCalculator.MaskPS2(pressed, reverse);
            else if (plat == "GC")
                value = JokerCalculator.MaskGC_BE(pressed);
            else if (plat == "Wii")
                value = rbClassic.Checked ? JokerCalculator.MaskWii_Classic(pressed, reverse)
                                          : JokerCalculator.MaskWii_Wiimote(pressed, reverse);
            else if (plat == "GBA")
                value = JokerCalculator.MaskGBA(pressed);

            ResultHex = value.ToUpperInvariant();
            ResultPressLabel = BuildPressLabel(plat, pressed);
            lblPreview.Text = $"Value: {ResultHex}";
        }

        private static string TitleCase(string s) =>
            string.IsNullOrWhiteSpace(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();

        // Produce "Press Select+Start" style label in a stable order per platform
        private string BuildPressLabel(string plat, HashSet<string> p)
        {
            var order = new List<string>();
            if (plat == "PS2" || plat == "GBA")
            {
                order.AddRange(new []{ "SELECT","START","L3","R3","UP","RIGHT","DOWN","LEFT","L2","R2","L1","R1","TRIANGLE","CIRCLE","X","SQUARE","A","B","R","L" });
            }
            else if (plat == "GC")
            {
                order.AddRange(new []{ "START","A","B","X","Y","Z","R","L","LEFT","RIGHT","DOWN","UP" });
            }
            else if (plat == "Wii")
            {
                if (rbClassic.Checked)
                    order.AddRange(new []{ "SUB","PLUS","L","R","ZL","ZR","A","B","X","Y","UP","LEFT","DOWN","RIGHT" });
                else
                    order.AddRange(new []{ "HOME","MINUS","PLUS","C","Z","A","B","ONE","TWO","LEFT","RIGHT","DOWN","UP" });
            }
            else // fallback
            {
                order.AddRange(p);
            }

            var picked = order.Where(x => p.Contains(x)).Select(TitleCase);
            var joined = string.Join("+", picked);
            if (string.IsNullOrEmpty(joined)) joined = "Buttons";
            return "Press " + joined;
        }

        private (string name, string[] buttons)[] GetGroupsFor(string plat)
        {
            if (plat == "PS2")
            {
                return new[]
                {
                    ("Face",    new []{"Triangle","Circle","X","Square"}),
                    ("Shoulder",new []{"L1","R1","L2","R2"}),
                    ("D-Pad",   new []{"Up","Right","Down","Left"}),
                    ("System",  new []{"Select","L3","R3","Start"}),
                };
            }
            if (plat == "GC")
            {
                return new[]
                {
                    ("Start",   new []{"Start"}),
                    ("Actions", new []{"A","B","X","Y"}),
                    ("Shoulder",new []{"Z","R","L"}),
                    ("D-Pad",   new []{"Left","Right","Down","Up"}),
                };
            }
            if (plat == "Wii")
            {
                if (rbClassic.Checked)
                {
                    return new[]
                    {
                        ("Group A", new []{"Sub","L","Down","Right"}),
                        ("Group B", new []{"R","Plus"}),
                        ("Group C", new []{"A","Y","B","ZL"}),
                        ("Group D", new []{"Up","Left","ZR","X"}),
                    };
                }
                else
                {
                    return new[]
                    {
                        ("Sys",     new []{"Home","C","Z","Minus"}),
                        ("Actions", new []{"A","B","One","Two"}),
                        ("Plus",    new []{"Plus"}),
                        ("D-Pad",   new []{"Left","Right","Down","Up"}),
                    };
                }
            }
            // GBA
            return new[]
            {
                ("Actions", new []{"A","B","Select","Start"}),
                ("D-Pad",   new []{"Right","Left","Up","Down"}),
                ("Shoulder",new []{"R","L"}),
            };
        }
    }
}
