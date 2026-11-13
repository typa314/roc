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
                            s.PrcStab -= 15; s.PrcGdpMod = 0.84; s.LandingBonus += 0.10;
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
                            s.PrcStab -= 12; s.PrcGdpMod = 0.84; s.Manpower += 3;
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
                            s.PrcStab -= 18; s.PrcGdpMod = 0.82;
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
                            s.PrcStab -= 22; s.PrcGdpMod = 0.80; s.PrcTechMod *= 0.9; s.PrcReinfMod = 0.55;
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
                            s.PrcStab -= 15; s.PrcGdpMod = 0.82; s.PrcReinfMod = 0.7;
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
                            s.PrcStab -= 18; s.PrcGdpMod = 0.82; s.PrcReinfMod = 0.62;
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
                            s.PrcStab -= 18; s.Intl += 6;
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
                            s.PrcStab -= 12; s.Gdp *= 1.08; s.UsRel += 8; s.Intl += 8;
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
        }

        public static void CheckEvents(GameState s)
        {
            if (s.Over != null) return;
            CheckHistoricalForDate(s);
            if (s.PendingEvents.Count > 0) return;
            if (s.TotalMonths >= 2) CheckDynamic(s);
        }

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
                            Hint = $"消耗補給 3；在{GameData.Nodes[spawnLoc].Name}獲得 1 支「民兵/游擊」部隊（戰力 85%），並削弱{pName}{milEnemy} 1.0 師",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 3);
                                SimEngine.AddUnit(st, "mil", spawnLoc, 85);
                                if (st.Control[pid] != Faction.ROC) st.Garrison[pid] = Math.Max(0.5, st.Garrison[pid] - 1.0);
                                SimEngine.Log(st, $"★ {pName}{milCause}民兵獲我方空投支援，於【{GameData.Nodes[spawnLoc].Name}】正式加入戰鬥序列！", "good");
                            }
                        },
                        new() {
                            Label = $"指示民兵就地潛伏，襲擊{milEnemy}糧倉與鐵路補給線",
                            Hint = $"{pName}{milEnemy}守軍 -1.6 師、民心浮動 +12，我方獲得情報與兵源 +1.5",
                            Effect = st => {
                                if (st.Control[pid] != Faction.ROC) st.Garrison[pid] = Math.Max(0.5, st.Garrison[pid] - 1.6);
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
                            Hint = $"在{GameData.Nodes[destNode].Name}獲得 1 支「投誠部隊」（戰力 85%），{defSrcName}共軍 -1 師，民心 +4",
                            Effect = st => {
                                SimEngine.AddUnit(st, "dfc", destNode, 85);
                                if (st.Control[defSrcId] == Faction.PRC)
                                    st.Garrison[defSrcId] = Math.Max(0.8, st.Garrison[defSrcId] - 1.0);
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
                                    Hint = $"補給 -4；空軍與海軍科技各 +0.25、{ztNames}敵軍各 -0.6 師、美國關係 -2",
                                    Effect = st => {
                                        st.Supply = Math.Max(0, st.Supply - 4);
                                        st.AirTech += 0.25; st.NavyTech += 0.25;
                                        foreach (var tid in zoneTargets)
                                            if (st.Control[tid] != Faction.ROC) st.Garrison[tid] = Math.Max(0.5, st.Garrison[tid] - 0.6);
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
                                Hint = $"補給 -4；空軍與陸軍科技各 +0.25、{atNames}{enemyTerm}守軍各 -0.7 師",
                                Effect = st => {
                                    st.Supply = Math.Max(0, st.Supply - 4);
                                    st.AirTech += 0.25; st.ArmyTech += 0.25;
                                    foreach (var tid in airTargets)
                                        if (st.Control[tid] != Faction.ROC) st.Garrison[tid] = Math.Max(0.5, st.Garrison[tid] - 0.7);
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
                                Hint = $"國庫 +$0.5 億、補給 +10、{tgtNames}敵軍略減，但國際地位 -4",
                                Effect = st => {
                                    st.Treasury += 0.5; st.Supply += 10; st.Intl -= 4;
                                    foreach (var id in ctx.HostileFrontNodes.Take(2))
                                        if (st.Control[id] != Faction.ROC) st.Garrison[id] = Math.Max(0.5, st.Garrison[id] - 0.5);
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
                                foreach (var id in frontTargets)
                                {
                                    if (st.Control[id] != Faction.ROC)
                                    {
                                        st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 10, 0, 100);
                                        st.Garrison[id] = Math.Max(0.5, st.Garrison[id] - 0.5);
                                    }
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
                            Hint = $"國庫 -${(0.30 * SimEngine.PriceIndex(s)):F1} 億；{tNames}動盪 +16、敵守軍 -0.8 師、中共穩定度 -5",
                            Effect = st => {
                                SpendTreasury(st, 0.30);
                                st.PrcStab -= 5;
                                foreach (var id in targets)
                                {
                                    if (st.Control[id] == Faction.PRC)
                                    {
                                        st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 16, 0, 100);
                                        st.Garrison[id] = Math.Max(0.5, st.Garrison[id] - 0.8);
                                    }
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
                            Hint = $"{tNames}共軍佈防暴露削弱（-0.9 師）、中共穩定度 -4",
                            Effect = st => {
                                st.PrcStab -= 4;
                                foreach (var id in targets)
                                    if (st.Control[id] == Faction.PRC) st.Garrison[id] = Math.Max(0.5, st.Garrison[id] - 0.9);
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
                        Hint = $"國庫 -${(0.45 * px):F1} 億、補給 -4；{ftNames}{enemyTerm}守軍各 -0.9 師、地面戰鬥 +3%",
                        Effect = st => {
                            SpendTreasury(st, 0.45);
                            st.Supply = Math.Max(0, st.Supply - 4);
                            st.BattleBonus += 0.03;
                            foreach (var tid in frontTargets)
                                if (st.Control[tid] != Faction.ROC) st.Garrison[tid] = Math.Max(0.5, st.Garrison[tid] - 0.9);
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
                            Hint = $"國庫 -${(0.40 * px):F1} 億、補給 -4；{frontNames3}{enemyTerm}守軍各 -0.8 師、陸軍科技 +0.2",
                            Effect = st => {
                                SpendTreasury(st, 0.40);
                                st.Supply = Math.Max(0, st.Supply - 4);
                                st.ArmyTech += 0.2;
                                foreach (var id in frontTargets.Take(3))
                                    if (st.Control[id] != Faction.ROC)
                                        st.Garrison[id] = Math.Max(0.5, st.Garrison[id] - 0.8);
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
                                Hint = $"國庫 -${(0.45 * px):F1} 億、補給 -4；{uNames}{uEnemy}守軍各 -1.0 師、前線戰力 +6%",
                                Effect = st => {
                                    SpendTreasury(st, 0.45);
                                    st.Supply = Math.Max(0, st.Supply - 4);
                                    foreach (var uid in uTargets)
                                        if (st.Control[uid] == Faction.USSR) st.Garrison[uid] = Math.Max(0.5, st.Garrison[uid] - 1.0);
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
                            Hint = $"補給 -2；{frontNames3}{enemyTerm}動盪 +12 並削弱守軍 -0.6 師、民心 +3",
                            Effect = st => {
                                st.Supply = Math.Max(0, st.Supply - 2);
                                st.Morale += 3;
                                foreach (var id in frontTargets.Take(3))
                                {
                                    if (st.Control[id] != Faction.ROC)
                                    {
                                        st.Unrest[id] = SimEngine.Clamp(st.Unrest[id] + 12, 0, 100);
                                        st.Garrison[id] = Math.Max(0.5, st.Garrison[id] - 0.6);
                                    }
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

        public static void ResolveChoice(GameState s, int choiceIndex)
        {
            if (s.PendingEvents.Count == 0) return;
            var ev = s.PendingEvents[0];
            s.PendingEvents.RemoveAt(0);
            if (choiceIndex >= 0 && choiceIndex < ev.Choices.Count)
            {
                ev.Choices[choiceIndex].Effect?.Invoke(s);
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
