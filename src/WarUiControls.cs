using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RocGame
{
    /// <summary>
    /// 二戰復古兵棋圖桌配色體系：深煙燻核桃木、做舊黃銅、普魯士戰地灰藍與羊皮紙墨色。
    /// </summary>
    public static class WarPalette
    {
        public static readonly Color BgDark = Color.FromArgb(22, 20, 17);
        public static readonly Color BgPanel = Color.FromArgb(30, 27, 22);
        public static readonly Color BgCard = Color.FromArgb(40, 35, 29);
        public static readonly Color BgCardHi = Color.FromArgb(56, 48, 38);
        public static readonly Color BorderCol = Color.FromArgb(108, 91, 64);
        public static readonly Color GoldCol = Color.FromArgb(218, 174, 92);
        public static readonly Color GoldDim = Color.FromArgb(142, 114, 62);
        public static readonly Color TextMain = Color.FromArgb(240, 232, 214);
        public static readonly Color TextSub = Color.FromArgb(182, 168, 144);
        public static readonly Color RocBlue = Color.FromArgb(52, 96, 134);
        public static readonly Color PrcRed = Color.FromArgb(142, 62, 56);
        public static readonly Color UssrPurple = Color.FromArgb(106, 62, 92);
        public static readonly Color GoodGreen = Color.FromArgb(102, 168, 112);
        public static readonly Color BadRed = Color.FromArgb(214, 92, 82);

        public static Color AdjustBrightness(Color c, int delta, int? alpha = null)
        {
            return Color.FromArgb(
                alpha ?? c.A,
                Math.Clamp(c.R + delta, 0, 255),
                Math.Clamp(c.G + delta, 0, 255),
                Math.Clamp(c.B + delta, 0, 255));
        }
    }

    public static class WarGdi
    {
        public static GraphicsPath CreateRoundedPath(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float w = Math.Max(1f, r.Width);
            float h = Math.Max(1f, r.Height);
            float rad = Math.Clamp(radius, 0.5f, Math.Min(w, h) * 0.45f);
            float d = rad * 2f;

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawPlaqueFrame(Graphics g, RectangleF r, float radius, Color fillCol, Color outerCol, Color innerCol, bool drawRivets = false)
        {
            if (r.Width <= 2 || r.Height <= 2) return;
            using var path = CreateRoundedPath(r, radius);

            Color topCol = WarPalette.AdjustBrightness(fillCol, 7);
            Color botCol = WarPalette.AdjustBrightness(fillCol, -5);
            using (var bg = new LinearGradientBrush(r, topCol, botCol, LinearGradientMode.Vertical))
                g.FillPath(bg, path);

            // 頂緣紙墨微光
            if (r.Width > 14 && r.Height > 10)
            {
                using var sheenPen = new Pen(Color.FromArgb(26, 255, 244, 214), 1f);
                float pad = Math.Min(radius, r.Width * 0.25f) + 1f;
                g.DrawLine(sheenPen, r.X + pad, r.Y + 1f, r.Right - pad, r.Y + 1f);
            }

            using (var penOut = new Pen(outerCol, 1.35f))
                g.DrawPath(penOut, path);

            if (r.Width > 18 && r.Height > 18)
            {
                var rIn = new RectangleF(r.X + 3f, r.Y + 3f, r.Width - 6f, r.Height - 6f);
                using var pathIn = CreateRoundedPath(rIn, Math.Max(1.5f, radius - 2.5f));
                using var penIn = new Pen(innerCol, 0.85f);
                g.DrawPath(penIn, pathIn);
            }

            if (drawRivets && r.Width >= 200 && r.Height >= 110)
            {
                using var rivetBr = new SolidBrush(Color.FromArgb(175, WarPalette.GoldDim));
                float d = 3.2f;
                float off = 7.5f;
                g.FillEllipse(rivetBr, r.X + off, r.Y + off, d, d);
                g.FillEllipse(rivetBr, r.Right - off - d, r.Y + off, d, d);
                g.FillEllipse(rivetBr, r.X + off, r.Bottom - off - d, d, d);
                g.FillEllipse(rivetBr, r.Right - off - d, r.Bottom - off - d, d, d);
            }
        }
    }

    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
    }

    /// <summary>
    /// 復古戰情室銅邊木紋銘牌面板（圓角漸層＋內外壓線與角釘質感）。
    /// </summary>
    public class WarRoomPanel : BufferedPanel
    {
        public Color FrameBorderColor { get; set; } = WarPalette.GoldCol;
        public Color InnerBorderColor { get; set; } = Color.FromArgb(72, 62, 46);
        public int Chamfer { get; set; } = 10;
        public bool ShowCornerRivets { get; set; } = true;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Parent != null && BackColor.A < 255)
            {
                base.OnPaintBackground(e);
            }
            else if (Parent != null)
            {
                Color pCol = Color.FromArgb(255, Parent.BackColor.R, Parent.BackColor.G, Parent.BackColor.B);
                g.Clear(pCol);
            }
            float rad = Math.Clamp(Chamfer * 0.72f, 4f, 11f);
            WarGdi.DrawPlaqueFrame(g, new RectangleF(0.5f, 0.5f, Width - 2f, Height - 2f),
                rad, BackColor, FrameBorderColor, InnerBorderColor, ShowCornerRivets);
        }
    }

    /// <summary>
    /// 全自繪圓角黃銅質感按鈕，徹底取代生硬原生直角 WinForms 按鈕。
    /// </summary>
    public class WarButton : Button
    {
        private bool _hovered;
        private bool _pressed;

        public WarButton()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
        }

        protected override bool ShowFocusCues => false;

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color p = Parent?.BackColor ?? WarPalette.BgPanel;
            pevent.Graphics.Clear(Color.FromArgb(255, p.R, p.G, p.B));
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new RectangleF(0.5f, 0.5f, Math.Max(4f, Width - 2f), Math.Max(4f, Height - 2f));
            float radius = Math.Clamp(Height * 0.22f, 4f, 7.5f);

            Color baseFill = BackColor == Color.Transparent ? WarPalette.BgCard : BackColor;
            Color fillCol = !Enabled
                ? Color.FromArgb(28, 25, 21)
                : _pressed ? WarPalette.AdjustBrightness(baseFill, -8)
                : _hovered ? WarPalette.AdjustBrightness(baseFill, 14)
                : baseFill;

            Color rawBorder = FlatAppearance.BorderColor.IsEmpty ? WarPalette.BorderCol : FlatAppearance.BorderColor;
            Color borderCol = !Enabled
                ? Color.FromArgb(62, 54, 42)
                : _hovered ? WarPalette.GoldCol
                : rawBorder;

            Color topGrad = WarPalette.AdjustBrightness(fillCol, Enabled ? 10 : 3);
            Color botGrad = WarPalette.AdjustBrightness(fillCol, Enabled ? -9 : -3);

            using (var path = WarGdi.CreateRoundedPath(rect, radius))
            using (var brush = new LinearGradientBrush(rect, topGrad, botGrad, LinearGradientMode.Vertical))
            using (var pen = new Pen(borderCol, _hovered && Enabled ? 1.45f : 1.1f))
            {
                g.FillPath(brush, path);
                if (Enabled && Width > 14)
                {
                    using var sheenPen = new Pen(Color.FromArgb(_hovered ? 42 : 24, 255, 244, 214), 1f);
                    float pad = Math.Min(radius, Width * 0.25f) + 1f;
                    g.DrawLine(sheenPen, rect.X + pad, rect.Y + 1.2f, rect.Right - pad, rect.Y + 1.2f);
                }
                g.DrawPath(pen, path);
            }

            if (Enabled && Focused)
            {
                var focusRect = RectangleF.Inflate(rect, -4.5f, -4.5f);
                using var focusPath = WarGdi.CreateRoundedPath(focusRect, Math.Max(2.5f, radius - 2.5f));
                using var focusPen = new Pen(Color.FromArgb(235, WarPalette.GoldCol), 1.2f);
                g.DrawPath(focusPen, focusPath);
            }

            Color fg = !Enabled
                ? Color.FromArgb(128, 118, 102)
                : ForeColor;

            var txtRect = new Rectangle(
                TextAlign == ContentAlignment.MiddleLeft ? 10 : 4,
                _pressed && Enabled ? 2 : 1,
                TextAlign == ContentAlignment.MiddleLeft ? Math.Max(8, Width - 18) : Math.Max(8, Width - 8),
                Math.Max(8, Height - 2));

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            flags |= TextAlign switch
            {
                ContentAlignment.MiddleLeft => TextFormatFlags.Left,
                ContentAlignment.MiddleRight => TextFormatFlags.Right,
                _ => TextFormatFlags.HorizontalCenter
            };

            TextRenderer.DrawText(g, Text ?? "", Font, txtRect, fg, flags);
        }
    }

    /// <summary>
    /// 深色自繪細捲軸容器：徹底消滅 WinForms 原生亮白色系統捲軸，支援滾輪、拖曳滑塊與軌道點擊。
    /// </summary>
    public class DarkScrollContainer : BufferedPanel
    {
        private readonly BufferedPanel _viewportHost;
        private Control _content;
        private int _scrollX;
        private int _scrollY;
        private bool _hoverVBar;
        private bool _hoverHBar;
        private bool _dragV;
        private bool _dragH;
        private int _dragStartMouse;
        private int _dragStartScroll;

        public const int BarThickness = 11;
        public bool ShowVerticalBar { get; set; } = true;
        public bool ShowHorizontalBar { get; set; } = false;
        public Color TrackBorderColor { get; set; } = Color.FromArgb(58, 49, 37);

        public int ScrollX => _scrollX;
        public int ScrollY => _scrollY;
        public int ViewportWidth => Math.Max(20, ClientSize.Width - (ShowVerticalBar ? BarThickness + 3 : 2));
        public int ViewportHeight => Math.Max(20, ClientSize.Height - (ShowHorizontalBar ? BarThickness + 3 : 2));

        public DarkScrollContainer()
        {
            AutoScroll = false;
            BackColor = WarPalette.BgDark;
            _viewportHost = new BufferedPanel
            {
                Location = new Point(1, 1),
                BackColor = WarPalette.BgDark
            };
            Controls.Add(_viewportHost);
        }

        public void SetContent(Control content)
        {
            if (_content != null)
            {
                _content.SizeChanged -= Content_SizeChanged;
                _viewportHost.Controls.Remove(_content);
            }
            _content = content;
            if (_content != null)
            {
                _content.Location = new Point(0, 0);
                _content.SizeChanged += Content_SizeChanged;
                _viewportHost.Controls.Add(_content);
            }
            _scrollX = 0;
            _scrollY = 0;
            SyncScroll();
        }

        private void Content_SizeChanged(object sender, EventArgs e) => SyncScroll();

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            SyncScroll();
        }

        private int MaxScrollX => _content == null || !ShowHorizontalBar ? 0 : Math.Max(0, _content.Width - ViewportWidth);
        private int MaxScrollY => _content == null || !ShowVerticalBar ? 0 : Math.Max(0, _content.Height - ViewportHeight);

        public void SyncScroll()
        {
            _viewportHost.BackColor = BackColor;
            _viewportHost.Bounds = new Rectangle(1, 1, ViewportWidth, ViewportHeight);
            _scrollX = Math.Clamp(_scrollX, 0, MaxScrollX);
            _scrollY = Math.Clamp(_scrollY, 0, MaxScrollY);
            if (_content != null)
            {
                var targetLoc = new Point(-_scrollX, -_scrollY);
                if (_content.Location != targetLoc)
                    _content.Location = targetLoc;
            }
            Invalidate();
        }

        public void ScrollTo(int x, int y)
        {
            _scrollX = x;
            _scrollY = y;
            SyncScroll();
        }

        public bool HandleMouseWheel(int delta, bool horizontal)
        {
            if (horizontal && MaxScrollX > 0)
            {
                _scrollX -= Math.Sign(delta) * 56;
                SyncScroll();
                return true;
            }
            if (MaxScrollY > 0)
            {
                _scrollY -= Math.Sign(delta) * 56;
                SyncScroll();
                return true;
            }
            if (MaxScrollX > 0)
            {
                _scrollX -= Math.Sign(delta) * 56;
                SyncScroll();
                return true;
            }
            return false;
        }

        private Rectangle GetVTrackRect()
            => new Rectangle(ClientSize.Width - BarThickness, 2, BarThickness - 2, Math.Max(16, ViewportHeight - 4));

        private Rectangle GetHTrackRect()
            => new Rectangle(2, ClientSize.Height - BarThickness, Math.Max(16, ViewportWidth - 4), BarThickness - 2);

        private Rectangle GetVThumbRect()
        {
            if (_content == null || MaxScrollY <= 0) return Rectangle.Empty;
            var tr = GetVTrackRect();
            float ratio = Math.Clamp((float)ViewportHeight / Math.Max(1, _content.Height), 0.08f, 1f);
            int th = Math.Max(26, (int)(tr.Height * ratio));
            int travel = Math.Max(1, tr.Height - th);
            int ty = tr.Y + (int)(travel * ((float)_scrollY / MaxScrollY));
            return new Rectangle(tr.X + 1, ty, tr.Width - 2, th);
        }

        private Rectangle GetHThumbRect()
        {
            if (_content == null || MaxScrollX <= 0) return Rectangle.Empty;
            var tr = GetHTrackRect();
            float ratio = Math.Clamp((float)ViewportWidth / Math.Max(1, _content.Width), 0.08f, 1f);
            int tw = Math.Max(26, (int)(tr.Width * ratio));
            int travel = Math.Max(1, tr.Width - tw);
            int tx = tr.X + (int)(travel * ((float)_scrollX / MaxScrollX));
            return new Rectangle(tx, tr.Y + 1, tw, tr.Height - 2);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragV && MaxScrollY > 0)
            {
                var tr = GetVTrackRect();
                var th = GetVThumbRect();
                int travel = Math.Max(1, tr.Height - th.Height);
                int dy = e.Y - _dragStartMouse;
                _scrollY = _dragStartScroll + (int)Math.Round(dy * ((double)MaxScrollY / travel));
                SyncScroll();
                return;
            }
            if (_dragH && MaxScrollX > 0)
            {
                var tr = GetHTrackRect();
                var th = GetHThumbRect();
                int travel = Math.Max(1, tr.Width - th.Width);
                int dx = e.X - _dragStartMouse;
                _scrollX = _dragStartScroll + (int)Math.Round(dx * ((double)MaxScrollX / travel));
                SyncScroll();
                return;
            }
            bool hv = ShowVerticalBar && GetVTrackRect().Contains(e.Location);
            bool hh = ShowHorizontalBar && GetHTrackRect().Contains(e.Location);
            if (hv != _hoverVBar || hh != _hoverHBar)
            {
                _hoverVBar = hv;
                _hoverHBar = hh;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            if (ShowVerticalBar && MaxScrollY > 0)
            {
                var vThumb = GetVThumbRect();
                var vTrack = GetVTrackRect();
                if (vThumb.Contains(e.Location))
                {
                    _dragV = true;
                    _dragStartMouse = e.Y;
                    _dragStartScroll = _scrollY;
                    Capture = true;
                    return;
                }
                if (vTrack.Contains(e.Location))
                {
                    float frac = Math.Clamp((float)(e.Y - vTrack.Y) / Math.Max(1, vTrack.Height), 0f, 1f);
                    _scrollY = (int)(frac * MaxScrollY);
                    SyncScroll();
                    return;
                }
            }

            if (ShowHorizontalBar && MaxScrollX > 0)
            {
                var hThumb = GetHThumbRect();
                var hTrack = GetHTrackRect();
                if (hThumb.Contains(e.Location))
                {
                    _dragH = true;
                    _dragStartMouse = e.X;
                    _dragStartScroll = _scrollX;
                    Capture = true;
                    return;
                }
                if (hTrack.Contains(e.Location))
                {
                    float frac = Math.Clamp((float)(e.X - hTrack.X) / Math.Max(1, hTrack.Width), 0f, 1f);
                    _scrollX = (int)(frac * MaxScrollX);
                    SyncScroll();
                    return;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragV || _dragH)
            {
                _dragV = false;
                _dragH = false;
                Capture = false;
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverVBar || _hoverHBar)
            {
                _hoverVBar = false;
                _hoverHBar = false;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (ShowVerticalBar)
            {
                var tr = GetVTrackRect();
                using var trackBr = new SolidBrush(Color.FromArgb(20, 18, 15));
                using var trPath = WarGdi.CreateRoundedPath(tr, 4f);
                g.FillPath(trackBr, trPath);

                var thumb = GetVThumbRect();
                if (!thumb.IsEmpty)
                {
                    Color thCol = _dragV ? WarPalette.GoldCol : _hoverVBar ? Color.FromArgb(182, 146, 82) : Color.FromArgb(116, 97, 66);
                    using var thPath = WarGdi.CreateRoundedPath(thumb, 3.5f);
                    using var thBr = new SolidBrush(thCol);
                    g.FillPath(thBr, thPath);
                }
            }

            if (ShowHorizontalBar)
            {
                var tr = GetHTrackRect();
                using var trackBr = new SolidBrush(Color.FromArgb(20, 18, 15));
                using var trPath = WarGdi.CreateRoundedPath(tr, 4f);
                g.FillPath(trackBr, trPath);

                var thumb = GetHThumbRect();
                if (!thumb.IsEmpty)
                {
                    Color thCol = _dragH ? WarPalette.GoldCol : _hoverHBar ? Color.FromArgb(182, 146, 82) : Color.FromArgb(116, 97, 66);
                    using var thPath = WarGdi.CreateRoundedPath(thumb, 3.5f);
                    using var thBr = new SolidBrush(thCol);
                    g.FillPath(thBr, thPath);
                }
            }

            using var bdPen = new Pen(TrackBorderColor, 1f);
            g.DrawRectangle(bdPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
    }
}
