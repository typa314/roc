using System;
using System.Collections.Generic;
using System.Linq;

namespace RocGame
{
    public static partial class EventSystem
    {
        public static readonly List<GameEvent> HistoricalEvents = new()
        {
            new GameEvent
            {
                Id = "zhoushan_1950", Year = 1950, Month = 4, Day = 10, Tag = "時局",
                Title = "海南島與舟山群島撤退抉擇",
                Desc = "1950 年春，林彪第四野戰軍背靠華南大陸集結重兵強渡僅二十餘公里寬的瓊州海峽，海口港口受脅、島內又有瓊崖縱隊裡應外合；陳毅第三野戰軍亦重兵緊盯舟山群島，而台灣本島防禦亟待整補且美軍尚未協防。薛岳與石覺急電請示：是否放棄孤島、搶救主力退守台灣？",
                Cond = s => s.Control["ZS"] == Faction.ROC || s.Control["HI"] == Faction.ROC,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "執行撤退計畫，將海南與舟山主力完整轉進台灣",
                        Hint = "海南、舟山守軍安全撤回台灣本島，國庫 +0.6、民心 +4；避免落入跨海千公里添油泥淖",
                        Effect = s => {
                            foreach (var id in new[] { "ZS", "HI" })
                            {
                                if (s.Control[id] != Faction.ROC) continue;
                                foreach (var u in SimEngine.UnitsAt(s, id, UnitDomain.Land)) { u.Loc = "TW"; u.Str = Math.Min(100, u.Str + 10); }
                                s.Control[id] = Faction.PRC; s.Garrison[id] = 2.5;
                            }
                            s.Flags.Remove("holdHainan");
                            s.Flags.Remove("hiPortBlocked");
                            s.Flags.Remove("hiQiongya");
                            s.Treasury += 0.6; s.Morale += 4;
                            SimEngine.Log(s, "海南島與舟山國軍完成戰略撤退，保存實力轉進台灣鞏固中樞。", "good");
                        }
                    },
                    new() {
                        Label = "撤離海南島、但死守舟山群島作為反攻滬寧跳板",
                        Hint = "海南守軍撤回台灣；舟山不撤並強化工事（補給 -4，舟山工事 +1，江蘇共軍警戒增兵）",
                        Effect = s => {
                            if (s.Control["HI"] == Faction.ROC)
                            {
                                foreach (var u in SimEngine.UnitsAt(s, "HI", UnitDomain.Land)) u.Loc = "TW";
                                s.Control["HI"] = Faction.PRC; s.Garrison["HI"] = 2.5;
                            }
                            s.Flags.Remove("holdHainan");
                            s.Flags.Remove("hiPortBlocked");
                            s.Flags.Remove("hiQiongya");
                            s.Supply = Math.Max(0, s.Supply - 4);
                            if (s.Control["ZS"] == Faction.ROC) SimEngine.IncreaseFortification(s, "ZS");
                            if (s.Control["JS"] == Faction.PRC) s.Garrison["JS"] += 1.5;
                            SimEngine.Log(s, "國軍撤離海南島避免添油消耗，但下令死守舟山群島，直指上海長江口！");
                        }
                    },
                    new() {
                        Label = "南北並守！命薛岳死守海南「伯陵防線」、石覺死守舟山（高風險）",
                        Hint = "補給 -8；海口港遭奪封鎖（後續增援須冒砲火消耗運力逆登陸）、瓊崖縱隊內應、四野自瓊州海峽持續增兵",
                        Effect = s => {
                            s.Supply = Math.Max(0, s.Supply - 8);
                            s.Flags["holdHainan"] = 1;
                            if (s.Control["ZS"] == Faction.ROC) SimEngine.IncreaseFortification(s, "ZS");
                            if (s.Control["HI"] == Faction.ROC)
                            {
                                SimEngine.IncreaseFortification(s, "HI");
                                s.Flags["hiPortBlocked"] = 1;
                                s.Flags["hiQiongya"] = 1;
                                s.Garrison["HI"] = Math.Max(s.Garrison["HI"], 4.2);
                            }
                            SimEngine.Log(s, "統帥部下令死守海南與舟山！四野背靠華南強渡瓊州海峽搶佔海口周邊、瓊崖縱隊裡應外合，逾千公里海運補保與本島防務陷入雙重險境！", "bad");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "korean_war_1950", Year = 1950, Month = 6, Day = 25, Tag = "時局",
                Title = "韓戰爆發！美軍第七艦隊進入台灣海峽",
                Desc = "1950 年 6 月 25 日朝鮮半島戰火驟燃，美國總統杜魯門隨即下令第七艦隊進入台灣海峽、宣布「台海中立化」並恢復對華軍經援助！中共緊急暫停渡海攻台整備並準備抽兵北調，李彌將軍亦於滇緬邊境請纓整軍。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "接受美方協防與軍經援助，整軍經武鞏固海峽防務",
                        Hint = "開啟美援（每年 +0.8）、美軍第七艦隊協防生效、美國關係 +15、補給 +6；東南前線敵軍防務受牽制（-15%）",
                        Effect = s => {
                            s.UsAid = 0.8; s.UsProtect = true; s.UsRel += 15; s.Supply += 6;
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "JS", "ZJ", "FJ", "GD", "GX" }, 5, Faction.PRC))
                                s.Garrison[id] *= 0.85;
                            if (s.Control["HLJ"] == Faction.PRC) s.Garrison["HLJ"] += 1.5;
                            SimEngine.Log(s, "韓戰爆發！第七艦隊進駐台海，美援恢復，共軍渡海攻勢受阻。", "good");
                        }
                    },
                    new() {
                        Label = "配合協防並空投支援李彌「滇緬孤軍」整編牽制西南",
                        Hint = "開啟美援 +0.5 與第七艦隊協防、美國關係 +8；獲得游擊部隊與前線牽制效果",
                        Effect = s => {
                            var ctx = AnalyzeFrontline(s);
                            s.UsAid = 0.5; s.UsProtect = true; s.UsRel += 8;
                            if (s.Control["YN"] == Faction.PRC)
                            {
                                s.Unrest["YN"] += 18;
                                s.Garrison["YN"] = Math.Max(1.0, s.Garrison["YN"] - 1.2);
                            }
                            SimEngine.AddUnit(s, "mil", "YN", 82);
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "ZJ", "FJ" }, 2, Faction.PRC))
                            {
                                s.Garrison[id] = Math.Max(1.0, s.Garrison[id] - 1.0);
                                s.Unrest[id] += 10;
                            }
                            SimEngine.AddUnit(s, "mil", s.Control["DC"] == Faction.ROC ? "DC" : ctx.PickForwardRocNode(s), 80);
                            SimEngine.Log(s, "美軍第七艦隊協防台海之際，滇緬孤軍同步展開西南敵後整編牽制！", "big");
                        }
                    },
                    new() {
                        Label = "主動提議派遣三個師赴韓與美軍並肩作戰，換取全力軍援",
                        Hint = "兵源 -3、美國關係 +25、美援 +1.0、第七艦隊協防生效、陸軍科技 +0.4、前線敵軍減損",
                        Effect = s => {
                            s.UsAid = 1.0; s.UsProtect = true; s.UsRel += 25; s.Manpower = Math.Max(0, s.Manpower - 3); s.ArmyTech += 0.4;
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "JS", "ZJ", "FJ", "GD", "GX" }, 5, Faction.PRC))
                                s.Garrison[id] *= 0.80;
                            SimEngine.Log(s, "國府主動請纓赴韓並肩作戰，華府對台軍經承諾大幅加碼！", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "pva_korea_1950", Year = 1950, Month = 10, Day = 25, Tag = "時局",
                Title = "中共「抗美援朝」重兵北調・東南防線現出空隙",
                Desc = "共軍主力部隊大舉開赴東北並跨過鴨綠江參戰，華東、華南兵力連帶北調補防；同時「滇緬孤軍」蓄勢待發，東南游擊與經濟整編亦迎來關鍵窗口。",
                Cond = s => GameData.MainlandIds.Any(id => s.Control[id] == Faction.PRC),
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "支援李彌反共救國軍越境出擊雲南、並擴大沿海敵後破襲",
                        Hint = "雲南起義民兵 +1、西南與東南敵軍各削弱，中共穩定度 -5",
                        Effect = s => {
                            if (s.Control["YN"] == Faction.PRC)
                            {
                                s.Unrest["YN"] = SimEngine.Clamp(s.Unrest["YN"] + 16, 0, 100);
                                s.Garrison["YN"] = Math.Max(0.8, s.Garrison["YN"] - 1.2);
                            }
                            SimEngine.AddUnit(s, "mil", "YN", 85);
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "FJ", "GD", "ZJ" }, 3, Faction.PRC))
                                s.Garrison[id] = Math.Max(0.8, s.Garrison[id] * 0.86);
                            s.PrcStab -= 5;
                            SimEngine.Log(s, "趁共軍精銳北調朝鮮戰場，滇緬孤軍與東南游擊隊多點出擊！", "big");
                        }
                    },
                    new() {
                        Label = "藉韓戰美援順勢推動耕者有其田與工業整編，充實台灣國力",
                        Hint = "GDP +6%、民心 +5、當前科研進度 +1.5、補給 +8",
                        Effect = s => {
                            s.Gdp *= 1.06; s.Morale += 5; s.ResearchProg += 1.5; s.Supply += 8;
                            SimEngine.Log(s, "趁台海局勢趨穩之機全力推動農工整建與科研，後方戰備基底大為鞏固。", "good");
                        }
                    },
                    new() {
                        Label = "派遣海空機艦趁隙強化外島運補與沿海情蒐封鎖網",
                        Hint = "海軍與空軍科技各 +0.2、補給 +6、東南前線動盪 +10",
                        Effect = s => {
                            s.NavyTech += 0.2; s.AirTech += 0.2; s.Supply += 6;
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "FJ", "ZJ", "JS", "GD" }, 4, Faction.PRC))
                                s.Unrest[id] = SimEngine.Clamp(s.Unrest[id] + 10, 0, 100);
                            SimEngine.Log(s, "海空軍把握敵兵北調空檔壓制東南海域並擴大沿岸情報滲透。", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "maag_1951", Year = 1951, Month = 5, Day = 1, Tag = "時局",
                Title = "美軍顧問團（MAAG）抵台・三軍美械換裝磋商",
                Desc = "美軍駐台軍事援助顧問團正式成軍，協助國軍重編散亂番號、建立美式後勤補保體系；惟顧問團就優先「防衛整編」抑或「擴建兩棲海空」徵詢統帥部意見。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "全力配合美式編制與軍紀整頓，先求三軍戰力滿編精實",
                        Hint = "陸軍科技 +0.35、海軍科技 +0.2、本島部隊戰力 +8%、美國關係 +6",
                        Effect = s => {
                            s.ArmyTech += 0.35; s.NavyTech += 0.2; s.UsRel += 6;
                            foreach (var u in SimEngine.UnitsAt(s, "TW", UnitDomain.Land)) u.Str = Math.Min(100, u.Str + 8);
                            SimEngine.Log(s, "美軍顧問團協助國軍完成師級重編，部隊妥善率顯著躍升。", "good");
                        }
                    },
                    new() {
                        Label = "要求顧問團偏重兩棲登陸艇與噴射戰機換裝，為跨海反攻預作準備",
                        Hint = "兩棲登陸加成 +4%、空軍科技 +0.35、海軍與空軍部隊戰力 +6%",
                        Effect = s => {
                            s.LandingBonus += 0.04; s.AirTech += 0.35;
                            foreach (var u in s.Units.Where(x => GameData.UnitTypes[x.Type].Domain != UnitDomain.Land))
                                u.Str = Math.Min(100, u.Str + 6);
                            SimEngine.Log(s, "三軍爭取到重點兩棲與空防裝備撥交，跨海投送戰力穩步成長。", "good");
                        }
                    },
                    new() {
                        Label = "協調將部分軍經美援轉為電力、肥料與民生物資以抑平通膨",
                        Hint = "GDP +6%、國庫 +$0.8 億、民心 +6",
                        Effect = s => {
                            s.Gdp *= 1.06; s.Treasury += 0.8; s.Morale += 6;
                            SimEngine.Log(s, "美援民生物資與基建貸款落地，台灣物價趨穩且民間生產力走強。", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "sino_japan_peace_1952", Year = 1952, Month = 4, Day = 28, Tag = "時局",
                Title = "《中日和約》於台北簽署暨外交地位角力",
                Desc = "美英等國在舊金山與日本簽署和約後，東京政府代表赴台北談判雙邊和約；此舉攸關中華民國法統承認與東南亞僑資往來，但也伴隨賠償與談判讓步爭議。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "務實簽署《台北和約》以鞏固西方陣營承認與戰後貿易通道",
                        Hint = "國際地位 +8、GDP +5%、國庫 +$0.6 億、美國關係 +4",
                        Effect = s => {
                            s.Intl += 8; s.Gdp *= 1.05; s.Treasury += 0.6; s.UsRel += 4;
                            SimEngine.Log(s, "《中日和約》於台北賓館簽署，我國東亞外交認可與對外經貿大幅回穩。", "good");
                        }
                    },
                    new() {
                        Label = "堅持維護抗戰法統尊嚴與嚴正條件，凝聚國內外反共民氣",
                        Hint = "民心 +7、兵源 +2、國際地位 +3",
                        Effect = s => {
                            s.Morale += 7; s.Manpower += 2; s.Intl += 3;
                            SimEngine.Log(s, "我方代表團據理力爭抗戰大義，國內軍民民心士氣為之一振。", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "dongshan_1953", Year = 1953, Month = 7, Day = 16, Tag = "時局",
                Title = "東山島三軍聯合突擊作戰",
                Desc = "韓戰停戰前夕，金門防衛司令胡璉與傘兵總隊擬定突擊福建東山島計畫，測試三軍聯合兩棲與空降作戰能力，並伺機擄獲共軍裝備與兵員。",
                Cond = s => s.Control["KM"] == Faction.ROC && s.Control["FJ"] == Faction.PRC,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "發動東山島突擊，收編俘虜後主動撤回金門",
                        Hint = "金門獲得 1 支投誠部隊（戰力 75%），福建共軍 -1.2 師，兩棲登陸加成 +6%",
                        Effect = s => {
                            s.Garrison["FJ"] = Math.Max(1, s.Garrison["FJ"] - 1.2);
                            SimEngine.AddUnit(s, "dfc", "KM", 75);
                            s.LandingBonus += 0.06;
                            s.Supply = Math.Max(0, s.Supply - 3);
                            SimEngine.Log(s, "東山島突擊成功，收編俘虜與起義士兵 1 個師返回金門！", "good");
                        }
                    },
                    new() {
                        Label = "按兵不動，保存實力鞏固金馬地下坑道防務",
                        Hint = "金門工事強化，補給儲備 +8",
                        Effect = s => {
                            s.Supply += 8; SimEngine.IncreaseFortification(s, "KM");
                            SimEngine.Log(s, "國軍取消東山島行動，全力構築金門地下坑道工事。");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "mutual_treaty_1954", Year = 1954, Month = 9, Day = 3, Tag = "時局",
                Title = "九三砲戰與《中美共同防禦條約》",
                Desc = "1954 年 9 月共軍猛烈砲擊金門，台海爆發第一次危機。美方同意與我國簽署《中美共同防禦條約》，但國務卿杜勒斯要求透過換文約定：國軍對大陸採取任何進攻行動，須先取得美方同意。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "簽署條約與換文，換取美軍正式防衛承諾與高額美援",
                        Hint = "條約生效（美援 +0.5、美國關係 +12、美軍防衛承諾大幅提高）；但未經美方同意進攻大陸將構成違約",
                        Effect = s => {
                            s.Treaty = true; s.TreatyRestrict = true; s.UsProtect = true;
                            s.UsAid += 0.5; s.UsRel += 12; s.Morale += 5;
                            SimEngine.Log(s, "《中美共同防禦條約》正式簽署，台灣與澎湖獲得美軍條約保障。", "good");
                        }
                    },
                    new() {
                        Label = "簽署條約但拒簽限制反攻之秘密換文",
                        Hint = "保有隨時反攻大陸之自由（無違約懲罰），但美國關係 -10、美援減少",
                        Effect = s => {
                            s.Treaty = true; s.TreatyRestrict = false; s.UsProtect = true;
                            s.UsRel -= 10; s.Morale += 6;
                            SimEngine.Log(s, "國府堅持保有自主反攻權，華府雖簽約但對我方抱持戒心。");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "dachen_1955", Year = 1955, Month = 2, Day = 8, Tag = "時局",
                Title = "一江山戰役與大陳島撤退（金剛計畫）",
                Desc = "共軍發動三軍聯合作戰攻陷一江山島，大陳島暴露於共軍岸砲與米格機航程之內。美軍第七艦隊提議協助執行「金剛計畫」，撤離大陳軍民。",
                Cond = s => s.Control["DC"] == Faction.ROC && (s.Control["ZJ"] == Faction.PRC || s.Control["FJ"] == Faction.PRC),
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "執行「金剛計畫」，大陳軍民義胞全數撤退來台",
                        Hint = "大陳守軍撤回台灣，1.8 萬大陳義胞來台（兵源 +2、民心 +5、美國關係 +5），大陳島放棄",
                        Effect = s => {
                            foreach (var u in SimEngine.UnitsAt(s, "DC", UnitDomain.Land)) { u.Loc = "TW"; u.Str = Math.Min(100, u.Str + 10); }
                            s.Control["DC"] = Faction.PRC; s.Garrison["DC"] = 2.0;
                            s.Manpower += 2; s.Morale += 5; s.UsRel += 5;
                            SimEngine.Log(s, "大陳義胞與守軍全數撤抵基隆，防線收縮至金馬台澎。", "good");
                        }
                    },
                    new() {
                        Label = "拒絕撤退！增援大陳島與共軍決戰浙東",
                        Hint = "大陳島獲得 1 支反共救國軍民兵助戰，但共軍即刻對大陳發起渡海猛攻",
                        Effect = s => {
                            SimEngine.AddUnit(s, "mil", "DC", 80);
                            s.Garrison["DC"] = 2.5;
                            SimEngine.Log(s, "國府拒絕撤離大陳，浙東反共游擊隊集結協防，共軍發起登陸戰！", "bad");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "kinmen_823_1958", Year = 1958, Month = 8, Day = 23, Tag = "時局",
                Title = "八二三砲戰（第二次台海危機）",
                Desc = "1958 年 8 月 23 日傍晚，共軍以數百門火砲向金門傾瀉近五萬發砲彈，企圖封鎖金門迫使守軍投降。台海空戰與料羅灣運補隨即展開。",
                Cond = s => s.Control["KM"] == Faction.ROC && s.Control["FJ"] == Faction.PRC,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "執行「鴻運計畫」強行運補，並以響尾蛇飛彈痛擊米格機",
                        Hint = "金門守軍堅守成功（全軍經驗提升、空軍科技 +0.3、民心 +8、國際地位 +5），消耗補給 6",
                        Stance = "固守防線", StyleTag = "blue",
                        Effect = s => {
                            s.Supply = Math.Max(0, s.Supply - 6);
                            s.AirTech += 0.3; s.Morale += 8; s.Intl += 5;
                            foreach (var u in SimEngine.UnitsAt(s, "KM", UnitDomain.Land)) u.Exp = Math.Min(5, u.Exp + 1.5);
                            SimEngine.Log(s, "八二三砲戰大捷！國軍成功突破封鎖，空軍創下輝煌戰果。", "good");
                        }
                    },
                    new() {
                        Label = "下令空軍轟炸福建砲兵陣地與鷹廈鐵路，擴大戰事",
                        Hint = "福建共軍守軍 -2 師，並獲得美方默許反攻窗口（3 年），但美國關係 -6",
                        Stance = "制空壓制", StyleTag = "gold",
                        Effect = s => {
                            s.Garrison["FJ"] = Math.Max(1, s.Garrison["FJ"] - 2);
                            s.UsConsent = Math.Max(s.UsConsent, 3);
                            s.UsRel -= 6;
                            SimEngine.Log(s, "國軍對福建內地發動大規模反擊，台海戰火全面升溫！", "big");
                        }
                    },
                    new() {
                        Label = "陸戰隊自澎湖突襲福建側翼灘頭，發動反攻先導破襲（高風險）",
                        Hint = "福建守軍 -1.5 師、搶灘登陸加成 +10%、民心 +4；但補給 -10、美國關係 -8",
                        Stance = "高風險搶灘", StyleTag = "red",
                        Effect = s => {
                            s.Supply = Math.Max(0, s.Supply - 10);
                            s.Garrison["FJ"] = Math.Max(1, s.Garrison["FJ"] - 1.5);
                            s.LandingBonus += 0.10; s.Morale += 4; s.UsRel -= 8;
                            SimEngine.Log(s, "國軍趁砲戰期間突擊福建沿岸據點並重創共軍海防工事，累積兩棲實戰經驗！", "big");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "tibet_1959", Year = 1959, Month = 3, Day = 17, Tag = "時局",
                Title = "一九五九藏區抗暴與遠程空投行動",
                Desc = "拉薩與康區爆發大規模武裝抗暴運動，「四水六崗衛教軍」與共軍展開激戰。我方空軍特種作戰部隊與美方合作，評估派遣運輸機深入西藏（XZ）與四川（SC）實施武器空投。",
                Cond = s => s.Control["XZ"] == Faction.PRC,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "派遣特種機隊遠程空投武器支援藏區與西南抗暴游擊隊",
                        Hint = "消耗補給 4；西藏（XZ）共軍守軍削減，西藏產生 1 支反共民兵游擊隊牽制共軍",
                        Effect = s => {
                            s.Supply = Math.Max(0, s.Supply - 4);
                            s.Unrest["XZ"] += 35;
                            if (s.Control["SC"] == Faction.PRC) s.Unrest["SC"] += 15;
                            s.Garrison["XZ"] = Math.Max(0.5, s.Garrison["XZ"] - 1.0);
                            SimEngine.AddUnit(s, "mil", "XZ", 85);
                            SimEngine.Log(s, "空軍完成高原特種空投，藏區反共游擊武裝起事牽制西部共軍！", "good");
                        }
                    },
                    new() {
                        Label = "在聯合國大會提案譴責中共，爭取國際輿論支持",
                        Hint = "國際地位 +6、美國關係 +4、中共穩定度 -5",
                        Effect = s => {
                            s.Intl += 6; s.UsRel += 4; s.PrcStab -= 5;
                            SimEngine.Log(s, "我國於聯合國聲援藏胞抗暴，國際輿論譴責北京。", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "great_leap_1960", Year = 1960, Month = 6, Day = 15, Tag = "時局",
                Title = "大躍進失敗與三年大饑荒（國光作業室成立）",
                Desc = "中共推行「大躍進」與人民公社化運動導致嚴重饑荒，大陸經濟倒退、民怨沸騰，各地出現大批逃荒難民，共軍基層官兵亦因家鄉受災而士氣動搖。",
                Cond = s => GameData.MainlandIds.Any(id => s.Control[id] == Faction.PRC),
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "成立「國光作業室」加速反攻整備，同時對大陸空投救濟糧與傳單",
                        Hint = "中共穩定度 -15、敵區全境民心浮動 +15、兩棲登陸加成 +10%",
                        Stance = "整備反攻", StyleTag = "gold",
                        Effect = s => {
                            ShockPrc(s, 15); s.PrcGdpMod = 0.84; s.LandingBonus += 0.10;
                            foreach (var id in GameData.MainlandIds) if (s.Control[id] == Faction.PRC) s.Unrest[id] += 15;
                            SimEngine.Log(s, "大陸深陷三年困難時期，我方成立國光作業室，積極籌劃反攻作戰！", "good");
                        }
                    },
                    new() {
                        Label = "大規模接納流亡青壯與起義人員，編組「反共救國軍」與敵後游擊隊",
                        Hint = "中共穩定度 -12；前線獲得 1 支民兵游擊隊與 1 支投誠部隊、兵源 +3",
                        Stance = "招撫擴軍", StyleTag = "blue",
                        Effect = s => {
                            var ctx = AnalyzeFrontline(s);
                            string milSpawn = ctx.RocFrontNodes.Count > 0 ? ctx.RocFrontNodes[0] : (s.Control["KM"] == Faction.ROC ? "KM" : ctx.PickForwardRocNode(s));
                            ShockPrc(s, 12); s.PrcGdpMod = 0.84; s.Manpower += 3;
                            SimEngine.AddUnit(s, "mil", milSpawn, 85);
                            SimEngine.AddUnit(s, "dfc", ctx.PickForwardRocNode(s), 85);
                            foreach (var id in GameData.MainlandIds) if (s.Control[id] == Faction.PRC) s.Unrest[id] += 10;
                            SimEngine.Log(s, "大批大陸青年與起義官兵投奔自由，編成反共救國軍與起義部隊！", "good");
                        }
                    },
                    new() {
                        Label = "動用秘密經費全面啟動前線敵區「地下反共組織策反」",
                        Hint = "花費國庫 1.2 億；中共穩定度 -18、敵前線及鄰近省分動盪 +22 並削弱守軍",
                        Stance = "情報滲透", StyleTag = "purple",
                        Effect = s => {
                            s.Treasury = Math.Max(0, s.Treasury - 1.2);
                            ShockPrc(s, 18); s.PrcGdpMod = 0.82;
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "FJ", "ZJ", "GD", "GX", "JX", "AH", "HA", "HB", "HN" }, 7, Faction.PRC))
                            {
                                s.Unrest[id] += 22;
                                s.Garrison[id] = Math.Max(1.0, s.Garrison[id] * 0.85);
                            }
                            SimEngine.Log(s, "情報局趁饑荒啟動前線敵後工作網，大陸民間反抗暗潮洶湧！", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "xinjiang_1962", Year = 1962, Month = 4, Day = 22, Tag = "時局",
                Title = "新疆「伊塔事件」與中蘇邊境武裝對峙",
                Desc = "中蘇關係惡化，新疆伊犁、塔城地區逾六萬邊民越境逃往蘇聯，蘇軍在中亞邊境陳兵施壓，中共被迫急調防務兵力北進新疆防蘇。",
                Cond = s => s.Control["XJ"] == Faction.PRC,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "趁共軍西調防蘇，在活躍前線發動「海威行動」特戰突擊",
                        Hint = "解鎖對俄秘密接觸、中蘇緊張度 +25%、當前前線共軍守軍 -15%",
                        Effect = s => {
                            s.Flags["sinoSovietSplit"] = 1;
                            s.PrcSovTension = Math.Max(s.PrcSovTension, 0.25);
                            s.Unrest["XJ"] += 20;
                            if (s.Control["IM"] == Faction.PRC) s.Unrest["IM"] += 15;
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "GD", "GX", "FJ", "ZJ" }, 4, Faction.PRC))
                                s.Garrison[id] *= 0.85;
                            SimEngine.Log(s, "新疆伊塔邊境告急，共軍被迫分兵西北，前線防務出現空隙！", "good");
                        }
                    },
                    new() {
                        Label = "秘密聯絡新疆反共舊部，並透過第三國試探蘇聯意向",
                        Hint = "解鎖對俄秘密接觸，對俄關係 +12，新疆共軍 -1.2 師",
                        Effect = s => {
                            s.Flags["sinoSovietSplit"] = 1;
                            s.SovRel += 12; s.Garrison["XJ"] = Math.Max(1, s.Garrison["XJ"] - 1.2);
                            SimEngine.Log(s, "我方情報網與新疆反共勢力及蘇聯邊境情報站建立秘密聯繫。");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "lop_nor_1964", Year = 1964, Month = 10, Day = 16, Tag = "時局",
                Title = "中共新疆羅布泊核試爆（五九六工程）",
                Desc = "1964 年 10 月 16 日，中共於新疆（XJ）羅布泊成功試爆第一顆原子彈，震驚全球。國府面臨核威懾陰影，必須決定應對戰略。",
                Cond = s => s.Control["XJ"] == Faction.PRC,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "啟動「新竹計畫」，秘密籌建核子研究設施",
                        Hint = "解鎖科技「新竹計畫」，每回合研發點數 +1、當前研發 +4",
                        Effect = s => {
                            s.Flags["prcNuke"] = 1; s.PrcNuke = true; s.RpBonus += 1;
                            s.ResearchProg += 4;
                            SimEngine.Log(s, "中共完成核試。國府秘密指示籌備「新竹計畫」核能研究。", "bad");
                        }
                    },
                    new() {
                        Label = "向華府提議美軍與國軍聯合突襲摧毀新疆羅布泊核設施",
                        Hint = "若美國關係 ≥ 60 則成功重創羅布泊並削弱中共科技；否則獲美方額外撥交戰機聯隊",
                        Effect = s => {
                            s.Flags["prcNuke"] = 1;
                            if (s.UsRel >= 60) {
                                s.PrcTechMod *= 0.85; s.PrcStab -= 10; s.Garrison["XJ"] = Math.Max(1, s.Garrison["XJ"] - 1.5);
                                SimEngine.Log(s, "中美聯合特種行動成功重創新疆羅布泊核設施，中共核武化進程受挫！", "good");
                            } else {
                                s.PrcNuke = true;
                                SimEngine.AddUnit(s, "air", "TW", 95);
                                SimEngine.Log(s, "詹森政府婉拒突襲羅布泊，但撥交新式戰機聯隊安撫台北。");
                            }
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "vietnam_1965", Year = 1965, Month = 3, Day = 8, Tag = "時局",
                Title = "越戰全面升級與開闢南部第二戰場構想",
                Desc = "美軍大規模介入越戰，台灣成為美軍西太平洋後勤基地。統帥部與美方會商：配合越戰牽制華南／西南方向中共兵力，為我方正面戰線減壓。",
                Cond = s => GameData.MainlandIds.Any(id => s.Control[id] == Faction.PRC),
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "開放清泉崗基地並承接美軍後勤訂單，帶動台灣工業起飛",
                        Hint = "經濟效率 +6%、國庫 +3、美國關係 +8、設定越戰協力標記",
                        Effect = s => {
                            s.EconMult += 0.06; s.Treasury += 3; s.UsRel += 8; s.Flags["vietnamHelp"] = 1;
                            SimEngine.Log(s, "越戰美軍後勤訂單湧入台灣，台灣經濟與對美關係雙雙受惠。", "good");
                        }
                    },
                    new() {
                        Label = "獲美方默許對華南與西南敵線實施特戰滲透與游擊牽制",
                        Hint = "獲得美方默許反攻窗口 2 年，敵軍南線及前線守軍各 -1.5 師，並增編 1 支游擊民兵",
                        Effect = s => {
                            var ctx = AnalyzeFrontline(s);
                            s.Flags["vietnamHelp"] = 1; s.UsConsent = Math.Max(s.UsConsent, 2);
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "GD", "GX", "YN" }, 3, Faction.PRC))
                            {
                                s.Garrison[id] = Math.Max(1, s.Garrison[id] - 1.5);
                                s.Unrest[id] += 18;
                            }
                            SimEngine.AddUnit(s, "mil", ctx.PickForwardRocNode(s, "GD"), 85);
                            SimEngine.Log(s, "特戰部隊配合越戰態勢滲透南疆與前線敵區，反共游擊勢力紛紛響應！", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "cultural_rev_1966", Year = 1966, Month = 5, Day = 16, Tag = "時局",
                Title = "文化大革命爆發（十年動亂）",
                Desc = "1966 年大陸爆發「無產階級文化大革命」，紅衛兵串聯批鬥、各地造反派武鬥不止，黨政軍機構陷入癱瘓，工廠停工、科研中斷。這是遷台以來大陸內部最劇烈的動盪時刻！",
                Cond = s => GameData.MainlandIds.Any(id => s.Control[id] == Faction.PRC),
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "把握歷史天機！下令三軍進入「王師北定」最高戰備，策動大陸起義",
                        Hint = "中共穩定度 -22、敵區民心浮動 +20、獲得美方默許窗口 3 年、共軍增援減半，前線獲得起義部隊",
                        Stance = "全面反攻備戰", StyleTag = "red",
                        Effect = s => {
                            var ctx = AnalyzeFrontline(s);
                            ShockPrc(s, 22); s.PrcGdpMod = 0.80; s.PrcTechMod *= 0.9; s.PrcReinfMod = 0.55;
                            s.UsConsent = Math.Max(s.UsConsent, 3);
                            foreach (var id in GameData.MainlandIds) if (s.Control[id] == Faction.PRC) { s.Unrest[id] += 20; s.Garrison[id] *= 0.80; }
                            SimEngine.AddUnit(s, "dfc", ctx.PickForwardRocNode(s), 85);
                            SimEngine.Log(s, "文革武鬥席捲大陸！前線共軍 1 個師集體起義投誠，反攻戰略窗口大開！", "big");
                        }
                    },
                    new() {
                        Label = "推動「中華文化復興運動」並全力衝刺台灣經濟與科技建設",
                        Hint = "中共穩定度 -15；台灣民心 +10、經濟效率 +6%、研發點數 +6",
                        Stance = "文化與經濟建設", StyleTag = "blue",
                        Effect = s => {
                            ShockPrc(s, 15); s.PrcGdpMod = 0.82; s.PrcReinfMod = 0.7;
                            s.Morale += 10; s.EconMult += 0.06; s.ResearchProg += 6;
                            foreach (var id in GameData.MainlandIds) if (s.Control[id] == Faction.PRC) s.Unrest[id] += 12;
                            SimEngine.Log(s, "大陸陷入十年動亂，台灣推行中華文化復興運動並加速經濟起飛。", "good");
                        }
                    },
                    new() {
                        Label = "執行「海威特戰全面滲透」，空投電台武器破壞大陸鐵路樞紐",
                        Hint = "花費國庫 2.0 億；前線敵軍再削 -22%、增援減緩，並於前線戰區獲得 1 支民兵游擊隊",
                        Stance = "敵後游擊破襲", StyleTag = "gold",
                        Effect = s => {
                            var ctx = AnalyzeFrontline(s);
                            s.Treasury = Math.Max(0, s.Treasury - 2.0);
                            ShockPrc(s, 18); s.PrcGdpMod = 0.82; s.PrcReinfMod = 0.62;
                            foreach (var id in PickFromPreferredOrFront(s, new[] { "SD", "JS", "ZJ", "FJ", "GD", "GX" }, 6, Faction.PRC))
                            {
                                s.Garrison[id] *= 0.78;
                                s.Unrest[id] += 16;
                            }
                            string spawn = ctx.RocFrontNodes.Count > 0 ? ctx.RocFrontNodes[0] : (s.Control["ZJ"] == Faction.ROC ? "ZJ" : "YN");
                            SimEngine.AddUnit(s, "mil", spawn, 85);
                            SimEngine.Log(s, "特戰機隊深入前線敵後投放大批武器與電台，各地武鬥派與反共游擊趁勢並起！", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "zhenbao_1969", Year = 1969, Month = 3, Day = 2, Tag = "時局",
                Title = "珍寶島事件與蘇聯趁機南下之密使交涉",
                Desc = "中蘇在東北烏蘇里江珍寶島與新疆鐵列克提爆發激烈邊境戰鬥！蘇聯在北疆陳兵百萬，莫斯科秘密派特使維克多・路易斯（Victor Louis）抵台，試探與國府「南北夾擊中共」——但蘇方暗示事成後將以東北或新疆作為「蘇聯安全緩衝區」！",
                Cond = s => GameData.BorderUssrIds.Any(id => s.Control[id] == Faction.PRC),
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "與蘇聯締結《南北夾擊密約》，默許蘇軍越界進軍北疆（引狼入室）",
                        Hint = "【警訊】蘇軍立即出兵強占【東北】與【新疆】！共軍北方主力崩潰（穩定度 -20、全境守軍 -30%），但北疆落入俄手且民心 -8",
                        Effect = s => {
                            s.Flags["sinoSovietSplit"] = 1; s.PrcSovTension = 0.85;
                            s.SovRel += 30; s.Morale -= 8; s.UsRel -= 8;
                            if (s.Control["HLJ"] == Faction.PRC) SimEngine.SeizeByUssr(s, "HLJ", 8.5);
                            if (s.Control["XJ"] == Faction.PRC) SimEngine.SeizeByUssr(s, "XJ", 6.5);
                            s.PrcStab -= 20;
                            foreach (var id in GameData.MainlandIds) if (s.Control[id] == Faction.PRC) s.Garrison[id] *= 0.70;
                            SimEngine.Log(s, "‼ 蘇聯遠東與中亞方面軍全面越界入侵東北與新疆！共軍腹背受敵陷入大亂！", "big");
                        }
                    },
                    new() {
                        Label = "嚴拒割讓北疆主權！僅以情報合作牽制北方共軍",
                        Hint = "中蘇緊張度升至 65%（共軍沿海與南方守軍抽調北上 -20%），保住主權完整且民心 +6",
                        Effect = s => {
                            s.Flags["sinoSovietSplit"] = 1; s.PrcSovTension = 0.65;
                            s.SovRel += 15; s.Morale += 6;
                            foreach (var id in GameData.MainlandIds) if (s.Control[id] == Faction.PRC && !GameData.Nodes[id].BorderUssr) s.Garrison[id] *= 0.80;
                            SimEngine.Log(s, "中蘇邊境戰雲密布！共軍被迫將大批精銳北調防蘇，前線各省防務空虛。", "good");
                        }
                    },
                    new() {
                        Label = "將蘇聯企圖侵占中國北疆之情報通報華府，換取美方軍援",
                        Hint = "中蘇緊張度 +45%、美國關係 +12、獲得 1 個滿編航空聯隊與 1 支裝甲師",
                        Effect = s => {
                            s.Flags["sinoSovietSplit"] = 1; s.PrcSovTension = 0.45;
                            s.UsRel += 12; SimEngine.AddUnit(s, "air", "TW", 100); SimEngine.AddUnit(s, "arm", "TW", 95);
                            SimEngine.Log(s, "我方與華府共享中蘇邊境情資，獲美方緊急撥交先進戰機聯隊與裝甲裝備。", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "un_2758_1971", Year = 1971, Month = 10, Day = 25, Tag = "時局",
                Title = "聯合國第 2758 號決議與代表權危機",
                Desc = "1971 年 10 月，聯合國大會表決阿爾巴尼亞提案。美方曾建議接受「雙重代表權」以保住席位，但國內堅持「漢賊不兩立」聲浪高漲。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "宣布退出聯合國，「處變不驚、莊敬自強」",
                        Hint = "國際地位 -20，但內部凝聚力提升（民心 +6、兵源 +2）",
                        Stance = "外交拒妥", StyleTag = "blue",
                        Effect = s => {
                            s.Intl -= 20; s.Morale += 6; s.Manpower += 2;
                            SimEngine.Log(s, "我國宣布退出聯合國。發表《告全國同胞書》，號召莊敬自強、處變不驚。", "bad");
                        }
                    },
                    new() {
                        Label = "務實接受「雙重代表權」安排，力保聯合國席位",
                        Hint = "保住國際地位（國際地位 +8、美國關係 +6），但保守派不滿（民心 -8）",
                        Stance = "外交折衝", StyleTag = "gold",
                        Effect = s => {
                            s.Intl += 8; s.UsRel += 6; s.Morale -= 8;
                            SimEngine.Log(s, "國府務實接受雙重代表權方案，成功在聯合國保住席位。", "good");
                        }
                    },
                    new() {
                        Label = "宣布戰時總動員！全額投入中科院研發與整軍備戰",
                        Hint = "兵源 +4、補給 +15、當前研發進度 +8；但國際地位 -22、美國關係 -6",
                        Stance = "戰時總動員", StyleTag = "red",
                        Effect = s => {
                            s.Intl -= 22; s.UsRel -= 6;
                            s.Manpower += 4; s.Supply += 15; s.ResearchProg += 8;
                            SimEngine.Log(s, "面對外交劇變，統帥部轉入最高戰時動員體制，全力擴充戰備與科研！", "big");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "nixon_1972", Year = 1972, Month = 2, Day = 21, Tag = "時局",
                Title = "尼克森訪問北京與《上海公報》",
                Desc = "美國總統尼克森破冰訪問北京，與周恩來簽署《上海公報》，美中聯手抗蘇格局成形，美軍逐步縮減駐台人數。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "推動十大建設與國防自主，降低對美依賴",
                        Hint = "美國關係 -8，但經濟效率 +6%、研發進度 +5",
                        Effect = s => {
                            s.UsRel -= 8; s.EconMult += 0.06; s.ResearchProg += 5;
                            SimEngine.Log(s, "面對尼克森訪華衝擊，政府全力投入十大建設與國防自主研發。");
                        }
                    },
                    new() {
                        Label = "動員國會友台派「中國遊說團」全力穩住華府關係",
                        Hint = "花費國庫 3，美國關係維持穩定（+5）",
                        Effect = s => {
                            s.Treasury = Math.Max(0, s.Treasury - 3); s.UsRel += 5;
                            SimEngine.Log(s, "駐美外交團隊全力遊說美國國會，穩住美台軍事合作基礎。");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "reform_1978", Year = 1978, Month = 12, Day = 18, Tag = "時局",
                Title = "中共十一屆三中全會與「改革開放」",
                Desc = "1978 年底鄧小平主導改革開放，大陸走出文革動亂，設立經濟特區。對岸經濟與國防現代化即將進入高速增長期，軍事反攻的時間窗口正急速收窄！",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "以科技領先對抗量體優勢：全力發展科學園區與高階國防",
                        Hint = "台灣經濟效率 +8%、陸海空科技各 +0.2；中共穩定度回升、增援效率恢復",
                        Effect = s => {
                            s.PrcStab = Math.Max(s.PrcStab, 72); s.PrcGdpMod = 1.05; s.PrcReinfMod = 1.0;
                            s.EconMult += 0.08; s.ArmyTech += 0.2; s.NavyTech += 0.2; s.AirTech += 0.2;
                            SimEngine.Log(s, "大陸啟動改革開放，國力開始急速攀升！台灣加速高科技產業轉型應對。", "big");
                        }
                    },
                    new() {
                        Label = "趁對岸轉型初期軍備尚未換代，集中國力作最後軍事一搏",
                        Hint = "兵源 +4、補給 +18、登陸加成 +12%，並獲美方默許 2 年；但經濟效率 -3%",
                        Effect = s => {
                            s.PrcStab = Math.Max(s.PrcStab, 68); s.PrcReinfMod = 1.0;
                            s.Manpower += 4; s.Supply += 18; s.LandingBonus += 0.12; s.UsConsent = Math.Max(s.UsConsent, 2);
                            s.EconMult -= 0.03;
                            SimEngine.Log(s, "統帥部研判此乃反攻大陸最後戰略窗口，下令三軍總動員！", "big");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "us_break_1979", Year = 1979, Month = 1, Day = 1, Tag = "時局",
                Title = "中美斷交與《台灣關係法》",
                Desc = "1979 年 1 月 1 日美國與中共正式建交，與我國斷交並終止《中美共同防禦條約》。美國國會隨即通過《台灣關係法》，繼續提供防禦性武器。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "促請美國國會強化《台灣關係法》安全條款，並發起自強救國基金",
                        Hint = "共同防禦條約終止、美援歸零，但保留美方防衛承諾、民眾捐獻自強基金（國庫 +8、民心 +5）",
                        Effect = s => {
                            s.Treaty = false; s.TreatyRestrict = false; s.UsAid = 0; s.UsProtect = true;
                            s.Intl -= 10; s.Treasury += 8; s.Morale += 5;
                            SimEngine.Log(s, "中美斷交，全國發起自強救國捐款，空軍編成「自強中隊」。《台灣關係法》生效。", "bad");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "comm817_1982", Year = 1982, Month = 8, Day = 17, Tag = "時局",
                Title = "《八一七公報》與國防自主突破",
                Desc = "雷根政府與北京簽署《八一七公報》，承諾逐步減少對台軍售，但同時對我方作出「六項保證」並暗中轉移技術協助研發自製戰機。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "啟動「鷹揚、天劍、天弓、潛龍」四大國防自主計畫",
                        Hint = "對美軍購門檻提高，但研發點數每年 +3、空軍與海軍科技各 +0.3",
                        Effect = s => {
                            s.Flags["comm817"] = 1; s.RpBonus += 3; s.AirTech += 0.3; s.NavyTech += 0.3;
                            SimEngine.Log(s, "面對軍購受限，中科院全面展開戰機與飛彈自主研發計畫。", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "open_visit_1987", Year = 1987, Month = 7, Day = 15, Tag = "時局",
                Title = "解除戒嚴與開放老兵赴大陸探親",
                Desc = "1987 年台灣解除長達 38 年的戒嚴，並開放退伍老兵赴大陸探親。兩岸民間交流開啟，「台灣經驗」與繁榮生活水準傳入大陸各省，引發大陸民間對體制的反思。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "開放探親與民間交流，以「台灣經驗」進行文化與政治號召",
                        Hint = "台灣民心 +8、國際地位 +8、大陸全境民心浮動 +12",
                        Effect = s => {
                            s.Morale += 8; s.Intl += 8;
                            foreach (var id in GameData.MainlandIds) s.Unrest[id] += 12;
                            SimEngine.Log(s, "開放大陸探親，「台灣經驗」深入大陸人心，國際形象大幅提升。", "good");
                        }
                    },
                    new() {
                        Label = "維持戰時體制與全面封鎖，嚴防統戰滲透",
                        Hint = "兵源 +3、防衛力量穩固，但民心 -6、國際地位 -5",
                        Effect = s => {
                            s.Manpower += 3; s.Morale -= 6; s.Intl -= 5;
                            SimEngine.Log(s, "政府決定維持戒嚴動員體制，持續備戰。");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "tiananmen_1989", Year = 1989, Month = 5, Day = 20, Tag = "時局",
                Title = "一九八九大陸學潮與國際制裁",
                Desc = "1989 年春夏之交，北京與大陸各大城市爆發大規模運動，中共政局劇烈震盪，西方國家隨即對北京實施武器禁運與外交制裁。",
                Cond = s => s.Control["HE"] == Faction.PRC || GameData.MainlandIds.Count(id => s.Control[id] == Faction.PRC) >= 4,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "聲援大陸民運並透過地下網路策動各地駐軍起義",
                        Hint = "中共穩定度 -18、敵區民心浮動 +18；前線獲得 2 支投誠部隊",
                        Stance = "策反起義", StyleTag = "purple",
                        Effect = s => {
                            ShockPrc(s, 18); s.Intl += 6;
                            foreach (var id in GameData.MainlandIds) if (s.Control[id] == Faction.PRC) s.Unrest[id] += 18;
                            string spawn = AnalyzeFrontline(s).PickForwardRocNode(s);
                            SimEngine.AddUnit(s, "dfc", spawn, 85); SimEngine.AddUnit(s, "dfc", spawn, 85);
                            SimEngine.Log(s, "大陸政局震盪，前線共軍 2 個師集體起義倒戈加入國軍！", "big");
                        }
                    },
                    new() {
                        Label = "穩健應對並吸引國際資金與技術轉移台灣",
                        Hint = "中共穩定度 -12；台灣 GDP +8%、美國關係 +8、國際地位 +8",
                        Stance = "經濟與外交吸資", StyleTag = "blue",
                        Effect = s => {
                            ShockPrc(s, 12); s.Gdp *= 1.08; s.UsRel += 8; s.Intl += 8;
                            SimEngine.Log(s, "外資轉向台灣，我國國際空間與高科技產業顯著成長。", "good");
                        }
                    },
                    new() {
                        Label = "趁北京動盪與西方禁運，三軍壓境前線發動聯合威懾",
                        Hint = "花費國庫 6 億；前線敵軍兩省守軍各 -2 師、登陸加成 +12%、獲得美方默許窗口 2 年",
                        Stance = "重兵壓境", StyleTag = "red",
                        Effect = s => {
                            s.Treasury = Math.Max(0, s.Treasury - 6);
                            s.UsConsent = Math.Max(s.UsConsent, 2);
                            s.LandingBonus += 0.12;
                            var targets = PickFromPreferredOrFront(s, new[] { "FJ", "ZJ" }, 2, Faction.PRC);
                            foreach (var tid in targets) s.Garrison[tid] = Math.Max(1, s.Garrison[tid] - 2);
                            string tNames = targets.Count > 0 ? string.Join("、", targets.Select(id => GameData.Nodes[id].Name)) : "敵軍防線";
                            SimEngine.Log(s, $"國軍展開大型聯合威懾演訓，共軍【{tNames}】沿線調度陷入手忙腳亂！", "big");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "ussr_fall_1991", Year = 1991, Month = 12, Day = 26, Tag = "時局",
                Title = "蘇聯解體與新俄羅斯遠東局勢",
                Desc = "1991 年底蘇聯解體為俄羅斯聯邦與各共和國。中共開始自俄羅斯引進 Su-27 與基洛級潛艦；同時我國也獲得向法美採購幻象 2000、F-16 與拉法葉艦的契機。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "啟動「二代兵力整建」（採購 F-16、幻象 2000 與拉法葉艦）",
                        Hint = "花費國庫 15，空軍科技 +0.6、海軍科技 +0.5，並獲得 1 個航空聯隊與 1 支艦隊",
                        Stance = "西方二代換裝", StyleTag = "blue",
                        Effect = s => {
                            s.Flags["ussrGone"] = 1; s.PrcSovTension = 0;
                            s.Treasury = Math.Max(0, s.Treasury - 15);
                            s.AirTech += 0.6; s.NavyTech += 0.5;
                            SimEngine.AddUnit(s, "air", "TW", 95); SimEngine.AddUnit(s, "fleet", "Z2", 95);
                            SimEngine.Log(s, "國軍完成二代兵力換裝，海空戰力大幅躍升！", "good");
                        }
                    },
                    new() {
                        Label = "趁俄國經濟困頓，以重金贖回北疆占領區或延攬前蘇聯軍工專家",
                        Hint = "花費國庫 15；若有俄占區則贖回 1 省主權，且陸海空科技各 +0.4",
                        Stance = "北疆外交贖買", StyleTag = "gold",
                        Effect = s => {
                            s.Flags["ussrGone"] = 1; s.PrcSovTension = 0;
                            s.Treasury = Math.Max(0, s.Treasury - 15);
                            s.ArmyTech += 0.4; s.NavyTech += 0.4; s.AirTech += 0.4;
                            var uNodes = SimEngine.UssrNodes(s);
                            if (uNodes.Count > 0) {
                                string back = uNodes[0];
                                s.Control[back] = Faction.ROC; s.Garrison[back] = 0;
                                SimEngine.AddUnit(s, "inf", back, 85);
                                SimEngine.Log(s, $"★ 我方趁莫斯科財政困頓，以巨額經援成功收回【{GameData.Nodes[back].Name}】主權！", "big");
                            } else {
                                SimEngine.Log(s, "大批前蘇聯航太與造船專家秘密來台任教，國防科技突飛猛進！", "good");
                            }
                        }
                    },
                    new() {
                        Label = "與俄羅斯建立經貿與秘密軍工採購管道，補充重裝與海軍科技",
                        Hint = "花費國庫 12 億；國際地位 +6、海軍科技 +0.4、獲 1 支滿編裝甲師，但美國關係 -8",
                        Stance = "對俄務實採購", StyleTag = "purple",
                        Effect = s => {
                            s.Flags["ussrGone"] = 1; s.PrcSovTension = 0;
                            s.Treasury = Math.Max(0, s.Treasury - 12);
                            s.Intl += 6; s.NavyTech += 0.4; s.UsRel -= 8;
                            SimEngine.AddUnit(s, "arm", "TW", 95);
                            SimEngine.Log(s, "我方自遠東取得大批俄系裝甲與艦用雷達技術，但引來華府關切。", "good");
                        }
                    }
                }
            },
            new GameEvent
            {
                Id = "strait_crisis_1996", Year = 1996, Month = 3, Day = 8, Tag = "時局",
                Title = "一九九六台海飛彈危機",
                Desc = "共軍在台海周邊進行大規模飛彈試射與三軍聯合作戰演習，企圖以武力威懾台灣。台海局勢再度瀕臨戰爭邊緣。",
                Cond = s => s.Control["FJ"] == Faction.PRC || s.Control["ZJ"] == Faction.PRC || s.Control["GD"] == Faction.PRC,
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "協調美軍派遣獨立號、尼米茲號雙航母戰鬥群巡弋台海",
                        Hint = "美軍防衛承諾 +15、美國關係 +6、民心 +5",
                        Effect = s => {
                            s.UsDefBonus += 15; s.UsRel += 6; s.Morale += 5;
                            SimEngine.Log(s, "美軍雙航母戰鬥群駛入台海周邊海域，共軍演習被迫降溫。", "good");
                        }
                    },
                    new() {
                        Label = "展示天弓防空網與雄風反艦飛彈，並對對岸沿海軍事目標實施電子壓制",
                        Hint = "本島防空 +15%、民心 +8、對岸前線共軍戰備受挫（守軍各 -1.5 師）",
                        Effect = s => {
                            s.AirDefense += 0.15; s.Morale += 8;
                            foreach (var tid in PickFromPreferredOrFront(s, new[] { "FJ", "ZJ", "GD" }, 2, Faction.PRC))
                                s.Garrison[tid] = Math.Max(1, s.Garrison[tid] - 1.5);
                            SimEngine.Log(s, "國軍展現強大防空與反制戰力，成功嚇阻共軍渡海企圖！", "good");
                        }
                    }
                }
            }
        };

        public enum FrontlineTheater
        {
            StraitStandoff,
            SouthEastCoast,
            YangtzeCentral,
            NorthChinaPlain,
            NorthernBorderAntiUssr
        }

        public sealed class FrontlineContext
        {
            public FrontlineTheater Theater { get; init; }
            public List<string> HeldMainland { get; init; } = new();
            public List<string> PrcNodes { get; init; } = new();
            public List<string> UssrNodes { get; init; } = new();
            public List<string> PrcFrontNodes { get; init; } = new();
            public List<string> UssrFrontNodes { get; init; } = new();
            public List<string> RocFrontNodes { get; init; } = new();
            public List<string> HostileFrontNodes { get; init; } = new();
            public List<string> ContestedZones { get; init; } = new();

            public bool OnlyUssrRemains => PrcNodes.Count == 0 && UssrNodes.Count > 0;

            public string EnemyLabel(GameState s) => OnlyUssrRemains || (PrcFrontNodes.Count == 0 && UssrFrontNodes.Count > 0)
                ? (s.Year >= 1992 ? "俄軍" : "蘇俄軍")
                : "共軍";

            public string TheaterTitle => Theater switch
            {
                FrontlineTheater.NorthernBorderAntiUssr => "北疆塞外抗俄前線",
                FrontlineTheater.NorthChinaPlain => "華北中原暨黃渤海前線",
                FrontlineTheater.YangtzeCentral => "江淮漢水會戰前線",
                FrontlineTheater.SouthEastCoast => "華南閩粵浙贛前線",
                _ => "台海外島與海防前線"
            };

            public string PrimaryZoneId => ContestedZones.Count > 0
                ? ContestedZones[0]
                : (HeldMainland.Contains("JS") || HeldMainland.Contains("HA") ? "Z1" : "Z3");

            public string PrimaryZoneName => GameData.Zones[PrimaryZoneId].Name;

            public string FrontNames(int max = 3)
            {
                var ids = HostileFrontNodes.Take(max).ToList();
                return ids.Count > 0 ? string.Join("、", ids.Select(id => GameData.Nodes[id].Name)) : "前線敵區";
            }

            public string FrontNotice(GameState s, int salt)
            {
                string enemy = HostileFrontNodes.Count > 0
                    ? GameData.Nodes[HostileFrontNodes[0]].Name
                    : "前線";
                string ours = PickForwardRocNode(s);
                string oursName = GameData.Nodes.TryGetValue(ours, out var n) ? n.Name : "金門";
                string[] lines =
                {
                    $"{enemy}前線一連回報：迫擊砲彈藥只夠再打兩輪，請後方補送。",
                    $"{oursName}收容站今夜接收十餘名攜械投降的士兵，已送往後方訊問。",
                    $"{enemy}觀測所看見對岸炊煙如常，沒有大規模部隊調動。",
                    $"{oursName}運輸連報告山路塌方，乾糧與藥包晚到半日。",
                    $"{enemy}方向傳來零星砲擊，前線一連無人傷亡，但掩體需要修。",
                    $"{oursName}連部收到三名越線投誠的敵軍伙伕，身上沒有武器。",
                    $"{enemy}沿海漁船聚集，疑似在替守軍搬運彈藥，巡邏艇已登記船號。",
                    $"{oursName}醫務所說繃帶還夠，消炎藥開始不足。"
                };
                int i = Math.Abs(salt) % lines.Length;
                return lines[i];
            }

            public string PickForwardRocNode(GameState s, string preferredId = null)
            {
                if (!string.IsNullOrEmpty(preferredId) && s.Control.TryGetValue(preferredId, out var pf) && pf == Faction.ROC)
                    return preferredId;
                if (RocFrontNodes.Count > 0) return RocFrontNodes[0];
                if (HeldMainland.Count > 0) return HeldMainland.OrderByDescending(id => GameData.Nodes[id].Lat).First();
                foreach (var isl in new[] { "KM", "MZ", "DC", "ZS", "HI", "PH", "TW" })
                    if (s.Control.TryGetValue(isl, out var f) && f == Faction.ROC) return isl;
                return "TW";
            }
        }

        public static FrontlineContext AnalyzeFrontline(GameState s)
        {
            var held = SimEngine.HeldMainland(s);
            var prcNodes = GameData.MainlandIds.Where(id => s.Control[id] == Faction.PRC).ToList();
            var ussrNodes = SimEngine.UssrNodes(s);

            var rocFront = held
                .Where(id => GameData.Nodes[id].Adj.Any(a => s.Control[a] != Faction.ROC))
                .OrderByDescending(id => GameData.Nodes[id].Lat)
                .ToList();
            if (rocFront.Count == 0 && held.Count > 0)
                rocFront = held.OrderByDescending(id => GameData.Nodes[id].Lat).ToList();

            var rocZones = GameData.NodeIds
                .Where(id => s.Control[id] == Faction.ROC)
                .SelectMany(id => GameData.Nodes[id].Zones)
                .Distinct()
                .ToHashSet();

            var contestedZones = GameData.ZoneIds
                .Where(z => GameData.Nodes.Values.Any(n => n.Kind == NodeKind.Mainland && n.Zones.Contains(z) && s.Control[n.Id] != Faction.ROC)
                         && (rocZones.Contains(z) || held.Count > 0))
                .ToList();
            // 優先將鄰接未光復省分或當前高緯海區排在前方（戰線北推後優先 Z1 黃渤海，不再停在 Z3 台海）
            if (held.Any(id => id is "JS" or "HA" or "AH" or "SD" or "HE"))
            {
                contestedZones = contestedZones
                    .OrderBy(z => z switch { "Z1" => 0, "Z2" => 1, "Z4" => 2, _ => 3 })
                    .ToList();
            }
            else
            {
                contestedZones = contestedZones
                    .OrderBy(z => z switch { "Z3" => 0, "Z2" => 1, "Z4" => 2, _ => 3 })
                    .ToList();
            }

            var prcAdjFront = prcNodes
                .Where(id => GameData.Nodes[id].Adj.Any(a => s.Control[a] == Faction.ROC))
                .ToList();
            if (prcAdjFront.Count == 0 && prcNodes.Count > 0)
            {
                prcAdjFront = prcNodes
                    .Where(id => GameData.Nodes[id].Zones.Any(z => contestedZones.Contains(z)))
                    .ToList();
                if (prcAdjFront.Count == 0) prcAdjFront = new List<string>(prcNodes);
            }

            var ussrAdjFront = ussrNodes
                .Where(id => GameData.Nodes[id].Adj.Any(a => s.Control[a] == Faction.ROC)
                          || GameData.Nodes[id].Zones.Any(z => contestedZones.Contains(z)))
                .OrderByDescending(id => GameData.Nodes[id].Lat)
                .ToList();
            if (ussrAdjFront.Count == 0 && ussrNodes.Count > 0)
                ussrAdjFront = ussrNodes.OrderByDescending(id => GameData.Nodes[id].Lat).ToList();

            FrontlineTheater theater;
            if (prcNodes.Count == 0 && ussrNodes.Count > 0)
                theater = FrontlineTheater.NorthernBorderAntiUssr;
            else if (held.Count == 0)
                theater = FrontlineTheater.StraitStandoff;
            else if (rocFront.Any(id => GameData.Nodes[id].Lat >= 32.0f)
                     || held.Contains("HE")
                     || ((held.Contains("HA") || held.Contains("HB") || held.Contains("JS"))
                         && prcAdjFront.Any(id => id is "SD" or "HE" or "SX" or "SN" or "LN" or "IM")))
                theater = FrontlineTheater.NorthChinaPlain;
            else if (held.Any(id => id is "JS" or "AH" or "HB" or "HA" or "HN" or "SC")
                     || (!prcNodes.Contains("FJ") && !prcNodes.Contains("GD")))
                theater = FrontlineTheater.YangtzeCentral;
            else
                theater = FrontlineTheater.SouthEastCoast;

            // 依戰區將最前線敵軍省分排前面：北伐至華北／長江時優先華北與沿線目標
            IEnumerable<string> orderedPrc = theater switch
            {
                FrontlineTheater.NorthChinaPlain => prcAdjFront.OrderByDescending(id => GameData.Nodes[id].Lat),
                FrontlineTheater.YangtzeCentral => prcAdjFront.OrderByDescending(id => GameData.Nodes[id].Lat),
                _ => prcAdjFront
                    .OrderBy(id => id is "FJ" or "GD" or "ZJ" or "GX" ? 0 : 1)
                    .ThenBy(id => GameData.Nodes[id].Lat)
            };
            var prcFrontSorted = orderedPrc.ToList();

            var hostileFront = new List<string>();
            if (theater == FrontlineTheater.NorthernBorderAntiUssr || prcFrontSorted.Count == 0)
            {
                hostileFront.AddRange(ussrAdjFront);
                hostileFront.AddRange(prcFrontSorted);
            }
            else
            {
                hostileFront.AddRange(prcFrontSorted);
                foreach (var u in ussrAdjFront)
                    if (!hostileFront.Contains(u)) hostileFront.Add(u);
            }

            return new FrontlineContext
            {
                Theater = theater,
                HeldMainland = held,
                PrcNodes = prcNodes,
                UssrNodes = ussrNodes,
                PrcFrontNodes = prcFrontSorted,
                UssrFrontNodes = ussrAdjFront,
                RocFrontNodes = rocFront,
                HostileFrontNodes = hostileFront,
                ContestedZones = contestedZones
            };
        }

        public static List<string> PickFromPreferredOrFront(GameState s, IEnumerable<string> preferred, int maxCount, Faction? preferredFaction = null)
        {
            var result = new List<string>();
            var ctx = AnalyzeFrontline(s);

            if (preferred != null)
            {
                foreach (var id in preferred)
                {
                    if (!s.Control.TryGetValue(id, out var f) || f == Faction.ROC) continue;
                    if (preferredFaction != null && f != preferredFaction.Value) continue;
                    if (!result.Contains(id))
                    {
                        result.Add(id);
                        if (result.Count >= maxCount) return result;
                    }
                }
            }

            IEnumerable<string> fallback = preferredFaction switch
            {
                Faction.PRC => ctx.PrcFrontNodes.Concat(ctx.PrcNodes),
                Faction.USSR => ctx.UssrFrontNodes.Concat(ctx.UssrNodes),
                _ => ctx.HostileFrontNodes.Concat(ctx.PrcNodes).Concat(ctx.UssrNodes)
            };
            foreach (var id in fallback)
            {
                if (!s.Control.TryGetValue(id, out var f) || f == Faction.ROC) continue;
                if (preferredFaction != null && f != preferredFaction.Value) continue;
                if (!result.Contains(id))
                {
                    result.Add(id);
                    if (result.Count >= maxCount) return result;
                }
            }
            return result;
        }

        public static void TriggerBeijingUssrChoice(GameState s)
        {
            string uNames = string.Join("、", SimEngine.UssrNodes(s).Select(id => GameData.Nodes[id].Name));
            s.PendingEvents.Insert(0, new GameEvent
            {
                Id = "beijing_ussr_choice", Year = s.Year, Month = s.Month, Day = s.Day, Tag = "重大抉擇",
                Title = $"北平光復與北疆俄占區交涉（{uNames}）",
                Desc = $"國軍已攻克北京、推翻中共中樞！然而俄國趁神州戰亂占領了我國北疆（{uNames}），莫斯科要求台北簽署《中俄新勘界條約》承認其占領現狀，否則俄軍裝甲集群將拒絕撤離！",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "「漢家山河寸土不讓！」揮師北上驅逐俄軍，誓言收復全境！",
                        Hint = "戰役繼續！民心 +15、華北獲得 2 支整編裝甲/步兵師與 +20 補給；逐俄收復全境即可贏得完全勝利！",
                        Effect = st => {
                            st.Morale += 15; st.Supply += 20;
                            SimEngine.AddUnit(st, "arm", "HE", 95); SimEngine.AddUnit(st, "inf", "HE", 95);
                            SimEngine.Log(st, "最高統帥於北平發表抗俄宣言，三軍將士揮師塞北，誓驅俄軍收復全境！", "big");
                        }
                    },
                    new() {
                        Label = "將士久戰疲敝，暫簽北疆停火協議凍結交戰",
                        Hint = "補給 +15 並記錄妥協立場；戰役將持續至終戰（若屆時北疆仍未收復，將以【山河破碎・北疆俄占】結算；若期間肅清俄軍仍可達成完全收復）",
                        Effect = st => {
                            st.Flags["acceptUssrSplit"] = 1;
                            st.Supply += 15;
                            SimEngine.Log(st, "台北暫與莫斯科簽署北疆停火協議換取喘息，最終結局仍待全國光復或終戰結算。", "bad");
                        }
                    }
                }
            });
        }

        public static void OnCapture(GameState s, string nid)
        {
            var n = GameData.Nodes[nid];
            if (nid == "GD")
            {
                s.Flags.Remove("hiPortBlocked");
                s.Flags.Remove("hiQiongya");
                SimEngine.Log(s, "★ 收復廣東、截斷雷州半島渡海基地，海南島海口封鎖與瓊崖縱隊威脅解除！", "good");
            }
            if (n.Kind != NodeKind.Mainland) return;
            if (n.Major)
            {
                foreach (var aid in n.Adj)
                {
                    if (s.Control[aid] == Faction.PRC)
                    {
                        s.Garrison[aid] *= 0.84;
                        s.Unrest[aid] = SimEngine.Clamp(s.Unrest[aid] + 10, 0, 100);
                    }
                }
            }
            double unrest = s.Unrest.TryGetValue(nid, out double ur) ? ur : 0;
            double bonusP = (s.Techs.Contains("underground") ? 0.42 : 0.22) + unrest / 150.0;
            if (SimEngine.Chance(s, bonusP))
            {
                var u = SimEngine.AddUnit(s, "mil", nid, 85);
                u.Moved = true;
                SimEngine.Log(s, $"★ {n.Name}地方反共義勇軍紛紛響應王師，編成 1 支民兵游擊隊助戰！", "good");
            }
            double defP = 0.38 + (s.Techs.Contains("hongkong_psy") ? 0.12 : 0.0) + (s.Techs.Contains("defector_award") ? 0.10 : 0.0) + (s.Techs.Contains("united_subvert") ? 0.25 : 0.0) + (s.Techs.Contains("cyber_psy") ? 0.10 : 0.0);
            if (s.PrcStab < 52 && SimEngine.Chance(s, Math.Min(0.85, defP)))
            {
                var d = SimEngine.AddUnit(s, "dfc", nid, 85);
                d.Moved = true;
                SimEngine.Log(s, $"★ {n.Name}共軍殘部陣前起義，整編為 1 支投誠部隊！", "good");
            }

            // 系統 D：光復區治理方針（Major 重鎮彈三選一，一般省分沿用上次選擇）
            s.ProvincePolicy ??= new();
            if (n.Major && !s.FiredEvents.Contains($"lib_policy_{nid}"))
            {
                s.FiredEvents.Add($"lib_policy_{nid}");
                s.PendingEvents.Add(BuildLiberationPolicyEvent(s, nid, n));
            }
            else
            {
                ApplyInheritedLiberationPolicy(s, nid, n);
            }
        }

        private static GameEvent BuildLiberationPolicyEvent(GameState s, string nid, NodeDef n)
        {
            return new GameEvent
            {
                Id = $"lib_policy_{nid}",
                Year = s.Year, Month = s.Month, Day = s.Day, Tag = "光復治理",
                Title = $"光復重鎮【{n.Name}（{n.City}）】治理方針",
                Desc = $"王師克復戰略重鎮【{n.Name}】，地方急須建立軍政秩序。統帥部須裁示採用軍事管制就地徵補、推行民政撫輯恢復生產，或委任地方鄉紳武裝綏靖自治。",
                Choices = new List<EventChoice>
                {
                    new() {
                        Label = "實施軍事管制，就地徵調壯丁與軍需物資",
                        Hint = $"兵源 +2.5、補給 +6、{n.Name}動盪 +12；後續一般光復省分將沿用軍事管制",
                        FactionTag = "army",
                        Effect = st => {
                            st.Manpower += 2.5;
                            st.Supply += 6;
                            st.Unrest[nid] = SimEngine.Clamp(st.Unrest.GetValueOrDefault(nid, 0) + 12, 0, 100);
                            st.LastLiberationPolicy = "military";
                            st.ProvincePolicy[nid] = "military";
                            SimEngine.AddMemoir(st, "光復治理", $"軍事管制【{n.Name}】", "採行戰時軍管就地徵募，全力支應前線推進。", 2);
                            SimEngine.Log(st, $"【{n.Name}】實施軍事管制，完成兵源與軍需徵調（後續一般省分沿用此方針）。", "good");
                        }
                    },
                    new() {
                        Label = "推動民政重建，撫輯流亡並減免田賦恢復工商",
                        Hint = $"民心 +5、補給 -5、{n.Name}動盪歸零且工業歲入 +25%；後續一般省分沿用民政重建",
                        FactionTag = "econ",
                        Effect = st => {
                            st.Morale += 5;
                            st.Supply = Math.Max(0, st.Supply - 5);
                            st.Unrest[nid] = 0;
                            st.LastLiberationPolicy = "civil";
                            st.ProvincePolicy[nid] = "civil";
                            SimEngine.AddMemoir(st, "光復治理", $"民政重建【{n.Name}】", "減免賦稅安撫地方，厚植光復區長期稅收與民心。", 2);
                            SimEngine.Log(st, $"【{n.Name}】推動民政重建，民間迅速安定並恢復稅賦生產。", "good");
                        }
                    },
                    new() {
                        Label = "委任地方綏靖自治，收編保甲武裝協防省境",
                        Hint = $"於{n.Name}編成 1 支地方民兵（戰力 85%）、軍費減負；但地方割據風險 +1 級",
                        FactionTag = "intel",
                        Effect = st => {
                            if (SimEngine.UnitsAt(st, nid, UnitDomain.Land).Count(u => u.Type == "mil") < 3)
                                SimEngine.AddUnit(st, "mil", nid, 85);
                            st.Flags["warlordRisk"] = st.GetFlag("warlordRisk") + 1;
                            st.LastLiberationPolicy = "autonomy";
                            st.ProvincePolicy[nid] = "autonomy";
                            SimEngine.AddMemoir(st, "光復治理", $"綏靖自治【{n.Name}】", "授權地方武裝自治省力省餉，同時埋下藩鎮自主隱患。", 2);
                            SimEngine.Log(st, $"【{n.Name}】採行地方綏靖自治並編成民兵協防，省中央經費但地方自主抬頭。", "");
                        }
                    }
                }
            };
        }

        private static void ApplyInheritedLiberationPolicy(GameState s, string nid, NodeDef n)
        {
            string pol = string.IsNullOrEmpty(s.LastLiberationPolicy) ? "civil" : s.LastLiberationPolicy;
            s.ProvincePolicy[nid] = pol;
            if (pol == "military")
            {
                s.Manpower += 1.2;
                s.Supply += 3;
                s.Unrest[nid] = SimEngine.Clamp(s.Unrest.GetValueOrDefault(nid, 0) + 6, 0, 100);
                SimEngine.Log(s, $"【{n.Name}】沿用軍事管制方針（兵源 +1.2、補給 +3）。", "");
            }
            else if (pol == "autonomy")
            {
                if (SimEngine.UnitsAt(s, nid, UnitDomain.Land).Count(u => u.Type == "mil") < 3)
                {
                    var mu = SimEngine.AddUnit(s, "mil", nid, 75);
                    mu.Moved = true;
                }
                SimEngine.Log(s, $"【{n.Name}】沿用地方綏靖方針（編成地方民兵協防）。", "");
            }
            else
            {
                s.Morale += 2;
                s.Unrest[nid] = 0;
                SimEngine.Log(s, $"【{n.Name}】沿用民政重建方針（民心 +2、動盪平息）。", "");
            }
        }

        static GameEvent PrepareEvent(GameState s, GameEvent ev)
        {
            if (ev.Specialize == null) return ev;
            var copy = new GameEvent
            {
                Id = ev.Id,
                Year = ev.Year,
                Month = ev.Month,
                Day = ev.Day,
                Tag = ev.Tag,
                Title = ev.Title,
                Desc = ev.Desc,
                Choices = ev.Choices.Select(c => new EventChoice
                {
                    Label = c.Label,
                    Hint = c.Hint,
                    Stance = c.Stance,
                    StyleTag = c.StyleTag,
                    FactionTag = c.FactionTag,
                    Effect = c.Effect
                }).ToList()
            };
            ev.Specialize(s, copy);
            return copy;
        }

        public static void CheckHistoricalForDate(GameState s)
        {
            if (s.Over != null) return;
            foreach (var ev in HistoricalEvents)
            {
                if (s.FiredEvents.Contains(ev.Id)) continue;
                bool due = (s.Year > ev.Year)
                        || (s.Year == ev.Year && (s.Month > ev.Month || (s.Month == ev.Month && s.Day >= ev.Day)));
                if (due)
                {
                    if (s.Year > ev.Year && ev.Cond != null)
                    {
                        s.FiredEvents.Add(ev.Id);
                        continue;
                    }
                    if (ev.Cond != null && !ev.Cond(s)) continue;
                    s.FiredEvents.Add(ev.Id);
                    s.PendingEvents.Add(PrepareEvent(s, ev));
                }
            }

            // 系統 A：每五年時代國策（1950/03/01 起，後續 1955/01/01..2020/01/01）
            bool dirDue = s.Day == 1 && ((s.Year == 1950 && s.Month == 3) || (s.Year >= 1955 && s.Year <= 2020 && (s.Year - 1950) % 5 == 0 && s.Month == 1));
            string dirKey = $"era_dir_{s.Year}";
            if (dirDue && !s.FiredEvents.Contains(dirKey))
            {
                s.FiredEvents.Add(dirKey);
                s.PendingEvents.Add(BuildEraDirectiveEvent(s));
            }
        }

        public static GameEvent BuildEraDirectiveEvent(GameState s)
        {
            var ctx = AnalyzeFrontline(s);
            int endY = s.Year == 1950 ? 1955 : Math.Min(GameData.EndYear, s.Year + 5);

            EventChoice MakeDirChoice(string id, string name, string label, string hint, string factionTag, Action<GameState> onPick) => new()
            {
                Label = label,
                Hint = hint,
                FactionTag = factionTag,
                Effect = st =>
                {
                    st.ActiveDirectiveId = id;
                    st.ActiveDirectiveName = name;
                    st.DirectiveStartYear = st.Year;
                    st.DirectiveEndYear = endY;
                    st.DirectiveBaselineGdp = st.Gdp;
                    st.DirectiveBaselineHeld = SimEngine.HeldMainland(st).Count;
                    st.Flags["dirUssrBase"] = SimEngine.UssrNodes(st).Count;
                    onPick?.Invoke(st);
                    SimEngine.AddMemoir(st, "時代國策", $"確立【{name}】", $"{st.Year}–{endY} 年中樞施政總綱。", 3);
                    SimEngine.Log(st, $"★ 中樞拍板 {st.Year}–{endY} 時代國策：【{name}】。", "big");
                }
            };

            List<EventChoice> choices;
            string subTitle;
            if (ctx.OnlyUssrRemains)
            {
                subTitle = "北疆塞外抗俄階段";
                choices = new List<EventChoice>
                {
                    MakeDirChoice("dir_drive_ussr", "還我北疆・塞外決戰",
                        "確立國策【還我北疆・塞外決戰】：全力破敵逐俄",
                        $"未來 {endY - s.Year} 年：對俄地面作戰 +10%、月耗補給 0.2｜五年承諾：收復至少 1 個俄占省分",
                        "army", null),
                    MakeDirChoice("dir_allied_ring", "聯美制俄・國際施壓",
                        "確立國策【聯美制俄・國際施壓】：外交孤立莫斯科",
                        $"未來 {endY - s.Year} 年：每年國際地位 +1、美國關係 +1｜五年承諾：期滿美國關係 ≥ 65 且國際地位 ≥ 60",
                        "econ", null),
                    MakeDirChoice("dir_border_guerrilla", "塞外義軍・斷敵鐵道",
                        "確立國策【塞外義軍・斷敵鐵道】：消耗俄軍遠東補保",
                        $"未來 {endY - s.Year} 年：北疆敵區每月守軍衰減且動盪上升｜五年承諾：俄占減至 ≤ 1 省或光復 ≥ 24 省",
                        "intel", null)
                };
            }
            else if (ctx.HeldMainland.Count >= 1)
            {
                subTitle = $"大陸反攻推進階段（已光復 {ctx.HeldMainland.Count} 省）";
                choices = new List<EventChoice>
                {
                    MakeDirChoice("dir_north_expedition", "王師北定・主力決戰",
                        "確立國策【王師北定・主力決戰】：集中資源擴張戰果",
                        $"未來 {endY - s.Year} 年：全軍地面攻防 +8%、軍費維持 +8%｜五年承諾：再光復 ≥ 3 省或克復河北",
                        "army", null),
                    MakeDirChoice("dir_pacify_rear", "三分軍事・七分政治",
                        "確立國策【三分軍事・七分政治】：安撫後方消弭割據",
                        $"未來 {endY - s.Year} 年：光復省動盪下降、民心緩升、微幅動搖中共｜五年承諾：民心 ≥ 60 且無藩鎮割據風險",
                        "intel", null),
                    MakeDirChoice("dir_port_corridor", "海運動脈・以港養戰",
                        "確立國策【海運動脈・以港養戰】：打通沿海吞吐運補",
                        $"未來 {endY - s.Year} 年：兩棲運力 +2 師、海外吞吐 +3、月補給 +0.5｜五年承諾：握有 ≥ 2 座大陸港口且補給率 ≥ 85%",
                        "navair", null)
                };
            }
            else if (s.Year >= 1970)
            {
                subTitle = "科技經建與台海對峙階段";
                choices = new List<EventChoice>
                {
                    MakeDirChoice("dir_silicon_shield", "科技立國・矽盾防衛",
                        "確立國策【科技立國・矽盾防衛】：傾力升級科研與經濟",
                        $"未來 {endY - s.Year} 年：科研產出 +15%、GDP 成長 +10%、軍費維持 +5%｜五年承諾：GDP 達現值 ×1.35",
                        "econ", null),
                    MakeDirChoice("dir_asymmetric", "不對稱重層嚇阻",
                        "確立國策【不對稱重層嚇阻】：強化海空與防空拒止網",
                        $"未來 {endY - s.Year} 年：海空戰力 +10%、GDP 成長 -4%｜五年承諾：保衛本島且 GDP 達現值 ×1.15",
                        "navair", null),
                    MakeDirChoice("dir_cultural_wave", "台灣經驗・和平演變",
                        "確立國策【台灣經驗・和平演變】：擴大廣播破網與國際聲望",
                        $"未來 {endY - s.Year} 年：持續動搖對岸穩定與國際曝光｜五年承諾：國際地位 ≥ 55 或對岸穩定度 < 55",
                        "intel", null)
                };
            }
            else
            {
                subTitle = "固本整軍與反攻窗口期";
                choices = new List<EventChoice>
                {
                    MakeDirChoice("dir_build_taiwan", "生聚教訓・建設台灣",
                        "確立國策【生聚教訓・建設台灣】：厚植農工財經底座",
                        $"未來 {endY - s.Year} 年：GDP 成長 +15%、軍費維持 +6%｜五年承諾：期滿台灣 GDP 達現值 ×1.30",
                        "econ", null),
                    MakeDirChoice("dir_fort_islands", "固守外島・海空拒止",
                        "確立國策【固守外島・海空拒止】：構築金馬澎堡壘與制海傘",
                        $"未來 {endY - s.Year} 年：外島工事即時強化、海空戰力 +8%、GDP 成長 -5%｜五年承諾：保住至少 4 座島嶼據點",
                        "navair", st =>
                        {
                            foreach (var iid in new[] { "KM", "MZ", "PH" })
                                if (st.Control.GetValueOrDefault(iid) == Faction.ROC) SimEngine.IncreaseFortification(st, iid);
                        }),
                    MakeDirChoice("dir_prep_counter", "一年準備・兩年反攻",
                        "確立國策【一年準備・兩年反攻】：優先動員兵源準備渡海",
                        $"未來 {endY - s.Year} 年：兵源恢復 +30%、登陸戰力 +8%、未登陸前月民心 -0.15｜五年承諾：期滿前光復 ≥ 1 省",
                        "army", null)
                };
            }

            return new GameEvent
            {
                Id = $"era_dir_{s.Year}",
                Year = s.Year, Month = s.Month, Day = s.Day, Tag = "時代國策",
                Title = $"{s.Year}–{endY} 年中樞大政方針抉擇（{subTitle}）",
                Desc = $"時序邁入 {s.Year} 年，統帥部須拍板未來 {endY - s.Year} 年的國家大政總綱。國策一經定案即鎖定施行至 {endY} 年，期滿依承諾成敗賞罰威信！",
                Choices = choices
            };
        }

        public static void CheckEvents(GameState s)
        {
            if (s.Over != null) return;
            CheckHistoricalForDate(s);
            if (s.PendingEvents.Count > 0) return;
            if (s.TotalMonths >= 2) CheckDynamic(s);
        }

        public static void ResolveChoice(GameState s, int choiceIndex)
        {
            if (s.PendingEvents.Count == 0) return;
            var ev = s.PendingEvents[0];
            s.PendingEvents.RemoveAt(0);
            if (choiceIndex >= 0 && choiceIndex < ev.Choices.Count)
            {
                var chosen = ev.Choices[choiceIndex];
                chosen.Effect?.Invoke(s);
                if (ev.Choices.Count > 1 && !ev.Id.StartsWith("dyn_faction_backlash_"))
                    SimEngine.ApplyChoiceFactionShift(s, chosen);
            }
            SimEngine.StepResearch(s, 0, chargeDailySurcharge: false);
            SimEngine.Normalize(s);
            SimEngine.CheckEnd(s, tickAnnualCounters: false);
        }

        public static GameState NewScenario(string preset, uint? seed = null, bool easyMode = false)
        {
            var s = SimEngine.NewGame(seed, easyMode);
            if (preset == "1962")
            {
                s.PendingEvents.Clear();
                foreach (var id in new[] { "ZS", "HI", "DC" })
                {
                    foreach (var u in SimEngine.UnitsAt(s, id, UnitDomain.Land)) u.Loc = "TW";
                    s.Control[id] = Faction.PRC;
                    s.Garrison[id] = 2.0;
                }
                s.Year = 1962;
                s.Month = 1;
                s.Day = 1;
                s.Turn = 12;
                s.Gdp = 29;
                s.Treasury = 5.2;
                s.Supply = 52;
                s.Manpower = 11;
                s.UsRel = 64;
                s.UsAid = 0.9;
                s.UsProtect = true;
                s.Treaty = true;
                s.TreatyRestrict = true;
                s.PrcStab = 45;
                s.PrcGdpMod = 0.85;
                foreach (var t in new[] { "land_reform", "import_sub", "us_reorg", "armor", "airborne", "amphib_doc", "lst_fleet", "front_depot", "jets", "recon_coast", "comint", "black_cat" })
                    SimEngine.GrantTech(s, t);
                SimEngine.AddUnit(s, "arm", "TW", 100);
                SimEngine.AddUnit(s, "abn", "TW", 100);
                SimEngine.AddUnit(s, "amph", "TW", 100);
                foreach (var ev in HistoricalEvents.Where(e => e.Year < 1962)) s.FiredEvents.Add(ev.Id);
                s.Logs.Clear();
                s.IntelReports.Clear();
                s.History.Clear();
                s.History.Add(SimEngine.Snapshot(s));
                SimEngine.Log(s, "【1962 國光計畫劇本】三年大饑荒與中蘇分裂交織，三軍完成整編，反攻第一窗口開啟！", "big");
                SimEngine.SampleIntelReports(s, isOpening: true);
                CheckHistoricalForDate(s);
            }
            else if (preset == "1967")
            {
                s.PendingEvents.Clear();
                foreach (var id in new[] { "ZS", "HI", "DC" })
                {
                    foreach (var u in SimEngine.UnitsAt(s, id, UnitDomain.Land)) u.Loc = "TW";
                    s.Control[id] = Faction.PRC;
                    s.Garrison[id] = 2.0;
                }
                s.Year = 1967;
                s.Month = 1;
                s.Day = 1;
                s.Turn = 17;
                s.Gdp = 48;
                s.Treasury = 8.0;
                s.Supply = 65;
                s.Manpower = 13;
                s.UsRel = 68;
                s.UsAid = 0.6;
                s.UsProtect = true;
                s.Treaty = true;
                s.TreatyRestrict = true;
                s.UsConsent = 3;
                s.PrcStab = 36;
                s.PrcGdpMod = 0.80;
                s.PrcReinfMod = 0.60;
                s.PrcNuke = true;
                s.Flags["prcNuke"] = 1;
                s.Flags["sinoSovietSplit"] = 1;
                s.PrcSovTension = 0.35;
                foreach (var id in GameData.MainlandIds)
                {
                    s.Unrest[id] = 26;
                    s.Garrison[id] *= 0.82;
                }
                foreach (var t in new[] { "land_reform", "import_sub", "export_zone", "us_reorg", "armor", "airborne", "amphib_doc", "lst_fleet", "front_depot", "amphib_group", "jets", "sidewinder", "f104", "f5e", "recon_coast", "comint", "black_cat", "hkmacau_net", "psywar", "hongkong_psy", "underground" })
                    SimEngine.GrantTech(s, t);
                SimEngine.AddUnit(s, "arm", "TW", 100);
                SimEngine.AddUnit(s, "arm", "TW", 100);
                SimEngine.AddUnit(s, "abn", "TW", 100);
                SimEngine.AddUnit(s, "mar", "TW", 100);
                SimEngine.AddUnit(s, "amph", "TW", 100);
                SimEngine.AddUnit(s, "amph", "TW", 100);
                SimEngine.AddUnit(s, "dfc", "KM", 85);
                foreach (var ev in HistoricalEvents.Where(e => e.Year <= 1966)) s.FiredEvents.Add(ev.Id);
                s.Logs.Clear();
                s.IntelReports.Clear();
                s.History.Clear();
                s.History.Add(SimEngine.Snapshot(s));
                SimEngine.Log(s, "【1967 文革風暴劇本】大陸全面陷入文革武鬥、中蘇邊境劍拔弩張，王師北定最佳戰機！", "big");
                SimEngine.SampleIntelReports(s, isOpening: true);
                CheckHistoricalForDate(s);
            }
            return s;
        }
    }
}
