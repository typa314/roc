using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace RocGame
{
    public partial class MainForm : Form, IMessageFilter
    {
        // ==================== 全版面軍事情報處專頁（◎ 軍情處） ====================
        private static readonly (string key, string label)[] IntelFilterSpecs =
        {
            ("all",  "全部動態"),
            ("plot", "⚠ 密謀警訊"),
            ("move", "◆ 部隊與俄情"),
            ("subv", "◈ 敵後與電偵")
        };

        private void BuildIntelPageOverlay()
        {
            _intelPageOverlay = new WarRoomPanel
            {
                Visible = false,
                BackColor = Color.FromArgb(250, 27, 24, 19),
                FrameBorderColor = GoldCol,
                InnerBorderColor = Color.FromArgb(78, 66, 48),
                Chamfer = 14
            };

            var lblTitle = new Label
            {
                Text = "◎ 國防部軍事情報處",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 12.6f, FontStyle.Bold),
                Location = new Point(22, 14),
                AutoSize = true
            };

            _intelLevelBadgeLbl = new Label
            {
                Text = "情蒐網：Lv.0 基礎海岸哨戒網",
                ForeColor = Color.FromArgb(228, 214, 174),
                BackColor = Color.FromArgb(46, 41, 32),
                Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold),
                Location = new Point(220, 15),
                Size = new Size(348, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _intelPageSubLbl = new Label
            {
                ForeColor = TextSub,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.8f),
                Location = new Point(580, 15),
                Size = new Size(420, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _intelTechLinkBtn = CreateWarButton("⚙ 情蒐科技總覽 →", Color.FromArgb(44, 56, 48), Color.FromArgb(196, 224, 194), 160, 30, GoodGreen);
            _intelTechLinkBtn.Font = new Font("Microsoft JhengHei UI", 8.6f, FontStyle.Bold);
            _intelTechLinkBtn.Click += (_, _) => OpenTechPage("intel");

            _intelCloseBtn = CreateWarButton("✕ 返回戰圖 (ESC)", Color.FromArgb(52, 46, 36), GoldCol, 140, 30, GoldCol);
            _intelCloseBtn.Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
            _intelCloseBtn.Click += (_, _) => CloseIntelPage();

            _intelPageOverlay.Controls.AddRange(new Control[]
            {
                lblTitle, _intelLevelBadgeLbl, _intelPageSubLbl, _intelTechLinkBtn, _intelCloseBtn
            });

            foreach (var spec in IntelFilterSpecs)
            {
                string kCopy = spec.key;
                var b = CreateWarButton(spec.label, BgCard, TextMain, 190, 32);
                b.Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
                b.Click += (_, _) => SelectIntelFilter(kCopy);
                _intelFilterBtns[spec.key] = b;
                _intelPageOverlay.Controls.Add(b);
            }

            Color[] kpiBorders = { BadRed, GoldCol, Color.FromArgb(106, 174, 156) };
            for (int i = 0; i < 3; i++)
            {
                int idxCopy = i;
                Color accent = kpiBorders[i];
                var card = new BufferedPanel
                {
                    BackColor = BgCard
                };
                card.Paint += (_, pe) =>
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    WarGdi.DrawPlaqueFrame(g, new RectangleF(0.5f, 0.5f, card.Width - 2f, card.Height - 2f),
                        6f, BgCard, BorderCol, Color.FromArgb(50, 44, 34));
                    using var sideBar = new SolidBrush(accent);
                    g.FillRectangle(sideBar, 2f, 8f, 3.5f, card.Height - 16f);

                    if (State != null)
                    {
                        float pct = idxCopy == 0 ? (float)Math.Clamp(SimEngine.ThreatIndex(State) / 2.2, 0.06, 1.0)
                                  : idxCopy == 1 ? (float)Math.Clamp(State.PrcStab / 100.0, 0.04, 1.0)
                                  : (float)Math.Clamp(State.PrcSovTension, 0.04, 1.0);
                        var gaugeR = new RectangleF(14, card.Height - 16, card.Width - 28, 7);
                        using var gp = WarGdi.CreateRoundedPath(gaugeR, 3f);
                        using var gBg = new SolidBrush(BgDark);
                        g.FillPath(gBg, gp);
                        g.SetClip(gp);
                        using var gFg = new SolidBrush(accent);
                        g.FillRectangle(gFg, gaugeR.X, gaugeR.Y, gaugeR.Width * pct, gaugeR.Height);
                        g.ResetClip();
                    }
                };
                var kTitle = new Label
                {
                    ForeColor = accent,
                    BackColor = Color.Transparent,
                    Font = new Font("Microsoft JhengHei UI", 9.6f, FontStyle.Bold),
                    Location = new Point(14, 10),
                    Size = new Size(360, 22)
                };
                var kBody = new Label
                {
                    ForeColor = TextMain,
                    BackColor = Color.Transparent,
                    Font = new Font("Microsoft JhengHei UI", 8.8f),
                    Location = new Point(14, 36),
                    Size = new Size(360, 56)
                };
                card.Controls.AddRange(new Control[] { kTitle, kBody });
                _intelKpiCards.Add(card);
                _intelKpiTitles.Add(kTitle);
                _intelKpiBodies.Add(kBody);
                _intelPageOverlay.Controls.Add(card);
            }

            _intelFeedScroll = new DarkScrollContainer
            {
                BackColor = Color.FromArgb(23, 21, 17),
                ShowVerticalBar = true,
                ShowHorizontalBar = false,
                TrackBorderColor = BorderCol
            };
            _intelFeedCanvas = new BufferedPanel
            {
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(23, 21, 17)
            };
            _intelFeedCanvas.Paint += IntelFeedCanvas_Paint;
            _intelFeedScroll.SetContent(_intelFeedCanvas);
            _intelPageOverlay.Controls.Add(_intelFeedScroll);
        }

        private void LayoutIntelPageOverlay()
        {
            if (_intelPageOverlay == null || _mapPanel == null || _mapPanel.Width <= 0 || _mapPanel.Height <= 0) return;

            // 全版面雙欄情報辦公桌：左欄「分類篩選＋三項戰區量化儀表簡報」，右欄「即時情蒐卷宗動態」
            int w = Math.Max(680, _mapPanel.Width);
            int h = Math.Max(460, _mapPanel.Height);
            _intelPageOverlay.Size = new Size(w, h);
            _intelPageOverlay.Location = new Point(0, 0);

            _intelCloseBtn.Location = new Point(w - _intelCloseBtn.Width - 20, 12);
            _intelTechLinkBtn.Location = new Point(_intelCloseBtn.Left - _intelTechLinkBtn.Width - 10, 12);
            _intelPageSubLbl.Width = Math.Max(140, _intelTechLinkBtn.Left - _intelPageSubLbl.Left - 10);

            int leftColW = Math.Clamp((int)(w * 0.285), 340, 420);
            int leftX = 20;

            int btnW = (leftColW - 10) / 2;
            for (int fi = 0; fi < IntelFilterSpecs.Length; fi++)
            {
                if (_intelFilterBtns.TryGetValue(IntelFilterSpecs[fi].key, out var b))
                {
                    int col = fi % 2;
                    int row = fi / 2;
                    b.Location = new Point(leftX + col * (btnW + 10), 54 + row * 38);
                    b.Size = new Size(btnW, 32);
                }
            }

            int kpiTop = 136;
            int kpiGap = 12;
            int availKpiH = Math.Max(310, h - kpiTop - 20);
            int kpiH = Math.Clamp((availKpiH - kpiGap * 2) / 3, 98, 152);
            for (int i = 0; i < _intelKpiCards.Count; i++)
            {
                var card = _intelKpiCards[i];
                card.Location = new Point(leftX, kpiTop + i * (kpiH + kpiGap));
                card.Size = new Size(leftColW, kpiH);
                _intelKpiTitles[i].Size = new Size(Math.Max(160, leftColW - 26), 22);
                _intelKpiBodies[i].Size = new Size(Math.Max(160, leftColW - 26), Math.Max(44, kpiH - 60));
                card.Invalidate();
            }

            int rightX = leftX + leftColW + 16;
            _intelFeedScroll.Location = new Point(rightX, 54);
            _intelFeedScroll.Size = new Size(Math.Max(300, w - rightX - 20), Math.Max(260, h - 72));

            if (_intelPageOpen)
                UpdateIntelCanvasSize();
        }

        public void OpenIntelPage()
        {
            if (_showTitle || State == null || State.Over != null || State.PendingEvents.Count > 0) return;
            if (_techPageOpen) CloseTechPage();
            if (_activeDrawer != "none") CloseDrawer();
            _nodeCard.Visible = false;
            _zoneCard.Visible = false;
            _hudTooltipCard.Visible = false;

            if (State.IntelReports == null) State.IntelReports = new();
            if (State.IntelReports.Count == 0) SimEngine.SampleIntelReports(State, isOpening: true);

            _intelPageOpen = true;
            SyncSimTimer();
            _intelPageOverlay.Visible = true;
            LayoutIntelPageOverlay();
            _intelPageOverlay.BringToFront();
            RefreshIntelPage();
            UpdateSpeedButtonsUi();
            SyncSimTimer();
            _hudPanel?.Invalidate();
            RefreshDockButtons();
        }

        public void CloseIntelPage()
        {
            if (!_intelPageOpen) return;
            _intelPageOpen = false;
            if (_intelPageOverlay != null) _intelPageOverlay.Visible = false;
            UpdateOverlays();
            UpdateSpeedButtonsUi();
            SyncSimTimer();
            _hudPanel?.Invalidate();
            RefreshDockButtons();
        }

        public void ToggleIntelPage()
        {
            if (_intelPageOpen) CloseIntelPage();
            else OpenIntelPage();
        }

        public void SelectIntelFilter(string filterKey)
        {
            if (!IntelFilterSpecs.Any(x => x.key == filterKey)) return;
            _intelFilter = filterKey;
            _intelFeedScroll?.ScrollTo(0, 0);
            RefreshIntelPage();
        }

        private List<IntelReport> GetFilteredIntelReports()
        {
            var src = State?.IntelReports;
            if (src == null || src.Count == 0) return new List<IntelReport>();
            IEnumerable<IntelReport> q = src;
            if (_intelFilter == "plot") q = q.Where(r => r.Cat == "密謀警示");
            else if (_intelFilter == "move") q = q.Where(r => r.Cat == "部隊調動" || r.Cat == "蘇俄動態");
            else if (_intelFilter == "subv") q = q.Where(r => r.Cat == "敵後線索" || r.Cat == "電偵動態");
            return q.ToList();
        }

        private void UpdateIntelCanvasSize()
        {
            if (_intelFeedScroll == null || _intelFeedCanvas == null) return;
            int targetW = Math.Max(360, _intelFeedScroll.ViewportWidth);
            var list = GetFilteredIntelReports();
            int y = 12;
            if (list.Count == 0)
            {
                y = 140;
            }
            else
            {
                int cw = Math.Max(300, targetW - 20);
                int textW = Math.Max(220, cw - 40);
                using var g = _intelFeedCanvas.CreateGraphics();
                using var bodyF = new Font("Microsoft JhengHei UI", 10.2f, FontStyle.Regular);
                foreach (var rep in list)
                {
                    var sz = g.MeasureString(rep.Text ?? "", bodyF, textW);
                    int cardH = Math.Max(78, 38 + (int)Math.Ceiling(sz.Height) + 14);
                    y += cardH + 10;
                }
            }
            int totalH = Math.Max(_intelFeedScroll.ViewportHeight, y + 10);
            _intelFeedCanvas.Size = new Size(targetW, totalH);
            _lastIntelCanvasWidth = targetW;
            _intelFeedScroll.SyncScroll();
        }

        private void RefreshIntelPage()
        {
            if (!_intelPageOpen || _intelPageOverlay == null || State == null) return;

            int intelLv = SimEngine.IntelLevel(State);
            double fog = SimEngine.FogError(State);
            string fogText = fog > 0 ? $"兵力誤差 ±{fog * 100:F0}%" : "精確掌握 (誤差 0%)";
            _intelLevelBadgeLbl.Text = $"情蒐網：{SimEngine.IntelLevelName(State)}（{fogText}）";

            var list = GetFilteredIntelReports();
            int totalCnt = State.IntelReports?.Count ?? 0;
            _intelPageSubLbl.Text = $"▌▌ 時間已自動暫停 · 共 {totalCnt} 則卷宗（顯示 {list.Count} 則）· 按 I 或 ESC 返回";

            foreach (var spec in IntelFilterSpecs)
            {
                if (_intelFilterBtns.TryGetValue(spec.key, out var b))
                {
                    bool active = spec.key == _intelFilter;
                    SetWarButtonStyle(
                        b,
                        active ? Color.FromArgb(66, 56, 36) : BgCard,
                        active ? GoldCol : TextSub,
                        active ? GoldCol : BorderCol);
                }
            }

            if (_intelKpiTitles.Count >= 3)
            {
                double threat = SimEngine.ThreatIndex(State);
                string threatLv = threat >= 1.35 ? "高度緊繃" : threat >= 1.05 ? "警戒升高" : "正常巡弋";
                var rpt = State.LastReport;
                string moveHint = "東南沿海電台訊號定期監測中";
                if (rpt != null && rpt.PrcMoves != null && rpt.PrcMoves.Count > 0)
                {
                    var mv = rpt.PrcMoves[^1];
                    string fn = GameData.Nodes.TryGetValue(mv.From ?? "", out var n1) ? n1.Name : mv.From;
                    string tn = GameData.Nodes.TryGetValue(mv.To ?? "", out var n2) ? n2.Name : mv.To;
                    moveHint = mv.Landing
                        ? $"警示：偵獲自【{fn}】企圖渡海進犯【{tn}】動向"
                        : $"偵獲：共軍自【{fn}】朝【{tn}】機動調整兵力";
                }
                if (moveHint.Length > 28) moveHint = moveHint.Substring(0, 28) + "...";

                _intelKpiTitles[0].Text = $"台海警戒：{threatLv}（{threat:F2}）";
                _intelKpiBodies[0].Text = $"{moveHint}\n{(State.HasFlag("kinmen_shell") ? "外島砲戰摩擦風險仍在高檔" : "前線外島防線持續保持戒備")}";

                int unrestCnt = State.Unrest?.Values.Count(v => v > 0) ?? 0;
                _intelKpiTitles[1].Text = $"大陸內部：穩定 {State.PrcStab:F0}% · 動盪 {unrestCnt} 省";
                _intelKpiBodies[1].Text = (State.HasFlag("subv_hk")
                    ? "港澳地下情報網與華南交通線運作中"
                    : "敵後無線電網尚在建置，持續接收零星線報")
                    + "\n" + SimEngine.DescribePrcDoctrineIntel(State);

                int ussrCnt = SimEngine.UssrNodes(State).Count;
                _intelKpiTitles[2].Text = $"北疆俄情：緊張 {State.PrcSovTension * 100:F0}% · 俄占 {ussrCnt} 省";
                _intelKpiBodies[2].Text = $"情報等級 Lv.{intelLv}/5 · {fogText}\n升級情蒐科技可截獲更深層加密密電";

                foreach (var c in _intelKpiCards) c.Invalidate();
            }

            int targetW = Math.Max(360, _intelFeedScroll.ViewportWidth);
            if (_lastIntelRenderedCount != totalCnt || _lastIntelRenderedFilter != _intelFilter || _lastIntelCanvasWidth != targetW)
            {
                UpdateIntelCanvasSize();
                _lastIntelRenderedCount = totalCnt;
                _lastIntelRenderedFilter = _intelFilter;
            }
            _intelFeedCanvas.Invalidate();
        }

        private void IntelFeedCanvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.FromArgb(23, 21, 17));

            if (State == null) return;
            var list = GetFilteredIntelReports();
            if (list.Count == 0)
            {
                using var emptyF = new Font("Microsoft JhengHei UI", 10.5f, FontStyle.Bold);
                using var emptyBr = new SolidBrush(TextSub);
                g.DrawString("目前此分類尚無情蒐動態紀錄，時間推進後將持續匯報。", emptyF, emptyBr, 24, 34);
                return;
            }

            int cw = Math.Max(300, _intelFeedCanvas.Width - 20);
            int textW = Math.Max(220, cw - 40);
            int y = 12;

            using var dateF = new Font("Consolas", 9.4f, FontStyle.Bold);
            using var catF = new Font("Microsoft JhengHei UI", 8.6f, FontStyle.Bold);
            using var bodyF = new Font("Microsoft JhengHei UI", 10.2f, FontStyle.Regular);
            using var textBr = new SolidBrush(TextMain);
            using var dateBr = new SolidBrush(GoldCol);

            foreach (var rep in list)
            {
                var sz = g.MeasureString(rep.Text ?? "", bodyF, textW);
                int cardH = Math.Max(78, 38 + (int)Math.Ceiling(sz.Height) + 14);
                var rect = new Rectangle(10, y, cw, cardH);

                (Color accent, Color cardBg, Color borderCol, Color pillBg, string sevLabel) = rep.Level switch
                {
                    "alert" => (BadRed, Color.FromArgb(44, 30, 28), Color.FromArgb(132, 68, 62), Color.FromArgb(72, 32, 30), "高度警戒"),
                    "warn"  => (GoldCol, Color.FromArgb(42, 37, 26), Color.FromArgb(128, 102, 54), Color.FromArgb(64, 52, 26), "異常徵候"),
                    "good"  => (GoodGreen, Color.FromArgb(30, 42, 32), Color.FromArgb(74, 118, 82), Color.FromArgb(32, 62, 38), "我方線報"),
                    _       => (Color.FromArgb(118, 178, 188), Color.FromArgb(34, 32, 28), BorderCol, Color.FromArgb(44, 48, 46), "電偵紀錄")
                };

                WarGdi.DrawPlaqueFrame(g, rect, 5.5f, cardBg, borderCol, Color.FromArgb(52, borderCol));
                using (var barBr = new SolidBrush(accent))
                    g.FillRectangle(barBr, rect.X + 2, rect.Y + 6, 4, rect.Height - 12);

                string dateStr = string.IsNullOrEmpty(rep.Date) ? $"{State.Year}/{State.Month:D2}/{State.Day:D2}" : rep.Date;
                g.DrawString(dateStr, dateF, dateBr, rect.X + 16, rect.Y + 10);

                string catStr = $"【{rep.Cat ?? "情蒐"}】";
                var catSz = g.MeasureString(catStr, catF);
                int pillX = rect.X + 114;
                int pillW = (int)Math.Ceiling(catSz.Width) + 12;
                var pillRect = new Rectangle(pillX, rect.Y + 8, pillW, 20);
                using (var pillPath = WarGdi.CreateRoundedPath(pillRect, 3.5f))
                using (var pillBr = new SolidBrush(pillBg))
                    g.FillPath(pillBr, pillPath);
                using (var catBr = new SolidBrush(accent))
                    g.DrawString(catStr, catF, catBr, pillX + 6, rect.Y + 10);

                var sevRect = new RectangleF(rect.Right - 98, rect.Y + 10, 84, 18);
                using (var sevBr = new SolidBrush(Color.FromArgb(190, accent.R, accent.G, accent.B)))
                using (var sfRight = new StringFormat { Alignment = StringAlignment.Far })
                    g.DrawString(sevLabel, catF, sevBr, sevRect, sfRight);

                var bodyRect = new RectangleF(rect.X + 16, rect.Y + 36, textW, cardH - 44);
                g.DrawString(rep.Text ?? "", bodyF, textBr, bodyRect);

                y += cardH + 10;
            }
        }

        // ==================== 自適應戰略圖卡式事件與結局彈窗 ====================
        private void CenterEventOverlay()
        {
            if (!_eventOverlay.Visible) return;
            int chCount = (State.Over == null && State.PendingEvents.Count > 0) ? State.PendingEvents[0].Choices.Count : 0;
            int targetW = chCount >= 3 ? 980 : chCount == 2 ? 840 : 730;
            int targetH = chCount >= 3 ? 642 : chCount == 2 ? 630 : chCount == 1 ? 490 : 510;
            int w = Math.Min(targetW, _mapPanel.Width - 32);
            int h = Math.Min(targetH, _mapPanel.Height - 24);
            _eventOverlay.Size = new Size(Math.Max(560, w), Math.Max(400, h));
            _eventOverlay.Location = new Point((_mapPanel.Width - _eventOverlay.Width) / 2, (_mapPanel.Height - _eventOverlay.Height) / 2);
        }

        private void UpdateOverlays()
        {
            _titleOverlay.Visible = _showTitle;
            if (_showTitle)
            {
                _techPageOpen = false;
                _intelPageOpen = false;
                if (_techOverlay != null) _techOverlay.Visible = false;
                if (_intelPageOverlay != null) _intelPageOverlay.Visible = false;
                _hudPanel.Visible = false;
                _dockBar.Visible = false;
                _mapPanel.Visible = false;
                RefreshTitleOverlay();
                _titleOverlay.BringToFront();
                return;
            }
            _hudPanel.Visible = true;
            _dockBar.Visible = true;
            _mapPanel.Visible = true;
            Controls.SetChildIndex(_titleOverlay, 0);
            Controls.SetChildIndex(_mapPanel, 1);
            Controls.SetChildIndex(_dockBar, 2);
            Controls.SetChildIndex(_hudPanel, 3);
            PerformLayout();

            if (State.Over != null)
            {
                if (_techPageOpen) CloseTechPage();
                if (_intelPageOpen) CloseIntelPage();
                if (_activeDrawer != "none") CloseDrawer();
                _nodeCard.Visible = false;
                _zoneCard.Visible = false;
                _hudTooltipCard.Visible = false;
                RecordEnding(State.Over.Key);
                ShowEndingOverlay(State.Over);
            }
            else if (State.PendingEvents.Count > 0 && !_intelPageOpen)
            {
                if (_activeDrawer != "none") CloseDrawer();
                _nodeCard.Visible = false;
                _zoneCard.Visible = false;
                _hudTooltipCard.Visible = false;
                ShowEventOverlay(State.PendingEvents[0]);
            }
            else
            {
                _eventOverlay.Visible = false;
            }
        }

        private static (string badge, Color col, string icon) InferChoiceVisual(EventChoice ch, int idx, int total, bool easyMode)
        {
            string lbl = ch.Label ?? "";
            string hint = ch.Hint ?? "";
            string combined = lbl + " " + hint;

            string icon = "star";
            if (combined.Contains("登陸") || combined.Contains("海軍") || combined.Contains("艦隊") || combined.Contains("兩棲") || combined.Contains("舟山"))
                icon = "anchor";
            else if (combined.Contains("空軍") || combined.Contains("戰機") || combined.Contains("飛彈") || combined.Contains("轟炸") || combined.Contains("米格") || combined.Contains("空投"))
                icon = "wing";
            else if (combined.Contains("突擊") || combined.Contains("進軍") || combined.Contains("決戰") || combined.Contains("驅逐") || combined.Contains("壓境"))
                icon = "sword";
            else if (combined.Contains("情報") || combined.Contains("秘密") || combined.Contains("策反") || combined.Contains("游擊"))
                icon = "spy";
            else if (combined.Contains("撤退") || combined.Contains("防務") || combined.Contains("條約") || combined.Contains("協防") || combined.Contains("工事"))
                icon = "shield";

            if (!easyMode)
            {
                // 一般模式：完全中性一致的標示與邊框色，不顯示「主力方案／替代方針／高風險」等誘導字樣
                string neutralBadge = total == 1 ? "◇ 統帥電令" : "◇ 決策方針";
                return (neutralBadge, Color.FromArgb(136, 112, 68), icon);
            }

            string tag = ch.StyleTag;
            if (string.IsNullOrEmpty(tag))
            {
                if (combined.Contains("警訊") || combined.Contains("高風險") || combined.Contains("嚴懲") || combined.Contains("承認北疆") || combined.Contains("決戰") || combined.Contains("拒絕"))
                    tag = "red";
                else if (combined.Contains("情報") || combined.Contains("秘密") || combined.Contains("蘇聯") || combined.Contains("起義") || combined.Contains("投誠") || combined.Contains("游擊"))
                    tag = "purple";
                else if (combined.Contains("經濟") || combined.Contains("建軍") || combined.Contains("撤退") || combined.Contains("退出") || combined.Contains("整軍"))
                    tag = "blue";
                else if (combined.Contains("反攻") || combined.Contains("默許") || combined.Contains("動員") || combined.Contains("突擊"))
                    tag = "gold";
                else
                    tag = idx == 0 ? "blue" : idx == 1 ? "gold" : "purple";
            }

            Color col = tag switch
            {
                "red" => Color.FromArgb(172, 52, 58),
                "gold" => Color.FromArgb(182, 134, 42),
                "purple" => Color.FromArgb(124, 62, 152),
                "green" => Color.FromArgb(42, 138, 92),
                _ => Color.FromArgb(40, 102, 178)
            };

            string badge = ch.Stance;
            if (string.IsNullOrEmpty(badge))
            {
                if (total == 1) badge = "⚡ 緊急處置指令";
                else if (lbl.Contains("引狼入室") || lbl.Contains("高風險")) badge = "⚠ 高風險決策";
                else if (combined.Contains("情報") || combined.Contains("策動") || combined.Contains("秘密")) badge = "◆ 情報秘密線";
                else badge = idx == 0 ? "★ 主力方案" : idx == 1 ? "◆ 替代方針" : "▲ 非常路線";
            }

            return (badge, col, icon);
        }

        private static void DrawCardEmblem(Graphics g, RectangleF r, string iconKind, Color col)
        {
            float cx = r.X + r.Width * 0.5f;
            float cy = r.Y + r.Height * 0.5f;
            float rad = Math.Min(r.Width, r.Height) * 0.42f;

            using var bgBr = new SolidBrush(Color.FromArgb(32, col));
            using var ringPen = new Pen(Color.FromArgb(145, col), 1.4f);
            using var drawPen = new Pen(Color.FromArgb(235, Math.Min(255, col.R + 65), Math.Min(255, col.G + 65), Math.Min(255, col.B + 65)), 2.0f);

            g.FillEllipse(bgBr, cx - rad, cy - rad, rad * 2, rad * 2);
            g.DrawEllipse(ringPen, cx - rad, cy - rad, rad * 2, rad * 2);

            if (iconKind == "shield")
            {
                var pts = new PointF[]
                {
                    new(cx - rad * 0.52f, cy - rad * 0.48f),
                    new(cx + rad * 0.52f, cy - rad * 0.48f),
                    new(cx + rad * 0.52f, cy + rad * 0.05f),
                    new(cx, cy + rad * 0.62f),
                    new(cx - rad * 0.52f, cy + rad * 0.05f)
                };
                g.DrawPolygon(drawPen, pts);
                g.DrawLine(drawPen, cx, cy - rad * 0.25f, cx, cy + rad * 0.25f);
                g.DrawLine(drawPen, cx - rad * 0.25f, cy, cx + rad * 0.25f, cy);
            }
            else if (iconKind == "sword")
            {
                g.DrawLine(drawPen, cx - rad * 0.5f, cy + rad * 0.45f, cx + rad * 0.5f, cy - rad * 0.45f);
                g.DrawLine(drawPen, cx + rad * 0.5f, cy + rad * 0.45f, cx - rad * 0.5f, cy - rad * 0.45f);
                g.DrawLine(drawPen, cx + rad * 0.15f, cy - rad * 0.45f, cx + rad * 0.5f, cy - rad * 0.45f);
                g.DrawLine(drawPen, cx + rad * 0.5f, cy - rad * 0.45f, cx + rad * 0.5f, cy - rad * 0.1f);
            }
            else if (iconKind == "anchor")
            {
                g.DrawEllipse(drawPen, cx - rad * 0.16f, cy - rad * 0.56f, rad * 0.32f, rad * 0.32f);
                g.DrawLine(drawPen, cx, cy - rad * 0.24f, cx, cy + rad * 0.52f);
                g.DrawLine(drawPen, cx - rad * 0.35f, cy - rad * 0.08f, cx + rad * 0.35f, cy - rad * 0.08f);
                g.DrawArc(drawPen, cx - rad * 0.48f, cy - rad * 0.05f, rad * 0.96f, rad * 0.58f, 15, 150);
            }
            else if (iconKind == "wing")
            {
                var wPts = new PointF[]
                {
                    new(cx, cy - rad * 0.55f),
                    new(cx + rad * 0.62f, cy + rad * 0.22f),
                    new(cx + rad * 0.18f, cy + rad * 0.12f),
                    new(cx, cy + rad * 0.48f),
                    new(cx - rad * 0.18f, cy + rad * 0.12f),
                    new(cx - rad * 0.62f, cy + rad * 0.22f)
                };
                g.DrawPolygon(drawPen, wPts);
            }
            else if (iconKind == "spy")
            {
                g.DrawEllipse(drawPen, cx - rad * 0.48f, cy - rad * 0.48f, rad * 0.96f, rad * 0.96f);
                g.DrawLine(drawPen, cx - rad * 0.65f, cy, cx + rad * 0.65f, cy);
                g.DrawLine(drawPen, cx, cy - rad * 0.65f, cx, cy + rad * 0.65f);
            }
            else
            {
                g.DrawPolygon(drawPen, new PointF[]
                {
                    new(cx, cy - rad * 0.58f),
                    new(cx + rad * 0.18f, cy - rad * 0.16f),
                    new(cx + rad * 0.60f, cy),
                    new(cx + rad * 0.18f, cy + rad * 0.16f),
                    new(cx, cy + rad * 0.58f),
                    new(cx - rad * 0.18f, cy + rad * 0.16f),
                    new(cx - rad * 0.60f, cy),
                    new(cx - rad * 0.18f, cy - rad * 0.16f)
                });
            }
        }

        private static List<string> PickEventArtKeys(GameEvent ev)
        {
            var fallbackOrder = new[] { "diplomacy", "defense", "naval_air", "amphibious", "economy", "uprising", "underground", "ussr_crisis" };
            var used = new HashSet<string>();
            var result = new List<string>();

            foreach (var ch in ev.Choices)
            {
                string txt = $"{ch.Label} {ch.Hint}";
                string evCtx = $"{ev.Id} {ev.Title} {ev.Desc}";
                var candidates = new List<string>();

                if (txt.Contains("蘇聯") || txt.Contains("俄軍") || txt.Contains("俄占") || txt.Contains("北疆") || txt.Contains("俄羅斯"))
                    candidates.Add("ussr_crisis");
                if (txt.Contains("條約") || txt.Contains("台灣關係法") || txt.Contains("聯合國") || txt.Contains("雙重代表權") || txt.Contains("華府") || txt.Contains("遊說") || txt.Contains("照會") || txt.Contains("停戰"))
                    candidates.Add("diplomacy");
                if (txt.Contains("空投") || txt.Contains("地下") || txt.Contains("游擊") || txt.Contains("滲透") || txt.Contains("民兵"))
                    candidates.Add("underground");
                if (txt.Contains("搶灘") || txt.Contains("兩棲") || txt.Contains("登陸") || txt.Contains("舟山") || txt.Contains("東山"))
                    candidates.Add("amphibious");
                if (txt.Contains("空軍") || txt.Contains("米格") || txt.Contains("轟炸") || txt.Contains("戰機") || txt.Contains("艦隊") || txt.Contains("航母") || txt.Contains("飛彈") || txt.Contains("羅布泊") || txt.Contains("防空"))
                    candidates.Add("naval_air");
                if (txt.Contains("起義") || txt.Contains("投誠") || txt.Contains("反攻") || txt.Contains("北定") || txt.Contains("揮師") || txt.Contains("壓境"))
                    candidates.Add("uprising");
                if (txt.Contains("撤退") || txt.Contains("坑道") || txt.Contains("死守") || txt.Contains("堅守") || txt.Contains("退伍") || txt.Contains("戒嚴"))
                    candidates.Add("defense");
                if (txt.Contains("經濟") || txt.Contains("工業") || txt.Contains("科學園區") || txt.Contains("文化復興") || txt.Contains("自強") || txt.Contains("研發") || txt.Contains("外資"))
                    candidates.Add("economy");

                if (evCtx.Contains("蘇") || evCtx.Contains("俄")) candidates.Add("ussr_crisis");
                if (evCtx.Contains("外交") || evCtx.Contains("美") || evCtx.Contains("聯合國")) candidates.Add("diplomacy");
                foreach (var f in fallbackOrder) candidates.Add(f);

                string picked = candidates.FirstOrDefault(k => !used.Contains(k)) ?? candidates[0];
                used.Add(picked);
                result.Add(picked);
            }
            return result;
        }

        private void ShowEventOverlay(GameEvent ev)
        {
            _eventOverlay.SuspendLayout();
            _eventOverlay.Controls.Clear();
            _eventOverlay.Visible = true;
            CenterEventOverlay();
            _eventOverlay.BringToFront();

            int w = _eventOverlay.Width - 44;
            int y = 14;

            bool isForcedNotice = ev.Choices.Count == 1;
            var lblStamp = new Label
            {
                Text = isForcedNotice ? "最高統帥部・不可抗變局急電" : "最高統帥部・機密決策急電",
                ForeColor = TextMain,
                BackColor = Color.FromArgb(132, 48, 44),
                Font = new Font("Microsoft JhengHei UI", 8.5f, FontStyle.Bold),
                Location = new Point(22, y),
                Size = new Size(192, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblTag = new Label
            {
                Text = $"【{ev.Year} 年 {ev.Month:D2} 月 {ev.Day:D2} 日 · {ev.Tag}電文】",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 9.8f, FontStyle.Bold),
                Location = new Point(222, y + 1),
                AutoSize = true
            };
            y += 28;

            var lblTitle = new Label
            {
                Text = ev.Title,
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 15.0f, FontStyle.Bold),
                Location = new Point(22, y),
                Size = new Size(w, 30)
            };
            y += 34;

            var lblDesc = new Label
            {
                Text = ev.Desc,
                ForeColor = TextMain,
                BackColor = Color.FromArgb(39, 34, 28),
                Font = new Font("Microsoft JhengHei UI", 9.4f),
                Location = new Point(22, y),
                Size = new Size(w, 80),
                Padding = new Padding(12, 7, 12, 7)
            };
            y += 90;

            _eventOverlay.Controls.AddRange(new Control[] { lblStamp, lblTag, lblTitle, lblDesc });

            var artKeys = PickEventArtKeys(ev);
            int cardTop = y;
            int deckH = Math.Max(240, _eventOverlay.Height - cardTop - 16);
            int count = ev.Choices.Count;

            if (count == 1)
            {
                // 不可抗外交衝擊單卡：左大幅插畫 ＋ 右側下方說明式寬版軍令卡
                int cw = Math.Min(680, w);
                int cx = 22 + (w - cw) / 2;
                int chH = Math.Min(272, deckH);
                var card = BuildChoiceCardControl(ev.Choices[0], 0, 1, artKeys[0], new Rectangle(cx, cardTop + Math.Max(0, (deckH - chH) / 2), cw, chH));
                _eventOverlay.Controls.Add(card);
            }
            else if (count == 2)
            {
                // 二元抉擇雙卡對決：上大幅插畫 ＋ 下方說明區（中附 VS 分隔徽章）
                int gap = 48;
                int cw = Math.Min(368, (w - gap) / 2);
                int startX = 22 + (w - (cw * 2 + gap)) / 2;
                int chH = Math.Min(428, deckH);

                var c0 = BuildChoiceCardControl(ev.Choices[0], 0, 2, artKeys[0], new Rectangle(startX, cardTop, cw, chH));
                var c1 = BuildChoiceCardControl(ev.Choices[1], 1, 2, artKeys[1], new Rectangle(startX + cw + gap, cardTop, cw, chH));

                var vsBadge = new BufferedPanel
                {
                    Location = new Point(startX + cw + 4, cardTop),
                    Size = new Size(gap - 8, chH),
                    BackColor = Color.Transparent
                };
                vsBadge.Paint += (_, pe) =>
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float mx = vsBadge.Width * 0.5f;
                    float my = vsBadge.Height * 0.45f;
                    using var linePen = new Pen(GoldDim, 1.2f) { DashStyle = DashStyle.Dash };
                    g.DrawLine(linePen, mx, 16, mx, my - 22);
                    g.DrawLine(linePen, mx, my + 22, mx, vsBadge.Height - 16);

                    var hex = new RectangleF(mx - 18, my - 18, 36, 36);
                    DrawChamferFrame(g, hex, 8f, BgCardHi, GoldCol, GoldDim);
                    using var vsF = new Font("Consolas", 10.5f, FontStyle.Bold);
                    var sz = g.MeasureString("VS", vsF);
                    g.DrawString("VS", vsF, new SolidBrush(GoldCol), mx - sz.Width * 0.5f, my - sz.Height * 0.5f);
                };

                _eventOverlay.Controls.AddRange(new Control[] { c0, vsBadge, c1 });
            }
            else
            {
                // 重大事件：三選一橫排遊戲圖卡（圖片在上 ＋ 說明在下）
                int showCount = Math.Min(3, count);
                int gap = 14;
                int cw = (w - gap * (showCount - 1)) / showCount;
                int chH = Math.Min(432, deckH);

                for (int i = 0; i < showCount; i++)
                {
                    var card = BuildChoiceCardControl(ev.Choices[i], i, showCount, artKeys[i], new Rectangle(22 + i * (cw + gap), cardTop, cw, chH));
                    _eventOverlay.Controls.Add(card);
                }
            }

            _eventOverlay.ResumeLayout();
        }

        private Control BuildChoiceCardControl(EventChoice ch, int idx, int total, string artKey, Rectangle bounds)
        {
            bool easyMode = IsEasyMode;
            var (badge, themeCol, iconKind) = InferChoiceVisual(ch, idx, total, easyMode);
            bool isSingle = total == 1;
            bool hovered = false;
            _cardImages.TryGetValue(artKey, out var artImg);

            var card = new BufferedPanel
            {
                Location = bounds.Location,
                Size = bounds.Size,
                BackColor = BgCard,
                Cursor = Cursors.Hand,
                AccessibleDescription = badge
            };

            void TriggerChoice()
            {
                EventSystem.ResolveChoice(State, idx);
                CheckAndRecordCheckpoints();
                UpdateOverlays();
                RefreshAllUi();
            }

            void SetHover(bool h)
            {
                if (hovered == h) return;
                hovered = h;
                card.Invalidate();
            }

            // 圖片視窗與下方說明箱幾何規格
            int artX = 12;
            int artY = 40;
            int artW = isSingle ? 256 : card.Width - 24;
            int artH = isSingle ? card.Height - 92 : (int)Math.Clamp((card.Width - 24) * 0.52f, 126, 162);

            int descBoxX = isSingle ? artX + artW + 14 : 12;
            int descBoxY = isSingle ? artY + 72 : artY + artH + 74;
            int descBoxW = isSingle ? card.Width - descBoxX - 12 : card.Width - 24;
            int descBoxH = Math.Max(46, (card.Height - 46) - descBoxY);

            card.Paint += (_, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Color fill = hovered ? BgCardHi : BgCard;
                Color borderOut = hovered ? GoldCol : Color.FromArgb(185, themeCol);
                Color borderIn = hovered ? Color.FromArgb(145, GoldCol) : Color.FromArgb(62, 54, 42);
                WarGdi.DrawPlaqueFrame(g, new RectangleF(0.5f, 0.5f, card.Width - 2, card.Height - 2), 8.5f, fill, borderOut, borderIn);

                // 1. 頂部卡牌標頭（路線立場徽章 + 羅馬數字編號）
                var hdrR = new RectangleF(12, 10, card.Width - 24, 24);
                using (var hdrPath = WarGdi.CreateRoundedPath(hdrR, 4f))
                using (var hdrBr = new SolidBrush(Color.FromArgb(84, themeCol)))
                using (var hdrPen = new Pen(hovered ? GoldCol : Color.FromArgb(175, themeCol), 1.1f))
                using (var hdrFont = new Font("Microsoft JhengHei UI", 8.5f, FontStyle.Bold))
                using (var hdrTxtBr = new SolidBrush(TextMain))
                {
                    g.FillPath(hdrBr, hdrPath);
                    g.DrawPath(hdrPen, hdrPath);
                    g.DrawString(badge, hdrFont, hdrTxtBr, hdrR.X + 8, hdrR.Y + 4);

                    if (!isSingle)
                    {
                        string roman = idx == 0 ? "I" : idx == 1 ? "II" : "III";
                        using var romF = new Font("Consolas", 9.0f, FontStyle.Bold);
                        var rsz = g.MeasureString(roman, romF);
                        g.DrawString(roman, romF, new SolidBrush(GoldCol), hdrR.Right - rsz.Width - 8, hdrR.Y + 4);
                    }
                }

                // 2. 卡牌插畫視窗（遊戲卡片核心視覺區）
                var artR = new RectangleF(artX, artY, artW, artH);
                if (artImg != null)
                {
                    DrawImageCover(g, artImg, artR);
                    var shadeR = new RectangleF(artR.X, artR.Bottom - 26, artR.Width, 26);
                    using var shadeBr = new LinearGradientBrush(shadeR, Color.FromArgb(0, 22, 19, 15), Color.FromArgb(205, 22, 19, 15), LinearGradientMode.Vertical);
                    g.FillRectangle(shadeBr, shadeR);
                }
                else
                {
                    using var fbBr = new SolidBrush(BgPanel);
                    g.FillRectangle(fbBr, artR);
                    DrawCardEmblem(g, new RectangleF(artR.X + (artR.Width - 64) * 0.5f, artR.Y + (artR.Height - 64) * 0.5f, 64, 64), iconKind, themeCol);
                }

                // 右下角嵌徽章 ＋ 圖片雙線黃銅框
                if (artImg != null)
                {
                    var embR = new RectangleF(artR.Right - 34, artR.Bottom - 34, 28, 28);
                    using var embBg = new SolidBrush(Color.FromArgb(220, 24, 21, 17));
                    g.FillEllipse(embBg, embR);
                    DrawCardEmblem(g, embR, iconKind, hovered ? GoldCol : themeCol);
                }
                using (var artPenOut = new Pen(hovered ? GoldCol : GoldDim, hovered ? 1.8f : 1.3f))
                using (var artPenIn = new Pen(Color.FromArgb(140, 22, 20, 16), 1f))
                {
                    g.DrawRectangle(artPenOut, artR.X, artR.Y, artR.Width, artR.Height);
                    g.DrawRectangle(artPenIn, artR.X + 1.5f, artR.Y + 1.5f, artR.Width - 3, artR.Height - 3);
                }

                // 3. 下方文字說明盒（內凹檔案規則效果欄）
                var boxR = new RectangleF(descBoxX, descBoxY, descBoxW, descBoxH);
                using (var boxPath = WarGdi.CreateRoundedPath(boxR, 4f))
                using (var boxBr = new SolidBrush(Color.FromArgb(215, 24, 21, 17)))
                using (var boxPen = new Pen(hovered ? GoldDim : BorderCol, 1f))
                {
                    g.FillPath(boxBr, boxPath);
                    g.DrawPath(boxPen, boxPath);
                }

                // 4. 最底端觸發按鈕條
                var actR = new RectangleF(12, card.Height - 38, card.Width - 24, 28);
                WarGdi.DrawPlaqueFrame(g, actR, 5f,
                    hovered ? Color.FromArgb(156, 108, 36) : Color.FromArgb(52, 82, 108),
                    hovered ? GoldCol : themeCol,
                    Color.FromArgb(62, 54, 42));
                using var actF = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
                using var actTxtBr = new SolidBrush(TextMain);
                string actTxt = isSingle ? "▶  奉行緊急應變指令" : $"▶  採行此張方針卡";
                var asz = g.MeasureString(actTxt, actF);
                g.DrawString(actTxt, actF, actTxtBr, actR.X + (actR.Width - asz.Width) * 0.5f, actR.Y + 6);
            };

            // 卡牌標題（位於圖片正下方 / 單卡位於右側上方）
            int txtX = isSingle ? descBoxX : 12;
            int txtY = isSingle ? artY : artY + artH + 6;
            int txtW = isSingle ? descBoxW : card.Width - 24;
            int titleH = isSingle ? 66 : 64;

            var lblTitle = new Label
            {
                Text = easyMode ? ch.Label : ch.Label.Replace("（高風險）", ""),
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", isSingle ? 10.4f : (total == 2 ? 9.6f : 9.1f), FontStyle.Bold),
                Location = new Point(txtX, txtY),
                Size = new Size(txtW, titleH),
                TextAlign = isSingle ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            // 下方效果說明欄文字
            var lblHint = new Label
            {
                Text = ch.Hint,
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", isSingle ? 8.8f : 8.1f),
                Location = new Point(descBoxX + 6, descBoxY + 4),
                Size = new Size(descBoxW - 12, descBoxH - 8),
                TextAlign = isSingle ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };

            void AttachEvents(Control c)
            {
                c.Click += (_, _) => TriggerChoice();
                c.MouseEnter += (_, _) => SetHover(true);
                c.MouseLeave += (_, _) =>
                {
                    if (!card.ClientRectangle.Contains(card.PointToClient(Cursor.Position)))
                        SetHover(false);
                };
            }

            card.Controls.AddRange(new Control[] { lblTitle, lblHint });
            AttachEvents(card);
            AttachEvents(lblTitle);
            AttachEvents(lblHint);

            return card;
        }

        private void ShowEndingOverlay(GameOverInfo over)
        {
            _eventOverlay.SuspendLayout();
            _eventOverlay.Controls.Clear();
            _eventOverlay.Visible = true;
            _eventOverlay.Width = Math.Min(1040, _mapPanel.Width - 32);
            CenterEventOverlay();
            _eventOverlay.Width = Math.Min(1040, _mapPanel.Width - 32);
            _eventOverlay.Location = new Point((_mapPanel.Width - _eventOverlay.Width) / 2, _eventOverlay.Location.Y);
            _eventOverlay.BringToFront();

            int w = _eventOverlay.Width - 40;
            int y = 20;

            var lblGroup = new Label
            {
                Text = $"【歷史終局 · {over.Group}】 （{State.Year} 年 {State.Month:D2} 月 {State.Day:D2} 日結算 · 綜合評分 {over.Score} 分）",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 11f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true
            };
            y += 32;

            var lblTitle = new Label
            {
                Text = over.Title,
                ForeColor = over.Kind == "win" ? GoodGreen : over.Kind == "lose" ? BadRed : GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 18f, FontStyle.Bold),
                Location = new Point(20, y),
                Size = new Size(w, 40)
            };
            y += 46;

            string summaryText = over.Text + "\n\n" +
                       $"• 光復大陸省分：{SimEngine.HeldMainland(State).Count} / {GameData.MainlandIds.Length} 省\n" +
                       $"• 北疆俄占省分：{SimEngine.UssrNodes(State).Count} 省\n" +
                       $"• 台灣 GDP：${State.Gdp:F0} 億  |  累計勝仗：{State.BattlesWon} 場\n" +
                       $"• 國軍總折損：{State.RocLoss:F1} 師  |  敵軍總折損：{State.EnemyLoss:F1} 師";

            var memLines = new List<string> { "◈ 統帥回憶錄 · 關鍵決策軌跡" };
            var topMem = (State.Memoir ?? new List<MemoirEntry>())
                .OrderByDescending(m => m.Weight)
                .ThenBy(m => m.Date ?? "", StringComparer.Ordinal)
                .Take(8)
                .OrderBy(m => m.Date ?? "", StringComparer.Ordinal)
                .ThenBy(m => m.Title ?? "")
                .ToList();
            if (topMem.Count == 0)
                memLines.Add("• 本局未留特別戰略決策紀錄。");
            else
                foreach (var me in topMem)
                {
                    string ym = !string.IsNullOrEmpty(me.Date) && me.Date.Length >= 7 ? me.Date.Substring(0, 7) : (me.Date ?? "");
                    memLines.Add($"• {ym} {me.Title}");
                }
            string domKey = SimEngine.DominantStaffFaction(State);
            if (!string.IsNullOrEmpty(domKey))
            {
                var spec = SimEngine.StaffFactions.FirstOrDefault(sp => sp.Key == domKey);
                memLines.Add($"★ 主導派系：【{spec.Name}】{spec.Chief}（{spec.BonusDesc}）");
            }
            else
            {
                memLines.Add("★ 中樞評述：參謀四派制衡並存，軍政權力保持均衡。");
            }
            string memoirText = string.Join("\n", memLines);

            int leftW = (int)(w * 0.48);
            int rightW = Math.Max(180, w - leftW - 12);
            var summaryFont = new Font("Microsoft JhengHei UI", 9.6f);
            var memFont = new Font("Microsoft JhengHei UI", 8.4f);
            var summarySize = TextRenderer.MeasureText(summaryText, summaryFont,
                new Size(Math.Max(120, leftW - 22), 300), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            var memSize = TextRenderer.MeasureText(memoirText, memFont,
                new Size(Math.Max(120, rightW - 20), 300), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            int rowH = Math.Clamp(Math.Max(summarySize.Height + 20, memSize.Height + 20), 148, 184);

            var lblText = new Label
            {
                Text = summaryText,
                ForeColor = TextMain,
                BackColor = BgCard,
                Font = summaryFont,
                Location = new Point(20, y),
                Size = new Size(leftW, rowH),
                Padding = new Padding(10)
            };
            var lblMemoir = new Label
            {
                Text = memoirText,
                ForeColor = TextMain,
                BackColor = BgCard,
                Font = memFont,
                Location = new Point(20 + leftW + 12, y),
                Size = new Size(rightW, rowH),
                Padding = new Padding(10)
            };
            y += rowH + 14;

            int chartHeight = Math.Clamp(_mapPanel.Height - 445, 145, 190);
            var historyCharts = new BufferedPanel
            {
                Location = new Point(20, y),
                Size = new Size(w, chartHeight),
                BackColor = BgPanel
            };
            historyCharts.Paint += (_, pe) =>
            {
                int gap = 12;
                int chartW = Math.Max(180, (historyCharts.Width - gap) / 2);
                DrawLineChart(pe.Graphics, new Rectangle(0, 0, chartW, historyCharts.Height),
                    "兩岸 GDP 規模演變（億美元）",
                    State.History.Select(h => (h.Year, h.RocGdp, h.PrcGdp)).ToList());
                DrawLineChart(pe.Graphics, new Rectangle(chartW + gap, 0, chartW, historyCharts.Height),
                    "兩岸綜合軍力指數對比",
                    State.History.Select(h => (h.Year, h.RocMil, h.PrcMil)).ToList());
            };
            y += chartHeight + 14;

            var btnRestart = CreateWarButton("重新展開新局（返回主選單）", Color.FromArgb(148, 102, 34), TextMain, 260, 42, GoldCol);
            btnRestart.Font = new Font("Microsoft JhengHei UI", 10.5f, FontStyle.Bold);
            btnRestart.Location = new Point(Math.Max(20, (_eventOverlay.Width - 260) / 2), y);
            btnRestart.Click += (_, _) => { _showTitle = true; UpdateOverlays(); SyncSimTimer(); };

            _eventOverlay.Controls.AddRange(new Control[] { lblGroup, lblTitle, lblText, lblMemoir, historyCharts, btnRestart });
            _eventOverlay.Height = Math.Min(Math.Max(500, btnRestart.Bottom + 18), Math.Max(500, _mapPanel.Height - 24));
            _eventOverlay.Location = new Point((_mapPanel.Width - _eventOverlay.Width) / 2, (_mapPanel.Height - _eventOverlay.Height) / 2);
            _eventOverlay.ResumeLayout();
        }
    }
}
