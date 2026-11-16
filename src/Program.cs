using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace RocGame
{
    internal static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Contains("--sim"))
            {
                try
                {
                    int rc = BatchSim.RunCli(args);
                    Environment.Exit(rc);
                    return rc;
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sim_error.txt"), ex.ToString());
                    Environment.Exit(1);
                    return 1;
                }
            }

            if (args.Contains("--test"))
            {
                try
                {
                    int rc = RunSelfTests();
                    Environment.Exit(rc);
                    return rc;
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_report.txt"), "EXCEPTION:\n" + ex);
                    Environment.Exit(1);
                    return 1;
                }
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
            return 0;
        }

        private static int RunSelfTests()
        {
            var sb = new System.Text.StringBuilder();
            void LogLine(string line)
            {
                sb.AppendLine(line);
                try { Console.WriteLine(line); } catch { }
            }

            void CaptureUiSnapshot(Form form, string fileName)
            {
                using var bmp = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName), System.Drawing.Imaging.ImageFormat.Png);
            }


            LogLine("=== 《國光：反攻大陸》單機版自動化測試開始 ===");

            void Assert(bool cond, string msg)
            {
                if (!cond)
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_report.txt"), sb.ToString() + "\n[FAIL] " + msg);
                    throw new Exception("測試失敗: " + msg);
                }
            }

            // 1. 驗證遊戲名稱、地圖節點與北疆蘇俄邊境設定
            Assert(GameData.GameTitle == "國光：反攻大陸", "遊戲名稱必須為 國光：反攻大陸");
            Assert(GameData.NodeIds.Length == 33, $"應有 33 個控制節點，實際為 {GameData.NodeIds.Length}");
            Assert(GameData.MainlandIds.Length == 26, $"應有 26 個大陸省分節點，實際為 {GameData.MainlandIds.Length}");
            Assert(GameData.BorderUssrIds.SequenceEqual(new[] { "HLJ", "JL", "LN", "IM", "XJ" }), "北疆接壤蘇俄節點應為 HLJ, JL, LN, IM, XJ");
            Assert(GameData.TechList.Count == 63, $"科技樹應有 63 項，實際為 {GameData.TechList.Count}");
            foreach (var br in new[] { "science", "arms", "intel", "subv", "logi" })
                Assert(GameData.TechList.Count(t => t.Branch == br) >= 10, $"科技樹分支 {br} 應至少包含 10 項節點");
            Assert(GameData.TechList.All(t => t.Req == null || t.Req.All(r => GameData.Techs.ContainsKey(r))), "所有科技樹前置 ID 必須有效存在");
            Assert(!GameData.TechList.Any(t => t.Name.Contains("史實") || t.Name.Contains("架空") || t.Desc.Contains("史實") || t.Desc.Contains("架空")),
                "科技樹名稱與說明不應包含『史實』或『架空』提示字樣");
            Assert(!EventSystem.HistoricalEvents.Any(e => e.Tag.Contains("史實") || e.Tag.Contains("架空") || e.Choices.Any(c => c.Label.Contains("史實") || c.Label.Contains("架空") || (c.Stance != null && (c.Stance.Contains("史實") || c.Stance.Contains("架空"))))),
                "事件標題與選項不應包含『史實』或『架空』提示字樣");
            Assert(!GameData.Mutators.Values.Any(m => m.Title.Contains("史實") || m.Title.Contains("架空") || m.Desc.Contains("史實") || m.Desc.Contains("架空")),
                "戰局變數標題與說明不應包含『史實』或『架空』提示字樣");
            Assert(GameData.Endings.Count == 16, $"結局應有 16 種，實際為 {GameData.Endings.Count}");
            LogLine("[PASS] 1. 地圖節點(33)、大陸全境省分(26)、五大分支科技樹(63)、結局(16) 定義完整");

            // 2. 驗證 1950 史實開局（黃金約 $1.25 億、GDP $14 億、殘編裝甲總隊）、1950 即時反攻自由度與時代紀錄點解鎖
            {
                var s50 = SimEngine.NewGame(12345u);
                Assert(s50.Year == 1950 && s50.Month == 1 && s50.Day == 1 && s50.PendingEvents.Count == 0, "1950 開局日期應為 1950/01/01 且無阻擋初始視窗");
                Assert(Math.Abs(s50.Treasury - 1.25) < 1e-6 && Math.Abs(s50.Gdp - 14.0) < 1e-6, "1950 初始國庫應為 $1.25 億、台灣 GDP $14 億");
                Assert(SimEngine.UnitsAt(s50, "TW", UnitDomain.Land).Any(u => u.Type == "arm"), "1950 台灣本島應含 1 支殘編裝甲師");

                // 確認 1950 年可自由反攻（無條約限制或年份封鎖）
                var kmUnit = SimEngine.UnitsAt(s50, "KM", UnitDomain.Land).First();
                Assert(!SimEngine.WouldBreakTreaty(s50, "FJ"), "1950 年尚未簽署共同防禦條約，不應有違約懲罰");
                var earlyAtk = SimEngine.IssueOrder(s50, kmUnit.Id, "FJ");
                Assert(earlyAtk.ok, $"1950 年應允許玩家自行評估後直接發起渡海反攻閩南，失敗原因：{earlyAtk.reason}");
                SimEngine.CancelOrder(s50, kmUnit.Id);

                // 驗證簡化生兵：僅 4 種可造主力（步兵師／裝甲突擊師／艦隊／航空聯隊），且支援已控制之大陸港口就地整編
                var buildableIds = GameData.UnitTypes.Values.Where(u => !u.NoBuild).Select(u => u.Id).OrderBy(x => x).ToArray();
                Assert(buildableIds.SequenceEqual(new[] { "air", "arm", "fleet", "inf" }),
                    $"可自行編成之主力軍種應精簡為 4 種，實際：{string.Join(",", buildableIds)}");
                Assert(!SimEngine.CanBuild(s50, "amph").ok && !SimEngine.CanBuild(s50, "mar").ok && !SimEngine.CanBuild(s50, "abn").ok,
                    "兩棲船團／陸戰師／空降旅不應開放於建軍頁面手動整編");
                var sPortBuild = SimEngine.NewGame(1951u);
                sPortBuild.Treasury = 5.0; sPortBuild.Manpower = 10.0;
                Assert(!SimEngine.CanBuild(sPortBuild, "inf", "FJ").ok, "中共控制之大陸港口不可直接生兵");
                sPortBuild.Control["FJ"] = Faction.ROC;
                Assert(!SimEngine.CanBuild(sPortBuild, "inf", "FJ").ok, "仍在交戰中之大陸港口不可就地生兵");
                sPortBuild.Garrison["FJ"] = 0;
                sPortBuild.Control["JX"] = Faction.ROC; sPortBuild.Garrison["JX"] = 0;
                Assert(!SimEngine.CanBuild(sPortBuild, "inf", "JX").ok, "非港口內陸省分不應開放就地整編主力");
                int fjBefore = SimEngine.UnitsAt(sPortBuild, "FJ", UnitDomain.Land).Count;
                var portRes = SimEngine.BuildUnit(sPortBuild, "inf", "FJ");
                Assert(portRes.ok && SimEngine.UnitsAt(sPortBuild, "FJ", UnitDomain.Land).Count == fjBefore + 1,
                    $"已控制且無敵軍交戰之大陸港口應可直接就地編成步兵師：{portRes.reason}");
                double liftBeforeFleet = SimEngine.LiftCapacity(sPortBuild);
                Assert(SimEngine.BuildUnit(sPortBuild, "fleet").ok && SimEngine.LiftCapacity(sPortBuild) > liftBeforeFleet + 0.5,
                    "編成艦隊應同步擴充跨海登陸運力");

                // 驗證逐日推進（1950/01/01 -> 1950/04/10 觸發舟山撤退，事件期間時間凍結，解決後繼續推進）
                bool hitZhoushan = false;
                for (int guard = 0; guard < 120 && !hitZhoushan; guard++)
                {
                    if (s50.PendingEvents.Count > 0)
                    {
                        int fy = s50.Year, fm = s50.Month, fd = s50.Day;
                        Assert(!SimEngine.AdvanceDay(s50), "尚有未處理事件時 AdvanceDay 應回傳 false 凍結時間");
                        Assert(s50.Year == fy && s50.Month == fm && s50.Day == fd, "事件暫停期間日期不應跳動");
                        if (s50.PendingEvents.Any(e => e.Id == "zhoushan_1950")) hitZhoushan = true;
                        EventSystem.ResolveChoice(s50, 0);
                        continue;
                    }
                    Assert(SimEngine.AdvanceDay(s50), "無事件與結束狀態時應順利推進每日時間");
                }
                Assert(hitZhoushan && s50.Year == 1950 && s50.Month >= 4, "逐日時間流動應於 1950/04/10 觸發海南舟山撤退事件");

                int y0 = s50.Year;
                SimEngine.EndTurn(s50);
                Assert(s50.Year == y0 + 1, "1950 開局推進一年度後年份應 +1");

                var sGrow = SimEngine.NewGame(1952u);
                sGrow.PendingEvents.Clear();
                double fj0 = sGrow.Garrison["FJ"];
                double gd0 = sGrow.Garrison["GD"];
                double dep0 = SimEngine.PrcDeployed(sGrow);
                Assert(Math.Abs(SimEngine.NominalGarrison(sGrow, "FJ") - fj0) < 0.05, "1950 福建名目守軍應等於開局師數");
                sGrow.Year = 1952;
                Assert(SimEngine.NominalGarrison(sGrow, "FJ") >= 13.0, $"1952 福建名目守軍應明顯高於開局。實際 {SimEngine.NominalGarrison(sGrow, "FJ"):F2}");
                sGrow.Year = 1962;
                Assert(SimEngine.NominalGarrison(sGrow, "FJ") >= 19.0, $"1962 福建名目守軍應繼續上升。實際 {SimEngine.NominalGarrison(sGrow, "FJ"):F2}");
                sGrow.Year = 1996;
                double fj1996 = SimEngine.NominalGarrison(sGrow, "FJ");
                sGrow.Year = 2025;
                Assert(SimEngine.NominalGarrison(sGrow, "FJ") > fj1996 + 5, "2025 福建名目守軍應高於 1996");
                sGrow.Year = 1950;
                for (int year = 1951; year <= 1952; year++)
                {
                    sGrow.Year = year;
                    for (int m = 0; m < 12; m++) SimEngine.MonthlyTick(sGrow, stepResearch: false);
                }
                Assert(sGrow.Over == null && sGrow.Control["FJ"] == Faction.PRC && sGrow.Control["TW"] == Faction.ROC,
                    "兩年整補不應改福建或台灣的控制方，也不應結束遊戲");
                Assert(sGrow.Garrison["FJ"] >= fj0 + 1.5 && sGrow.Garrison["GD"] >= gd0 + 0.5,
                    $"1952 年底地圖上的福建、廣東師數應看得出增加（福建 {fj0:F1}→{sGrow.Garrison["FJ"]:F1}，廣東 {gd0:F1}→{sGrow.Garrison["GD"]:F1}）");
                Assert(SimEngine.PrcDeployed(sGrow) >= dep0 + 10, $"頂欄共軍部署應在兩年內增加至少 10 師。實際 {dep0:F0}→{SimEngine.PrcDeployed(sGrow):F0}");
                Assert(sGrow.Logs.Any(l => l.Msg.Contains("擴編")), "共軍擴編應寫進戰報");
                Assert(SimEngine.PrcSea(sGrow, "Z3") > SimEngine.PrcSea(SimEngine.NewGame(1u), "Z3"), "同期共軍台海海空也應高於 1950");

                var sFight = SimEngine.NewGame(77u);
                Assert(!SimEngine.IsContested(sFight, "FJ") && !SimEngine.IsContested(sFight, "Z3"), "開局沒有國軍踏進敵省時，不是交戰區");
                SimEngine.AddUnit(sFight, "inf", "GD", 80);
                Assert(SimEngine.IsContested(sFight, "GD") && sFight.Control["GD"] == Faction.PRC, "國軍已進入仍有守軍的廣東，應視為交戰區");
                sFight.Garrison["GD"] = 0;
                Assert(!SimEngine.IsContested(sFight, "GD"), "守軍清零後不再是交戰區");
                sFight.Control["KM"] = Faction.ROC;
                sFight.Garrison["KM"] = 2;
                Assert(SimEngine.IsContested(sFight, "KM"), "我方島上同時有國軍與敵軍守備時，應視為交戰區");
                using (var splitBmp = new Bitmap(48, 24))
                using (var splitG = Graphics.FromImage(splitBmp))
                {
                    var rectPts = new[] { new PointF(0, 0), new PointF(47, 0), new PointF(47, 23), new PointF(0, 23) };
                    MainForm.FillContestedPolygon(splitG, rectPts, Color.FromArgb(255, 52, 92, 140), Color.FromArgb(255, 150, 58, 52));
                    var leftPx = splitBmp.GetPixel(6, 12);
                    var rightPx = splitBmp.GetPixel(40, 12);
                    Assert(leftPx.B > leftPx.R && rightPx.R > rightPx.B, $"交戰區應左藍右紅。左 {leftPx} 右 {rightPx}");
                }

                var sUnEarly = SimEngine.NewGame(1972u);
                sUnEarly.PendingEvents.Clear();
                sUnEarly.Year = 1969; sUnEarly.Month = 6; sUnEarly.Day = 15;
                SimEngine.TouchFrontClock(sUnEarly);
                sUnEarly.FrontStableSince = sUnEarly.TotalMonths - 120;
                Assert(!EventSystem.UnPressureDue(sUnEarly), "1972 年以前即使戰線長期不動，也不出現聯合國和談壓力");

                var sUn = SimEngine.NewGame(1975u);
                sUn.PendingEvents.Clear();
                sUn.Year = 1975; sUn.Month = 6; sUn.Day = 15;
                Assert(!EventSystem.UnPressureDue(sUn), "只是把年份撥到後期、沒有累積膠著時，不應立刻出現和談聲浪");
                SimEngine.TouchFrontClock(sUn);
                sUn.FrontStableSince = sUn.TotalMonths - 40;
                Assert(EventSystem.UnPressureDue(sUn), "1975 年戰線連續 40 個月沒有省分易手，應開始出現和談壓力");
                sUn.Control["FJ"] = Faction.ROC;
                Assert(!EventSystem.UnPressureDue(sUn), "有省分易手後，膠著計時應歸零，和談壓力消失");

                var sUnTalk = SimEngine.NewGame(1976u);
                sUnTalk.PendingEvents.Clear();
                sUnTalk.Year = 1976; sUnTalk.Month = 9; sUnTalk.Day = 15;
                SimEngine.TouchFrontClock(sUnTalk);
                sUnTalk.FrontStableSince = sUnTalk.TotalMonths - 40;
                EventSystem.CheckDynamic(sUnTalk);
                var unTalk = sUnTalk.PendingEvents.FirstOrDefault(e => e.Id.StartsWith("dyn_un_"));
                Assert(unTalk != null && unTalk.Choices.Count == 3 && (unTalk.Desc.Contains("廣東") || unTalk.Desc.Contains("廣西")),
                    $"台海膠著時，聯合國事件應指向華南戰線。實際：{unTalk?.Desc}");
                foreach (var id in GameData.MainlandIds)
                    if (sUnTalk.Control[id] == Faction.PRC)
                        sUnTalk.Garrison[id] = SimEngine.NominalGarrison(sUnTalk, id);
                double garBefore = GameData.MainlandIds.Where(id => sUnTalk.Control[id] == Faction.PRC).Sum(id => sUnTalk.Garrison[id]);
                var talkYes = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(sUnTalk));
                talkYes.PendingEvents.Add(unTalk);
                EventSystem.ResolveChoice(talkYes, 0);
                Assert(talkYes.Over == null && talkYes.HasFlag("armistice") && talkYes.GetFlag("ceasefireAt") > 0 && talkYes.Control["TW"] == Faction.ROC,
                    "接受會談只記下停火，當天不結束遊戲");
                int ceaseAt = talkYes.GetFlag("ceasefireAt") - 1;
                talkYes.Year = 1950 + (ceaseAt + 35) / 12;
                talkYes.Month = ((ceaseAt + 35) % 12) + 1;
                talkYes.Day = 1;
                SimEngine.CheckEnd(talkYes);
                Assert(talkYes.Over == null, "停火未滿三年不應結束");
                talkYes.Year = 1950 + (ceaseAt + 36) / 12;
                talkYes.Month = ((ceaseAt + 36) % 12) + 1;
                SimEngine.CheckEnd(talkYes);
                Assert(talkYes.Over?.Key == "armistice" && talkYes.Over.Text.Contains("分治") && talkYes.Over.Title.Contains("分治"),
                    $"停火滿三年應以中國分治結束。實際：{talkYes.Over?.Title} {talkYes.Over?.Text}");

                var sNote = SimEngine.NewGame(3u);
                string n0 = EventSystem.FrontNotice(sNote, 0);
                string n1 = EventSystem.FrontNotice(sNote, 1);
                Assert(n0.Contains("彈藥") && n1.Contains("投降") && n0 != n1, $"戰地電訊應輪流報導前線狀況。實際：{n0} / {n1}");
                Assert(n0.Contains("廣東") || n0.Contains("廣西"), $"開局電訊應指向當前敵前。實際：{n0}");

                var talkNo = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(sUnTalk));
                talkNo.PendingEvents.Add(unTalk);
                EventSystem.ResolveChoice(talkNo, 1);
                double garAfter = GameData.MainlandIds.Where(id => talkNo.Control[id] == Faction.PRC).Sum(id => talkNo.Garrison[id]);
                bool flipped = GameData.MainlandIds.Any(id => sUnTalk.Control[id] != talkNo.Control[id]);
                Assert(talkNo.Over == null && !talkNo.HasFlag("armistice") && !flipped && garAfter < garBefore - 0.2,
                    $"拒絕和談應削減敵前守軍且不改省色、不記停火。守軍 {garBefore:F1}→{garAfter:F1}");

                var sUnNorth = SimEngine.NewGame(1978u);
                sUnNorth.PendingEvents.Clear();
                foreach (var id in new[] { "FJ", "GD", "ZJ", "JS" }) sUnNorth.Control[id] = Faction.ROC;
                sUnNorth.Year = 1978; sUnNorth.Month = 8; sUnNorth.Day = 15;
                SimEngine.TouchFrontClock(sUnNorth);
                sUnNorth.FrontStableSince = sUnNorth.TotalMonths - 100;
                EventSystem.CheckDynamic(sUnNorth);
                var unNorth = sUnNorth.PendingEvents.FirstOrDefault(e => e.Id.StartsWith("dyn_un_"));
                Assert(unNorth != null && unNorth.Title.Contains("強制調解") && unNorth.Desc.Contains("山東") && !unNorth.Desc.Contains("海南"),
                    $"膠著超過八年且戰線已北移時，事件應改寫到山東並提高調解壓力。實際：{unNorth?.Title} {unNorth?.Desc}");

                // 驗證從 1950 推進至達標（如 1962 或光復 >=1 省且 GDP>=22）自動解鎖紀錄點並可序列化還原
                var cpStore = new Dictionary<string, CheckpointRecord>();
                Assert(SimEngine.EvaluateCheckpoints(s50, cpStore).Count == 0, "剛開局 1951 尚未達標不應提早解鎖紀錄點");
                s50.Year = 1962; s50.Turn = 12; s50.PendingEvents.Clear();
                var unlocked = SimEngine.EvaluateCheckpoints(s50, cpStore);
                Assert(unlocked.Contains("cp_1962") && cpStore.ContainsKey("cp_1962"), "推進達標後應自動解鎖 cp_1962 紀錄點");
                Assert(s50.HasFlag("cpRecorded_cp_1962"), "紀錄點快照應標記已紀錄以防覆寫分岐點");

                // 驗證「軍事部署電文」具備多元事件組合（含 2 卡 / 3 卡、前線外島與大陸橋頭堡情境，且短期內不重複）
                var sm = SimEngine.NewGame(777u);
                sm.Control["FJ"] = Faction.ROC;
                sm.Garrison["FJ"] = 0;
                var milTitles = new HashSet<string>();
                var choiceCounts = new HashSet<int>();
                for (int k = 0; k < 8; k++)
                {
                    sm.Year = GameData.StartYear + (k / 12);
                    sm.Month = (k % 12) + 1;
                    var evMil = EventSystem.CreateMilitaryDeploymentEvent(sm);
                    Assert(evMil != null && evMil.Tag == "軍事部署" && evMil.Choices.Count >= 2, "軍事部署電文應為有效決策事件");
                    Assert(!evMil.Title.Contains("史實") && !evMil.Title.Contains("架空") && !evMil.Title.Contains("二元抉擇"), "軍事部署電文不得包含禁止標籤字樣");
                    milTitles.Add(evMil.Title);
                    choiceCounts.Add(evMil.Choices.Count);
                    sm.PendingEvents.Add(evMil);
                    EventSystem.ResolveChoice(sm, k % evMil.Choices.Count);
                }
                Assert(milTitles.Count == 8 && choiceCounts.Contains(2) && choiceCounts.Contains(3),
                    $"連續觸發 8 次軍事部署電文應產生 8 種不同事件並涵蓋 2 卡/3 卡組合，實際種數={milTitles.Count}");

                // 2B. 戰線隨進度動態調整驗證：推進至華北／黃渤海與北疆抗俄戰線後，事件對象、標題與削弱效果必須隨之轉移，不得停留在閩南／台海或誤傷已光復省分
                var sNorth = SimEngine.NewGame(1955u);
                sNorth.PendingEvents.Clear();
                sNorth.Year = 1966; sNorth.Month = 6; sNorth.Day = 15; sNorth.Turn = 16;
                foreach (var libId in new[] { "FJ", "ZJ", "GD", "GX", "JX", "JS", "AH", "HB", "HA" })
                {
                    sNorth.Control[libId] = Faction.ROC;
                    sNorth.Garrison[libId] = 0;
                    SimEngine.AddUnit(sNorth, "inf", libId, 90);
                }
                sNorth.Cooldowns.Clear();
                for (int k = 0; k < 10; k++)
                {
                    sNorth.Month = (k % 12) + 1;
                    sNorth.Day = 15;
                    var evN = EventSystem.CreateMilitaryDeploymentEvent(sNorth);
                    Assert(!evN.Title.Contains("東南沿海") && !evN.Title.Contains("台灣海峽"),
                        $"戰線已推進至華北前線時，軍事部署電文不應再以東南沿海／台海為主軸：{evN.Title}");
                    Assert(evN.Choices.All(c => !c.Hint.Contains("沿海五省") && !c.Hint.Contains("閩南") && !c.Hint.Contains("閩北")),
                        $"戰線已推離閩粵時，選項不應出現過時的『沿海五省／閩南／閩北』：{evN.Title}");
                    sNorth.PendingEvents.Add(evN);
                    double fjUnrestBefore = sNorth.Unrest["FJ"];
                    double fjGarBefore = sNorth.Garrison["FJ"];
                    EventSystem.ResolveChoice(sNorth, 0);
                    Assert(Math.Abs(sNorth.Unrest["FJ"] - fjUnrestBefore) < 1e-6 && Math.Abs(sNorth.Garrison["FJ"] - fjGarBefore) < 1e-6,
                        $"已光復的福建(FJ)不應被軍事部署事件當作敵方目標受損：{evN.Title}");
                }
                var kinmen823 = EventSystem.HistoricalEvents.First(e => e.Id == "kinmen_823_1958");
                var lopNor64 = EventSystem.HistoricalEvents.First(e => e.Id == "lop_nor_1964");
                Assert(kinmen823.Cond != null && !kinmen823.Cond(sNorth),
                    "福建已光復時不應觸發共軍自福建砲擊封鎖金門之八二三砲戰事件");
                sNorth.Control["XJ"] = Faction.ROC; sNorth.Garrison["XJ"] = 0;
                Assert(lopNor64.Cond != null && !lopNor64.Cond(sNorth),
                    "新疆已非中共控制時不應觸發中共新疆羅布泊核試爆事件");

                // 北疆抗俄戰線：關內全復、僅剩俄占東北與新疆，動態與部署事件應隨戰線轉為北疆抗俄且不應再稱共軍閩南起義
                var sUssrFront = SimEngine.NewGame(2028u);
                sUssrFront.PendingEvents.Clear();
                sUssrFront.Year = 1970; sUssrFront.Month = 5; sUssrFront.Day = 15; sUssrFront.Turn = 20;
                foreach (var mid in GameData.MainlandIds)
                {
                    sUssrFront.Control[mid] = Faction.ROC;
                    sUssrFront.Garrison[mid] = 0;
                }
                SimEngine.AddUnit(sUssrFront, "arm", "HE", 95);
                SimEngine.SeizeByUssr(sUssrFront, "HLJ", 7.0);
                SimEngine.SeizeByUssr(sUssrFront, "XJ", 6.0);
                var ussrTitles = new System.Collections.Generic.List<string>();
                for (int k = 0; k < 6; k++)
                {
                    var evU = EventSystem.CreateMilitaryDeploymentEvent(sUssrFront);
                    Assert(!evU.Title.Contains("東南") && evU.Choices.All(c => !c.Hint.Contains("共軍")),
                        $"僅剩北疆俄占區對峙時事件不應出現共軍／東南沿海描述：{evU.Title}");
                    ussrTitles.Add(evU.Title);
                }
                Assert(ussrTitles.Any(t => t.Contains("黑龍江") || t.Contains("新疆") || t.Contains("北疆")),
                    "僅剩北疆俄占區戰線時軍事部署應針對俄占東北／新疆前線調整");
            }
            LogLine("[PASS] 2. 1950 史實國庫/經濟/兵力開局、首年即時反攻自由度與紀錄點解鎖系統驗證通過");

            // 3. 驗證俄國趁機侵占北疆、對俄交涉與「遊戲時間結束／全滅後方終局」規則
            {
                var s = SimEngine.NewGame(999u);
                s.PendingEvents.Clear();
                SimEngine.SeizeByUssr(s, "HLJ", 7.0);
                SimEngine.SeizeByUssr(s, "XJ", 6.0);
                Assert(SimEngine.UssrNodes(s).Count == 2, "俄占省分應為 2 省 (HLJ, XJ)");

                // 攻克北京但北疆仍有俄軍 -> 觸發北平光復與北疆俄占抉擇事件，且戰役繼續
                s.Control["HE"] = Faction.ROC;
                s.Garrison["HE"] = 0;
                SimEngine.AddUnit(s, "inf", "HE", 100);
                SimEngine.CheckEnd(s);
                Assert(s.Over == null && s.PendingEvents.Any(e => e.Id == "beijing_ussr_choice"), "攻克北京且有俄占區時應觸發 beijing_ussr_choice 事件");

                // 選擇選項 1（北上抗俄繼續作戰）
                EventSystem.ResolveChoice(s, 0);
                Assert(s.Over == null, "選擇繼續抗俄後戰役應繼續");

                // 僅收復 HLJ, XJ 而仍有其他中共省分 -> 遊戲時間未結束且敵方未被全滅，不應提早結束
                s.Control["HLJ"] = Faction.ROC; s.Garrison["HLJ"] = 0;
                s.Control["XJ"] = Faction.ROC; s.Garrison["XJ"] = 0;
                SimEngine.CheckEnd(s);
                Assert(s.Over == null, $"遊戲途中尚有敵方省分未全滅時，即使攻克北京與俄占區亦不應提早結束，實際為 {s.Over?.Key}");

                // 將大陸所有省分全數收復（敵方全數肅清全滅）-> 立即達成 full 完全收復結局
                foreach (var mid in GameData.MainlandIds)
                {
                    s.Control[mid] = Faction.ROC;
                    s.Garrison[mid] = 0;
                }
                SimEngine.CheckEnd(s);
                Assert(s.Over != null && s.Over.Key == "full", $"敵方全數肅清全滅後應達成 full 結局，實際為 {s.Over?.Key}");
            }
            {
                var s2 = SimEngine.NewGame(888u);
                s2.PendingEvents.Clear();
                s2.Year = 1965;
                SimEngine.SeizeByUssr(s2, "HLJ", 7.0);
                s2.Control["HE"] = Faction.ROC; s2.Garrison["HE"] = 0;
                SimEngine.CheckEnd(s2);
                EventSystem.ResolveChoice(s2, 1); // 選擇暫簽北疆停火協議
                Assert(s2.Over == null, $"遊戲途中(1965年)簽署對俄停火立場不應提早跳出結局，實際={s2.Over?.Key}");
                s2.Year = GameData.EndYear + 1;
                SimEngine.CheckEnd(s2);
                Assert(s2.Over != null && s2.Over.Key == "split_ussr", "終戰結算時北疆仍由俄占應達成 split_ussr 結局");
            }
            LogLine("[PASS] 3. 俄國趁機侵占北疆、全滅即時勝利與終戰對俄結局驗證通過");

            // 4. 驗證遊戲過程中僅全滅會提早結束，軍閥／停戰／崩潰／破產等皆須待時間結束後出現
            {
                var s = SimEngine.NewGame(777u);
                s.PendingEvents.Clear();
                s.Year = 1966;
                s.Flags["armistice"] = 1;
                SimEngine.CheckEnd(s);
                Assert(s.Over == null, $"遊戲進行中(1966年)停戰立場不應提早跳出結局，實際={s.Over?.Key}");
                s.Year = GameData.EndYear + 1;
                SimEngine.CheckEnd(s);
                Assert(s.Over?.Key == "armistice", "終戰結算時應達成 armistice 結局");

                var sWp = SimEngine.NewGame(778u);
                sWp.PendingEvents.Clear();
                sWp.Year = 1968;
                sWp.PrcStab = 0;
                sWp.Flags["prcZeroTurns"] = 2;
                SimEngine.CheckEnd(sWp);
                Assert(sWp.Over == null, $"遊戲進行中(1968年)即使中共穩定度低落，亦不應提早觸發軍閥割據結局，實際={sWp.Over?.Key}");

                sWp.Year = GameData.EndYear + 1;
                SimEngine.CheckEnd(sWp);
                Assert(sWp.Over?.Key == "warlordP", "終局時中共崩潰且國軍登陸兵力不足應達成 warlordP 軍閥割據結局");

                var sWr = SimEngine.NewGame(779u);
                sWr.PendingEvents.Clear();
                sWr.Year = 1965;
                sWr.Control["FJ"] = Faction.ROC;
                sWr.Control["GD"] = Faction.ROC;
                sWr.Morale = 18;
                sWr.SupplyRatio = 0.4;
                sWr.Flags["warlordRisk"] = 3;
                SimEngine.CheckEnd(sWr);
                Assert(sWr.Over == null, $"遊戲進行中(1965年)不應提早出現藩鎮割據結局，實際={sWr.Over?.Key}");

                sWr.Year = GameData.EndYear + 1;
                SimEngine.CheckEnd(sWr);
                Assert(sWr.Over?.Key == "warlordR", "終局時前線長期斷補低民心應達成 warlordR 藩鎮割據結局");

                var sBank = SimEngine.NewGame(782u);
                sBank.PendingEvents.Clear();
                sBank.Year = 1970;
                sBank.Treasury = 0;
                sBank.Debt = sBank.Gdp * 2.0;
                sBank.PrcStab = 0;
                sBank.Control["FJ"] = Faction.ROC; sBank.Control["GD"] = Faction.ROC;
                sBank.Control["ZJ"] = Faction.ROC; sBank.Control["JS"] = Faction.ROC;
                sBank.Morale = 60;
                SimEngine.CheckEnd(sBank);
                Assert(sBank.Over == null, $"遊戲進行中(1970年)破產與中共崩潰皆不應提早出現結局，實際={sBank.Over?.Key}");
                sBank.Year = GameData.EndYear + 1;
                SimEngine.CheckEnd(sBank);
                Assert(sBank.Over?.Key == "bankrupt", "終戰結算時國家破產應達成 bankrupt 結局");

                var sDynNoEarly = SimEngine.NewGame(781u);
                sDynNoEarly.PendingEvents.Clear();
                sDynNoEarly.Year = 1966; sDynNoEarly.Month = 6; sDynNoEarly.Day = 15; sDynNoEarly.Turn = 16;
                sDynNoEarly.Control["FJ"] = Faction.ROC; sDynNoEarly.Control["GD"] = Faction.ROC;
                sDynNoEarly.PrcStab = 12; sDynNoEarly.Morale = 20; sDynNoEarly.SupplyRatio = 0.4;
                for (int t = 0; t < 24; t++)
                {
                    sDynNoEarly.Cooldowns.Clear();
                    sDynNoEarly.PendingEvents.Clear();
                    sDynNoEarly.Year = 1966;
                    sDynNoEarly.Month = (t % 12) + 1;
                    EventSystem.CheckDynamic(sDynNoEarly);
                    if (sDynNoEarly.PendingEvents.Count > 0)
                    {
                        var ev = sDynNoEarly.PendingEvents[0];
                        for (int ci = 0; ci < ev.Choices.Count; ci++)
                        {
                            var probe = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(sDynNoEarly));
                            probe.PendingEvents.Add(ev);
                            EventSystem.ResolveChoice(probe, ci);
                            Assert(probe.Over == null,
                                $"動態事件 {ev.Id} 選項 {ci} 於遊戲途中（時間未結束且未全滅）不應直接結束出現結局：{probe.Over?.Key}");
                        }
                    }
                }

                // 4C. 1973 以後補上的時局事件：依當前戰線改寫目標，力道按守軍比例，且不在遊戲途中結束
                {
                    string[] lateIds = {
                        "paris_1973", "oil_1974", "chiang_1975", "mao_1976", "reagan_1981", "hk_1984", "ccg_1988", "gulf_1990", "wangku_1993", "cornell_1995",
                        "liangguo_1999", "election_2000", "wto_2002", "antisec_2005", "links_2008", "diaoyu_2012", "sunflower_2014", "scs_2016",
                        "hk_2019", "covid_2020", "ukraine_2022", "pelosi_2022", "drone_2023", "budget_2024"
                    };
                    Assert(GameData.EndYear == 2025, $"終戰年應為 2025，實際 {GameData.EndYear}");
                    Assert(GameData.Interp(GameData.CurvePrcGdp, 2025) > GameData.Interp(GameData.CurvePrcGdp, 1996),
                        "2025 中共 GDP 曲線應高於 1996，不能停在舊終點");
                    Assert(GameData.Interp(GameData.CurveRocGdp, 2025) > GameData.Interp(GameData.CurveRocGdp, 1996),
                        "2025 台灣 GDP 曲線應高於 1996");
                    var sRp = SimEngine.NewGame(32u);
                    int dCoast = SimEngine.EstDaysRemaining(sRp, "recon_coast");
                    int dNuke = SimEngine.EstDaysRemaining(sRp, "nuke_weapon");
                    Assert(dCoast >= 10 && dCoast <= 22, $"1950 外島雷達應約兩週半（10–22 天），實際 {dCoast} 天");
                    Assert(dNuke >= 80 && dNuke <= 125, $"1950 Lv.1 核武原型應大幅縮短（80–125 天），實際 {dNuke} 天");
                    foreach (var id in lateIds)
                    {
                        var template = EventSystem.HistoricalEvents.FirstOrDefault(e => e.Id == id);
                        Assert(template != null && template.Choices.Count == 3 && template.Specialize != null,
                            $"後期事件 {id} 應為三選一，並在觸發時依戰局改寫");
                    }

                    GameState Prepare(uint seed, bool north)
                    {
                        var s = SimEngine.NewGame(seed);
                        s.PendingEvents.Clear();
                        if (north)
                        {
                            foreach (var id in new[] { "FJ", "GD", "ZJ", "JS" }) s.Control[id] = Faction.ROC;
                        }
                        foreach (var ev in EventSystem.HistoricalEvents)
                            if (ev.Id != "mao_1976") s.FiredEvents.Add(ev.Id);
                        s.Year = 1976; s.Month = 9; s.Day = 9;
                        return s;
                    }

                    var sStand = Prepare(880u, north: false);
                    EventSystem.CheckHistoricalForDate(sStand);
                    var maoStand = sStand.PendingEvents.FirstOrDefault(e => e.Id == "mao_1976");
                    Assert(maoStand != null && (maoStand.Desc.Contains("廣東") || maoStand.Desc.Contains("廣西")) && !maoStand.Desc.Contains("山東"),
                        $"台海對峙時，對岸權力真空事件應指向華南敵前。實際：{maoStand?.Desc}");

                    var sNorthMao = Prepare(881u, north: true);
                    sNorthMao.Garrison["SD"] = 20;
                    EventSystem.CheckHistoricalForDate(sNorthMao);
                    var maoNorth = sNorthMao.PendingEvents.FirstOrDefault(e => e.Id == "mao_1976");
                    Assert(maoNorth != null && maoNorth.Desc.Contains("山東") && !maoNorth.Desc.Contains("海南") && !maoNorth.Desc.Contains("廣東"),
                        $"閩粵浙蘇已光復時，同一事件應改寫到山東一線。實際：{maoNorth?.Desc}");
                    var big = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(sNorthMao));
                    big.PendingEvents.Add(maoNorth);
                    EventSystem.ResolveChoice(big, 0);
                    Assert(big.Over == null && big.Control["SD"] == Faction.PRC && big.Garrison["SD"] < 20 && big.Garrison["SD"] >= 16,
                        $"施壓應削掉山東約一成守軍且不改省色。實際守軍 {big.Garrison["SD"]:F2}，結局 {big.Over?.Key}");

                    var sSmall = Prepare(882u, north: true);
                    sSmall.Garrison["SD"] = 10;
                    EventSystem.CheckHistoricalForDate(sSmall);
                    var maoSmall = sSmall.PendingEvents.First(e => e.Id == "mao_1976");
                    var small = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(sSmall));
                    small.PendingEvents.Add(maoSmall);
                    EventSystem.ResolveChoice(small, 0);
                    double dropBig = 20 - big.Garrison["SD"];
                    double dropSmall = 10 - small.Garrison["SD"];
                    Assert(dropBig > dropSmall + 0.5, $"守軍愈厚，施壓削掉的師數應愈多（20→{big.Garrison["SD"]:F2}，10→{small.Garrison["SD"]:F2}）");

                    foreach (var id in lateIds)
                    {
                        var template = EventSystem.HistoricalEvents.First(e => e.Id == id);
                        var sLate = SimEngine.NewGame(883u);
                        sLate.PendingEvents.Clear();
                        sLate.Year = template.Year; sLate.Month = template.Month; sLate.Day = template.Day;
                        foreach (var other in EventSystem.HistoricalEvents)
                            if (other.Id != id) sLate.FiredEvents.Add(other.Id);
                        EventSystem.CheckHistoricalForDate(sLate);
                        var fired = sLate.PendingEvents.FirstOrDefault(e => e.Id == id);
                        Assert(fired != null, $"{id} 在其日期、大陸仍有中共省時應出現");
                        for (int ci = 0; ci < fired.Choices.Count; ci++)
                        {
                            var probe = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(sLate));
                            probe.PendingEvents.Add(fired);
                            EventSystem.ResolveChoice(probe, ci);
                            Assert(probe.Over == null && probe.Control["TW"] == Faction.ROC,
                                $"後期事件 {id} 選項 {ci} 不應在遊戲途中結束或失去台灣：{probe.Over?.Key}");
                        }
                    }

                    GameState PrepareDated(string id, uint seed)
                    {
                        var template = EventSystem.HistoricalEvents.First(e => e.Id == id);
                        var s = SimEngine.NewGame(seed);
                        s.PendingEvents.Clear();
                        s.Year = template.Year; s.Month = template.Month; s.Day = template.Day;
                        foreach (var other in EventSystem.HistoricalEvents)
                            if (other.Id != id) s.FiredEvents.Add(other.Id);
                        return s;
                    }

                    var sUk = PrepareDated("ukraine_2022", 900u);
                    sUk.Control["XJ"] = Faction.USSR;
                    sUk.Garrison["XJ"] = 10;
                    EventSystem.CheckHistoricalForDate(sUk);
                    var uk = sUk.PendingEvents.FirstOrDefault(e => e.Id == "ukraine_2022");
                    Assert(uk != null && uk.Desc.Contains("俄") && uk.Desc.Contains("新疆") && uk.Desc.Contains("北疆"),
                        $"北疆仍有俄軍時，歐洲戰爭事件應寫明俄軍與新疆。實際：{uk?.Desc}");
                    var ukHit = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(sUk));
                    ukHit.PendingEvents.Add(uk);
                    EventSystem.ResolveChoice(ukHit, 2);
                    Assert(ukHit.Over == null && ukHit.Control["XJ"] == Faction.USSR && ukHit.Garrison["XJ"] < 10 && ukHit.Garrison["XJ"] > 8,
                        $"削弱俄軍駐守應減少新疆守軍且不改控制方。實際守軍 {ukHit.Garrison["XJ"]:F2}，控制 {ukHit.Control["XJ"]}，結局 {ukHit.Over?.Key}");

                    var sNoUk = PrepareDated("ukraine_2022", 901u);
                    foreach (var id in GameData.MainlandIds)
                        if (sNoUk.Control[id] == Faction.PRC)
                            sNoUk.Garrison[id] = SimEngine.NominalGarrison(sNoUk, id);
                    double prcBefore = GameData.MainlandIds.Where(id => sNoUk.Control[id] == Faction.PRC).Sum(id => sNoUk.Garrison[id]);
                    EventSystem.CheckHistoricalForDate(sNoUk);
                    var ukOpen = sNoUk.PendingEvents.First(e => e.Id == "ukraine_2022");
                    Assert(!ukOpen.Desc.Contains("新疆"), $"北疆沒有俄軍時，歐洲戰爭事件不應寫成新疆仍被占領。實際：{ukOpen.Desc}");
                    var ukOpenHit = JsonSerializer.Deserialize<GameState>(JsonSerializer.Serialize(sNoUk));
                    ukOpenHit.PendingEvents.Add(ukOpen);
                    EventSystem.ResolveChoice(ukOpenHit, 2);
                    double prcAfter = GameData.MainlandIds.Where(id => ukOpenHit.Control[id] == Faction.PRC).Sum(id => ukOpenHit.Garrison[id]);
                    bool anyFlip = GameData.MainlandIds.Any(id => sNoUk.Control[id] != ukOpenHit.Control[id]);
                    Assert(ukOpenHit.Over == null && !anyFlip && prcAfter < prcBefore - 0.05,
                        $"沒有俄占省時，施壓應削當前敵前守軍且不改省色。守軍 {prcBefore:F2}→{prcAfter:F2}，結局 {ukOpenHit.Over?.Key}");
                }

                // 若僅本島變色但仍有大陸光復省與國軍部隊抵抗，不應判全滅；全軍／全境覆沒才立即達成 invaded
                var sInv = SimEngine.NewGame(780u);
                sInv.PendingEvents.Clear();
                sInv.Year = 1965;
                sInv.Control["TW"] = Faction.PRC;
                sInv.Control["FJ"] = Faction.ROC;
                SimEngine.AddUnit(sInv, "inf", "FJ", 90);
                SimEngine.CheckEnd(sInv);
                Assert(sInv.Over == null, $"仍有大陸據點與部隊抵抗時不應判為全滅結局，實際={sInv.Over?.Key}");

                foreach (var nid in GameData.NodeIds) sInv.Control[nid] = Faction.PRC;
                sInv.Units.Clear();
                SimEngine.CheckEnd(sInv);
                Assert(sInv.Over?.Key == "invaded", "國軍全境失守被全滅時應立即達成 invaded 結局");
            }
            LogLine("[PASS] 4. 僅時間結束後或全滅時結算結局、軍閥／停戰／崩潰不搶跑驗證通過");

            // 4B. 迴歸測試：海空部隊駐防 Z3/Z2 等海域期間發生地面戰鬥月結時，不得以海域代碼查詢地形表拋出 KeyNotFoundException('Z3')
            {
                var sBattle = SimEngine.NewGame(2026u);
                sBattle.PendingEvents.Clear();
                var kmAttackers = SimEngine.UnitsAt(sBattle, "KM", UnitDomain.Land).Take(2).ToList();
                Assert(SimEngine.UnitsAt(sBattle, "Z3", UnitDomain.Sea).Count > 0, "1950 開局台海 (Z3) 應有駐防艦隊");
                foreach (var u in kmAttackers)
                {
                    var ord = SimEngine.IssueOrder(sBattle, u.Id, "FJ");
                    Assert(ord.ok, $"金門出兵閩南前應可成功下令：{ord.reason}");
                }
                SimEngine.MonthlyTick(sBattle, stepResearch: false);
                Assert(sBattle.LastReport != null && sBattle.LastReport.BattleNodes.Contains("FJ"),
                    "MonthlyTick 應實際執行閩南戰役 (BattleNodes.Count > 0) 而不拋出 Z3 字典例外");

                // 簡化調兵驗證：我方控制港島互調 (如金門 -> 海南) 免佔登陸運力，且一鍵海空佈防會補齊南海 Z4
                var sEasyDeploy = SimEngine.NewGame(2027u);
                sEasyDeploy.PendingEvents.Clear();
                var kmReserve = SimEngine.UnitsAt(sEasyDeploy, "KM", UnitDomain.Land).First();
                var chkHi = SimEngine.CheckMove(sEasyDeploy, kmReserve, "HI");
                Assert(chkHi.ok && chkHi.kind == "sea" && chkHi.lift == 0,
                    "我方控制港島之間跨海調防（如金門 -> 海南）應免佔兩棲登陸運力 (lift == 0)");
                double liftBeforeHi = SimEngine.LiftUsed(sEasyDeploy);
                Assert(SimEngine.IssueOrder(sEasyDeploy, kmReserve.Id, "HI").ok && SimEngine.LiftUsed(sEasyDeploy) == liftBeforeHi,
                    "調防海南島不應消耗兩棲搶灘運力");
                SimEngine.AutoBalanceNavalAir(sEasyDeploy);
                Assert(SimEngine.UnitsAt(sEasyDeploy, "Z4", UnitDomain.Sea).Count >= 1 && SimEngine.UnitsAt(sEasyDeploy, "Z4", UnitDomain.Air).Count >= 1,
                    "一鍵智能海空佈防應自動派駐艦隊與空軍進入南海 Z4 護航海南");
            }
            LogLine("[PASS] 4B. 海空部隊駐防海域 (Z3) 期間發生戰役月結安全無例外 ＋ 簡化跨海調防與智能海空佈防通過");

            // 5. 驗證存讀檔、海域直調、勝算預估、主選單鎖定紀錄點與極簡戰情室 UI
            string cpFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "checkpoints.json");
            string endFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "endings_unlocked.json");
            string cpBackup = File.Exists(cpFile) ? File.ReadAllText(cpFile) : null;
            string endBackup = File.Exists(endFile) ? File.ReadAllText(endFile) : null;
            try
            {
                if (File.Exists(cpFile)) File.Delete(cpFile);
                if (File.Exists(endFile)) File.Delete(endFile);

                var st = EventSystem.NewScenario("1962", 42u);
                string json = JsonSerializer.Serialize(st);
                // Test 4C: 真實行軍天數、兩棲登陸籌備（需 >= 50% 制海權）與多省陸路自動尋路＋前線自動補防
                {
                    var sPlan = SimEngine.NewGame(2027u);
                    sPlan.PendingEvents.Clear();
                    sPlan.Treaty = false;
                    sPlan.AutoFrontFill = false;
                    // 把所有我方艦隊移離 Z3 以外至 Z1 以製造 Z3 < 50% 制海權
                    foreach (var fl in sPlan.Units.Where(u => u.Type == "fleet")) { fl.Loc = "Z1"; fl.Moved = false; }
                    sPlan.Units.First(u => u.Type == "fleet").Loc = "Z1";
                    // 直接壓縮 Z3 僅存 0 艦使制海低於 50%
                    Assert(SimEngine.SeaSup(sPlan, "Z3") < 0.50, "清空 Z3 艦隊後台海制海權應低於 50%");
                    var attackers = SimEngine.UnitsAt(sPlan, "KM", UnitDomain.Land).Take(2).ToList();
                    var atkRes = SimEngine.AttackNow(sPlan, attackers.Select(u => u.Id), "FJ");
                    Assert(atkRes.moved == 2 && sPlan.Orders.Count == 2 && sPlan.Orders.All(o => o.IsPrep),
                        "對敵港發起跨海進攻應建立兩棲登陸籌備訂單而非瞬間傳送");
                    // 再將所有艦隊移回 Z1，確保 Z3 不足 50% 時即使滿籌備天數也不搶航
                    foreach (var fl in sPlan.Units.Where(u => u.Type == "fleet")) fl.Loc = "Z1";
                    for (int d = 0; d < 10; d++) SimEngine.AdvanceDay(sPlan);
                    Assert(sPlan.Orders.Count == 2 && sPlan.Orders.All(o => o.IsPrep && o.PrepDays == o.PrepRequired),
                        "Z3 制海權未達 50% 時，登陸計畫籌備滿期應持續待命起航，不盲目強渡");
                    // 一鍵海空佈防後補回制海權 >= 50%，隔日即應起航，數日後抵達福建交戰
                    SimEngine.AutoBalanceNavalAir(sPlan);
                    if (SimEngine.SeaSup(sPlan, "Z3") < 0.50)
                    {
                        foreach (var fl in sPlan.Units.Where(u => u.Type == "fleet").Take(3)) fl.Loc = "Z3";
                    }
                    Assert(SimEngine.SeaSup(sPlan, "Z3") >= 0.50, "補派艦隊後 Z3 制海權應達標 >= 50%");
                    SimEngine.AdvanceDay(sPlan);
                    Assert(sPlan.Orders.Count == 2 && sPlan.Orders.All(o => !o.IsPrep),
                        "制海權達 50% 後下一日登陸船團應正式起航轉入航渡行進狀態");
                    int fjLogCountBefore = sPlan.Logs.Count;
                    for (int d = 0; d < 8 && sPlan.Orders.Count > 0; d++) SimEngine.AdvanceDay(sPlan);
                    Assert(sPlan.Orders.Count == 0 && (SimEngine.UnitsAt(sPlan, "FJ", UnitDomain.Land).Count > 0 || sPlan.Logs.Count > fjLogCountBefore),
                        "跨海船團經短天數航渡抵達福建時應立即執行搶灘戰鬥並結算");

                    // 驗證多省友軍自動尋路與前線自動補防（不瞬移、看得出在途行軍）
                    var sFront = SimEngine.NewGame(2028u);
                    sFront.PendingEvents.Clear();
                    sFront.Control["GD"] = Faction.ROC; sFront.Garrison["GD"] = 0;
                    sFront.Control["GX"] = Faction.ROC; sFront.Garrison["GX"] = 0;
                    sFront.Control["FJ"] = Faction.ROC; sFront.Garrison["FJ"] = 0;
                    sFront.Control["JX"] = Faction.PRC; sFront.Garrison["JX"] = 8.0;
                    var path = SimEngine.FindFriendlyLandPath(sFront, "GX", "FJ", allowHostileDest: false);
                    Assert(path != null && path.SequenceEqual(new[] { "GX", "GD", "FJ" }),
                        "跨省友軍尋路應找到廣西 -> 廣東 -> 福建最短安全路徑");
                    SimEngine.AddUnit(sFront, "inf", "GX", 95);
                    SimEngine.AddUnit(sFront, "inf", "GX", 95);
                    int frontBefore = sFront.Orders.Count;
                    SimEngine.AutoBalanceLandForces(sFront);
                    Assert(sFront.Orders.Count > frontBefore && sFront.Orders.All(o => !o.IsPrep && o.TotalDays >= 2 && o.TotalDays <= 8),
                        "前線自動補防應下達短程真實行軍（2～8 天）而非瞬間移動後方部隊");
                }
                Console.WriteLine("[PASS] 4C. 短程真實行軍、兩棲籌備 (>=50%制海＋奪港) 與多省自動補防驗證通過");

                var loaded = JsonSerializer.Deserialize<GameState>(json);
                Assert(loaded != null && loaded.Year == 1962 && loaded.Control.Count == 33, "JSON 存讀檔反序列化應完整保留狀態");

                int beforeZ1 = SimEngine.UnitsAt(st, "Z1", UnitDomain.Sea).Count;
                var adjRes = SimEngine.AdjustZoneUnit(st, "Z1", "fleet", true);
                Assert(adjRes.ok && SimEngine.UnitsAt(st, "Z1", UnitDomain.Sea).Count == beforeZ1 + 1, "AdjustZoneUnit(+fleet) 應能調入 1 艘艦隊至黃渤海");
                var twLand = SimEngine.UnitsAt(st, "TW", UnitDomain.Land).Take(2).ToList();
                var odds = SimEngine.EstimateMoveOdds(st, twLand, "FJ");
                Assert(!string.IsNullOrEmpty(odds.label), "EstimateMoveOdds 應回傳非空勝算評估標籤");

                using var form = new MainForm();
                form.Show();
                Application.DoEvents();

                Assert(!HasButtonWithText(form, "快進"), "介面中不應存在任何『快進』按鈕");
                Assert(!HasButtonWithText(form, "推進下一年度"), "已改為即時時間流逝後，不應保留『推進下一年度』手動按鈕");
                Assert(!HasNonBmpControlText(form), "介面文字中不應出現非 BMP 字元（避免 GDI+ 顯示方塊 □）");

                // 驗證即時時間控制列：暫停 / 1× / 2× / 4×
                Assert(FindButtonWithText(form, "暫停") != null && FindButtonWithText(form, "1×") != null && FindButtonWithText(form, "2×") != null && FindButtonWithText(form, "4×") != null,
                    "頂部 HUD 應包含 暫停 / 1× / 2× / 4× 即時時間控制按鈕");

                // 驗證初始狀態下，主選單僅 1950 啟用，3 個紀錄點按鈕皆為未解鎖鎖定狀態
                var b1950 = FindButtonWithText(form, "1950 風雨飄搖");
                var bCp1 = FindButtonWithText(form, "紀錄點 I");
                var bCp2 = FindButtonWithText(form, "紀錄點 II");
                var bCp3 = FindButtonWithText(form, "紀錄點 III");
                Assert(b1950 != null && b1950.Enabled, "主選單應啟用 1950 唯一初始開局按鈕");
                Assert(bCp1 != null && !bCp1.Enabled && bCp2 != null && !bCp2.Enabled && bCp3 != null && !bCp3.Enabled,
                    "未解鎖前主選單三個時代紀錄點應維持鎖定狀態 (Enabled == false)");

                // 驗證 1950 開始後本島自動預選 3 師精銳（無各省按鈕洗版），並支援直接拖曳調防海南（免佔運力、自動護航），且計時器自動啟動
                form.StartScenario("1950");
                Application.DoEvents();
                using (var s50Bmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(s50Bmp, new Rectangle(0, 0, form.Width, form.Height));
                    s50Bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_1950_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                Assert(HasLabelWithText(form, "選 3/14"), "1950 開局應預設勾選台灣本島 3 支部隊讓玩家直接在地圖派兵");
                Assert(FindButtonWithText(form, "調防·海南島") == null, "不應替各省逐一建立派兵按鈕，改用共用 2D 地圖目標選取");
                Assert(HasLabelWithText(form, "跨海兩棲籌備"), "左下指戰卡應提示拖曳行軍／兩棲籌備操作");
                int hiUnitsBefore = SimEngine.UnitsAt(form.State, "HI", UnitDomain.Land).Count;
                form.DispatchCheckedUnitsForTest("HI");
                Assert(form.State.Orders.Count == 3
                    && SimEngine.UnitsAt(form.State, "HI", UnitDomain.Land).Count == hiUnitsBefore
                    && SimEngine.LiftUsed(form.State) == 0
                    && SimEngine.UnitsAt(form.State, "Z4", UnitDomain.Sea).Count >= 1,
                    "本島調防海南應派出 3 師在途行軍（不瞬間瞬移）、免佔運力並於南海 Z4 派艦護航");
                for (int d = 0; d < 2; d++) form.StepClockTick();
                Assert(form.State.Orders.Count == 3 && form.State.Orders.All(o => o.ElapsedDays >= 2),
                    "推進 2 天後在途船團應保有可視化行進進度");
                for (int d = 0; d < 6 && form.State.Orders.Count > 0; d++) form.StepClockTick();
                Assert(form.State.Orders.Count == 0 && SimEngine.UnitsAt(form.State, "HI", UnitDomain.Land).Count == hiUnitsBefore + 3,
                    "經歷短天數航渡後 3 師應實際抵達海南島");
                Assert(SimEngine.EstDaysRemaining(form.State, "recon_coast") <= 25,
                    $"戰時科技研發天數應顯著加快（Lv.1 外島雷達應 <=25 天，實際 {SimEngine.EstDaysRemaining(form.State, "recon_coast")} 天）");
                foreach (var u in form.State.Units) u.Moved = false;
                form.State.Orders.Clear();
                form.SelectNode("TW");
                Assert(form.IsSimTimerRunning, "開始 1950 戰局後即時時間計時器應自動流動");
                form.SetSimSpeed(4);
                Assert(form.SimSpeed == 4 && form.IsSimTimerRunning, "切換至 4× 倍速後計時器應保持運作");
                form.TogglePause();
                Assert(form.IsUserPaused && !form.IsSimTimerRunning, "手動暫停後計時器應停止");
                form.TogglePause();
                Assert(!form.IsUserPaused && form.IsSimTimerRunning, "解除暫停後計時器應恢復運作");

                bool gotEventOverlay = false;
                for (int i = 0; i < 120; i++)
                {
                    form.StepClockTick();
                    if (form.State.PendingEvents.Count > 0)
                    {
                        gotEventOverlay = true;
                        break;
                    }
                }
                Assert(gotEventOverlay && !form.IsSimTimerRunning, "出現事件選單時必須自動暫停即時時間流動");
                Assert(!form.IsEasyMode, "預設應為一般模式 (EasyMode == false)");
                Application.DoEvents();
                Assert(!HasControlWithText(form, "二元抉擇") && !HasControlWithText(form, "雙卡戰略對決")
                    && !HasControlWithText(form, "三選一戰略圖卡") && !HasControlWithText(form, "緊急應變處置"),
                    "事件介面不應出現『二元抉擇 / 雙卡戰略對決 / 三選一戰略圖卡 / 緊急應變處置』等字樣");
                var normalBadges = form.GetOpenEventCardBadgesForTest();
                Assert(normalBadges.Count >= 2 && normalBadges.All(b => b == "◇ 決策方針" || b == "◇ 統帥電令")
                    && !HasControlWithText(form, "主力方案") && !HasControlWithText(form, "替代方針")
                    && !HasControlWithText(form, "非常路線") && !HasControlWithText(form, "高風險決策")
                    && !HasControlWithText(form, "（高風險）"),
                    "一般模式下事件圖卡不應出現『主力方案 / 替代方針 / 高風險』等誘導性選擇字樣");

                using (var evBmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(evBmp, new Rectangle(0, 0, form.Width, form.Height));
                    evBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_event_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                // 切換至簡易模式驗證可保留輔助路線提示，且同樣不含二元抉擇等字樣
                form.SetGameMode(true);
                Application.DoEvents();
                var easyBadges = form.GetOpenEventCardBadgesForTest();
                Assert(form.IsEasyMode && easyBadges.Count >= 2
                    && easyBadges.Any(b => b != "◇ 決策方針" && b != "◇ 統帥電令")
                    && !HasControlWithText(form, "二元抉擇"),
                    "簡易模式應顯示輔助決策路線標示，且仍不可出現『二元抉擇』字樣");
                form.SetGameMode(false);
                Application.DoEvents();
                int frozeY = form.State.Year, frozeM = form.State.Month, frozeD = form.State.Day;
                form.StepClockTick();
                Assert(form.State.Year == frozeY && form.State.Month == frozeM && form.State.Day == frozeD, "事件選單開啟期間時間日期不得往前移動");

                while (form.State.PendingEvents.Count > 0) EventSystem.ResolveChoice(form.State, 0);
                form.RefreshAllUi();
                Assert(form.IsSimTimerRunning, "事件選擇完畢後應自動恢復即時時間流動");

                // 驗證科技樹專頁：開啟時時間繼續流動（僅發生事件時才暫停）、五大分支、無一鍵完成鈕、統一按天累計、無冷卻專案趕工(+50%速/+40%費)、底部工作列僅顯示完成通知
                Assert(string.IsNullOrEmpty(form.State.LastTechCompleted), "尚未完成研發前不應出現底部研發完成通知");
                var techDockBtn = FindButtonWithText(form, "科技樹");
                Assert(techDockBtn != null, "底部導航列應包含『科技樹』按鈕");
                techDockBtn.PerformClick();
                Assert(form.IsTechPageOpen && form.IsSimTimerRunning, "開啟科技樹頁面期間時間應繼續流動，不得暫停");
                Assert(FindButtonWithText(form, "直昇") == null && FindButtonWithText(form, "突破") == null, "科技樹不應存在一鍵瞬間完成按鈕");
                var techOpenDate = (form.State.Year, form.State.Month, form.State.Day);
                form.StepClockTick();
                Assert((form.State.Year, form.State.Month, form.State.Day) != techOpenDate || form.State.PendingEvents.Count > 0,
                    "在科技樹頁面且無事件發生期間日期應正常推進");
                while (form.State.PendingEvents.Count > 0) EventSystem.ResolveChoice(form.State, 0);
                form.RefreshAllUi();

                foreach (var brKey in new[] { "science", "arms", "intel", "subv", "logi" })
                {
                    form.SelectTechBranch(brKey);
                    Assert(form.ActiveTechBranch == brKey, $"應可切換科技樹五大分支 ({brKey})");
                }

                // 迴歸測試：於「科技」點擊研發即撥款並直接啟動（無排程等待）、支援多分支同時推進、切換分頁或軍情處不遺失、國庫不足不可點
                form.SelectTechBranch("science");
                double treasBeforeTech = form.State.Treasury;
                double expectedFee1 = SimEngine.TechStartCost(form.State, GameData.Techs["land_reform"]);
                Assert(form.ClickTechNodeButtonForTest("land_reform")
                    && SimEngine.IsResearching(form.State, "land_reform")
                    && form.State.ActiveTechs.SequenceEqual(new[] { "land_reform" })
                    && Math.Abs(form.State.Treasury - (treasBeforeTech - expectedFee1)) < 1e-6,
                    "點擊科技節點啟動研發『耕者有其田』應立即扣除國庫立項經費並直接研發");
                Assert(!form.HasClickableTechButtonForTest("land_reform") && form.GetTechTabButtonTextForTest("science").Contains("★"),
                    "啟動研發後該節點應轉為研發中狀態且分頁顯示 ★ 標記");

                double savedTreasury = form.State.Treasury;
                form.State.Treasury = 0.01;
                form.SelectTechBranch("logi");
                Assert(form.ClickTechNodeButtonForTest("lst_fleet")
                    && !SimEngine.IsResearching(form.State, "lst_fleet")
                    && form.State.ActiveTechs.Count == 1
                    && form.State.ResearchQueue.Count == 0,
                    "國庫不足時點擊科技節點不應啟動或排入研發");
                form.State.Treasury = savedTreasury;

                double treasBeforeLst = form.State.Treasury;
                double feeLst = SimEngine.TechStartCost(form.State, GameData.Techs["lst_fleet"]);
                Assert(form.ClickTechNodeButtonForTest("lst_fleet")
                    && SimEngine.IsResearching(form.State, "land_reform")
                    && SimEngine.IsResearching(form.State, "lst_fleet")
                    && form.State.ResearchQueue.Count == 0
                    && form.GetTechTabButtonTextForTest("logi").Contains("★")
                    && Math.Abs(form.State.Treasury - (treasBeforeLst - feeLst)) < 1e-6,
                    "切換至後勤點擊『中字號登陸艦隊』應扣除經費並直接並行研發（無須排程）");

                form.SelectTechBranch("intel");
                Assert(form.ClickTechNodeButtonForTest("recon_coast")
                    && form.State.ActiveTechs.SequenceEqual(new[] { "land_reform", "lst_fleet", "recon_coast" })
                    && form.State.ResearchQueue.Count == 0,
                    "再切換至情蒐點擊『外島雷達』應直接加入並行研發，不得洗掉或卡在排程中");

                form.SelectTechBranch("science");
                Assert(SimEngine.IsResearching(form.State, "land_reform") && !form.HasClickableTechButtonForTest("land_reform"),
                    "切回『科技』分頁後『耕者有其田』應仍維持研發中，不得變回未研發按鈕");

                // 離開科技樹去其他專頁（軍情處）再返回，進行中的並行研發須完整保存
                form.CloseTechPage();
                form.OpenIntelPage();
                Assert(form.IsIntelPageOpen, "應可開啟軍情處專頁");
                form.OpenTechPage("science");
                Assert(form.State.ActiveTechs.SequenceEqual(new[] { "land_reform", "lst_fleet", "recon_coast" }),
                    "切換至軍情處等專頁後再切回科技樹，所有已撥款研發項目應完整保存");

                // 驗證專案趕工增加 +50% 研發速度與 +40% 科研成本
                double normalRp = SimEngine.RpPerTurn(form.State);
                double normalCost = SimEngine.ResCost(form.State);
                int normalDays = SimEngine.EstDaysRemaining(form.State, "recon_coast");

                form.ToggleResearchRush(); // ON（無冷卻）
                Assert(form.State.ResearchRush && Math.Abs(SimEngine.RpPerTurn(form.State) - normalRp * 1.50) < 1e-6
                    && Math.Abs(SimEngine.ResCost(form.State) - normalCost * 1.40) < 1e-6,
                    "開啟專案趕工後應無冷卻即時生效：研發速度 +50%、科研成本 +40%");
                Assert(SimEngine.EstDaysRemaining(form.State, "recon_coast") < normalDays, "開啟專案趕工後預估剩餘天數應明顯縮短");
                form.ToggleResearchRush(); // OFF（無冷卻立即關回）
                Assert(!form.State.ResearchRush, "專案趕工應可無冷卻自由關閉");
                form.ToggleResearchRush(); // 再 ON

                form.CloseTechPage();
                Assert(!form.IsTechPageOpen && form.IsSimTimerRunning, "離開科技樹頁面後時間應持續流動");

                // 逐日推進：驗證三項同時按天推進並於短天數內陸續完成、觸發底部完成通知
                double p0 = SimEngine.SavedResearchProg(form.State, "land_reform");
                double pCoast0 = SimEngine.SavedResearchProg(form.State, "recon_coast");
                form.StepClockTick();
                if (form.State.PendingEvents.Count == 0)
                    Assert(SimEngine.SavedResearchProg(form.State, "land_reform") > p0
                        && SimEngine.SavedResearchProg(form.State, "recon_coast") > pCoast0,
                        "統一按天累計：推進 1 日後多個並行專案進度皆應即時增加");

                double fogBefore = SimEngine.FogError(form.State);
                for (int d = 0; d < 40 && (!form.State.Techs.Contains("recon_coast") || !form.State.Techs.Contains("land_reform")); d++)
                {
                    while (form.State.PendingEvents.Count > 0) EventSystem.ResolveChoice(form.State, 0);
                    form.RefreshAllUi();
                    form.StepClockTick();
                }
                while (form.State.PendingEvents.Count > 0) EventSystem.ResolveChoice(form.State, 0);
                form.RefreshAllUi();
                Assert(form.State.Techs.Contains("recon_coast") && SimEngine.FogError(form.State) < fogBefore,
                    "『外島雷達與偵巡網』應於短天數內按天累計完成並自動套用迷霧降低效果");
                Assert(form.State.Techs.Contains("land_reform"),
                    "『耕者有其田』應隨並行研發於短天數內同步完成（無排程等待）");
                Assert(!string.IsNullOrEmpty(form.FrontNoticeTextForTest), "時間推進後頂欄應跳出戰地電訊");
                Assert(!string.IsNullOrEmpty(form.State.LastTechCompleted) && HasLabelWithText(form, form.State.LastTechCompleted),
                    "科技完成後底部工作列應顯示已完成通知");
                form.SetSimSpeed(1);

                // 模擬由 1950 推進達標並解鎖紀錄點 I，截圖驗證主選單呈現效果並測試從同個紀錄點開始
                form.State.Turn = 12;
                form.State.Year = 1962;
                form.State.Gdp = 29;
                form.State.Control["FJ"] = Faction.ROC;
                form.State.Garrison["FJ"] = 0;
                form.CheckAndRecordCheckpoints();
                form.RefreshAllUi();
                Assert(bCp1.Enabled && !bCp2.Enabled, "達標後紀錄點 I 應解鎖啟用，紀錄點 II 仍鎖定");

                FindButtonWithText(form, "選單")?.PerformClick();
                Application.DoEvents();
                using (var titleBmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(titleBmp, new Rectangle(0, 0, form.Width, form.Height));
                    titleBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_title_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                var focusTestButton = FindButtonWithText(form, "返回當前戰局");
                Assert(focusTestButton != null && focusTestButton.Focus(), "主選單按鈕應可取得鍵盤焦點");
                Application.DoEvents();
                CaptureUiSnapshot(form, "ui_keyboard_focus_snapshot.png");

                // 實測從解鎖的紀錄點開局，應完美還原同一份 1962 且已光復閩南的戰局狀態
                bool resumedCp = form.StartFromCheckpoint("cp_1962");
                Assert(resumedCp && form.State.Year == 1962 && form.State.Control["FJ"] == Faction.ROC, "從紀錄點開局應完整還原該局紀錄狀態");

                // 接著檢視 1967 戰情地圖與遊戲圖卡渲染，以及科技樹五分支全圖頁面截圖
                form.StartScenario("1967");
                while (form.State.PendingEvents.Count > 0) EventSystem.ResolveChoice(form.State, 0);
                SimEngine.SeizeByUssr(form.State, "HLJ", 7.5);
                form.State.Control["FJ"] = Faction.ROC;
                form.State.Garrison["FJ"] = 0;
                SimEngine.AddUnit(form.State, "mar", "FJ", 92);
                SimEngine.AddUnit(form.State, "mil", "FJ", 85);
                Assert(FindButtonWithText(form, "1師") != null && FindButtonWithText(form, "3師") != null && FindButtonWithText(form, "半數") != null,
                    "左下指戰卡應具備 1師 / 3師 / 半數 一鍵挑兵按鈕");
                form.SelectNode("TW");
                form.SelectUnitsCount(3);
                Assert(HasLabelWithText(form, "選 3/"), "點選 3師 快捷鈕後應準確選取 3 支部隊");
                // 建立一組在途陸軍行軍與一組兩棲登陸籌備以捕捉實戰行軍圖示截圖
                form.State.Control["GD"] = Faction.ROC;
                form.State.Garrison["GD"] = 0;
                SimEngine.AddUnit(form.State, "arm", "GD", 98);
                SimEngine.AddUnit(form.State, "inf", "GD", 96);
                var gdUnits = SimEngine.UnitsAt(form.State, "GD", UnitDomain.Land).Take(2).Select(u => u.Id).ToList();
                SimEngine.MoveNow(form.State, gdUnits, "FJ");
                SimEngine.StepOrders(form.State);
                SimEngine.StepOrders(form.State);
                var twPrepUnits = SimEngine.UnitsAt(form.State, "TW", UnitDomain.Land).Take(2).Select(u => u.Id).ToList();
                form.State.Treaty = false;
                SimEngine.AttackNow(form.State, twPrepUnits, "ZJ");
                SimEngine.StepOrders(form.State);
                SimEngine.StepOrders(form.State);

                form.SelectNode("FJ");
                form.RefreshAllUi();
                Application.DoEvents();
                Assert(form.MapPanelClientSize.Height == form.ClientSize.Height - form.HudBarHeight - form.DockBarHeight,
                    $"地圖畫布高度應扣除頂欄({form.HudBarHeight})與底部導航列({form.DockBarHeight})，避免重疊 (mapH={form.MapPanelClientSize.Height}, clientH={form.ClientSize.Height})");

                using (var bmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                    bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                form.State.Orders.Clear();
                foreach (var u in form.State.Units) u.Moved = false;

                // 驗證地圖滑鼠滾輪以游標為錨點放大縮小與重置
                Assert(Math.Abs(form.MapZoom - 1.0f) < 1e-4, "初始地圖縮放倍率應為 1.0×");
                float uiScaleBefore = form.CurrentMapUiScaleForTest;
                var mnBefore = form.GetNodeScreenPointForTest("FJ");
                var anchor = new Point((int)Math.Round(mnBefore.X), (int)Math.Round(mnBefore.Y));
                form.ZoomMapAt(+120, anchor);
                form.ZoomMapAt(+120, anchor);
                form.ZoomMapAt(+120, anchor);
                Assert(form.MapZoom > 1.5f, $"滾輪向上放大後地圖倍率應大於 1.5×，實際 {form.MapZoom:F2}×");
                Assert(Math.Abs(form.CurrentMapUiScaleForTest - uiScaleBefore) < 1e-4,
                    $"放大地圖後地圖文字與兵牌應維持螢幕固定清晰比例，不隨倍率等比放大 ({uiScaleBefore:F3} vs {form.CurrentMapUiScaleForTest:F3})");
                var mnAfter = form.GetNodeScreenPointForTest("FJ");
                Assert(Math.Abs(mnAfter.X - anchor.X) < 3.5f && Math.Abs(mnAfter.Y - anchor.Y) < 3.5f,
                    $"游標錨定縮放後閩南節點應仍貼近游標位置 (delta={Math.Abs(mnAfter.X - anchor.X):F1},{Math.Abs(mnAfter.Y - anchor.Y):F1})");
                Application.DoEvents();
                using (var zBmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(zBmp, new Rectangle(0, 0, form.Width, form.Height));
                    zBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_zoom_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                form.ResetMapZoom();
                Assert(Math.Abs(form.MapZoom - 1.0f) < 1e-4 && form.MapPanX == 0f && form.MapPanY == 0f, "重設縮放後應精確歸位至 1.0×");

                // 切換進入科技樹頁面（驗證 1967 下五大分支、六邊形向量圖標樹與懸停浮動詳情卡）
                Assert(SimEngine.SetResearch(form.State, "nuke_program"), "1967 應可啟動新竹計畫");
                form.State.ResearchProg = 10.5;
                form.State.ResearchRush = true;
                form.OpenTechPage("science");
                form.SetHoverTechForTest("nuke_program");
                Application.DoEvents();
                Assert(form.IsTechPageOpen && form.IsSimTimerRunning, "1967 開啟科技樹頁面且無待決事件時，時間應繼續流動");
                using (var techSciBmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(techSciBmp, new Rectangle(0, 0, form.Width, form.Height));
                    techSciBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_tech_science_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                form.SelectTechBranch("arms");
                form.SetHoverTechForTest("armor");
                Application.DoEvents();
                using (var techBmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(techBmp, new Rectangle(0, 0, form.Width, form.Height));
                    techBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_tech_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                // 驗證核心規則：查看科技樹期間時間續流，一旦發生事件立即自動暫停並彈出事件卡；解決事件後時間於科技樹內自動恢復
                var testEv = EventSystem.HistoricalEvents.First(e => e.Id == "cultural_rev_1966");
                form.State.PendingEvents.Add(testEv);
                form.RefreshAllUi();
                Application.DoEvents();
                Assert(form.IsTechPageOpen && !form.IsSimTimerRunning && form.IsEventOverlayVisibleForTest,
                    "於科技樹頁面期間發生事件時應立即暫停時間並彈出事件視窗");
                var evFrozeDate = (form.State.Year, form.State.Month, form.State.Day);
                form.StepClockTick();
                Assert((form.State.Year, form.State.Month, form.State.Day) == evFrozeDate,
                    "科技樹期間發生待決事件未解決前日期不得前進");
                form.State.PendingEvents.Clear();
                form.RefreshAllUi();
                Application.DoEvents();
                Assert(form.IsTechPageOpen && !form.IsEventOverlayVisibleForTest && form.IsSimTimerRunning,
                    "事件處置完畢後，停留科技樹頁面應自動恢復時間流動");
                form.CloseTechPage();

                // 驗證軍情處全版面專頁與動態情蒐紀錄（「解放軍似乎正在密謀...」）＋ 開啟時必須自動暫停時間
                var intelBtn = FindButtonWithText(form, "軍情處");
                Assert(intelBtn != null, "底部導航列應包含『軍情處』按鈕");
                intelBtn.PerformClick();
                Application.DoEvents();
                Assert(form.IsIntelPageOpen && !form.IsSimTimerRunning, "點擊『軍情處』開啟全版面專頁時必須自動暫停即時時間");
                int iFrozeY = form.State.Year, iFrozeM = form.State.Month, iFrozeD = form.State.Day;
                form.StepClockTick();
                Assert(form.State.Year == iFrozeY && form.State.Month == iFrozeM && form.State.Day == iFrozeD,
                    "在軍情處頁面期間日期時間不得往前推進");
                var mapSz = form.MapPanelClientSize;
                var intelBounds = form.IntelOverlayBounds;
                Assert(intelBounds.Width >= mapSz.Width * 0.95 && intelBounds.Height >= mapSz.Height * 0.95,
                    $"軍情處專頁必須為全版面顯示 (overlay={intelBounds.Width}x{intelBounds.Height}, map={mapSz.Width}x{mapSz.Height})");
                Assert(form.State.IntelReports.Count >= 2 && form.State.IntelReports.Any(r => r.Text.Contains("密謀")),
                    "軍情處應自動收錄含『密謀』徵候的情蒐動態紀錄");
                int cntBeforeIntelAction = form.State.IntelReports.Count;
                SimEngine.AddIntel(form.State, "密謀警示", "alert", "閩粵沿海多處漁港徵調機動登陸艇並實施無線電靜默，解放軍似乎正在密謀新一波突擊行動……");
                form.RefreshAllUi();
                Assert(form.State.IntelReports.Count == cntBeforeIntelAction + 1, "新增情蒐動態應即時納入軍情處列表");
                foreach (var fKey in new[] { "plot", "move", "subv", "all" })
                {
                    form.SelectIntelFilter(fKey);
                    Assert(form.ActiveIntelFilter == fKey, $"軍情處應可切換篩選分類 ({fKey})");
                }
                Application.DoEvents();
                using (var intelBmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(intelBmp, new Rectangle(0, 0, form.Width, form.Height));
                    intelBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_intel_page_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                form.CloseIntelPage();
                Assert(!form.IsIntelPageOpen && form.IsSimTimerRunning, "關閉軍情處專頁返回戰略地圖後應自動恢復即時時間流動");

                var evUsBreak = EventSystem.HistoricalEvents.First(e => e.Id == "us_break_1979");
                var ev817 = EventSystem.HistoricalEvents.First(e => e.Id == "comm817_1982");
                var evCr66 = EventSystem.HistoricalEvents.First(e => e.Id == "cultural_rev_1966");
                var evDachen55 = EventSystem.HistoricalEvents.First(e => e.Id == "dachen_1955");
                Assert(evUsBreak.Choices.Count == 1 && ev817.Choices.Count == 1, "不可抗外交衝擊事件必須保留單一通知處置選項");
                Assert(evCr66.Choices.Count == 3, "重大歷史事件(1966文革)應具備 3 選 1 戰略路線");
                Assert(evDachen55.Choices.Count == 2, "1955大陳事件應具備雙選項路線");

                form.SetGameMode(false);
                form.State.PendingEvents.Add(evDachen55);
                form.RefreshAllUi();
                Application.DoEvents();
                Assert(!HasControlWithText(form, "二元抉擇") && !HasControlWithText(form, "雙卡戰略對決")
                    && !HasControlWithText(form, "主力方案") && !HasControlWithText(form, "替代方針"),
                    "雙選項事件於一般模式下不可出現『二元抉擇 / 主力方案 / 替代方針』等字樣");
                using (var dachenBmp = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(dachenBmp, new Rectangle(0, 0, form.Width, form.Height));
                    dachenBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_dachen_event_snapshot.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                form.State.PendingEvents.Clear();
                form.RefreshAllUi();

                form.Close();
            }
            finally
            {
                if (cpBackup != null) File.WriteAllText(cpFile, cpBackup); else if (File.Exists(cpFile)) File.Delete(cpFile);
                if (endBackup != null) File.WriteAllText(endFile, endBackup); else if (File.Exists(endFile)) File.Delete(endFile);
            }
            LogLine("[PASS] 5. 存讀檔、海域直調、勝算預估、1950 唯一開局＋紀錄點鎖定/解鎖與 UI 渲染全數通過");

            // 5B. 驗證 1950 死守與增兵海南的四重困境（粵桂殘部與瓊崖縱隊內應、海口封鎖逆登陸運力×1.5、遠海補給瓶頸、瓊州海峽四野持續壓境與抽空台灣危機）
            {
                var sHi = SimEngine.NewGame(1950u);
                sHi.PendingEvents.Clear();
                var hiInit = SimEngine.UnitsAt(sHi, "HI", UnitDomain.Land);
                Assert(sHi.HasFlag("hiQiongya") && hiInit.Count == 2 && hiInit.All(u => u.Str <= 68.0),
                    "1950 開局海南應呈現粵桂殘部缺編與瓊崖縱隊內應威脅");

                var evZs = EventSystem.HistoricalEvents.First(e => e.Id == "zhoushan_1950");
                sHi.Year = 1950; sHi.Month = 4; sHi.Day = 10;
                sHi.PendingEvents.Add(evZs);
                EventSystem.ResolveChoice(sHi, 2); // 選擇死守海南與舟山
                Assert(sHi.HasFlag("holdHainan") && sHi.HasFlag("hiPortBlocked") && sHi.Garrison["HI"] >= 4.0,
                    "選擇死守海南後應觸發海口港封鎖與四野強渡瓊州海峽重兵進逼");

                var twInf = SimEngine.UnitsAt(sHi, "TW", UnitDomain.Land).First(u => u.Type == "inf");
                var mvHi = SimEngine.CheckMoveDetailed(sHi, twInf, "HI");
                Assert(mvHi.ok && mvHi.needsPrep && mvHi.lift >= GameData.UnitTypes[twInf.Type].Lift * 1.49,
                    $"海口封鎖／交戰下增援海南應強制兩棲籌備並付出 1.5× 遠距逆登陸運力，實際 needsPrep={mvHi.needsPrep}, lift={mvHi.lift:F2}");

                // 模擬 5 師自台灣本島「添油」調往外島導致本島空虛（<5.8 師），同時測試遠海補給受挫、瓊州海峽續增與 1950 台海急報
                var drained = SimEngine.UnitsAt(sHi, "TW", UnitDomain.Land).Take(8).ToList();
                foreach (var u in drained) u.Loc = "HI";
                sHi.Orders.Clear();
                sHi.Month = 5;
                sHi.Supply = 4.0;
                SimEngine.ResolveSupply(sHi, 1.0 / 12.0);
                var hiPostSup = SimEngine.UnitsAt(sHi, "HI", UnitDomain.Land);
                Assert(hiPostSup.Count > 0 && hiPostSup.Average(u => u.SupFactor) < 0.85,
                    "南海未具壓倒性制海下，海南重兵與遭封鎖海口應遭受遠距補給惡化 (SupFactor < 0.85)");
                double hiEnemyBeforeTick = sHi.Garrison["HI"];
                SimEngine.MonthlyTick(sHi, stepResearch: false, resolveOrders: false);
                Assert(sHi.HasFlag("twEmptyWarned") && sHi.Logs.Any(l => l.Msg.Contains("本島極危")),
                    "1950 韓戰前夕把本島主力抽空至 <5.8 師時，應立即觸發三野渡海犯台極危警訊");
                Assert(sHi.Logs.Any(l => l.Msg.Contains("瓊州海峽")) || sHi.Control["HI"] == Faction.PRC || sHi.Garrison["HI"] > 0,
                    $"海南戰線應感受四野背靠華南透過瓊州海峽持續增兵壓境（tick 前 {hiEnemyBeforeTick:F1}）");
            }
            LogLine("[PASS] 5B. 1950 死守與增兵海南四重困境機制（運力×1.5、遠距補保、瓊崖內應、四野跨海續增、本島抽空危急）全數通過");

            // 5C. 驗證 1950 韓戰（6/25）與抗美援朝/MAAG 史實時點準時觸發，及多元動態事件輪替不單調
            {
                var sKw = SimEngine.NewGame(1950u);
                sKw.PendingEvents.Clear();
                sKw.Year = 1950; sKw.Month = 6; sKw.Day = 24;
                sKw.FiredEvents.Add("zhoushan_1950");
                Assert(SimEngine.AdvanceDay(sKw), "1950/06/24 推進至 1950/06/25 應成功推進日曆");
                Assert(sKw.PendingEvents.Any(e => e.Id == "korean_war_1950"),
                    $"韓戰爆發事件應於 1950/06/25 準時觸發，實際 Pending={string.Join(",", sKw.PendingEvents.Select(e => e.Id))}");
                EventSystem.ResolveChoice(sKw, 0);
                Assert(sKw.UsProtect && sKw.UsAid >= 0.7, "韓戰爆發後應隨即開啟第七艦隊協防與美援");

                sKw.Month = 10; sKw.Day = 25;
                EventSystem.CheckHistoricalForDate(sKw);
                Assert(sKw.PendingEvents.Any(e => e.Id == "pva_korea_1950"), "1950/10/25 應觸發中共「抗美援朝」重兵北調事件");
                sKw.PendingEvents.Clear();

                // 連續推進 36 個月動態檢定（維持正常冷卻機制），確認輪替產生多元主題與非軍事類別，不會卡在單一重複事件
                var sVar = SimEngine.NewGame(2026u);
                sVar.PendingEvents.Clear();
                var seenTitles = new HashSet<string>();
                var seenTags = new HashSet<string>();
                for (int m = 2; m < 38; m++)
                {
                    sVar.Year = 1950 + m / 12;
                    sVar.Month = (m % 12) + 1;
                    sVar.Day = 10;
                    sVar.PendingEvents.Clear();
                    EventSystem.CheckDynamic(sVar);
                    if (sVar.PendingEvents.Count > 0)
                    {
                        var dev = sVar.PendingEvents[0];
                        seenTitles.Add(dev.Title);
                        seenTags.Add(dev.Tag);
                        Assert(!dev.Title.Contains("史實") && !dev.Title.Contains("架空"), $"動態事件不得包含禁止字詞：{dev.Title}");
                        EventSystem.ResolveChoice(sVar, 0);
                        Assert(sVar.Over == null, $"多元動態事件 {dev.Id} 不得於戰中提早結束遊戲");
                    }
                }
                Assert(seenTitles.Count >= 7 && seenTags.Count >= 4,
                    $"36 個月內動態事件應涵蓋多種主題與領域以免單調（實際 {seenTitles.Count} 種標題、{seenTags.Count} 種標籤：{string.Join("、", seenTags)}）");
            }
            LogLine("[PASS] 5C. 1950/06/25 韓戰準時觸發與多元主題動態事件輪替（非單一軍事重複）驗證通過");

            // 5D. 驗證仿美戰爭公債（War Bonds）嚴格錨定 GDP 18%、自動發債/償債/經貿、無外交抽屜與 63 項科研分支
            {
                var sBond = SimEngine.NewGame(2026u);
                double bAmt = SimEngine.BondAmount(sBond);
                Assert(Math.Abs(bAmt - sBond.Gdp * SimEngine.WarBondGdpPct) < 1e-6,
                    $"仿美 War Bonds 發行額度必須嚴格等於 GDP 的 18%（實際 ${bAmt:F2} 億 vs GDP 18% ${sBond.Gdp * SimEngine.WarBondGdpPct:F2} 億）");
                Assert(SimEngine.IsAutoActionEnabled(sBond, "bonds") && SimEngine.IsAutoActionEnabled(sBond, "jp_trade"),
                    "新局應預設啟用『發行國防戰爭公債（War Bonds）』與『對日經貿合作』自動執行");

                // 模擬國庫偏低跨月時自動發行 War Bonds 與經貿續行
                sBond.Treasury = 0.18;
                double jpBefore = sBond.JpRel;
                double debtBefore = sBond.Debt;
                double moraleBefore = sBond.Morale;
                sBond.Month = 1; sBond.Day = 31;
                SimEngine.AdvanceDay(sBond); // 跨入 2/1 -> MonthlyTick 觸發 StepAutoActions
                Assert(sBond.Debt >= debtBefore + bAmt - 1e-6 && sBond.Treasury >= 2.0,
                    $"國庫偏低跨月應自動發行 War Bonds +${bAmt:F2} 億，實際國庫 ${sBond.Treasury:F2} 億 / 債務 ${sBond.Debt:F2} 億");
                Assert(sBond.Morale >= moraleBefore + 0.9, "發行國防戰爭公債應提振戰時士氣（民心 +1）");
                Assert(sBond.JpRel >= jpBefore + 4.9, "跨月國庫充裕時應自動執行對日經貿合作提升對日關係");
                Assert(sBond.Logs.Any(l => l.Msg.StartsWith("[自動]") && l.Msg.Contains("War Bonds")), "自動發債應記入仿美 War Bonds 戰報日誌");

                // 測試國庫充裕時開啟自動償還公債
                sBond.Treasury = 15.0;
                double dBeforeRepay = sBond.Debt;
                SimEngine.ToggleAutoAction(sBond, "repay");
                Assert(SimEngine.IsAutoActionEnabled(sBond, "repay") && sBond.Debt < dBeforeRepay,
                    "國庫充裕並開啟自動償還公債時應立即自動減債");

                // 測試新科技效果與 UI 抽屜/擴充後科技樹截圖
                var sTechNew = SimEngine.NewGame(2027u);
                sTechNew.Year = 1970;
                SimEngine.GrantTech(sTechNew, "se_asia_listen");
                Assert(SimEngine.HasIntel(sTechNew, "SC") && SimEngine.HasIntel(sTechNew, "YN"), "西南邊境聽音站應提供四川與雲南常駐精確情報");
                double upBeforeLogi = SimEngine.Upkeep(sTechNew);
                SimEngine.GrantTech(sTechNew, "high_speed_logi");
                Assert(SimEngine.Upkeep(sTechNew) < upBeforeLogi * 0.95, "戰略空運與智能補保應實質降低軍費維持負擔");

                using var form5d = new MainForm();
                form5d.Show();
                form5d.StartScenario("1950");
                Application.DoEvents();
                if (!form5d.IsUserPaused) form5d.TogglePause();
                var dipBtn = FindButtonWithText(form5d, "外交");
                Assert(dipBtn == null, "底部導航列應已完全移除外交政策抽屜按鈕");
                Assert(!HasButtonWithText(form5d, "外交心戰") && !HasControlWithText(form5d, "外交戰略行動"),
                    "主介面不應存在任何外交政策頁面或抽屜元件");
                form5d.ShowHudTooltipForTest("treasury");
                Application.DoEvents();
                Assert(HasControlWithText(form5d, "War Bonds") && HasControlWithText(form5d, "佔 GDP 18%"),
                    "國庫 Tooltip 應明確標明仿美 War Bonds 且單期籌資額嚴格佔 GDP 18%");
                using (var dockBmp = new Bitmap(form5d.Width, form5d.Height))
                {
                    form5d.DrawToBitmap(dockBmp, new Rectangle(0, 0, form5d.Width, form5d.Height));
                    dockBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_dock_snapshot.png"));
                }
                form5d.ShowHudTooltipForTest(null);
                foreach (var drawer in new[]
                {
                    (ButtonText: "建軍後勤", FileName: "ui_build_drawer_snapshot.png"),
                    (ButtonText: "兩岸國力", FileName: "ui_chart_drawer_snapshot.png")
                })
                {
                    var drawerButton = FindButtonWithText(form5d, drawer.ButtonText);
                    Assert(drawerButton != null, $"底部應提供 {drawer.ButtonText} 頁面入口");
                    drawerButton.PerformClick();
                    Application.DoEvents();
                    CaptureUiSnapshot(form5d, drawer.FileName);
                    drawerButton.PerformClick();
                }
                form5d.OpenTechPage("intel");
                Application.DoEvents();
                using (var intelTechBmp = new Bitmap(form5d.Width, form5d.Height))
                {
                    form5d.DrawToBitmap(intelTechBmp, new Rectangle(0, 0, form5d.Width, form5d.Height));
                    intelTechBmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui_tech_intel_snapshot.png"));
                }
                form5d.CloseTechPage();
                form5d.Size = new Size(1280, 760);
                Application.DoEvents();
                var menuButton = FindButtonWithText(form5d, "選單");
                Assert(menuButton != null, "頂部 HUD 應提供返回主選單入口");
                menuButton.PerformClick();
                Application.DoEvents();
                CaptureUiSnapshot(form5d, "ui_title_min_snapshot.png");
                form5d.StartScenario("1950");
                if (!form5d.IsUserPaused) form5d.TogglePause();
                Application.DoEvents();
                CaptureUiSnapshot(form5d, "ui_game_min_snapshot.png");
                form5d.State.Over = new GameOverInfo
                {
                    Key = "ui_preview",
                    Group = "完全收復",
                    Kind = "win",
                    Title = "光復大陸・還我河山",
                    Text = "國軍光復全境，戰役目標達成。"
                };
                form5d.RefreshAllUi();
                Application.DoEvents();
                CaptureUiSnapshot(form5d, "ui_ending_snapshot.png");
                form5d.Close();
            }
            LogLine("[PASS] 5D. 公債單期額度提升(18% GDP)、自動發債/償債/經貿與 63 項科研分支 UI 驗證通過");

            // 5E. S1–S5 態勢感知、破綻與戰機事件、全國比例折損驗證
            {
                // S1 & S3: 海空暴露偵測 + 增援嚴守 CheckMove 不作弊傳送
                var s5e = SimEngine.NewGame(2001u);
                s5e.Month = 3; // TotalMonths = 2
                SimEngine.AssessPosture(s5e);
                Assert(!string.IsNullOrEmpty(s5e.ExposedNodeId) && s5e.FrontHeadlineKind == "threat",
                    "1950 初期南海 Z4 無海空掩護時應偵測到海空暴露態勢");

                foreach (var u in SimEngine.UnitsAt(s5e, "TW", UnitDomain.Land)) u.Moved = true;
                s5e.PendingEvents.Clear();
                EventSystem.CheckDynamic(s5e);
                Assert(s5e.PendingEvents.Count > 0 && s5e.PendingEvents[0].Id.StartsWith("dyn_posture_exp_"),
                    "暴露態勢應觸發 dyn_posture_exp 動態事件");
                int ordersBefore = s5e.Orders.Count;
                EventSystem.ResolveChoice(s5e, 0);
                Assert(s5e.Orders.Count == ordersBefore, "後方無可 CheckMove 部隊時暴露增援不應作弊瞬移");

                // S1 & S4: 戰略窗口偵測 + 百分比折損與 2 個月登陸窗口加成
                var sWin = SimEngine.NewGame(2003u);
                sWin.Month = 3;
                foreach (var z in GameData.ZoneIds)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        SimEngine.AddUnit(sWin, "fleet", z, 100);
                        SimEngine.AddUnit(sWin, "air", z, 100);
                    }
                }
                SimEngine.AssessPosture(sWin);
                Assert(string.IsNullOrEmpty(sWin.ExposedNodeId) && !string.IsNullOrEmpty(sWin.WindowTargetId) && sWin.FrontHeadlineKind == "window",
                    "全海域具備海空優勢時應識別戰略窗口與目標節點");
                string winZ = sWin.WindowZoneId;
                string winTgt = sWin.WindowTargetId;
                sWin.Garrison[winTgt] = 20.0;
                double gx0 = sWin.Garrison["GX"] = 4.0;
                EventSystem.ChipHeld(sWin, new[] { "GX" }, 0.10);
                Assert(Math.Abs((gx0 - sWin.Garrison["GX"]) - 0.40) < 1e-6, "ChipHeld 應依現有兵力等比例削減 10%");
                sWin.PendingEvents.Clear();
                EventSystem.CheckDynamic(sWin);
                Assert(sWin.PendingEvents.Count > 0 && sWin.PendingEvents[0].Id.StartsWith("dyn_posture_win_"),
                    "窗口態勢應觸發 dyn_posture_win 戰機事件");
                EventSystem.ResolveChoice(sWin, 0);
                Assert(Math.Abs(sWin.Garrison[winTgt] - 18.0) < 1e-6, "破襲應對 20 師守軍造成精確 10%（2.0 師）折損");
                Assert(sWin.GetFlag("winBonus_" + winZ) >= sWin.TotalMonths + 2, "破襲後應開啟 2 個月登陸窗口加成旗標");

                // S5: 全國性穩定度衝擊 ShockPrc 依剩餘省分比例縮放
                var sFull = SimEngine.NewGame(2004u);
                var sSmall = SimEngine.NewGame(2004u);
                foreach (var id in GameData.MainlandIds.Skip(2)) sSmall.Control[id] = Faction.ROC;
                sFull.PrcStab = 60.0;
                sSmall.PrcStab = 60.0;
                EventSystem.ShockPrc(sFull, 15.0);
                EventSystem.ShockPrc(sSmall, 15.0);
                double dropFull = 60.0 - sFull.PrcStab;
                double dropSmall = 60.0 - sSmall.PrcStab;
                Assert(dropFull > dropSmall * 2.0 && sSmall.PrcStab > 50.0,
                    $"ShockPrc 應依大陸剩餘省分比例縮放（26省降 {dropFull:F2}，2省降 {dropSmall:F2}）");
            }
            LogLine("[PASS] 5E. S1–S5 戰線態勢感知、破綻/戰機事件與比例折損驗證通過");

            // 5F. 系統 A–J（季節、國策、光復治理、參謀四派、作戰計畫、地下網、敵學說、變數、回憶錄、16結局）驗證
            {
                // B. 季節與北疆嚴冬
                Assert(SimEngine.GetSeasonPrepDelta(8, "Z3") == 3 && SimEngine.GetSeasonPrepDelta(5, "Z3") == -1,
                    "颱風季應 +3 天、順風季應 -1 天登陸籌備");
                var sB = SimEngine.NewGame(3001u);
                sB.Control["HLJ"] = Faction.ROC;
                sB.Garrison["HLJ"] = 0;
                foreach (var gid in GameData.MainlandIds) sB.Garrison[gid] = 0;
                var coldU = SimEngine.AddUnit(sB, "inf", "HLJ", 100);
                sB.Year = 1952; sB.Month = 12; sB.Day = 31;
                SimEngine.AdvanceDay(sB);
                Assert(coldU.Str < 100.0, "12 月嚴冬應對北疆駐軍造成低溫損耗");

                // A. 時代國策與期滿承諾（自動觸發於 1950/03/01，五年後 YearlyTick 結算並記入回憶錄）
                var sA = SimEngine.NewGame(3002u);
                for (int d = 0; d < 90 && (sA.Year == 1950 && sA.Month < 3); d++)
                {
                    if (sA.PendingEvents.Count > 0) EventSystem.ResolveChoice(sA, 0);
                    SimEngine.AdvanceDay(sA);
                }
                if (sA.Year == 1950 && sA.Month == 3 && sA.Day == 1 && !sA.FiredEvents.Contains("era_dir_1950"))
                    EventSystem.CheckEvents(sA);
                Assert(sA.PendingEvents.Any(e => e.Id.StartsWith("era_dir_")), "1950/03/01 應觸發時代國策選擇");
                while (sA.PendingEvents.Count > 0 && !sA.PendingEvents[0].Id.StartsWith("era_dir_"))
                    EventSystem.ResolveChoice(sA, 0);
                EventSystem.ResolveChoice(sA, 0);
                Assert(!string.IsNullOrEmpty(sA.ActiveDirectiveId) && sA.Memoir.Count > 0, "選定國策後應記錄啟動與回憶錄");
                sA.Year = sA.DirectiveEndYear; sA.Month = 12; sA.Day = 31;
                int memBefore = sA.Memoir.Count;
                SimEngine.AdvanceDay(sA);
                Assert(string.IsNullOrEmpty(sA.ActiveDirectiveId) && sA.Memoir.Count > memBefore,
                    "五年期滿跨年後應結算時代國策承諾並追加回憶錄");

                // D. 光復區治理方針：Major 彈窗，一般省分自動沿用
                var sD = SimEngine.NewGame(3003u);
                sD.PendingEvents.Clear();
                sD.Control["GD"] = Faction.ROC;
                EventSystem.OnCapture(sD, "GD");
                Assert(sD.PendingEvents.Any(e => e.Id == "lib_policy_GD"), "收復廣東重鎮應觸發三選一治理方針");
                EventSystem.ResolveChoice(sD, 1); // 民政撫輯 civil
                Assert(sD.LastLiberationPolicy == "civil" && sD.ProvincePolicy.GetValueOrDefault("GD") == "civil",
                    "選擇民政撫輯應記錄廣東與預設治理方針");
                sD.Control["GX"] = Faction.ROC;
                EventSystem.OnCapture(sD, "GX");
                Assert(!sD.PendingEvents.Any(e => e.Id == "lib_policy_GX") && sD.ProvincePolicy.GetValueOrDefault("GX") == "civil",
                    "非 Major 省分應自動沿用上次光復治理方針不另彈窗");

                // C. 參謀四派影響力、主導與 24 個月反彈
                var sC = SimEngine.NewGame(3004u);
                for (int k = 0; k < 5; k++)
                    SimEngine.ApplyChoiceFactionShift(sC, new EventChoice { FactionTag = "intel" });
                Assert(SimEngine.DominantStaffFaction(sC) == "intel", "連續偏向情報選項後情報派應為主導派系");
                sC.Month = 3;
                foreach (var z in GameData.ZoneIds) { SimEngine.AddUnit(sC, "fleet", z, 100); SimEngine.AddUnit(sC, "air", z, 100); }
                sC.FactionLowMonths["econ"] = 24;
                sC.PendingEvents.Clear();
                string thC = EventSystem.AnalyzeFrontline(sC).Theater.ToString();
                sC.Cooldowns[$"posture_exp_{thC}"] = 999;
                sC.Cooldowns[$"posture_win_{thC}"] = 999;
                EventSystem.CheckDynamic(sC);
                Assert(sC.PendingEvents.Any(e => e.Id.StartsWith("dyn_faction_backlash_econ_")),
                    "派系遭冷落滿 24 個月應觸發反彈事件");

                // E. 作戰計畫與欺敵（僅手動優先目標兩棲整備觸發；戰後清空）
                var sE1 = SimEngine.NewGame(3005u);
                sE1.PriorityTargets.Add("FJ");
                sE1.PendingEvents.Clear();
                var twU = SimEngine.UnitsAt(sE1, "TW", UnitDomain.Land).Take(2).Select(u => u.Id).ToList();
                var (movedE, errE) = SimEngine.AttackNow(sE1, twU, "FJ", manual: true);
                Assert(movedE > 0 && string.IsNullOrEmpty(errE), "優先目標手動反攻應成功建立兩棲計畫");
                Assert(sE1.PendingEvents.Any(e => e.Id.StartsWith("op_plan_FJ_")), "手動進攻優先登陸目標應彈出作戰計畫會議");
                int reqBefore = sE1.Orders.First(o => o.To == "FJ").PrepRequired;
                EventSystem.ResolveChoice(sE1, 0); // blitz
                Assert(sE1.ActiveOpPlanKind == "blitz" && sE1.Orders.First(o => o.To == "FJ").PrepRequired <= reqBefore,
                    "奇襲搶灘應設定 blitz 並縮減籌備天數");

                var sE2 = SimEngine.NewGame(3005u);
                sE2.PriorityTargets.Add("FJ");
                sE2.PendingEvents.Clear();
                SimEngine.AttackNow(sE2, SimEngine.UnitsAt(sE2, "TW", UnitDomain.Land).Take(2).Select(u => u.Id), "FJ", manual: true);
                double garBeforeMethod = sE2.Garrison["FJ"];
                EventSystem.ResolveChoice(sE2, 2); // methodical
                Assert(sE2.ActiveOpPlanKind == "methodical" && sE2.Garrison["FJ"] < garBeforeMethod,
                    "步步為營方案應先削弱目標守軍並設定 methodical");
                SimEngine.AddUnit(sE2, "inf", "FJ", 100);
                SimEngine.Battle(sE2, "FJ", true, new HashSet<int>());
                Assert(string.IsNullOrEmpty(sE2.ActiveOpPlanKind) && string.IsNullOrEmpty(sE2.ActiveOpTargetId),
                    "目標交戰結算後應清空當前作戰計畫狀態");

                // F. 敵後滲透網 Lv.3 成熟與永久無迷霧情資
                var sF = SimEngine.NewGame(3006u);
                sF.Month = 3;
                sF.PendingEvents.Clear();
                foreach (var z in GameData.ZoneIds) { SimEngine.AddUnit(sF, "fleet", z, 100); SimEngine.AddUnit(sF, "air", z, 100); }
                string thF = EventSystem.AnalyzeFrontline(sF).Theater.ToString();
                sF.Cooldowns[$"posture_exp_{thF}"] = 999;
                sF.Cooldowns[$"posture_win_{thF}"] = 999;
                sF.UndergroundNet["FJ"] = 3;
                EventSystem.CheckDynamic(sF);
                Assert(sF.PendingEvents.Any(e => e.Id.StartsWith("ug_mature_FJ_")), "地下網達 Lv.3 應觸發成熟三選一事件");
                EventSystem.ResolveChoice(sF, 1); // 維持潛伏情報網
                Assert(sF.HasFlag("ugIntel_FJ") && SimEngine.HasIntel(sF, "FJ") && sF.UndergroundNet["FJ"] == 2,
                    "選擇潛伏情報網應啟用常時無迷霧情資並降至 Lv.2");

                // G. 共軍五年戰略學說輪替、情報迷霧揭露與 >= 2 省緊急總動員
                var sG = SimEngine.NewGame(3007u);
                Assert(sG.PrcDoctrine == "coastal_wall", "1950 初期共軍學說應為東南岸防重兵");
                string descApprox = SimEngine.DescribePrcDoctrineIntel(sG);
                Assert(descApprox.Contains("概估") && descApprox.Contains("或"), "無高階電偵或情報優勢時應只提供二選一概估");
                SimEngine.GrantTech(sG, "black_cat");
                string descExact = SimEngine.DescribePrcDoctrineIntel(sG);
                Assert(descExact.Contains("確報") && descExact.Contains("東南岸防重兵"), "擁有黑貓中隊後應獲得精確學說確報");
                sG.Year = 1952; sG.Month = 12; sG.Day = 31;
                SimEngine.AdvanceDay(sG);
                Assert(sG.Year == 1953 && sG.PrcDoctrine == "inland_lure", "1953 年應依五年週期輪替共軍戰略學說");
                sG.Control["GX"] = Faction.ROC; sG.Control["YN"] = Faction.ROC;
                sG.Garrison["GX"] = 0; sG.Garrison["YN"] = 0;
                SimEngine.MonthlyTick(sG);
                Assert(sG.HasFlag("prcMobWarned"), "國軍光復 2 省以上應觸發對岸全國緊急總動員體制");

                // H. 自選開局變數（零選擇完全無擾動 vs 雙變數 +35% 積分加成）
                var sH0a = SimEngine.NewGame(999u, false, null);
                var sH0b = SimEngine.NewGame(999u, false, Array.Empty<string>());
                Assert(Math.Abs(sH0a.UsRel - sH0b.UsRel) < 1e-9 && Math.Abs(sH0a.PrcStab - sH0b.PrcStab) < 1e-9
                    && sH0a.ActiveMutators.Count == 0 && sH0b.ActiveMutators.Count == 0,
                    "未選任何變數時開局數值必須與預設一般局完全一致");
                var sH1 = SimEngine.NewGame(999u, false, new[] { "mut_aid_delayed", "mut_prc_harvest" });
                Assert(sH1.ActiveMutators.Count == 2 && sH1.UsRel < sH0a.UsRel && sH1.PrcStab > sH0a.PrcStab,
                    "啟用變數後應正確套用初期難度修正");
                sH1.Gdp = sH0a.Gdp; sH1.Intl = sH0a.Intl; sH1.Morale = sH0a.Morale; sH1.BattlesWon = sH0a.BattlesWon;
                int scBase = SimEngine.Score(sH0a), scMut = SimEngine.Score(sH1);
                Assert(scMut > scBase, $"變數開局積分應顯著高於無變數基準（無={scBase}, 雙變數={scMut}）");

                // I & J. 雙新結局 (junta / fortress) 與全新增字串無非 BMP 代理字元
                var sJ1 = SimEngine.NewGame(4001u);
                sJ1.Year = 2026;
                sJ1.FactionInfluence["army"] = 85.0;
                sJ1.Morale = 30.0;
                SimEngine.CheckEnd(sJ1);
                Assert(sJ1.Over?.Key == "junta", "單一派系 >= 80 且民心 < 40 應於終戰達成 junta 強人軍政結局");

                var sJ2 = SimEngine.NewGame(4002u);
                sJ2.Year = 2026;
                SimEngine.CheckEnd(sJ2);
                Assert(sJ2.Over?.Key == "fortress", "未失守任何離島且光復省數為 0 應於終戰達成 fortress 金馬鐵壁結局");

                var sJ3 = SimEngine.NewGame(4003u);
                sJ3.Flags["lostAnyIsland"] = 1;
                sJ3.Year = 2026;
                SimEngine.CheckEnd(sJ3);
                Assert(sJ3.Over?.Key != "fortress", "曾失守過離島者不應觸發 fortress 金馬鐵壁結局");

                bool anySurrogate = GameData.Endings.Values.Any(e => (e.Title + e.Text + e.Group).Any(char.IsSurrogate))
                    || GameData.Mutators.Values.Any(m => (m.Title + m.Desc).Any(char.IsSurrogate))
                    || SimEngine.StaffFactions.Any(sp => (sp.Name + sp.Chief + sp.BonusDesc).Any(char.IsSurrogate))
                    || SimEngine.PrcDoctrines.Any(d => (d.Title + d.EffectDesc).Any(char.IsSurrogate));
                Assert(!anySurrogate, "新增結局、變數、派系與學說文字不應包含非 BMP 代理字元");
            }
            LogLine("[PASS] 5F. 系統 A–J 全數十項自創戰略機制與 16 結局驗證通過");

            // 5G. 16 種結局全數透過 1950–2025 模擬器實跑達成驗證
            {
                var cov = BatchSim.RunEndingCoverageSuite(1000u);
                var missing = new List<string>();
                foreach (var ek in GameData.Endings.Keys)
                {
                    if (!cov.TryGetValue(ek, out var rr) || rr.EndingKey != ek)
                        missing.Add($"{ek}(實際={rr?.EndingKey ?? "none"})");
                }
                Assert(missing.Count == 0, $"仍有結局無法透過 1950–2025 模擬器實際達成：{string.Join(", ", missing)}");
            }
            LogLine("[PASS] 5G. 全數 16 種結局皆可透過 1950–2025 模擬策略實跑達成驗證通過");

            // 6. 確認工作區無任何 HTML 檔案
            var htmlFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.html", SearchOption.AllDirectories);
            Assert(htmlFiles.Length == 0, "輸出目錄不應存在任何 HTML 檔案");
            LogLine("=== 全部自動化測試 100% 通過！ ===");
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_report.txt"), sb.ToString());
            return 0;
        }

        private static bool HasButtonWithText(Control root, string keyword)
            => FindButtonWithText(root, keyword) != null;

        private static Button FindButtonWithText(Control root, string keyword)
        {
            if (root is Button b && b.Text != null && b.Text.Contains(keyword)) return b;
            foreach (Control c in root.Controls)
            {
                var hit = FindButtonWithText(c, keyword);
                if (hit != null) return hit;
            }
            return null;
        }

        private static bool HasLabelWithText(Control root, string keyword)
        {
            if (root is Label l && l.Text != null && l.Text.Contains(keyword)) return true;
            foreach (Control c in root.Controls)
            {
                if (HasLabelWithText(c, keyword)) return true;
            }
            return false;
        }

        private static bool HasControlWithText(Control root, string keyword)
        {
            if (!string.IsNullOrEmpty(root.Text) && root.Text.Contains(keyword)) return true;
            foreach (Control c in root.Controls)
            {
                if (HasControlWithText(c, keyword)) return true;
            }
            return false;
        }

        private static bool HasNonBmpControlText(Control root)
        {
            if (!string.IsNullOrEmpty(root.Text) && root.Text.Any(char.IsSurrogate)) return true;
            foreach (Control c in root.Controls)
            {
                if (HasNonBmpControlText(c)) return true;
            }
            return false;
        }
    }
}
