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
        // 二戰復古兵棋圖桌配色體系（委託 WarPalette 統一管理）
        private static readonly Color BgDark = WarPalette.BgDark;
        private static readonly Color BgPanel = WarPalette.BgPanel;
        private static readonly Color BgCard = WarPalette.BgCard;
        private static readonly Color BgCardHi = WarPalette.BgCardHi;
        private static readonly Color BorderCol = WarPalette.BorderCol;
        private static readonly Color GoldCol = WarPalette.GoldCol;
        private static readonly Color GoldDim = WarPalette.GoldDim;
        private static readonly Color TextMain = WarPalette.TextMain;
        private static readonly Color TextSub = WarPalette.TextSub;
        private static readonly Color RocBlue = WarPalette.RocBlue;
        private static readonly Color PrcRed = WarPalette.PrcRed;
        private static readonly Color UssrPurple = WarPalette.UssrPurple;
        private static readonly Color GoodGreen = WarPalette.GoodGreen;
        private static readonly Color BadRed = WarPalette.BadRed;

        public GameState State { get; private set; }
        public string SelectedNode { get; private set; } = "TW";
        public string SelectedZone { get; private set; } = null;

        private string _hoverNode = null;
        private string _hoverZone = null;
        private string _hoverHudKey = null; // "treasury" | "stability" | "diplomacy"
        private Point _mousePos;
        private readonly HashSet<int> _checkedUnitIds = new();
        private HashSet<string> _unlockedEndings = new();
        private Dictionary<string, CheckpointRecord> _checkpoints = new();
        private readonly Dictionary<string, Button> _checkpointBtns = new();
        private readonly ToolTip _checkpointToolTip = new() { ShowAlways = true, InitialDelay = 350, ReshowDelay = 100 };
        private Image _titleBgImage;
        private readonly Dictionary<string, Image> _cardImages = new();
        private bool _showTitle = true;
        private bool _selectedEasyMode = false;
        private Button _btnModeNormal;
        private Button _btnModeEasy;
        private readonly HashSet<string> _selectedMutators = new();
        private readonly Dictionary<string, Button> _mutatorBtns = new();

        // 隨選右側戰情抽屜狀態："none" | "build"
        private string _activeDrawer = "none";
        private string _buildLoc = "TW";

        // 頂部流線型儀表列與底部工具帶
        private BufferedPanel _hudPanel;
        private string _noticeText;
        private DateTime _noticeAt;
        private int _noticeOrdinal;
        private Button _btnSave;
        private Button _btnLoad;
        private Button _btnMenu;
        private Button _btnPause;
        private Button _btnSpd1;
        private Button _btnSpd2;
        private Button _btnSpd4;
        private System.Windows.Forms.Timer _simTimer;
        private bool _userPaused = false;
        private int _simSpeed = 1;
        private RectangleF _hudRectTreasury;
        private RectangleF _hudRectStability;
        private RectangleF _hudRectDiplomacy;

        private BufferedPanel _dockBar;
        private readonly Dictionary<string, Button> _drawerBtns = new();
        private Label _lblDockTech;

        // 全幅 2D 地圖與懸浮式指揮小卡
        private BufferedPanel _mapPanel;
        private float _mapZoom = 1.0f;
        private float _mapPanX = 0.0f;
        private float _mapPanY = 0.0f;
        private bool _mapDragCandidate = false;
        private bool _mapDidDrag = false;
        private string _leftDragSource = null;
        private bool _leftDragCommand = false;
        private Point _mapDragStartPt;
        private float _mapDragStartPanX;
        private float _mapDragStartPanY;
        private RectangleF _zoomMinusRect;
        private RectangleF _zoomPlusRect;
        private RectangleF _zoomResetRect;
        private RectangleF _autoNavAirRect;
        private RectangleF _autoLandRect;
        private string _rightDragSource = null;
        private Point _rightDragStart;
        private bool _rightDragging = false;
        private WarRoomPanel _nodeCard;
        private Label _nodeTitleLbl;
        private Label _nodeStatsLbl;
        private Label _nodeSelCountLbl;
        private DarkScrollContainer _nodeChipsWrap;
        private FlowLayoutPanel _nodeChipsPanel;
        private Label _nodeOrderHintLbl;
        private Button _btnLocalRecruit, _btnLocalBuildInf, _btnNodePriority, _btnAirStrike, _btnNukeStrike;
        private readonly List<(RectangleF rect, List<int> unitIds, bool isPrep, string tip)> _orderBadgeHits = new();

        private WarRoomPanel _zoneCard;
        private Label _zoneTitleLbl;
        private Label _zoneStatsLbl;
        private Label _zoneFleetLbl;
        private Label _zoneAirLbl;
        private Button _btnFleetSub, _btnFleetAdd, _btnAirSub, _btnAirAdd;

        // 頂部 HUD 懸停明細氣泡
        private WarRoomPanel _hudTooltipCard;
        private Label _hudTooltipTitle;
        private Label _hudTooltipBody;

        // 右側滑出式戰情抽屜
        private WarRoomPanel _drawerPanel;
        private Label _drawerTitleLbl;
        private DarkScrollContainer _drawerScrollWrap;
        private Panel _drawerScrollBody;

        // 事件、結局與標題遮罩
        private WarRoomPanel _eventOverlay;
        private BufferedPanel _titleOverlay;

        // 五大國策科技樹專頁（開啟時自動暫停時間）
        private WarRoomPanel _techOverlay;
        private bool _techPageOpen = false;
        private string _techBranchTab = "science";
        private string _hoverTechId = null;
        private string _hoverTechBtnId = null;
        private string _selectedTechId = null;
        private string _techFlashMsg = null;
        private Label _techHdrSummaryLbl;
        private Label _techPauseBadgeLbl;
        private Label _techDetailTitleLbl;
        private Label _techDetailDescLbl;
        private Label _techBranchBonusLbl;
        private Button _techCloseBtn;
        private Button _techRushToggleBtn;
        private readonly Dictionary<string, Button> _techBranchTabBtns = new();
        private readonly Dictionary<int, Button> _techResLvBtns = new();
        private DarkScrollContainer _techScrollWrap;
        private BufferedPanel _techCanvas;
        private readonly List<(string id, Rectangle rect, Rectangle btnResearch)> _techHitBoxes = new();

        // 軍事情報處全版面專頁（◎ 軍情處）
        private WarRoomPanel _intelPageOverlay;
        private bool _intelPageOpen = false;
        private string _intelFilter = "all";
        private int _lastIntelRenderedCount = -1;
        private string _lastIntelRenderedFilter = "";
        private int _lastIntelCanvasWidth = 0;
        private Label _intelLevelBadgeLbl;
        private Label _intelPageSubLbl;
        private Button _intelCloseBtn;
        private Button _intelTechLinkBtn;
        private readonly Dictionary<string, Button> _intelFilterBtns = new();
        private readonly List<Panel> _intelKpiCards = new();
        private readonly List<Label> _intelKpiTitles = new();
        private readonly List<Label> _intelKpiBodies = new();
        private DarkScrollContainer _intelFeedScroll;
        private BufferedPanel _intelFeedCanvas;

        private static readonly (string key, string label, string sub, Color col)[] TechBranchSpecs =
        {
            ("science", "◆ 科技", "基礎科研與經濟基建", Color.FromArgb(198, 154, 72)),
            ("arms",    "★ 軍備", "三軍換裝與現代兵器", Color.FromArgb(196, 118, 62)),
            ("intel",   "◎ 情蒐", "雷達電偵與戰區透視", Color.FromArgb(86, 158, 148)),
            ("subv",    "◈ 策反", "心戰滲透與陣前起義", Color.FromArgb(154, 96, 134)),
            ("logi",    "⚓ 後勤", "兩棲運力與戰備補給", Color.FromArgb(102, 168, 112))
        };

        private static readonly string SavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savegame.json");
        private static readonly string EndingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "endings_unlocked.json");
        private static readonly string CheckpointsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "checkpoints.json");

        public MainForm()
        {
            Text = "《國光：反攻大陸》 — 2D 單機戰略模擬遊戲";
            Size = new Size(1600, 930);
            MinimumSize = new Size(1280, 760);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgDark;
            ForeColor = TextMain;
            Font = new Font("Microsoft JhengHei UI", 9.5f, FontStyle.Regular);
            KeyPreview = true;

            KeyDown += MainForm_KeyDown;

            LoadUnlockedEndings();
            LoadCheckpoints();
            LoadTitleImage();
            LoadCardImages();

            State = SimEngine.NewGame();
            BuildLayout();
            InitSimTimer();
            Application.AddMessageFilter(this);
            SelectNode("TW");
        }

        private void InitSimTimer()
        {
            _simTimer = new System.Windows.Forms.Timer { Interval = IntervalForSpeed(_simSpeed), Enabled = false };
            _simTimer.Tick += (_, _) => StepClockTick();
            SyncSimTimer();
            UpdateSpeedButtonsUi();
        }

        public bool IsSimTimerRunning => _simTimer?.Enabled ?? false;
        public bool IsUserPaused => _userPaused;
        public bool IsTechPageOpen => _techPageOpen;
        public bool IsIntelPageOpen => _intelPageOpen;
        public bool IsEventOverlayVisibleForTest => _eventOverlay?.Visible ?? false;
        public string ActiveIntelFilter => _intelFilter;
        public Rectangle IntelOverlayBounds => _intelPageOverlay?.Bounds ?? Rectangle.Empty;
        public Size MapPanelClientSize => _mapPanel?.ClientSize ?? Size.Empty;
        public int HudBarHeight => _hudPanel?.Height ?? 78;
        public string FrontNoticeTextForTest => _noticeText;
        public int DockBarHeight => _dockBar?.Height ?? 50;
        public string ActiveTechBranch => _techBranchTab;
        public string GetTechTabButtonTextForTest(string branchKey)
            => _techBranchTabBtns.TryGetValue(branchKey, out var b) ? b.Text : "";
        public bool HasClickableTechButtonForTest(string techId)
            => _techHitBoxes.Any(x => x.id == techId && !x.btnResearch.IsEmpty);
        public bool ClickTechNodeButtonForTest(string techId, bool rightClick = false)
        {
            var box = _techHitBoxes.FirstOrDefault(x => x.id == techId);
            if (string.IsNullOrEmpty(box.id) || box.btnResearch.IsEmpty) return false;
            var pt = new Point(box.btnResearch.X + box.btnResearch.Width / 2, box.btnResearch.Y + box.btnResearch.Height / 2);
            HandleTechCanvasClick(pt, rightClick);
            return true;
        }
        public void SetHoverTechForTest(string techId)
        {
            _hoverTechId = techId;
            _techCanvas?.Invalidate();
            UpdateTechDetailLabels();
        }
        public void ShowHudTooltipForTest(string key)
        {
            _hoverHudKey = key;
            UpdateHudTooltip();
        }
        public int SimSpeed => _simSpeed;
        public bool IsEasyMode => State?.EasyMode ?? _selectedEasyMode;
        private bool IsOverlayPausingTime => _intelPageOpen || _techPageOpen;
        private bool IsClockRunning => !_showTitle && !_userPaused && !IsOverlayPausingTime && State != null && State.Over == null && State.PendingEvents.Count == 0;

        public List<string> GetOpenEventCardBadgesForTest()
        {
            var res = new List<string>();
            if (_eventOverlay == null || !_eventOverlay.Visible) return res;
            foreach (Control c in _eventOverlay.Controls)
                if (c is BufferedPanel && !string.IsNullOrEmpty(c.AccessibleDescription))
                    res.Add(c.AccessibleDescription);
            return res;
        }

        public void SetGameMode(bool easyMode)
        {
            _selectedEasyMode = easyMode;
            if (State != null) State.EasyMode = easyMode;
            UpdateModeButtonsUi();
            _hudPanel?.Invalidate();
            if (!_showTitle && !_intelPageOpen && State?.PendingEvents.Count > 0 && State.Over == null)
            {
                ShowEventOverlay(State.PendingEvents[0]);
            }
        }

        private static int IntervalForSpeed(int spd) => spd switch
        {
            4 => 50,
            2 => 110,
            _ => 240
        };

        private void SyncSimTimer()
        {
            if (_simTimer == null) return;
            _simTimer.Interval = IntervalForSpeed(_simSpeed);
            bool shouldRun = IsClockRunning;
            if (shouldRun && !_simTimer.Enabled) _simTimer.Start();
            else if (!shouldRun && _simTimer.Enabled) _simTimer.Stop();
        }

        public void TogglePause()
        {
            if (_showTitle || IsOverlayPausingTime || State == null || State.Over != null || State.PendingEvents.Count > 0) return;
            _userPaused = !_userPaused;
            SyncSimTimer();
            UpdateSpeedButtonsUi();
            if (_techPageOpen) RefreshTechOverlay();
            _hudPanel?.Invalidate();
        }

        public void SetSimSpeed(int spd)
        {
            _simSpeed = (spd == 4 || spd == 2) ? spd : 1;
            if (_userPaused && !_showTitle && !IsOverlayPausingTime && State?.Over == null && State?.PendingEvents.Count == 0)
                _userPaused = false;
            SyncSimTimer();
            UpdateSpeedButtonsUi();
            if (_techPageOpen) RefreshTechOverlay();
            _hudPanel?.Invalidate();
        }

        private static void SetWarButtonStyle(Button b, Color back, Color fore, Color border)
        {
            if (b == null) return;
            b.BackColor = back;
            b.ForeColor = fore;
            b.FlatAppearance.BorderColor = border;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, back.R + 22),
                Math.Min(255, back.G + 24),
                Math.Min(255, back.B + 28));
            b.Invalidate();
        }

        private void UpdateSpeedButtonsUi()
        {
            if (_btnPause == null) return;
            bool blocked = State == null || State.Over != null || State.PendingEvents.Count > 0 || IsOverlayPausingTime;
            _btnPause.Enabled = !blocked;
            _btnSpd1.Enabled = !blocked;
            _btnSpd2.Enabled = !blocked;
            _btnSpd4.Enabled = !blocked;

            if (State?.Over != null)
            {
                _btnPause.Text = "■ 結束";
                SetWarButtonStyle(_btnPause, Color.FromArgb(48, 42, 34), TextSub, BorderCol);
            }
            else if (State != null && State.PendingEvents.Count > 0)
            {
                _btnPause.Text = "▌▌ 事件";
                SetWarButtonStyle(_btnPause, Color.FromArgb(94, 68, 28), GoldCol, GoldCol);
            }
            else if (_techPageOpen)
            {
                _btnPause.Text = "▌▌ 科技";
                SetWarButtonStyle(_btnPause, Color.FromArgb(46, 72, 92), GoldCol, GoldCol);
            }
            else if (_intelPageOpen)
            {
                _btnPause.Text = "▌▌ 軍情";
                SetWarButtonStyle(_btnPause, Color.FromArgb(46, 72, 92), GoldCol, GoldCol);
            }
            else if (_userPaused)
            {
                _btnPause.Text = "▶ 繼續";
                SetWarButtonStyle(_btnPause, Color.FromArgb(52, 108, 66), TextMain, GoodGreen);
            }
            else
            {
                _btnPause.Text = "▌▌ 暫停";
                SetWarButtonStyle(_btnPause, Color.FromArgb(148, 102, 34), TextMain, GoldCol);
            }

            SetWarButtonStyle(_btnSpd1, _simSpeed == 1 ? RocBlue : BgCard, _simSpeed == 1 ? TextMain : TextSub, _simSpeed == 1 ? GoldCol : BorderCol);
            SetWarButtonStyle(_btnSpd2, _simSpeed == 2 ? RocBlue : BgCard, _simSpeed == 2 ? TextMain : TextSub, _simSpeed == 2 ? GoldCol : BorderCol);
            SetWarButtonStyle(_btnSpd4, _simSpeed == 4 ? RocBlue : BgCard, _simSpeed == 4 ? TextMain : TextSub, _simSpeed == 4 ? GoldCol : BorderCol);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space && !_showTitle && !IsOverlayPausingTime && State?.Over == null && State?.PendingEvents.Count == 0)
            {
                TogglePause();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.T && !_showTitle && State?.Over == null && State?.PendingEvents.Count == 0)
            {
                ToggleTechPage();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.I && !_showTitle && State?.Over == null && State?.PendingEvents.Count == 0)
            {
                ToggleIntelPage();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.R && CanInteractMap() && _mapZoom > 1.001f)
            {
                ResetMapZoom();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                if (_techPageOpen)
                {
                    CloseTechPage();
                    e.Handled = true;
                }
                else if (_intelPageOpen)
                {
                    CloseIntelPage();
                    e.Handled = true;
                }
                else if (_activeDrawer != "none")
                {
                    CloseDrawer();
                    e.Handled = true;
                }
                else if (SelectedZone != null)
                {
                    SelectedZone = null;
                    RefreshFloatingCards();
                    _mapPanel.Invalidate();
                    e.Handled = true;
                }
                else if (_mapZoom > 1.001f)
                {
                    ResetMapZoom();
                    e.Handled = true;
                }
            }
        }

        // ==================== 通用自繪銘牌框與圓角黃銅按鈕 ====================
        public static GraphicsPath CreateChamferPath(RectangleF r, float c)
            => WarGdi.CreateRoundedPath(r, Math.Clamp(c * 0.72f, 4f, 11f));

        public static void DrawChamferFrame(Graphics g, RectangleF r, float chamfer, Color fillCol, Color outerCol, Color innerCol)
            => WarGdi.DrawPlaqueFrame(g, r, Math.Clamp(chamfer * 0.72f, 3.5f, 11f), fillCol, outerCol, innerCol, drawRivets: false);

        private Button CreateWarButton(string text, Color back, Color fore, int w, int h, Color? borderOverride = null)
        {
            Color bCol = borderOverride ?? BorderCol;
            var b = new WarButton
            {
                Text = text,
                Size = new Size(w, h),
                BackColor = back,
                ForeColor = fore,
                Margin = new Padding(2)
            };
            b.FlatAppearance.BorderColor = bCol;
            return b;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _simTimer?.Stop();
            _simTimer?.Dispose();
            _simTimer = null;
            _checkpointToolTip.Dispose();
            _titleBgImage?.Dispose();
            foreach (var img in _cardImages.Values) img.Dispose();
            _cardImages.Clear();
            base.OnFormClosed(e);
        }

        private static string FindAssetFile(params string[] relParts)
        {
            var roots = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Directory.GetCurrentDirectory(),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..")
            };
            foreach (var root in roots)
            {
                var p = Path.Combine(new[] { root }.Concat(relParts).ToArray());
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private void LoadTitleImage()
        {
            try
            {
                var p = FindAssetFile("assets", "title.jpg");
                if (p != null) _titleBgImage = Image.FromFile(p);
            }
            catch { _titleBgImage = null; }
        }

        private void LoadCardImages()
        {
            string[] keys = { "amphibious", "naval_air", "defense", "diplomacy", "economy", "underground", "uprising", "ussr_crisis" };
            foreach (var k in keys)
            {
                try
                {
                    var p = FindAssetFile("assets", "cards", $"card_{k}.jpg");
                    if (p != null) _cardImages[k] = Image.FromFile(p);
                }
                catch { }
            }
        }

        private static void DrawImageCover(Graphics g, Image img, RectangleF dest)
        {
            if (img == null || dest.Width <= 0 || dest.Height <= 0) return;
            float imgAspect = (float)img.Width / img.Height;
            float dstAspect = dest.Width / dest.Height;
            RectangleF src;
            if (imgAspect > dstAspect)
            {
                float sw = img.Height * dstAspect;
                src = new RectangleF((img.Width - sw) * 0.5f, 0, sw, img.Height);
            }
            else
            {
                float sh = img.Width / dstAspect;
                src = new RectangleF(0, (img.Height - sh) * 0.5f, img.Width, sh);
            }
            var prevInterp = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, dest, src, GraphicsUnit.Pixel);
            g.InterpolationMode = prevInterp;
        }

        private void LoadUnlockedEndings()
        {
            try
            {
                if (File.Exists(EndingsPath))
                {
                    var list = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(EndingsPath));
                    if (list != null) _unlockedEndings = new HashSet<string>(list);
                }
            }
            catch { }
        }

        private void RecordEnding(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            _unlockedEndings.Add(key);
            try { File.WriteAllText(EndingsPath, JsonSerializer.Serialize(_unlockedEndings.ToList())); } catch { }
        }

        public void LoadCheckpoints()
        {
            try
            {
                if (File.Exists(CheckpointsPath))
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, CheckpointRecord>>(File.ReadAllText(CheckpointsPath));
                    _checkpoints = dict ?? new Dictionary<string, CheckpointRecord>();
                }
                else
                {
                    _checkpoints = new Dictionary<string, CheckpointRecord>();
                }
            }
            catch { _checkpoints = new Dictionary<string, CheckpointRecord>(); }
        }

        private void SaveCheckpoints()
        {
            try
            {
                File.WriteAllText(CheckpointsPath, JsonSerializer.Serialize(_checkpoints, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public void CheckAndRecordCheckpoints()
        {
            var newKeys = SimEngine.EvaluateCheckpoints(State, _checkpoints);
            if (_checkpoints.Count > 0) SaveCheckpoints();
            if (_titleOverlay != null) RefreshTitleOverlay();
        }

        public bool StartFromCheckpoint(string cpKey)
        {
            if (!_checkpoints.TryGetValue(cpKey, out var rec) || string.IsNullOrEmpty(rec.StateJson))
                return false;
            try
            {
                var loaded = JsonSerializer.Deserialize<GameState>(rec.StateJson);
                if (loaded == null) return false;
                if (loaded.IntelReports == null) loaded.IntelReports = new();
                if (loaded.IntelReports.Count == 0) SimEngine.SampleIntelReports(loaded, isOpening: true);
                loaded.ResearchProgMap ??= new();
                loaded.ActiveTechs ??= new();
                loaded.ResearchQueue ??= new();
                SimEngine.Normalize(loaded);
                SimEngine.RollFog(loaded);
                loaded.EasyMode = _selectedEasyMode;
                State = loaded;
                _checkedUnitIds.Clear();
                SelectedZone = null;
                SelectedNode = "TW";
                foreach (var u in SimEngine.UnitsAt(State, "TW", UnitDomain.Land).Where(u => !u.Moved).OrderByDescending(u => u.Str).Take(3))
                    _checkedUnitIds.Add(u.Id);
                _activeDrawer = "none";
                _showTitle = false;
                _userPaused = false;
                _techPageOpen = false;
                _intelPageOpen = false;
                _lastIntelRenderedCount = -1;
                _selectedTechId = null;
                _hoverTechId = null;
                _hoverTechBtnId = null;
                _techFlashMsg = null;
                _mapZoom = 1.0f;
                _mapPanX = 0.0f;
                _mapPanY = 0.0f;
                if (_techOverlay != null) _techOverlay.Visible = false;
                if (_intelPageOverlay != null) _intelPageOverlay.Visible = false;
                _drawerPanel.Visible = false;
                UpdateOverlays();
                RefreshAllUi();
                return true;
            }
            catch { return false; }
        }

        // ==================== 版面建構（二戰復古兵棋桌：核桃木邊框＋全幅戰圖） ====================
        private void BuildLayout()
        {
            // 1. 頂部核桃木與黃銅儀表列（56px 儀表 + 22px 戰地電訊）
            _hudPanel = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.FromArgb(26, 23, 19)
            };
            _hudPanel.Paint += HudPanel_Paint;
            _hudPanel.MouseMove += HudPanel_MouseMove;
            _hudPanel.MouseLeave += (_, _) =>
            {
                if (_hoverHudKey != null)
                {
                    _hoverHudKey = null;
                    _hudTooltipCard.Visible = false;
                    _hudPanel.Invalidate();
                }
            };

            _btnPause = CreateWarButton("▌▌ 暫停", Color.FromArgb(148, 102, 34), TextMain, 84, 34, GoldCol);
            _btnPause.Font = new Font("Microsoft JhengHei UI", 9.0f, FontStyle.Bold);
            _btnPause.Click += (_, _) => TogglePause();

            _btnSpd1 = CreateWarButton("1×", RocBlue, TextMain, 38, 34, GoldCol);
            _btnSpd1.Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
            _btnSpd1.Click += (_, _) => SetSimSpeed(1);

            _btnSpd2 = CreateWarButton("2×", BgCard, TextSub, 38, 34);
            _btnSpd2.Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
            _btnSpd2.Click += (_, _) => SetSimSpeed(2);

            _btnSpd4 = CreateWarButton("4×", BgCard, TextSub, 38, 34);
            _btnSpd4.Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
            _btnSpd4.Click += (_, _) => SetSimSpeed(4);

            _btnMenu = CreateWarButton("選單", BgCard, GoldCol, 64, 34, GoldDim);
            _btnMenu.Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
            _btnMenu.Click += (_, _) => { _showTitle = true; UpdateOverlays(); SyncSimTimer(); };

            _btnSave = CreateWarButton("存檔", BgCard, TextMain, 58, 34);
            _btnSave.Font = new Font("Microsoft JhengHei UI", 8.8f);
            _btnSave.Click += (_, _) => SaveGame();

            _btnLoad = CreateWarButton("讀檔", BgCard, TextMain, 58, 34);
            _btnLoad.Font = new Font("Microsoft JhengHei UI", 8.8f);
            _btnLoad.Click += (_, _) => LoadGame();

            _hudPanel.Controls.AddRange(new Control[] { _btnPause, _btnSpd1, _btnSpd2, _btnSpd4, _btnMenu, _btnSave, _btnLoad });
            _hudPanel.Resize += (_, _) => LayoutHudButtons();
            LayoutHudButtons();

            // 2. 底部復古黃銅戰情導航列 (50px)
            _dockBar = new BufferedPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(26, 23, 19)
            };
            _dockBar.Paint += DockBar_Paint;

            var drawerSpecs = new (string key, string label)[]
            {
                ("build", "⚔ 建軍後勤"),
                ("tech",  "⚙ 科技樹"),
                ("mi",    "◎ 軍情處")
            };
            int dx = 14;
            foreach (var (key, label) in drawerSpecs)
            {
                var btn = CreateWarButton(label, BgCard, TextMain, 108, 34);
                btn.Font = new Font("Microsoft JhengHei UI", 9.0f, FontStyle.Bold);
                btn.Location = new Point(dx, 8);
                string kCopy = key;
                btn.Click += (_, _) => ToggleDrawer(kCopy);
                _drawerBtns[key] = btn;
                _dockBar.Controls.Add(btn);
                dx += 115;
            }

            _lblDockTech = new Label
            {
                ForeColor = TextSub,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold),
                AutoSize = false,
                Height = 34,
                TextAlign = ContentAlignment.MiddleRight
            };
            _dockBar.Controls.Add(_lblDockTech);
            _dockBar.Resize += (_, _) => LayoutDockBar();
            LayoutDockBar();

            // 3. 中央全幅 2D 戰略地圖畫布（佔比 88%+）
            _mapPanel = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark
            };
            _mapPanel.Paint += MapPanel_Paint;
            _mapPanel.MouseMove += MapPanel_MouseMove;
            _mapPanel.MouseDown += MapPanel_MouseDown;
            _mapPanel.MouseUp += MapPanel_MouseUp;
            _mapPanel.Resize += (_, _) => { ClampMapPan(); LayoutFloatingMapPanels(); };

            // 4. 懸浮左下「地面戰區與兵牌指戰卡」（取代左欄常駐面板與 CheckedListBox）
            BuildNodeCommandCard();

            // 5. 懸浮右側海域旁「海空兵力快速配屬卡」（取代 50 顆調防按鈕）
            BuildZoneCommandCard();

            // 6. 頂部 HUD 懸停明細氣泡卡
            BuildHudTooltipCard();

            // 7. 右側滑出式自繪戰情抽屜（取代原生白色 TabControl）
            BuildDrawerPanel();

            // 8. 機密急電式事件與結局彈窗
            _eventOverlay = new WarRoomPanel
            {
                Visible = false,
                BackColor = Color.FromArgb(248, 30, 27, 22),
                FrameBorderColor = GoldCol,
                InnerBorderColor = Color.FromArgb(88, 74, 52),
                Chamfer = 14
            };

            // 9. 五大國策科技樹全幅專頁（開啟時自動暫停時間）
            BuildTechOverlay();

            // 9B. 軍事情報處全版面專頁（◎ 軍情處）
            BuildIntelPageOverlay();

            // 10. 標題主選單遮罩層
            _titleOverlay = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                Visible = true,
                BackColor = BgDark
            };
            _titleOverlay.Paint += TitleOverlay_Paint;
            BuildTitleOverlayControls();

            _mapPanel.Controls.AddRange(new Control[]
            {
                _nodeCard, _zoneCard, _hudTooltipCard, _drawerPanel, _intelPageOverlay, _techOverlay, _eventOverlay
            });

            Controls.Add(_mapPanel);
            Controls.Add(_dockBar);
            Controls.Add(_hudPanel);
            Controls.Add(_titleOverlay);
            // 明確鎖定停駐優先序（WinForms 由高索引往低索引計算 Dock，故 Fill 必須在 Bottom / Top 之前即較小索引）
            Controls.SetChildIndex(_titleOverlay, 0);
            Controls.SetChildIndex(_mapPanel, 1);
            Controls.SetChildIndex(_dockBar, 2);
            Controls.SetChildIndex(_hudPanel, 3);
        }

        private void LayoutHudButtons()
        {
            int right = _hudPanel.Width - 12;
            _btnSpd4.Location = new Point(right - _btnSpd4.Width, 11);
            right = _btnSpd4.Left - 4;
            _btnSpd2.Location = new Point(right - _btnSpd2.Width, 11);
            right = _btnSpd2.Left - 4;
            _btnSpd1.Location = new Point(right - _btnSpd1.Width, 11);
            right = _btnSpd1.Left - 4;
            _btnPause.Location = new Point(right - _btnPause.Width, 11);
            right = _btnPause.Left - 12;
            _btnMenu.Location = new Point(right - _btnMenu.Width, 11);
            right = _btnMenu.Left - 5;
            _btnLoad.Location = new Point(right - _btnLoad.Width, 11);
            right = _btnLoad.Left - 5;
            _btnSave.Location = new Point(right - _btnSave.Width, 11);
        }

        private void LayoutDockBar()
        {
            int lastBtnRight = _drawerBtns.Values.Count > 0 ? _drawerBtns.Values.Max(b => b.Right) : 360;
            int startX = Math.Max(380, lastBtnRight + 20);
            _lblDockTech.Location = new Point(startX, 8);
            _lblDockTech.Width = Math.Max(120, _dockBar.Width - startX - 16);
        }

        private void BuildNodeCommandCard()
        {
            _nodeCard = new WarRoomPanel
            {
                Size = new Size(446, 250),
                BackColor = Color.FromArgb(246, 30, 27, 22),
                FrameBorderColor = GoldCol,
                InnerBorderColor = Color.FromArgb(78, 66, 48),
                Chamfer = 10,
                Visible = false
            };

            _nodeTitleLbl = new Label
            {
                Text = "台灣本島（台北）",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 10.2f, FontStyle.Bold),
                Location = new Point(14, 9),
                Size = new Size(330, 22)
            };

            var btnCloseNode = CreateWarButton("✕", BgCard, TextSub, 26, 22);
            btnCloseNode.Font = new Font("Microsoft JhengHei UI", 8f, FontStyle.Bold);
            btnCloseNode.Location = new Point(408, 8);
            btnCloseNode.Click += (_, _) => { SelectedNode = null; _checkedUnitIds.Clear(); RefreshFloatingCards(); _mapPanel.Invalidate(); };

            _btnNodePriority = CreateWarButton("☆ 重點", BgCard, TextSub, 56, 22);
            _btnNodePriority.Font = new Font("Microsoft JhengHei UI", 7.4f, FontStyle.Bold);
            _btnNodePriority.Location = new Point(348, 8);
            _btnNodePriority.Click += (_, _) =>
            {
                if (SelectedNode == null || State == null) return;
                if (State.PriorityTargets.Contains(SelectedNode))
                    State.PriorityTargets.Remove(SelectedNode);
                else
                    State.PriorityTargets.Add(SelectedNode);
                if (State.AutoFrontFill) SimEngine.AutoBalanceLandForces(State, quiet: true);
                RefreshAllUi();
            };

            _nodeStatsLbl = new Label
            {
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.3f),
                Location = new Point(14, 32),
                Size = new Size(418, 34)
            };

            var btn1 = CreateWarButton("1師", BgCard, TextMain, 40, 26);
            btn1.Font = new Font("Microsoft JhengHei UI", 8f, FontStyle.Bold);
            btn1.Location = new Point(14, 68);
            btn1.Click += (_, _) => SelectUnitsCount(1);

            var btn3 = CreateWarButton("3師", BgCard, TextMain, 40, 26);
            btn3.Font = new Font("Microsoft JhengHei UI", 8f, FontStyle.Bold);
            btn3.Location = new Point(58, 68);
            btn3.Click += (_, _) => SelectUnitsCount(3);

            var btnHalf = CreateWarButton("半數", BgCard, TextMain, 44, 26);
            btnHalf.Font = new Font("Microsoft JhengHei UI", 8f, FontStyle.Bold);
            btnHalf.Location = new Point(102, 68);
            btnHalf.Click += (_, _) => SelectUnitsFraction(0.5);

            var btnAll = CreateWarButton("全選", BgCard, TextMain, 44, 26);
            btnAll.Font = new Font("Microsoft JhengHei UI", 8f, FontStyle.Bold);
            btnAll.Location = new Point(150, 68);
            btnAll.Click += (_, _) => SelectUnitsFraction(1.0);

            var btnNone = CreateWarButton("撤令", BgCard, BadRed, 44, 26);
            btnNone.Font = new Font("Microsoft JhengHei UI", 8f, FontStyle.Bold);
            btnNone.Location = new Point(198, 68);
            btnNone.Click += (_, _) => CancelSelectedNodeOrders();

            _nodeSelCountLbl = new Label
            {
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.1f, FontStyle.Bold),
                Location = new Point(246, 71),
                Size = new Size(188, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            _nodeChipsWrap = new DarkScrollContainer
            {
                Location = new Point(12, 98),
                Size = new Size(422, 78),
                BackColor = Color.FromArgb(24, 21, 17),
                ShowVerticalBar = true,
                ShowHorizontalBar = false
            };

            _nodeChipsPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(24, 21, 17),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MaximumSize = new Size(_nodeChipsWrap.ViewportWidth, 0),
                MinimumSize = new Size(_nodeChipsWrap.ViewportWidth, 20),
                Padding = new Padding(4)
            };
            _nodeChipsWrap.SetContent(_nodeChipsPanel);

            _nodeOrderHintLbl = new Label
            {
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 7.9f, FontStyle.Bold),
                Location = new Point(14, 216),
                Size = new Size(418, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _btnLocalRecruit = CreateWarButton("接納民兵·併入國軍", BgCard, TextMain, 205, 28);
            _btnLocalRecruit.Font = new Font("Microsoft JhengHei UI", 7.8f, FontStyle.Bold);
            _btnLocalRecruit.Location = new Point(14, 183);
            _btnLocalRecruit.Click += (_, _) => ExecuteProvinceGarrisonAction();

            _btnLocalBuildInf = CreateWarButton("基地編成步兵", Color.FromArgb(46, 78, 56), TextMain, 205, 28, GoodGreen);
            _btnLocalBuildInf.Font = new Font("Microsoft JhengHei UI", 7.8f, FontStyle.Bold);
            _btnLocalBuildInf.Location = new Point(227, 183);
            _btnLocalBuildInf.Click += (_, _) =>
            {
                if (SelectedNode == null || State.PendingEvents.Count > 0 || State.Over != null) return;
                var res = SimEngine.BuildUnit(State, "inf", SelectedNode);
                if (!res.ok)
                    MessageBox.Show(res.reason, "就地生兵", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshAllUi();
            };

            _btnAirStrike = CreateWarButton("✈ 發動戰術空襲", Color.FromArgb(42, 68, 88), GoldCol, 418, 28, GoldCol);
            _btnAirStrike.Font = new Font("Microsoft JhengHei UI", 8.2f, FontStyle.Bold);
            _btnAirStrike.Location = new Point(14, 183);
            _btnAirStrike.Visible = false;
            _btnAirStrike.Click += (_, _) =>
            {
                if (SelectedNode == null || State.PendingEvents.Count > 0 || State.Over != null) return;
                var res = SimEngine.AirStrikeProvince(State, SelectedNode);
                if (!res.ok)
                    MessageBox.Show(res.reason, "戰術空襲", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshAllUi();
                _mapPanel.Invalidate();
            };

            _btnNukeStrike = CreateWarButton("☢ 戰術核打擊", Color.FromArgb(78, 28, 28), GoldCol, 205, 28, BadRed);
            _btnNukeStrike.Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
            _btnNukeStrike.Location = new Point(227, 183);
            _btnNukeStrike.Visible = false;
            _btnNukeStrike.Click += (_, _) =>
            {
                if (SelectedNode == null || State.PendingEvents.Count > 0 || State.Over != null) return;
                if (!GameData.Nodes.TryGetValue(SelectedNode, out var pNode)) return;
                if (State.NukeStockpile <= 0)
                {
                    MessageBox.Show("戰術核彈頭庫存不足（0 枚）。請等待核研所年度核組件產出，或研發相關核武科技！", "戰術核打擊", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var dr = MessageBox.Show(
                    $"確定對【{pNode.Name}】發射天馬戰術核飛彈？\n\n打擊效果：\n• 消滅該省 75% 守軍主力\n• 徹底摧毀防禦工事\n• 中共政權穩定度重挫 -18%\n• 消耗 1 枚戰術核彈頭（剩餘 {State.NukeStockpile - 1} 枚）",
                    "天馬戰術核打擊確認",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    SimEngine.NukeStrike(State, SelectedNode);
                    RefreshAllUi();
                    _mapPanel.Invalidate();
                }
            };

            _nodeCard.Controls.AddRange(new Control[]
            {
                _nodeTitleLbl, _btnNodePriority, btnCloseNode, _nodeStatsLbl,
                btn1, btn3, btnHalf, btnAll, btnNone, _nodeSelCountLbl,
                _nodeChipsWrap, _btnLocalRecruit, _btnLocalBuildInf, _btnAirStrike, _btnNukeStrike, _nodeOrderHintLbl
            });
        }

        private void ExecuteProvinceGarrisonAction()
        {
            if (SelectedNode == null || State.PendingEvents.Count > 0 || State.Over != null) return;
            var result = SimEngine.RecruitLocalMilitia(State, SelectedNode);
            if (!result.ok)
                MessageBox.Show(result.reason, "接納民兵", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshAllUi();
        }

        private void BuildZoneCommandCard()
        {
            _zoneCard = new WarRoomPanel
            {
                Size = new Size(346, 156),
                BackColor = Color.FromArgb(246, 30, 27, 22),
                FrameBorderColor = GoldCol,
                InnerBorderColor = Color.FromArgb(84, 96, 86),
                Chamfer = 10,
                Visible = false
            };

            _zoneTitleLbl = new Label
            {
                Text = "⚓ 海域配屬",
                ForeColor = Color.FromArgb(164, 212, 210),
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 10.2f, FontStyle.Bold),
                Location = new Point(14, 9),
                Size = new Size(288, 22)
            };

            var btnCloseZone = CreateWarButton("✕", BgCard, TextSub, 26, 22);
            btnCloseZone.Font = new Font("Microsoft JhengHei UI", 8f, FontStyle.Bold);
            btnCloseZone.Location = new Point(310, 8);
            btnCloseZone.Click += (_, _) => { SelectedZone = null; RefreshFloatingCards(); _mapPanel.Invalidate(); };

            _zoneStatsLbl = new Label
            {
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.5f),
                Location = new Point(14, 34),
                Size = new Size(318, 34)
            };

            _zoneFleetLbl = new Label
            {
                ForeColor = TextMain,
                BackColor = BgCard,
                Font = new Font("Microsoft JhengHei UI", 9f, FontStyle.Bold),
                Location = new Point(14, 74),
                Size = new Size(228, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            _btnFleetSub = CreateWarButton("－", Color.FromArgb(110, 48, 44), TextMain, 42, 30);
            _btnFleetSub.Font = new Font("Microsoft JhengHei UI", 10f, FontStyle.Bold);
            _btnFleetSub.Location = new Point(248, 74);
            _btnFleetSub.Click += (_, _) => AdjustSelectedZoneUnit("fleet", false);

            _btnFleetAdd = CreateWarButton("＋", RocBlue, TextMain, 42, 30);
            _btnFleetAdd.Font = new Font("Microsoft JhengHei UI", 10f, FontStyle.Bold);
            _btnFleetAdd.Location = new Point(294, 74);
            _btnFleetAdd.Click += (_, _) => AdjustSelectedZoneUnit("fleet", true);

            _zoneAirLbl = new Label
            {
                ForeColor = TextMain,
                BackColor = BgCard,
                Font = new Font("Microsoft JhengHei UI", 9f, FontStyle.Bold),
                Location = new Point(14, 110),
                Size = new Size(228, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            _btnAirSub = CreateWarButton("－", Color.FromArgb(110, 48, 44), TextMain, 42, 30);
            _btnAirSub.Font = new Font("Microsoft JhengHei UI", 10f, FontStyle.Bold);
            _btnAirSub.Location = new Point(248, 110);
            _btnAirSub.Click += (_, _) => AdjustSelectedZoneUnit("air", false);

            _btnAirAdd = CreateWarButton("＋", RocBlue, TextMain, 42, 30);
            _btnAirAdd.Font = new Font("Microsoft JhengHei UI", 10f, FontStyle.Bold);
            _btnAirAdd.Location = new Point(294, 110);
            _btnAirAdd.Click += (_, _) => AdjustSelectedZoneUnit("air", true);

            _zoneCard.Controls.AddRange(new Control[]
            {
                _zoneTitleLbl, btnCloseZone, _zoneStatsLbl,
                _zoneFleetLbl, _btnFleetSub, _btnFleetAdd,
                _zoneAirLbl, _btnAirSub, _btnAirAdd
            });
        }

        private void BuildHudTooltipCard()
        {
            _hudTooltipCard = new WarRoomPanel
            {
                Size = new Size(356, 142),
                BackColor = Color.FromArgb(246, 28, 25, 20),
                FrameBorderColor = GoldCol,
                InnerBorderColor = BorderCol,
                Chamfer = 9,
                Visible = false
            };

            _hudTooltipTitle = new Label
            {
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 9.6f, FontStyle.Bold),
                Location = new Point(12, 8),
                Size = new Size(332, 22)
            };

            _hudTooltipBody = new Label
            {
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.6f),
                Location = new Point(12, 32),
                Size = new Size(332, 100)
            };

            _hudTooltipCard.Controls.AddRange(new Control[] { _hudTooltipTitle, _hudTooltipBody });
        }

        private void BuildDrawerPanel()
        {
            _drawerPanel = new WarRoomPanel
            {
                Size = new Size(446, 620),
                BackColor = Color.FromArgb(248, 28, 25, 20),
                FrameBorderColor = GoldCol,
                InnerBorderColor = Color.FromArgb(74, 63, 46),
                Chamfer = 12,
                Visible = false
            };

            _drawerTitleLbl = new Label
            {
                Text = "◆ 建軍後勤",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 11.2f, FontStyle.Bold),
                Location = new Point(16, 10),
                Size = new Size(310, 26)
            };

            var btnClose = CreateWarButton("✕ 收合 (ESC)", BgCard, TextSub, 102, 26);
            btnClose.Font = new Font("Microsoft JhengHei UI", 8.2f, FontStyle.Bold);
            btnClose.Location = new Point(330, 10);
            btnClose.Click += (_, _) => CloseDrawer();

            _drawerScrollWrap = new DarkScrollContainer
            {
                Location = new Point(10, 42),
                Size = new Size(426, 566),
                BackColor = BgDark,
                ShowVerticalBar = true,
                ShowHorizontalBar = false
            };

            _drawerScrollBody = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(_drawerScrollWrap.ViewportWidth, 566),
                BackColor = BgDark,
                Padding = new Padding(4)
            };
            _drawerScrollWrap.SetContent(_drawerScrollBody);

            _drawerPanel.Controls.AddRange(new Control[] { _drawerTitleLbl, btnClose, _drawerScrollWrap });
        }

        private void LayoutFloatingMapPanels()
        {
            if (_mapPanel.Width <= 0 || _mapPanel.Height <= 0) return;

            _nodeCard.Location = new Point(14, Math.Max(10, _mapPanel.Height - _nodeCard.Height - 12));

            if (SelectedZone != null && GameData.Zones.TryGetValue(SelectedZone, out var z))
            {
                var vp = GetMapViewport();
                var boxPt = MapToScreen(new PointF((float)z.MapX, (float)z.MapY), vp);
                float uiScale = GetMapUiScale();
                int zx = Math.Clamp((int)(boxPt.X - 58 * uiScale - _zoneCard.Width - 10), 12, Math.Max(12, _mapPanel.Width - _zoneCard.Width - 12));
                int zy = Math.Clamp((int)(boxPt.Y - _zoneCard.Height * 0.5f), 12, Math.Max(12, _mapPanel.Height - _zoneCard.Height - 12));
                if (zx < 14) zx = 14;
                if (_drawerPanel.Visible && zx + _zoneCard.Width > _mapPanel.Width - _drawerPanel.Width - 20)
                {
                    zx = 14;
                    zy = Math.Max(12, _mapPanel.Height - _zoneCard.Height - 12);
                }
                _zoneCard.Location = new Point(zx, zy);
            }

            if (_drawerPanel.Visible)
            {
                int dw = Math.Min(448, Math.Max(368, _mapPanel.Width - 40));
                int dh = Math.Max(260, _mapPanel.Height - 24);
                _drawerPanel.Size = new Size(dw, dh);
                _drawerPanel.Location = new Point(_mapPanel.Width - dw - 12, 12);
                _drawerScrollWrap.Size = new Size(dw - 20, dh - 52);
                _drawerScrollBody.Width = _drawerScrollWrap.ViewportWidth;
            }

            LayoutIntelPageOverlay();
            CenterTechOverlay();
            CenterEventOverlay();
        }

        private void ToggleDrawer(string key)
        {
            if (key == "tech")
            {
                ToggleTechPage();
                return;
            }
            if (key == "mi")
            {
                ToggleIntelPage();
                return;
            }
            if (_techPageOpen) CloseTechPage();
            if (_intelPageOpen) CloseIntelPage();
            if (_activeDrawer == key)
            {
                CloseDrawer();
                return;
            }
            _activeDrawer = key;
            _drawerPanel.Visible = true;
            LayoutFloatingMapPanels();
            _drawerPanel.BringToFront();
            RefreshDrawerContent();
            RefreshDockButtons();
        }

        private void CloseDrawer()
        {
            _activeDrawer = "none";
            _drawerPanel.Visible = false;
            RefreshDockButtons();
        }

        private void RefreshDockButtons()
        {
            foreach (var kv in _drawerBtns)
            {
                bool active = (kv.Key == "tech" && _techPageOpen) || (kv.Key == "mi" && _intelPageOpen) || kv.Key == _activeDrawer;
                SetWarButtonStyle(
                    kv.Value,
                    active ? Color.FromArgb(72, 58, 34) : BgCard,
                    active ? GoldCol : TextMain,
                    active ? GoldCol : BorderCol);
            }
        }

        // ==================== 標題主選單（二戰參謀本部戰略計畫書） ====================
        private void BuildTitleOverlayControls()
        {
            var card = new WarRoomPanel
            {
                Size = new Size(616, 616),
                Location = new Point(64, 52),
                BackColor = Color.FromArgb(242, 26, 23, 19),
                FrameBorderColor = GoldCol,
                InnerBorderColor = Color.FromArgb(76, 65, 47),
                Chamfer = 14
            };

            var lblSub = new Label
            {
                Text = "★ 1950–2025 單機 2D 大戰略兵棋推演",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 10.6f, FontStyle.Bold),
                Location = new Point(30, 14),
                AutoSize = true
            };

            var lblTitle = new Label
            {
                Text = "國光：反攻大陸",
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 26f, FontStyle.Bold),
                Location = new Point(26, 36),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "二戰結束、國府播遷來台，您將扮演中華民國三軍最高統帥（全戰役一律自 1950 年真實局勢起步）。\n" +
                       "• 1950 年初黃金存底僅餘約 $1.25 億、台灣 GDP $14 億，可自行評估當下立即搶灘或先固守整軍\n" +
                       "• 慎防北方蘇聯／俄羅斯趁亂出兵侵占東北、內蒙與新疆\n" +
                       "• 推進達到時代里程碑後將解鎖「紀錄點」，下次可直接自同紀錄點續開不同結局！",
                ForeColor = TextMain,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.8f),
                Location = new Point(30, 86),
                Size = new Size(556, 98)
            };

            var lblModeHdr = new Label
            {
                Text = "◆ 決策提示模式（可於開始前或隨時返主選單切換）",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold),
                Location = new Point(30, 190),
                Size = new Size(556, 18)
            };

            _btnModeNormal = CreateWarButton("◆ 一般模式（無路線誘導）", Color.FromArgb(52, 86, 114), GoldCol, 272, 34, GoldCol);
            _btnModeNormal.Font = new Font("Microsoft JhengHei UI", 9.0f, FontStyle.Bold);
            _btnModeNormal.Location = new Point(30, 210);
            _btnModeNormal.Click += (_, _) => SetGameMode(false);

            _btnModeEasy = CreateWarButton("★ 簡易模式（顯示方針提示）", Color.FromArgb(34, 30, 25), TextSub, 272, 34, BorderCol);
            _btnModeEasy.Font = new Font("Microsoft JhengHei UI", 9.0f, FontStyle.Bold);
            _btnModeEasy.Location = new Point(314, 210);
            _btnModeEasy.Click += (_, _) => SetGameMode(true);

            var lblMutHdr = new Label
            {
                Text = "◈ 戰局變數（可自選 0–2 項 · 提高初期難度並增加最終通關評分）",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.6f, FontStyle.Bold),
                Location = new Point(30, 250),
                Size = new Size(556, 18)
            };

            _mutatorBtns.Clear();
            int mx = 30;
            foreach (var mut in GameData.Mutators.Values)
            {
                string mId = mut.Id;
                var bMut = CreateWarButton($"{mut.Title} (+{mut.ScoreBonus * 100:F0}%)", Color.FromArgb(34, 30, 25), TextSub, 133, 30, BorderCol);
                bMut.Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
                bMut.Location = new Point(mx, 270);
                _checkpointToolTip.SetToolTip(bMut, $"{mut.Title}：{mut.Desc}（通關評分 +{mut.ScoreBonus * 100:F0}%）");
                bMut.Click += (_, _) => ToggleMutatorSelection(mId);
                _mutatorBtns[mId] = bMut;
                card.Controls.Add(bMut);
                mx += 141;
            }

            var btn1950 = CreateWarButton("▶  1950 風雨飄搖 — 從頭展開新戰役（唯一初始開局）", Color.FromArgb(52, 86, 114), TextMain, 556, 42, GoldCol);
            btn1950.Font = new Font("Microsoft JhengHei UI", 10.0f, FontStyle.Bold);
            btn1950.Location = new Point(30, 308);
            btn1950.Click += (_, _) => StartScenario("1950");

            var lblCpHdr = new Label
            {
                Text = "◆ 時代紀錄點（須自 1950 起打到一定程度方能解鎖，解鎖後可自同紀錄點續開分歧）",
                ForeColor = GoldCol,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.6f, FontStyle.Bold),
                Location = new Point(30, 358),
                Size = new Size(556, 18)
            };

            card.Controls.AddRange(new Control[] { lblSub, lblTitle, lblDesc, lblModeHdr, _btnModeNormal, _btnModeEasy, lblMutHdr, btn1950, lblCpHdr });

            int cy = 380;
            foreach (var spec in SimEngine.CheckpointSpecs)
            {
                string kCopy = spec.Key;
                var bCp = CreateWarButton("", Color.FromArgb(34, 30, 25), TextSub, 556, 38, BorderCol);
                bCp.Font = new Font("Microsoft JhengHei UI", 8.8f, FontStyle.Bold);
                bCp.TextAlign = ContentAlignment.MiddleLeft;
                bCp.AccessibleName = spec.Title;
                bCp.Location = new Point(30, cy);
                bCp.Click += (_, _) => StartFromCheckpoint(kCopy);
                _checkpointBtns[kCopy] = bCp;
                card.Controls.Add(bCp);
                cy += 44;
            }

            var btnResume = CreateWarButton("返回當前戰局", BgCard, GoldCol, 556, 38, GoldDim);
            btnResume.Font = new Font("Microsoft JhengHei UI", 9.6f, FontStyle.Bold);
            btnResume.Location = new Point(30, 516);
            btnResume.Click += (_, _) =>
            {
                _showTitle = false;
                UpdateOverlays();
                RefreshAllUi();
            };

            var lblTip = new Label
            {
                Text = "提示：直接拖曳駐軍到目的地；敵區立即交戰，我方地區立即調防。空白鍵暫停／繼續。",
                ForeColor = TextSub,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 8.4f),
                Location = new Point(30, 562),
                Size = new Size(556, 38)
            };

            card.Controls.AddRange(new Control[] { btnResume, lblTip });
            _titleOverlay.Controls.Add(card);
            UpdateModeButtonsUi();
            UpdateMutatorButtonsUi();
            RefreshTitleOverlay();
        }

        private void ToggleMutatorSelection(string id)
        {
            if (!_selectedMutators.Remove(id))
            {
                if (_selectedMutators.Count >= 2)
                {
                    string oldest = _selectedMutators.First();
                    _selectedMutators.Remove(oldest);
                }
                _selectedMutators.Add(id);
            }
            UpdateMutatorButtonsUi();
        }

        private void UpdateMutatorButtonsUi()
        {
            foreach (var kv in _mutatorBtns)
            {
                bool on = _selectedMutators.Contains(kv.Key);
                SetWarButtonStyle(kv.Value,
                    on ? Color.FromArgb(118, 72, 26) : Color.FromArgb(34, 30, 25),
                    on ? GoldCol : TextSub,
                    on ? GoldCol : BorderCol);
            }
        }

        private void UpdateModeButtonsUi()
        {
            if (_btnModeNormal == null || _btnModeEasy == null) return;
            bool easy = IsEasyMode;
            SetWarButtonStyle(_btnModeNormal,
                !easy ? Color.FromArgb(52, 86, 114) : Color.FromArgb(34, 30, 25),
                !easy ? GoldCol : TextSub,
                !easy ? GoldCol : BorderCol);
            SetWarButtonStyle(_btnModeEasy,
                easy ? Color.FromArgb(52, 86, 114) : Color.FromArgb(34, 30, 25),
                easy ? GoldCol : TextSub,
                easy ? GoldCol : BorderCol);
        }

        public void RefreshTitleOverlay()
        {
            foreach (var spec in SimEngine.CheckpointSpecs)
            {
                if (!_checkpointBtns.TryGetValue(spec.Key, out var btn)) continue;
                if (_checkpoints.TryGetValue(spec.Key, out var rec))
                {
                    btn.Enabled = true;
                    btn.BackColor = Color.FromArgb(52, 82, 108);
                    btn.ForeColor = GoldCol;
                    btn.FlatAppearance.BorderColor = GoldCol;
                    string checkpointLabel = spec.Title.Contains('·')
                        ? spec.Title[..spec.Title.IndexOf('·')].Trim()
                        : spec.Title;
                    btn.Text = $"  ★ {checkpointLabel} · {rec.Year} 年 · 光復 {rec.HeldCount} 省 · GDP ${rec.RocGdp:F0} 億";
                    btn.AccessibleDescription = $"從 {rec.Title} 開始分歧戰役。存檔年份：{rec.Year}；光復 {rec.HeldCount} 省；台灣 GDP ${rec.RocGdp:F0} 億。";
                    _checkpointToolTip.SetToolTip(btn, btn.AccessibleDescription);
                }
                else
                {
                    btn.Enabled = false;
                    btn.BackColor = Color.FromArgb(28, 25, 21);
                    btn.ForeColor = TextSub;
                    btn.FlatAppearance.BorderColor = BorderCol;
                    string checkpointTitle = spec.Title.Contains('·')
                        ? spec.Title[..spec.Title.IndexOf('·')].Trim()
                        : spec.Title;
                    string shortRequirement = spec.Key switch
                    {
                        "cp_1962" => "到 1962 年／光復 1 省＋GDP 22 億",
                        "cp_1967" => "到 1967 年／光復 3 省",
                        "cp_1978" => "到 1978 年／攻克重鎮／光復 5 省",
                        _ => "完成指定戰役進度"
                    };
                    btn.Text = $"  ■ {checkpointTitle} · {shortRequirement}";
                    btn.AccessibleDescription = $"尚未解鎖。完整條件：{spec.RequirementDesc}。";
                    _checkpointToolTip.SetToolTip(btn, $"{spec.Title}\n解鎖條件：{spec.RequirementDesc}");
                }
            }
        }

        private void TitleOverlay_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (_titleBgImage != null)
            {
                g.DrawImage(_titleBgImage, new Rectangle(0, 0, _titleOverlay.Width, _titleOverlay.Height));
                using var darkBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, _titleOverlay.Width, _titleOverlay.Height),
                    Color.FromArgb(228, 20, 17, 13),
                    Color.FromArgb(148, 20, 17, 13),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(darkBrush, 0, 0, _titleOverlay.Width, _titleOverlay.Height);
            }
            else
            {
                g.Clear(BgDark);
            }
        }

        public void StartScenario(string preset)
        {
            State = EventSystem.NewScenario(preset, easyMode: _selectedEasyMode, mutators: preset == "1950" ? _selectedMutators : null);
            UpdateModeButtonsUi();
                _checkedUnitIds.Clear();
            SelectedZone = null;
            SelectedNode = "TW";
            foreach (var u in SimEngine.UnitsAt(State, "TW", UnitDomain.Land).Where(u => !u.Moved).OrderByDescending(u => u.Str).Take(3))
                _checkedUnitIds.Add(u.Id);
            _activeDrawer = "none";
            _showTitle = false;
            _userPaused = false;
            _techPageOpen = false;
            _intelPageOpen = false;
            _lastIntelRenderedCount = -1;
            _selectedTechId = null;
            _hoverTechId = null;
            _hoverTechBtnId = null;
            _techFlashMsg = null;
            _mapZoom = 1.0f;
            _mapPanX = 0.0f;
            _mapPanY = 0.0f;
            _noticeText = null;
            PulseFrontNotice();
            if (_techOverlay != null) _techOverlay.Visible = false;
            if (_intelPageOverlay != null) _intelPageOverlay.Visible = false;
            _drawerPanel.Visible = false;
            UpdateOverlays();
            RefreshAllUi();
        }

        // ==================== 頂部單行核桃木黃銅 HUD 繪製與懸停明細 ====================
        private void HudPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var bgGrad = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(1, _hudPanel.Width), Math.Max(1, _hudPanel.Height)),
                Color.FromArgb(35, 31, 26), Color.FromArgb(22, 19, 16), LinearGradientMode.Vertical))
            {
                g.FillRectangle(bgGrad, _hudPanel.ClientRectangle);
            }
            using (var linePen = new Pen(GoldDim, 1.3f))
                g.DrawLine(linePen, 0, _hudPanel.Height - 1, _hudPanel.Width, _hudPanel.Height - 1);
            using (var innerPen = new Pen(Color.FromArgb(48, GoldCol), 1f))
                g.DrawLine(innerPen, 0, _hudPanel.Height - 3, _hudPanel.Width, _hudPanel.Height - 3);

            using var titleF = new Font("Microsoft JhengHei UI", 11.4f, FontStyle.Bold);
            using var pillLblF = new Font("Microsoft JhengHei UI", 7.8f);
            using var pillValF = new Font("Microsoft JhengHei UI", 9.5f, FontStyle.Bold);
            using var goldBr = new SolidBrush(GoldCol);
            using var mainBr = new SolidBrush(TextMain);
            using var subBr = new SolidBrush(TextSub);

            // 1. 標題與即時戰局日期
            string titleTxt = "★ 國光：反攻大陸";
            g.DrawString(titleTxt, titleF, goldBr, 14, 6);
            var titleSz = g.MeasureString(titleTxt, titleF);
            g.DrawString(IsEasyMode ? "· 簡易" : "· 一般", pillLblF, IsEasyMode ? goldBr : subBr, 14 + titleSz.Width + 1, 10);
            string clockStatus = State.PendingEvents.Count > 0 ? "▌▌ 事件暫停"
                               : State.Over != null ? "■ 歷史終局"
                               : _techPageOpen ? "▌▌ 科技暫停"
                               : _intelPageOpen ? "▌▌ 軍情暫停"
                               : _userPaused ? "▌▌ 已暫停"
                               : $"▶ {_simSpeed}×";
            Color clkCol = State.PendingEvents.Count > 0 ? GoldCol
                         : State.Over != null ? BadRed
                         : IsOverlayPausingTime ? Color.FromArgb(148, 212, 204)
                         : _userPaused ? Color.FromArgb(228, 178, 92)
                         : GoodGreen;
            using var dateF = new Font("Microsoft JhengHei UI", 8.2f, FontStyle.Bold);
            using var clkBr = new SolidBrush(clkCol);
            string dateTxt = $"{State.Year}年{State.Month:D2}月{State.Day:D2}日";
            g.DrawString(dateTxt, dateF, mainBr, 15, 31);
            var dateSz = g.MeasureString(dateTxt, dateF);
            string seasonTxt = SimEngine.GetSeasonLabel(State.Month);
            g.DrawString($"{clockStatus} · {seasonTxt}", pillLblF, clkBr, 15 + dateSz.Width + 4, 32);

            int x = 214;
            int rightLimit = _btnSave.Left - 14;

            var b = SimEngine.Budget(State);
            double liftLeft = SimEngine.LiftCapacity(State) - SimEngine.LiftUsed(State);
            int heldCount = SimEngine.HeldMainland(State).Count;
            int ussrCount = SimEngine.UssrNodes(State).Count;

            // 2. 國庫淨收銘牌（可懸停看稅率/債息明細）
            int w1 = Math.Min(182, Math.Max(142, (rightLimit - x) / 4));
            _hudRectTreasury = new RectangleF(x, 8, w1, 40);
            DrawHudCapsule(g, _hudRectTreasury, _hoverHudKey == "treasury");
            g.DrawString("國庫（移入看明細）", pillLblF, subBr, x + 9, 10);
            string netSign = b.net >= 0 ? "+" : "";
            using (var cBr = new SolidBrush(b.net >= 0 ? GoodGreen : BadRed))
                g.DrawString($"${State.Treasury:F1}億 ({netSign}{b.net:F1})", pillValF, cBr, x + 9, 25);
            x += w1 + 9;

            // 3. 兵源與兩棲運力銘牌
            int w2 = Math.Min(180, Math.Max(142, (rightLimit - x) / 3));
            var rMan = new RectangleF(x, 8, w2, 40);
            DrawHudCapsule(g, rMan, false);
            g.DrawString($"補給 {State.Supply:F0} ({State.SupplyRatio * 100:F0}%)", pillLblF, State.SupplyRatio >= 0.8 ? subBr : new SolidBrush(BadRed), x + 9, 10);
            g.DrawString($"兵源 {State.Manpower:F1} · 運 {liftLeft:F1}師", pillValF, mainBr, x + 9, 25);
            x += w2 + 9;

            // 4. 兩岸民心與穩定度雙色拉鋸條
            int w3 = Math.Min(238, Math.Max(168, (rightLimit - x) / 2));
            _hudRectStability = new RectangleF(x, 8, w3, 40);
            DrawHudCapsule(g, _hudRectStability, _hoverHudKey == "stability");
            g.DrawString($"台灣民心 {State.Morale:F0}", pillLblF, new SolidBrush(Color.FromArgb(148, 198, 224)), x + 9, 10);
            string prcTxt = $"中共穩定 {State.PrcStab:F0}";
            var prcSz = g.MeasureString(prcTxt, pillLblF);
            g.DrawString(prcTxt, pillLblF, new SolidBrush(Color.FromArgb(228, 138, 126)), x + w3 - prcSz.Width - 7, 10);

            var barR = new RectangleF(x + 9, 29, w3 - 18, 10);
            double totalSt = Math.Max(1.0, State.Morale + State.PrcStab);
            float rocW = (float)(barR.Width * (State.Morale / totalSt));
            using (var rBr = new SolidBrush(PrcRed))
            using (var bBr = new SolidBrush(RocBlue))
            using (var rimPen = new Pen(BorderCol, 0.9f))
            using (var sepPen = new Pen(GoldCol, 1.5f))
            using (var barPath = WarGdi.CreateRoundedPath(barR, 3.5f))
            {
                g.SetClip(barPath);
                g.FillRectangle(rBr, barR);
                g.FillRectangle(bBr, barR.X, barR.Y, rocW, barR.Height);
                g.ResetClip();
                g.DrawPath(rimPen, barPath);
                g.DrawLine(sepPen, barR.X + rocW, barR.Y - 1, barR.X + rocW, barR.Bottom + 1);
            }
            x += w3 + 9;

            // 5. 光復省分與外交/北疆警示
            int w4 = Math.Max(142, rightLimit - x);
            _hudRectDiplomacy = new RectangleF(x, 8, w4, 40);
            DrawHudCapsule(g, _hudRectDiplomacy, _hoverHudKey == "diplomacy");
            string usTxt = State.Treaty ? (State.UsConsent > 0 ? $"美默許({State.UsConsent}y)" : "防禦條約") : "無條約限";
            g.DrawString($"美 {State.UsRel:F0}（{usTxt}）", pillLblF, subBr, x + 9, 10);
            double prcDiv = SimEngine.PrcDeployed(State);
            string mapTxt = ussrCount > 0
                ? $"共軍 {prcDiv:F0}師 · ⚠俄占{ussrCount}"
                : $"共軍 {prcDiv:F0}師 · 光復{heldCount}/{GameData.MainlandIds.Length}";
            using (var mBr = new SolidBrush(ussrCount > 0 ? BadRed : GoldCol))
                g.DrawString(mapTxt, pillValF, mBr, x + 9, 25);

            string notice = string.IsNullOrEmpty(_noticeText) ? "戰地電訊接線中……" : _noticeText;
            var nRect = new RectangleF(10, 50, Math.Max(40, _hudPanel.Width - 20), 22);
            WarGdi.DrawPlaqueFrame(g, nRect, 4f, Color.FromArgb(32, 28, 24), GoldDim, Color.FromArgb(48, 42, 34));
            using var noticeF = new Font("Microsoft JhengHei UI", 8.4f, FontStyle.Bold);
            using var noticeBr = new SolidBrush(TextMain);
            using var noticeFmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString("戰地電訊  " + notice, noticeF, noticeBr, new RectangleF(nRect.X + 8, nRect.Y + 2, nRect.Width - 16, nRect.Height - 2), noticeFmt);
        }

        private void PulseFrontNotice()
        {
            if (State == null || _showTitle) return;
            int ordinal = State.Year * 400 + State.Month * 32 + State.Day;
            bool due = string.IsNullOrEmpty(_noticeText)
                || ((DateTime.UtcNow - _noticeAt).TotalMilliseconds >= 3200 && ordinal - _noticeOrdinal >= 8);
            if (!due) return;
            _noticeText = EventSystem.FrontNotice(State, ordinal);
            _noticeAt = DateTime.UtcNow;
            _noticeOrdinal = ordinal;
        }

        private void DrawHudCapsule(Graphics g, RectangleF r, bool hovered)
        {
            WarGdi.DrawPlaqueFrame(g, r, 5.5f,
                hovered ? BgCardHi : BgCard,
                hovered ? GoldCol : BorderCol,
                Color.FromArgb(46, 40, 32));
        }

        private void HudPanel_MouseMove(object sender, MouseEventArgs e)
        {
            string nextKey = _hudRectTreasury.Contains(e.Location) ? "treasury"
                           : _hudRectStability.Contains(e.Location) ? "stability"
                           : _hudRectDiplomacy.Contains(e.Location) ? "diplomacy"
                           : null;

            if (nextKey != _hoverHudKey)
            {
                _hoverHudKey = nextKey;
                _hudPanel.Invalidate();
                UpdateHudTooltip();
            }
        }

        private void UpdateHudTooltip()
        {
            if (_hoverHudKey == null || _showTitle || State.PendingEvents.Count > 0)
            {
                _hudTooltipCard.Visible = false;
                return;
            }

            if (_hoverHudKey == "treasury")
            {
                var b = SimEngine.Budget(State);
                string taxName = State.Tax == "low" ? "輕稅 12%" : State.Tax == "high" ? "重稅 20%" : "標準 16%";
                string dirLine = !string.IsNullOrEmpty(State.ActiveDirectiveName)
                    ? $"\n時代國策：{State.ActiveDirectiveName}（至 {State.DirectiveEndYear} 年）"
                    : "";
                _hudTooltipTitle.Text = "◆ 國庫財政與經濟明細";
                _hudTooltipBody.Text =
                    $"年度歲入：+${b.income:F2} 億（稅率：{taxName}）\n" +
                    $"三軍維持：-${b.upkeep:F2} 億  |  研發債息：-${(b.research + b.interest):F2} 億\n" +
                    $"戰爭公債：單期 ${SimEngine.BondAmount(State):F2} 億（佔 GDP 18% · 仿美 War Bonds）\n" +
                    $"累計公債：${State.Debt:F1} 億  |  經濟乘數：{State.EconMult * 100:F0}%  |  物價：×{SimEngine.PriceIndex(State):F1}{dirLine}";
                _hudTooltipCard.Location = new Point((int)_hudRectTreasury.X, 8);
            }
            else if (_hoverHudKey == "stability")
            {
                _hudTooltipTitle.Text = "◆ 兩岸國力與綜合對比";
                _hudTooltipBody.Text =
                    $"台灣 GDP：${State.Gdp:F0} 億  vs  對岸 GDP：${SimEngine.PrcGdp(State):F0} 億\n" +
                    $"共軍部署：{SimEngine.PrcDeployed(State):F0} 師（隨年份擴編，地圖紅籌碼同步增加）\n" +
                    $"國軍綜合軍力：{SimEngine.RocMilIndex(State):F1}  vs  共軍軍力：{SimEngine.PrcMilIndex(State):F1}\n" +
                    $"共軍喪失工業比例：{SimEngine.PrcLostIndustry(State) * 100:F0}%  |  物資補給上限：{State.SupplyCap:F0}\n" +
                    $"{SimEngine.DescribePrcDoctrineIntel(State)}";
                _hudTooltipCard.Location = new Point((int)_hudRectStability.X, 8);
            }
            else
            {
                int ussr = SimEngine.UssrNodes(State).Count;
                _hudTooltipTitle.Text = "◆ 國際外交與北疆情勢";
                _hudTooltipBody.Text =
                    $"對美關係：{State.UsRel:F0}  |  對日關係：{State.JpRel:F0}  |  國際地位：{State.Intl:F0}\n" +
                    $"美軍協防承諾：{SimEngine.UsDefense(State):F1}  |  台海威脅：{SimEngine.ThreatIndex(State):F2}\n" +
                    $"中蘇邊境緊張度：{State.PrcSovTension * 100:F0}%  |  北疆俄占：{ussr} 省";
                _hudTooltipCard.Location = new Point(Math.Min(_mapPanel.Width - _hudTooltipCard.Width - 10, (int)_hudRectDiplomacy.X), 8);
            }

            _hudTooltipCard.Visible = true;
            _hudTooltipCard.BringToFront();
        }

        private void DockBar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bgGrad = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(1, _dockBar.Width), Math.Max(1, _dockBar.Height)),
                Color.FromArgb(30, 26, 21), Color.FromArgb(20, 18, 14), LinearGradientMode.Vertical))
            {
                g.FillRectangle(bgGrad, _dockBar.ClientRectangle);
            }
            using var linePen = new Pen(GoldDim, 1.3f);
            g.DrawLine(linePen, 0, 0, _dockBar.Width, 0);
            using var innerPen = new Pen(Color.FromArgb(46, GoldCol), 1f);
            g.DrawLine(innerPen, 0, 2, _dockBar.Width, 2);
        }

        // ==================== 中央全幅 2D 二戰復古兵棋地圖縮放、平移與繪製 ====================
        public float MapZoom => _mapZoom;
        public float MapPanX => _mapPanX;
        public float MapPanY => _mapPanY;

        private bool CanInteractMap()
            => !_showTitle && !_techPageOpen && !_intelPageOpen && State?.Over == null && State?.PendingEvents.Count == 0
               && _mapPanel != null && _mapPanel.Visible;

        private bool IsPointInFloatingMapChild(Point mapPt)
        {
            Control[] overlays = { _drawerPanel, _nodeCard, _zoneCard, _hudTooltipCard, _intelPageOverlay, _techOverlay, _eventOverlay };
            foreach (var c in overlays)
            {
                if (c != null && c.Visible && c.Bounds.Contains(mapPt)) return true;
            }
            return false;
        }

        public bool PreFilterMessage(ref Message m)
        {
            const int WM_MOUSEWHEEL = 0x020A;
            if (m.Msg != WM_MOUSEWHEEL) return false;

            var target = Control.FromHandle(m.HWnd);
            if (target == null || target.FindForm() != this) return false;

            long lp = m.LParam.ToInt64();
            int sx = unchecked((short)(lp & 0xFFFF));
            int sy = unchecked((short)((lp >> 16) & 0xFFFF));
            int delta = unchecked((short)((m.WParam.ToInt64() >> 16) & 0xFFFF));
            var screenPt = new Point(sx, sy);

            // 先檢查是否捲動自繪深色捲軸容器（依上層遮罩優先順序）
            bool shift = (ModifierKeys & Keys.Shift) == Keys.Shift;
            DarkScrollContainer[] scrollTargets = { _techScrollWrap, _intelFeedScroll, _drawerScrollWrap, _nodeChipsWrap };
            foreach (var sc in scrollTargets)
            {
                if (sc == null || !sc.Visible) continue;
                var localPt = sc.PointToClient(screenPt);
                if (sc.ClientRectangle.Contains(localPt))
                {
                    if (sc.HandleMouseWheel(delta, shift)) return true;
                }
            }

            if (CanInteractMap())
            {
                var mapPt = _mapPanel.PointToClient(screenPt);
                if (_mapPanel.ClientRectangle.Contains(mapPt) && !IsPointInFloatingMapChild(mapPt))
                {
                    ZoomMapAt(delta, mapPt);
                    return true;
                }
            }
            return false;
        }

        private RectangleF GetBaseMapViewport()
        {
            float pad = 8f;
            float availW = Math.Max(100, _mapPanel.Width - pad * 2);
            float availH = Math.Max(100, _mapPanel.Height - pad * 2);
            float scale = Math.Min(availW / GameData.MapW, availH / GameData.MapH);
            float w = GameData.MapW * scale;
            float h = GameData.MapH * scale;
            float ox = (_mapPanel.Width - w) * 0.5f;
            float oy = (_mapPanel.Height - h) * 0.5f;
            return new RectangleF(ox, oy, w, h);
        }

        private void ClampMapPan()
        {
            if (_mapPanel == null || _mapPanel.Width <= 0) return;
            if (_mapZoom <= 1.001f)
            {
                _mapZoom = 1.0f;
                _mapPanX = 0.0f;
                _mapPanY = 0.0f;
                return;
            }
            var b = GetBaseMapViewport();
            float w = b.Width * _mapZoom;
            float h = b.Height * _mapZoom;
            float keepX = Math.Min(w * 0.55f, _mapPanel.Width * 0.45f);
            float keepY = Math.Min(h * 0.55f, _mapPanel.Height * 0.45f);
            float minX = Math.Min(b.X, keepX - w);
            float maxX = Math.Max(b.X, _mapPanel.Width - keepX);
            float minY = Math.Min(b.Y, keepY - h);
            float maxY = Math.Max(b.Y, _mapPanel.Height - keepY);
            _mapPanX = Math.Clamp(_mapPanX, minX - b.X, maxX - b.X);
            _mapPanY = Math.Clamp(_mapPanY, minY - b.Y, maxY - b.Y);
        }

        public void ZoomMapAt(int delta, Point? mapPt = null)
        {
            if (_mapPanel == null || _mapPanel.Width <= 0 || delta == 0) return;
            var anchor = mapPt ?? new Point(_mapPanel.Width / 2, _mapPanel.Height / 2);
            var b = GetBaseMapViewport();
            var oldVp = GetMapViewport();

            float u = (anchor.X - oldVp.X) / Math.Max(1f, oldVp.Width);
            float v = (anchor.Y - oldVp.Y) / Math.Max(1f, oldVp.Height);

            float step = delta > 0 ? 1.22f : (1.0f / 1.22f);
            float newZoom = Math.Clamp(_mapZoom * step, 1.0f, 4.0f);
            if (newZoom < 1.04f) newZoom = 1.0f;

            _mapZoom = newZoom;
            if (_mapZoom <= 1.001f)
            {
                _mapPanX = 0f;
                _mapPanY = 0f;
            }
            else
            {
                float newW = b.Width * _mapZoom;
                float newH = b.Height * _mapZoom;
                float newX = anchor.X - u * newW;
                float newY = anchor.Y - v * newH;
                _mapPanX = newX - b.X;
                _mapPanY = newY - b.Y;
                ClampMapPan();
            }

            LayoutFloatingMapPanels();
            _mapPanel.Invalidate();
        }

        public void ResetMapZoom()
        {
            _mapZoom = 1.0f;
            _mapPanX = 0.0f;
            _mapPanY = 0.0f;
            LayoutFloatingMapPanels();
            _mapPanel?.Invalidate();
        }

        public PointF GetNodeScreenPointForTest(string nodeId)
        {
            var n = GameData.Nodes[nodeId];
            return MapToScreen(GameData.Project(n.Lon, n.Lat), GetMapViewport());
        }

        private float GetMapUiScale() => GetBaseMapViewport().Width / GameData.MapW;

        public float CurrentMapUiScaleForTest => GetMapUiScale();

        private RectangleF GetMapViewport()
        {
            var b = GetBaseMapViewport();
            if (_mapZoom <= 1.001f) return b;
            return new RectangleF(b.X + _mapPanX, b.Y + _mapPanY, b.Width * _mapZoom, b.Height * _mapZoom);
        }

        private PointF MapToScreen(PointF mp, RectangleF vp)
        {
            float sx = vp.X + (mp.X / GameData.MapW) * vp.Width;
            float sy = vp.Y + (mp.Y / GameData.MapH) * vp.Height;
            return new PointF(sx, sy);
        }

        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var vp = GetMapViewport();
            float scale = vp.Width / GameData.MapW;
            float uiScale = GetMapUiScale();
            float strokeScale = Math.Min(scale, uiScale * 1.45f);
            var fullRect = _mapPanel.ClientRectangle;

            // 1. 普魯士深海青灰漸層底色 ＋ 蘭伯特正形圓錐投影經緯線網格
            using (var seaBrush = new LinearGradientBrush(fullRect, Color.FromArgb(24, 38, 44), Color.FromArgb(14, 24, 29), LinearGradientMode.Vertical))
            {
                g.FillRectangle(seaBrush, fullRect);
            }

            using (var gridPen = new Pen(Color.FromArgb(24, 206, 188, 148), 1f))
            using (var gridFont = new Font("Consolas", Math.Max(6.8f, 7.6f * uiScale)))
            using (var gridBrush = new SolidBrush(Color.FromArgb(88, 198, 182, 148)))
            {
                for (int lon = 45; lon <= 165; lon += 5)
                {
                    var merPts = new List<PointF>();
                    for (int lat = 6; lat <= 64; lat += 2)
                        merPts.Add(MapToScreen(GameData.Project(lon, lat), vp));
                    g.DrawLines(gridPen, merPts.ToArray());
                    if (lon % 10 == 0 && lon >= 60 && lon <= 150)
                    {
                        var topPt = MapToScreen(GameData.Project(lon, 53.5), vp);
                        if (topPt.X > 16 && topPt.X < fullRect.Width - 36)
                            g.DrawString($"{lon}°E", gridFont, gridBrush, topPt.X - 12 * uiScale, 32);
                    }
                }
                for (int lat = 10; lat <= 60; lat += 5)
                {
                    var parPts = new List<PointF>();
                    for (int lon = 45; lon <= 165; lon += 2)
                        parPts.Add(MapToScreen(GameData.Project(lon, lat), vp));
                    g.DrawLines(gridPen, parPts.ToArray());
                    var leftPt = MapToScreen(GameData.Project(73, lat), vp);
                    if (leftPt.Y > 36 && leftPt.Y < fullRect.Height - 24)
                        g.DrawString($"{lat}°N", gridFont, gridBrush, 8, leftPt.Y - 8);
                }
            }

            DrawCompassRoseWatermark(g, MapToScreen(GameData.Project(123.5, 16.5), vp), Math.Min(scale, uiScale * 1.35f), uiScale);

            // 1B. 海岸線淺海等深暈染（三層水彩淺海光暈，隨即被陸塊覆蓋內側僅留海域側漸層）
            DrawCoastalBathymetryHalo(g, vp, strokeScale);

            // 2. 周邊亞洲鄰國羊皮紙戰圖底色（蘇聯／俄羅斯、外蒙古、朝鮮半島、日本、中南半島、印度）
            DrawPolygonRings(g, ChinaMapGeo.NeighborAsianRings, vp,
                Color.FromArgb(232, 47, 44, 35), Color.FromArgb(115, 118, 106, 82), 0.9f * strokeScale);
            DrawPolygonRings(g, ChinaMapGeo.MongoliaRings, vp,
                Color.FromArgb(234, 54, 48, 38), Color.FromArgb(135, 132, 118, 92), 1.05f * strokeScale);
            DrawPolygonRings(g, ChinaMapGeo.UssrRings, vp,
                Color.FromArgb(234, 58, 40, 46), Color.FromArgb(145, 146, 98, 112), 1.15f * strokeScale);

            using (var neighborFont = new Font("Microsoft JhengHei UI", Math.Max(7.5f, 8.6f * uiScale), FontStyle.Bold))
            using (var neighborBrush = new SolidBrush(Color.FromArgb(118, 198, 186, 158)))
            using (var ussrBgBrush = new SolidBrush(Color.FromArgb(138, 214, 164, 178)))
            {
                DrawCenteredText(g, "蘇 聯 ／ 俄 羅 斯（西伯利亞・遠東軍區）", neighborFont, ussrBgBrush, MapToScreen(GameData.Project(108.0, 53.2), vp));
                DrawCenteredText(g, "蘇聯中亞軍區", neighborFont, ussrBgBrush, MapToScreen(GameData.Project(76.5, 45.8), vp));
                DrawCenteredText(g, "外 蒙 古", neighborFont, neighborBrush, MapToScreen(GameData.Project(103.5, 46.8), vp));
                DrawCenteredText(g, "朝鮮半島", neighborFont, neighborBrush, MapToScreen(GameData.Project(127.3, 38.6), vp));
                DrawCenteredText(g, "日 本", neighborFont, neighborBrush, MapToScreen(GameData.Project(138.2, 36.2), vp));
                DrawCenteredText(g, "印 度", neighborFont, neighborBrush, MapToScreen(GameData.Project(82.5, 25.5), vp));
                DrawCenteredText(g, "中南半島", neighborFont, neighborBrush, MapToScreen(GameData.Project(101.2, 20.0), vp));
            }

            // 計算目前勾選部隊之可進軍省分集合（供地圖目標省分發光導引）
            var readyUnits = _checkedUnitIds
                .Select(id => SimEngine.UnitById(State, id))
                .Where(u => u != null && !u.Moved)
                .ToList();
            var reachableDest = new Dictionary<string, (string kind, string oddsLabel, string tier)>();
            if (SelectedNode != null && readyUnits.Count > 0)
            {
                var sampleU = readyUnits[0];
                foreach (var destId in GameData.NodeIds)
                {
                    if (destId == SelectedNode) continue;
                    var chk = SimEngine.CheckMove(State, sampleU, destId);
                    if (!chk.ok) continue;
                    var odds = SimEngine.EstimateMoveOdds(State, readyUnits, destId);
                    reachableDest[destId] = (chk.kind, odds.label, odds.tier);
                }
            }

            // 3. 繪製全中國 29 個真實省級疆域多邊形（二戰軍圖墨色：普魯士藍 / 復古硃紅 / 蘇俄梅紅）
            using (var provBorderPen = new Pen(Color.FromArgb(92, 224, 212, 184), Math.Max(0.65f, 0.78f * strokeScale)))
            {
                for (int pi = 0; pi < ChinaMapGeo.RealProvinces.Length; pi++)
                {
                    var prov = ChinaMapGeo.RealProvinces[pi];
                    string nid = prov.NodeId;
                    Faction ctrl = State.Control.GetValueOrDefault(nid, Faction.PRC);

                    int shade = ((pi % 3) - 1) * 6;
                    int rBase = ctrl == Faction.ROC ? 52 : ctrl == Faction.USSR ? 96 : 132;
                    int gBase = ctrl == Faction.ROC ? 92 : ctrl == Faction.USSR ? 56 : 58;
                    int bBase = ctrl == Faction.ROC ? 122 : ctrl == Faction.USSR ? 76 : 52;
                    int alpha = 228;

                    if (nid == SelectedNode)
                    {
                        rBase += 24; gBase += 24; bBase += 20; alpha = 242;
                    }
                    else if (reachableDest.ContainsKey(nid))
                    {
                        rBase += 18; gBase += 18; bBase += 10; alpha = 236;
                    }
                    else if (nid == _hoverNode)
                    {
                        rBase += 12; gBase += 12; bBase += 10; alpha = 232;
                    }

                    Color fillCol = Color.FromArgb(
                        alpha,
                        Math.Clamp(rBase + shade, 0, 255),
                        Math.Clamp(gBase + shade, 0, 255),
                        Math.Clamp(bBase + shade, 0, 255));
                    bool contested = SimEngine.IsContested(State, nid);
                    Color otherCol = Color.Empty;
                    if (contested)
                    {
                        Faction other = ctrl == Faction.ROC ? Faction.PRC : Faction.ROC;
                        int or = other == Faction.ROC ? 52 : other == Faction.USSR ? 96 : 132;
                        int og = other == Faction.ROC ? 92 : other == Faction.USSR ? 56 : 58;
                        int ob = other == Faction.ROC ? 122 : other == Faction.USSR ? 76 : 52;
                        if (nid == SelectedNode || reachableDest.ContainsKey(nid) || nid == _hoverNode)
                        {
                            or += 16; og += 16; ob += 12;
                        }
                        otherCol = Color.FromArgb(alpha,
                            Math.Clamp(or + shade, 0, 255),
                            Math.Clamp(og + shade, 0, 255),
                            Math.Clamp(ob + shade, 0, 255));
                    }

                    foreach (var ring in prov.Rings)
                    {
                        if (ring.Length < 3) continue;
                        var pts = ring.Select(p => MapToScreen(GameData.Project(p.X, p.Y), vp)).ToArray();
                        if (contested) FillContestedPolygon(g, pts, fillCol, otherCol);
                        else
                        {
                            using var br = new SolidBrush(fillCol);
                            g.FillPolygon(br, pts);
                        }
                        g.DrawPolygon(provBorderPen, pts);
                    }
                }
            }

            // 4. 四大自然水系（長江、黃河、黑龍江、珠江）
            using (var riverPen = new Pen(Color.FromArgb(148, 86, 146, 158), Math.Max(1.1f, 1.3f * strokeScale)))
            {
                foreach (var line in ChinaMapGeo.MajorRivers)
                {
                    if (line.Length < 2) continue;
                    var pts = line.Select(p => MapToScreen(GameData.Project(p.X, p.Y), vp)).ToArray();
                    g.DrawLines(riverPen, pts);
                }
            }

            // 5. Province-level 大戰略軍區邊界線（柔化暗墨描邊 ＋ 目標淡金虛線高光）
            foreach (var kv in ChinaMapGeo.NodeOuterRings)
            {
                string nid = kv.Key;
                var rings = kv.Value;
                Faction ctrl = State.Control.GetValueOrDefault(nid, Faction.PRC);
                bool isSel = nid == SelectedNode;
                bool isTarget = reachableDest.ContainsKey(nid);
                bool isHov = nid == _hoverNode;

                Color borderCol = isSel ? GoldCol
                                : isTarget ? Color.FromArgb(236, 224, 182, 92)
                                : isHov ? Color.FromArgb(205, 228, 212, 168)
                                : Color.FromArgb(118, 222, 208, 174);
                float borderW = isSel ? 2.4f * strokeScale : isTarget ? 1.9f * strokeScale : isHov ? 1.6f * strokeScale : 1.15f * strokeScale;

                using var inkPen = new Pen(Color.FromArgb(135, 22, 20, 16), Math.Max(1.6f, borderW + 1.0f));
                using var outPen = new Pen(borderCol, Math.Max(0.95f, borderW));
                if (isTarget) outPen.DashStyle = DashStyle.Dash;
                foreach (var ring in rings)
                {
                    if (ring.Length < 3) continue;
                    var pts = ring.Select(p => MapToScreen(GameData.Project(p.X, p.Y), vp)).ToArray();
                    if (ctrl == Faction.USSR && !SimEngine.IsContested(State, nid))
                    {
                        using var hatch = new HatchBrush(HatchStyle.BackwardDiagonal, Color.FromArgb(78, 210, 152, 182), Color.Transparent);
                        g.FillPolygon(hatch, pts);
                    }
                    if (!isTarget) g.DrawPolygon(inkPen, pts);
                    g.DrawPolygon(outPen, pts);
                }
            }

            // 6. 29 省真實省名暗紋
            using (var provFont = new Font("Microsoft JhengHei UI", Math.Max(6.8f, 7.4f * uiScale), FontStyle.Regular))
            using (var provTextBr = new SolidBrush(Color.FromArgb(156, 240, 232, 212)))
            {
                var singleProvNodes = new HashSet<string> { "台灣", "海南", "閩北", "閩南", "浙江", "山東", "新疆", "西藏", "內蒙古" };
                var customLabelPos = new Dictionary<string, (double lon, double lat)>
                {
                    ["河北"] = (116.8, 40.5),
                    ["山西"] = (112.1, 37.6),
                    ["甘肅"] = (103.8, 36.0),
                    ["青海"] = (95.5, 35.4),
                    ["寧夏"] = (106.1, 37.4),
                    ["陝西"] = (108.8, 34.4),
                    ["湖北"] = (109.8, 31.2),
                    ["河南"] = (113.6, 34.1),
                    ["湖南"] = (111.5, 27.3),
                    ["安徽"] = (117.0, 31.8),
                    ["江西"] = (115.1, 26.3),
                    ["江蘇"] = (119.0, 34.3),
                    ["廣西"] = (107.8, 23.8),
                    ["廣東"] = (113.8, 23.6),
                    ["四川"] = (102.2, 30.8),
                    ["貴州"] = (106.8, 26.8),
                    ["雲南"] = (101.2, 24.4),
                    ["黑龍江"] = (128.0, 48.2),
                    ["吉林"] = (126.2, 43.4),
                    ["遼寧"] = (122.6, 41.3),
                };

                foreach (var prov in ChinaMapGeo.RealProvinces)
                {
                    if (singleProvNodes.Contains(prov.Name)) continue;
                    double lLon = prov.LabelLon, lLat = prov.LabelLat;
                    if (customLabelPos.TryGetValue(prov.Name, out var cp)) { lLon = cp.lon; lLat = cp.lat; }
                    DrawCenteredText(g, prov.Name, provFont, provTextBr, MapToScreen(GameData.Project(lLon, lLat), vp));
                }
            }

            // 7. 內陸戰略交通幹線
            var drawnEdges = new HashSet<string>();
            using (var roadPen = new Pen(Color.FromArgb(52, 224, 206, 162), Math.Max(0.9f, 1.05f * strokeScale)) { DashStyle = DashStyle.Dot })
            {
                foreach (var n in GameData.Nodes.Values)
                {
                    var p1 = MapToScreen(GameData.Project(n.Lon, n.Lat), vp);
                    foreach (var aid in n.Adj)
                    {
                        string key = string.CompareOrdinal(n.Id, aid) < 0 ? $"{n.Id}-{aid}" : $"{aid}-{n.Id}";
                        if (!drawnEdges.Add(key)) continue;
                        var nb = GameData.Nodes[aid];
                        var p2 = MapToScreen(GameData.Project(nb.Lon, nb.Lat), vp);
                        g.DrawLine(roadPen, p1, p2);
                    }
                }
            }

            // 9. 四大海域水名與右側海空戰術框
            DrawSeaZones(g, vp, uiScale);

            // 10. 繪製敵我進軍路線箭頭（底層線條）
            DrawMovementLines(g, vp, uiScale);

            // 11. 繪製省級控制節點、外島引線與復古籌碼兵牌＋預估標籤
            DrawNodesAndCounters(g, vp, scale, uiScale, reachableDest);

            // 11.5. 於節點上層繪製移動中軍隊小圖示與兩棲籌備進度牌（避免被省名遮擋）
            DrawOrderBadgesAndTokens(g, vp, uiScale);

            // 12. 邊緣軍桌暗角暈染與頂部簡潔圖例／北方邊境警示圓角牌
            DrawMapTableVignette(g, fullRect);

            using (var ussrFont = new Font("Microsoft JhengHei UI", Math.Clamp(8.2f * uiScale, 7.6f, 8.6f), FontStyle.Bold))
            using (var ussrTextBrush = new SolidBrush(Color.FromArgb(242, 236, 198, 204)))
            {
                var bRect = new RectangleF(12, 8, 548, 23);
                WarGdi.DrawPlaqueFrame(g, bRect, 5f, Color.FromArgb(206, 54, 34, 38), Color.FromArgb(165, 146, 92, 102), Color.FromArgb(72, 46, 52));
                g.DrawString("▲ 北方強鄰：蘇聯／俄羅斯邊境（大陸動盪或中蘇決裂時，俄軍可能強占新疆・內蒙・東北）", ussrFont, ussrTextBrush, bRect.X + 9, bRect.Y + 4);
            }

            using (var legFont = new Font("Microsoft JhengHei UI", Math.Clamp(8.0f * uiScale, 7.4f, 8.4f)))
            using (var legTxtBr = new SolidBrush(TextMain))
            {
                float lw = 480;
                var lRect = new RectangleF(fullRect.Width - lw - 12, 8, lw, 23);
                WarGdi.DrawPlaqueFrame(g, lRect, 5f, Color.FromArgb(208, 30, 27, 22), BorderCol, Color.FromArgb(52, 46, 36));
                string legend = uiScale < 0.9f && _mapZoom <= 1.25f
                    ? "■藍 國軍   ■紅 中共   ■紫 俄占   左右各半＝交戰   ★北京   ⚓港口   腹地兵力懸停查閱"
                    : "■藍 國軍   ■紅 中共   ■紫斜紋 俄占   左右各半＝交戰   ★北京   ⚓港口";
                g.DrawString(legend, legFont, legTxtBr, lRect.X + 9, lRect.Y + 4);
            }

            if (!string.IsNullOrEmpty(State?.FrontHeadline))
            {
                using var hlFont = new Font("Microsoft JhengHei UI", Math.Clamp(8.2f * uiScale, 7.6f, 8.6f), FontStyle.Bold);
                string kind = State.FrontHeadlineKind;
                Color hlBg = kind == "threat" ? Color.FromArgb(216, 62, 28, 28)
                           : kind == "window" ? Color.FromArgb(216, 26, 56, 38)
                           : Color.FromArgb(204, 30, 27, 22);
                Color hlBorder = kind == "threat" ? Color.FromArgb(224, 212, 82, 72)
                               : kind == "window" ? Color.FromArgb(220, 92, 186, 124)
                               : GoldDim;
                Color hlInk = kind == "threat" ? Color.FromArgb(250, 238, 198, 192)
                            : kind == "window" ? Color.FromArgb(226, 246, 224)
                            : TextMain;
                var hsz = g.MeasureString(State.FrontHeadline, hlFont);
                float hw = Math.Clamp(hsz.Width + 28f, 260f, Math.Max(260f, fullRect.Width - 40f));
                var hRect = new RectangleF((fullRect.Width - hw) * 0.5f, 36, hw, 22);
                WarGdi.DrawPlaqueFrame(g, hRect, 5f, hlBg, hlBorder, Color.FromArgb(46, 40, 32));
                using var hlBr = new SolidBrush(hlInk);
                using var hlFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(State.FrontHeadline, hlFont, hlBr, hRect, hlFmt);
            }

            DrawMapZoomBar(g, fullRect);

            if (_leftDragCommand && _leftDragSource != null && GameData.Nodes.TryGetValue(_leftDragSource, out var leftDragSrcNode))
            {
                var pStart = GetNodeDisplayPoint(leftDragSrcNode, vp);
                using var dragPen = new Pen(Color.FromArgb(238, 224, 182, 92), 3.0f)
                {
                    DashStyle = DashStyle.Solid,
                    CustomEndCap = new AdjustableArrowCap(5.5f, 5.5f)
                };
                g.DrawLine(dragPen, pStart, _mousePos);
            }
            if (_rightDragging && _rightDragSource != null && GameData.Nodes.TryGetValue(_rightDragSource, out var dragSrcNode))
            {
                var pStart = GetNodeDisplayPoint(dragSrcNode, vp);
                using var dragPen = new Pen(GoldCol, 2.8f) { DashStyle = DashStyle.Dash, CustomEndCap = new AdjustableArrowCap(5f, 5f) };
                g.DrawLine(dragPen, pStart, _mousePos);
                using var df = new Font("Microsoft JhengHei UI", 8.2f, FontStyle.Bold);
                using var dBr = new SolidBrush(GoldCol);
                var dsz = g.MeasureString("放開右鍵派兵", df);
                var dRect = new RectangleF(_mousePos.X + 12, _mousePos.Y - 12, dsz.Width + 10, dsz.Height + 4);
                WarGdi.DrawPlaqueFrame(g, dRect, 4f, Color.FromArgb(236, 26, 23, 18), GoldCol, BorderCol);
                g.DrawString("放開右鍵派兵", df, dBr, dRect.X + 5, dRect.Y + 2);
            }

            // 13. 滑鼠懸停在途／籌備標籤或省分極簡提示
            var hoveredOrder = !_mapDidDrag && !_leftDragCommand && !_rightDragging
                ? _orderBadgeHits.FirstOrDefault(h => h.rect.Contains(_mousePos))
                : default;
            if (!string.IsNullOrEmpty(hoveredOrder.tip) && !_hudTooltipCard.Visible)
            {
                DrawOrderBadgeTooltip(g, hoveredOrder.tip);
            }
            else if (_hoverNode != null && GameData.Nodes.TryGetValue(_hoverNode, out var hn) && !_hudTooltipCard.Visible && !_mapDidDrag)
            {
                DrawNodeTooltip(g, hn, reachableDest);
            }
        }

        private void DrawCoastalBathymetryHalo(Graphics g, RectangleF vp, float scale)
        {
            (int alpha, int r, int gr, int b, float width)[] bands =
            {
                (26, 62, 108, 114, 22f),
                (40, 82, 134, 138, 11f),
                (58, 128, 174, 166, 4.5f)
            };

            foreach (var band in bands)
            {
                using var pen = new Pen(Color.FromArgb(band.alpha, band.r, band.gr, band.b), Math.Max(1.5f, band.width * scale))
                {
                    LineJoin = LineJoin.Round
                };
                foreach (var rings in ChinaMapGeo.NodeOuterRings.Values)
                {
                    foreach (var ring in rings)
                    {
                        if (ring.Length < 3) continue;
                        var pts = ring.Select(p => MapToScreen(GameData.Project(p.X, p.Y), vp)).ToArray();
                        g.DrawPolygon(pen, pts);
                    }
                }
                foreach (var ring in ChinaMapGeo.NeighborAsianRings)
                {
                    if (ring.Length < 3) continue;
                    var pts = ring.Select(p => MapToScreen(GameData.Project(p.X, p.Y), vp)).ToArray();
                    g.DrawPolygon(pen, pts);
                }
            }
        }

        private static void DrawCompassRoseWatermark(Graphics g, PointF center, float roseScale, float uiScale)
        {
            float rad = 22f * roseScale;
            if (rad < 10f) return;
            using var pen = new Pen(Color.FromArgb(46, 214, 186, 126), 1.1f);
            using var br = new SolidBrush(Color.FromArgb(42, 214, 186, 126));
            using var f = new Font("Consolas", Math.Max(6.5f, 7.5f * uiScale), FontStyle.Bold);
            g.DrawEllipse(pen, center.X - rad, center.Y - rad, rad * 2, rad * 2);
            g.DrawEllipse(pen, center.X - rad * 0.72f, center.Y - rad * 0.72f, rad * 1.44f, rad * 1.44f);
            g.DrawLine(pen, center.X, center.Y - rad * 1.2f, center.X, center.Y + rad * 1.2f);
            g.DrawLine(pen, center.X - rad * 1.2f, center.Y, center.X + rad * 1.2f, center.Y);
            PointF[] nTri =
            {
                new PointF(center.X, center.Y - rad * 1.15f),
                new PointF(center.X - 4f * roseScale, center.Y),
                new PointF(center.X + 4f * roseScale, center.Y)
            };
            g.FillPolygon(br, nTri);
            var nsz = g.MeasureString("N", f);
            g.DrawString("N", f, br, center.X - nsz.Width * 0.5f, center.Y - rad * 1.25f - nsz.Height);
        }

        private static void DrawMapTableVignette(Graphics g, Rectangle r)
        {
            int edge = 28;
            if (r.Width <= edge * 2 || r.Height <= edge * 2) return;
            using (var topBr = new LinearGradientBrush(new Rectangle(0, 0, r.Width, edge), Color.FromArgb(80, 12, 10, 8), Color.FromArgb(0, 12, 10, 8), LinearGradientMode.Vertical))
                g.FillRectangle(topBr, 0, 0, r.Width, edge);
            using (var botBr = new LinearGradientBrush(new Rectangle(0, r.Height - edge, r.Width, edge), Color.FromArgb(0, 12, 10, 8), Color.FromArgb(90, 12, 10, 8), LinearGradientMode.Vertical))
                g.FillRectangle(botBr, 0, r.Height - edge, r.Width, edge);
        }

        private void DrawMapZoomBar(Graphics g, Rectangle fullRect)
        {
            float barW = 232f;
            float barH = 28f;
            float bx = fullRect.Width - barW - 12f;
            float by = fullRect.Height - barH - 10f;

            float abW = 148f;
            _autoNavAirRect = new RectangleF(bx - abW - 8f, by, abW, barH);
            WarGdi.DrawPlaqueFrame(g, _autoNavAirRect, 6f, Color.FromArgb(228, 34, 48, 60), GoldDim, Color.FromArgb(52, 62, 68));
            using (var af = new Font("Microsoft JhengHei UI", 8.2f, FontStyle.Bold))
            using (var aBr = new SolidBrush(GoldCol))
            {
                var asf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("⚓ 一鍵海空佈防", af, aBr, _autoNavAirRect, asf);
            }

            _autoLandRect = new RectangleF(_autoNavAirRect.Left - 166f, by, 158f, barH);
            bool autoOn = State != null && State.AutoFrontFill;
            WarGdi.DrawPlaqueFrame(g, _autoLandRect, 6f,
                autoOn ? Color.FromArgb(232, 34, 62, 50) : Color.FromArgb(228, 44, 38, 28),
                autoOn ? GoodGreen : GoldDim,
                Color.FromArgb(68, 58, 42));
            using (var landFont = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold))
            using (var landBrush = new SolidBrush(autoOn ? Color.FromArgb(166, 232, 186) : TextSub))
            {
                var landFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(autoOn ? "⛨ 自動補防：開" : "⛨ 自動補防：關", landFont, landBrush, _autoLandRect, landFormat);
            }

            var pill = new RectangleF(bx, by, barW, barH);
            WarGdi.DrawPlaqueFrame(g, pill, 6f, Color.FromArgb(228, 28, 25, 20), _mapZoom > 1.01f ? GoldCol : BorderCol, Color.FromArgb(62, 54, 40));

            _zoomMinusRect = new RectangleF(bx + 6f, by + 5f, 22f, 18f);
            _zoomPlusRect = new RectangleF(bx + 94f, by + 5f, 22f, 18f);
            _zoomResetRect = new RectangleF(bx + 120f, by + 5f, 106f, 18f);

            using var btnBr = new SolidBrush(BgCardHi);
            using var btnPen = new Pen(BorderCol, 1f);
            using var f = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
            using var txtBr = new SolidBrush(TextMain);
            using var goldBr = new SolidBrush(GoldCol);
            using var subBr = new SolidBrush(TextSub);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            using (var mp = WarGdi.CreateRoundedPath(_zoomMinusRect, 3.5f))
            {
                g.FillPath(btnBr, mp);
                g.DrawPath(btnPen, mp);
            }
            g.DrawString("－", f, _mapZoom > 1.01f ? txtBr : subBr, _zoomMinusRect, sf);

            g.DrawString($"{_mapZoom:F1}×", f, _mapZoom > 1.01f ? goldBr : txtBr, new RectangleF(bx + 30f, by + 4f, 62f, 20f), sf);

            using (var pp = WarGdi.CreateRoundedPath(_zoomPlusRect, 3.5f))
            {
                g.FillPath(btnBr, pp);
                g.DrawPath(btnPen, pp);
            }
            g.DrawString("＋", f, _mapZoom < 3.99f ? txtBr : subBr, _zoomPlusRect, sf);

            string rTxt = _mapZoom > 1.01f ? "重設 · 拖曳平移" : "滾輪縮放地圖";
            g.DrawString(rTxt, f, _mapZoom > 1.01f ? goldBr : subBr, _zoomResetRect, sf);
        }

        private void DrawPolygonRings(Graphics g, PointF[][] rings, RectangleF vp, Color fillCol, Color borderCol, float borderWidth)
        {
            using var br = new SolidBrush(fillCol);
            using var pen = new Pen(borderCol, Math.Max(0.7f, borderWidth));
            foreach (var ring in rings)
            {
                if (ring.Length < 3) continue;
                var pts = ring.Select(p => MapToScreen(GameData.Project(p.X, p.Y), vp)).ToArray();
                g.FillPolygon(br, pts);
                g.DrawPolygon(pen, pts);
            }
        }

        private void DrawCenteredText(Graphics g, string text, Font font, Brush brush, PointF center)
        {
            var sz = g.MeasureString(text, font);
            g.DrawString(text, font, brush, center.X - sz.Width * 0.5f, center.Y - sz.Height * 0.5f);
        }

        private PointF GetNodeDisplayPoint(NodeDef n, RectangleF vp)
        {
            if (n.HasCallout)
                return MapToScreen(GameData.Project(n.CalloutLon, n.CalloutLat), vp);
            return MapToScreen(GameData.Project(n.Lon, n.Lat), vp);
        }

        private void DrawSeaZones(Graphics g, RectangleF vp, float uiScale)
        {
            using var waterF = new Font("Microsoft JhengHei UI", Math.Max(7.8f, 9.0f * uiScale), FontStyle.Bold | FontStyle.Italic);
            using var waterBr = new SolidBrush(Color.FromArgb(105, 146, 186, 196));
            using var titleF = new Font("Microsoft JhengHei UI", Math.Max(7.1f, 8.0f * uiScale), FontStyle.Bold);
            using var smallF = new Font("Microsoft JhengHei UI", Math.Max(6.4f, 7.0f * uiScale));
            using var leadPen = new Pen(Color.FromArgb(95, 188, 168, 122), Math.Max(0.9f, 1.0f * uiScale)) { DashStyle = DashStyle.Dot };

            foreach (var z in GameData.Zones.Values)
            {
                var waterPt = MapToScreen(GameData.Project(z.Lon, z.Lat), vp);
                DrawCenteredText(g, $"～ {z.Name} ～", waterF, waterBr, waterPt);

                var boxPt = MapToScreen(new PointF((float)z.MapX, (float)z.MapY), vp);
                float bw = 110 * uiScale, bh = 44 * uiScale;
                var rect = new RectangleF(boxPt.X - bw / 2, boxPt.Y - bh / 2, bw, bh);

                g.DrawLine(leadPen, waterPt, new PointF(rect.Left, boxPt.Y));

                bool sel = SelectedZone == z.Id || _hoverZone == z.Id;
                WarGdi.DrawPlaqueFrame(g, rect, 5.5f * uiScale,
                    Color.FromArgb(228, 30, 27, 22),
                    sel ? GoldCol : BorderCol,
                    Color.FromArgb(62, 54, 42));

                double seaSup = SimEngine.SeaSup(State, z.Id);
                double airSup = SimEngine.AirSup(State, z.Id);
                int fleets = SimEngine.UnitsAt(State, z.Id, UnitDomain.Sea).Count;
                int wings = SimEngine.UnitsAt(State, z.Id, UnitDomain.Air).Count;

                string usBadge = (State.UsProtect && (z.Id == "Z2" || (z.Id == "Z3" && State.UsRel >= 55))) ? " [🇺🇸第七艦隊]" : "";
                using var hdrBr = new SolidBrush(sel ? GoldCol : Color.FromArgb(224, 216, 194));
                g.DrawString($"⚓ {z.Name}{usBadge} (艦{fleets}/空{wings})", titleF, hdrBr, rect.X + 6 * uiScale, rect.Y + 3 * uiScale);

                double prcSea = SimEngine.PrcSea(State, z.Id);
                double prcAir = SimEngine.PrcAir(State, z.Id);
                DrawSupBar(g, new RectangleF(rect.X + 6 * uiScale, rect.Y + 18 * uiScale, bw - 12 * uiScale, 10 * uiScale), seaSup, $"海{seaSup * 100:F0}% 共{prcSea:F0}", smallF);
                DrawSupBar(g, new RectangleF(rect.X + 6 * uiScale, rect.Y + 30 * uiScale, bw - 12 * uiScale, 10 * uiScale), airSup, $"空{airSup * 100:F0}% 共{prcAir:F0}", smallF);
            }
        }

        private void DrawSupBar(Graphics g, RectangleF r, double rocShare, string label, Font f)
        {
            using var prcBr = new SolidBrush(Color.FromArgb(196, 134, 58, 52));
            using var rocBr = new SolidBrush(Color.FromArgb(220, 56, 104, 146));
            using var path = WarGdi.CreateRoundedPath(r, 2.5f);
            g.SetClip(path);
            g.FillRectangle(prcBr, r);
            float rw = (float)(r.Width * SimEngine.Clamp(rocShare, 0, 1));
            g.FillRectangle(rocBr, r.X, r.Y, rw, r.Height);
            g.ResetClip();
            using var rimPen = new Pen(Color.FromArgb(95, 190, 174, 140), 0.7f);
            g.DrawPath(rimPen, path);
            using var txtBr = new SolidBrush(TextMain);
            g.DrawString(label, f, txtBr, r.X + 3, r.Y - 1);
        }

        private void DrawMovementLines(Graphics g, RectangleF vp, float uiScale)
        {
            if (State.LastReport != null)
            {
                using var prcPen = new Pen(Color.FromArgb(215, 206, 86, 76), 2.4f * uiScale)
                {
                    DashStyle = DashStyle.Dash,
                    CustomEndCap = new AdjustableArrowCap(4f, 4f)
                };
                foreach (var mv in State.LastReport.PrcMoves)
                {
                    var p1 = MapToScreen(GameData.Project(GameData.Nodes[mv.From].Lon, GameData.Nodes[mv.From].Lat), vp);
                    var p2 = MapToScreen(GameData.Project(GameData.Nodes[mv.To].Lon, GameData.Nodes[mv.To].Lat), vp);
                    g.DrawLine(prcPen, p1, p2);
                }
            }

            var grouped = State.Orders.GroupBy(o => $"{o.From}->{o.To}:{o.Kind}:{o.IsPrep}");
            foreach (var grp in grouped)
            {
                var first = grp.First();
                var pathNodes = (first.Path != null && first.Path.Count >= 2 ? first.Path : new List<string> { first.From, first.To })
                    .Where(id => GameData.Nodes.ContainsKey(id))
                    .ToList();
                if (pathNodes.Count < 2) continue;

                var pts = pathNodes.Select(id => MapToScreen(GameData.Project(GameData.Nodes[id].Lon, GameData.Nodes[id].Lat), vp)).ToArray();
                double seaPct = string.IsNullOrEmpty(first.Zone) ? 100.0 : SimEngine.SeaSup(State, first.Zone) * 100.0;
                Color col = first.IsPrep
                    ? (seaPct >= 50.0 ? Color.FromArgb(240, 96, 204, 218) : Color.FromArgb(240, 232, 134, 86))
                    : first.Kind == "sea" ? Color.FromArgb(236, 134, 198, 214)
                    : first.Kind == "air" ? Color.FromArgb(236, 204, 166, 208)
                    : GoldCol;

                using var pen = new Pen(col, (first.IsPrep ? 2.4f : 2.9f) * uiScale)
                {
                    DashStyle = first.IsPrep ? DashStyle.Dot : (first.Kind == "land" ? DashStyle.Solid : DashStyle.Dash),
                    CustomEndCap = new AdjustableArrowCap(4.5f, 4.5f)
                };
                g.DrawLines(pen, pts);
            }
        }

        private void DrawOrderBadgesAndTokens(Graphics g, RectangleF vp, float uiScale)
        {
            _orderBadgeHits.Clear();
            var grouped = State.Orders.GroupBy(o => $"{o.From}->{o.To}:{o.Kind}:{o.IsPrep}");
            foreach (var grp in grouped)
            {
                var orders = grp.ToList();
                var first = orders[0];
                var pathNodes = (first.Path != null && first.Path.Count >= 2 ? first.Path : new List<string> { first.From, first.To })
                    .Where(id => GameData.Nodes.ContainsKey(id))
                    .ToList();
                if (pathNodes.Count < 2) continue;

                var pts = pathNodes.Select(id => MapToScreen(GameData.Project(GameData.Nodes[id].Lon, GameData.Nodes[id].Lat), vp)).ToArray();
                bool isHostile = SimEngine.IsHostileTarget(State, first.To);
                bool hasArm = orders.Any(o => SimEngine.UnitById(State, o.UnitId)?.Type == "arm");
                double seaPct = string.IsNullOrEmpty(first.Zone) ? 100.0 : SimEngine.SeaSup(State, first.Zone) * 100.0;
                bool seaOk = seaPct >= 50.0;

                Color col = first.IsPrep
                    ? (seaOk ? Color.FromArgb(240, 96, 204, 218) : Color.FromArgb(240, 232, 134, 86))
                    : first.Kind == "sea" ? Color.FromArgb(236, 134, 198, 214)
                    : first.Kind == "air" ? Color.FromArgb(236, 204, 166, 208)
                    : GoldCol;

                PointF markerPt;
                if (first.IsPrep)
                {
                    markerPt = new PointF((pts[0].X + pts[^1].X) * 0.5f + 18f * uiScale, (pts[0].Y + pts[^1].Y) * 0.5f);
                }
                else
                {
                    var seg = SimEngine.GetOrderPosition(first);
                    var sp1 = MapToScreen(GameData.Project(GameData.Nodes[seg.segFrom].Lon, GameData.Nodes[seg.segFrom].Lat), vp);
                    var sp2 = MapToScreen(GameData.Project(GameData.Nodes[seg.segTo].Lon, GameData.Nodes[seg.segTo].Lat), vp);
                    float lt = (float)SimEngine.Clamp(seg.segT, 0.22, 0.82);
                    markerPt = new PointF(sp1.X + (sp2.X - sp1.X) * lt, sp1.Y + (sp2.Y - sp1.Y) * lt);
                }

                var uids = orders.Select(o => o.UnitId).ToList();
                string fromName = GameData.Nodes[first.From].Name;
                string toName = GameData.Nodes[first.To].Name;
                bool isPortDest = GameData.Nodes[first.To].Port;

                if (first.IsPrep)
                {
                    string zName = GameData.Zones.TryGetValue(first.Zone ?? "", out var zv) ? zv.Name : "沿線";
                    string portMark = isPortDest ? "·港" : "";
                    string seaMark = seaOk ? $"海{seaPct:F0}%✓" : $"海{seaPct:F0}%✖";
                    string label = $"⚓{orders.Count}師 {first.PrepDays}/{first.PrepRequired}天 · {seaMark}{portMark}";
                    using var f = new Font("Microsoft JhengHei UI", Math.Max(6.8f, 7.4f * uiScale), FontStyle.Bold);
                    using var fg = new SolidBrush(col);
                    var sz = g.MeasureString(label, f);
                    var pill = new RectangleF(markerPt.X - sz.Width / 2 - 5, markerPt.Y - sz.Height / 2 - 3, sz.Width + 10, sz.Height + 7);
                    WarGdi.DrawPlaqueFrame(g, pill, 4f, Color.FromArgb(242, 24, 22, 18), col, BorderCol);
                    g.DrawString(label, f, fg, pill.X + 5, pill.Y + 1.5f);

                    float progFrac = first.PrepRequired > 0 ? (float)first.PrepDays / first.PrepRequired : 1f;
                    var barBg = new RectangleF(pill.X + 5, pill.Bottom - 4f, pill.Width - 10, 2.5f);
                    using (var bgBr = new SolidBrush(Color.FromArgb(95, 70, 62, 48))) g.FillRectangle(bgBr, barBg);
                    using (var pgBr = new SolidBrush(seaOk ? GoodGreen : BadRed)) g.FillRectangle(pgBr, barBg.X, barBg.Y, barBg.Width * (float)SimEngine.Clamp(progFrac, 0.0, 1.0), barBg.Height);

                    string tip = $"兩棲登陸計畫：{fromName} → {toName}（{orders.Count} 師）\n" +
                                 $"籌備進度：{first.PrepDays} / {first.PrepRequired} 天（滿期自動起航）\n" +
                                 $"{zName}制海權：{seaPct:F0}%（需 ≥ 50% {(seaOk ? "已達標 ✓" : "未達標，請增派艦隊！")}）" +
                                 (isPortDest ? "\n⚓ 奪取該港口可立即接通大後方補給與就地徵募" : "") +
                                 "\n右鍵點擊此牌可撤回登陸計畫";
                    _orderBadgeHits.Add((pill, uids, true, tip));
                }
                else
                {
                    var hit = DrawMarchingToken(g, markerPt, first.Kind, hasArm, orders.Count, first.RemainingDays, (float)first.VisualProgress, col, isHostile, uiScale);
                    string kindName = first.Kind == "sea" ? "跨海航渡" : first.Kind == "air" ? "空降突擊" : "陸路行軍";
                    string tip = $"{kindName}：{fromName} → {toName}（{orders.Count} 支）\n" +
                                 $"行進進度：{first.ElapsedDays}/{first.TotalDays} 天（剩餘 {first.RemainingDays} 天抵達{(isHostile ? "交戰" : "")}）\n" +
                                 "右鍵點擊小圖示或於出發省點 [撤令] 可撤回";
                    _orderBadgeHits.Add((hit, uids, false, tip));
                }
            }
        }

        private RectangleF DrawMarchingToken(Graphics g, PointF center, string kind, bool hasArm, int count, int remDays, float prog, Color accent, bool hostile, float uiScale)
        {
            float w = 52f * uiScale;
            float h = 22f * uiScale;
            var rect = new RectangleF(center.X - w * 0.5f, center.Y - h * 0.5f, w, h);

            var sh = new RectangleF(rect.X + 1.5f, rect.Y + 1.8f, rect.Width, rect.Height);
            using (var shPath = WarGdi.CreateRoundedPath(sh, 4f))
            using (var shBr = new SolidBrush(Color.FromArgb(145, 8, 7, 5)))
                g.FillPath(shBr, shPath);

            Color fill = hostile ? Color.FromArgb(242, 76, 38, 28)
                       : kind == "sea" ? Color.FromArgb(242, 30, 56, 68)
                       : kind == "air" ? Color.FromArgb(242, 58, 42, 72)
                       : Color.FromArgb(242, 34, 56, 78);
            WarGdi.DrawPlaqueFrame(g, rect, 4f, fill, accent, BorderCol);

            // 左側迷你軍徽向量圖示
            float ix = rect.X + 4.5f * uiScale;
            float iy = rect.Y + 4f * uiScale;
            float isz = 11f * uiScale;
            using (var iPen = new Pen(accent, Math.Max(1.2f, 1.4f * uiScale)) { LineJoin = LineJoin.Round })
            using (var iBr = new SolidBrush(accent))
            {
                if (kind == "sea")
                {
                    // 登陸艦剪影
                    PointF[] hull = { new(ix, iy + isz * 0.55f), new(ix + isz, iy + isz * 0.55f), new(ix + isz * 0.8f, iy + isz * 0.9f), new(ix + isz * 0.15f, iy + isz * 0.9f) };
                    g.FillPolygon(iBr, hull);
                    g.DrawLine(iPen, ix + isz * 0.45f, iy + isz * 0.12f, ix + isz * 0.45f, iy + isz * 0.55f);
                }
                else if (kind == "air")
                {
                    // 雙翼飛行圖示
                    PointF[] wing = { new(ix + isz * 0.5f, iy + isz * 0.1f), new(ix + isz, iy + isz * 0.72f), new(ix + isz * 0.5f, iy + isz * 0.55f), new(ix, iy + isz * 0.72f) };
                    g.FillPolygon(iBr, wing);
                }
                else if (hasArm)
                {
                    // 裝甲履帶菱章
                    g.DrawEllipse(iPen, ix + 0.5f, iy + isz * 0.28f, isz - 1f, isz * 0.52f);
                    g.DrawLine(iPen, ix + isz * 0.5f, iy + isz * 0.45f, ix + isz * 1.02f, iy + isz * 0.28f);
                }
                else
                {
                    // 步兵盾＋箭頭行軍徽章
                    var badge = new RectangleF(ix + 1f, iy + 1.5f, isz - 2f, isz - 3f);
                    g.DrawRectangle(iPen, badge.X, badge.Y, badge.Width, badge.Height);
                    g.DrawLine(iPen, badge.Left, badge.Top, badge.Right, badge.Bottom);
                    g.DrawLine(iPen, badge.Right, badge.Top, badge.Left, badge.Bottom);
                }
            }

            string txt = kind == "air" ? $"{count}旅·{remDays}天" : $"{count}師·{remDays}天";
            using (var tf = new Font("Microsoft JhengHei UI", Math.Max(6.5f, 7.1f * uiScale), FontStyle.Bold))
            using (var tBr = new SolidBrush(TextMain))
            {
                g.DrawString(txt, tf, tBr, rect.X + 17.5f * uiScale, rect.Y + 2.5f * uiScale);
            }

            // 底部微型進度條
            var pBg = new RectangleF(rect.X + 4f * uiScale, rect.Bottom - 3.6f * uiScale, rect.Width - 8f * uiScale, 2.2f * uiScale);
            using (var pBgBr = new SolidBrush(Color.FromArgb(90, 20, 18, 14))) g.FillRectangle(pBgBr, pBg);
            using (var pFgBr = new SolidBrush(hostile ? BadRed : GoodGreen)) g.FillRectangle(pFgBr, pBg.X, pBg.Y, pBg.Width * (float)SimEngine.Clamp(prog, 0.05, 1.0), pBg.Height);

            return rect;
        }

        private void DrawOrderBadgeTooltip(Graphics g, string text)
        {
            using var f = new Font("Microsoft JhengHei UI", 8.6f);
            var sz = g.MeasureString(text, f);
            float tx = Math.Min(_mapPanel.Width - sz.Width - 20, _mousePos.X + 16);
            float ty = Math.Max(10, _mousePos.Y - sz.Height - 14);
            var rect = new RectangleF(tx, ty, sz.Width + 18, sz.Height + 12);
            WarGdi.DrawPlaqueFrame(g, rect, 6f, Color.FromArgb(246, 28, 25, 20), GoldCol, BorderCol);
            using var fg = new SolidBrush(TextMain);
            g.DrawString(text, f, fg, rect.X + 9, rect.Y + 6);
        }

        private void DrawNodesAndCounters(Graphics g, RectangleF vp, float geoScale, float uiScale,
            Dictionary<string, (string kind, string oddsLabel, string tier)> reachableDest)
        {
            using var nameF = new Font("Microsoft JhengHei UI", Math.Max(7.2f, 8.2f * uiScale), FontStyle.Bold);
            using var cntF = new Font("Microsoft JhengHei UI", Math.Max(6.6f, 7.4f * uiScale), FontStyle.Bold);
            using var oddsF = new Font("Microsoft JhengHei UI", Math.Max(6.5f, 7.2f * uiScale), FontStyle.Bold);

            foreach (var n in GameData.Nodes.Values)
            {
                var truePt = MapToScreen(GameData.Project(n.Lon, n.Lat), vp);
                var pt = GetNodeDisplayPoint(n, vp);
                Faction ctrl = State.Control[n.Id];
                bool isSel = SelectedNode == n.Id;
                bool isBattle = State.LastReport != null && State.LastReport.BattleNodes.Contains(n.Id);

                bool contestedNode = SimEngine.IsContested(State, n.Id);
                Color dotCol = ctrl == Faction.ROC ? Color.FromArgb(72, 134, 184)
                             : ctrl == Faction.USSR ? Color.FromArgb(164, 92, 142)
                             : Color.FromArgb(192, 82, 74);
                Color otherDot = ctrl == Faction.ROC ? Color.FromArgb(192, 82, 74) : Color.FromArgb(72, 134, 184);

                double calloutDist = Math.Sqrt((pt.X - truePt.X) * (pt.X - truePt.X) + (pt.Y - truePt.Y) * (pt.Y - truePt.Y));
                if (calloutDist > 8.0 * geoScale)
                {
                    using var leaderPen = new Pen(isSel ? GoldCol : Color.FromArgb(165, 208, 188, 142), Math.Max(0.95f, 1.15f * uiScale)) { DashStyle = DashStyle.Dot };
                    g.DrawLine(leaderPen, truePt, pt);

                    float ir = 3.0f * uiScale;
                    using var islBr = new SolidBrush(dotCol);
                    using var islPen = new Pen(Color.FromArgb(228, 218, 194), 1.0f);
                    g.FillEllipse(islBr, truePt.X - ir, truePt.Y - ir, ir * 2, ir * 2);
                    g.DrawEllipse(islPen, truePt.X - ir, truePt.Y - ir, ir * 2, ir * 2);
                }

                if (isBattle)
                {
                    using var battlePen = new Pen(Color.FromArgb(228, 224, 124, 68), 2.2f * uiScale);
                    g.DrawEllipse(battlePen, pt.X - 16 * uiScale, pt.Y - 16 * uiScale, 32 * uiScale, 32 * uiScale);
                }

                bool isRocFront = State.AutoFrontFill && ctrl == Faction.ROC && n.Kind == NodeKind.Mainland && n.Adj.Any(a => State.Control[a] != Faction.ROC);
                if (isRocFront && !isSel)
                {
                    using var fPen = new Pen(Color.FromArgb(165, 96, 178, 232), 1.6f * uiScale) { DashStyle = DashStyle.Dash };
                    g.DrawEllipse(fPen, pt.X - 11 * uiScale, pt.Y - 11 * uiScale, 22 * uiScale, 22 * uiScale);
                }

                float r = (n.Kind == NodeKind.Island ? 5.2f : n.Capital ? 7.2f : 5.5f) * uiScale;
                using (var shBr = new SolidBrush(Color.FromArgb(120, 12, 10, 8)))
                using (var dotBr = new SolidBrush(dotCol))
                using (var dotPen = new Pen(isSel ? GoldCol : (State.PriorityTargets.Contains(n.Id) ? GoodGreen : Color.FromArgb(224, 212, 182)), isSel ? 2.2f : (State.PriorityTargets.Contains(n.Id) ? 1.8f : 1.15f)))
                {
                    g.FillEllipse(shBr, pt.X - r + 1.2f, pt.Y - r + 1.4f, r * 2, r * 2);
                    if (contestedNode)
                    {
                        var pie = new RectangleF(pt.X - r, pt.Y - r, r * 2, r * 2);
                        using var holdBr = new SolidBrush(dotCol);
                        using var otherBr = new SolidBrush(otherDot);
                        g.FillPie(holdBr, pie.X, pie.Y, pie.Width, pie.Height, 90f, 180f);
                        g.FillPie(otherBr, pie.X, pie.Y, pie.Width, pie.Height, 270f, 180f);
                    }
                    else g.FillEllipse(dotBr, pt.X - r, pt.Y - r, r * 2, r * 2);
                    g.DrawEllipse(dotPen, pt.X - r, pt.Y - r, r * 2, r * 2);
                }

                double nodeAir = SimEngine.NodeAirSup(State, n.Id);
                bool airRecon = nodeAir >= 0.55 && State.Units.Any(u => u.Type == "air" && u.Str >= 25.0);
                string priMark = State.PriorityTargets.Contains(n.Id) ? "[重點]" : "";
                string prefix = n.Capital ? "★" : (ctrl == Faction.USSR ? "[俄]" : (n.Port ? "⚓" : ""));
                string airMark = airRecon ? "✈" : (!SimEngine.HasIntel(State, n.Id) ? "☁" : "");
                string label = $"{priMark}{prefix}{n.Name}{airMark}";
                var lblSz = g.MeasureString(label, nameF);
                float lx = pt.X - lblSz.Width / 2;
                float ly = pt.Y - r - lblSz.Height - 1.5f * uiScale;

                if (reachableDest.TryGetValue(n.Id, out var reach))
                {
                    Color ringCol = reach.tier == "high" ? GoodGreen
                                  : reach.tier == "even" ? GoldCol
                                  : reach.tier == "friendly" ? Color.FromArgb(132, 194, 226)
                                  : Color.FromArgb(224, 118, 76);
                    using var haloPen = new Pen(Color.FromArgb(215, ringCol), 2.1f * uiScale);
                    g.DrawEllipse(haloPen, pt.X - 12 * uiScale, pt.Y - 12 * uiScale, 24 * uiScale, 24 * uiScale);

                    string badge = reach.kind == "sea" ? $"⚓{reach.oddsLabel}"
                                 : reach.kind == "air" ? $"▲{reach.oddsLabel}"
                                 : $"▶{reach.oddsLabel}";
                    var bsz = g.MeasureString(badge, oddsF);
                    var bRect = new RectangleF(pt.X - bsz.Width * 0.5f - 4, ly - bsz.Height - 3 * uiScale, bsz.Width + 8, bsz.Height + 1.5f);
                    using var bPath = WarGdi.CreateRoundedPath(bRect, 3f);
                    using var bBg = new SolidBrush(Color.FromArgb(234, 26, 23, 18));
                    using var bPen = new Pen(ringCol, 1.05f);
                    using var bFg = new SolidBrush(ringCol);
                    g.FillPath(bBg, bPath);
                    g.DrawPath(bPen, bPath);
                    g.DrawString(badge, oddsF, bFg, bRect.X + 4, bRect.Y + 0.5f);
                }

                var lblBox = new RectangleF(lx - 3, ly, lblSz.Width + 6, lblSz.Height);
                using (var lblPath = WarGdi.CreateRoundedPath(lblBox, 3.2f))
                using (var lblBg = new SolidBrush(Color.FromArgb(196, 24, 21, 17)))
                using (var lblFg = new SolidBrush(isSel ? GoldCol : TextMain))
                {
                    g.FillPath(lblBg, lblPath);
                    g.DrawString(label, nameF, lblFg, lx, ly);
                }

                double rocStr = SimEngine.LandStr(SimEngine.UnitsAt(State, n.Id, UnitDomain.Land));
                var (estG, exact) = SimEngine.EstGarrison(State, n.Id);
                bool showRoc = rocStr > 0.05;
                bool compactMap = uiScale < 0.9f && _mapZoom <= 1.25f;
                bool showEnemy = (estG > 0.05 || ctrl != Faction.ROC)
                    && (!compactMap || isSel || _hoverNode == n.Id || isBattle || isRocFront || contestedNode
                        || State.PriorityTargets.Contains(n.Id) || reachableDest.ContainsKey(n.Id));

                float cy = pt.Y + r + 2.0f * uiScale;
                if (showRoc && showEnemy)
                {
                    DrawUnitCounter(g, new PointF(pt.X - 20 * uiScale, cy), $"國 {rocStr:F1}", RocBlue, cntF, uiScale);
                    string ePrefix = ctrl == Faction.USSR ? "俄" : "共";
                    Color eCol = ctrl == Faction.USSR ? UssrPurple : (exact ? PrcRed : Color.FromArgb(148, 64, 58));
                    string eTxt = exact ? $"{ePrefix} {estG:F1}" : $"{ePrefix} ~{estG:F0}?[霧]";
                    DrawUnitCounter(g, new PointF(pt.X + 20 * uiScale, cy), eTxt, eCol, cntF, uiScale);
                }
                else if (showRoc)
                {
                    DrawUnitCounter(g, new PointF(pt.X, cy), $"國 {rocStr:F1}", RocBlue, cntF, uiScale);
                }
                else if (showEnemy)
                {
                    string ePrefix = ctrl == Faction.USSR ? "俄" : "共";
                    Color eCol = ctrl == Faction.USSR ? UssrPurple : (exact ? PrcRed : Color.FromArgb(148, 64, 58));
                    string eTxt = exact ? $"{ePrefix} {estG:F1}" : $"{ePrefix} ~{estG:F0}?[霧]";
                    DrawUnitCounter(g, new PointF(pt.X, cy), eTxt, eCol, cntF, uiScale);
                }
            }
        }

        public static void FillContestedPolygon(Graphics g, PointF[] pts, Color hold, Color other)
        {
            if (g == null || pts == null || pts.Length < 3) return;
            float minX = pts[0].X, maxX = pts[0].X, minY = pts[0].Y, maxY = pts[0].Y;
            for (int i = 1; i < pts.Length; i++)
            {
                if (pts[i].X < minX) minX = pts[i].X;
                if (pts[i].X > maxX) maxX = pts[i].X;
                if (pts[i].Y < minY) minY = pts[i].Y;
                if (pts[i].Y > maxY) maxY = pts[i].Y;
            }
            float mid = (minX + maxX) * 0.5f;
            using (var holdBr = new SolidBrush(hold))
                g.FillPolygon(holdBr, pts);
            var state = g.Save();
            g.SetClip(new RectangleF(mid, minY - 4f, Math.Max(1f, maxX - mid) + 8f, Math.Max(1f, maxY - minY) + 12f), CombineMode.Intersect);
            using (var otherBr = new SolidBrush(other))
                g.FillPolygon(otherBr, pts);
            g.Restore(state);
        }

        private void DrawUnitCounter(Graphics g, PointF center, string text, Color bgCol, Font f, float uiScale)
        {
            var sz = g.MeasureString(text, f);
            float w = sz.Width + 6 * uiScale;
            float h = sz.Height + 1.5f * uiScale;
            var rect = new RectangleF(center.X - w / 2, center.Y, w, h);
            var shRect = new RectangleF(rect.X + 1.2f, rect.Y + 1.4f, w, h);
            using var shPath = WarGdi.CreateRoundedPath(shRect, 3.2f);
            using var shBr = new SolidBrush(Color.FromArgb(125, 10, 9, 7));
            g.FillPath(shBr, shPath);

            using var path = WarGdi.CreateRoundedPath(rect, 3.2f);
            using var br = new LinearGradientBrush(rect, WarPalette.AdjustBrightness(bgCol, 10), WarPalette.AdjustBrightness(bgCol, -8), LinearGradientMode.Vertical);
            using var pen = new Pen(Color.FromArgb(185, 214, 198, 162), 0.95f);
            using var txtBr = new SolidBrush(TextMain);
            g.FillPath(br, path);
            g.DrawPath(pen, path);
            g.DrawString(text, f, txtBr, rect.X + 3f * uiScale, rect.Y + 0.6f * uiScale);
        }

        private void DrawNodeTooltip(Graphics g, NodeDef n, Dictionary<string, (string kind, string oddsLabel, string tier)> reachable)
        {
            Faction ctrl = State.Control[n.Id];
            string ctrlStr = ctrl == Faction.ROC ? "中華民國" : ctrl == Faction.USSR ? "蘇聯/俄羅斯占領軍" : "中共解放軍";
            var (estG, exact) = SimEngine.EstGarrison(State, n.Id);
            double rocS = SimEngine.LandStr(SimEngine.UnitsAt(State, n.Id, UnitDomain.Land));

            string moveHint = "";
            if (SelectedNode != null && SelectedNode != n.Id && _checkedUnitIds.Count > 0)
            {
                var firstU = SimEngine.UnitById(State, _checkedUnitIds.First());
                if (firstU != null)
                {
                    var chk = SimEngine.CheckMoveDetailed(State, firstU, n.Id);
                    if (chk.ok)
                    {
                        bool friendlyMove = !SimEngine.IsHostileTarget(State, n.Id);
                        string actPrefix = "★ 左鍵拖曳／右鍵點擊";
                        if (friendlyMove)
                        {
                            string via = chk.kind == "sea" ? "⚓跨海調防" : $"▶陸路調防({(chk.path?.Count ?? 2) - 1}段)";
                            moveHint = $"{actPrefix}派遣 {_checkedUnitIds.Count} 師至【{n.Name}】（{via} · 約 {chk.totalDays} 天抵達 · 免佔運力）";
                        }
                        else if (chk.needsPrep)
                        {
                            double seaPct = string.IsNullOrEmpty(chk.zone) ? 100.0 : SimEngine.SeaSup(State, chk.zone) * 100.0;
                            string oddsStr = reachable.TryGetValue(n.Id, out var rInfo) ? $" · 預估：{rInfo.oddsLabel}" : "";
                            string portHint = n.Port ? " · ⚓奪港要衝" : "";
                            int sDelta = SimEngine.GetSeasonPrepDelta(State.Month, chk.zone);
                            string seasonHint = sDelta != 0 ? $"({SimEngine.GetSeasonLabel(State.Month)}{(sDelta > 0 ? "+" : "")}{sDelta}天)" : "";
                            moveHint = $"{actPrefix}建立兩棲登陸（籌備 {chk.prepDays} 天{seasonHint}＋航渡 {chk.totalDays} 天 · 制海 {seaPct:F0}%{(seaPct >= 50.0 ? "✓" : "<50%待制海")}{portHint}{oddsStr}）";
                        }
                        else
                        {
                            string kindTxt = chk.kind == "air" ? "▲空降突擊" : "▶陸路進攻";
                            string oddsStr = reachable.TryGetValue(n.Id, out var rInfo) ? $" · 預估：{rInfo.oddsLabel}" : "";
                            moveHint = $"{actPrefix}進攻（{kindTxt} · 約 {chk.totalDays} 天抵達交戰{oddsStr}）";
                        }
                    }
                    else
                    {
                        moveHint = $"⚠ 無法進軍：{chk.reason}";
                    }
                }
            }

            string ugStr = (n.Kind == NodeKind.Mainland && ctrl == Faction.PRC && State.UndergroundNet != null)
                ? (SimEngine.HasIntel(State, n.Id)
                    ? $"  |  地下網：Lv.{State.UndergroundNet.GetValueOrDefault(n.Id, 0)}"
                    : "  |  地下網：Lv.?")
                : "";
            double nAirSup = SimEngine.NodeAirSup(State, n.Id);
            string airSupTag = nAirSup >= 0.55 ? "(✔空中偵察)" : "(☁迷霧)";
            string text = $"【{n.Name}（{n.City}）】 — {ctrlStr}\n" +
                          $"國軍：{rocS:F1} 師  |  敵軍：{(exact ? $"{estG:F1} 師" : $"~{estG:F1} 師[霧]")}  |  工業：{n.Industry}  |  制空：{nAirSup * 100:F0}% {airSupTag}{ugStr}" +
                          (string.IsNullOrEmpty(moveHint) ? "" : "\n" + moveHint);

            using var f = new Font("Microsoft JhengHei UI", 8.8f);
            var sz = g.MeasureString(text, f);
            float tx = Math.Min(_mapPanel.Width - sz.Width - 20, _mousePos.X + 16);
            float ty = Math.Max(10, _mousePos.Y - sz.Height - 12);
            var rect = new RectangleF(tx, ty, sz.Width + 18, sz.Height + 12);

            WarGdi.DrawPlaqueFrame(g, rect, 6f, Color.FromArgb(244, 30, 27, 22), GoldCol, BorderCol);
            using var fg = new SolidBrush(TextMain);
            g.DrawString(text, f, fg, rect.X + 9, rect.Y + 6);
        }

        private (string nodeId, string zoneId) HitTestMap(Point sp)
        {
            var vp = GetMapViewport();
            float uiScale = GetMapUiScale();

            foreach (var z in GameData.Zones.Values)
            {
                var boxPt = MapToScreen(new PointF((float)z.MapX, (float)z.MapY), vp);
                var rect = new RectangleF(boxPt.X - 55 * uiScale, boxPt.Y - 22 * uiScale, 110 * uiScale, 44 * uiScale);
                if (rect.Contains(sp)) return (null, z.Id);
            }

            string bestNode = null;
            double bestDist = 25.0 * uiScale;
            foreach (var n in GameData.Nodes.Values)
            {
                var dispPt = GetNodeDisplayPoint(n, vp);
                double d1 = Math.Sqrt((sp.X - dispPt.X) * (sp.X - dispPt.X) + (sp.Y - dispPt.Y) * (sp.Y - dispPt.Y));
                if (d1 < bestDist) { bestDist = d1; bestNode = n.Id; }

                var truePt = MapToScreen(GameData.Project(n.Lon, n.Lat), vp);
                double d2 = Math.Sqrt((sp.X - truePt.X) * (sp.X - truePt.X) + (sp.Y - truePt.Y) * (sp.Y - truePt.Y));
                if (d2 < bestDist) { bestDist = d2; bestNode = n.Id; }
            }
            if (bestNode != null) return (bestNode, null);

            foreach (var kv in ChinaMapGeo.NodeOuterRings)
            {
                string id = kv.Key;
                foreach (var ring in kv.Value)
                {
                    if (ring.Length < 3) continue;
                    var pts = ring.Select(p => MapToScreen(GameData.Project(p.X, p.Y), vp)).ToArray();
                    using var path = new GraphicsPath();
                    path.AddPolygon(pts);
                    if (path.IsVisible(sp)) return (id, null);
                }
            }

            return (null, null);
        }

        private void MapPanel_MouseMove(object sender, MouseEventArgs e)
        {
            _mousePos = e.Location;
            if (_hoverHudKey != null)
            {
                _hoverHudKey = null;
                _hudTooltipCard.Visible = false;
                _hudPanel.Invalidate();
            }

            if (_mapDragCandidate && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle))
            {
                int dx = e.X - _mapDragStartPt.X;
                int dy = e.Y - _mapDragStartPt.Y;
                if (_leftDragSource != null && !_leftDragCommand && (Math.Abs(dx) >= 7 || Math.Abs(dy) >= 7))
                    _leftDragCommand = true;
                if (_leftDragCommand)
                {
                    var (targetId, _) = HitTestMap(e.Location);
                    if (targetId != _hoverNode) _hoverNode = targetId;
                    _mapPanel.Cursor = Cursors.Cross;
                    _mapPanel.Invalidate();
                    return;
                }
                if (!_mapDidDrag && (Math.Abs(dx) >= 6 || Math.Abs(dy) >= 6) && _mapZoom > 1.001f)
                    _mapDidDrag = true;
                if (_mapDidDrag)
                {
                    _mapPanX = _mapDragStartPanX + dx;
                    _mapPanY = _mapDragStartPanY + dy;
                    ClampMapPan();
                    LayoutFloatingMapPanels();
                    _mapPanel.Cursor = Cursors.SizeAll;
                    _mapPanel.Invalidate();
                    return;
                }
            }

            if (e.Button == MouseButtons.Right && _rightDragSource != null)
            {
                int rdx = e.X - _rightDragStart.X;
                int rdy = e.Y - _rightDragStart.Y;
                if (!_rightDragging && (rdx * rdx + rdy * rdy >= 100))
                    _rightDragging = true;
                if (_rightDragging)
                {
                    var (hovId, _) = HitTestMap(e.Location);
                    if (hovId != _hoverNode) _hoverNode = hovId;
                    _mapPanel.Cursor = Cursors.Cross;
                    _mapPanel.Invalidate();
                    return;
                }
            }

            if (_orderBadgeHits.Any(h => h.rect.Contains(e.Location)))
            {
                _mapPanel.Cursor = Cursors.Hand;
                _mapPanel.Invalidate();
                return;
            }

            if (_zoomMinusRect.Contains(e.Location) || _zoomPlusRect.Contains(e.Location) || (_mapZoom > 1.01f && _zoomResetRect.Contains(e.Location)) || _autoNavAirRect.Contains(e.Location) || _autoLandRect.Contains(e.Location))
            {
                _mapPanel.Cursor = Cursors.Hand;
                if (_hoverNode != null || _hoverZone != null)
                {
                    _hoverNode = null;
                    _hoverZone = null;
                    _mapPanel.Invalidate();
                }
                return;
            }

            var (nid, zid) = HitTestMap(e.Location);
            if (nid != _hoverNode || zid != _hoverZone)
            {
                _hoverNode = nid;
                _hoverZone = zid;
                _mapPanel.Cursor = (nid != null || zid != null) ? Cursors.Hand : Cursors.Default;
                _mapPanel.Invalidate();
            }
            else if (_hoverNode != null)
            {
                _mapPanel.Invalidate();
            }
        }

        private void MapPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (State.PendingEvents.Count > 0 || State.Over != null) return;

            if (e.Button == MouseButtons.Left)
            {
                if (_autoNavAirRect.Contains(e.Location)) { SimEngine.AutoBalanceNavalAir(State); RefreshAllUi(); return; }
                if (_autoLandRect.Contains(e.Location))
                {
                    State.AutoFrontFill = !State.AutoFrontFill;
                    if (State.AutoFrontFill) SimEngine.AutoBalanceLandForces(State, quiet: false);
                    else SimEngine.Log(State, "⛨ 已暫停前線自動補防，陸軍調動全交由玩家手動指派。", "info");
                    RefreshAllUi();
                    return;
                }
                if (_zoomMinusRect.Contains(e.Location)) { ZoomMapAt(-120); return; }
                if (_zoomPlusRect.Contains(e.Location)) { ZoomMapAt(+120); return; }
                if (_zoomResetRect.Contains(e.Location) && _mapZoom > 1.01f) { ResetMapZoom(); return; }
            }

            if (e.Button == MouseButtons.Right)
            {
                var (rnid, _) = HitTestMap(e.Location);
                _rightDragSource = rnid;
                _rightDragStart = e.Location;
                _rightDragging = false;
                return;
            }

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                _leftDragSource = null;
                _leftDragCommand = false;
                if (e.Button == MouseButtons.Left)
                {
                    var (sourceId, _) = HitTestMap(e.Location);
                    if (sourceId != null && SimEngine.UnitsAt(State, sourceId, UnitDomain.Land).Any(u => !u.Moved))
                        _leftDragSource = sourceId;
                }
                _mapDragCandidate = true;
                _mapDidDrag = false;
                _mapDragStartPt = e.Location;
                _mapDragStartPanX = _mapPanX;
                _mapDragStartPanY = _mapPanY;
            }
        }

        private void MapPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                string dragSrc = _rightDragSource;
                bool wasRightDrag = _rightDragging;
                _rightDragSource = null;
                _rightDragging = false;
                _mapPanel.Cursor = Cursors.Default;
                var (rnid, _) = HitTestMap(e.Location);

                if (wasRightDrag && dragSrc != null)
                {
                    if (rnid != null && rnid != dragSrc)
                    {
                        if (SelectedNode != dragSrc || _checkedUnitIds.Count == 0)
                            SelectNode(dragSrc);
                        DispatchCheckedUnitsTo(rnid);
                    }
                    else
                    {
                        _mapPanel.Invalidate();
                    }
                    return;
                }

                var hitBadge = _orderBadgeHits.FirstOrDefault(h => h.rect.Contains(e.Location));
                if (hitBadge.unitIds != null && hitBadge.unitIds.Count > 0)
                {
                    foreach (int uid in hitBadge.unitIds) SimEngine.CancelOrder(State, uid);
                    SimEngine.Log(State, "已撤回選中之部隊行軍／登陸籌備命令。", "info");
                    RefreshAllUi();
                    return;
                }

                if (rnid != null && SelectedNode != null && SelectedNode != rnid && _checkedUnitIds.Count > 0)
                    DispatchCheckedUnitsTo(rnid);
                return;
            }

            bool wasLeft = e.Button == MouseButtons.Left && _mapDragCandidate;
            bool wasDrag = _mapDidDrag;
            bool wasCommandDrag = _leftDragCommand;
            string commandSource = _leftDragSource;
            var (leftTarget, _) = e.Button == MouseButtons.Left ? HitTestMap(e.Location) : (null, null);
            _mapDragCandidate = false;
            _mapDidDrag = false;
            _leftDragSource = null;
            _leftDragCommand = false;
            _mapPanel.Cursor = Cursors.Default;

            if (!wasLeft) return;
            if (State.PendingEvents.Count > 0 || State.Over != null) return;

            if (wasCommandDrag)
            {
                if (commandSource != null && leftTarget != null && leftTarget != commandSource)
                {
                    if (SelectedNode != commandSource) SelectNode(commandSource);
                    DispatchCheckedUnitsTo(leftTarget);
                }
                else
                {
                    _mapPanel.Invalidate();
                }
                return;
            }
            if (wasDrag) return;

            var (nid, zid) = HitTestMap(e.Location);
            if (nid != null)
            {
                SelectNode(nid);
            }
            else if (zid != null)
            {
                SelectZone(zid);
            }
            else if (_activeDrawer != "none")
            {
                CloseDrawer();
            }
        }

        public void SelectNode(string nid)
        {
            SelectedNode = nid;
            SelectedZone = null;
            _checkedUnitIds.Clear();
            var avail = SimEngine.UnitsAt(State, nid, UnitDomain.Land).Where(u => !u.Moved).ToList();
            foreach (var u in avail) _checkedUnitIds.Add(u.Id);
            RefreshAllUi();
        }

        public void SelectZone(string zid)
        {
            SelectedZone = zid;
            SelectedNode = null;
            _checkedUnitIds.Clear();
            RefreshAllUi();
        }

        public void SelectUnitsCount(int take)
        {
            if (SelectedNode == null) return;
            _checkedUnitIds.Clear();
            var avail = SimEngine.UnitsAt(State, SelectedNode, UnitDomain.Land)
                .Where(u => !u.Moved)
                .OrderByDescending(u => u.Str)
                .ToList();
            foreach (var u in avail.Take(Math.Max(1, take))) _checkedUnitIds.Add(u.Id);
            RefreshFloatingCards();
            _mapPanel.Invalidate();
        }

        private void SelectUnitsFraction(double frac)
        {
            if (SelectedNode == null) return;
            _checkedUnitIds.Clear();
            var avail = SimEngine.UnitsAt(State, SelectedNode, UnitDomain.Land).Where(u => !u.Moved).ToList();
            int take = frac >= 0.99 ? avail.Count : Math.Max(1, (int)Math.Ceiling(avail.Count * frac));
            foreach (var u in avail.Take(take)) _checkedUnitIds.Add(u.Id);
            RefreshFloatingCards();
            _mapPanel.Invalidate();
        }

        private void CancelSelectedNodeOrders()
        {
            if (SelectedNode == null) return;
            foreach (var u in SimEngine.UnitsAt(State, SelectedNode, UnitDomain.Land))
                SimEngine.CancelOrder(State, u.Id);
            SelectNode(SelectedNode);
        }

        private void DispatchCheckedUnitsTo(string dest)
        {
            if (_checkedUnitIds.Count == 0) return;
            if (SimEngine.WouldBreakTreaty(State, dest) && !State.HasFlag("treatyWarned"))
            {
                var dr = MessageBox.Show(
                    "進攻大陸將違反《中美共同防禦條約》換文規定（未獲美方事前默許）！\n強行出兵將導致美方中止援助、撤回協防且對美關係 -30。\n\n是否仍要下令強行反攻？",
                    "統帥部條約警告",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes) return;
                State.Flags["treatyWarned"] = 1;
            }

            var result = SimEngine.IsHostileTarget(State, dest)
                ? SimEngine.AttackNow(State, _checkedUnitIds.ToList(), dest)
                : SimEngine.MoveNow(State, _checkedUnitIds.ToList(), dest);
            if (result.moved == 0 && !string.IsNullOrEmpty(result.error))
                MessageBox.Show(result.error, "無法移動部隊", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _checkedUnitIds.Clear();
            SelectedNode = dest;
            SelectedZone = null;
            CheckAndRecordCheckpoints();
            RefreshAllUi();
        }

        private void AdjustSelectedZoneUnit(string unitType, bool pullIn)
        {
            if (SelectedZone == null) return;
            SimEngine.AdjustZoneUnit(State, SelectedZone, unitType, pullIn);
            RefreshAllUi();
        }

        // ==================== 懸浮式左下指戰卡與海域直調卡刷新 ====================
        private void RefreshFloatingCards()
        {
            if (_showTitle || State.Over != null || State.PendingEvents.Count > 0)
            {
                _nodeCard.Visible = false;
                _zoneCard.Visible = false;
                return;
            }
            if (SelectedZone != null && GameData.Zones.TryGetValue(SelectedZone, out var z))
            {
                _nodeCard.Visible = false;
                _zoneCard.Visible = true;

                double rs = SimEngine.RocSea(State, z.Id), ps = SimEngine.PrcSea(State, z.Id);
                double ra = SimEngine.RocAir(State, z.Id), pa = SimEngine.PrcAir(State, z.Id);
                int fIn = SimEngine.UnitsAt(State, z.Id, UnitDomain.Sea).Count;
                int aIn = SimEngine.UnitsAt(State, z.Id, UnitDomain.Air).Count;
                int fReady = State.Units.Count(u => u.Type == "fleet" && !u.Moved && u.Loc != z.Id);
                int aReady = State.Units.Count(u => u.Type == "air" && !u.Moved && u.Loc != z.Id);
                bool canFOut = SimEngine.FindNavalAirCandidate(State, z.Id, "fleet", false) != null;
                bool canAOut = SimEngine.FindNavalAirCandidate(State, z.Id, "air", false) != null;

                string usZoneNote = (State.UsProtect && (z.Id == "Z2" || z.Id == "Z3" || State.UsConsent > 0)) ? " [🇺🇸 美軍第七艦隊巡弋]" : "";
                _zoneTitleLbl.Text = $"⚓ {z.Name}戰區・海空直調{usZoneNote}";
                _zoneStatsLbl.Text = $"制海 {SimEngine.SeaSup(State, z.Id) * 100:F0}% (國{rs:F0}/共{ps:F0})  ·  制空 {SimEngine.AirSup(State, z.Id) * 100:F0}% (國{ra:F0}/共{pa:F0})\n點右側 [＋][－] 微調，或按地圖右下「⚓ 一鍵海空佈防」自動平衡";
                _zoneFleetLbl.Text = $"⚓ 駐防艦隊：{fIn} 艘 (可用預備 {fReady})";
                _zoneAirLbl.Text = $"▲ 航空聯隊：{aIn} 隊 (可用預備 {aReady})";

                _btnFleetAdd.Enabled = fReady > 0;
                _btnFleetSub.Enabled = canFOut;
                _btnAirAdd.Enabled = aReady > 0;
                _btnAirSub.Enabled = canAOut;

                LayoutFloatingMapPanels();
                _zoneCard.BringToFront();
                return;
            }

            _zoneCard.Visible = false;

            if (SelectedNode != null && GameData.Nodes.TryGetValue(SelectedNode, out var n))
            {
                _nodeCard.Visible = true;
                Faction ctrl = State.Control[n.Id];
                string ctrlName = ctrl == Faction.ROC ? "中華民國" : ctrl == Faction.USSR ? "蘇聯/俄羅斯占領" : "中共解放軍";
                var (estG, exact) = SimEngine.EstGarrison(State, n.Id);
                var landUnits = SimEngine.UnitsAt(State, n.Id, UnitDomain.Land);
                double rocStr = SimEngine.LandStr(landUnits);
                int readyCount = landUnits.Count(u => !u.Moved);
                int orderedCount = landUnits.Count(u => State.Orders.Any(o => o.UnitId == u.Id));
                int fortLevel = SimEngine.FortificationLevel(State, n.Id);
                string fortStatus = fortLevel >= 3 ? "3/3(堅固)" : $"{fortLevel}/3(中樞自動加固中)";

                bool canWorkLocally = n.Kind == NodeKind.Mainland && ctrl == Faction.ROC;
                double recruitCost = 0.08 * SimEngine.PriceIndex(State);

                _nodeTitleLbl.Text = $"◆ {n.Name}（{n.City}）· {ctrlName}";
                bool isPriority = State.PriorityTargets.Contains(n.Id);
                _btnNodePriority.Text = isPriority ? "★ 標靶" : "☆ 標靶";
                SetWarButtonStyle(_btnNodePriority, isPriority ? Color.FromArgb(72, 58, 22) : BgCard, isPriority ? GoldCol : TextSub, isPriority ? GoldCol : BorderCol);

                int incomingCount = State.Orders.Count(o => o.To == n.Id);
                string incomingTxt = incomingCount > 0 ? $" · 在途{incomingCount}" : "";
                double cAirSup = SimEngine.NodeAirSup(State, n.Id);
                bool hasAirIntel = cAirSup >= 0.55 && State.Units.Any(u => u.Type == "air" && u.Str >= 25.0);
                string cAirTag = hasAirIntel ? "[空優透視]" : "";
                string islandWarn = n.Id == "HI" && ctrl == Faction.ROC
                    ? (State.HasFlag("hiPortBlocked") ? " · 瓊海封鎖·需搶灘登島" : (State.HasFlag("hiQiongya") ? " · 瓊崖內患" : ""))
                    : "";

                _nodeStatsLbl.Text =
                    $"部署 {rocStr:F1} 師（{landUnits.Count} 支）· 待命 {readyCount} · 已派令 {orderedCount}{incomingTxt}\n" +
                    $"敵軍 {(exact ? $"{estG:F1} 師" : $"~{estG:F1} 師[霧]")}  |  工業 {n.Industry}  |  {(n.Port ? "港口⚓" : "內陸")}  |  制空 {cAirSup * 100:F0}% {cAirTag}  |  陣地工事 {fortStatus}{islandWarn}";
                bool isEnemy = ctrl != Faction.ROC;
                if (isEnemy)
                {
                    _btnLocalRecruit.Visible = false;
                    _btnLocalBuildInf.Visible = false;
                    _btnAirStrike.Visible = true;

                    bool hasNukeTech = State.Techs.Contains("nuke_weapon") || State.Techs.Contains("tactical_nuke") || State.NukeStockpile > 0;
                    if (hasNukeTech)
                    {
                        _btnAirStrike.Location = new Point(14, 183);
                        _btnAirStrike.Size = new Size(205, 28);
                        _btnNukeStrike.Visible = true;
                        _btnNukeStrike.Location = new Point(227, 183);
                        _btnNukeStrike.Size = new Size(205, 28);
                        if (State.NukeStockpile > 0)
                        {
                            _btnNukeStrike.Text = $"☢ 戰術核打擊 ({State.NukeStockpile}枚)";
                            _btnNukeStrike.Enabled = true;
                            SetWarButtonStyle(_btnNukeStrike, Color.FromArgb(78, 28, 28), GoldCol, BadRed);
                        }
                        else
                        {
                            _btnNukeStrike.Text = "☢ 戰術核打擊 (0枚)";
                            _btnNukeStrike.Enabled = false;
                            SetWarButtonStyle(_btnNukeStrike, BgCard, TextSub, BorderCol);
                        }
                    }
                    else
                    {
                        _btnNukeStrike.Visible = false;
                        _btnAirStrike.Location = new Point(14, 183);
                        _btnAirStrike.Size = new Size(418, 28);
                    }

                    var canStrike = SimEngine.CanAirStrike(State, n.Id);
                    int cd = State.AirStrikeCooldowns.GetValueOrDefault(n.Id, 0);
                    double cost = Math.Round(0.06 * SimEngine.PriceIndex(State), 2);
                    if (cd > 0)
                    {
                        _btnAirStrike.Text = $"✈ 空襲冷卻({cd}天)";
                        _btnAirStrike.Enabled = false;
                        SetWarButtonStyle(_btnAirStrike, BgCard, TextSub, BorderCol);
                    }
                    else if (!canStrike.ok)
                    {
                        _btnAirStrike.Text = hasNukeTech ? $"✈ 空襲（{canStrike.reason}）" : $"✈ 戰術空襲（{canStrike.reason}）";
                        _btnAirStrike.Enabled = false;
                        SetWarButtonStyle(_btnAirStrike, BgCard, TextSub, BorderCol);
                    }
                    else
                    {
                        _btnAirStrike.Text = hasNukeTech ? $"✈ 戰術空襲 (${cost:F2}億)" : $"✈ 發動戰術空襲（花費 ${cost:F2} 億 · 削弱守軍）";
                        _btnAirStrike.Enabled = true;
                        SetWarButtonStyle(_btnAirStrike, Color.FromArgb(42, 68, 92), GoldCol, GoldCol);
                    }
                }
                else
                {
                    _btnNukeStrike.Visible = false;
                    _btnAirStrike.Visible = false;
                    _btnLocalRecruit.Visible = true;
                    int recruited = State.MilitiaRecruited.GetValueOrDefault(n.Id, 0);
                    int maxMil = SimEngine.MaxMilitiaForProvince(State, n.Id);
                    string autoRecTxt = State.AutoRecruitMilitia ? " [自動]" : "";
                    _btnLocalRecruit.Text = $"接納民兵·併入國軍 {recruited}/{maxMil}{autoRecTxt}";
                    _btnLocalRecruit.Enabled = canWorkLocally && recruited < maxMil && State.Manpower >= 0.5 && State.Treasury >= recruitCost;

                    bool showLocalBuild = SimEngine.IsBuildBase(State, n.Id);
                    _btnLocalBuildInf.Visible = showLocalBuild;
                    if (showLocalBuild)
                    {
                        _btnLocalRecruit.Location = new Point(14, 183);
                        _btnLocalRecruit.Size = new Size(205, 28);
                        _btnLocalBuildInf.Location = new Point(227, 183);
                        _btnLocalBuildInf.Size = new Size(205, 28);

                        var canInf = SimEngine.CanBuild(State, "inf", n.Id);
                        string autoInfTxt = State.AutoBuildInfantry ? " [自動]" : "";
                        double infCost = SimEngine.UnitCost(State, "inf");
                        double px = SimEngine.PriceIndex(State);
                        double bounty = State.Manpower < 1.0 ? 0.10 * px * (1.0 - Math.Max(0, State.Manpower)) : 0.0;
                        if (canInf.ok)
                        {
                            if (bounty > 0.01)
                            {
                                _btnLocalBuildInf.Text = $"撥款編成步兵 ${(infCost + bounty):F2}億{autoInfTxt}";
                                SetWarButtonStyle(_btnLocalBuildInf, Color.FromArgb(62, 54, 24), GoldCol, GoldCol);
                            }
                            else
                            {
                                _btnLocalBuildInf.Text = $"基地編成步兵 ${infCost:F2}億{autoInfTxt}";
                                SetWarButtonStyle(_btnLocalBuildInf, Color.FromArgb(46, 78, 56), TextMain, GoodGreen);
                            }
                            _btnLocalBuildInf.Enabled = true;
                        }
                        else
                        {
                            _btnLocalBuildInf.Text = $"編成步兵（{canInf.reason}）";
                            SetWarButtonStyle(_btnLocalBuildInf, BgCard, TextSub, BorderCol);
                            _btnLocalBuildInf.Enabled = false;
                        }
                    }
                    else
                    {
                        _btnLocalRecruit.Location = new Point(14, 183);
                        _btnLocalRecruit.Size = new Size(418, 28);
                    }
                }

                var availUnits = landUnits.Where(u => !u.Moved).ToList();
                int selectedCount = availUnits.Count(u => _checkedUnitIds.Contains(u.Id));
                double needLift = _checkedUnitIds
                    .Select(id => SimEngine.UnitById(State, id))
                    .Where(u => u != null && !u.Moved)
                    .Sum(u => GameData.UnitTypes[u.Type].Lift);
                double remLift = Math.Max(0, SimEngine.LiftCapacity(State) - SimEngine.LiftUsed(State));
                _nodeSelCountLbl.ForeColor = needLift > remLift + 0.01 ? Color.FromArgb(236, 174, 82) : GoldCol;
                _nodeSelCountLbl.Text = availUnits.Count > 0
                    ? $"選 {selectedCount}/{availUnits.Count} · 攻陸運力 {needLift:F1}/{remLift:F1}"
                    : "無待命部隊";

                BuildVisualUnitChips(landUnits);

                _nodeOrderHintLbl.Text = selectedCount > 0
                        ? "拖曳／右鍵目標：陸路短程行軍／跨海兩棲籌備（需≥50%制海與奪港）"
                        : availUnits.Count > 0
                            ? "請先點 1師 / 3師 / 全選 挑選要派出的部隊"
                            : (landUnits.Any(u => State.Orders.Any(o => o.UnitId == u.Id))
                                ? "點選上方在途／籌備籌碼或地圖小圖示右鍵可撤回命令"
                                : "本區暫無可用於進軍之待命地面部隊");

                LayoutFloatingMapPanels();
                _nodeCard.BringToFront();
            }
            else
            {
                _nodeCard.Visible = false;
            }
        }

        public void DispatchCheckedUnitsForTest(string dest) => DispatchCheckedUnitsTo(dest);

        private void BuildVisualUnitChips(List<Unit> landUnits)
        {
            _nodeChipsPanel.SuspendLayout();
            _nodeChipsPanel.Controls.Clear();
            int vpW = _nodeChipsWrap?.ViewportWidth ?? 376;
            _nodeChipsPanel.MaximumSize = new Size(vpW, 0);
            _nodeChipsPanel.MinimumSize = new Size(vpW, 20);

            if (landUnits.Count == 0)
            {
                _nodeChipsPanel.Controls.Add(new Label
                {
                    Text = "目前本戰區無國軍地面駐軍",
                    ForeColor = TextSub,
                    Size = new Size(320, 26),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Microsoft JhengHei UI", 8.5f),
                    Padding = new Padding(4, 0, 0, 0)
                });
                _nodeChipsPanel.ResumeLayout(true);
                _nodeChipsWrap?.SyncScroll();
                return;
            }

            // 1. 待命地面部隊按兵種聚合成視覺化兵種籌碼卡（解決台灣 20+ 步兵師清單冗長問題）
            var availByType = landUnits.Where(u => !u.Moved).GroupBy(u => u.Type);
            foreach (var grp in availByType)
            {
                var ut = GameData.UnitTypes[grp.Key];
                var units = grp.OrderByDescending(u => u.Str).ToList();
                int selInGrp = units.Count(u => _checkedUnitIds.Contains(u.Id));
                bool anySel = selInGrp > 0;
                double avgStr = units.Average(u => u.Str);

                var chip = new BufferedPanel
                {
                    Size = new Size(180, 32),
                    BackColor = anySel ? Color.FromArgb(46, 78, 106) : BgCard,
                    Margin = new Padding(2),
                    Cursor = Cursors.Hand
                };
                chip.Paint += (_, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    var r = new RectangleF(0.5f, 0.5f, chip.Width - 2f, chip.Height - 2f);
                    using var path = WarGdi.CreateRoundedPath(r, 4.5f);
                    using var p = new Pen(anySel ? GoldCol : BorderCol, anySel ? 1.4f : 1f);
                    pe.Graphics.DrawPath(p, path);
                };

                string title = units.Count == 1
                    ? $"{ut.Short} (戰{avgStr:F0}%){(anySel ? " ✓" : "")}"
                    : $"{ut.Short} ×{units.Count} (選{selInGrp})";
                var lbl = new Label
                {
                    Text = title,
                    ForeColor = anySel ? TextMain : TextSub,
                    Font = new Font("Microsoft JhengHei UI", 8.3f, FontStyle.Bold),
                    Location = new Point(8, 7),
                    Size = new Size(164, 18),
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                void ToggleGroupSelection(object _, EventArgs __)
                {
                    if (selInGrp > 0)
                    {
                        foreach (var u in units) _checkedUnitIds.Remove(u.Id);
                    }
                    else
                    {
                        foreach (var u in units) _checkedUnitIds.Add(u.Id);
                    }
                    RefreshFloatingCards();
                    _mapPanel.Invalidate();
                }

                chip.Click += ToggleGroupSelection;
                lbl.Click += ToggleGroupSelection;

                chip.Controls.Add(lbl);
                _nodeChipsPanel.Controls.Add(chip);
            }

            // 2. 已下達進軍命令之籌碼卡（點擊即撤回該批次命令）
            var ordGroups = landUnits
                .Select(u => new { u, ord = State.Orders.FirstOrDefault(o => o.UnitId == u.Id) })
                .Where(x => x.ord != null)
                .GroupBy(x => x.ord.To);

            foreach (var og in ordGroups)
            {
                string destName = GameData.Nodes[og.Key].Name;
                var firstOrd = og.First().ord;
                var uids = og.Select(x => x.u.Id).ToList();
                string ordTag = firstOrd.IsPrep
                    ? $"⚓ 籌備→{destName} ({uids.Count}支 {firstOrd.PrepDays}/{firstOrd.PrepRequired}天) [撤]"
                    : $"▶ 行軍→{destName} ({uids.Count}支 剩{firstOrd.RemainingDays}天) [撤]";
                var btnOrd = CreateWarButton(ordTag, firstOrd.IsPrep ? Color.FromArgb(30, 56, 66) : Color.FromArgb(62, 54, 24), GoldCol, 206, 30, GoldDim);
                btnOrd.Font = new Font("Microsoft JhengHei UI", 7.6f, FontStyle.Bold);
                btnOrd.Click += (_, _) =>
                {
                    foreach (int uid in uids) SimEngine.CancelOrder(State, uid);
                    SelectNode(SelectedNode);
                };
                _nodeChipsPanel.Controls.Add(btnOrd);
            }

            // 3. 新編或剛行動之整訓部隊摘要
            int rested = landUnits.Count(u => u.Moved && State.Orders.All(o => o.UnitId != u.Id));
            if (rested > 0)
            {
                var lblRest = new Label
                {
                    Text = $"◆ 本回合整訓/已調動：{rested} 支（下回合恢復待命）",
                    ForeColor = TextSub,
                    Font = new Font("Microsoft JhengHei UI", 7.8f),
                    Size = new Size(340, 20),
                    Padding = new Padding(4, 2, 0, 0)
                };
                _nodeChipsPanel.Controls.Add(lblRest);
            }

            _nodeChipsPanel.ResumeLayout(true);
            _nodeChipsWrap?.SyncScroll();
        }

        // ==================== 隨選右側戰情抽屜內容渲染 ====================
        private void RefreshDrawerContent()
        {
            if (!_drawerPanel.Visible || _activeDrawer == "none") return;

            _drawerScrollWrap.Visible = true;
            _drawerScrollBody.SuspendLayout();
            _drawerScrollBody.Controls.Clear();

            int cw = Math.Max(340, _drawerScrollWrap.ViewportWidth - 10);
            _drawerScrollBody.Width = _drawerScrollWrap.ViewportWidth;

            if (_activeDrawer == "build") RenderBuildDrawer(cw);

            int maxBottom = 0;
            foreach (Control c in _drawerScrollBody.Controls)
                if (c.Bottom > maxBottom) maxBottom = c.Bottom;
            _drawerScrollBody.Height = Math.Max(_drawerScrollWrap.ViewportHeight, maxBottom + 12);

            _drawerScrollBody.ResumeLayout();
            _drawerScrollWrap.ScrollTo(0, 0);
        }

        private void RenderBuildDrawer(int cw)
        {
            _drawerTitleLbl.Text = "⚔ 三軍整建與財政稅率";
            int y = 4;

            var b = SimEngine.Budget(State);
            double liftCap = SimEngine.LiftCapacity(State);
            double liftUsed = SimEngine.LiftUsed(State);

            var summaryCard = new Label
            {
                BackColor = BgCard,
                ForeColor = TextMain,
                Font = new Font("Microsoft JhengHei UI", 8.8f),
                Location = new Point(4, y),
                Size = new Size(cw, 74),
                Padding = new Padding(8),
                Text = $"歲入 +${b.income:F2}億  |  維持 -${b.upkeep:F2}億  |  研發債息 -${(b.research + b.interest):F2}億\n" +
                       $"⚓ 兩棲運力：{liftUsed:F1} / {liftCap:F1} 師  |  港口吞吐上限：{State.SupplyCap:F1}\n" +
                       $"◆ 經濟乘數：{State.EconMult * 100:F0}%  |  物價指數：×{SimEngine.PriceIndex(State):F1}"
            };
            _drawerScrollBody.Controls.Add(summaryCard);
            y += 80;

            var lblTax = new Label { Text = "稅率政策：", ForeColor = GoldCol, Location = new Point(4, y + 5), AutoSize = true, Font = new Font("Microsoft JhengHei UI", 9f, FontStyle.Bold) };
            _drawerScrollBody.Controls.Add(lblTax);
            string[] taxKeys = { "low", "mid", "high" };
            string[] taxNames = { "輕稅 12%", "標準 16%", "重稅 20%" };
            int tw = (cw - 92) / 3;
            for (int i = 0; i < 3; i++)
            {
                string tk = taxKeys[i];
                bool act = State.Tax == tk;
                var bt = CreateWarButton(taxNames[i], act ? RocBlue : BgCard, TextMain, tw - 4, 28, act ? GoldCol : BorderCol);
                bt.Location = new Point(88 + i * tw, y);
                bt.Click += (_, _) => { State.Tax = tk; RefreshAllUi(); };
                _drawerScrollBody.Controls.Add(bt);
            }
            y += 36;

            var lblRes = new Label { Text = $"研發預算（每回合 +{SimEngine.RpPerTurn(State):F1} 點）：", ForeColor = GoldCol, Location = new Point(4, y + 4), AutoSize = true, Font = new Font("Microsoft JhengHei UI", 9f, FontStyle.Bold) };
            _drawerScrollBody.Controls.Add(lblRes);
            y += 24;
            int rw = cw / 4;
            for (int lv = 1; lv <= 4; lv++)
            {
                int copyLv = lv;
                bool act = State.ResLevel == lv;
                var br = CreateWarButton($"等級 {lv}", act ? RocBlue : BgCard, TextMain, rw - 6, 28, act ? GoldCol : BorderCol);
                br.Location = new Point(4 + (lv - 1) * rw, y);
                br.Click += (_, _) => { State.ResLevel = copyLv; RefreshAllUi(); };
                _drawerScrollBody.Controls.Add(br);
            }
            y += 38;

            var lblAuto = new Label { Text = "◆ 戰地後勤與動員自動化：", ForeColor = GoldCol, Location = new Point(4, y), AutoSize = true, Font = new Font("Microsoft JhengHei UI", 9.4f, FontStyle.Bold) };
            _drawerScrollBody.Controls.Add(lblAuto);
            y += 24;

            int aw = (cw - 8) / 3;
            var btnAutoFort = CreateWarButton(State.AutoFortify ? "🛡 防線加固 [開]" : "🛡 防線加固 [關]", State.AutoFortify ? Color.FromArgb(46, 78, 56) : BgCard, TextMain, aw - 4, 28, State.AutoFortify ? GoodGreen : BorderCol);
            btnAutoFort.Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
            btnAutoFort.Location = new Point(4, y);
            btnAutoFort.Click += (_, _) => { State.AutoFortify = !State.AutoFortify; RefreshDrawerContent(); };
            _drawerScrollBody.Controls.Add(btnAutoFort);

            var btnAutoMil = CreateWarButton(State.AutoRecruitMilitia ? "👥 收編民兵 [開]" : "👥 收編民兵 [關]", State.AutoRecruitMilitia ? Color.FromArgb(46, 78, 56) : BgCard, TextMain, aw - 4, 28, State.AutoRecruitMilitia ? GoodGreen : BorderCol);
            btnAutoMil.Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
            btnAutoMil.Location = new Point(4 + aw, y);
            btnAutoMil.Click += (_, _) => { State.AutoRecruitMilitia = !State.AutoRecruitMilitia; RefreshDrawerContent(); };
            _drawerScrollBody.Controls.Add(btnAutoMil);

            var btnAutoInf = CreateWarButton(State.AutoBuildInfantry ? "⚔ 編成步兵 [開]" : "⚔ 編成步兵 [關]", State.AutoBuildInfantry ? Color.FromArgb(46, 78, 56) : BgCard, TextMain, aw - 4, 28, State.AutoBuildInfantry ? GoodGreen : BorderCol);
            btnAutoInf.Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
            btnAutoInf.Location = new Point(4 + aw * 2, y);
            btnAutoInf.Click += (_, _) => { State.AutoBuildInfantry = !State.AutoBuildInfantry; RefreshDrawerContent(); };
            _drawerScrollBody.Controls.Add(btnAutoInf);
            y += 32;

            var autoDesc = new Label
            {
                Text = "開啟後中樞自動撥款：光復省分陣地自動加固升至3級、依民心接納義勇併入國軍；兵源充裕時於基地整編步兵。",
                ForeColor = TextSub,
                Font = new Font("Microsoft JhengHei UI", 7.8f),
                Location = new Point(4, y),
                Size = new Size(cw, 18),
                AutoEllipsis = true
            };
            _drawerScrollBody.Controls.Add(autoDesc);
            y += 24;

            // 國防兵源與招募專區（解決國庫充盈卻兵源短缺無法徵兵問題）
            var lblMan = new Label
            {
                Text = $"◆ 國防兵源整備（可用兵源：{State.Manpower:F1} 師 · 國庫充盈時可撥款立即增募）：",
                ForeColor = GoldCol,
                Font = new Font("Microsoft JhengHei UI", 9.2f, FontStyle.Bold),
                Location = new Point(4, y),
                AutoSize = true
            };
            _drawerScrollBody.Controls.Add(lblMan);
            y += 24;

            int mw = (cw - 8) / 2;
            double recruitCost = Math.Round(0.20 * SimEngine.PriceIndex(State), 2);
            var btnRecruitMan = CreateWarButton($"💰 撥款招募兵源 +3.0 (${recruitCost:F2}億)", State.Treasury >= recruitCost ? Color.FromArgb(52, 94, 62) : BgCard, TextMain, mw - 4, 30, State.Treasury >= recruitCost ? GoodGreen : BorderCol);
            btnRecruitMan.Font = new Font("Microsoft JhengHei UI", 8.2f, FontStyle.Bold);
            btnRecruitMan.Location = new Point(4, y);
            btnRecruitMan.Enabled = State.Treasury >= recruitCost;
            btnRecruitMan.Click += (_, _) =>
            {
                var res = SimEngine.RecruitManpowerWithTreasury(State);
                if (!res.ok) MessageBox.Show(res.reason, "專案募兵", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshAllUi();
            };
            _drawerScrollBody.Controls.Add(btnRecruitMan);

            var btnConscript = CreateWarButton("📢 動員召集令 +4.0 (民心-2)", State.Morale >= 15 ? Color.FromArgb(62, 54, 24) : BgCard, TextMain, mw - 4, 30, State.Morale >= 15 ? GoldCol : BorderCol);
            btnConscript.Font = new Font("Microsoft JhengHei UI", 8.2f, FontStyle.Bold);
            btnConscript.Location = new Point(4 + mw, y);
            btnConscript.Enabled = State.Morale >= 15;
            btnConscript.Click += (_, _) =>
            {
                State.Manpower += 4.0;
                State.Morale = Math.Max(5.0, State.Morale - 2.0);
                SimEngine.Log(State, "★ 國防部發布年度後備役動員召集令，迅速充實常備兵源 +4.0 師（民心 -2）！", "good");
                RefreshAllUi();
            };
            _drawerScrollBody.Controls.Add(btnConscript);
            y += 36;

            var buildBases = SimEngine.BuildBases(State);
            if (!buildBases.Contains(_buildLoc))
                _buildLoc = buildBases.FirstOrDefault() ?? "TW";
            string baseName = GameData.Nodes.TryGetValue(_buildLoc, out var bn) ? bn.Name : "台灣本島";

            var hdrUnits = new Label
            {
                Text = "◆ 三軍擴編（四大主力精簡整編）",
                ForeColor = GoldCol,
                Font = new Font("Microsoft JhengHei UI", 9.6f, FontStyle.Bold),
                Location = new Point(4, y),
                AutoSize = true
            };
            _drawerScrollBody.Controls.Add(hdrUnits);
            y += 24;

            if (buildBases.Count <= 1)
            {
                _drawerScrollBody.Controls.Add(new Label
                {
                    Text = $"地面整編地點：{baseName}（光復大陸港口省分後可直接就地生兵）",
                    ForeColor = TextSub,
                    Font = new Font("Microsoft JhengHei UI", 8.0f),
                    Location = new Point(4, y),
                    Size = new Size(cw, 20)
                });
                y += 24;
            }
            else
            {
                _drawerScrollBody.Controls.Add(new Label
                {
                    Text = "地面整編基地：",
                    ForeColor = GoldCol,
                    Font = new Font("Microsoft JhengHei UI", 8.3f, FontStyle.Bold),
                    Location = new Point(4, y + 4),
                    AutoSize = true
                });
                var baseFlow = new FlowLayoutPanel
                {
                    Location = new Point(98, y),
                    Size = new Size(cw - 98, 28),
                    AutoSize = true,
                    MaximumSize = new Size(cw - 98, 0),
                    WrapContents = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    BackColor = Color.Transparent,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                foreach (var bid in buildBases)
                {
                    string copyBid = bid;
                    bool active = bid == _buildLoc;
                    string bLabel = bid == "TW" ? "台灣" : GameData.Nodes[bid].Name;
                    var bBtn = CreateWarButton(bLabel, active ? RocBlue : BgCard, TextMain, 64, 25, active ? GoldCol : BorderCol);
                    bBtn.Font = new Font("Microsoft JhengHei UI", 8.0f, FontStyle.Bold);
                    bBtn.Margin = new Padding(0, 0, 5, 4);
                    bBtn.Click += (_, _) => { _buildLoc = copyBid; RefreshDrawerContent(); };
                    baseFlow.Controls.Add(bBtn);
                }
                _drawerScrollBody.Controls.Add(baseFlow);
                y += Math.Max(30, baseFlow.GetPreferredSize(new Size(cw - 98, 0)).Height + 6);
            }

            foreach (var ut in GameData.UnitTypes.Values.Where(u => !u.NoBuild))
            {
                var card = new Panel
                {
                    Location = new Point(4, y),
                    Size = new Size(cw, 80),
                    BackColor = BgCard
                };

                bool isLand = ut.Domain == UnitDomain.Land;
                string spawnTarget = isLand ? _buildLoc : "TW";
                int count = SimEngine.CountType(State, ut.Id);
                double cost = SimEngine.UnitCost(State, ut.Id);
                var can = SimEngine.CanBuild(State, ut.Id, spawnTarget);
                string linkedTechId = ut.Id switch
                {
                    "inf" => "us_reorg",
                    "arm" => "armor",
                    "fleet" => "lst_fleet",
                    "air" => "jets",
                    _ => null
                };
                var linkedTech = linkedTechId != null ? GameData.Techs[linkedTechId] : null;
                string techStatus = linkedTech == null ? "" : State.Techs.Contains(linkedTech.Id) ? "已完成"
                    : SimEngine.IsResearching(State, linkedTech.Id) ? "研發中"
                    : SimEngine.TechAvailable(State, linkedTech) ? "可研發" : "前置未完成";
                string techLine = linkedTech == null ? "" : $"科技：{linkedTech.Name} · {techStatus}";
                if (ut.Id == "inf") techLine += $"  |  陸軍科技 {State.ArmyTech:F1}";
                else if (ut.Id == "arm") techLine += $"  |  陸軍科技 {State.ArmyTech:F1}";
                else if (ut.Id == "fleet") techLine += $"  |  海軍科技 {State.NavyTech:F1} · 運力 {liftCap:F1} 師";
                else if (ut.Id == "air") techLine += $"  |  空軍科技 {State.AirTech:F1}";

                string roleTxt = ut.Id switch
                {
                    "inf"   => $"主力步兵（攻 {ut.Atk:F2} · 防 {ut.Def:F2}）· 於{baseName}編成",
                    "arm"   => $"精銳突擊（攻 {ut.Atk:F2} · 防 {ut.Def:F2}）· 於{baseName}編成",
                    "fleet" => $"爭奪制海權 ＋ 跨海登陸運力 +1.0 師 · 配屬台灣/海域",
                    "air"   => "爭奪制空權 ＋ 對地作戰支援 · 配屬台灣/海域",
                    _       => ""
                };

                card.Controls.Add(new Label
                {
                    Text = $"{ut.Name}（現有 {count} · ${cost:F2}億 · 兵源 {ut.Man:F1}）",
                    ForeColor = TextMain,
                    Font = new Font("Microsoft JhengHei UI", 9.1f, FontStyle.Bold),
                    Location = new Point(8, 6),
                    Size = new Size(cw - 122, 21),
                    AutoEllipsis = true
                });
                card.Controls.Add(new Label
                {
                    Text = roleTxt,
                    ForeColor = TextSub,
                    Font = new Font("Microsoft JhengHei UI", 7.9f),
                    Location = new Point(8, 29),
                    Size = new Size(cw - 122, 19),
                    AutoEllipsis = true
                });
                card.Controls.Add(new Label
                {
                    Text = techLine,
                    ForeColor = linkedTech != null && State.Techs.Contains(linkedTech.Id) ? GoodGreen : GoldCol,
                    Font = new Font("Microsoft JhengHei UI", 7.7f, FontStyle.Bold),
                    Location = new Point(8, 51),
                    Size = new Size(cw - 122, 20),
                    AutoEllipsis = true
                });

                double baseCost = SimEngine.UnitCost(State, ut.Id);
                double px = SimEngine.PriceIndex(State);
                double extraBounty = State.Manpower < ut.Man ? 0.10 * px * (ut.Man - Math.Max(0, State.Manpower)) : 0.0;
                double totalCost = baseCost + extraBounty;
                bool canAffordNormal = can.ok && extraBounty <= 0.01;
                bool canAffordBounty = can.ok && extraBounty > 0.01;

                string btnText = canAffordNormal
                    ? $"編成 (${baseCost:F2}億)"
                    : canAffordBounty
                        ? $"撥款募兵 (${totalCost:F2}億)"
                        : can.reason;

                var btnColor = canAffordNormal ? Color.FromArgb(52, 94, 62)
                             : canAffordBounty ? Color.FromArgb(62, 54, 24)
                             : BgPanel;
                var btnBorder = canAffordNormal ? GoodGreen
                              : canAffordBounty ? GoldCol
                              : BorderCol;
                var btnTextCol = can.ok ? (canAffordBounty ? GoldCol : TextMain) : TextSub;

                var btn = CreateWarButton(btnText, btnColor, btnTextCol, 126, 32, btnBorder);
                btn.Font = new Font("Microsoft JhengHei UI", 7.8f, FontStyle.Bold);
                btn.Location = new Point(cw - 134, 6);
                btn.Enabled = can.ok;
                string tid = ut.Id;
                string tLoc = spawnTarget;
                btn.Click += (_, _) => { SimEngine.BuildUnit(State, tid, tLoc); RefreshAllUi(); };
                card.Controls.Add(btn);

                if (linkedTech != null)
                {
                    string focusTechId = linkedTech.Id;
                    var techBtn = CreateWarButton("查看科技 →", BgPanel, GoldCol, 126, 25, BorderCol);
                    techBtn.Font = new Font("Microsoft JhengHei UI", 7.8f, FontStyle.Bold);
                    techBtn.Location = new Point(cw - 134, 46);
                    techBtn.Click += (_, _) => OpenTechPageAt(focusTechId);
                    card.Controls.Add(techBtn);
                }

                _drawerScrollBody.Controls.Add(card);
                y += 85;
            }
        }

        private void DrawLineChart(Graphics g, Rectangle r, string title, List<(int year, double roc, double prc)> pts)
        {
            WarGdi.DrawPlaqueFrame(g, r, 6f, BgCard, BorderCol, Color.FromArgb(54, 46, 35));
            using var fTitle = new Font("Microsoft JhengHei UI", 9.0f, FontStyle.Bold);
            using var fSmall = new Font("Consolas", 7.8f);
            g.DrawString(title, fTitle, new SolidBrush(GoldCol), r.X + 10, r.Y + 6);
            g.DrawString("■藍:中華民國  ■紅:中共", fSmall, new SolidBrush(TextSub), r.Right - 148, r.Y + 7);

            var plot = new Rectangle(r.X + 44, r.Y + 28, r.Width - 58, r.Height - 40);
            using var penPlot = new Pen(BorderCol);
            g.DrawRectangle(penPlot, plot);

            if (pts.Count == 0)
            {
                using var emptyF = new Font("Microsoft JhengHei UI", 8.4f);
                using var emptyBr = new SolidBrush(TextSub);
                using var emptyFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("尚無年度資料", emptyF, emptyBr, plot, emptyFmt);
                return;
            }

            double maxV = Math.Max(10.0, pts.Max(p => Math.Max(p.roc, p.prc)) * 1.1);
            using var gridP = new Pen(Color.FromArgb(42, 198, 180, 144), 1f) { DashStyle = DashStyle.Dot };
            for (int i = 0; i <= 4; i++)
            {
                int py = plot.Bottom - (int)(plot.Height * (i / 4.0));
                g.DrawLine(gridP, plot.Left, py, plot.Right, py);
                g.DrawString($"{maxV * (i / 4.0):F0}", fSmall, new SolidBrush(TextSub), r.X + 4, py - 6);
            }

            PointF ToPt(int idx, double val)
            {
                float x = plot.Left + (pts.Count <= 1 ? 0 : (float)idx / (pts.Count - 1) * plot.Width);
                float y = plot.Bottom - (float)(val / maxV * plot.Height);
                return new PointF(x, y);
            }

            if (pts.Count >= 2)
            {
                using var prcLine = new Pen(Color.FromArgb(228, 198, 86, 78), 2.2f);
                using var rocLine = new Pen(Color.FromArgb(236, 98, 162, 206), 2.2f);
                g.DrawLines(prcLine, pts.Select((p, i) => ToPt(i, p.prc)).ToArray());
                g.DrawLines(rocLine, pts.Select((p, i) => ToPt(i, p.roc)).ToArray());
            }
            else
            {
                var pR = ToPt(0, pts[0].roc);
                var pP = ToPt(0, pts[0].prc);
                g.FillEllipse(Brushes.DeepSkyBlue, pR.X - 3, pR.Y - 3, 6, 6);
                g.FillEllipse(Brushes.IndianRed, pP.X - 3, pP.Y - 3, 6, 6);
                using var sparseF = new Font("Microsoft JhengHei UI", 8.0f);
                using var sparseBr = new SolidBrush(TextSub);
                using var sparseFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"目前只有 {pts[0].year} 年資料\n推進時間後即可查看趨勢", sparseF, sparseBr,
                    new RectangleF(plot.X + 12, plot.Y + 10, Math.Max(10, plot.Width - 24), Math.Max(10, plot.Height - 20)), sparseFmt);
            }
        }

        // ==================== 即時時間推進與存讀檔 ====================
        public void StepClockTick()
        {
            if (_showTitle || _userPaused || IsOverlayPausingTime || State == null || State.Over != null || State.PendingEvents.Count > 0)
            {
                SyncSimTimer();
                return;
            }

            int prevMonth = State.Month;
            int prevTechCount = State.Techs.Count;
            int prevIntelCount = State.IntelReports?.Count ?? 0;
            int prevOrderCount = State.Orders.Count;
            int prevHeldCount = SimEngine.HeldMainland(State).Count;
            bool moved = SimEngine.AdvanceDay(State);
            if (moved) PulseFrontNotice();
            if (!moved)
            {
                SyncSimTimer();
                UpdateOverlays();
                RefreshAllUi();
                return;
            }

            if (State.PendingEvents.Count > 0 || State.Over != null)
            {
                SyncSimTimer();
                CheckAndRecordCheckpoints();
                UpdateOverlays();
                RefreshAllUi();
                return;
            }

            if (State.Techs.Count != prevTechCount || State.Month != prevMonth || SimEngine.HeldMainland(State).Count != prevHeldCount || prevOrderCount != State.Orders.Count)
            {
                CheckAndRecordCheckpoints();
                RefreshAllUi();
                return;
            }

            if (_techPageOpen)
            {
                RefreshTechOverlay();
                _hudPanel?.Invalidate();
            }

            if (_intelPageOpen && (State.IntelReports?.Count ?? 0) != prevIntelCount)
            {
                RefreshIntelPage();
            }

            if (State.Orders.Count > 0 || prevOrderCount > 0)
            {
                RefreshFloatingCards();
                _mapPanel.Invalidate();
            }
            _hudPanel.Invalidate();
        }

        private void SaveGame()
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(SavePath, JsonSerializer.Serialize(State, opts));
                SimEngine.Log(State, "★ 戰局進度已儲存至 savegame.json。", "good");
            }
            catch (Exception ex)
            {
                MessageBox.Show("存檔失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadGame()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    MessageBox.Show("尚無存檔紀錄（savegame.json）。", "讀檔提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var loaded = JsonSerializer.Deserialize<GameState>(File.ReadAllText(SavePath));
                if (loaded != null)
                {
                    if (loaded.Month < 1 || loaded.Month > 12) loaded.Month = 1;
                    if (loaded.Day < 1 || loaded.Day > 31) loaded.Day = 1;
                    if (loaded.IntelReports == null) loaded.IntelReports = new();
                    if (loaded.IntelReports.Count == 0) SimEngine.SampleIntelReports(loaded, isOpening: true);
                    loaded.ResearchProgMap ??= new();
                    loaded.ActiveTechs ??= new();
                    loaded.ResearchQueue ??= new();
                    SimEngine.Normalize(loaded);
                    State = loaded;
                    _selectedEasyMode = loaded.EasyMode;
                    UpdateModeButtonsUi();
            _checkedUnitIds.Clear();
                    SelectedZone = null;
                    SelectedNode = "TW";
                    foreach (var u in SimEngine.UnitsAt(State, "TW", UnitDomain.Land).Where(u => !u.Moved).OrderByDescending(u => u.Str).Take(3))
                        _checkedUnitIds.Add(u.Id);
                    _showTitle = false;
                    _userPaused = false;
                    _techPageOpen = false;
                    _intelPageOpen = false;
                    _lastIntelRenderedCount = -1;
                    _selectedTechId = null;
                    _hoverTechId = null;
                    _hoverTechBtnId = null;
                    _techFlashMsg = null;
                    _mapZoom = 1.0f;
                    _mapPanX = 0.0f;
                    _mapPanY = 0.0f;
                    if (_techOverlay != null) _techOverlay.Visible = false;
                    if (_intelPageOverlay != null) _intelPageOverlay.Visible = false;
                    UpdateOverlays();
                    RefreshAllUi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("讀檔失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RefreshAllUi()
        {
            UpdateSpeedButtonsUi();
            SyncSimTimer();
            _hudPanel.Invalidate();
            _mapPanel.Invalidate();

            if (!string.IsNullOrEmpty(State.LastTechCompleted))
            {
                _lblDockTech.ForeColor = GoodGreen;
                _lblDockTech.Text = $"★ 科技研發完成通知：【{State.LastTechCompleted}】（{State.LastTechDate}）";
            }
            else
            {
                _lblDockTech.Text = "";
            }

            RefreshFloatingCards();
            RefreshDrawerContent();
            if (_techPageOpen) RefreshTechOverlay();
            if (_intelPageOpen) RefreshIntelPage();
            RefreshDockButtons();
            UpdateOverlays();
        }
    }
}
