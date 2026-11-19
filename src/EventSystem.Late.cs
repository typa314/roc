using System;
using System.Collections.Generic;
using System.Linq;

namespace RocGame
{
    public static partial class EventSystem
    {
        static EventSystem()
        {
            HistoricalEvents.AddRange(CreateLateEvents());
        }

        internal static bool AnyPrc(GameState s) => GameData.MainlandIds.Any(id => s.Control[id] == Faction.PRC);

        internal static double PrcRemainFrac(GameState s)
        {
            int n = GameData.MainlandIds.Count(id => s.Control[id] == Faction.PRC);
            return n / (double)Math.Max(1, GameData.MainlandIds.Length);
        }

        internal static List<string> CurrentPrcFront(GameState s, int max)
        {
            var ctx = AnalyzeFrontline(s);
            var ids = ctx.HostileFrontNodes.Where(id => s.Control.TryGetValue(id, out var f) && f == Faction.PRC).Take(max).ToList();
            if (ids.Count == 0)
                ids = ctx.PrcNodes.Take(max).ToList();
            return ids;
        }

        internal static void ChipFront(GameState s, double frac, int max = 2)
            => ChipHeld(s, CurrentPrcFront(s, max), frac);

        internal static void ChipHeld(GameState s, IEnumerable<string> ids, double frac)
        {
            foreach (var id in ids)
            {
                if (!s.Garrison.ContainsKey(id) || GameData.Zones.ContainsKey(id)) continue;
                if (s.Control.TryGetValue(id, out var f) && f == Faction.ROC) continue;
                double g = s.Garrison[id];
                double floor = Math.Max(0.8, SimEngine.NominalGarrison(s, id) * 0.45);
                double next = Math.Max(floor, g * (1.0 - frac));
                s.Garrison[id] = Math.Min(g, next);
            }
        }

        internal static void AlertFront(GameState s, double mult, int max = 2)
        {
            foreach (var id in CurrentPrcFront(s, max))
            {
                if (!s.Garrison.ContainsKey(id) || GameData.Zones.ContainsKey(id)) continue;
                s.Garrison[id] *= mult;
            }
        }

        internal static bool MoveOneFleet(GameState s, string zone)
        {
            if (!GameData.Zones.ContainsKey(zone)) return false;
            var fleet = s.Units.FirstOrDefault(u => u.Type == "fleet" && u.Loc != zone)
                     ?? s.Units.FirstOrDefault(u => u.Type == "fleet");
            if (fleet == null) return false;
            fleet.Loc = zone;
            return true;
        }

        internal static void TireLand(GameState s, int loss, int floor = 40)
        {
            foreach (var u in s.Units.Where(u => GameData.UnitTypes[u.Type].Domain == UnitDomain.Land))
                u.Str = Math.Max(floor, u.Str - loss);
        }

        internal static void ShockPrc(GameState s, double fullDrop)
        {
            double scale = 0.35 + 0.65 * PrcRemainFrac(s);
            s.PrcStab -= fullDrop * scale;
        }

        internal static void RaiseFrontUnrest(GameState s, double amount, int max = 3)
        {
            foreach (var id in CurrentPrcFront(s, max))
                s.Unrest[id] = SimEngine.Clamp(s.Unrest[id] + amount, 0, 100);
        }

        internal static string FrontPhrase(GameState s)
        {
            var ctx = AnalyzeFrontline(s);
            return $"{ctx.TheaterTitle}（{ctx.FrontNames(2)}）";
        }

        internal static void Retitle(GameEvent ev, GameState s, string title, string desc)
        {
            ev.Title = title;
            ev.Desc = desc.Replace("{front}", FrontPhrase(s));
        }

        static List<GameEvent> CreateLateEvents() => new()
        {
            Event("paris_1973", 1973, 1, 27, "巴黎協定與西南牽制的收束",
                "越戰和談告一段落，原本牽制中共西南的外援力量即將撤出。統帥部必須決定這些力量轉到{front}，還是用來換取美援續約。",
                AnyPrc,
                new[]
                {
                    Choice("把騰出的力量押上當前敵前", "補給 -6；當前敵前動盪 +8、登陸或攻堅加成 +4%", "blue", s =>
                    {
                        s.Supply = Math.Max(0, s.Supply - 6);
                        RaiseFrontUnrest(s, 8);
                        if (AnalyzeFrontline(s).ContestedZones.Count > 0) s.LandingBonus += 0.04;
                        else s.BattleBonus += 0.04;
                        SimEngine.Log(s, $"西南牽制結束，力量改押{FrontPhrase(s)}。", "good");
                    }),
                    Choice("在敵後再留一手", "當前敵前動盪 +12，美國關係 -3", "gold", s =>
                    {
                        RaiseFrontUnrest(s, 12);
                        s.UsRel -= 3;
                        SimEngine.Log(s, $"我方保留敵後網絡，繼續擾動{FrontPhrase(s)}。");
                    }),
                    Choice("以撤出換取美援續約", "美援 +0.25、美國關係 +5；攻勢加成略降", "purple", s =>
                    {
                        s.UsAid += 0.25; s.UsRel += 5;
                        s.LandingBonus = Math.Max(0, s.LandingBonus - 0.02);
                        SimEngine.Log(s, "我方答應收束外圍攻勢，華府同意延長援助。", "good");
                    })
                }),
            Event("oil_1974", 1974, 1, 1, "石油危機與油料分配",
                "國際油價暴漲。海軍、空軍與加工出口搶的是同一批油料。戰線已在{front}，分配方式會直接影響這一季能打多久。",
                null,
                new[]
                {
                    Choice("軍用油料優先", "艦機妥善率上升，經濟效率 -4%", "red", s =>
                    {
                        foreach (var u in s.Units.Where(u => u.Type is "fleet" or "air"))
                            u.Str = Math.Min(100, u.Str + 4);
                        s.EconMult = Math.Max(0.5, s.EconMult - 0.04);
                        SimEngine.Log(s, "油料撥給艦機，出口產業暫緩。", "bad");
                    }),
                    Choice("保住加工出口", "經濟效率 +5%；艦隊妥善率下降", "blue", s =>
                    {
                        s.EconMult += 0.05;
                        foreach (var u in s.Units.Where(u => u.Type == "fleet"))
                            u.Str = Math.Max(28, u.Str - 3);
                        SimEngine.Log(s, "油料優先供給出口，艦隊出勤下降。");
                    }),
                    Choice("向美日協調配額", "美日關係上升、補給 +8，並支付一筆採購", "gold", s =>
                    {
                        SpendTreasury(s, 0.8);
                        s.UsRel += 3; s.JpRel += 6; s.Supply = Math.Min(400, s.Supply + 8);
                        SimEngine.Log(s, "美日同意增撥能源配額，船團得以維持。", "good");
                    })
                }),
            Event("chiang_1975", 1975, 4, 5, "蔣中正總統逝世與權力過渡",
                "中樞權力交替。{front}的將領都在看台北要繼續把資源押在前線，還是先穩住島內。",
                null,
                new[]
                {
                    Choice("前線不停，中樞強續", "全軍經驗上升、民心 -4；已有光復省時前線部隊獲得整補", "red", s =>
                    {
                        foreach (var u in s.Units) u.Exp = Math.Min(5, u.Exp + 0.4);
                        s.Morale -= 4;
                        if (AnalyzeFrontline(s).HeldMainland.Count > 0)
                        {
                            string node = AnalyzeFrontline(s).PickForwardRocNode(s);
                            foreach (var u in SimEngine.UnitsAt(s, node, UnitDomain.Land))
                                u.Str = Math.Min(100, u.Str + 6);
                        }
                        SimEngine.Log(s, "權力過渡期間，統帥部下令前線維持原定攻勢。");
                    }),
                    Choice("先穩中樞，降低總動員", "民心 +6、兵源 +1.5；攻勢加成下降", "blue", s =>
                    {
                        s.Morale += 6; s.Manpower += 1.5;
                        s.LandingBonus = Math.Max(0, s.LandingBonus - 0.03);
                        SimEngine.Log(s, "台北先處理權力交替，本季攻勢降調。", "good");
                    }),
                    Choice("對當前敵前加強廣播", "支付宣傳經費；當前敵前動盪 +8", "purple", s =>
                    {
                        SpendTreasury(s, 0.8);
                        RaiseFrontUnrest(s, 8);
                        SimEngine.Log(s, $"心戰廣播改打{FrontPhrase(s)}。", "good");
                    })
                }),
            Event("mao_1976", 1976, 9, 9, "毛澤東逝世與對岸權力動盪",
                "北京權力層出現真空。窗口在{front}，不在已經打過或已經光復的地方。現在施壓、按兵整補，或趁亂策反，三條路只能走一條。",
                AnyPrc,
                new[]
                {
                    Choice("立即施壓當前敵前", "補給 -6；敵前守軍約 -10%，不低於守備地板", "red", s =>
                    {
                        s.Supply = Math.Max(0, s.Supply - 6);
                        ChipFront(s, 0.10);
                        SimEngine.Log(s, $"我軍趁權力真空壓迫{FrontPhrase(s)}，敵軍工事受創但未被攻破。", "big");
                    }),
                    Choice("按兵，先把部隊補實", "我方地面部隊戰力 +5；敵軍守軍不變", "blue", s =>
                    {
                        foreach (var u in s.Units.Where(u => GameData.UnitTypes[u.Type].Domain == UnitDomain.Land))
                            u.Str = Math.Min(100, u.Str + 5);
                        SimEngine.Log(s, "統帥部判斷窗口還會再開，本月不消耗攻勢。", "good");
                    }),
                    Choice("策反當前敵前，不急著進兵", "中共穩定度按剩餘省分比例下降；敵前動盪 +12，並在我方前哨編成 1 支民兵", "purple", s =>
                    {
                        ShockPrc(s, 10);
                        RaiseFrontUnrest(s, 12);
                        SimEngine.AddUnit(s, "mil", AnalyzeFrontline(s).PickForwardRocNode(s), 80);
                        SimEngine.Log(s, $"策反工作沿{FrontPhrase(s)}展開，前哨收編一支民兵。", "good");
                    })
                }),
            Event("reagan_1981", 1981, 1, 20, "華府換屆與對台軍售",
                "華府新政府重新談對台軍售。{front}需要飛機，也需要華府不要在我方跨海時出手制止。",
                null,
                new[]
                {
                    Choice("優先爭取戰機與飛彈", "支付軍購；空軍科技 +0.35、美國關係 +4", "blue", s =>
                    {
                        SpendTreasury(s, 4);
                        s.AirTech += 0.35; s.UsRel += 4;
                        SimEngine.Log(s, "軍售清單以戰機與飛彈為先，制空問題先於渡海。", "good");
                    }),
                    Choice("要求美方勿限制當前攻勢", "美國關係 -6；登陸加成 +5%，並獲得兩年默許窗口", "red", s =>
                    {
                        s.UsRel -= 6; s.LandingBonus += 0.05;
                        s.UsConsent = Math.Max(s.UsConsent, 2);
                        SimEngine.Log(s, $"我方以軍售談判換取對{FrontPhrase(s)}用兵的默許。", "big");
                    }),
                    Choice("以經貿換安全承諾", "台灣 GDP +3%、美國關係 +8；攻堅加成略降", "gold", s =>
                    {
                        s.Gdp *= 1.03; s.UsRel += 8;
                        s.BattleBonus = Math.Max(0, s.BattleBonus - 0.01);
                        SimEngine.Log(s, "談判重心改為經貿與安全承諾，軍事日程往後挪。");
                    })
                }),
            Event("hk_1984", 1984, 12, 19, "香港前途與資金去向",
                "香港前途進入倒數。資金和情報可以接上{front}，也可以留在台灣。公開杯葛則會同時拉高國際聲勢與對岸敵意。",
                AnyPrc,
                new[]
                {
                    Choice("把資金和情報接到當前敵前", "支付佈建經費；補給 +10、敵前動盪 +8", "gold", s =>
                    {
                        SpendTreasury(s, 1.2);
                        s.Supply = Math.Min(400, s.Supply + 10);
                        RaiseFrontUnrest(s, 8);
                        SimEngine.Log(s, $"香港管道改接到{FrontPhrase(s)}。", "good");
                    }),
                    Choice("公開杯葛，拉國際表態", "國際地位 +6、美國關係 +3；中共穩定度按剩餘省分下降", "blue", s =>
                    {
                        s.Intl += 6; s.UsRel += 3; s.Morale += 2;
                        ShockPrc(s, 6);
                        SimEngine.Log(s, "台北公開反對香港安排，外交聲勢上升，北京更為戒備。");
                    }),
                    Choice("資金留在台灣，前線不動", "台灣 GDP +4%；敵軍守軍不變", "purple", s =>
                    {
                        s.Gdp *= 1.04;
                        SimEngine.Log(s, "香港資金主要留在台灣，本季不拿它做前線用途。", "good");
                    })
                }),
            Event("ccg_1988", 1988, 1, 13, "蔣經國總統逝世與中樞交替",
                "中樞再次交替。島內要求降低動員，{front}的將領要求給養和權限不要一起被收回。",
                null,
                new[]
                {
                    Choice("反攻仍是第一優先", "已有光復省則前線整補並消耗補給；仍在對峙則提高登陸加成、兵源 -1", "red", s =>
                    {
                        var ctx = AnalyzeFrontline(s);
                        if (ctx.HeldMainland.Count >= 3)
                        {
                            s.Supply = Math.Max(0, s.Supply - 8);
                            string node = ctx.PickForwardRocNode(s);
                            foreach (var u in SimEngine.UnitsAt(s, node, UnitDomain.Land))
                                u.Str = Math.Min(100, u.Str + 8);
                            SimEngine.Log(s, $"{nodeName(s, node)}前線獲得整補，島內動員維持。", "big");
                        }
                        else
                        {
                            s.LandingBonus += 0.06;
                            s.Manpower = Math.Max(0, s.Manpower - 1);
                            SimEngine.Log(s, "統帥部維持渡海優先，後備兵源撥給攻勢。");
                        }
                    }),
                    Choice("降低動員，先處理島內", "民心 +8、兵源 +2；攻勢加成下降", "blue", s =>
                    {
                        s.Morale += 8; s.Manpower += 2;
                        s.LandingBonus = Math.Max(0, s.LandingBonus - 0.04);
                        SimEngine.Log(s, "動員等級下調，攻勢日程讓給島內政治。", "good");
                    }),
                    Choice("島內鬆綁，前線將領留任", "民心 +4、國際地位 +4、經濟效率 +2%；前線部隊經驗上升", "gold", s =>
                    {
                        s.Morale += 4; s.Intl += 4; s.EconMult += 0.02;
                        string node = AnalyzeFrontline(s).PickForwardRocNode(s);
                        foreach (var u in SimEngine.UnitsAt(s, node, UnitDomain.Land))
                            u.Exp = Math.Min(5, u.Exp + 0.5);
                        SimEngine.Log(s, "島內放寬管制，前線指揮權維持原將領。", "good");
                    })
                }),
            Event("gulf_1990", 1990, 8, 2, "海灣戰爭、油價與聯合作戰",
                "海灣戰爭改寫了油價，也讓各國重新估價聯合作戰。這些經驗要買裝備、存油，還是用在{front}。",
                null,
                new[]
                {
                    Choice("趁國際軍火流通補海空", "支付軍購；海軍科技 +0.25、空軍科技 +0.2", "blue", s =>
                    {
                        SpendTreasury(s, 3);
                        s.NavyTech += 0.25; s.AirTech += 0.2;
                        SimEngine.Log(s, "我方趁軍火市場鬆動補齊海空裝備。", "good");
                    }),
                    Choice("油價衝擊下先存油保經濟", "經濟效率 -3%、支付採購；補給 +12", "gold", s =>
                    {
                        SpendTreasury(s, 1);
                        s.EconMult = Math.Max(0.5, s.EconMult - 0.03);
                        s.Supply = Math.Min(400, s.Supply + 12);
                        SimEngine.Log(s, "高油價期間優先把補給庫補滿。");
                    }),
                    Choice("把聯合作戰打法用到當前敵前", "地面作戰加成 +4%；敵前守軍約 -8%", "red", s =>
                    {
                        s.BattleBonus += 0.04;
                        ChipFront(s, 0.08, 1);
                        SimEngine.Log(s, $"聯合作戰準則套用到{FrontPhrase(s)}。", "big");
                    })
                }),
            Event("wangku_1993", 1993, 4, 27, "辜汪會談與兩岸接觸",
                "對岸提出事務性接觸。談成可以換到貿易，代價是{front}的攻勢會緩下來。拒絕則要準備在當前敵前挨一輪政治反擊。",
                AnyPrc,
                new[]
                {
                    Choice("同意接觸，攻勢暫停一季", "經濟效率 +4%、國際地位 +5；登陸加成下降，中共穩定度回升", "blue", s =>
                    {
                        s.EconMult += 0.04; s.Intl += 5;
                        s.LandingBonus = Math.Max(-0.05, s.LandingBonus - 0.04);
                        s.PrcStab += 4 * (0.35 + 0.65 * PrcRemainFrac(s));
                        SimEngine.Log(s, "兩岸進入事務性接觸，本季不擴大攻勢。");
                    }),
                    Choice("拒絕和談，壓制當前敵前", "敵前守軍約 -12%；民心 -3、國際地位 -4", "red", s =>
                    {
                        ChipFront(s, 0.12);
                        s.Morale -= 3; s.Intl -= 4;
                        SimEngine.Log(s, $"我方拒絕接觸，並對{FrontPhrase(s)}施加壓力。", "big");
                    }),
                    Choice("只談事務，軍事日程不停", "國庫獲得一筆貿易進項；敵前守軍約 -4%", "gold", s =>
                    {
                        s.Treasury += 1.2 * SimEngine.PriceIndex(s);
                        ChipFront(s, 0.04, 1);
                        SimEngine.Log(s, "接觸限於事務，小規模軍事壓力仍留在當前敵前。", "good");
                    })
                }),
            Event("cornell_1995", 1995, 6, 9, "李登輝總統訪美（康乃爾之行）與台海戒備",
                "領導人獲邀出訪。出訪能拉高美國關係，也會讓{front}的敵軍進入戒備。艦隊要不要同時前推，是另一個決定。",
                AnyPrc,
                new[]
                {
                    Choice("推動出訪", "美國關係 +10、國際地位 +6；當前敵前守軍 +8%（對岸戒備）", "gold", s =>
                    {
                        s.UsRel += 10; s.Intl += 6;
                        foreach (var id in CurrentPrcFront(s, 2))
                            s.Garrison[id] *= 1.08;
                        SimEngine.Log(s, $"出訪升高國際能見度，{FrontPhrase(s)}敵軍隨之戒備。", "bad");
                    }),
                    Choice("低調延後", "美國關係 +3、民心 -2；敵軍守軍不變", "blue", s =>
                    {
                        s.UsRel += 3; s.Morale -= 2;
                        SimEngine.Log(s, "出訪延後，敵前戒備沒有上升。");
                    }),
                    Choice("出訪，同時把一支艦隊推進當前海域", "美國關係 +6；一支艦隊改駐當前主要海域", "red", s =>
                    {
                        s.UsRel += 6;
                        string zone = AnalyzeFrontline(s).PrimaryZoneId;
                        var fleet = s.Units.FirstOrDefault(u => u.Type == "fleet" && u.Loc != zone)
                                 ?? s.Units.FirstOrDefault(u => u.Type == "fleet");
                        if (fleet != null && GameData.Zones.ContainsKey(zone))
                        {
                            fleet.Loc = zone;
                            SimEngine.Log(s, $"出訪同時，艦隊前推至{GameData.Zones[zone].Name}。", "big");
                        }
                        else SimEngine.Log(s, "出訪成行，但沒有可調的艦隊。");
                    })
                }),
            Event("liangguo_1999", 1999, 7, 9, "提出「特殊國與國關係」（兩國論）",
                "領導人把兩岸說成特殊的國與國關係。{front}的敵軍會拿這句話當戒備理由，經濟部門則擔心市場關門。",
                null,
                new[]
                {
                    Choice("把表述定下來，並準備敵前戒備", "國際地位 +6、美國關係 +4、民心 +2；當前敵前守軍 +6%", "gold", s =>
                    {
                        s.Intl += 6; s.UsRel += 4; s.Morale += 2;
                        AlertFront(s, 1.06);
                        SimEngine.Log(s, $"表述沒有收回，{FrontPhrase(s)}敵軍隨之戒備。", "bad");
                    }),
                    Choice("把表述收回，換市場不要關門", "台灣 GDP +3%；國際地位 -3，攻勢加成下降", "blue", s =>
                    {
                        s.Gdp *= 1.03; s.Intl -= 3;
                        s.LandingBonus = Math.Max(0, s.LandingBonus - 0.02);
                        SimEngine.Log(s, "表述收回，貿易管道維持，攻勢日程往後。");
                    }),
                    Choice("把這句話做成當前敵前的心戰", "支付宣傳經費；當前敵前動盪 +10", "purple", s =>
                    {
                        SpendTreasury(s, 0.6);
                        RaiseFrontUnrest(s, 10);
                        SimEngine.Log(s, $"心戰廣播把這句話打進{FrontPhrase(s)}。", "good");
                    })
                }),
            Event("election_2000", 2000, 3, 18, "2000年總統大選與政黨輪替",
                "中樞換人。新班子要決定反攻預算留在{front}，轉去經濟，或改成拒止防衛。",
                null,
                new[]
                {
                    Choice("反攻預算維持", "登陸加成 +3%；兵源 -0.8、民心 -2", "red", s =>
                    {
                        s.LandingBonus += 0.03;
                        s.Manpower = Math.Max(0, s.Manpower - 0.8);
                        s.Morale -= 2;
                        SimEngine.Log(s, $"反攻預算留在{FrontPhrase(s)}。");
                    }),
                    Choice("預算轉去經濟建設", "台灣 GDP +4%、經濟效率 +2%；攻勢加成下降", "blue", s =>
                    {
                        s.Gdp *= 1.04; s.EconMult += 0.02;
                        s.LandingBonus = Math.Max(0, s.LandingBonus - 0.04);
                        SimEngine.Log(s, "預算從攻勢挪到經濟建設。", "good");
                    }),
                    Choice("改做拒止，少談渡海", "防空 +0.06、海軍科技 +0.1；攻勢加成下降", "gold", s =>
                    {
                        s.AirDefense += 0.06; s.NavyTech += 0.1;
                        s.LandingBonus = Math.Max(0, s.LandingBonus - 0.03);
                        SimEngine.Log(s, "國防預算改押防空與海軍，渡海日程放慢。");
                    })
                }),
            Event("wto_2002", 2002, 1, 1, "我國正式加入世界貿易組織（WTO）",
                "台灣進入多邊貿易體系，對岸也已在裡面。規則可以拿來擴市場，也可以拿來保護還在供給{front}的產業。",
                null,
                new[]
                {
                    Choice("照規則開放市場", "台灣 GDP +5%、經濟效率 +3%；攻堅加成略降", "blue", s =>
                    {
                        s.Gdp *= 1.05; s.EconMult += 0.03;
                        s.BattleBonus = Math.Max(0, s.BattleBonus - 0.01);
                        SimEngine.Log(s, "市場按新規則開放，軍事採購讓出一部分產能。", "good");
                    }),
                    Choice("戰略產業不開放", "經濟效率 +1%、民心 +3、補給 +6", "gold", s =>
                    {
                        s.EconMult += 0.01; s.Morale += 3;
                        s.Supply = Math.Min(400, s.Supply + 6);
                        SimEngine.Log(s, "戰略產業留在管制名單，補給庫得到保障。");
                    }),
                    Choice("以入會換一紙安全附帶", "支付交涉經費；美國關係 +6、國際地位 +3", "purple", s =>
                    {
                        SpendTreasury(s, 1.5);
                        s.UsRel += 6; s.Intl += 3;
                        SimEngine.Log(s, "入會談判附帶一份安全諒解。", "good");
                    })
                }),
            Event("antisec_2005", 2005, 3, 14, "中共通過《反分裂國家法》與台海戒備",
                "對岸把動武寫進法律。{front}的部隊會不會進入戒備，取決於台北怎麼回應。",
                AnyPrc,
                new[]
                {
                    Choice("公開抗議這部法律", "國際地位 +5、美國關係 +3、民心 +2；當前敵前守軍 +6%", "blue", s =>
                    {
                        s.Intl += 5; s.UsRel += 3; s.Morale += 2;
                        AlertFront(s, 1.06);
                        SimEngine.Log(s, $"公開抗議之後，{FrontPhrase(s)}敵軍提高戒備。", "bad");
                    }),
                    Choice("趁戒備未完成，施壓當前敵前", "補給 -4；當前敵前守軍約 -8%", "red", s =>
                    {
                        s.Supply = Math.Max(0, s.Supply - 4);
                        ChipFront(s, 0.08);
                        SimEngine.Log(s, $"我軍在敵軍完成戒備前壓迫{FrontPhrase(s)}。", "big");
                    }),
                    Choice("不公開表態，保住貿易", "台灣 GDP +3%；國際地位 -2；敵軍守軍不變", "gold", s =>
                    {
                        s.Gdp *= 1.03; s.Intl -= 2;
                        SimEngine.Log(s, "台北不公開回應，貿易繼續，敵前守軍沒有變動。");
                    })
                }),
            Event("links_2008", 2008, 12, 15, "兩岸全面實施大三通（通郵、通商、通航）",
                "對岸提議包機、海運與郵務直接往來。通了，{front}的攻勢會被貿易拖慢；不通，市場留在島內。",
                AnyPrc,
                new[]
                {
                    Choice("開通直接往來", "經濟效率 +5%；中共穩定度回升，攻勢加成下降", "blue", s =>
                    {
                        s.EconMult += 0.05;
                        s.PrcStab += 3 * (0.35 + 0.65 * PrcRemainFrac(s));
                        s.LandingBonus = Math.Max(-0.05, s.LandingBonus - 0.03);
                        SimEngine.Log(s, "直接通航開啟，本季攻勢放慢。");
                    }),
                    Choice("拒絕直接往來", "民心 +4；當前敵前守軍約 -5%", "red", s =>
                    {
                        s.Morale += 4;
                        ChipFront(s, 0.05, 1);
                        SimEngine.Log(s, $"我方拒絕直航，並對{FrontPhrase(s)}保持壓力。", "big");
                    }),
                    Choice("只開貨運", "國庫獲得貿易進項、經濟效率 +2%；客運與攻勢日程不動", "gold", s =>
                    {
                        s.Treasury += 1.5 * SimEngine.PriceIndex(s);
                        s.EconMult += 0.02;
                        SimEngine.Log(s, "貨運開放，客運與軍事日程維持原狀。", "good");
                    })
                }),
            Event("diaoyu_2012", 2012, 9, 11, "日本將釣魚台國有化與東海保釣巡航",
                "釣魚台爭議升溫。東海的船可以開出去，也可以調回{front}。日本關係會跟著這次部署一起變。",
                null,
                new[]
                {
                    Choice("與日本協調，加強東海巡弋", "日本關係 +4、國際地位 +2；一支艦隊改駐東海", "blue", s =>
                    {
                        s.JpRel += 4; s.Intl += 2;
                        if (MoveOneFleet(s, "Z2"))
                            SimEngine.Log(s, "艦隊前推至東海，與日本的巡弋範圍重疊。", "big");
                        else SimEngine.Log(s, "協調完成，但沒有可調的艦隊。");
                    }),
                    Choice("對日抗議，船調回當前戰線", "日本關係 -4、民心 +3", "red", s =>
                    {
                        s.JpRel -= 4; s.Morale += 3;
                        SimEngine.Log(s, "船團不進釣魚台海域，民心因表態而上升。");
                    }),
                    Choice("不介入，預算留在當前敵前", "地面作戰加成 +2%；日本關係不變", "gold", s =>
                    {
                        s.BattleBonus += 0.02;
                        SimEngine.Log(s, $"釣魚台爭議不介入，預算留在{FrontPhrase(s)}。");
                    })
                }),
            Event("sunflower_2014", 2014, 3, 18, "太陽花學運（反服貿運動）爆發",
                "服貿協議衝進立法院。簽下去換到對岸市場，擋下來換到美國與島內支持，擱置則可以把錢改撥{front}的防衛。",
                null,
                new[]
                {
                    Choice("站在抗議一邊，協議停住", "美國關係 +5、國際地位 +3、民心 +4；經濟效率 -3%", "blue", s =>
                    {
                        s.UsRel += 5; s.Intl += 3; s.Morale += 4;
                        s.EconMult = Math.Max(0.5, s.EconMult - 0.03);
                        SimEngine.Log(s, "協議停住，島內動員與美國關係上升，貿易成長放慢。", "good");
                    }),
                    Choice("強行通過協議", "台灣 GDP +4%；民心 -6、美國關係 -4，中共穩定度回升", "red", s =>
                    {
                        s.Gdp *= 1.04; s.Morale -= 6; s.UsRel -= 4;
                        s.PrcStab += 2 * (0.35 + 0.65 * PrcRemainFrac(s));
                        SimEngine.Log(s, "協議強行通過，市場打開，島內與華府同時不滿。", "bad");
                    }),
                    Choice("先擱置，錢改撥防衛", "支付撥補；防空 +0.05、海軍科技 +0.12", "gold", s =>
                    {
                        SpendTreasury(s, 1.5);
                        s.AirDefense += 0.05; s.NavyTech += 0.12;
                        SimEngine.Log(s, "協議擱置，這筆預算改做防空與海軍。", "good");
                    })
                }),
            Event("scs_2016", 2016, 7, 12, "海牙公布「南海仲裁案」裁決",
                "國際仲裁對南海島礁做出不利對岸的裁決。這張紙可以拿來加強南海巡弋，也可以不去牽動{front}的地面戰線。",
                null,
                new[]
                {
                    Choice("引用裁決，加強南海巡弋", "國際地位 +6；一支艦隊改駐南海", "blue", s =>
                    {
                        s.Intl += 6;
                        if (MoveOneFleet(s, "Z4"))
                            SimEngine.Log(s, "艦隊前推至南海，巡弋範圍按裁決表述。", "big");
                        else SimEngine.Log(s, "裁決被引用，但沒有可調的艦隊。");
                    }),
                    Choice("裁決歸裁決，陸軍預算不動", "地面作戰加成 +2%、兵源 +0.5", "gold", s =>
                    {
                        s.BattleBonus += 0.02; s.Manpower += 0.5;
                        SimEngine.Log(s, "南海裁決不改變本季陸軍日程。");
                    }),
                    Choice("低調處理，避免敵前戒備", "日本關係 +3、美國關係 +2；敵軍守軍不變", "purple", s =>
                    {
                        s.JpRel += 3; s.UsRel += 2;
                        SimEngine.Log(s, "裁決低調處理，當前敵前守軍沒有增加。");
                    })
                }),
            Event("hk_2019", 2019, 6, 9, "香港爆發「反送中」抗爭與華南局勢",
                "香港大規模上街。動盪可以送進仍由對岸控制的廣東；廣東若已光復，就改送到{front}。",
                AnyPrc,
                new[]
                {
                    Choice("聲援，把動盪送進廣東或當前敵前", "國際地位 +5、美國關係 +4、民心 +2；廣東仍為敵方則廣東動盪 +12，否則當前敵前動盪 +12", "blue", s =>
                    {
                        s.Intl += 5; s.UsRel += 4; s.Morale += 2;
                        if (s.Control.TryGetValue("GD", out var gf) && gf == Faction.PRC)
                        {
                            s.Unrest["GD"] = SimEngine.Clamp(s.Unrest["GD"] + 12, 0, 100);
                            SimEngine.Log(s, "聲援香港的同時，動盪送進仍由對岸控制的廣東。", "good");
                        }
                        else
                        {
                            RaiseFrontUnrest(s, 12);
                            SimEngine.Log(s, $"廣東已不在對岸手中，動盪改送到{FrontPhrase(s)}。", "good");
                        }
                    }),
                    Choice("只做新聞與人道，不接敵前", "國際地位 +3、民心 +1；敵軍守軍不變", "gold", s =>
                    {
                        s.Intl += 3; s.Morale += 1;
                        SimEngine.Log(s, "支援停在新聞與人道，敵前守軍沒有變動。");
                    }),
                    Choice("趁華南亂，施壓當前敵前", "補給 -4；當前敵前守軍約 -8%", "red", s =>
                    {
                        s.Supply = Math.Max(0, s.Supply - 4);
                        ChipFront(s, 0.08);
                        SimEngine.Log(s, $"我軍趁華南動盪壓迫{FrontPhrase(s)}。", "big");
                    })
                }),
            Event("covid_2020", 2020, 1, 23, "武漢封城與COVID-19新冠疫情爆發",
                "對岸封城，疫情正在往外擴。島上要決定封多緊、軍隊後勤停不停，以及要不要把醫療物資拿去換外交。{front}的補給也在這張單子上。",
                null,
                new[]
                {
                    Choice("島內緊縮，先把庫存補上", "民心 -4、經濟效率 -5%；補給 +14", "blue", s =>
                    {
                        s.Morale -= 4;
                        s.EconMult = Math.Max(0.5, s.EconMult - 0.05);
                        s.Supply = Math.Min(400, s.Supply + 14);
                        SimEngine.Log(s, "島內緊縮，補給庫優先補滿。", "bad");
                    }),
                    Choice("軍隊後勤不停", "補給 -4、地面部隊妥善率下降；經濟效率只降 2%", "red", s =>
                    {
                        s.Supply = Math.Max(0, s.Supply - 4);
                        TireLand(s, 2);
                        s.EconMult = Math.Max(0.5, s.EconMult - 0.02);
                        SimEngine.Log(s, "後勤不停，部隊出勤磨損，經濟只略收。");
                    }),
                    Choice("醫療物資外送，換外交", "台灣 GDP +2%、美國關係 +5、國際地位 +4；地面部隊妥善率下降", "gold", s =>
                    {
                        s.Gdp *= 1.02; s.UsRel += 5; s.Intl += 4;
                        TireLand(s, 3);
                        SimEngine.Log(s, "醫療物資外送，外交分數上升，部隊讓出一部分妥善率。", "good");
                    })
                }),
            Tune(Event("ukraine_2022", 2022, 2, 24, "俄羅斯全面入侵烏克蘭（俄烏戰爭爆發）",
                "歐洲爆發大規模戰爭。島上要決定油料、防衛預算，以及要不要利用這場戰爭對當前壓力點施壓。",
                null,
                new[]
                {
                    Choice("先保住島上油料", "支付採購、經濟效率 -2%；補給 +10", "gold", s =>
                    {
                        SpendTreasury(s, 1.2);
                        s.EconMult = Math.Max(0.5, s.EconMult - 0.02);
                        s.Supply = Math.Min(400, s.Supply + 10);
                        SimEngine.Log(s, "歐洲戰爭推高油價，島上先把燃料庫補上。");
                    }),
                    Choice("提高防衛預算，研讀這場戰爭", "支付增額；地面作戰加成 +3%、防空 +0.04、海軍科技 +0.1", "blue", s =>
                    {
                        SpendTreasury(s, 2);
                        s.BattleBonus += 0.03; s.AirDefense += 0.04; s.NavyTech += 0.1;
                        SimEngine.Log(s, "防衛預算提高，參謀本部開始改寫這場戰爭的教訓。", "good");
                    }),
                    Choice("利用這場戰爭施壓", "北疆有俄軍則俄占省守軍約 -15%；否則當前敵前守軍約 -8%。控制方不變", "red", s =>
                    {
                        var ussr = SimEngine.UssrNodes(s);
                        if (ussr.Count > 0)
                        {
                            ChipHeld(s, ussr, 0.15);
                            SimEngine.Log(s, "歐洲戰事牽動北疆俄軍補給，駐軍收縮，控制區沒有易手。", "big");
                        }
                        else
                        {
                            ChipFront(s, 0.08, 1);
                            SimEngine.Log(s, $"歐洲戰事讓對岸分神，{FrontPhrase(s)}守軍受擾，省境沒有易手。", "big");
                        }
                    })
                }), (s, copy) =>
            {
                var ussr = SimEngine.UssrNodes(s);
                if (ussr.Count > 0)
                {
                    string places = string.Join("、", ussr.Select(id => nodeName(s, id)));
                    copy.Desc = $"歐洲爆發大規模戰爭。北疆仍有俄軍駐守{places}，遠東俄軍的油料與戒備被歐洲戰場牽走。要保島上油料、提高防衛預算，或趁俄軍分神削減這些駐軍。駐軍減少不會改變省的控制方。";
                    copy.Choices[2].Label = $"趁俄軍分神，削弱{places}駐軍";
                    copy.Choices[2].Hint = "俄占省守軍約 -15%，控制方不變";
                }
                else
                {
                    var ctx = AnalyzeFrontline(s);
                    copy.Desc = $"歐洲爆發大規模戰爭。北疆沒有俄軍，這場戰爭主要讓{ctx.TheaterTitle}（{ctx.FrontNames(2)}）的對岸分神，也會打亂島上的能源與軍火價格。";
                    copy.Choices[2].Label = $"趁對岸分神，施壓{ctx.FrontNames(2)}";
                    copy.Choices[2].Hint = "當前敵前守軍約 -8%，省色不變";
                }
            }),
            Event("pelosi_2022", 2022, 8, 2, "美國眾議院議長裴洛西訪台與飛彈戒備",
                "美國會議長準備過境。公開迎接會拉高美國關係，也會讓{front}的飛彈部隊進駐。也可以先把防空和反艦推上去。",
                null,
                new[]
                {
                    Choice("高調迎接", "美國關係 +8、國際地位 +4；若仍有對岸省分，當前敵前守軍 +6%", "gold", s =>
                    {
                        s.UsRel += 8; s.Intl += 4;
                        if (AnyPrc(s)) AlertFront(s, 1.06);
                        SimEngine.Log(s, AnyPrc(s)
                            ? $"過境升高能見度，{FrontPhrase(s)}敵軍飛彈部隊戒備。"
                            : "過境升高能見度。大陸已無對岸省分，沒有敵前戒備可加。", "bad");
                    }),
                    Choice("低調度過", "美國關係 +2、民心 -2；敵軍守軍不變", "blue", s =>
                    {
                        s.UsRel += 2; s.Morale -= 2;
                        SimEngine.Log(s, "過境低調處理，敵前戒備沒有上升。");
                    }),
                    Choice("先部署防空與反艦", "防空 +0.08、海軍科技 +0.15；補給 -6", "red", s =>
                    {
                        s.AirDefense += 0.08; s.NavyTech += 0.15;
                        s.Supply = Math.Max(0, s.Supply - 6);
                        SimEngine.Log(s, "防空與反艦提前展開，補給庫付出代價。", "big");
                    })
                }),
            Tune(Event("drone_2023", 2023, 2, 24, "俄烏戰爭一週年與無人機戰術革新",
                "戰爭打了一年。廉價無人機、油料和防空的算法已經寫在公開戰報裡。這些打法要落在{front}，還是只拿來守島。",
                null,
                new[]
                {
                    Choice("買巡飛彈與無人機，用在當前敵前", "支付軍購、地面作戰加成 +4%；當前敵前守軍約 -6%", "red", s =>
                    {
                        SpendTreasury(s, 2.5);
                        s.BattleBonus += 0.04;
                        ChipFront(s, 0.06, 1);
                        SimEngine.Log(s, $"巡飛彈與無人機套用到{FrontPhrase(s)}，省境沒有易手。", "big");
                    }),
                    Choice("教訓用來守島", "補給 +10、防空 +0.06；攻勢加成下降", "blue", s =>
                    {
                        s.Supply = Math.Min(400, s.Supply + 10);
                        s.AirDefense += 0.06;
                        s.LandingBonus = Math.Max(0, s.LandingBonus - 0.02);
                        SimEngine.Log(s, "這場戰爭的教訓先用來守島，渡海日程放慢。", "good");
                    }),
                    Choice("再看遠東補給線", "北疆有俄軍則俄占省守軍約 -8%；否則當前敵前動盪 +8。控制方不變", "purple", s =>
                    {
                        var ussr = SimEngine.UssrNodes(s);
                        if (ussr.Count > 0)
                        {
                            ChipHeld(s, ussr, 0.08);
                            SimEngine.Log(s, "遠東俄軍補給再縮一輪，控制區沒有易手。", "big");
                        }
                        else
                        {
                            RaiseFrontUnrest(s, 8);
                            SimEngine.Log(s, $"沒有俄占省，心戰改打{FrontPhrase(s)}，不進兵。", "good");
                        }
                    })
                }), (s, copy) =>
            {
                var ussr = SimEngine.UssrNodes(s);
                if (ussr.Count > 0)
                {
                    string places = string.Join("、", ussr.Select(id => nodeName(s, id)));
                    copy.Desc = $"戰爭打了一年。北疆仍有俄軍駐守{places}，遠東補給線比島上的無人機更先被這場戰爭扯動。打法可以落在當前敵前、用來守島，或再削這些俄軍駐軍。駐軍減少不會改變省的控制方。";
                    copy.Choices[2].Label = $"再削{places}的俄軍補給";
                    copy.Choices[2].Hint = "俄占省守軍約 -8%，控制方不變";
                }
            }),
            Event("budget_2024", 2024, 5, 20, "新政府就職與國防國造預算提升",
                "新的防衛預算要佔多少。加在海空、加在民生，或加在不對稱拒止，會改變未來一年對{front}的打法。",
                null,
                new[]
                {
                    Choice("提高防衛佔比", "支付增額、經濟效率 -2%；海軍科技 +0.2、空軍科技 +0.2", "red", s =>
                    {
                        SpendTreasury(s, 3);
                        s.EconMult = Math.Max(0.5, s.EconMult - 0.02);
                        s.NavyTech += 0.2; s.AirTech += 0.2;
                        SimEngine.Log(s, "防衛佔比提高，海空裝備優先於其他預算。", "big");
                    }),
                    Choice("社會預算優先", "民心 +6；攻堅加成略降", "blue", s =>
                    {
                        s.Morale += 6;
                        s.BattleBonus = Math.Max(0, s.BattleBonus - 0.01);
                        SimEngine.Log(s, "預算讓給社會支出，本季攻堅放慢。", "good");
                    }),
                    Choice("不對稱拒止優先", "防空 +0.07、海軍科技 +0.18", "gold", s =>
                    {
                        s.AirDefense += 0.07; s.NavyTech += 0.18;
                        SimEngine.Log(s, "預算改押防空與反艦，不追求對等軍備。", "good");
                    })
                })
        };

        static GameEvent Tune(GameEvent ev, Action<GameState, GameEvent> extra)
        {
            var inner = ev.Specialize;
            ev.Specialize = (s, copy) =>
            {
                inner?.Invoke(s, copy);
                extra(s, copy);
            };
            return ev;
        }

        static string nodeName(GameState s, string id)
            => GameData.Nodes.TryGetValue(id, out var n) ? n.Name : id;

        static EventChoice Choice(string label, string hint, string style, Action<GameState> effect) => new()
        {
            Label = label,
            Hint = hint,
            StyleTag = style,
            Effect = effect
        };

        static GameEvent Event(string id, int year, int month, int day, string title, string desc, Func<GameState, bool> cond, EventChoice[] choices)
        {
            var ev = new GameEvent
            {
                Id = id,
                Year = year,
                Month = month,
                Day = day,
                Tag = "時局",
                Title = title,
                Desc = desc.Replace("{front}", "當前前線"),
                Cond = cond,
                Choices = choices.ToList()
            };
            ev.Specialize = (s, copy) =>
            {
                var ctx = AnalyzeFrontline(s);
                copy.Title = title;
                copy.Desc = desc.Replace("{front}", $"{ctx.TheaterTitle}（{ctx.FrontNames(2)}）");
                string names = ctx.FrontNames(2);
                for (int i = 0; i < copy.Choices.Count && i < choices.Length; i++)
                {
                    copy.Choices[i].Label = choices[i].Label.Replace("當前敵前", names).Replace("當前海域", ctx.PrimaryZoneName);
                    copy.Choices[i].Hint = choices[i].Hint;
                }
            };
            return ev;
        }
    }
}
