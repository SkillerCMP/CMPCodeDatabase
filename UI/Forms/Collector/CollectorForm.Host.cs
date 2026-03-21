using System.Drawing;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Window host for CollectorControl (windowed collector mode).
    /// The real collector UI + logic lives in CollectorControl so it can also be embedded in tabs.
    /// </summary>
    public sealed class CollectorForm : Form
    {
        public CollectorControl Collector { get; }

        public CollectorForm()
        {
            Text = "Code Collector";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(660, 680);
            Size = new Size(780, 820);

            Collector = new CollectorControl
            {
                Dock = DockStyle.Fill
            };

            Controls.Add(Collector);

            // Attach after the handle exists and the window is visible.
            Shown += (_, __) => Collector.AttachHost(this, CollectorControl.CollectorHostMode.Windowed);
        }

        // Compatibility wrappers (so older MainForm code can keep calling CollectorForm.*)
        public void AddItem(string name, string code) => Collector.AddItem(name, code);
        public void AddItem(string name, string code, string? author, string? description) => Collector.AddItem(name, code, author, description);
        public void SetActiveGame(string gameName) => Collector.SetActiveGame(gameName);
        public bool HasAnyItems() => Collector.HasAnyItems();
    }
}
