using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CMPCodeDatabase.Tools;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Drag-and-drop/browse dialog to compute PCSX2 ELF CRC and persist a preferred .pnach name.
    /// </summary>
    public class ElfCrcPickerForm : Form
    {
        private readonly CollectorForm _owner;
        private Label _dropZone;
        private Button _btnBrowse;
        private Label _tip;

        public ElfCrcPickerForm(CollectorForm owner)
        {
            _owner = owner;
            InitializeUi();
        }

        private void InitializeUi()
        {
            this.Text = "Get ELF CRC";
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(540, 260);
            this.Padding = new Padding(12);

            _dropZone = new Label
            {
                Text = "PLEASE DROP FILE HERE",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(SystemFonts.DefaultFont.FontFamily, 16f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Height = 160,
                AllowDrop = true
            };
            _dropZone.DragEnter += DropZone_DragEnter;
            _dropZone.DragDrop += DropZone_DragDrop;

            _tip = new Label
            {
                Text = "Tip: You can drop SLUS_*, SLES_*, SCUS_*, SCES_*, *.ELF, or BOOT* files.\n" +
                       "We will compute the CRC and save a default .pnach name like SLES-52641_A1FD63D6.",
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 48
            };

            _btnBrowse = new Button
            {
                Text = "Browse…",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Width = 100,
                Height = 30,
                Left = this.ClientSize.Width - 100 - 12,
                Top = this.ClientSize.Height - 30 - 12
            };
            _btnBrowse.Click += BtnBrowse_Click;

            this.Controls.Add(_btnBrowse);
            this.Controls.Add(_tip);
            this.Controls.Add(_dropZone);

            // keep button at bottom-right even if system scales a bit
            this.Resize += (s, e) =>
            {
                _btnBrowse.Left = this.ClientSize.Width - _btnBrowse.Width - 12;
                _btnBrowse.Top  = this.ClientSize.Height - _btnBrowse.Height - 12;
            };
        }

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void DropZone_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length == 0) return;
                ComputeAndPersist(files[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to read dropped file.\n\n" + ex.Message, "Get ELF CRC", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Select PS2 ELF",
                Filter = "PS2 ELF|SLUS_*;SLES_*;SCUS_*;SCES_*;*.ELF;BOOT*;*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                ComputeAndPersist(ofd.FileName);
            }
        }

        private void ComputeAndPersist(string elfPath)
        {
            if (string.IsNullOrWhiteSpace(elfPath) || !File.Exists(elfPath))
            {
                MessageBox.Show(this, "Invalid file selected.", "Get ELF CRC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string crc = Pcsx2ElfCrc.ComputeFromFile(elfPath);
            if (string.IsNullOrWhiteSpace(crc))
            {
                MessageBox.Show(this, "Unable to compute CRC from the selected ELF.", "Get ELF CRC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string baseName = Path.GetFileName(elfPath);
            string formatted = FormatElfBaseForPnach(baseName);
            if (string.IsNullOrEmpty(formatted))
                formatted = SanitizeBaseName(baseName);

            string preferred = $"{formatted}_{crc}";
            try { Clipboard.SetText(preferred); } catch { /* ignore */ }

            // Persist into collector as default base name
            try { _owner?.AcceptPreferredPnachBaseName_META(preferred); } catch { }

            MessageBox.Show(this,
                $"CRC = {crc}\nSaved preferred .pnach base name:\n{preferred}\n\n" +
                $"Copied to clipboard.\nThis will be used as the default file name for PCSX2 export when available.",
                "PCSX2 ELF CRC",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>Heuristic: turn e.g. "SLES_526.41" or "SLUS_209.56" into "SLES-52641" / "SLUS-20956".</summary>
        private static string FormatElfBaseForPnach(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            // Keep letters/digits only, uppercase.
            var alnum = Regex.Replace(fileName.ToUpperInvariant(), "[^A-Z0-9]", "");
            // Expect 4-letter region prefix followed by digits.
            var m = Regex.Match(alnum, @"^([A-Z]{4})([0-9]+)$");
            if (m.Success)
            {
                string tag = m.Groups[1].Value;
                string digits = m.Groups[2].Value;
                return $"{tag}-{digits}";
            }
            // Fallback: if it starts with 4 letters somewhere, split there.
            m = Regex.Match(alnum, @"([A-Z]{4})([0-9]{2,})");
            if (m.Success) return $"{m.Groups[1].Value}-{m.Groups[2].Value}";
            return null;
        }

        private static string SanitizeBaseName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "GAME";
            var clean = Regex.Replace(fileName, @"[^\w\-]+", "");
            return string.IsNullOrEmpty(clean) ? "GAME" : clean.ToUpperInvariant();
        }
    }
}
