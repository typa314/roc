using System;
using System.Collections.Generic;
using System.Linq;

namespace RocGame
{
    public static partial class EventSystem
    {
        private static bool CooldownOk(GameState s, string key, int months)
        {
            if (s.Cooldowns.TryGetValue(key, out int until) && s.TotalMonths < until) return false;
            s.Cooldowns[key] = s.TotalMonths + months;
            return true;
        }

        public static string FrontNotice(GameState s, int salt)
            => AnalyzeFrontline(s).FrontNotice(s, salt);

        public static bool UnPressureDue(GameState s)
        {
            if (s == null || s.Over != null || s.Year < 1972) return false;
            if (s.Cooldowns != null && s.Cooldowns.TryGetValue("un_intervention", out int until) && s.TotalMonths < until) return false;
            bool rocRemains = GameData.NodeIds.Any(id => s.Control.TryGetValue(id, out var rf) && rf == Faction.ROC);
            bool enemyRemains = GameData.MainlandIds.Any(id => s.Control.TryGetValue(id, out var ef) && ef != Faction.ROC);
            if (!rocRemains || !enemyRemains) return false;
            return SimEngine.FrontStableMonths(s) >= 36;
        }

        static void PressStalemateFront(GameState s, double frac)
        {
            if (AnyPrc(s)) ChipFront(s, frac, 1);
            else ChipHeld(s, SimEngine.UssrNodes(s), frac);
        }

        public static void CheckDynamic(GameState s)
        {
            if (s.Over != null || s.PendingEvents.Count > 0) return;
            var ctx = AnalyzeFrontline(s);
            var held = ctx.HeldMainland;
            string enemyTerm = ctx.EnemyLabel(s);

            if (UnPressureDue(s))
            {
                s.Cooldowns["un_intervention"] = s.TotalMonths + 24;
                int months = SimEngine.FrontStableMonths(s);
                int years = Math.Max(3, months / 12);
                string heat = months >= 96 ? "主要會員國已在起草強制調解，並揚言凍結對雙方的軍售"
                    : months >= 60 ? "大會有人提案派遣停火觀察團進駐當前戰線"
                    : "秘書處的停火說帖開始在代表團之間流傳";
                string front = $"{ctx.TheaterTitle}（{ctx.FrontNames(2)}）";
                s.PendingEvents.Add(new GameEvent
                {
                    Id = $"dyn_un_{s.Year}_{s.Month}",
                    Year = s.Year, Month = s.Month, Day = s.Day, Tag = "時局",
                    Title = months >= 96 ? "聯合國施壓強制調解" : months >= 60 ? "聯合國要求停火觀察" : "聯合國會談聲浪",
                    Desc = $"戰線已連續約 {years} 年沒有省分易手，膠著停在{front}。{heat}。和談可以換來外交與經濟，代價是攻勢停下來；拒絕則要準備在當前敵前挨一輪政治反擊。",
                    Choices = new List<EventChoice>
                    {
                        new()
                        {
                            Label = "接受會談，暫停當前攻勢",
                            Hint = "國際地位 +8、經濟效率 +3%、民心 +2；攻勢加成下降。停火滿三年後結束遊戲，中國沿當時戰線分治",
                            StyleTag = "blue",
                            Effect = st =>
                            {
                                st.Flags["armistice"] = 1;
                                st.Flags["ceasefireAt"] = st.TotalMonths + 1;
                                st.Intl += 8; st.Morale += 2; st.EconMult += 0.03;
                                st.LandingBonus = Math.Max(0, st.LandingBonus - 0.04);
                                SimEngine.Log(st, "台北接受聯合國會談。停火滿三年後，沿當時戰線結束戰爭，中國分治。", "");
                            }
                        },
                        new()
                        {
                            Label = "拒絕和談，施壓當前敵前",
                            Hint = "國際地位 -6、民心 +4；當前敵前守軍約 -10%，省色不變",
                            StyleTag = "red",
                            Effect = st =>
                            {
                                st.Intl -= 6; st.Morale += 4;
                                PressStalemateFront(st, 0.10);
                                var live = AnalyzeFrontline(st);
                                SimEngine.Log(st, $"台北拒絕聯合國停火，並對{live.TheaterTitle}（{live.FrontNames(2)}）施加壓力。", "big");
                            }
                        },
                        new()
                        {
                            Label = "只派人旁聽，戰線照舊",
                            Hint = "國際地位 +3，並有一筆外交進項；當前敵前守軍約 -4%，不記停火",
                            StyleTag = "gold",
                            Effect = st =>
                            {
                                st.Intl += 3;
                                st.Treasury += 0.8 * SimEngine.PriceIndex(st);
                                PressStalemateFront(st, 0.04);
                                SimEngine.Log(st, "會談只派觀察員旁聽，軍事日程沒有停。", "good");
                            }
                        }
                    }
                });
                return;
            }

            if (s.TotalMonths >= 2)
            {
                SimEngine.AssessPosture(s);
                if (!string.IsNullOrEmpty(s.ExposedNodeId) && CooldownOk(s, $"posture_exp_{ctx.Theater}", 6))
                {
                    s.PendingEvents.Add(BuildExposurePostureEvent(s, ctx));
                    return;
                }
                if (string.IsNullOrEmpty(s.ExposedNodeId) && !string.IsNullOrEmpty(s.WindowTargetId)
                    && !ctx.OnlyUssrRemains && CooldownOk(s, $"posture_win_{ctx.Theater}", 6))
                {
                    s.PendingEvents.Add(BuildWindowPostureEvent(s, ctx));
                    return;
                }
            }

            // 1) 🐻 俄國/蘇聯趁機侵占北疆事件
            var availBorder = GameData.BorderUssrIds.Where(id => s.Control[id] == Faction.PRC).ToList();
            if (availBorder.Count > 0 && (s.PrcStab < 46 || held.Count >= 2 || s.SovRel >= 24) && SimEngine.Chance(s, 0.14) && CooldownOk(s, "russian_inv", 18))
            {
                string targetId = availBorder[(int)(SimEngine.Rng(s) * availBorder.Count)];
                string tName = GameData.Nodes[targetId].Name;
                string rusName = s.Year >= 1992 ? "俄羅斯" : "蘇聯";
                s.PendingEvents.Add(new GameEvent
                {
                    Id = $"dyn_russian_inv_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "北疆危機",
                    Title = $"【北極熊趁火打劫】{rusName}出兵強占{tName}！",
                    Desc = $"見大陸內部戰火連天、北疆防務空虛，{rusName}以「保護僑民與邊境安全」為藉口，突然出動大批裝甲師越過邊界強占【{tName}（{GameData.Nodes[targetId].City}）】，企圖製造既成事實吞併我國北疆領土！",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "發表《抗俄保土宣言》！號召北方共軍陣前起義、與國軍聯手抗俄",
                            Hint = $"{rusName}占領{tName}；但我方大義凜然（民心 +12、國際地位 +8），不願當亡國奴的 2 支北方共軍師起義投誠進駐前線！",
                            Effect = st => {
                                SimEngine.SeizeByUssr(st, targetId, 7.0);
                                st.Morale += 12; st.Intl += 8;
                                string sp = AnalyzeFrontline(st).PickForwardRocNode(st);
                                SimEngine.AddUnit(st, "dfc", sp, 85); SimEngine.AddUnit(st, "dfc", sp, 85);
                                SimEngine.Log(st, $"★ 國府通電討伐俄寇！兩支北方共軍主力師憤而起義歸順國府，於【{GameData.Nodes[sp].Name}】會師抗俄！", "big");
                            }
                        },
                        new() {
                            Label = "緊急照會華府與聯合國，以國際制裁迫使俄軍撤退",
                            Hint = "若美國關係 ≥ 62 則迫使俄軍撤出一半兵力（僅留 4 師）；否則獲美援補給 +12",
                            Effect = st => {
                                if (st.UsRel >= 62) {
                                    SimEngine.SeizeByUssr(st, targetId, 4.0);
                                    st.Intl += 5;
                                    SimEngine.Log(st, $"在美方與國際強大壓力下，{rusName}駐{tName}部隊被迫縮減一半兵力。", "good");
                                } else {
                                    SimEngine.SeizeByUssr(st, targetId, 7.5);
                                    st.Supply += 12;
                                    SimEngine.Log(st, $"{rusName}無視外交抗議強占{tName}，美方緊急撥交戰略物資支援我國。", "bad");
                                }
                            }
                        },
                        new() {
                            Label = "暫時默認俄占現狀，換取俄方封鎖中共北方補給線（借刀殺人）",
                            Hint = $"{rusName}占領{tName}；中共穩定度 -15、各地共軍守軍 -15%，但國府背負罵名（民心 -10、國際地位 -8）",
                            Effect = st => {
                                SimEngine.SeizeByUssr(st, targetId, 8.0);
                                st.PrcStab -= 15; st.Morale -= 10; st.Intl -= 8;
                                foreach (var id in GameData.MainlandIds) if (st.Control[id] == Faction.PRC) st.Garrison[id] *= 0.85;
                                SimEngine.Log(st, $"國府默許俄軍占領{tName}以夾擊中共，雖重創共軍但引發輿論譁然！", "bad");
                            }
                        }
                    }
                });
                return;
            }

            // 2) 前線逃兵與厭戰事件（優先選最前線戰區部隊）
            var allFrontLand = s.Units.Where(u => GameData.UnitTypes[u.Type].Domain == UnitDomain.Land && u.Loc != "TW" && u.Type != "mil").ToList();
            var frontUnits = allFrontLand.Where(u => ctx.RocFrontNodes.Contains(u.Loc)).ToList();
            if (frontUnits.Count == 0) frontUnits = allFrontLand;
            if (frontUnits.Count > 0 && (s.Morale < 45 || s.SupplyRatio < 0.75) && SimEngine.Chance(s, 0.16) && CooldownOk(s, "desertion", 12))
            {
                var targetU = frontUnits[(int)(SimEngine.Rng(s) * frontUnits.Count)];
                string locName = GameData.Nodes[targetU.Loc].Name;
                s.PendingEvents.Add(new GameEvent
                {
                    Id = $"dyn_desertion_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "動態",
                    Title = $"前線逃兵與厭戰風波（{locName}）",
                    Desc = $"駐守於{locName}的{GameData.UnitTypes[targetU.Type].Name}因長期在【{ctx.TheaterTitle}】苦戰戒備、補給吃緊與思鄉情緒蔓延，發生官兵私自逃亡與抗命情事！",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "發放特別犒賞並緊急空投補給安撫軍心",
                            Hint = $"花費國庫 {(0.2 * SimEngine.PriceIndex(s)):F1}、補給 -4；該部戰力恢復 +10%、民心 +3",
                            Effect = st => {
                                st.Treasury = Math.Max(0, st.Treasury - 0.2 * SimEngine.PriceIndex(st));
                                st.Supply = Math.Max(0, st.Supply - 4);
                                targetU.Str = Math.Min(100, targetU.Str + 10);
                                st.Morale += 3;
                                SimEngine.Log(st, $"統帥部撥發特別犒賞並改善{locName}補給，穩定了前線軍心。", "good");
                            }
                        },
                        new() {
                            Label = "軍法從事！嚴懲逃兵以儆效尤",
                            Hint = "遏止逃亡（該部戰力僅 -8%），但家屬與輿論反彈（民心 -5、兵源 -0.8）",
                            Effect = st => {
                                targetU.Str = Math.Max(15, targetU.Str - 8);
                                st.Morale -= 5; st.Manpower = Math.Max(0, st.Manpower - 0.8);
                                SimEngine.Log(st, $"督戰隊在{locName}執行軍法處決逃兵，雖壓制嘩變但重傷民心。", "bad");
                            }
                        },
                        new() {
                            Label = "容許老弱病殘退伍返鄉，縮編整頓部隊",
                            Hint = "該部隊戰力 -22%，但換取民心 +2",
                            Effect = st => {
                                targetU.Str = Math.Max(15, targetU.Str - 22);
                                st.Morale += 2;
                                SimEngine.Log(st, $"{locName}部隊因逃兵與縮編導致兵力減損。", "bad");
                            }
                        }
                    }
                });
                return;
            }

            // 3) 地方民兵／義勇軍起義加入（隨當前交戰前線選擇省分並自動區分抗共／抗俄敘事）
            var hotProvs = ctx.HostileFrontNodes.Where(id => s.Unrest[id] >= 20).ToList();
            if (hotProvs.Count == 0)
                hotProvs = GameData.MainlandIds.Where(id => s.Control[id] != Faction.ROC && s.Unrest[id] >= 24).ToList();
            if (hotProvs.Count == 0 && ctx.RocFrontNodes.Count > 0)
                hotProvs = new List<string>(ctx.RocFrontNodes);
            if (hotProvs.Count > 0 && SimEngine.Chance(s, 0.18) && CooldownOk(s, "militia", 10))
            {
                string pid = hotProvs[(int)(SimEngine.Rng(s) * hotProvs.Count)];
                string pName = GameData.Nodes[pid].Name;
                bool isUssrTarget = s.Control[pid] == Faction.USSR || ctx.OnlyUssrRemains;
                string milEnemy = isUssrTarget ? (s.Year >= 1992 ? "俄軍" : "蘇俄軍") : "共軍";
                string milCause = isUssrTarget ? "抗俄保鄉" : "反共救國";
                string spawnLoc = s.Control[pid] == Faction.ROC ? pid : ctx.PickForwardRocNode(s);
                s.PendingEvents.Add(new GameEvent
                {
                    Id = $"dyn_militia_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "動態",
                    Title = $"{pName}{milCause}義勇民兵武裝起事！",
                    Desc = $"{pName}（{GameData.Nodes[pid].City}）地方義勇組織與山區游擊隊秘密派員聯絡我方情報站，表示已有數千青壯年集結抗擊{milEnemy}，請求國府空投武器或收編為正式支隊！",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"空投武器彈藥，正式收編為「{milCause}民兵」支隊",
                            Hint = $"消耗補給 3；在{GameData.Nodes[spawnLoc].Name}獲得 1 支「民兵/游擊」部隊（戰力 85%），並削弱{pName}{milEnemy}約 8% 守軍",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 3);
                                SimEngine.AddUnit(st, "mil", spawnLoc, 85);
                                ChipHeld(st, new[] { pid }, 0.08);
                                SimEngine.Log(st, $"★ {pName}{milCause}民兵獲我方空投支援，於【{GameData.Nodes[spawnLoc].Name}】正式加入戰鬥序列！", "good");
                            }
                        },
                        new() {
                            Label = $"指示民兵就地潛伏，襲擊{milEnemy}糧倉與鐵路補給線",
                            Hint = $"{pName}{milEnemy}守軍約 -12%、民心浮動 +12，我方獲得情報與兵源 +1.5",
                            Effect = st => {
                                ChipHeld(st, new[] { pid }, 0.12);
                                st.Unrest[pid] = SimEngine.Clamp(st.Unrest[pid] + 12, 0, 100);
                                st.Manpower += 1.5;
                                SimEngine.Log(st, $"{pName}義勇民兵襲擊{milEnemy}後勤車隊，重創當地守軍！", "good");
                            }
                        }
                    }
                });
                return;
            }

            // 4) 共軍官兵駕機／陣前起義投誠（僅於仍有中共省分時出現，並精準削弱當前前線共軍省分）
            double defDynP = 0.15 + (s.Techs.Contains("hongkong_psy") ? 0.05 : 0.0) + (s.Techs.Contains("defector_award") ? 0.05 : 0.0) + (s.Techs.Contains("united_subvert") ? 0.06 : 0.0) + (s.Techs.Contains("cyber_psy") ? 0.05 : 0.0);
            if (ctx.PrcNodes.Count > 0 && (s.PrcStab < 52 || s.Gdp > SimEngine.PrcGdp(s) * 0.4) && SimEngine.Chance(s, defDynP) && CooldownOk(s, "defection", 12))
            {
                var prcPool = ctx.PrcFrontNodes.Count > 0 ? ctx.PrcFrontNodes : ctx.PrcNodes;
                string defSrcId = prcPool[(int)(SimEngine.Rng(s) * Math.Min(3, prcPool.Count))];
                string defSrcName = GameData.Nodes[defSrcId].Name;
                string destNode = ctx.PickForwardRocNode(s);
                s.PendingEvents.Add(new GameEvent
                {
                    Id = $"dyn_defection_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "動態",
                    Title = $"{defSrcName}前線官兵起義來歸！",
                    Desc = $"受大陸政局動盪與我方心戰號召影響，駐防【{defSrcName}】的共軍官兵與飛行員秘密透過電台向我方前線表達起義意願，請求接應！",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "派遣空地兵力掩護接應，整編為國軍「起義投誠部隊」",
                            Hint = $"在{GameData.Nodes[destNode].Name}獲得 1 支「投誠部隊」（戰力 85%），{defSrcName}共軍約 -9%，民心 +4",
                            Effect = st => {
                                SimEngine.AddUnit(st, "dfc", destNode, 85);
                                ChipHeld(st, new[] { defSrcId }, 0.09);
                                st.Morale += 4;
                                SimEngine.Log(st, $"★ {defSrcName}共軍部隊陣前起義來歸，已於{GameData.Nodes[destNode].Name}整編入列！", "good");
                            }
                        },
                        new() {
                            Label = "頒發黃金獎勵反共義士，並將攜來之新型戰機送交中科院拆解研究",
                            Hint = "空軍科技 +0.3、民心 +5、中共穩定度 -4",
                            Effect = st => {
                                st.AirTech += 0.3; st.Morale += 5; st.PrcStab -= 4;
                                SimEngine.Log(st, "反共義士駕新型戰機來歸，空軍獲得珍貴技術參數，全國士氣大振！", "good");
                            }
                        }
                    }
                });
                return;
            }

            // 5) 國際調停停戰提案（記錄外交停戰立場，須待遊戲時間結束後方結算）
            if (held.Count >= 3 && s.Turn >= 4 && ctx.HostileFrontNodes.Count > 0 && !s.HasFlag("armisticeOffered") && SimEngine.Chance(s, 0.25))
            {
                s.Flags["armisticeOffered"] = 1;
                string heldNames = string.Join("、", held.Take(6).Select(id => GameData.Nodes[id].Name)) + (held.Count > 6 ? $" 等 {held.Count} 省" : "");
                string fwdRoc = ctx.PickForwardRocNode(s);
                s.PendingEvents.Add(new GameEvent
                {
                    Id = "dyn_armistice_offer", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "戰略抉擇",
                    Title = "國際聯合調停：《劃界分治與停火協定》提案",
                    Desc = $"國軍已在大陸光復 {heldNames}，目前於【{ctx.TheaterTitle}】持續交鋒。國際強權擔心全面戰爭失控，提出「暫行停火、劃界談判」方案。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "簽署暫行停火協議，暫緩全面北上先圖休養生息",
                            Hint = "國庫 +$1.2 億、民心 +5、補給 +10；戰役持續至終戰（若屆時未全面光復，將達成【停止戰線・停戰協定】結局）",
                            Effect = st => {
                                st.Flags["armistice"] = 1;
                                st.Treasury += 1.2; st.Morale += 5; st.Supply += 10;
                                SimEngine.Log(st, "雙方暫時締結停火協商線休養生息，終局歸屬仍待終戰或戰線變化定奪。", "");
                            }
                        },
                        new() {
                            Label = "嚴詞拒絕！「漢賊不兩立，王師必克復全境！」",
                            Hint = $"繼續推進戰線！民心 +8、於{GameData.Nodes[fwdRoc].Name}獲得 1 支民兵與 +2 兵源，但美國關係 -6",
                            Effect = st => {
                                st.Morale += 8; st.Manpower += 2; st.UsRel -= 6;
                                SimEngine.AddUnit(st, "mil", fwdRoc, 85);
                                SimEngine.Log(st, $"最高統帥拒絕停戰劃界，宣示反攻到底，【{GameData.Nodes[fwdRoc].Name}】前線民眾踴躍從軍！", "big");
                            }
                        }
                    }
                });
                return;
            }

            // 6) 大陸軍區割據危機（僅作戰局情勢與戰術應對，絕不在遊戲途中提早結束達成軍閥結局）
            if (ctx.PrcNodes.Count > 0 && s.PrcStab <= 25 && !s.HasFlag("warlordEventFired") && SimEngine.Chance(s, 0.30))
            {
                s.Flags["warlordEventFired"] = 1;
                string fwdRoc = ctx.PickForwardRocNode(s);
                s.PendingEvents.Add(new GameEvent
                {
                    Id = "dyn_warlord_crisis", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "戰略抉擇",
                    Title = "大陸各大軍區擁兵自重・藩鎮割據暗潮湧動",
                    Desc = $"北京中央威信動搖，地方軍區紛紛截留稅賦、觀望戰局，部分將領派密使與台北接洽「以自治換取停止敵對」。統帥部須抉擇如何應對此一戰略變局，以避免大陸陷入長期混戰！",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "策動軍區地方派系反水自保，許以戰後優遇條件分化敵軍",
                            Hint = "中共穩定 -8、敵方前線與內地共軍守軍 -25%、我方補給 +6；但戰後地方割據風險升高（若終戰兵力不足將有割據之虞）",
                            Effect = st => {
                                st.PrcStab -= 8; st.Supply += 6;
                                st.Flags["prcZeroTurns"] = Math.Max(st.GetFlag("prcZeroTurns"), 1);
                                foreach (var id in GameData.MainlandIds) if (st.Control[id] == Faction.PRC) st.Garrison[id] = Math.Max(0.5, st.Garrison[id] * 0.75);
                                SimEngine.Log(st, "各地軍區派系彼此掣肘、消極避戰，共軍防線大面積鬆動！", "good");
                            }
                        },
                        new() {
                            Label = "拒絕與軍頭分贓！收編願歸順部隊，乘勝直取中樞統一全國",
                            Hint = $"敵軍守軍削弱 45%，於{GameData.Nodes[fwdRoc].Name}獲得 2 支投誠部隊，全力擴大戰果",
                            Effect = st => {
                                foreach (var id in GameData.MainlandIds) if (st.Control[id] == Faction.PRC) st.Garrison[id] = Math.Max(0.5, st.Garrison[id] * 0.55);
                                SimEngine.AddUnit(st, "dfc", fwdRoc, 85); SimEngine.AddUnit(st, "dfc", fwdRoc, 85);
                                SimEngine.Log(st, $"各省軍頭內訌瓦解，兩支主力師向國軍【{GameData.Nodes[fwdRoc].Name}】前線投誠！", "big");
                            }
                        }
                    }
                });
                return;
            }

            // 7) 國軍前線將領擁兵自重（謹慎調整戰局風險與軍紀，絕不於遊戲進行中提早結算藩鎮割據結局）
            if (held.Count >= 2 && (s.Morale < 32 || s.SupplyRatio < 0.65) && SimEngine.Chance(s, 0.28) && CooldownOk(s, "roc_warlord", 14))
            {
                string fwdName = GameData.Nodes[ctx.PickForwardRocNode(s)].Name;
                s.PendingEvents.Add(new GameEvent
                {
                    Id = $"dyn_roc_warlord_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "戰略抉擇",
                    Title = $"前線戰區將領軍紀整頓風波（{fwdName}）",
                    Desc = $"由於台北中央補給吃緊且戰事膠著，【{ctx.TheaterTitle}】前線將領以「就地籌餉」為由擅自擴編親兵、截留地方稅賦，恐成藩鎮割據隱患！",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "暫時授權前線就地籌餉以穩住戰力，延後處置軍政整合",
                            Hint = "前線部隊戰力 +8%、補給 +8；但民心 -5 且割據風險累積（+1 級），須於終戰前扭轉局勢",
                            Effect = st => {
                                st.Supply += 8; st.Morale -= 5;
                                st.Flags["warlordRisk"] = st.GetFlag("warlordRisk") + 1;
                                foreach (var u in st.Units.Where(x => ctx.RocFrontNodes.Contains(x.Loc)))
                                    u.Str = Math.Min(100, u.Str + 8);
                                SimEngine.Log(st, $"中央暫許{fwdName}前線就地籌補以救急，地方自主傾向持續累積。", "bad");
                            }
                        },
                        new() {
                            Label = "派特使攜重金與補給赴前線安撫，並輪調將領收回兵權",
                            Hint = $"花費國庫 {(0.4 * SimEngine.PriceIndex(s)):F1}、補給 -5，消弭割據風險（歸零）且民心 +5",
                            Effect = st => {
                                st.Treasury = Math.Max(0, st.Treasury - 0.4 * SimEngine.PriceIndex(st));
                                st.Supply = Math.Max(0, st.Supply - 5);
                                st.Morale += 5; st.Flags["warlordRisk"] = 0;
                                SimEngine.Log(st, $"中央順利完成{fwdName}戰區將領輪調，徹消藩鎮割據隱患。", "good");
                            }
                        }
                    }
                });
                return;
            }

            // 8) 北京最後通牒（僅作外交立場紀錄，不於遊戲途中直接結束）
            if (s.Year >= 1988 && held.Count == 0 && ctx.PrcNodes.Count >= 6 && SimEngine.ThreatIndex(s) > 1.8 && s.Intl < 40 && !s.HasFlag("ultimatumFired") && SimEngine.Chance(s, 0.28))
            {
                s.Flags["ultimatumFired"] = 1;
                s.PendingEvents.Add(new GameEvent
                {
                    Id = "dyn_ultimatum", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "戰略抉擇",
                    Title = "北京發出最後通牒：「一國兩制」或武力解決",
                    Desc = "隨著對岸經濟與海空軍力急速膨脹，解放軍已在東南沿海完成渡海集結，北京向台北發出最後通牒，要求限期談判接受併吞，否則將發動全面攻台作戰。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "拒絕投降！發布緊急動員令，全島背水一戰",
                            Hint = "民心 +10、兵源 +4、台灣本島增編 2 支步兵師與 1 支民兵，準備迎擊渡海攻勢",
                            Effect = st => {
                                st.Morale += 10; st.Manpower += 4;
                                SimEngine.AddUnit(st, "inf", "TW", 85); SimEngine.AddUnit(st, "inf", "TW", 85); SimEngine.AddUnit(st, "mil", "TW", 85);
                                SimEngine.Log(st, "全國拒絕北京最後通牒，百萬後備軍人動員入伍，誓死保衛台灣！", "big");
                            }
                        },
                        new() {
                            Label = "在懸殊壓力下被迫簽署城下妥協協議以圖喘息",
                            Hint = "民心 -15、國庫 +$1.5 億；若至終戰時仍無反攻翻盤將以【反被併吞・城下之盟】結算",
                            Effect = st => {
                                st.Flags["annexed"] = 1;
                                st.Morale -= 15; st.Treasury += 1.5;
                                SimEngine.Log(st, "台北被迫簽署屈辱政治協議暫緩衝突，全國士民悲憤待變。", "bad");
                            }
                        }
                    }
                });
                return;
            }

            if (s.TotalMonths < 2) return;

            // 9) 多元主題加權輪抽池：破除固定順序重複，依當下戰局與冷卻輪替呈現外交、內政、諜報、財經、海空與前線事件
            if (!SimEngine.Chance(s, 0.72)) return;

            var dynPool = BuildDynamicVarietyPool(s, ctx, enemyTerm);
            var readyPool = dynPool.Where(p => p.ok && (!s.Cooldowns.TryGetValue($"dynpool_{p.key}", out int until) || s.TotalMonths >= until)).ToList();
            if (readyPool.Count == 0) readyPool = dynPool.Where(p => p.ok).ToList();
            if (readyPool.Count == 0) return;

            double totalWeight = readyPool.Sum(p => p.weight);
            double roll = SimEngine.Rng(s) * totalWeight;
            double acc = 0;
            var chosen = readyPool[readyPool.Count - 1];
            foreach (var item in readyPool)
            {
                acc += item.weight;
                if (roll <= acc) { chosen = item; break; }
            }
            s.Cooldowns[$"dynpool_{chosen.key}"] = s.TotalMonths + chosen.cd;
            s.PendingEvents.Add(chosen.build());
        }

        private static GameEvent PickRotatedTemplate(GameState s, string prefix, IReadOnlyList<(string key, bool ok, Func<GameEvent> build)> pool, int cooldownMonths)
        {
            var eligible = pool.Where(x => x.ok).ToList();
            var fresh = eligible.Where(x => !s.Cooldowns.TryGetValue($"{prefix}{x.key}", out int until) || s.TotalMonths >= until).ToList();
            var pickFrom = fresh.Count > 0
                ? fresh
                : eligible.OrderBy(x => s.Cooldowns.TryGetValue($"{prefix}{x.key}", out int until) ? until : 0).Take(Math.Max(1, eligible.Count / 2)).ToList();
            var winner = pickFrom[(int)(SimEngine.Rng(s) * pickFrom.Count)];
            s.Cooldowns[$"{prefix}{winner.key}"] = s.TotalMonths + cooldownMonths;
            return winner.build();
        }

        private static List<(string key, int cd, bool ok, double weight, Func<GameEvent> build)> BuildDynamicVarietyPool(
            GameState s, FrontlineContext ctx, string enemyTerm)
        {
            double px = SimEngine.PriceIndex(s);
            string fwdRoc = ctx.PickForwardRocNode(s);
            string fwdRocName = GameData.Nodes[fwdRoc].Name;
            var frontTargets = ctx.HostileFrontNodes.Take(3).ToList();
            string ftNames = frontTargets.Count > 0
                ? string.Join("、", frontTargets.Select(id => GameData.Nodes[id].Name))
                : ctx.FrontNames(2);

            return new List<(string key, int cd, bool ok, double weight, Func<GameEvent> build)>
            {
                ("patrol", 10, s.Year >= 1951 && ctx.HostileFrontNodes.Count > 0, 1.0, () => BuildPatrolVarietyEvent(s, ctx, enemyTerm)),
                ("econ", 11, s.Year >= 1951, 1.25, () => BuildEconVarietyEvent(s, px)),
                ("diplo", 11, true, 1.2, () => BuildDiploVarietyEvent(s, ctx, px)),
                ("intel_soc", 9, true, 1.4, () => BuildIntelSocietyVarietyEvent(s, ctx, enemyTerm, frontTargets, ftNames, px)),
                ("prc_crack", 11, ctx.PrcNodes.Count > 0, 1.15, () => BuildPrcCrackVarietyEvent(s, ctx, ftNames)),
                ("theater_front", 11, ctx.HostileFrontNodes.Count > 0, 0.95, () => BuildTheaterFrontEvent(s, ctx, enemyTerm, frontTargets, ftNames, fwdRoc, fwdRocName, px)),
                ("mil_deploy", 8, true, 1.0, () => CreateMilitaryDeploymentEvent(s))
            };
        }

        private static GameEvent BuildPatrolVarietyEvent(GameState s, FrontlineContext ctx, string enemyTerm)
        {
            var pool = new List<(string key, bool ok, Func<GameEvent> build)>
            {
                ("skirmish", true, () =>
                {
                    if (ctx.ContestedZones.Count > 0 && !ctx.OnlyUssrRemains)
                    {
                        string zId = ctx.PrimaryZoneId;
                        string zName = GameData.Zones[zId].Name;
                        var zoneTargets = PickFromPreferredOrFront(s,
                            GameData.MainlandIds.Where(id => GameData.Nodes[id].Zones.Contains(zId)), 3, Faction.PRC);
                        string ztNames = zoneTargets.Count > 0
                            ? string.Join("、", zoneTargets.Select(id => GameData.Nodes[id].Name))
                            : ctx.FrontNames(2);
                        string patrolTitle = zId switch
                        {
                            "Z1" => "黃渤海制海權巡弋暨遼魯沿岸遭遇戰",
                            "Z2" => "東海海域海空巡弋暨蘇浙前線遭遇戰",
                            "Z4" => "南海沿岸海空巡弋暨粵桂警戒遭遇戰",
                            _ => "台灣海峽海空巡弋遭遇戰"
                        };
                        return new GameEvent
                        {
                            Id = $"dyn_strait_patrol_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "海空前線",
                            Title = patrolTitle,
                            Desc = $"空軍偵照機與海軍巡防艦隊於{zName}前線空域發現{ztNames}方向共軍艦機試圖前推偵巡，前線艦隊長官急電請示處置原則！",
                            Choices = new List<EventChoice>
                            {
                                new() {
                                    Label = $"下令艦隊與戰機強勢攔截接戰，爭奪{zName}制海制空權",
                                    Hint = $"補給 -4；空軍與海軍科技各 +0.25、{ztNames}敵軍約 -9%、美國關係 -2",
                                    Effect = st => {
                                        st.Supply = Math.Max(0, st.Supply - 4);
                                        st.AirTech += 0.25; st.NavyTech += 0.25;
                                        ChipHeld(st, zoneTargets, 0.09);
                                        st.UsRel -= 2;
                                        SimEngine.Log(st, $"我軍{zName}海空編隊果斷迎擊敵軍挑釁，迫退敵機艦並削弱【{ztNames}】防務！", "good");
                                    }
                                },
                                new() {
                                    Label = $"採取穩健護航態勢，掩護{zName}前線運補船團安全卸載",
                                    Hint = "補給 +5、民心 +2，海軍艦隊戰力略微整補 +6%",
                                    Effect = st => {
                                        st.Supply += 5; st.Morale += 2;
                                        foreach (var u in st.Units.Where(u => u.Type == "fleet")) u.Str = Math.Min(100, u.Str + 6);
                                        SimEngine.Log(st, $"海軍護航編隊安全護送{zName}運補船團完成卸載。", "good");
                                    }
                                },
                                new() {
                                    Label = "通報盟軍協防指揮部並執行高空電子偵照前線",
                                    Hint = $"美國關係 +3、科研進度 +1.2、{ztNames}情報滲透動盪 +6",
                                    Effect = st => {
                                        st.UsRel += 3; st.ResearchProg += 1.2;
                                        foreach (var tid in zoneTargets)
                                            if (st.Control[tid] != Faction.ROC) st.Unrest[tid] = SimEngine.Clamp(st.Unrest[tid] + 6, 0, 100);
                                        SimEngine.Log(st, $"我方聯合情蒐掌握{ztNames}敵軍兵力調動與雷達參數。", "");
                                    }
                                }
                            }
                        };
                    }
                    var airTargets = ctx.HostileFrontNodes.Take(3).ToList();
                    string atNames = string.Join("、", airTargets.Select(id => GameData.Nodes[id].Name));
                    string fwdNode = ctx.PickForwardRocNode(s);
                    return new GameEvent
                    {
                        Id = $"dyn_strait_patrol_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "空地前線",
                        Title = ctx.OnlyUssrRemains
                            ? $"北疆（{atNames}）空地聯合作戰制空遭遇戰"
                            : $"{ctx.TheaterTitle}（{atNames}）前線空地支援遭遇戰",
                        Desc = $"我軍空軍前線聯隊於【{atNames}】戰區上空偵獲{enemyTerm}戰機與裝甲先遣縱隊大規模調動，前敵總部急電請示空地協同接戰原則！",
                        Choices = new List<EventChoice>
                        {
                            new() {
                                Label = $"全力出動戰鬥轟炸機壓制{atNames}之{enemyTerm}機群與裝甲縱隊",
                                Hint = $"補給 -4；空軍與陸軍科技各 +0.25、{atNames}{enemyTerm}守軍約 -9%",
                                Effect = st => {
                                    st.Supply = Math.Max(0, st.Supply - 4);
                                    st.AirTech += 0.25; st.ArmyTech += 0.25;
                                    ChipHeld(st, airTargets, 0.09);
                                    SimEngine.Log(st, $"我方空地編隊重創【{atNames}】{enemyTerm}先遣縱隊，奪得前線制空優勢！", "good");
                                }
                            },
                            new() {
                                Label = $"優先掩護{GameData.Nodes[fwdNode].Name}前方補給車站與野戰機場運補",
                                Hint = "補給 +6、民心 +2，空軍聯隊戰力恢復 +6%",
                                Effect = st => {
                                    st.Supply += 6; st.Morale += 2;
                                    foreach (var u in st.Units.Where(u => u.Type == "air")) u.Str = Math.Min(100, u.Str + 6);
                                    SimEngine.Log(st, $"空軍奪取護航傘保護，順利保障{GameData.Nodes[fwdNode].Name}前線鐵公運補線暢通。", "good");
                                }
                            },
                            new() {
                                Label = "執行高空電戰干擾並策應前線情報網滲透",
                                Hint = $"國際聲望 +3、科研進度 +1.4、{atNames}敵後動盪 +8",
                                Effect = st => {
                                    st.Intl += 3; st.ResearchProg += 1.4;
                                    foreach (var tid in airTargets)
                                        if (st.Control[tid] != Faction.ROC) st.Unrest[tid] = SimEngine.Clamp(st.Unrest[tid] + 8, 0, 100);
                                    SimEngine.Log(st, $"我方電子偵察與干擾行動大幅癱瘓{atNames}{enemyTerm}指管通聯。", "");
                                }
                            }
                        }
                    };
                }),
                ("blockade_ship", ctx.ContestedZones.Count > 0 && !ctx.OnlyUssrRemains, () =>
                {
                    string zName = ctx.PrimaryZoneName;
                    string tgtNames = ctx.FrontNames(2);
                    return new GameEvent
                    {
                        Id = $"dyn_blockade_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "海上封鎖",
                        Title = $"「關閉政策」攔檢外籍資敵貨輪外交風波（{zName}）",
                        Desc = $"巡弋艦隊於{zName}公海邊緣攔獲掛有外國旗幟、滿載油料與機械零件駛往【{tgtNames}】的外輪。船東與外國使館提出抗議，海軍請示處置！",
                        Choices = new List<EventChoice>
                        {
                            new() {
                                Label = "依戰時封鎖令強制押回高雄港沒收戰略物資",
                                Hint = $"國庫 +$0.5 億、補給 +10、{tgtNames}敵軍約 -8%，但國際地位 -4",
                                Effect = st => {
                                    st.Treasury += 0.5; st.Supply += 10; st.Intl -= 4;
                                    ChipHeld(st, ctx.HostileFrontNodes.Take(2), 0.08);
                                    SimEngine.Log(st, $"海軍依關閉政策扣押資匪貨輪，充實軍需但也招致外國領事抗議。", "");
                                }
                            },
                            new() {
                                Label = "只卸除違禁軍火零件後放行船隻，兼顧封鎖與西方外交體面",
                                Hint = "補給 +5、海軍科技 +0.2、國際地位 +2",
                                Effect = st => {
                                    st.Supply += 5; st.NavyTech += 0.2; st.Intl += 2;
                                    SimEngine.Log(st, "海軍有序查扣軍用物資並依海商法放行商輪，贏得西方外交界認可。", "good");
                                }
                            }
                        }
                    };
                }),
                ("night_torpedo", ctx.ContestedZones.Count > 0 && !ctx.OnlyUssrRemains, () =>
                {
                    string zName = ctx.PrimaryZoneName;
                    string tgtNames = ctx.FrontNames(2);
                    return new GameEvent
                    {
                        Id = $"dyn_torpedo_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "海空前線",
                        Title = $"{zName}夜間雷達伏擊暨魚雷快艇戰術檢討",
                        Desc = $"近期敵軍改採夜間小型機動船隊沿【{tgtNames}】近岸潛航滲透，艦隊司令建議換裝夜間雷達與快速獵潛艦艇以維持制海壓制力。",
                        Choices = new List<EventChoice>
                        {
                            new() {
                                Label = "核撥專案費加裝夜戰雷達並發動近岸夜襲",
                                Hint = $"國庫 -${(0.35 * SimEngine.PriceIndex(s)):F1} 億；海軍科技 +0.3、兩棲登陸 +2%、{tgtNames}動盪 +6",
                                Effect = st => {
                                    SpendTreasury(st, 0.35);
                                    st.NavyTech += 0.3; st.LandingBonus += 0.02;
                                    foreach (var id in ctx.HostileFrontNodes.Take(2))
                                        if (st.Control[id] != Faction.ROC) st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 6, 0, 100);
                                    SimEngine.Log(st, $"夜間雷達小艦隊於{zName}重挫敵方近岸運輸線！", "good");
                                }
                            },
                            new() {
                                Label = "改採誘敵深入伏擊戰法並趁夜空投蛙人情蒐",
                                Hint = "科研進度 +1.5、海軍艦隊戰力 +5%",
                                Effect = st => {
                                    st.ResearchProg += 1.5;
                                    foreach (var u in st.Units.Where(x => x.Type == "fleet")) u.Str = Math.Min(100, u.Str + 5);
                                    SimEngine.Log(st, "我軍夜襲特戰隊順利帶回對岸沿岸雷達網與水文參數。", "good");
                                }
                            }
                        }
                    };
                })
            };
            return PickRotatedTemplate(s, "patrol_tpl_", pool, 24);
        }

        private static GameEvent BuildEconVarietyEvent(GameState s, double px)
        {
            var pool = new List<(string key, bool ok, Func<GameEvent> build)>
            {
                ("econ_boom", true, () => new GameEvent
                {
                    Id = $"dyn_economic_boom_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "內政經濟",
                    Title = "經建成長與資源配置諮議",
                    Desc = "伴隨台灣農業改革與工業升級，民間儲蓄與海外僑資逐步回流；財經與國防部門就新一階段預算優先順序向總統提出建議。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "擴大民生投資與出口加工產業，厚植經濟體質",
                            Hint = "GDP 成長 +6%、民心 +4",
                            Effect = st => {
                                st.Gdp *= 1.06; st.Morale += 4;
                                SimEngine.Log(st, "政府加大民間工業與出口設施投入，經濟繁榮躍進！", "good");
                            }
                        },
                        new() {
                            Label = "發行愛國建設公債，將民間游資轉入國庫與戰備儲糧",
                            Hint = "國庫 +$0.8 億、補給 +10，民心 -3",
                            Effect = st => {
                                st.Treasury += 0.8; st.Supply += 10; st.Morale -= 3;
                                SimEngine.Log(st, "財政部完成公債募資，國庫銀彈與前線戰備補給獲得充實。", "");
                            }
                        },
                        new() {
                            Label = "設立國防科研與理工獎學金專案，加速三軍技術研發",
                            Hint = $"花費國庫 ${(0.4 * px):F1} 億；科研進度 +2.5、陸海空科技各 +0.2",
                            Effect = st => {
                                SpendTreasury(st, 0.4);
                                st.ResearchProg += 2.5;
                                st.ArmyTech += 0.2; st.NavyTech += 0.2; st.AirTech += 0.2;
                                SimEngine.Log(st, "國防科研專案帶動軍工與基礎工程突破，三軍技術同步提升。", "good");
                            }
                        }
                    }
                }),
                ("scholars_return", true, () => new GameEvent
                {
                    Id = $"dyn_scholars_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "內政科研",
                    Title = "旅外學人暨理工人才返國投效國防與經建",
                    Desc = "一批留學歐美與日本的物理、機械與經濟學人響應號召返台任教或參與工研單位，教育部與中研院呈請定奪其首要研究方向。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "延攬進入國防科研計畫，加速當前核心專案突破",
                            Hint = $"國庫 -${(0.3 * px):F1} 億；當前研發進度 +3.2、常年科研產出加成提升",
                            Effect = st => {
                                SpendTreasury(st, 0.3);
                                st.ResearchProg += 3.2; st.RpBonus += 0.5;
                                SimEngine.Log(st, "海外歸國學人進駐研究機構，關鍵工程難題連日攻克！", "good");
                            }
                        },
                        new() {
                            Label = "投入電力、紡織與電子加工產業規劃，拉動長期經濟成長",
                            Hint = "GDP 成長 +5%、經濟效率 +3%、民心 +3",
                            Effect = st => {
                                st.Gdp *= 1.05; st.EconMult += 0.03; st.Morale += 3;
                                SimEngine.Log(st, "科技人才轉化為產業升級引擎，工業出口與就業率顯著攀升！", "good");
                            }
                        }
                    }
                }),
                ("eca_quota", true, () => new GameEvent
                {
                    Id = $"dyn_eca_quota_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "內政財經",
                    Title = "外援物資與民生物價平抑配額協調",
                    Desc = "軍需油料消耗居高不下之際，行政院財經小組憂心糧價與匯率波動影響後方安定，呈請核定本季麵粉、鋼料與外匯配額重心。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "優先穩定後方糧價、水利電力與新台幣幣值",
                            Hint = "民心 +6、GDP +4%、國庫 +$0.4 億",
                            Effect = st => {
                                st.Morale += 6; st.Gdp *= 1.04; st.Treasury += 0.4;
                                SimEngine.Log(st, "物價迅速平穩，後方農工生產秩序與民心安定度全面提升。", "good");
                            }
                        },
                        new() {
                            Label = "偏重軍用燃料、特種鋼材與彈藥零件進口儲備",
                            Hint = "補給 +14、兵源 +1.5，民心 -2",
                            Effect = st => {
                                st.Supply += 14; st.Manpower += 1.5; st.Morale -= 2;
                                SimEngine.Log(st, "軍工原料與野戰油彈優先入庫，三軍連續作戰儲備倍增。", "");
                            }
                        }
                    }
                })
            };
            return PickRotatedTemplate(s, "econ_tpl_", pool, 24);
        }

        private static GameEvent BuildDiploVarietyEvent(GameState s, FrontlineContext ctx, double px)
        {
            var pool = new List<(string key, bool ok, Func<GameEvent> build)>
            {
                ("overseas_aid", s.Morale >= 40 || ctx.HeldMainland.Count >= 1, () => new GameEvent
                {
                    Id = $"dyn_overseas_chinese_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "外交僑務",
                    Title = "海外僑團聯合致敬與愛國捐獻",
                    Desc = "東南亞與北美華僑領袖發起愛國募款並組團返國勞軍，請示這批僑匯應優先用於前線軍備補實，或是於華府及西方國會展開政治游說。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "專款用於添購軍需補給與前線裝備",
                            Hint = "國庫 +$0.6 億、補給 +8、民心 +3",
                            Effect = st => {
                                st.Treasury += 0.6; st.Supply += 8; st.Morale += 3;
                                SimEngine.Log(st, "海外僑胞募款如期匯入國庫，化作前線將士所需補給與器材！", "good");
                            }
                        },
                        new() {
                            Label = "動員僑界展開美國國會與西方友邦外交游說",
                            Hint = "美國關係 +6、國際地位 +5",
                            Effect = st => {
                                st.UsRel += 6; st.Intl += 5;
                                SimEngine.Log(st, "僑界與國際友人協助發聲，自由世界對我方聲援明顯升溫。", "good");
                            }
                        }
                    }
                }),
                ("un_lobby", s.Year < 1990, () => new GameEvent
                {
                    Id = $"dyn_un_lobby_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "國際外交",
                    Title = "聯合國大會會期外交代表權攻防戰",
                    Desc = "本屆聯大開議在即，若干第三世界與歐亞國家再度串聯提案討論中國代表權問題，駐聯合國代表團與友邦大使急請台北指示外交籌碼方向。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "派出農耕技術與經貿訪問團馳援友邦，固票保住安理會席次",
                            Hint = $"國庫 -${(0.35 * px):F1} 億；國際地位 +8、美國關係 +4",
                            Effect = st => {
                                SpendTreasury(st, 0.35);
                                st.Intl += 8; st.UsRel += 4;
                                SimEngine.Log(st, "我方農技外交獲得亞非拉多國票源力挺，順利守穩多邊外交防線！", "good");
                            }
                        },
                        new() {
                            Label = "於聯大揭露對岸迫害與鐵幕內情，化被動為主動爭取西方民意",
                            Hint = "民心 +5、國際地位 +4、中共穩定度 -4",
                            Effect = st => {
                                st.Morale += 5; st.Intl += 4; st.PrcStab -= 4;
                                SimEngine.Log(st, "我國代表於國際講壇公布大陸實況，引發全球自由媒體廣泛關注。", "good");
                            }
                        }
                    }
                }),
                ("press_tour", true, () =>
                {
                    string fwdName = GameData.Nodes[ctx.PickForwardRocNode(s)].Name;
                    return new GameEvent
                    {
                        Id = $"dyn_press_tour_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "國際輿論",
                        Title = $"西方戰地記者團申請赴【{fwdName}】前線採訪",
                        Desc = $"多家英美主流通訊社與攝影記者聯名申請登島／前進【{fwdName}】陣地採訪，新聞局力主藉此爭取國際同情，但軍情單位顧慮暴露布防位置。",
                        Choices = new List<EventChoice>
                        {
                            new() {
                                Label = "由新聞局安排記者團參訪精銳駐軍與民生實況，主動發聲",
                                Hint = "國際地位 +6、美國關係 +5、民心 +3",
                                Effect = st => {
                                    st.Intl += 6; st.UsRel += 5; st.Morale += 3;
                                    SimEngine.Log(st, $"國際新聞界實地報導{fwdName}軍民堅毅備戰風貌，西方援華民氣明顯提升。", "good");
                                }
                            },
                            new() {
                                Label = "嚴守軍事保密原則，謝絕實地拍攝僅發布官方戰報",
                                Hint = "戰場迷霧／情報安全無虞；前線防衛整備 +3%，外交曝光持平",
                                Effect = st => {
                                    st.BattleBonus += 0.02;
                                    foreach (var u in SimEngine.UnitsAt(st, ctx.PickForwardRocNode(st), UnitDomain.Land))
                                        u.Str = Math.Min(100, u.Str + 4);
                                    SimEngine.Log(st, "軍方貫徹保密防諜規範，確保前線砲兵與兵力部署不被外洩。", "");
                                }
                            }
                        }
                    };
                })
            };
            return PickRotatedTemplate(s, "diplo_tpl_", pool, 24);
        }

        private static GameEvent BuildIntelSocietyVarietyEvent(
            GameState s, FrontlineContext ctx, string enemyTerm, List<string> frontTargets, string ftNames, double px)
        {
            var pool = new List<(string key, bool ok, Func<GameEvent> build)>
            {
                ("spy_ring", true, () => new GameEvent
                {
                    Id = $"dyn_spy_ring_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "情治內政",
                    Title = "情治單位破獲潛伏密電小組！",
                    Desc = "保密局暨警備監偵網截獲一處隱藏於都市民宅的秘密發報電台，正與敵方華東情報網單線通聯，搜獲密碼本與潛伏聯絡名單。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "立即逮捕全數涉案人員並切斷滲透網，公告安定後方治安",
                            Hint = "民心 +4、補給免遭破壞（+4），軍事治安風險消除",
                            Effect = st => {
                                st.Morale += 4; st.Supply += 4;
                                SimEngine.AddIntel(st, "反諜戰果", "alert", "情治單位順利拔除潛伏密電小組，截獲多份對岸密碼通聯紀要。");
                                SimEngine.Log(st, "後方特務組織遭到連根拔除，軍政機密安全獲得確保。", "good");
                            }
                        },
                        new() {
                            Label = "秘而不宣！改行「將計就計」反向餵送假情資誤導對岸部署",
                            Hint = $"科研進度 +1.6、{ftNames}敵後動盪 +10、前線敵軍防務受誤導削弱",
                            Effect = st => {
                                st.ResearchProg += 1.6;
                                ChipHeld(st, frontTargets, 0.08);
                                foreach (var id in frontTargets)
                                {
                                    if (st.Control[id] != Faction.ROC)
                                        st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 10, 0, 100);
                                }
                                SimEngine.Log(st, "我方成功策反雙面電台誤導敵情研判，使敵軍調防陷入混亂！", "big");
                            }
                        }
                    }
                }),
                ("refugee_wave", ctx.PrcNodes.Count > 0, () => new GameEvent
                {
                    Id = $"dyn_refugee_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "社會救濟",
                    Title = "東南難民暨投奔自由潮湧現（港澳與沿海轉進）",
                    Desc = "大批難民不顧海上封鎖湧向港澳與前哨島嶼，其中不乏熟悉內地糧倉、機場與工廠現況的技術工與退伍青壯，救總請示安置方針。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "撥款興建眷舍與職業訓導處，吸納青壯編補兵源與技工",
                            Hint = $"國庫 -${(0.30 * px):F1} 億；兵源 +3.2、民心 +5、{ftNames}情報掌握 +8",
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                st.Manpower += 3.2; st.Morale += 5;
                                foreach (var id in frontTargets)
                                    if (st.Control[id] != Faction.ROC) st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 8, 0, 100);
                                SimEngine.Log(st, "救總妥善安置來歸難民，獲補充寶貴兵源並掌握對岸社會實情。", "good");
                            }
                        },
                        new() {
                            Label = "透過港澳救災渠道兼辦糧食救濟，爭取國際人道援助捐款",
                            Hint = "國際地位 +5、國庫 +$0.4 億、補給 +5",
                            Effect = st => {
                                st.Intl += 5; st.Treasury += 0.4; st.Supply += 5;
                                SimEngine.Log(st, "人道救濟贏得國際讚譽，西方救災基金會與僑界踴躍響應。", "good");
                            }
                        }
                    }
                }),
                ("psy_radio", ctx.PrcNodes.Count > 0, () => new GameEvent
                {
                    Id = $"dyn_psy_radio_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "心戰文宣",
                    Title = ctx.Theater == FrontlineTheater.StraitStandoff
                        ? $"海峽大功率短波廣播暨高空氣球心戰攻勢（對準{ftNames}）"
                        : $"全頻短波廣播暨高空氣球心戰攻勢（對準{ftNames}）",
                    Desc = $"心戰團隊研發高空季風氣球與大功率中短波電台，可將生活物資、通行證與《告同胞書》直投【{ftNames}】腹地，但電台功率與預算有限。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "趁季風順風大規模施放空飄物資包與免責投誠證",
                            Hint = $"補給 -4；{ftNames}動盪 +14、中共穩定度 -4",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 4);
                                st.PrcStab -= 4;
                                foreach (var id in frontTargets)
                                    if (st.Control[id] != Faction.ROC) st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 14, 0, 100);
                                SimEngine.Log(st, $"數萬只心戰空飄氣球飛越【{ftNames}】，基層軍民紛紛私藏傳單。", "good");
                            }
                        },
                        new() {
                            Label = "強化夜間藝文播音與海外華語廣播網，打通敵後秘密電訊",
                            Hint = "國際聲望 +3、科研進度 +1.3、民心 +3",
                            Effect = st => {
                                st.Intl += 3; st.ResearchProg += 1.3; st.Morale += 3;
                                SimEngine.Log(st, "短波播音突破干擾覆蓋東南大城，維繫大批同情國府地下群眾。", "good");
                            }
                        }
                    }
                }),
                ("cabinet_debate", true, () => new GameEvent
                {
                    Id = $"dyn_cabinet_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "中樞政情",
                    Title = "行政院暨國防會議「建軍與經建優先序」路線之爭",
                    Desc = "中樞會議上，財經文官力主壓低赤字並扶持工業出口、留住外匯，三軍將領則呼籲擴充常備野戰師與裝備更新。兩派呈請最高統帥折衝定奪。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "採納財經技術派方案：優先扶植工業產值與科研底座",
                            Hint = "GDP +5%、科研進度 +2.0、民心 +2",
                            Effect = st => {
                                st.Gdp *= 1.05; st.ResearchProg += 2.0; st.Morale += 2;
                                SimEngine.Log(st, "行政院獲准加大工業建廠與基礎研發投入，國力基底厚植。", "good");
                            }
                        },
                        new() {
                            Label = "力挺強軍備戰路線：增補前線彈藥與部隊妥善率",
                            Hint = $"國庫 -${(0.35 * px):F1} 億；補給 +12、陸海空主力戰力 +5%",
                            Effect = st => {
                                SpendTreasury(st, 0.35);
                                st.Supply += 12;
                                foreach (var u in st.Units) u.Str = Math.Min(100, u.Str + 5);
                                SimEngine.Log(st, "統帥部定調備戰優先，三軍裝備檢修與戰備儲糧迅速到位。", "good");
                            }
                        }
                    }
                })
            };
            return PickRotatedTemplate(s, "intelsoc_tpl_", pool, 24);
        }

        private static GameEvent BuildPrcCrackVarietyEvent(GameState s, FrontlineContext ctx, string ftNames)
        {
            var targets = ctx.PrcFrontNodes.Count > 0 ? ctx.PrcFrontNodes.Take(3).ToList() : ctx.PrcNodes.Take(3).ToList();
            string tNames = string.Join("、", targets.Select(id => GameData.Nodes[id].Name));
            var pool = new List<(string key, bool ok, Func<GameEvent> build)>
            {
                ("famine_unrest", true, () => new GameEvent
                {
                    Id = $"dyn_prc_unrest_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "對岸政局",
                    Title = $"對岸【{tNames}】基層糧荒與抗徵交糧風潮！",
                    Desc = $"前線情報證實【{tNames}】因過度徵糧支應戰費與集體化失調，鄉鎮連傳抗繳與地方基層陽奉陰違。我方擬趁機介入擴大裂痕。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "密令地下人員串聯抗暴群眾並收買邊區治安幹部",
                            Hint = $"國庫 -${(0.30 * SimEngine.PriceIndex(s)):F1} 億；{tNames}動盪 +16、敵守軍約 -9%、中共穩定度 -5",
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                st.PrcStab -= 5;
                                ChipHeld(st, targets, 0.09);
                                foreach (var id in targets)
                                {
                                    if (st.Control[id] == Faction.PRC)
                                        st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 16, 0, 100);
                                }
                                SimEngine.Log(st, $"【{tNames}】抗徵糧浪潮愈演愈烈，共軍被迫抽兵維持後方秩序。", "good");
                            }
                        },
                        new() {
                            Label = "把握民心向背契機，廣播宣示「收復後減免賦稅」爭取歸附",
                            Hint = $"國際地位 +3、民心 +4、{tNames}動盪 +9",
                            Effect = st => {
                                st.Intl += 3; st.Morale += 4;
                                foreach (var id in targets)
                                    if (st.Control[id] == Faction.PRC) st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 9, 0, 100);
                                SimEngine.Log(st, "國府復土安民宣言在對岸民間口耳相傳，反戰情緒擴散。", "good");
                            }
                        }
                    }
                }),
                ("tech_defector", true, () => new GameEvent
                {
                    Id = $"dyn_prc_defector_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "敵後密線",
                    Title = $"敵方海空技勤與譯電人員透過境外情報站秘密投靠",
                    Desc = $"情報處密報：對岸駐守【{tNames}】方向的數名雷達技官與譯電校級軍官因不滿政治審查與清洗，試圖透過港澳管道與我方接洽！",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "核發重金與安家保證，秘密接應其携帶密本與裝備來歸",
                            Hint = $"國庫 -${(0.40 * SimEngine.PriceIndex(s)):F1} 億；空海軍科技各 +0.25、科研進度 +2.0",
                            Effect = st => {
                                SpendTreasury(st, 0.40);
                                st.AirTech += 0.25; st.NavyTech += 0.25; st.ResearchProg += 2.0;
                                SimEngine.AddIntel(st, "敵後線索", "good", $"境外秘密情報站成功接應【{tNames}】敵營技術軍官携電訊參數來歸。");
                                SimEngine.Log(st, "敵方關鍵雷達與通訊參數順利入手，情蒐與空防戰力大增！", "big");
                            }
                        },
                        new() {
                            Label = "令其就地潛伏充當長期內線，專供我軍前線調動情資",
                            Hint = $"{tNames}共軍佈防暴露削弱（約 -10%）、中共穩定度 -4",
                            Effect = st => {
                                st.PrcStab -= 4;
                                ChipHeld(st, targets, 0.10);
                                SimEngine.AddIntel(st, "密謀警示", "good", $"潛伏內線定期回報【{tNames}】兵力輪調明細，前線動向一目了然。");
                                SimEngine.Log(st, $"我方高層內線持續回傳【{tNames}】部署情報，前線敵情完全透明。", "good");
                            }
                        }
                    }
                })
            };
            return PickRotatedTemplate(s, "prccrack_tpl_", pool, 24);
        }

        private static GameEvent BuildTheaterFrontEvent(
            GameState s, FrontlineContext ctx, string enemyTerm, List<string> frontTargets, string ftNames, string fwdRoc, string fwdRocName, double px)
        {
            var (evTitle, evDesc) = ctx.Theater switch
            {
                FrontlineTheater.NorthernBorderAntiUssr => (
                    $"北疆抗俄前線（{ftNames}）塞外反攻態勢會商",
                    $"國軍主力已推進至【{fwdRocName}】與塞外邊防，前方正是{enemyTerm}盤據之【{ftNames}】。前敵司令部就耐寒重裝突穿與後勤鐵道整補呈請聖裁！"
                ),
                FrontlineTheater.NorthChinaPlain => (
                    $"華北中原前線（{ftNames}）重裝推進暨補給線決戰",
                    $"戰線已全面推進至黃淮華北！王師自【{fwdRocName}】直逼{enemyTerm}守軍重鎮【{ftNames}】，北方鐵道網與港口運補面臨決勝考驗。"
                ),
                FrontlineTheater.YangtzeCentral => (
                    $"江淮漢水戰區（{ftNames}）橫渡天塹暨機械化突破會議",
                    $"東南橋頭堡已連成一片，前線大軍正由【{fwdRocName}】向江淮腹地【{ftNames}】進逼，急需統帥部拍板突破方向與兵力支援！"
                ),
                FrontlineTheater.SouthEastCoast => (
                    $"華南橋頭堡（{ftNames}）擴大縱深暨敵後破襲協調會",
                    $"我軍立足大陸南端【{fwdRocName}】，前方【{ftNames}】{enemyTerm}重兵集結反撲，請裁示集中優勢火力破陣或擴大游擊滲透。"
                ),
                _ => (
                    $"台海外島（{fwdRocName}）前哨砲戰暨對岸（{ftNames}）壓制決斷",
                    $"前沿守軍於【{fwdRocName}】與隔岸【{ftNames}】{enemyTerm}砲兵陣地持續對峙，前線請求增撥反砲兵雷達或發動特戰襲擾。"
                )
            };
            return new GameEvent
            {
                Id = $"dyn_theater_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "戰區動態",
                Title = evTitle,
                Desc = evDesc,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = $"發動前線砲兵與裝甲重拳突穿，強壓【{ftNames}】{enemyTerm}防線",
                        Hint = $"國庫 -${(0.45 * px):F1} 億、補給 -4；{ftNames}{enemyTerm}守軍約 -10%、地面戰鬥 +3%",
                        Effect = st => {
                            SpendTreasury(st, 0.45);
                            st.Supply = Math.Max(0, st.Supply - 4);
                            st.BattleBonus += 0.03;
                            ChipHeld(st, frontTargets, 0.10);
                            SimEngine.Log(st, $"★ 我軍重裝火力猛襲【{ftNames}】{enemyTerm}防線，前線將士乘勢擴大突破口！", "big");
                        }
                    },
                    new() {
                        Label = $"鞏固【{fwdRocName}】前進兵站並收編地方反抗游擊",
                        Hint = $"於{fwdRocName}編成 1 支民兵並獲補給 +6、{ftNames}敵後動盪 +10",
                        Effect = st => {
                            st.Supply += 6;
                            SimEngine.AddUnit(st, "mil", fwdRoc, 85);
                            foreach (var tid in frontTargets)
                                if (st.Control[tid] != Faction.ROC) st.Unrest[tid] = SimEngine.Clamp(st.Unrest[tid] + 10, 0, 100);
                            SimEngine.Log(st, $"{fwdRocName}前進兵站擴編完成，並牽動【{ftNames}】敵後響應！", "good");
                        }
                    }
                }
            };
        }

        private static void SpendTreasury(GameState st, double baseCost)
            => st.Treasury = Math.Max(0, st.Treasury - baseCost * SimEngine.PriceIndex(st));

        private static string PickForwardIsland(GameState s)
        {
            var owned = new[] { "KM", "HI", "DC", "MZ", "ZS", "PH" }
                .Where(id => s.Control.TryGetValue(id, out var f) && f == Faction.ROC)
                .ToList();
            if (owned.Count == 0) return null;
            return owned[(int)(SimEngine.Rng(s) * owned.Count)];
        }

        public static GameEvent CreateMilitaryDeploymentEvent(GameState s)
        {
            var ctx = AnalyzeFrontline(s);
            var held = ctx.HeldMainland;
            string enemyTerm = ctx.EnemyLabel(s);
            string islandId = PickForwardIsland(s);
            string islandName = islandId != null ? GameData.Nodes[islandId].Name : "";
            string islandZone = islandId != null ? GameData.Nodes[islandId].Zones[0] : "Z3";
            string islandZoneName = GameData.Zones[islandZone].Name;
            string heldId = ctx.RocFrontNodes.Count > 0
                ? ctx.RocFrontNodes[(int)(SimEngine.Rng(s) * Math.Min(3, ctx.RocFrontNodes.Count))]
                : (held.Count > 0 ? held[(int)(SimEngine.Rng(s) * held.Count)] : null);
            string heldName = heldId != null ? GameData.Nodes[heldId].Name : "";
            string fwdRoc = ctx.PickForwardRocNode(s);
            string fwdRocName = GameData.Nodes[fwdRoc].Name;
            var frontTargets = ctx.HostileFrontNodes.Take(4).ToList();
            string frontNames2 = ctx.FrontNames(2);
            string frontNames3 = ctx.FrontNames(3);
            string zoneId = ctx.PrimaryZoneId;
            string zoneName = ctx.PrimaryZoneName;
            bool coastalFront = ctx.Theater is FrontlineTheater.StraitStandoff or FrontlineTheater.SouthEastCoast;
            double px = SimEngine.PriceIndex(s);

            var pool = new List<(string key, bool ok, Func<GameEvent> build)>
            {
                ("petition", true, () => new GameEvent
                {
                    Id = $"dyn_mil_petition_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = $"參謀本部聯合作戰檢討會議（{ctx.TheaterTitle}）",
                    Desc = $"參謀總長邀集陸海空三軍將領就【{ctx.TheaterTitle}】態勢呈報季度戰備評估，針對前線主攻操演、{frontNames2}敵後心戰情報與部隊休整提出三項方針。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = ctx.ContestedZones.Count > 0 && !ctx.OnlyUssrRemains
                                ? $"實施{zoneName}海空聯合兩棲搶灘實兵操演"
                                : $"實施【{fwdRocName}】前線步戰砲聯合攻堅實兵操演",
                            Hint = ctx.ContestedZones.Count > 0 && !ctx.OnlyUssrRemains
                                ? $"國庫 -${(0.45 * px):F1} 億；兩棲登陸加成 +3%、民心 +4"
                                : $"國庫 -${(0.45 * px):F1} 億；地面作戰加成 +3%、民心 +4",
                            Effect = st => {
                                SpendTreasury(st, 0.45);
                                if (ctx.ContestedZones.Count > 0 && !ctx.OnlyUssrRemains) st.LandingBonus += 0.03;
                                else st.BattleBonus += 0.03;
                                st.Morale += 4;
                                SimEngine.Log(st, $"三軍順利完成【{ctx.TheaterTitle}】聯合實兵演習，前線突擊默契大幅增強。", "good");
                            }
                        },
                        new() {
                            Label = $"增撥【{frontNames2}】前線敵後地下工作與心戰廣播經費",
                            Hint = $"國庫 -${(0.35 * px):F1} 億；{frontNames3}動盪 +10、削弱{enemyTerm}前方防務",
                            Effect = st => {
                                SpendTreasury(st, 0.35);
                                foreach (var id in frontTargets)
                                    if (st.Control[id] != Faction.ROC)
                                        st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 10, 0, 100);
                                if (!ctx.OnlyUssrRemains && ctx.PrcNodes.Count > 0) st.PrcStab -= 3;
                                else
                                {
                                    foreach (var id in frontTargets.Take(2))
                                        if (st.Control[id] != Faction.ROC) st.Garrison[id] = Math.Max(0.5, st.Garrison[id] * 0.94);
                                }
                                SimEngine.Log(st, $"情報站針對【{frontNames3}】擴大敵後短波與策反心戰，{enemyTerm}後方動盪升高。", "good");
                            }
                        },
                        new() {
                            Label = "改善官兵與眷屬給養配給，展開前線部隊休整補員",
                            Hint = $"國庫 -${(0.30 * px):F1} 億；兵源 +2.2、全軍部隊戰力 +4%",
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                st.Manpower += 2.2;
                                foreach (var u in st.Units) u.Str = Math.Min(100, u.Str + 4);
                                SimEngine.Log(st, "統帥部撥款改善軍眷照顧與部隊給養，前線士官兵戰力回升！", "good");
                            }
                        }
                    }
                }),
                ("outer_island", islandId != null && coastalFront, () => new GameEvent
                {
                    Id = $"dyn_mil_island_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = $"{islandName}前沿要塞暨{islandZoneName}海空輪防電文",
                    Desc = $"前線指揮官急電呈報{islandName}防務與對岸【{frontNames2}】態勢，請統帥部核定地下坑道儲備、{islandZoneName}巡弋強度與守軍輪調方針。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"搶建{islandName}地下坑道庫房與前沿反砲兵掩體",
                            Hint = $"國庫 -${(0.35 * px):F1} 億；補給 +8、{islandName}守軍戰力 +8%、登陸協同 +2%",
                            Effect = st => {
                                SpendTreasury(st, 0.35);
                                st.Supply += 8; st.LandingBonus += 0.02;
                                foreach (var u in st.Units.Where(x => x.Loc == islandId)) u.Str = Math.Min(100, u.Str + 8);
                                SimEngine.Log(st, $"{islandName}要塞地下化工程與前沿油彈囤儲完工，固若金湯！", "good");
                            }
                        },
                        new() {
                            Label = $"派遣艦隊與戰機編隊前推{islandZoneName}常態巡弋威懾",
                            Hint = $"補給 -4；海軍與空軍科技各 +0.2、{islandZoneName}海空兵力戰力 +8%",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 4);
                                st.NavyTech += 0.2; st.AirTech += 0.2;
                                foreach (var u in st.Units.Where(x => x.Loc == islandZone)) u.Str = Math.Min(100, u.Str + 8);
                                SimEngine.Log(st, $"我方海空兵力前出{islandZoneName}加強巡弋，有效壓制{enemyTerm}艦機挑釁。", "good");
                            }
                        },
                        new() {
                            Label = $"實施{islandName}守軍精銳換防整訓，補足前線欠員",
                            Hint = $"國庫 -${(0.25 * px):F1} 億；兵源 +1.6、民心 +4、{islandName}地面部隊戰力 +12%",
                            Effect = st => {
                                SpendTreasury(st, 0.25);
                                st.Manpower += 1.6; st.Morale += 4;
                                foreach (var u in st.Units.Where(x => x.Loc == islandId && GameData.UnitTypes[x.Type].Domain == UnitDomain.Land))
                                    u.Str = Math.Min(100, u.Str + 12);
                                SimEngine.Log(st, $"{islandName}完成部隊換防整訓，駐島官兵士氣高昂。", "good");
                            }
                        }
                    }
                }),
                ("amphib_prep", ctx.ContestedZones.Count > 0 && !ctx.OnlyUssrRemains, () => new GameEvent
                {
                    Id = $"dyn_mil_amphib_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = $"{zoneName}兩棲突擊船團調度暨登陸艦整訓電令",
                    Desc = $"作戰次長室針對{zoneName}方向（{frontNames2}）兩棲輸運與跨海轉進瓶頸呈報緊急預案：可徵用商輪擴編船團，或強化登陸艦與前進港埠載具。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = "徵調民間商輪改裝為兩棲輸運船團，擴大跨海投送能量",
                            Hint = $"國庫 -${(0.50 * px):F1} 億；兩棲總運力 +2 師、民心 -1",
                            Effect = st => {
                                SpendTreasury(st, 0.50);
                                st.LiftBonus += 2; st.Morale -= 1;
                                SimEngine.Log(st, $"航運界配合完成船團整編，{zoneName}跨海投送運力大幅擴充！", "good");
                            }
                        },
                        new() {
                            Label = "撥款整修海軍登陸艦隊並演練多波次搶灘接駁",
                            Hint = $"國庫 -${(0.35 * px):F1} 億；兩棲登陸加成 +4%、海軍艦隊戰力 +6%",
                            Effect = st => {
                                SpendTreasury(st, 0.35);
                                st.LandingBonus += 0.04;
                                foreach (var u in st.Units.Where(x => x.Type == "fleet")) u.Str = Math.Min(100, u.Str + 6);
                                SimEngine.Log(st, "海軍登陸艦隊完成搶灘協調整訓，突擊效率顯著提升。", "good");
                            }
                        },
                        new() {
                            Label = "擴建前方港口轉運碼頭與工兵野戰棧橋",
                            Hint = $"國庫 -${(0.40 * px):F1} 億；海外後勤吞吐上限 +2、登陸加成 +2%、補給 +5",
                            Effect = st => {
                                SpendTreasury(st, 0.40);
                                st.SupplyCap += 2; st.LandingBonus += 0.02; st.Supply += 5;
                                SimEngine.Log(st, "後勤工兵整備妥善野戰碼頭與轉運駁船，強力支撐前線長期作戰。", "good");
                            }
                        }
                    }
                }),
                ("rail_logistics", held.Count >= 1, () => new GameEvent
                {
                    Id = $"dyn_mil_rail_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = $"【{ctx.TheaterTitle}】前進鐵道暨摩托化重裝運補部署電令",
                    Desc = $"隨著王師戰線推向【{fwdRocName}】並與{frontNames2}敵軍對峙，陸上幹線鐵路搶通與前線油彈車隊已成為決定【{ctx.TheaterTitle}】攻勢節奏的關鍵。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"動員鐵道兵搶通通往{fwdRocName}之雙軌樞紐與兵站調度場",
                            Hint = $"國庫 -${(0.45 * px):F1} 億；海外後勤吞吐上限 +2、補給 +10、地上作戰 +2%",
                            Effect = st => {
                                SpendTreasury(st, 0.45);
                                st.SupplyCap += 2; st.Supply += 10; st.BattleBonus += 0.02;
                                SimEngine.Log(st, $"通往【{fwdRocName}】的戰略鐵道幹線全線暢通，大軍北進後勤無虞！", "good");
                            }
                        },
                        new() {
                            Label = "配發摩托化野戰油料縱隊與前線砲兵彈藥基數",
                            Hint = $"國庫 -${(0.35 * px):F1} 億；{fwdRocName}與前線陸軍戰力 +9%、陸軍科技 +0.2",
                            Effect = st => {
                                SpendTreasury(st, 0.35);
                                st.ArmyTech += 0.2;
                                foreach (var u in st.Units.Where(x => ctx.RocFrontNodes.Contains(x.Loc) || x.Loc == fwdRoc))
                                    u.Str = Math.Min(100, u.Str + 9);
                                SimEngine.Log(st, $"摩托化運補縱隊馳抵{fwdRocName}前線，精銳主力裝甲步兵飽滿待機！", "good");
                            }
                        },
                        new() {
                            Label = "於前方野戰樞紐架設野戰修理廠與戰地醫院",
                            Hint = $"國庫 -${(0.30 * px):F1} 億；兵源 +2.0、民心 +4、全軍戰力 +5%",
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                st.Manpower += 2.0; st.Morale += 4;
                                foreach (var u in st.Units) u.Str = Math.Min(100, u.Str + 5);
                                SimEngine.Log(st, "前方戰地修理廠與醫護體系運作順暢，三軍持續戰力明顯增強。", "good");
                            }
                        }
                    }
                }),
                ("radar_air", true, () => new GameEvent
                {
                    Id = $"dyn_mil_radar_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = ctx.ContestedZones.Count > 0 && !ctx.OnlyUssrRemains
                        ? $"{zoneName}至{frontNames2}高空電偵暨夜間攔截部署"
                        : $"【{ctx.TheaterTitle}】{frontNames2}前線空偵與野戰防空部署",
                    Desc = $"空軍作戰司令部指出【{frontNames3}】一帶{enemyTerm}雷達與野戰機場近期活動頻繁，請示優先執行高空偵照滲透、或強化前方機場與野戰防空攔截網。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"派遣高空偵照機測繪{frontNames2}敵軍集結區與防空陣地",
                            Hint = $"補給 -3；科研進度 +1.8、空軍科技 +0.2、{frontNames3}敵後動盪 +6",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 3);
                                st.ResearchProg += 1.8; st.AirTech += 0.2;
                                foreach (var tid in frontTargets.Take(3))
                                    if (st.Control[tid] != Faction.ROC)
                                        st.Unrest[tid] = SimEngine.Clamp(st.Unrest[tid] + 6, 0, 100);
                                SimEngine.Log(st, $"高空偵照編隊帶回【{frontNames3}】完整圖資，情報部門與空軍精準鎖定目標！", "good");
                            }
                        },
                        new() {
                            Label = "強化防空飛彈陣地聯網並整補空軍攔截聯隊戰力",
                            Hint = $"國庫 -${(0.30 * px):F1} 億；本島防空 +8%、民心 +3、空軍聯隊戰力 +8%",
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                st.AirDefense += 0.08; st.Morale += 3;
                                foreach (var u in st.Units.Where(x => x.Type == "air")) u.Str = Math.Min(100, u.Str + 8);
                                SimEngine.Log(st, "我軍防空與前線空中攔截聯網整備就緒，制空信心大增。", "good");
                            }
                        }
                    }
                }),
                ("special_ops", ctx.HostileFrontNodes.Count > 0, () => new GameEvent
                {
                    Id = $"dyn_mil_specops_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = ctx.OnlyUssrRemains
                        ? $"北疆【{frontNames2}】敵後特戰滲透暨補給破壞部署會議"
                        : (coastalFront
                            ? $"敵後海岸突擊暨【{frontNames2}】滲透作戰部署會議"
                            : $"敵後鐵道破襲暨【{frontNames2}】空降滲透作戰部署會議"),
                    Desc = $"情治與特戰部門彙整【{frontNames3}】一帶{enemyTerm}防務薄弱節點，請總統裁示本季優先投入特戰突擊、秘密空投接濟敵後抗暴網，或電訊破譯整備。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"派遣特戰突擊隊夜襲破壞【{frontNames2}】敵軍補給站與彈藥庫",
                            Hint = $"補給 -4；{frontNames3}{enemyTerm}守軍各削弱 6%、民心 +3",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 4); st.Morale += 3;
                                foreach (var id in frontTargets.Take(3))
                                    if (st.Control[id] != Faction.ROC)
                                        st.Garrison[id] = Math.Max(0.5, st.Garrison[id] * 0.94);
                                SimEngine.Log(st, $"★ 特戰突擊隊深入【{frontNames3}】引爆多座{enemyTerm}油彈庫，迫使敵軍後方戒備！", "good");
                            }
                        },
                        new() {
                            Label = $"秘密空投無線電台與軍火接濟【{frontNames3}】敵後義勇網",
                            Hint = $"國庫 -${(0.30 * px):F1} 億；{frontNames3}動盪 +12" + (ctx.OnlyUssrRemains ? $"、削弱{enemyTerm}後方控制" : "、中共穩定 -2"),
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                foreach (var id in frontTargets)
                                    if (st.Control[id] != Faction.ROC)
                                        st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 12, 0, 100);
                                if (!ctx.OnlyUssrRemains && ctx.PrcNodes.Count > 0) st.PrcStab -= 2;
                                else
                                {
                                    foreach (var id in frontTargets.Take(2))
                                        if (st.Control[id] != Faction.ROC) st.Garrison[id] = Math.Max(0.5, st.Garrison[id] * 0.95);
                                }
                                SimEngine.Log(st, $"【{frontNames3}】敵後聯絡網接獲空投補給，抗暴活動持續升溫！", "good");
                            }
                        },
                        new() {
                            Label = "擴編電訊破譯小組並優化三軍戰場協調與密語指管",
                            Hint = $"國庫 -${(0.25 * px):F1} 億；當前科研進度 +2.0、地面戰力加成 +2%",
                            Effect = st => {
                                SpendTreasury(st, 0.25);
                                st.ResearchProg += 2.0; st.BattleBonus += 0.02;
                                SimEngine.Log(st, "特種電訊破譯中心成果斐然，大幅縮短科研時程並提升部隊協同戰力。", "good");
                            }
                        }
                    }
                }),
                ("mainland_corridor", heldId != null, () => new GameEvent
                {
                    Id = $"dyn_mil_bridgehead_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = ctx.OnlyUssrRemains
                        ? $"北疆前線基地【{heldName}】抗俄補給走廊與衛戍部署"
                        : $"前線光復區【{heldName}】前進補給走廊與衛戍部署",
                    Desc = $"隨著王師扼守【{heldName}】直面【{frontNames2}】之{enemyTerm}主力，前敵總部急請台北裁定前進機場擴建、保鄉民兵編組或主力輪休方針。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"搶修{heldName}前進交通樞紐與野戰機場，打通大型補保走廊",
                            Hint = $"國庫 -${(0.45 * px):F1} 億；海外吞吐上限 +2、補給 +8、{heldName}駐軍戰力 +10%",
                            Effect = st => {
                                SpendTreasury(st, 0.45);
                                st.SupplyCap += 2; st.Supply += 8;
                                foreach (var u in st.Units.Where(x => x.Loc == heldId)) u.Str = Math.Min(100, u.Str + 10);
                                SimEngine.Log(st, $"{heldName}前進補給走廊暢通無阻，國軍對【{frontNames2}】投送能力再上層樓！", "good");
                            }
                        },
                        new() {
                            Label = $"號召{heldName}青壯成立「光復區義勇民兵」協防地方",
                            Hint = $"補給 -3；於{heldName}編成 1 支民兵部隊（戰力 85%）、民心 +4",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 3); st.Morale += 4;
                                if (st.Control[heldId] == Faction.ROC)
                                    SimEngine.AddUnit(st, "mil", heldId, 85);
                                SimEngine.Log(st, $"★ {heldName}熱血青年踴躍從軍編成民兵守備隊，解放主力師推進前線！", "good");
                            }
                        },
                        new() {
                            Label = $"調度醫護車隊與補充兵員赴{heldName}展開前線整補輪休",
                            Hint = $"國庫 -${(0.35 * px):F1} 億；兵源 +1.8、全軍戰力 +6%、民心 +3",
                            Effect = st => {
                                SpendTreasury(st, 0.35);
                                st.Manpower += 1.8; st.Morale += 3;
                                foreach (var u in st.Units) u.Str = Math.Min(100, u.Str + 6);
                                SimEngine.Log(st, $"{heldName}前線與各戰區完成梯次整補，三軍戰力顯著恢復。", "good");
                            }
                        }
                    }
                }),
                ("night_resupply", islandId != null && coastalFront, () => new GameEvent
                {
                    Id = $"dyn_mil_resupply_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = $"霧夜秘密航線暨{islandName}搶灘運補電令",
                    Desc = $"前線艦艇回報近期{islandZoneName}海霧濃重，後勤船團可趁夜加速滿載搶灘補充彈藥，或採護航驅逐艦分批穩妥接駁。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"把握夜霧掩護強行搶運大量油彈登陸{islandName}與前線",
                            Hint = $"補給 +12、{islandName}守軍戰力 +10%、民心 +3，艦隊略受損耗（-4%）",
                            Effect = st => {
                                st.Supply += 12; st.Morale += 3;
                                foreach (var u in st.Units.Where(x => x.Loc == islandId)) u.Str = Math.Min(100, u.Str + 10);
                                foreach (var u in st.Units.Where(x => x.Type == "fleet")) u.Str = Math.Max(20, u.Str - 4);
                                SimEngine.Log(st, $"運補船團連夜成功搶灘卸載，{islandName}前線彈藥補給充裕！", "good");
                            }
                        },
                        new() {
                            Label = "安排海軍護航編隊分批接駁運補並同步進廠保固船隻",
                            Hint = "補給 +6、海軍艦隊戰力 +8%、民心 +2",
                            Effect = st => {
                                st.Supply += 6; st.Morale += 2;
                                foreach (var u in st.Units.Where(x => x.Type == "fleet")) u.Str = Math.Min(100, u.Str + 8);
                                SimEngine.Log(st, "護航編隊穩妥完成分批補給接駁，艦隊妥善率明顯回升。", "good");
                            }
                        }
                    }
                }),
                ("mobile_defense", true, () => new GameEvent
                {
                    Id = $"dyn_mil_mobile_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = $"三軍機械化機動暨【{ctx.TheaterTitle}】預備動員演訓電文",
                    Desc = $"陸軍總司令部針對【{ctx.TheaterTitle}】與前方【{frontNames2}】戰場需求提報機動打擊演訓規劃，請示預算撥配重點。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"舉行針對【{frontNames2}】戰場之裝砲聯合實兵協同演習",
                            Hint = $"國庫 -${(0.45 * px):F1} 億、補給 -3；陸軍科技 +0.3、地面戰鬥 +2%",
                            Effect = st => {
                                SpendTreasury(st, 0.45);
                                st.Supply = Math.Max(0, st.Supply - 3);
                                st.ArmyTech += 0.3; st.BattleBonus += 0.02;
                                SimEngine.Log(st, $"三軍完成【{ctx.TheaterTitle}】聯合機動操演，砲兵與裝甲部隊協同火力更勝以往。", "good");
                            }
                        },
                        new() {
                            Label = "執行全國後備軍人動員點召，擴充兵源與軍需庫存",
                            Hint = $"國庫 -${(0.35 * px):F1} 億；兵源 +3.0、補給 +6",
                            Effect = st => {
                                SpendTreasury(st, 0.35);
                                st.Manpower += 3.0; st.Supply += 6;
                                SimEngine.Log(st, "各地團管區順利完成後備動員召集，前線補充兵源充沛！", "good");
                            }
                        },
                        new() {
                            Label = "將部分演訓專款轉撥兵工與通信科研專案",
                            Hint = $"國庫 -${(0.30 * px):F1} 億；當前科研進度 +2.4、民心 +2",
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                st.ResearchProg += 2.4; st.Morale += 2;
                                SimEngine.Log(st, "軍工科研部門獲專款奧援，各項國防科技進度大幅超前。", "good");
                            }
                        }
                    }
                }),
                ("front_artillery_clash", ctx.HostileFrontNodes.Count > 0, () => new GameEvent
                {
                    Id = $"dyn_mil_artillery_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = ctx.OnlyUssrRemains
                        ? $"北疆【{frontNames2}】重砲壓制暨抗俄先鋒部署電文"
                        : $"前線【{frontNames2}】砲兵制壓暨突擊突破部署電文",
                    Desc = $"駐【{fwdRocName}】砲兵指揮部測得【{frontNames3}】{enemyTerm}陣地集結動向，請求核撥特種砲彈基數或調動工兵協助開闢突破走廊。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"集中重砲與空中火力猛轟【{frontNames2}】之{enemyTerm}防線",
                            Hint = $"國庫 -${(0.40 * px):F1} 億、補給 -4；{frontNames3}{enemyTerm}守軍約 -10%、陸軍科技 +0.2",
                            Effect = st => {
                                SpendTreasury(st, 0.40);
                                st.Supply = Math.Max(0, st.Supply - 4);
                                st.ArmyTech += 0.2;
                                ChipHeld(st, frontTargets.Take(3), 0.10);
                                SimEngine.Log(st, $"我方重砲火力精準覆蓋【{frontNames3}】陣地，{enemyTerm}前線構築工事遭嚴重摧毀！", "good");
                            }
                        },
                        new() {
                            Label = $"強化{fwdRocName}前線裝甲掃雷工兵與戰術通信車輛",
                            Hint = $"國庫 -${(0.30 * px):F1} 億；地面戰力加成 +3%、{fwdRocName}部隊戰力 +8%",
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                st.BattleBonus += 0.03;
                                foreach (var u in st.Units.Where(x => x.Loc == fwdRoc))
                                    u.Str = Math.Min(100, u.Str + 8);
                                SimEngine.Log(st, $"{fwdRocName}前線部隊配發最新工兵破障裝備，隨時準備發動突擊！", "good");
                            }
                        }
                    }
                }),
                ("anti_ussr_northern", ctx.UssrFrontNodes.Count > 0, () =>
                {
                    var uTargets = ctx.UssrFrontNodes.Take(3).ToList();
                    string uNames = string.Join("、", uTargets.Select(id => GameData.Nodes[id].Name));
                    string uEnemy = s.Year >= 1992 ? "俄軍" : "蘇軍";
                    return new GameEvent
                    {
                        Id = $"dyn_mil_northern_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                        Title = $"北疆【{uNames}】塞外遠征暨耐寒裝甲整訓電文",
                        Desc = $"北伐兵鋒已臨近塞外邊疆！面對盤據【{uNames}】之{uEnemy}重裝集群，統帥部就嚴寒作戰被服防凍油料及外交孤立對手呈請核示。",
                        Choices = new List<EventChoice>
                        {
                            new() {
                                Label = $"配發極寒冬防油料被服，集中裝甲師向【{uNames}】實施多路穿插",
                                Hint = $"國庫 -${(0.45 * px):F1} 億、補給 -4；{uNames}{uEnemy}守軍約 -10%、前線戰力 +6%",
                                Effect = st => {
                                    SpendTreasury(st, 0.45);
                                    st.Supply = Math.Max(0, st.Supply - 4);
                                    ChipHeld(st, uTargets, 0.10);
                                    foreach (var u in st.Units.Where(x => ctx.RocFrontNodes.Contains(x.Loc) || x.Loc == fwdRoc))
                                        u.Str = Math.Min(100, u.Str + 6);
                                    SimEngine.Log(st, $"國軍寒地裝甲部隊突破風雪壓迫【{uNames}】{uEnemy}防線！", "good");
                                }
                            },
                            new() {
                                Label = $"策動【{uNames}】當地各族保鄉義勇軍剪斷{uEnemy}遠東後勤補給線",
                                Hint = $"國庫 -${(0.30 * px):F1} 億；{uNames}動盪 +14、國際地位 +4、獲得 1 支民兵於{fwdRocName}",
                                Effect = st => {
                                    SpendTreasury(st, 0.30);
                                    st.Intl += 4;
                                    SimEngine.AddUnit(st, "mil", fwdRoc, 85);
                                    foreach (var uid in uTargets)
                                        if (st.Control[uid] == Faction.USSR) st.Unrest[uid] = SimEngine.Clamp(st.Unrest[uid] + 14, 0, 100);
                                    SimEngine.Log(st, $"★ 北疆各族義勇軍響應號召襲擾{uEnemy}鐵道補給線，{uNames}局勢動盪加劇！", "good");
                                }
                            }
                        }
                    };
                }),
                ("encirclement_psyop", ctx.HostileFrontNodes.Count > 0, () => new GameEvent
                {
                    Id = $"dyn_mil_psyop_{s.Year}_{s.Month}", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "軍事部署",
                    Title = ctx.OnlyUssrRemains
                        ? $"北疆義軍聯防暨俄占區【{frontNames2}】心戰瓦解電文"
                        : $"對【{frontNames2}】敵前心戰喊話暨策反工作協調會",
                    Desc = $"政治作戰部門回報【{frontNames3}】前線{enemyTerm}長期征戰疲憊、給養緊繃，請示加派心戰播音與傳單攻勢或深化國際盟軍情報協作。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = $"對【{frontNames2}】全面投送返鄉心戰傳單並開啟投誠接應窗口",
                            Hint = $"補給 -2；{frontNames3}{enemyTerm}動盪 +12 並削弱守軍約 8%、民心 +3",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 2);
                                st.Morale += 3;
                                ChipHeld(st, frontTargets.Take(3), 0.08);
                                foreach (var id in frontTargets.Take(3))
                                {
                                    if (st.Control[id] != Faction.ROC)
                                        st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 12, 0, 100);
                                }
                                SimEngine.Log(st, $"我軍心理攻勢直擊【{frontNames3}】{enemyTerm}官兵士氣，敵營逃亡與厭戰情事激增！", "good");
                            }
                        },
                        new() {
                            Label = "公布前線人道收容戰俘實況，爭取西方友邦外援加碼",
                            Hint = $"國際地位 +5、美國關係 +3、當前科研進度 +1.5",
                            Effect = st => {
                                st.Intl += 5; st.UsRel += 3; st.ResearchProg += 1.5;
                                SimEngine.Log(st, "我軍人道清廉形象贏得國際高度肯定，外交與技術合作進展順利。", "good");
                            }
                        }
                    }
                })
            };

            return PickRotatedTemplate(s, "mil_tpl_", pool, 36);
        }

        private static string FindRearSourceFor(GameState s, string targetId, FrontlineContext ctx)
        {
            if (ctx.HeldMainland.Count > 0)
            {
                var rearMainland = ctx.HeldMainland
                    .Where(id => id != targetId && !ctx.RocFrontNodes.Contains(id)
                                 && SimEngine.UnitsAt(s, id, UnitDomain.Land).Any(u => !u.Moved))
                    .OrderByDescending(id => SimEngine.UnitsAt(s, id, UnitDomain.Land).Count(u => !u.Moved))
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(rearMainland)) return rearMainland;

                var anyMainland = ctx.HeldMainland
                    .Where(id => id != targetId && SimEngine.UnitsAt(s, id, UnitDomain.Land).Count(u => !u.Moved) >= 2)
                    .OrderByDescending(id => SimEngine.UnitsAt(s, id, UnitDomain.Land).Count(u => !u.Moved))
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(anyMainland)) return anyMainland;
            }
            return targetId == "TW" ? ctx.PickForwardRocNode(s) : "TW";
        }

        private static Unit FindReinforceUnit(GameState s, string srcId, string destId, out string failReason)
        {
            failReason = "";
            var idle = SimEngine.UnitsAt(s, srcId, UnitDomain.Land)
                .Where(u => !u.Moved)
                .OrderByDescending(u => u.Str)
                .ToList();
            if (idle.Count == 0)
            {
                failReason = $"{GameData.Nodes[srcId].Name}無待命地面部隊";
                return null;
            }
            foreach (var u in idle)
            {
                var chk = SimEngine.CheckMove(s, u, destId);
                if (chk.ok) return u;
                failReason = chk.reason;
            }
            return null;
        }

        private static string FindRetreatDestFor(GameState s, string expId, FrontlineContext ctx)
        {
            var rearMainland = ctx.HeldMainland
                .Where(id => id != expId && !ctx.RocFrontNodes.Contains(id))
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(rearMainland) && GameData.Nodes[expId].Kind == NodeKind.Mainland)
                return rearMainland;
            return "TW";
        }

        private static GameEvent BuildExposurePostureEvent(GameState s, FrontlineContext ctx)
        {
            if (ctx.OnlyUssrRemains)
            {
                string expId = !string.IsNullOrEmpty(s.ExposedNodeId) ? s.ExposedNodeId : ctx.PickForwardRocNode(s);
                string expName = GameData.Nodes[expId].Name;
                string srcId = FindRearSourceFor(s, expId, ctx);
                string srcName = GameData.Nodes[srcId].Name;
                var cand = FindReinforceUnit(s, srcId, expId, out string failWhy);
                string ussrTgt = ctx.UssrFrontNodes.FirstOrDefault() ?? ctx.UssrNodes.FirstOrDefault() ?? "";
                string ussrName = !string.IsNullOrEmpty(ussrTgt) ? GameData.Nodes[ussrTgt].Name : "北疆俄占區";
                string rName = s.Year >= 1992 ? "俄軍" : "蘇俄軍";

                return new GameEvent
                {
                    Id = $"dyn_posture_exp_{s.Year}_{s.Month}",
                    Year = s.Year, Month = s.Month, Day = s.Day, Tag = "態勢警訊",
                    Title = $"北疆防線告急：【{expName}】對俄側翼兵力薄弱",
                    Desc = $"前線急電：我軍駐防【{expName}】兵力不足以單獨抗衡【{ussrName}】方向的{rName}機械化兵團，統帥部須決定立即增兵、收縮加固防線，或藉外交輿論施壓牽制。",
                    Choices = new List<EventChoice>
                    {
                        new() {
                            Label = cand != null ? $"自【{srcName}】調派 1 師星夜馳援【{expName}】" : $"嘗試自【{srcName}】抽調增援【{expName}】（條件不足）",
                            Hint = cand != null ? $"調派部隊自{srcName}增援{expName}，消耗補給 2" : $"無法執行調動：{failWhy}",
                            FactionTag = "army",
                            Effect = st => {
                                var u = FindReinforceUnit(st, srcId, expId, out string why);
                                if (u != null && SimEngine.CheckMove(st, u, expId).ok)
                                {
                                    SimEngine.MoveNow(st, new[] { u.Id }, expId, manual: true);
                                    st.Supply = Math.Max(0, st.Supply - 2);
                                }
                                else SimEngine.Log(st, $"北疆增援【{expName}】未執行：{why}", "bad");
                                SimEngine.AssessPosture(st);
                            }
                        },
                        new() {
                            Label = $"就地加固【{expName}】防禦工事並收縮前沿哨所",
                            Hint = $"消耗補給 3；{expName}防禦工事 +1 級（滿級則守軍整補 +8%）",
                            FactionTag = "army",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 3);
                                int curF = SimEngine.FortificationLevel(st, expId);
                                if (curF < 3) SimEngine.IncreaseFortification(st, expId);
                                else foreach (var u in SimEngine.UnitsAt(st, expId, UnitDomain.Land)) u.Str = Math.Min(100, u.Str + 8);
                                SimEngine.Log(st, $"【{expName}】前線完成防線收縮與反裝甲工事加固。", "good");
                                SimEngine.AssessPosture(st);
                            }
                        },
                        new() {
                            Label = $"照會各國揭露{rName}集結動向，對【{ussrName}】施壓牽制",
                            Hint = $"國際地位 +3；{ussrName}{rName}駐軍約 -10%",
                            FactionTag = "intel",
                            Effect = st => {
                                st.Intl += 3;
                                if (!string.IsNullOrEmpty(ussrTgt)) ChipHeld(st, new[] { ussrTgt }, 0.10);
                                SimEngine.Log(st, $"國際輿論施壓莫斯科，牽制【{ussrName}】{rName}前進部署。", "good");
                                SimEngine.AssessPosture(st);
                            }
                        }
                    }
                };
            }

            string expId2 = !string.IsNullOrEmpty(s.ExposedNodeId) ? s.ExposedNodeId : "HI";
            string expZone = !string.IsNullOrEmpty(s.ExposedZoneId) ? s.ExposedZoneId : (GameData.Nodes[expId2].Zones.FirstOrDefault() ?? "Z4");
            string expName2 = GameData.Nodes[expId2].Name;
            string zName = GameData.Zones[expZone].Name;
            string srcId2 = FindRearSourceFor(s, expId2, ctx);
            string srcName2 = GameData.Nodes[srcId2].Name;
            var cand2 = FindReinforceUnit(s, srcId2, expId2, out string failWhy2);
            string retreatDest = FindRetreatDestFor(s, expId2, ctx);
            string retreatName = GameData.Nodes[retreatDest].Name;
            var naCand = SimEngine.FindNavalAirCandidate(s, expZone, "fleet", true)
                      ?? SimEngine.FindNavalAirCandidate(s, expZone, "air", true);
            var expLandUnits = SimEngine.UnitsAt(s, expId2, UnitDomain.Land).Where(u => !u.Moved).ToList();
            bool canRetreat = expLandUnits.Count > 0 && expLandUnits.All(u => SimEngine.CheckMove(s, u, retreatDest).ok);

            return new GameEvent
            {
                Id = $"dyn_posture_exp_{s.Year}_{s.Month}",
                Year = s.Year, Month = s.Month, Day = s.Day, Tag = "態勢警訊",
                Title = $"海空暴露：【{zName}】失掩下【{expName2}】防務吃緊",
                Desc = $"海軍電偵顯示【{zName}】我方海空掩護薄弱，使【{expName2}】直接暴露於敵軍渡海／側翼威脅之下。統帥部須決定自【{srcName2}】抽兵增援、改派海空協防【{zName}】，或將駐軍收縮回【{retreatName}】。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = cand2 != null ? $"自【{srcName2}】調派 1 師增援【{expName2}】固守" : $"自【{srcName2}】增援【{expName2}】（運力或兵力不足）",
                        Hint = cand2 != null ? $"檢查移動後派遣 1 師自{srcName2}赴{expName2}，補給 -2" : $"無法調動：{failWhy2}",
                        FactionTag = "army",
                        Effect = st => {
                            var u = FindReinforceUnit(st, srcId2, expId2, out string why);
                            if (u != null && SimEngine.CheckMove(st, u, expId2).ok)
                            {
                                SimEngine.MoveNow(st, new[] { u.Id }, expId2, manual: true);
                                st.Supply = Math.Max(0, st.Supply - 2);
                            }
                            else SimEngine.Log(st, $"增援【{expName2}】未能成行：{why}", "bad");
                            SimEngine.AssessPosture(st);
                        }
                    },
                    new() {
                        Label = naCand != null ? $"將 1 支海空主力改駐【{zName}】重奪掩護" : $"改派海空進駐【{zName}】（無待命艦機）",
                        Hint = naCand != null ? $"調派 {GameData.UnitTypes[naCand.Type].Name} 進駐{zName}，消除該區暴露態勢" : "目前無待命艦隊或航空聯隊可調",
                        FactionTag = "navair",
                        Effect = st => {
                            var na = SimEngine.FindNavalAirCandidate(st, expZone, "fleet", true)
                                  ?? SimEngine.FindNavalAirCandidate(st, expZone, "air", true);
                            if (na != null && SimEngine.DeployNavalAir(st, na.Id, expZone).ok)
                                SimEngine.Log(st, $"⚓ 已調派{GameData.UnitTypes[na.Type].Name}進駐【{zName}】掩護【{expName2}】。", "good");
                            else
                                SimEngine.Log(st, $"改駐【{zName}】未執行：無待命海空單位。", "bad");
                            SimEngine.AssessPosture(st);
                        }
                    },
                    new() {
                        Label = canRetreat ? $"收縮防線，將【{expName2}】守軍撤回【{retreatName}】" : $"收縮【{expName2}】駐軍（無待命可撤部隊）",
                        Hint = canRetreat ? $"依 CheckMove 撤回{expName2}駐軍至{retreatName}（保留主權與零敵軍），補給 +4" : $"目前{expName2}無可立即撤回之待命部隊",
                        FactionTag = "econ",
                        Effect = st => {
                            var list = SimEngine.UnitsAt(st, expId2, UnitDomain.Land)
                                .Where(u => !u.Moved && SimEngine.CheckMove(st, u, retreatDest).ok)
                                .Select(u => u.Id)
                                .ToList();
                            if (list.Count > 0)
                            {
                                SimEngine.MoveNow(st, list, retreatDest, manual: true);
                                st.Supply += 4;
                                SimEngine.Log(st, $"我軍收縮【{expName2}】駐軍轉進【{retreatName}】保存實力。", "good");
                            }
                            else
                            {
                                SimEngine.Log(st, $"【{expName2}】目前無可撤離之待命部隊。", "bad");
                            }
                            SimEngine.AssessPosture(st);
                        }
                    }
                }
            };
        }

        private static GameEvent BuildWindowPostureEvent(GameState s, FrontlineContext ctx)
        {
            string zId = !string.IsNullOrEmpty(s.WindowZoneId) ? s.WindowZoneId : ctx.PrimaryZoneId;
            string tgtId = !string.IsNullOrEmpty(s.WindowTargetId) ? s.WindowTargetId : (ctx.PrcFrontNodes.FirstOrDefault() ?? "FJ");
            string zName = GameData.Zones[zId].Name;
            string tgtName = GameData.Nodes[tgtId].Name;
            int assetCount = SimEngine.UnitsAt(s, zId, UnitDomain.Sea).Count + SimEngine.UnitsAt(s, zId, UnitDomain.Air).Count;
            double supCost = Math.Clamp(assetCount * 1.5, 2.0, 6.0);

            bool northWithUssr = ctx.Theater == FrontlineTheater.NorthChinaPlain && ctx.UssrNodes.Count > 0;
            string northTarget = northWithUssr
                ? GameData.MainlandIds.FirstOrDefault(id => s.Control[id] == Faction.ROC && GameData.Nodes[id].Adj.Any(a => s.Control[a] == Faction.USSR))
                : null;
            string northSrc = !string.IsNullOrEmpty(northTarget) ? FindRearSourceFor(s, northTarget, ctx) : null;
            Unit northUnit = !string.IsNullOrEmpty(northTarget) && !string.IsNullOrEmpty(northSrc)
                ? FindReinforceUnit(s, northSrc, northTarget, out _)
                : null;

            return new GameEvent
            {
                Id = $"dyn_posture_win_{s.Year}_{s.Month}",
                Year = s.Year, Month = s.Month, Day = s.Day, Tag = "戰略窗口",
                Title = $"戰略窗口：【{zName}】優勢成形・直指【{tgtName}】",
                Desc = $"我方在【{zName}】已壓倒敵方海空兵力，【{tgtName}】沿岸防線出現短暫窗口。是要發動聯合破襲打開登陸戰機、實施近岸封鎖，或是按兵整補？",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = $"趁【{zName}】優勢對【{tgtName}】發動海空聯合破襲",
                        Hint = $"按參戰規模消耗補給 {supCost:F1}；削弱{tgtName}約 10% 守軍（不改控制方），並開啟{zName}登陸窗口 2 個月（登陸戰力 +8%）",
                        FactionTag = "navair",
                        Effect = st => {
                            st.Supply = Math.Max(0, st.Supply - supCost);
                            ChipHeld(st, new[] { tgtId }, 0.10);
                            st.Flags["winBonus_" + zId] = st.TotalMonths + 2;
                            SimEngine.Log(st, $"★ 我軍趁【{zName}】海空優勢破襲【{tgtName}】，打開為期兩個月的跨海登陸窗口！", "good");
                        }
                    },
                    northWithUssr && northUnit != null
                        ? new EventChoice {
                            Label = $"抽調【{GameData.Nodes[northSrc].Name}】1 師北上協防【{GameData.Nodes[northTarget].Name}】抗俄",
                            Hint = $"趁華北沿海優勢抽兵強化北疆邊防（{GameData.Nodes[northSrc].Name}→{GameData.Nodes[northTarget].Name}）",
                            FactionTag = "army",
                            Effect = st => {
                                var u = FindReinforceUnit(st, northSrc, northTarget, out string why);
                                if (u != null && SimEngine.CheckMove(st, u, northTarget).ok)
                                    SimEngine.MoveNow(st, new[] { u.Id }, northTarget, manual: true);
                                else
                                    SimEngine.Log(st, $"北疆抽調未執行：{why}", "bad");
                            }
                        }
                        : new EventChoice {
                            Label = $"嚴密海上封鎖【{tgtName}】航道，壓迫敵後民心",
                            Hint = $"{tgtName}動盪 +12；{zName}出勤艦隊耐久略降 -4%",
                            FactionTag = "intel",
                            Effect = st => {
                                if (st.Control[tgtId] == Faction.PRC)
                                    st.Unrest[tgtId] = SimEngine.Clamp(st.Unrest[tgtId] + 12, 0, 100);
                                var fl = SimEngine.UnitsAt(st, zId, UnitDomain.Sea).FirstOrDefault();
                                if (fl != null) fl.Str = Math.Max(25, fl.Str - 4);
                                SimEngine.Log(st, $"我軍封鎖【{tgtName}】近岸航線，當地民心浮動加劇。", "good");
                            }
                        },
                    new() {
                        Label = $"按兵待命，保留【{zName}】海空編隊休整",
                        Hint = $"不耗補給，{zName}海空兵力恢復 +3%；共軍將依常態增補填上此處空隙",
                        FactionTag = "econ",
                        Effect = st => {
                            foreach (var u in SimEngine.UnitsAt(st, zId, UnitDomain.Sea).Concat(SimEngine.UnitsAt(st, zId, UnitDomain.Air)))
                                u.Str = Math.Min(100, u.Str + 3);
                            st.FrontHeadlineKind = "quiet";
                            st.FrontHeadline = $"態勢轉變：我方按兵未動，共軍正增補【{tgtName}】防務填補窗口。";
                            SimEngine.Log(st, $"我軍暫緩對【{tgtName}】出擊，海空編隊完成整補。", "");
                        }
                    }
                }
            };
        }
    }
}
