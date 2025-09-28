using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CMPCodeDatabase
{
    /// <summary>
    /// Robust TreeView horizontal-extent updater.
    /// v5:
    ///  - Measure with NodeFont when set, otherwise control Font
    ///  - Include glyph/indent + checkbox + state image + image columns
    ///  - Consider visible node Bounds.Right
    ///  - Take the max of padded and unpadded text measurements
    ///  - Add generous tail padding so no final glyph is clipped
    /// </summary>
    internal static class TreeViewExtent
    {
        private const int TV_FIRST = 0x1100;
        private const int TVM_SETHORIZONTALEXTENT = TV_FIRST + 40;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static void UpdateHorizontalExtent(TreeView tv)
        {
            if (tv == null || tv.IsDisposed || !tv.IsHandleCreated) return;

            int max = 0;
            using (var g = tv.CreateGraphics())
            {
                max = MaxNodeWidth(tv, tv.Nodes, g, level: 0);
            }

            // Final headroom (covers DPI/theme/rounding and bold)
            max += 128;

            // Force at least a bit wider than client area so a scrollbar appears
            int min = tv.ClientSize.Width + 16;
            if (max < min) max = min;

            SendMessage(tv.Handle, TVM_SETHORIZONTALEXTENT, (IntPtr)max, IntPtr.Zero);
        }

        private static int MaxNodeWidth(TreeView tv, TreeNodeCollection nodes, Graphics g, int level)
        {
            int max = 0;

            foreach (TreeNode n in nodes)
            {
                // Use the actual node font if provided (bold, etc.). Do NOT dispose control fonts.
                Font f = n.NodeFont ?? tv.Font;

                // Two text widths: strict (no padding) and padded (Windows default)
                int wStrict = TextRenderer.MeasureText(
                    g, n.Text ?? string.Empty, f, new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                ).Width;

                int wPadded = TextRenderer.MeasureText(
                    g, n.Text ?? string.Empty, f, new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                ).Width;

                int textW = Math.Max(wStrict, wPadded);

                // Visual chrome/columns
                int indentPerLevel = tv.Indent;
                int baseGlyph = indentPerLevel;               // expando column even for root
                int perLevel  = level * indentPerLevel;
                int checkW    = tv.CheckBoxes ? 16 : 0;
                int stateW    = tv.StateImageList?.ImageSize.Width ?? 0;
                int imageW    = tv.ImageList?.ImageSize.Width ?? 0;
                const int spacing = 8;

                int computed = baseGlyph + perLevel + checkW + stateW + imageW + spacing + textW;

                // If node is visible, also consider its actual Bounds.Right (includes theme offsets)
                if (n.IsVisible)
                {
                    int boundsRight = n.Bounds.Right + 12; // small pad
                    if (boundsRight > computed) computed = boundsRight;
                }

                if (computed > max) max = computed;

                if (n.Nodes != null && n.Nodes.Count > 0)
                {
                    int childMax = MaxNodeWidth(tv, n.Nodes, g, level + 1);
                    if (childMax > max) max = childMax;
                }
            }

            return max;
        }
    }
}
