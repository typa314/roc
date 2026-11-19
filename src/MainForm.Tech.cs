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
        private void OpenTechPageAt(string techId)
        {
            if (!GameData.Techs.TryGetValue(techId, out var tech)) return;
            OpenTechPage(tech.Branch);
            if (!_techPageOpen) return;

            _selectedTechId = tech.Id;
            _hoverTechId = null;
            _hoverTechBtnId = null;
            RefreshTechOverlay();
            var box = _techHitBoxes.FirstOrDefault(x => x.id == tech.Id);
            if (!box.rect.IsEmpty)
            {
                int targetX = Math.Max(0, box.rect.Left + box.rect.Width / 2 - _techScrollWrap.ViewportWidth / 2);
                int targetY = Math.Max(0, box.rect.Top + box.rect.Height / 2 - _techScrollWrap.ViewportHeight / 2);
                _techScrollWrap.ScrollTo(targetX, targetY);
            }
            _techCanvas.Invalidate();
        }

        // ==================== 五大國策科技樹藍圖檔案專頁（時間續流 · 事件發生時自動暫停） ====================
        private void BuildTechOverlay()
        {
            _techOverlay = new WarRoomPanel
            {
                Visible = false,
                BackColor = Color.FromArgb(250, 28, 25, 20),
                FrameBorderColor = GoldCol,
                InnerBorderColor = Color.FromArgb(78, 66, 48),
                Chamfer = 14
            };

            var lblTitle = new Label
            {
                Text = "⚙ 國防科研與五大國策藍圖",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 12.2f, FontStyle.Bold),
                Location = new Point(20, 12),
                AutoSize = true
            };

            _techPauseBadgeLbl = new Label
            {
                Text = "▌▌ 時間已自動暫停 · 按 T 或 ESC 返回戰場",
                ForeColor = Color.FromArgb(148, 212, 204),
                BackColor = Color.FromArgb(36, 56, 52),
                Font = new Font("Microsoft JhengHei UI", 8.4f, FontStyle.Bold),
                Location = new Point(248, 13),
                Size = new Size(290, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _techHdrSummaryLbl = new Label
            {
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold),
                Location = new Point(526, 13),
                Size = new Size(444, 25),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            _techCloseBtn = CreateWarButton("✕ 返回戰圖 (ESC)", Color.FromArgb(52, 46, 36), GoldCol, 140, 30, GoldCol);
            _techCloseBtn.Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
            _techCloseBtn.Click += (_, _) => CloseTechPage();

            _techOverlay.Controls.AddRange(new Control[] { lblTitle, _techPauseBadgeLbl, _techHdrSummaryLbl, _techCloseBtn });

            foreach (var spec in TechBranchSpecs)
            {
                string kCopy = spec.key;
                var b = CreateWarButton(spec.label, BgCard, TextMain, 148, 32);
                b.Font = new Font("Microsoft JhengHei UI", 9.0f, FontStyle.Bold);
                b.Click += (_, _) => SelectTechBranch(kCopy);
                _techBranchTabBtns[spec.key] = b;
                _techOverlay.Controls.Add(b);
            }

            var lblResCap = new Label
            {
                Text = "預算:",
                ForeColor = TextSub,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.4f, FontStyle.Bold),
                Size = new Size(42, 30),
                TextAlign = ContentAlignment.MiddleRight
            };
            _techOverlay.Controls.Add(lblResCap);

            for (int lv = 1; lv <= 4; lv++)
            {
                int copyLv = lv;
                var rb = CreateWarButton($"Lv.{lv}", BgCard, TextMain, 46, 30);
                rb.Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
                rb.Click += (_, _) =>
                {
                    State.ResLevel = copyLv;
                    RefreshAllUi();
                };
                _techResLvBtns[lv] = rb;
                _techOverlay.Controls.Add(rb);
            }

            _techRushToggleBtn = CreateWarButton("⚡ 專案趕工 (+50%速/+40%費)", BgCard, TextSub, 178, 30);
            _techRushToggleBtn.Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
            _techRushToggleBtn.Click += (_, _) => ToggleResearchRush();
            _techOverlay.Controls.Add(_techRushToggleBtn);

            _techScrollWrap = new DarkScrollContainer
            {
                BackColor = Color.FromArgb(24, 22, 18),
                ShowVerticalBar = true,
                ShowHorizontalBar = true,
                TrackBorderColor = BorderCol
            };
            _techCanvas = new BufferedPanel
            {
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(24, 22, 18),
                Cursor = Cursors.Hand
            };
            _techCanvas.Paint += TechCanvas_Paint;
            _techCanvas.MouseMove += TechCanvas_MouseMove;
            _techCanvas.MouseLeave += (_, _) =>
            {
                if (_hoverTechId != null || _hoverTechBtnId != null)
                {
                    _hoverTechId = null;
                    _hoverTechBtnId = null;
                    _techCanvas.Invalidate();
                    UpdateTechDetailLabels();
                }
            };
            _techCanvas.MouseDown += TechCanvas_MouseDown;
            _techScrollWrap.SetContent(_techCanvas);
            _techOverlay.Controls.Add(_techScrollWrap);

            _techDetailTitleLbl = new Label
            {
                ForeColor = GoldCol,
                BackColor = BgCard,
                Font = new Font("Microsoft JhengHei UI", 9.6f, FontStyle.Bold),
                Padding = new Padding(12, 6, 8, 0),
                TextAlign = ContentAlignment.TopLeft
            };
            _techDetailDescLbl = new Label
            {
                ForeColor = TextMain,
                BackColor = BgCard,
                Font = new Font("Microsoft JhengHei UI", 8.6f),
                Padding = new Padding(12, 2, 8, 4),
                TextAlign = ContentAlignment.TopLeft
            };
            _techBranchBonusLbl = new Label
            {
                ForeColor = TextSub,
                BackColor = BgCard,
                Font = new Font("Microsoft JhengHei UI", 8.5f, FontStyle.Bold),
                Padding = new Padding(6, 8, 12, 6),
                TextAlign = ContentAlignment.MiddleRight
            };
            _techOverlay.Controls.AddRange(new Control[] { _techDetailTitleLbl, _techDetailDescLbl, _techBranchBonusLbl });
            _techOverlay.Tag = lblResCap;
        }

        private void CenterTechOverlay()
        {
            if (_techOverlay == null || !_techOverlay.Visible || _mapPanel.Width <= 0 || _mapPanel.Height <= 0) return;
            int w = Math.Clamp(_mapPanel.Width - 36, 980, 1300);
            int h = Math.Clamp(_mapPanel.Height - 20, 540, 740);
            _techOverlay.Size = new Size(w, h);
            _techOverlay.Location = new Point((_mapPanel.Width - w) / 2, (_mapPanel.Height - h) / 2);
            LayoutTechOverlayControls();
        }

        private void LayoutTechOverlayControls()
        {
            if (_techOverlay == null || _techOverlay.Width <= 0) return;
            int w = _techOverlay.Width;
            int h = _techOverlay.Height;

            _techCloseBtn.Location = new Point(w - _techCloseBtn.Width - 18, 10);
            _techHdrSummaryLbl.Width = Math.Max(140, _techCloseBtn.Left - _techHdrSummaryLbl.Left - 10);

            int tabsRight = w - 446;
            int tabW = Math.Clamp((tabsRight - 20 - 4 * 6) / 5, 68, 160);
            int tx = 20;
            foreach (var spec in TechBranchSpecs)
            {
                if (_techBranchTabBtns.TryGetValue(spec.key, out var b))
                {
                    b.Location = new Point(tx, 50);
                    b.Size = new Size(tabW, 32);
                    tx += tabW + 6;
                }
            }

            _techRushToggleBtn.Location = new Point(w - _techRushToggleBtn.Width - 18, 51);
            int lvRight = _techRushToggleBtn.Left - 8;
            for (int lv = 4; lv >= 1; lv--)
            {
                if (_techResLvBtns.TryGetValue(lv, out var rb))
                {
                    rb.Location = new Point(lvRight - rb.Width, 51);
                    lvRight = rb.Left - 4;
                }
            }
            if (_techOverlay.Tag is Label lblResCap)
                lblResCap.Location = new Point(lvRight - lblResCap.Width - 2, 51);

            _techScrollWrap.Location = new Point(18, 90);
            _techScrollWrap.Size = new Size(w - 36, Math.Max(220, h - 178));

            int footY = h - 78;
            int leftW = (int)((w - 42) * 0.67);
            int rightX = 18 + leftW + 6;
            int rightW = (w - 18) - rightX;

            _techDetailTitleLbl.Location = new Point(18, footY);
            _techDetailTitleLbl.Size = new Size(leftW, 28);
            _techDetailDescLbl.Location = new Point(18, footY + 28);
            _techDetailDescLbl.Size = new Size(leftW, 38);
            _techBranchBonusLbl.Location = new Point(rightX, footY);
            _techBranchBonusLbl.Size = new Size(rightW, 66);

            ComputeTechHitBoxes();
            _techScrollWrap.SyncScroll();
        }

        public void OpenTechPage(string branch = null)
        {
            if (_showTitle || State == null || State.Over != null || State.PendingEvents.Count > 0) return;
            _techPageOpen = true;
            SyncSimTimer();
            if (_intelPageOpen) CloseIntelPage();
            if (_activeDrawer != "none") CloseDrawer();
            _nodeCard.Visible = false;
            _zoneCard.Visible = false;
            _hudTooltipCard.Visible = false;
            if (!string.IsNullOrEmpty(branch) && TechBranchSpecs.Any(x => x.key == branch))
                _techBranchTab = branch;
            if (string.IsNullOrEmpty(_selectedTechId) || !GameData.Techs.TryGetValue(_selectedTechId, out var selT) || selT.Branch != _techBranchTab)
                _selectedTechId = GameData.TechList.FirstOrDefault(t => t.Branch == _techBranchTab && SimEngine.TechAvailable(State, t))?.Id
                               ?? GameData.TechList.FirstOrDefault(t => t.Branch == _techBranchTab)?.Id;
            _techFlashMsg = null;
            _techOverlay.Visible = true;
            CenterTechOverlay();
            _techOverlay.BringToFront();
            RefreshTechOverlay();
            UpdateSpeedButtonsUi();
            SyncSimTimer();
            _hudPanel?.Invalidate();
            RefreshDockButtons();
        }

        public void CloseTechPage()
        {
            if (!_techPageOpen) return;
            _techPageOpen = false;
            _hoverTechId = null;
            _hoverTechBtnId = null;
            _techFlashMsg = null;
            if (_techOverlay != null) _techOverlay.Visible = false;
            UpdateOverlays();
            UpdateSpeedButtonsUi();
            SyncSimTimer();
            _hudPanel?.Invalidate();
            RefreshDockButtons();
        }

        public void ToggleTechPage()
        {
            if (_techPageOpen) CloseTechPage();
            else OpenTechPage();
        }

        public void SelectTechBranch(string branchKey)
        {
            if (!TechBranchSpecs.Any(x => x.key == branchKey)) return;
            _techBranchTab = branchKey;
            _hoverTechId = null;
            _hoverTechBtnId = null;
            _techFlashMsg = null;
            _selectedTechId = GameData.TechList.FirstOrDefault(t => t.Branch == branchKey && SimEngine.TechAvailable(State, t))?.Id
                           ?? GameData.TechList.FirstOrDefault(t => t.Branch == branchKey)?.Id;
            _techScrollWrap?.ScrollTo(0, 0);
            RefreshTechOverlay();
        }

        public void ToggleResearchRush()
        {
            if (State == null) return;
            State.ResearchRush = !State.ResearchRush;
            RefreshAllUi();
        }

        private void RefreshTechOverlay()
        {
            if (!_techPageOpen || _techOverlay == null || State == null) return;

            if (_techPauseBadgeLbl != null)
            {
                if (State.PendingEvents.Count > 0)
                {
                    _techPauseBadgeLbl.Text = "▌▌ 事件暫停 · 請先處置急電";
                    _techPauseBadgeLbl.ForeColor = GoldCol;
                    _techPauseBadgeLbl.BackColor = Color.FromArgb(94, 68, 28);
                }
                else
                {
                    _techPauseBadgeLbl.Text = "▌▌ 時間已自動暫停 · 按 T 或 ESC 返回戰場";
                    _techPauseBadgeLbl.ForeColor = Color.FromArgb(148, 212, 204);
                    _techPauseBadgeLbl.BackColor = Color.FromArgb(36, 56, 52);
                }
            }

            int actCnt = State.ActiveTechs?.Count ?? 0;
            string currentTag = "尚未啟動研發";
            if (actCnt == 1 && GameData.Techs.TryGetValue(State.ActiveTechs[0], out var ct1))
                currentTag = $"研發 {ct1.Name} · 剩{SimEngine.EstDaysRemaining(State, ct1.Id)}天";
            else if (actCnt > 1 && GameData.Techs.TryGetValue(State.ActiveTechs[0], out var ctMain))
                currentTag = $"並行研發 {actCnt} 項（{ctMain.Name} 等 · 剩{SimEngine.EstDaysRemaining(State, ctMain.Id)}天）";
            else if (!string.IsNullOrEmpty(State.ResearchId) && GameData.Techs.TryGetValue(State.ResearchId, out var ct))
                currentTag = $"研發 {ct.Name} · 剩{SimEngine.EstDaysRemaining(State, ct.Id)}天";
            string rushTag = State.ResearchRush ? " · 趕工" : "";
            _techHdrSummaryLbl.Text = $"國庫 ${State.Treasury:F1}億 · 科研 +{SimEngine.RpPerTurn(State):F0}/年{rushTag} · {currentTag}";

            if (_techRushToggleBtn != null)
            {
                _techRushToggleBtn.Text = State.ResearchRush ? "⚡ 趕工中 (+50%速/+40%費)" : "⚡ 專案趕工 (+50%速/+40%費)";
                SetWarButtonStyle(_techRushToggleBtn,
                    State.ResearchRush ? Color.FromArgb(136, 92, 28) : BgCard,
                    State.ResearchRush ? TextMain : TextSub,
                    State.ResearchRush ? GoldCol : BorderCol);
            }

            foreach (var spec in TechBranchSpecs)
            {
                if (!_techBranchTabBtns.TryGetValue(spec.key, out var b)) continue;
                int total = GameData.TechList.Count(t => t.Branch == spec.key);
                int owned = GameData.TechList.Count(t => t.Branch == spec.key && State.Techs.Contains(t.Id));
                bool hasActive = State.ActiveTechs != null && State.ActiveTechs.Any(aid => GameData.Techs.TryGetValue(aid, out var at) && at.Branch == spec.key);
                string badge = hasActive ? " ★" : "";
                b.Text = $"{spec.label} {owned}/{total}{badge}";
                bool act = _techBranchTab == spec.key;
                SetWarButtonStyle(
                    b,
                    act ? Color.FromArgb(58, 49, 36) : BgCard,
                    act ? TextMain : (hasActive ? GoodGreen : TextSub),
                    act ? spec.col : (hasActive ? GoodGreen : BorderCol));
            }

            for (int lv = 1; lv <= 4; lv++)
            {
                if (_techResLvBtns.TryGetValue(lv, out var rb))
                {
                    bool act = State.ResLevel == lv;
                    SetWarButtonStyle(rb, act ? RocBlue : BgCard, act ? TextMain : TextSub, act ? GoldCol : BorderCol);
                }
            }

            ComputeTechHitBoxes();
            _techScrollWrap?.SyncScroll();
            _techCanvas.Invalidate();
            UpdateTechDetailLabels();
        }

        private void ComputeTechHitBoxes()
        {
            _techHitBoxes.Clear();
            if (_techScrollWrap == null || _techCanvas == null || State == null) return;

            int wrapW = _techScrollWrap.ViewportWidth > 100 ? _techScrollWrap.ViewportWidth : 1240;
            int wrapH = _techScrollWrap.ViewportHeight > 100 ? _techScrollWrap.ViewportHeight : 442;
            int innerW = Math.Max(1040, wrapW - 8);

            int nodeW = Math.Clamp((innerW - 24 - 4 * 20) / 5, 190, 232);
            int colGap = Math.Max(20, (innerW - 24 - 5 * nodeW) / 4);
            const int topPad = 42;

            var branchTechs = GameData.TechList.Where(t => t.Branch == _techBranchTab).ToList();
            int maxRows = Math.Max(1, Enumerable.Range(0, 5).Max(c => branchTechs.Count(t => t.Tier == c)));
            int availH = Math.Max(352, wrapH - topPad - 8);
            int nodeH = Math.Clamp((availH - Math.Max(0, maxRows - 1) * 6) / maxRows, 84, 106);
            int rowGap = maxRows > 1 ? Math.Clamp((availH - maxRows * nodeH) / (maxRows - 1), 6, 26) : 0;

            int canvasW = Math.Max(wrapW, 24 + 5 * nodeW + 4 * colGap);
            int canvasH = Math.Max(wrapH, topPad + maxRows * nodeH + Math.Max(0, maxRows - 1) * rowGap + 10);

            if (_techCanvas.Width != canvasW || _techCanvas.Height != canvasH)
                _techCanvas.Size = new Size(canvasW, canvasH);

            for (int c = 0; c < 5; c++)
            {
                var colNodes = branchTechs.Where(t => t.Tier == c).ToList();
                int k = colNodes.Count;
                int colH = k * nodeH + Math.Max(0, k - 1) * rowGap;
                int startY = topPad + Math.Max(0, (canvasH - topPad - colH - 6) / 2);

                for (int i = 0; i < k; i++)
                {
                    var t = colNodes[i];
                    var rect = new Rectangle(12 + c * (nodeW + colGap), startY + i * (nodeH + rowGap), nodeW, nodeH);
                    bool done = State.Techs.Contains(t.Id);
                    bool active = SimEngine.IsResearching(State, t.Id);
                    bool avail = SimEngine.TechAvailable(State, t);

                    Rectangle btnRes = (!done && !active && avail) ? rect : Rectangle.Empty;
                    _techHitBoxes.Add((t.Id, rect, btnRes));
                }
            }
        }

        private static PointF[] TechHexPoints(float cx, float cy, float r)
        {
            var pts = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double ang = i * Math.PI / 3.0;
                pts[i] = new PointF(cx + r * (float)Math.Cos(ang), cy + r * (float)Math.Sin(ang));
            }
            return pts;
        }

        private static void DrawTechProgressHexRing(Graphics g, float cx, float cy, float r, double ratio, Color col)
        {
            if (ratio <= 0.001) return;
            var pts = TechHexPoints(cx, cy, r);
            // 自頂點 (4 = 240 deg) 順時針沿六邊形外框繪製進度條
            int[] order = { 4, 5, 0, 1, 2, 3, 4 };
            double totalSegs = 6.0 * Math.Clamp(ratio, 0.0, 1.0);
            int fullSegs = (int)Math.Floor(totalSegs);
            float partial = (float)(totalSegs - fullSegs);

            using var pen = new Pen(col, 2.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (int s = 0; s < fullSegs && s < 6; s++)
                g.DrawLine(pen, pts[order[s]], pts[order[s + 1]]);
            if (fullSegs < 6 && partial > 0.01f)
            {
                var pA = pts[order[fullSegs]];
                var pB = pts[order[fullSegs + 1]];
                g.DrawLine(pen, pA.X, pA.Y, pA.X + (pB.X - pA.X) * partial, pA.Y + (pB.Y - pA.Y) * partial);
            }
        }

        private static string GetTechIconKind(string techId) => techId switch
        {
            "land_reform" or "ntd_reform" => "grain",
            "import_sub" or "export_zone" or "ten_projects" or "us_aid_ind" or "itri_lab" => "factory",
            "csist" or "nuclear_power" => "flask",
            "sci_park" or "semicon" or "auto_logi" or "biotech_ai" or "high_speed_logi" => "chip",
            "nuke_program" or "nuke_weapon" => "atom",
            "us_reorg" or "armor" or "m48h" => "tank",
            "jets" or "f104" or "f5e" or "idf" or "p2v_patrol" => "jet",
            "airborne" or "coastal_frogmen" => "chute",
            "amphib_doc" or "lst_fleet" or "amphib_group" or "perry" or "yang_ddg" or "keelung_kaohsiung" or "merchant_reserve" => "ship",
            "subs" => "sub",
            "sidewinder" or "hsiung_feng" or "tienma" or "tienkung" or "nike_sam" or "patriot_pac" or "yunfeng_uav" => "missile",
            "recon_coast" or "sat_elint" or "c4i" or "leishan_radar" or "formosat_recon" => "radar",
            "comint" or "se_asia_listen" or "cyber_psy" => "wave",
            "black_cat" => "u2",
            "hkmacau_net" or "defector_award" => "eye",
            "psywar" or "hongkong_psy" or "radio_liberty" or "kinmen_horn" or "cultural_psy" => "tower",
            "underground" or "united_subvert" or "mainland_relief" => "net",
            "front_depot" or "sea_shuttle" or "cross_island_hw" => "depot",
            _ => "flask"
        };

        private static void DrawTechVectorIcon(Graphics g, string techId, float cx, float cy, Color ink, Color softFill)
        {
            string kind = GetTechIconKind(techId);
            using var pen = new Pen(ink, 1.85f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            using var thin = new Pen(ink, 1.25f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var br = new SolidBrush(ink);
            using var fill = new SolidBrush(softFill);

            switch (kind)
            {
                case "grain": // 麥穗與耕田：一眼看出農地改革
                    g.DrawLine(pen, cx - 2, cy + 11, cx + 2, cy - 10);
                    for (int i = -1; i <= 1; i++)
                    {
                        float yy = cy + i * 5f - 1f;
                        g.DrawArc(pen, cx - 9, yy - 3, 9, 6, 10, 140);
                        g.DrawArc(pen, cx, yy - 3, 9, 6, 30, 140);
                    }
                    g.DrawArc(thin, cx - 12, cy + 7, 24, 8, 195, 150);
                    break;

                case "factory": // 鋸齒廠房與煙囪：工業建設
                    var fPts = new[]
                    {
                        new PointF(cx - 11, cy + 9), new PointF(cx - 11, cy - 2),
                        new PointF(cx - 4, cy + 2), new PointF(cx - 4, cy - 2),
                        new PointF(cx + 3, cy + 2), new PointF(cx + 3, cy - 2),
                        new PointF(cx + 7, cy - 2), new PointF(cx + 7, cy - 10),
                        new PointF(cx + 11, cy - 10), new PointF(cx + 11, cy + 9)
                    };
                    g.FillPolygon(fill, fPts);
                    g.DrawPolygon(pen, fPts);
                    g.DrawArc(thin, cx + 5, cy - 15, 7, 5, 180, 140);
                    break;

                case "flask": // 三角燒瓶科研
                    var flPts = new[]
                    {
                        new PointF(cx - 3, cy - 11), new PointF(cx + 3, cy - 11),
                        new PointF(cx + 3, cy - 4), new PointF(cx + 10, cy + 9),
                        new PointF(cx - 10, cy + 9), new PointF(cx - 3, cy - 4)
                    };
                    g.FillPolygon(fill, new[]
                    {
                        new PointF(cx - 6, cy + 2), new PointF(cx + 6, cy + 2),
                        new PointF(cx + 10, cy + 9), new PointF(cx - 10, cy + 9)
                    });
                    g.DrawPolygon(pen, flPts);
                    g.FillEllipse(br, cx - 2, cy + 4, 3, 3);
                    break;

                case "chip": // 積體電路晶片：園區/半導體/自動化
                    g.FillRectangle(fill, cx - 7, cy - 7, 14, 14);
                    g.DrawRectangle(pen, cx - 7, cy - 7, 14, 14);
                    g.DrawRectangle(thin, cx - 3, cy - 3, 6, 6);
                    for (int k = -4; k <= 4; k += 4)
                    {
                        g.DrawLine(thin, cx + k, cy - 11, cx + k, cy - 7);
                        g.DrawLine(thin, cx + k, cy + 7, cx + k, cy + 11);
                        g.DrawLine(thin, cx - 11, cy + k, cx - 7, cy + k);
                        g.DrawLine(thin, cx + 7, cy + k, cx + 11, cy + k);
                    }
                    break;

                case "atom": // 原子軌道與核心：核研/核武
                    g.FillEllipse(br, cx - 3, cy - 3, 6, 6);
                    foreach (float ang in new[] { 0f, 60f, 120f })
                    {
                        var st = g.Save();
                        g.TranslateTransform(cx, cy);
                        g.RotateTransform(ang);
                        g.DrawEllipse(thin, -12, -4.5f, 24, 9f);
                        g.Restore(st);
                    }
                    break;

                case "tank": // 裝甲戰車側影（砲塔＋砲管＋履帶）
                    using (var tPath = WarGdi.CreateRoundedPath(new RectangleF(cx - 11, cy + 1, 20, 6), 3f))
                    {
                        g.FillPath(fill, tPath);
                        g.DrawPath(pen, tPath);
                    }
                    var tur = new[] { new PointF(cx - 6, cy + 1), new PointF(cx - 4, cy - 5), new PointF(cx + 4, cy - 5), new PointF(cx + 6, cy + 1) };
                    g.FillPolygon(fill, tur);
                    g.DrawPolygon(pen, tur);
                    g.DrawLine(pen, cx + 4, cy - 3, cx + 13, cy - 3);
                    for (float wx = cx - 8; wx <= cx + 6; wx += 4.6f)
                        g.FillEllipse(br, wx, cy + 7, 3, 3);
                    break;

                case "jet": // 三角翼噴射戰機（斜飛俯視輪廓）
                    var stJ = g.Save();
                    g.TranslateTransform(cx, cy);
                    g.RotateTransform(-35f);
                    var jPts = new[]
                    {
                        new PointF(12, 0), new PointF(2, -3), new PointF(-6, -11), new PointF(-8, -11),
                        new PointF(-5, -2.5f), new PointF(-10, -5), new PointF(-11, -5), new PointF(-9, 0),
                        new PointF(-11, 5), new PointF(-10, 5), new PointF(-5, 2.5f), new PointF(-8, 11),
                        new PointF(-6, 11), new PointF(2, 3)
                    };
                    g.FillPolygon(fill, jPts);
                    g.DrawPolygon(pen, jPts);
                    g.Restore(stJ);
                    break;

                case "chute": // 空降傘兵（傘蓋＋傘繩）
                    g.FillPie(fill, cx - 11, cy - 11, 22, 16, 180, 180);
                    g.DrawArc(pen, cx - 11, cy - 11, 22, 16, 180, 180);
                    g.DrawLine(pen, cx - 11, cy - 3, cx + 11, cy - 3);
                    g.DrawLine(thin, cx - 10, cy - 3, cx, cy + 7);
                    g.DrawLine(thin, cx + 10, cy - 3, cx, cy + 7);
                    g.DrawLine(thin, cx, cy - 3, cx, cy + 7);
                    g.FillEllipse(br, cx - 2.5f, cy + 6.5f, 5, 5);
                    break;

                case "ship": // 戰艦／登陸艦艦身＋波浪
                    var sPts = new[]
                    {
                        new PointF(cx - 12, cy + 1), new PointF(cx + 12, cy + 1),
                        new PointF(cx + 8, cy + 7), new PointF(cx - 9, cy + 7)
                    };
                    g.FillPolygon(fill, sPts);
                    g.DrawPolygon(pen, sPts);
                    g.DrawRectangle(pen, cx - 5, cy - 5, 8, 6);
                    g.DrawLine(pen, cx - 1, cy - 5, cx - 1, cy - 11);
                    g.DrawLine(thin, cx - 11, cy + 10, cx + 11, cy + 10);
                    break;

                case "sub": // 潛艦帆罩與潛望鏡
                    g.FillEllipse(fill, cx - 12, cy, 24, 9);
                    g.DrawEllipse(pen, cx - 12, cy, 24, 9);
                    g.DrawRectangle(pen, cx - 4, cy - 5, 7, 5);
                    g.DrawLine(pen, cx, cy - 5, cx, cy - 10);
                    g.DrawLine(pen, cx, cy - 10, cx + 4, cy - 10);
                    break;

                case "missile": // 斜升飛彈＋尾焰
                    var stM = g.Save();
                    g.TranslateTransform(cx, cy);
                    g.RotateTransform(-45f);
                    var mPts = new[]
                    {
                        new PointF(12, 0), new PointF(6, -3), new PointF(-7, -3), new PointF(-10, -6),
                        new PointF(-8, 0), new PointF(-10, 6), new PointF(-7, 3), new PointF(6, 3)
                    };
                    g.FillPolygon(fill, mPts);
                    g.DrawPolygon(pen, mPts);
                    g.DrawLine(thin, -11, -2, -14, -3);
                    g.DrawLine(thin, -11, 0, -15, 0);
                    g.DrawLine(thin, -11, 2, -14, 3);
                    g.Restore(stM);
                    break;

                case "radar": // 雷達天線與電磁波弧
                    g.DrawArc(pen, cx - 11, cy - 7, 14, 14, -40, 160);
                    g.DrawLine(pen, cx - 4, cy + 1, cx - 9, cy + 10);
                    g.DrawLine(pen, cx - 9, cy + 10, cx + 1, cy + 10);
                    g.DrawLine(thin, cx - 2, cy - 1, cx + 2, cy - 5);
                    g.DrawArc(thin, cx + 1, cy - 9, 8, 12, -55, 110);
                    g.DrawArc(thin, cx + 3, cy - 12, 10, 18, -55, 110);
                    break;

                case "wave": // 電訊截聽脈衝波
                    g.DrawRectangle(thin, cx - 11, cy - 8, 22, 16);
                    var wPts = new[]
                    {
                        new PointF(cx - 9, cy), new PointF(cx - 5, cy), new PointF(cx - 2, cy - 6),
                        new PointF(cx + 1, cy + 6), new PointF(cx + 4, cy - 4), new PointF(cx + 6, cy), new PointF(cx + 9, cy)
                    };
                    g.DrawLines(pen, wPts);
                    break;

                case "u2": // U-2 長翼高空偵察機
                    g.DrawLine(pen, cx - 13, cy, cx + 13, cy);
                    g.DrawLine(pen, cx, cy - 9, cx, cy + 9);
                    g.DrawLine(pen, cx - 4, cy + 7, cx + 4, cy + 7);
                    g.FillEllipse(br, cx - 2.5f, cy - 2.5f, 5, 5);
                    break;

                case "eye": // 情報偵照之眼
                    g.DrawArc(pen, cx - 12, cy - 6, 24, 14, 195, 150);
                    g.DrawArc(pen, cx - 12, cy - 8, 24, 14, 15, 150);
                    g.FillEllipse(fill, cx - 4.5f, cy - 4.5f, 9, 9);
                    g.DrawEllipse(pen, cx - 4.5f, cy - 4.5f, 9, 9);
                    g.FillEllipse(br, cx - 2, cy - 2, 4, 4);
                    break;

                case "tower": // 廣播心戰電塔＋電波
                    g.DrawLine(pen, cx - 6, cy + 11, cx, cy - 6);
                    g.DrawLine(pen, cx + 6, cy + 11, cx, cy - 6);
                    g.DrawLine(thin, cx - 4, cy + 4, cx + 4, cy + 4);
                    g.FillEllipse(br, cx - 2.5f, cy - 8.5f, 5, 5);
                    g.DrawArc(thin, cx - 9, cy - 11, 18, 12, 130, 80);
                    g.DrawArc(thin, cx - 9, cy - 11, 18, 12, -30, 80);
                    break;

                case "net": // 地下秘密聯絡網節點
                    var nPts = new[] { new PointF(cx, cy - 9), new PointF(cx + 10, cy - 1), new PointF(cx + 6, cy + 9), new PointF(cx - 6, cy + 9), new PointF(cx - 10, cy - 1) };
                    g.DrawPolygon(thin, nPts);
                    g.DrawLine(thin, cx, cy - 9, cx + 6, cy + 9);
                    g.DrawLine(thin, cx - 10, cy - 1, cx + 10, cy - 1);
                    foreach (var np in nPts) g.FillEllipse(br, np.X - 2.2f, np.Y - 2.2f, 4.4f, 4.4f);
                    break;

                case "depot": // 前進儲運油彈箱／後勤輸送
                    g.FillRectangle(fill, cx - 9, cy - 7, 18, 14);
                    g.DrawRectangle(pen, cx - 9, cy - 7, 18, 14);
                    g.DrawLine(thin, cx - 9, cy - 1, cx + 9, cy - 1);
                    g.DrawLine(pen, cx, cy - 7, cx, cy + 7);
                    g.DrawLine(thin, cx - 12, cy + 10, cx + 12, cy + 10);
                    g.DrawLine(thin, cx + 8, cy + 7, cx + 12, cy + 10);
                    break;
            }
        }

        private void DrawTechHoverTooltip(Graphics g, (string id, Rectangle rect, Rectangle btnResearch) box, Color branchCol)
        {
            if (State == null || !GameData.Techs.TryGetValue(box.id, out var t)) return;
            bool done = State.Techs.Contains(t.Id);
            bool active = SimEngine.IsResearching(State, t.Id);
            bool avail = SimEngine.TechAvailable(State, t);
            double prog = SimEngine.SavedResearchProg(State, t.Id);
            double fee = SimEngine.TechStartCost(State, t);
            bool paid = State.HasFlag("techPaid_" + t.Id);
            bool canAfford = paid || State.Treasury + 1e-9 >= fee;
            int daysNeed = SimEngine.EstDaysRemaining(State, t.Id);
            string brName = TechBranchSpecs.FirstOrDefault(x => x.key == t.Branch).label ?? t.Branch;

            string stateStr = done ? "✔ 已核定生效"
                            : active ? $"★ 研發中 {Math.Clamp(prog / Math.Max(1.0, t.Cost) * 100.0, 0, 100):F0}%（剩 {daysNeed} 天）"
                            : avail ? "▶ 可撥款立即研發"
                            : SimEngine.TechReqText(State, t);
            string reqStr = (t.Req != null && t.Req.Length > 0)
                ? "前置：" + string.Join("、", t.Req.Select(rid => GameData.Techs[rid].Name + (State.Techs.Contains(rid) ? "✔" : "(未)")))
                : "無前置科技需求";
            string feeStr = done ? $"原核定需求 {t.Cost:F0} 研發點"
                          : active ? $"進度 {prog:F2}/{t.Cost:F0} 研發點（剩 {daysNeed} 天）"
                          : paid ? $"✔ 已撥款 · 需 {t.Cost:F0} 研發點（約 {daysNeed} 天）"
                          : $"啟動經費 ${fee:F2}億{(canAfford ? "" : "（國庫不足）")} · 需 {t.Cost:F0} 點（約 {daysNeed} 天）";
            if (!done && !active && prog > 0.001) feeStr += $" · 已存 {prog:F2}";
            string hintStr = done ? "已完成研發並持續生效"
                           : active ? "已撥款立即研發中（可同時立項其他解鎖科技）"
                           : avail ? (canAfford ? "點擊六邊形：撥款後立即研發（免排程等待）" : "國庫不足，暫無法啟動研發")
                           : "尚未滿足前置條件或年代門檻";

            const int tipW = 316;
            using var titleF = new Font("Microsoft JhengHei UI", 9.4f, FontStyle.Bold);
            using var metaF = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
            using var descF = new Font("Microsoft JhengHei UI", 8.4f);
            using var hintF = new Font("Microsoft JhengHei UI", 7.5f);

            var descSz = g.MeasureString(t.Desc, descF, tipW - 24);
            var reqSz = g.MeasureString(reqStr, hintF, tipW - 24);
            int tipH = 74 + (int)Math.Ceiling(descSz.Height) + (int)Math.Ceiling(reqSz.Height) + 28;

            int sx = _techScrollWrap?.ScrollX ?? 0;
            int sy = _techScrollWrap?.ScrollY ?? 0;
            int vw = _techScrollWrap?.ViewportWidth ?? _techCanvas.Width;
            int vh = _techScrollWrap?.ViewportHeight ?? _techCanvas.Height;

            int hexCx = box.rect.X + box.rect.Width / 2;
            int tx = hexCx + 38;
            if (tx + tipW > sx + vw - 8) tx = hexCx - 38 - tipW;
            tx = Math.Clamp(tx, sx + 8, Math.Max(sx + 8, sx + vw - tipW - 8));
            int ty = Math.Clamp(box.rect.Top - 4, sy + 32, Math.Max(sy + 32, sy + vh - tipH - 8));

            var tipRect = new RectangleF(tx, ty, tipW, tipH);
            WarGdi.DrawPlaqueFrame(g, tipRect, 7f, Color.FromArgb(248, 25, 22, 18), GoldCol, Color.FromArgb(72, GoldCol));

            using var brTitle = new SolidBrush(GoldCol);
            using var brMeta = new SolidBrush(done ? GoodGreen : active ? GoldCol : (avail && !canAfford) ? BadRed : branchCol);
            using var brFee = new SolidBrush(paid || done ? GoodGreen : canAfford ? Color.FromArgb(236, 212, 152) : BadRed);
            using var brDesc = new SolidBrush(TextMain);
            using var brHint = new SolidBrush(TextSub);

            g.DrawString($"[{brName}] {t.Name}（{t.Year}）", titleF, brTitle, new RectangleF(tx + 12, ty + 9, tipW - 24, 22));
            g.DrawString(stateStr, metaF, brMeta, new RectangleF(tx + 12, ty + 31, tipW - 24, 18));
            g.DrawString(feeStr, hintF, brFee, new RectangleF(tx + 12, ty + 50, tipW - 24, 18));

            float descY = ty + 72;
            g.DrawString(t.Desc, descF, brDesc, new RectangleF(tx + 12, descY, tipW - 24, descSz.Height + 2));
            float reqY = descY + descSz.Height + 6;
            g.DrawString(reqStr, hintF, brHint, new RectangleF(tx + 12, reqY, tipW - 24, reqSz.Height + 2));
            float hintY = tipRect.Bottom - 22;
            using var divPen = new Pen(Color.FromArgb(65, 162, 140, 96), 1f);
            g.DrawLine(divPen, tx + 12, hintY - 3, tx + tipW - 12, hintY - 3);
            g.DrawString(hintStr, hintF, canAfford || done ? brFee : brMeta, new RectangleF(tx + 12, hintY, tipW - 24, 16));
        }

        private void TechCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (State == null || _techCanvas == null) return;
            if (_techHitBoxes.Count == 0) ComputeTechHitBoxes();

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.FromArgb(22, 20, 17));

            // 淡金六角圖桌背景網格線
            using (var draftPen = new Pen(Color.FromArgb(12, 212, 188, 134), 1f))
            {
                for (int gx = 48; gx < _techCanvas.Width; gx += 48)
                    g.DrawLine(draftPen, gx, 0, gx, _techCanvas.Height);
                for (int gy = 48; gy < _techCanvas.Height; gy += 48)
                    g.DrawLine(draftPen, 0, gy, _techCanvas.Width, gy);
            }

            var branchSpec = TechBranchSpecs.First(x => x.key == _techBranchTab);
            var boxById = _techHitBoxes.ToDictionary(x => x.id);
            string focusId = _hoverTechId ?? _selectedTechId;

            int wrapW = _techScrollWrap.ViewportWidth > 100 ? _techScrollWrap.ViewportWidth : 1240;
            int innerW = Math.Max(1040, wrapW - 8);
            int nodeW = Math.Clamp((innerW - 24 - 4 * 20) / 5, 190, 232);
            int colGap = Math.Max(20, (innerW - 24 - 5 * nodeW) / 4);

            // 頂部五時代卷軸標題橫條
            string[] eraNames = { "T0 · 初創奠基", "T1 · 戰力成型", "T2 · 越海攻防", "T3 · 自研現代化", "T4 · 九〇尖端" };
            using var eraFont = new Font("Microsoft JhengHei UI", 8.2f, FontStyle.Bold);
            using var eraBrush = new SolidBrush(GoldCol);
            using var lanePen = new Pen(Color.FromArgb(26, 152, 132, 92), 1f) { DashStyle = DashStyle.Dot };
            for (int c = 0; c < 5; c++)
            {
                int cx = 12 + c * (nodeW + colGap);
                var hdrRect = new RectangleF(cx, 6, nodeW, 22);
                WarGdi.DrawPlaqueFrame(g, hdrRect, 4f, Color.FromArgb(34, 30, 24), Color.FromArgb(84, 72, 52), Color.FromArgb(48, 42, 32));
                g.DrawString(eraNames[c], eraFont, eraBrush, cx + 10, 9);
                if (c < 4)
                {
                    int divX = cx + nodeW + colGap / 2;
                    g.DrawLine(lanePen, divX, 6, divX, _techCanvas.Height - 8);
                }
            }

            const float hexR = 25.5f;
            float HexCenterX((string id, Rectangle rect, Rectangle btnResearch) b) => b.rect.X + b.rect.Width * 0.5f;
            float HexCenterY((string id, Rectangle rect, Rectangle btnResearch) b) => b.rect.Y + 29f;

            // 平滑貝茲曲線相依連線（直接連至六邊形左右頂點）
            foreach (var box in _techHitBoxes)
            {
                if (!GameData.Techs.TryGetValue(box.id, out var t) || t.Req == null) continue;
                foreach (var reqId in t.Req)
                {
                    if (!boxById.TryGetValue(reqId, out var pBox)) continue;
                    bool focusEdge = focusId == t.Id || focusId == reqId;
                    bool bothDone = State.Techs.Contains(t.Id) && State.Techs.Contains(reqId);
                    bool parentDone = State.Techs.Contains(reqId);
                    Color lineCol = focusEdge ? GoldCol
                                  : bothDone ? GoodGreen
                                  : parentDone ? branchSpec.col
                                  : Color.FromArgb(72, 63, 50);

                    using var pen = new Pen(lineCol, focusEdge ? 2.4f : (parentDone ? 1.85f : 1.2f));
                    if (!parentDone && !focusEdge) pen.DashStyle = DashStyle.Dash;

                    float x1 = HexCenterX(pBox) + hexR;
                    float y1 = HexCenterY(pBox);
                    float x2 = HexCenterX(box) - hexR;
                    float y2 = HexCenterY(box);
                    float span = Math.Max(20f, (x2 - x1) * 0.52f);
                    g.DrawBezier(pen, x1, y1, x1 + span, y1, x2 - span, y2, x2, y2);
                    using var dotBr = new SolidBrush(lineCol);
                    g.FillEllipse(dotBr, x2 - 3.5f, y2 - 3f, 6.5f, 6.5f);
                }
            }

            using var nameF = new Font("Microsoft JhengHei UI", 8.5f, FontStyle.Bold);
            using var subF = new Font("Microsoft JhengHei UI", 7.3f, FontStyle.Bold);
            using var badgeF = new Font("Microsoft JhengHei UI", 6.9f, FontStyle.Bold);
            using var centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

            foreach (var box in _techHitBoxes)
            {
                var t = GameData.Techs[box.id];
                var r = box.rect;
                bool done = State.Techs.Contains(t.Id);
                bool active = SimEngine.IsResearching(State, t.Id);
                bool avail = SimEngine.TechAvailable(State, t);
                double prog = SimEngine.SavedResearchProg(State, t.Id);
                double fee = SimEngine.TechStartCost(State, t);
                bool paid = State.HasFlag("techPaid_" + t.Id);
                bool canAfford = paid || State.Treasury + 1e-9 >= fee;
                bool isHover = _hoverTechId == t.Id;
                bool isSel = _selectedTechId == t.Id;

                float cx = HexCenterX(box);
                float cy = HexCenterY(box);
                var hexPts = TechHexPoints(cx, cy, hexR);
                var innerHexPts = TechHexPoints(cx, cy, hexR - 4f);

                Color hexFill = done ? Color.FromArgb(34, 62, 42)
                              : active ? Color.FromArgb(68, 52, 24)
                              : avail ? Color.FromArgb(38, 42, 46)
                              : Color.FromArgb(30, 27, 23);
                Color hexBorder = isHover || isSel ? GoldCol
                                : done ? GoodGreen
                                : active ? GoldCol
                                : avail ? (canAfford ? branchSpec.col : BadRed)
                                : Color.FromArgb(72, 64, 52);

                if (isHover || active)
                {
                    var glowPts = TechHexPoints(cx, cy, hexR + 3.5f);
                    using var glowPen = new Pen(Color.FromArgb(95, hexBorder), 2.5f);
                    g.DrawPolygon(glowPen, glowPts);
                }

                using (var hBr = new SolidBrush(hexFill)) g.FillPolygon(hBr, hexPts);
                using (var hPen = new Pen(hexBorder, isHover || active ? 2.2f : 1.7f)) g.DrawPolygon(hPen, hexPts);
                using (var iPen = new Pen(Color.FromArgb(55, hexBorder), 1f)) g.DrawPolygon(iPen, innerHexPts);

                Color iconInk = done ? GoodGreen
                              : active ? GoldCol
                              : avail ? (canAfford ? Color.FromArgb(236, 232, 218) : Color.FromArgb(220, 128, 120))
                              : Color.FromArgb(116, 108, 94);
                DrawTechVectorIcon(g, t.Id, cx, cy, iconInk, Color.FromArgb(56, iconInk));

                if (active)
                {
                    double ratio = prog / Math.Max(1.0, t.Cost);
                    DrawTechProgressHexRing(g, cx, cy, hexR + 0.5f, ratio, GoldCol);
                }

                // 跨分支前置精簡角標（不佔卡面正文）
                if (t.Req != null)
                {
                    var cross = t.Req.Where(rid => GameData.Techs.TryGetValue(rid, out var rt) && rt.Branch != t.Branch).ToList();
                    if (cross.Count > 0)
                    {
                        bool crossOk = cross.All(rid => State.Techs.Contains(rid));
                        Color cCol = crossOk ? GoodGreen : GoldCol;
                        using var cBr = new SolidBrush(cCol);
                        g.DrawString("跨科", badgeF, cBr, cx - hexR - 28, cy - 22);
                    }
                }

                // 右上角狀態徽章
                string cornerMark = done ? "✔" : active ? "★" : avail ? (canAfford ? "▶" : "$") : "";
                if (!string.IsNullOrEmpty(cornerMark))
                {
                    var mRect = new RectangleF(cx + hexR - 10, cy - hexR + 2, 22, 15);
                    using var mBg = new SolidBrush(done ? GoodGreen : active ? GoldCol : canAfford ? branchSpec.col : BadRed);
                    using var mFg = new SolidBrush(Color.FromArgb(20, 18, 14));
                    using var mPath = WarGdi.CreateRoundedPath(mRect, 4f);
                    g.FillPath(mBg, mPath);
                    TextRenderer.DrawText(g, cornerMark, badgeF, Rectangle.Round(mRect), Color.FromArgb(18, 16, 12),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // 六邊形下方精簡兩行標題與價格/天數標籤
                Color titleCol = done ? GoodGreen : active ? GoldCol : avail ? TextMain : Color.FromArgb(142, 132, 114);
                using (var tBr = new SolidBrush(titleCol))
                    g.DrawString(t.Name, nameF, tBr, new RectangleF(r.X + 4, cy + hexR + 3, r.Width - 8, 18), centerFmt);

                int days = SimEngine.EstDaysRemaining(State, t.Id);
                string subTxt;
                Color subCol;
                if (done)
                {
                    subTxt = $"✔ {t.Year}";
                    subCol = Color.FromArgb(132, 206, 148);
                }
                else if (active)
                {
                    subTxt = $"研發 {Math.Clamp(prog / Math.Max(1.0, t.Cost) * 100.0, 0, 100):F0}% · {days}天";
                    subCol = GoldCol;
                }
                else if (avail)
                {
                    subTxt = paid
                        ? $"已撥款 · {days}天"
                        : canAfford ? $"${fee:F2}億 · {days}天" : $"缺資金 ${fee:F2}億";
                    subCol = canAfford ? Color.FromArgb(174, 218, 224) : BadRed;
                }
                else
                {
                    subTxt = prog > 0.001 ? $"暫存 {prog:F1}/{t.Cost:F0}" : "前置未達成";
                    subCol = Color.FromArgb(118, 110, 96);
                }
                using (var sBr = new SolidBrush(subCol))
                    g.DrawString(subTxt, subF, sBr, new RectangleF(r.X + 4, cy + hexR + 21, r.Width - 8, 16), centerFmt);
            }

            // 滑鼠懸停時直接於六邊形旁顯示完整詳細說明浮動卡
            if (!string.IsNullOrEmpty(_hoverTechId) && boxById.TryGetValue(_hoverTechId, out var hBox))
            {
                DrawTechHoverTooltip(g, hBox, branchSpec.col);
            }
        }

        private void TechCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            string hit = null;
            string btnHit = null;
            foreach (var box in _techHitBoxes)
            {
                if (!box.btnResearch.IsEmpty && box.btnResearch.Contains(e.Location))
                {
                    hit = box.id;
                    btnHit = box.id;
                    break;
                }
                if (box.rect.Contains(e.Location))
                {
                    hit = box.id;
                    break;
                }
            }
            if (hit != _hoverTechId || btnHit != _hoverTechBtnId)
            {
                _hoverTechId = hit;
                _hoverTechBtnId = btnHit;
                _techCanvas.Invalidate();
                UpdateTechDetailLabels();
            }
        }

        private void TechCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) return;
            HandleTechCanvasClick(e.Location, e.Button == MouseButtons.Right);
        }

        private void HandleTechCanvasClick(Point pt, bool rightClick)
        {
            if (State == null) return;
            foreach (var box in _techHitBoxes)
            {
                if (!box.btnResearch.IsEmpty && box.btnResearch.Contains(pt))
                {
                    var res = SimEngine.ClickTechAction(State, box.id);
                    _selectedTechId = box.id;
                    var t = GameData.Techs[box.id];
                    double fee = SimEngine.TechStartCost(State, t);
                    _techFlashMsg = res.action switch
                    {
                        "no_funds" => $"✖ 國庫不足：立項研發【{t.Name}】需先撥付 ${fee:F2}億經費。",
                        "started" => $"✔ 已撥款並立即推進研發：【{t.Name}】（剩 {SimEngine.EstDaysRemaining(State, t.Id)} 天；目前共 {res.activeCount} 項並行，無須排程）",
                        _ => null
                    };
                    RefreshAllUi();
                    return;
                }
                if (box.rect.Contains(pt) && !rightClick)
                {
                    _techFlashMsg = null;
                    _selectedTechId = box.id;
                    _techCanvas.Invalidate();
                    UpdateTechDetailLabels();
                    return;
                }
            }
        }

        private void UpdateTechDetailLabels()
        {
            if (State == null || _techDetailTitleLbl == null) return;
            string tid = _hoverTechId ?? _selectedTechId ?? State.ResearchId;
            if (string.IsNullOrEmpty(tid) || !GameData.Techs.TryGetValue(tid, out var t))
                t = GameData.TechList.First(x => x.Branch == _techBranchTab);

            bool done = State.Techs.Contains(t.Id);
            bool active = SimEngine.IsResearching(State, t.Id);
            double prog = SimEngine.SavedResearchProg(State, t.Id);
            string status = done ? "✔ 已完成"
                : active ? $"★ 研發中 ({prog:F2}/{t.Cost:F0} · 剩 {SimEngine.EstDaysRemaining(State, t.Id)} 天)"
                : prog > 0.001 ? $"⏸ 已保留進度 ({prog:F2}/{t.Cost:F0} · {SimEngine.TechReqText(State, t)})"
                : SimEngine.TechReqText(State, t);
            string brName = TechBranchSpecs.FirstOrDefault(x => x.key == t.Branch).label ?? t.Branch;
            _techDetailTitleLbl.Text = $"[{brName}] {t.Name} ({t.Year})  |  需求 {t.Cost:F0} 研發點  |  {status}";

            if (!string.IsNullOrEmpty(_techFlashMsg))
            {
                _techDetailDescLbl.ForeColor = _techFlashMsg.StartsWith("✖") ? BadRed : GoodGreen;
                _techDetailDescLbl.Text = _techFlashMsg;
            }
            else
            {
                _techDetailDescLbl.ForeColor = TextMain;
                string reqNote = (t.Req != null && t.Req.Length > 0)
                    ? "  |  前置：" + string.Join("、", t.Req.Select(r => GameData.Techs[r].Name + (State.Techs.Contains(r) ? "(✔)" : "(未)")))
                    : "";
                _techDetailDescLbl.Text = $"{t.Desc}{reqNote}";
            }

            double fogPct = SimEngine.FogError(State) * 100.0;
            _techBranchBonusLbl.Text = $"陸{State.ArmyTech:F1} · 海{State.NavyTech:F1} · 空{State.AirTech:F1} · 戰鬥+{State.BattleBonus * 100:F0}%\n" +
                                        $"迷霧±{fogPct:F0}% · 運力{SimEngine.LiftCapacity(State):F0}師 · 吞吐{State.SupplyCap:F0}";
        }
    }
}
