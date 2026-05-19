using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleTodo
{
    public class ModernCheckBox : Control
    {
        private bool isChecked;
        private bool isHovered;

        private static readonly Color BorderColor = Color.FromArgb(209, 209, 214);
        private static readonly Color CheckedBg = Color.FromArgb(234, 88, 12);
        private static readonly Color HoverBorder = Color.FromArgb(234, 88, 12);

        public bool Checked
        {
            get { return isChecked; }
            set { isChecked = value; Invalidate(); }
        }

        public event EventHandler CheckedChanged;

        public ModernCheckBox()
        {
            this.Size = new Size(20, 20);
            this.Cursor = Cursors.Hand;
            this.DoubleBuffered = true;
            this.TabStop = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(1, 1, 18, 18);

            using (var path = RoundedRect(rect, 5))
            {
                if (isChecked)
                {
                    using (var brush = new SolidBrush(CheckedBg))
                        g.FillPath(brush, path);
                    using (var pen = new Pen(CheckedBg, 1.5f))
                        g.DrawPath(pen, path);
                }
                else
                {
                    using (var brush = new SolidBrush(Color.White))
                        g.FillPath(brush, path);
                    var border = isHovered ? HoverBorder : BorderColor;
                    using (var pen = new Pen(border, 1.5f))
                        g.DrawPath(pen, path);
                }
            }

            if (isChecked)
            {
                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 5, 10, 8, 13);
                    g.DrawLine(pen, 8, 13, 14, 6);
                }
            }
        }

        protected override void OnClick(EventArgs e)
        {
            isChecked = !isChecked;
            Invalidate();
            base.OnClick(e);
            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                OnClick(EventArgs.Empty);
                return;
            }
            base.OnKeyDown(e);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
