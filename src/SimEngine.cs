using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RocGame
{
    public class Unit
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Loc { get; set; }
        public double Str { get; set; } = 100.0;
        public bool Moved { get; set; }
        public double Exp { get; set; }
        public double SupFactor { get; set; } = 1.0;
    }

    public class MoveOrder
    {
        public int UnitId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Kind { get; set; } // land, sea, air
        public double Lift { get; set; }
        public string Zone { get; set; }
        public List<string> Path { get; set; } = new();
        public int TotalDays { get; set; } = 1;
        public int ElapsedDays { get; set; } = 0;
        public bool IsPrep { get; set; } = false;
        public int PrepDays { get; set; } = 0;
        public int PrepRequired { get; set; } = 0;
        public bool ManualOverride { get; set; } = true;
        public bool FromAutoFront { get; set; } = false;

        [JsonIgnore]
        public double VisualProgress => TotalDays <= 0 ? 1.0 : Math.Max(0.0, Math.Min(1.0, (double)ElapsedDays / TotalDays));

        [JsonIgnore]
        public int RemainingDays => Math.Max(0, TotalDays - ElapsedDays);
    }

    public class LogEntry
    {
        public string Date { get; set; }
        public string Msg { get; set; }
        public string Kind { get; set; } // "", "good", "bad", "big"
    }

    public class HistPoint
    {
        public int Year { get; set; }
        public double RocGdp { get; set; }
        public double PrcGdp { get; set; }
        public double RocMil { get; set; }
        public double PrcMil { get; set; }
    }

    public class EnemyMoveRecord
    {
        public string From { get; set; }
        public string To { get; set; }
        public bool Landing { get; set; }
    }

    public class IntelReport
    {
        public string Date { get; set; }
        public string Cat { get; set; }   // 密謀警示, 部隊調動, 蘇俄動態, 敵後線索, 電偵動態
        public string Level { get; set; } // alert, warn, good, info
        public string Text { get; set; }
    }

    public class TurnReport
    {
        public List<string> BattleNodes { get; set; } = new();
        public List<EnemyMoveRecord> PrcMoves { get; set; } = new();
    }

    public class GameOverInfo
    {
        public string Key { get; set; }
        public string Group { get; set; }
        public string Kind { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public int Score { get; set; }
    }

    public class CheckpointRecord
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public int HeldCount { get; set; }
        public double RocGdp { get; set; }
        public int Score { get; set; }
        public string UnlockHint { get; set; }
        public string StateJson { get; set; }
    }

    public class CheckpointSpec
    {
        public string Key;
        public string Title;
        public string RequirementDesc;
        public Func<GameState, bool> Cond;
    }

    public class EventChoice
    {
        public string Label { get; set; }
        public string Hint { get; set; }
        public string Stance { get; set; }
        public string StyleTag { get; set; }
        [JsonIgnore]
        public Action<GameState> Effect { get; set; }
    }

    public class GameEvent
    {
        public string Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; } = 1;
        public int Day { get; set; } = 1;
        public string Tag { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        [JsonIgnore]
        public Func<GameState, bool> Cond { get; set; }
        public List<EventChoice> Choices { get; set; } = new();
        [JsonIgnore]
        public Action<GameState, GameEvent> Specialize { get; set; }
    }

    public class GameState
    {
        public uint Seed { get; set; }
        public int Year { get; set; } = GameData.StartYear;
        public int Month { get; set; } = 1;
        public int Day { get; set; } = 1;
        public int Turn { get; set; } = 0;
        public int FrontSig { get; set; }
        public int FrontStableSince { get; set; }

        [JsonIgnore]
        public int TotalMonths => (Year - GameData.StartYear) * 12 + (Month - 1);

        public double Treasury { get; set; } = 1.25; // 1950年初國庫運台黃金與外匯扣除初期軍費後實質可調度餘額 (~$1.25億美元)
        public double Debt { get; set; } = 0.0;
        public double Gdp { get; set; } = 14.0;      // 1950年台灣名義GDP規模 (~$14億美元)
        public string Tax { get; set; } = "mid"; // low, mid, high
        public int ResLevel { get; set; } = 1;

        public double Morale { get; set; } = 52.0;
        public double UsRel { get; set; } = 38.0;
        public double JpRel { get; set; } = 20.0;
        public double Intl { get; set; } = 55.0;
        public double SovRel { get; set; } = 0.0;

        public double EconMult { get; set; } = 0.85;
        public double ArmyTech { get; set; } = 1.0;
        public double NavyTech { get; set; } = 1.0;
        public double AirTech { get; set; } = 1.0;
        public double LandingBonus { get; set; } = 0.0;
        public double BattleBonus { get; set; } = 0.0;
        public double LiftBonus { get; set; } = 0.0;
        public double SupplyCap { get; set; } = 6.0;
        public double RpBonus { get; set; } = 0.0;
        public double AirDefense { get; set; } = 0.0;
        public double UsDefBonus { get; set; } = 0.0;
        public int InfraCount { get; set; } = 0;

        public double Supply { get; set; } = 24.0;
        public double Manpower { get; set; } = 6.5;
        public double SupplyRatio { get; set; } = 1.0;

        public HashSet<string> Techs { get; set; } = new();
        public string ResearchId { get; set; }
        public double ResearchProg { get; set; }
        public Dictionary<string, double> ResearchProgMap { get; set; } = new();
        public List<string> ActiveTechs { get; set; } = new();
        public List<string> ResearchQueue { get; set; } = new();
        public bool ResearchRush { get; set; } = false;
        public string LastTechCompleted { get; set; }
        public string LastTechDate { get; set; }

        public List<Unit> Units { get; set; } = new();
        public int NextUnitId { get; set; } = 1;

        public Dictionary<string, Faction> Control { get; set; } = new();
        public Dictionary<string, double> Garrison { get; set; } = new();
        public Dictionary<string, int> Fortification { get; set; } = new();
        public Dictionary<string, double> Unrest { get; set; } = new();
        public Dictionary<string, double> Fog { get; set; } = new();

        public double PrcStab { get; set; } = 65.0;
        public double PrcGdpMod { get; set; } = 1.0;
        public double PrcTechMod { get; set; } = 1.0;
        public double PrcSovTension { get; set; } = 0.0;
        public bool PrcNuke { get; set; } = false;
        public double PrcReinfMod { get; set; } = 1.0;

        public double UsAid { get; set; } = 0.0;
        public bool UsProtect { get; set; } = false;
        public bool Treaty { get; set; } = false;
        public bool TreatyRestrict { get; set; } = false;
        public int UsConsent { get; set; } = 0;

        public Dictionary<string, int> Flags { get; set; } = new();
        public HashSet<string> FiredEvents { get; set; } = new();
        public Dictionary<string, int> Cooldowns { get; set; } = new();
        public HashSet<string> ActionsUsed { get; set; } = new();
        public HashSet<string> AutoActions { get; set; } = new() { "bonds", "jp_trade" };

        public List<MoveOrder> Orders { get; set; } = new();
        public bool AutoFrontFill { get; set; } = true;
        public HashSet<string> PriorityTargets { get; set; } = new();
        public List<LogEntry> Logs { get; set; } = new();
        public List<IntelReport> IntelReports { get; set; } = new();
        public List<HistPoint> History { get; set; } = new();
        [JsonIgnore]
        public List<GameEvent> PendingEvents { get; set; } = new();
        public TurnReport LastReport { get; set; } = new();
        public GameOverInfo Over { get; set; }

        public bool EasyMode { get; set; } = false;
        public int BattlesWon { get; set; }
        public double RocLoss { get; set; }
        public double EnemyLoss { get; set; }

        public int GetFlag(string k) => Flags.TryGetValue(k, out int v) ? v : 0;
        public bool HasFlag(string k) => GetFlag(k) > 0;
    }

    public class ActionDef
    {
        public string Id;
        public string Cat; // 外交, 內政, 情報
        public string Name;
        public string Desc;
        public bool NeedTarget;
        public Func<GameState, double> Cost;
        public Func<GameState, bool> Avail;
        public Func<GameState, string, bool> TargetOk;
        public Action<GameState, string> Execute;
    }

    public static class SimEngine
    {
        public static double Rng(GameState s)
        {
            s.Seed += 0x6D2B79F5u;
            uint t = s.Seed;
            t = (t ^ (t >> 15)) * (t | 1u);
            t ^= t + (t ^ (t >> 7)) * (t | 61u);
            return ((t ^ (t >> 14)) & 0xFFFFFFFFu) / 4294967296.0;
        }

        public static bool Chance(GameState s, double p) => Rng(s) < p;
        public static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
        public static string DateStr(GameState s) => $"{s.Year}/{s.Month:D2}/{s.Day:D2}";
        public static int DaysToNextMonth(GameState s) => Math.Max(1, DateTime.DaysInMonth(s.Year, s.Month) - s.Day + 1);
        public static double PriceIndex(GameState s) => GameData.Interp(GameData.CurvePriceIndex, s.Year + (s.Month - 1) / 12.0);

        public static void Log(GameState s, string msg, string kind = "")
        {
            s.Logs.Insert(0, new LogEntry { Date = DateStr(s), Msg = msg, Kind = kind });
            if (s.Logs.Count > 250) s.Logs.RemoveAt(s.Logs.Count - 1);
        }

        public static GameState NewGame(uint? seed = null, bool easyMode = false)
        {
            var s = new GameState { Seed = seed ?? (uint)Environment.TickCount, EasyMode = easyMode };
            foreach (var id in GameData.NodeIds)
            {
                var n = GameData.Nodes[id];
                s.Control[id] = n.Kind == NodeKind.Mainland ? Faction.PRC : Faction.ROC;
                s.Garrison[id] = n.BaseGarrison;
                s.Fortification[id] = n.Fort;
                s.Unrest[id] = n.Kind == NodeKind.Mainland ? 10.0 : 0.0;
            }

            void Add(string t, string loc, int count, double str = 100.0)
            {
                for (int i = 0; i < count; i++) AddUnit(s, t, loc, str);
            }
            // 1950 年初國軍撤退來台真實建制規模（總員額號稱60萬，惟多數撤台陸軍師缺編整編中、海空軍油料零件妥善率受限）
            Add("inf", "TW", 11, 75.0); Add("mar", "TW", 2, 85.0); Add("arm", "TW", 1, 70.0); // 含戰車部隊殘編整建之裝甲總隊
            Add("inf", "KM", 3, 88.0);  Add("inf", "MZ", 1, 78.0);
            Add("inf", "DC", 1, 75.0);  Add("inf", "ZS", 2, 85.0);
            Add("inf", "HI", 2, 65.0);  Add("inf", "PH", 1, 82.0);
            Add("fleet", "Z3", 2, 80.0); Add("fleet", "Z2", 1, 78.0); Add("fleet", "TW", 1, 75.0);
            Add("air", "Z3", 1, 78.0);   Add("air", "Z2", 1, 75.0);   Add("air", "TW", 1, 75.0);
            Add("amph", "TW", 2, 85.0);
            s.Flags["hiQiongya"] = 1;

            RollFog(s);
            s.History.Add(Snapshot(s));
            Log(s, "1950年國府播遷來台，黃金存底僅餘約1.25億美元；本島亟待整編、舟山與海南孤懸海外且補給線逾千公里，是否收縮防線或跨海固守由統帥決斷。", "big");
            SampleIntelReports(s, isOpening: true);
            EventSystem.CheckEvents(s);
            return s;
        }

        public static Unit AddUnit(GameState s, string type, string loc, double str = 100.0)
        {
            var u = new Unit { Id = s.NextUnitId++, Type = type, Loc = loc, Str = str };
            s.Units.Add(u);
            return u;
        }

        public static int FortificationLevel(GameState s, string id)
            => (int)(s.Fortification.TryGetValue(id, out int level) ? Clamp(level, 0, 3) : Clamp(GameData.Nodes[id].Fort, 0, 3));

        public static (bool ok, string reason) RecruitLocalMilitia(GameState s, string id)
        {
            if (!GameData.Nodes.TryGetValue(id, out var node) || node.Kind != NodeKind.Mainland || s.Control[id] != Faction.ROC)
                return (false, "只能在我方控制的大陸省分收編地方民兵。");
            if (UnitsAt(s, id, UnitDomain.Land).Count(u => u.Type == "mil") >= 3)
                return (false, "本省地方民兵已達 3 支上限。");
            const double manpowerCost = 0.5;
            double moneyCost = 0.08 * PriceIndex(s);
            if (s.Manpower < manpowerCost) return (false, "兵源不足，收編需要 0.5 兵源。");
            if (s.Treasury < moneyCost) return (false, $"國庫不足，收編需要 {moneyCost:F2} 億美元。");

            s.Manpower -= manpowerCost;
            s.Treasury -= moneyCost;
            var unit = AddUnit(s, "mil", id, 65.0);
            unit.Moved = true;
            Log(s, $"【{node.Name}】完成地方民兵收編（兵源 -0.5，國庫 -{moneyCost:F2}）。", "good");
            return (true, "");
        }

        public static (bool ok, string reason) FortifyProvince(GameState s, string id)
        {
            if (!GameData.Nodes.TryGetValue(id, out var node) || node.Kind != NodeKind.Mainland || s.Control[id] != Faction.ROC)
                return (false, "只能加固我方控制的大陸省分。");
            int current = FortificationLevel(s, id);
            if (current >= 3) return (false, "本省陣地已達最高 3 級。");
            double moneyCost = 0.12 * (current + 1) * PriceIndex(s);
            if (s.Treasury < moneyCost) return (false, $"國庫不足，加固需要 {moneyCost:F2} 億美元。");

            s.Treasury -= moneyCost;
            s.Fortification[id] = current + 1;
            Log(s, $"【{node.Name}】陣地加固至 {current + 1} 級（防禦 +{30}%）。", "good");
            return (true, "");
        }

        public static void IncreaseFortification(GameState s, string id)
        {
            if (!GameData.Nodes.ContainsKey(id)) return;
            s.Fortification[id] = Math.Min(3, FortificationLevel(s, id) + 1);
        }

        public static Unit UnitById(GameState s, int id) => s.Units.FirstOrDefault(u => u.Id == id);
        public static List<Unit> UnitsAt(GameState s, string loc, UnitDomain? dom = null)
            => s.Units.Where(u => u.Loc == loc && (dom == null || GameData.UnitTypes[u.Type].Domain == dom.Value)).ToList();
        public static double LandStr(IEnumerable<Unit> list)
            => list.Where(u => GameData.UnitTypes[u.Type].Domain == UnitDomain.Land).Sum(u => u.Str / 100.0);
        public static int CountType(GameState s, string t) => s.Units.Count(u => u.Type == t);

        public static List<string> HeldMainland(GameState s)
            => GameData.MainlandIds.Where(id => s.Control[id] == Faction.ROC).ToList();
        public static List<string> UssrNodes(GameState s)
            => GameData.MainlandIds.Where(id => s.Control[id] == Faction.USSR).ToList();

        public static void SeizeByUssr(GameState s, string id, double force = 8.0)
        {
            foreach (var u in UnitsAt(s, id, UnitDomain.Land)) u.Loc = NearestRocNode(s, id);
            s.Control[id] = Faction.USSR;
            s.Garrison[id] = force;
            s.Unrest[id] = 35.0;
            var n = GameData.Nodes[id];
            Log(s, $"【北疆警訊】俄國/蘇聯出兵強占【{n.Name}（{n.City}）】，部署約 {force:F1} 個機械化師！", "bad");
            AddIntel(s, "蘇俄動態", "alert", $"北疆緊急密報：莫斯科突派遣機械化部隊越界佔領【{n.Name}】，邊境無線電加密流量激增。");
        }

        public static HistPoint Snapshot(GameState s) => new()
        {
            Year = s.Year,
            RocGdp = Math.Round(s.Gdp),
            PrcGdp = Math.Round(PrcGdp(s)),
            RocMil = Math.Round(RocMilIndex(s)),
            PrcMil = Math.Round(PrcMilIndex(s))
        };

        // ---------- 共軍與海空戰力計算 ----------
        public static double PrcLostIndustry(GameState s)
        {
            double lost = 0, tot = 0;
            foreach (var id in GameData.MainlandIds)
            {
                int ind = GameData.Nodes[id].Industry;
                tot += ind;
                if (s.Control[id] != Faction.PRC) lost += ind;
            }
            return lost / Math.Max(1.0, tot);
        }

        public static double PrcGdp(GameState s)
            => GameData.Interp(GameData.CurvePrcGdp, s.Year) * s.PrcGdpMod * (1.0 - 0.85 * PrcLostIndustry(s));

        public static double PrcStabFactor(GameState s) => 0.55 + 0.45 * (s.PrcStab / 100.0);

        public static double PrcTech(GameState s, SortedDictionary<int, double> curve)
            => 1.0 + (GameData.Interp(curve, s.Year) - 1.0) * s.PrcTechMod;

        public static double PrcSea(GameState s, string z)
        {
            double baseNavy = GameData.Interp(GameData.CurvePrcNavy, s.Year) * GameData.Zones[z].PrcWeight;
            return baseNavy * (1.0 + 0.3 * (PrcTech(s, GameData.CurvePrcNavyTech) - 1.0)) * PrcStabFactor(s) * (1.0 - 0.5 * PrcLostIndustry(s));
        }

        public static double PrcAir(GameState s, string z)
        {
            double share = GameData.Zones[z].PrcWeight * (1.0 - 0.4 * s.PrcSovTension);
            double baseAir = GameData.Interp(GameData.CurvePrcAir, s.Year) * share;
            return baseAir * (1.0 + 0.3 * (PrcTech(s, GameData.CurvePrcAirTech) - 1.0)) * PrcStabFactor(s) * (1.0 - 0.5 * PrcLostIndustry(s));
        }

        public static double RocSea(GameState s, string z)
        {
            double p = UnitsAt(s, z, UnitDomain.Sea).Sum(u => GameData.UnitTypes["fleet"].Power * (u.Str / 100.0));
            return p * (1.0 + 0.3 * (s.NavyTech - 1.0));
        }

        public static double RocAir(GameState s, string z)
        {
            double p = UnitsAt(s, z, UnitDomain.Air).Sum(u => GameData.UnitTypes["air"].Power * (u.Str / 100.0));
            return p * (1.0 + 0.3 * (s.AirTech - 1.0));
        }

        public static double SeaSup(GameState s, string z)
        {
            double a = RocSea(s, z), b = PrcSea(s, z);
            return a / (a + b + 0.01);
        }

        public static double AirSup(GameState s, string z)
        {
            double a = RocAir(s, z), b = PrcAir(s, z);
            return a / (a + b + 0.01);
        }

        public static double NodeAirSup(GameState s, string nid)
        {
            var zs = GameData.Nodes[nid].Zones;
            if (zs.Length > 0) return zs.Max(z => AirSup(s, z));
            return 0.35 * GameData.ZoneIds.Average(z => AirSup(s, z));
        }

        public static double UsDefense(GameState s)
        {
            if (s.Year < 1951) return 0.0;
            return s.UsProtect ? 20.0 + (s.UsRel + s.UsDefBonus) * 0.6 : Math.Max(0.0, s.UsRel + s.UsDefBonus - 55.0) * 0.5;
        }

        public static double RocMilIndex(GameState s)
        {
            double land = s.Units.Where(u => GameData.UnitTypes[u.Type].Domain == UnitDomain.Land).Sum(u => (u.Str / 100.0) * GameData.UnitTypes[u.Type].Def);
            double sea = s.Units.Where(u => u.Type == "fleet").Sum(u => u.Str / 10.0);
            double air = s.Units.Where(u => u.Type == "air").Sum(u => u.Str / 10.0);
            return land * (1.0 + 0.25 * (s.ArmyTech - 1.0)) + sea * (1.0 + 0.3 * (s.NavyTech - 1.0)) + air * (1.0 + 0.3 * (s.AirTech - 1.0));
        }

        public static double PrcMilIndex(GameState s)
        {
            double g = GameData.MainlandIds.Where(id => s.Control[id] == Faction.PRC).Sum(id => s.Garrison[id]);
            return g * (1.0 + 0.25 * (PrcTech(s, GameData.CurvePrcArmyTech) - 1.0)) * PrcStabFactor(s)
                 + GameData.Interp(GameData.CurvePrcNavy, s.Year) * (1.0 + 0.3 * (PrcTech(s, GameData.CurvePrcNavyTech) - 1.0))
                 + GameData.Interp(GameData.CurvePrcAir, s.Year) * (1.0 + 0.3 * (PrcTech(s, GameData.CurvePrcAirTech) - 1.0));
        }

        public static double NominalGarrison(GameState s, string id)
            => GameData.Nodes[id].BaseGarrison * GameData.Interp(GameData.CurvePrcGarrison, s.Year);

        public static double PrcDeployed(GameState s)
        {
            double n = 0;
            foreach (var id in GameData.NodeIds)
            {
                if (s.Control.TryGetValue(id, out var f) && f == Faction.PRC && s.Garrison.TryGetValue(id, out var g))
                    n += g;
            }
            return n;
        }

        public static int ControlSignature(GameState s)
        {
            unchecked
            {
                int h = 17;
                foreach (var id in GameData.NodeIds)
                {
                    int c = s.Control != null && s.Control.TryGetValue(id, out var f) ? (int)f + 1 : 0;
                    h = h * 31 + c;
                }
                return h;
            }
        }

        public static void TouchFrontClock(GameState s)
        {
            if (s == null) return;
            int sig = ControlSignature(s);
            if (s.GetFlag("frontClock") == 0)
            {
                s.Flags["frontClock"] = 1;
                s.FrontSig = sig;
                s.FrontStableSince = s.TotalMonths;
                return;
            }
            if (s.FrontSig != sig)
            {
                s.FrontSig = sig;
                s.FrontStableSince = s.TotalMonths;
            }
        }

        public static int FrontStableMonths(GameState s)
        {
            TouchFrontClock(s);
            return Math.Max(0, s.TotalMonths - s.FrontStableSince);
        }

        public static bool IsContested(GameState s, string id)
        {
            if (s == null || string.IsNullOrEmpty(id) || !GameData.Nodes.ContainsKey(id) || GameData.Zones.ContainsKey(id))
                return false;
            if (!s.Garrison.TryGetValue(id, out double g) || g <= 0.05) return false;
            return LandStr(UnitsAt(s, id, UnitDomain.Land)) > 0.05;
        }

        public static double ThreatIndex(GameState s)
        {
            double p = 0, d = 0;
            foreach (var z in new[] { "Z2", "Z3" })
            {
                p += PrcSea(s, z) + PrcAir(s, z);
                d += RocSea(s, z) + RocAir(s, z);
            }
            p += GameData.Interp(GameData.CurvePrcMissile, s.Year) * 0.3 * (1.0 - s.AirDefense);
            d += UsDefense(s) + LandStr(UnitsAt(s, "TW", UnitDomain.Land)) * 0.8;
            double amph = GameData.Interp(GameData.CurvePrcAmph, s.Year);
            return (p / Math.Max(1.0, d)) * (amph / 6.0);
        }

        // ---------- 情報迷霧 ----------
        private static readonly Dictionary<string, string[]> SightMap = new()
        {
            ["KM"] = new[] { "FJ" },
            ["MZ"] = new[] { "FJ" },
            ["DC"] = new[] { "ZJ" },
            ["ZS"] = new[] { "JS", "ZJ" },
            ["HI"] = new[] { "GD" }
        };

        public static int IntelLevel(GameState s)
        {
            int lv = 0;
            if (s.Techs.Contains("recon_coast") || s.Techs.Contains("p2v_patrol")) lv++;
            if (s.Techs.Contains("comint")) lv++;
            if (s.Techs.Contains("black_cat")) lv++;
            if (s.Techs.Contains("hkmacau_net") || s.Techs.Contains("se_asia_listen")) lv++;
            if (s.Techs.Contains("sat_elint") || s.Techs.Contains("formosat_recon")) lv++;
            return lv;
        }

        public static string IntelLevelName(GameState s) => IntelLevel(s) switch
        {
            0 => "Lv.0 基礎海岸哨戒網",
            1 => "Lv.1 外島雷達與機艦偵巡",
            2 => "Lv.2 特種電訊密碼破譯",
            3 => "Lv.3 高空戰略偵照與情蒐網",
            4 => "Lv.4 港澳東南地下總站網",
            _ => "Lv.5 全大陸天頻電偵透視"
        };

        public static void AddIntel(GameState s, string cat, string level, string text, string dedupKey = null, int cooldownMonths = 6)
        {
            if (s == null || string.IsNullOrWhiteSpace(text)) return;
            if (!string.IsNullOrEmpty(dedupKey))
            {
                string ck = "intel_" + dedupKey;
                if (s.Cooldowns.TryGetValue(ck, out int until) && s.TotalMonths < until) return;
                s.Cooldowns[ck] = s.TotalMonths + Math.Max(1, cooldownMonths);
            }
            s.IntelReports.Insert(0, new IntelReport { Date = DateStr(s), Cat = cat, Level = level, Text = text });
            if (s.IntelReports.Count > 40) s.IntelReports.RemoveRange(40, s.IntelReports.Count - 40);
        }

        public static void SampleIntelReports(GameState s, bool isOpening = false)
        {
            if (s == null || s.Over != null) return;
            int lv = IntelLevel(s);

            if (isOpening)
            {
                AddIntel(s, "密謀警示", "alert",
                    s.Year <= 1951
                        ? "沿海漁民與無線電監聽回報：華東、粵西徵集大量民船展開夜間登陸操演，解放軍似乎正在密謀跨海奪取外島據點……"
                        : "對岸東南軍區近期無線電通訊突然大量靜默，後勤車隊夜間集結頻繁，解放軍似乎正在密謀新一波軍事部署……",
                    "open_plot", 6);
                AddIntel(s, "蘇俄動態", "warn",
                    "遠東電偵站截獲西伯利亞與烏蘇里江方向頻密加密通訊，莫斯科正緊盯神州內戰空檔，北疆局勢暗流湧動。",
                    "open_ussr", 8);
                return;
            }

            // 1) 年代大事／共軍特殊密謀前兆
            if (s.Year == 1950 && (s.Control["ZS"] == Faction.ROC || s.Control["HI"] == Faction.ROC))
                AddIntel(s, "密謀警示", "alert", "華東與雷州半島大批機帆船集中掩蔽，三野虎視舟山、四野背靠華南正籌謀強渡僅二十餘公里的瓊州海峽並結合瓊崖縱隊攻取海南……", "era_1950", 14);
            if (!s.UsProtect && s.Year <= 1951 && LandStr(UnitsAt(s, "TW", UnitDomain.Land)) < 7.2)
                AddIntel(s, "本島危急", "alert", "【華東急電】台灣本島機動主力大幅抽調外島、美軍協防尚未就位，三野已在閩浙沿岸加速集結渡海船團，台澎防線極度吃緊！", "tw_empty_alert", 4);
            if (s.Control["HI"] == Faction.ROC && s.HasFlag("hiQiongya") && (s.Garrison["HI"] > 0.01 || UnitsAt(s, "HI", UnitDomain.Land).Count > 2))
                AddIntel(s, "海南困境", "warn", "【南海急電】逾千公里海運補保艱困且海口港受脅，粵桂殘部派系磨擦加上瓊崖縱隊裡應外合，增兵海南恐落入添油泥淖。", "hi_quagmire", 5);
            else if (s.Year == 1954 || s.Year == 1958)
                AddIntel(s, "密謀警示", "alert",
                    lv >= 2 ? "【電訊破譯】廈門、鷹廈鐵路夜間加開大批砲彈專列，共軍正秘密構築前方重砲陣地，似密謀封鎖金門！"
                            : "閩南前沿夜半砲車調動與火砲試射異常頻繁，解放軍似乎正在密謀對金門發動重大軍事行動……",
                    "era_artillery", 14);
            else if (s.Year == 1963 || s.Year == 1964)
                AddIntel(s, "密謀警示", "warn",
                    lv >= 1 ? "西北秘密波段截獲代號「五九六」加密通電與強輻射訊號，北京似乎正在新疆羅布泊秘密籌備重大試爆……"
                            : "西北邊陲深處近日封鎖異常森嚴、保密車隊頻繁進出，對岸似乎正暗中推進某項最高機密工程……",
                    "era_nuke", 16);
            else if (s.Year == 1965 || s.Year == 1966)
                AddIntel(s, "密謀警示", "warn", "平滬兩地報刊論戰白熱化、北京軍政高層派系矛盾激化，大陸內部似乎正在醞釀一場空前的政治風暴……", "era_cr", 18);
            else if ((s.Year == 1968 || s.Year == 1969 || s.PrcSovTension >= 0.25) && UssrNodes(s).Count == 0)
                AddIntel(s, "蘇俄動態", "alert", "珍寶島與中蘇北疆邊界雙方電台同時進入一級戰備，俄軍遠東裝甲集群似乎隨時可能趁亂越界施壓……", "era_ussr69", 14);
            else if (s.Year == 1971 || s.Year == 1972)
                AddIntel(s, "密謀警示", "warn", "多條駐外密線捕捉到華府與北京透過第三國密使往來頻繁，美共高層似乎正在暗中籌劃重大外交轉向……", "era_nixon", 18);
            else if (s.Year >= 1994 && s.Year <= 1996)
                AddIntel(s, "密謀警示", "alert", "東南山區捕捉到二砲飛彈機動營連夜進駐並架設測控雷達，解放軍似乎正密謀對台發動飛彈試射威懾……", "era_crisis96", 16);
            else if (s.Year == 2022)
                AddIntel(s, "蘇俄動態", "alert", "歐洲方向爆發大規模戰爭，遠東俄軍與對岸飛彈部隊同時提高戒備，電偵站研判北疆兵力與台海飛彈演訓都可能隨之變動……", "era_2022", 16);

            // 2) 即時海空威脅與共軍渡海奇襲警訊
            double th = ThreatIndex(s);
            if (th > 1.35)
            {
                string exposed = null;
                foreach (var iid in new[] { "KM", "MZ", "DC", "ZS", "HI", "PH" })
                {
                    if (s.Control[iid] != Faction.ROC) continue;
                    string z = GameData.Nodes[iid].Zones[0];
                    if (PrcSea(s, z) + PrcAir(s, z) > RocSea(s, z) + RocAir(s, z) * 1.05) { exposed = GameData.Nodes[iid].Name; break; }
                }
                string msg = lv >= 2 && exposed != null
                    ? $"【破譯預警】共軍沿岸登陸艇隊深夜秘密裝載，目標直指制空／制海薄弱的【{exposed}】，似密謀發動跨海奇襲！"
                    : "東南沿岸多個軍港連續夜間實施燈火管制並密集裝卸戰備物資，解放軍似乎正在密謀發起一場渡海突襲……";
                AddIntel(s, "密謀警示", "alert", msg, "live_amphib", 10);
            }

            // 3) 陸地戰線反撲或增兵動向
            if (s.LastReport?.PrcMoves.Count > 0)
            {
                var mv = s.LastReport.PrcMoves[0];
                string fromName = GameData.Nodes.TryGetValue(mv.From, out var nf) ? nf.Name : mv.From;
                string toName = GameData.Nodes.TryGetValue(mv.To, out var nt) ? nt.Name : mv.To;
                AddIntel(s, "部隊調動", "warn", $"情蒐網偵測到【{fromName}】共軍正秘密向【{toName}】方向集結機動，敵軍高層似意圖發起局部反撲。", "live_move", 5);
            }
            else
            {
                var held = HeldMainland(s);
                if (held.Count > 0)
                {
                    string hId = held[(int)(Rng(s) * held.Count)];
                    string adjPrc = GameData.Nodes[hId].Adj.FirstOrDefault(a => s.Control[a] == Faction.PRC);
                    if (adjPrc != null)
                        AddIntel(s, "部隊調動", "info", $"前沿偵察兵回報：{GameData.Nodes[adjPrc].Name} 共軍近期加修掩體並調動補保車隊，似在籌備防禦或伺機反擊 {GameData.Nodes[hId].Name}。", "front_watch", 7);
                }
            }

            // 4) 大陸內部穩定度／地下策反線索
            if (s.PrcStab < 52 || GameData.MainlandIds.Any(id => s.Control[id] == Faction.PRC && s.Unrest[id] >= 22))
            {
                string hotId = GameData.MainlandIds.Where(id => s.Control[id] == Faction.PRC).OrderByDescending(id => s.Unrest[id]).FirstOrDefault();
                string msg = lv >= 2 && hotId != null
                    ? $"地下情報站電告：【{GameData.Nodes[hotId].Name}】民間不滿情緒已達 {s.Unrest[hotId]:F0}%、中共穩定度僅 {s.PrcStab:F0}，基層部隊軍紀動搖！"
                    : "多個內陸車站與地方廣播異常繁雜，大陸內部似乎正湧現物資怨聲與潛在動盪，正是進行心戰與策反的時機。";
                AddIntel(s, "敵後線索", "good", msg, "live_unrest", 9);
            }

            // 5) 定期一般敵情密探動態
            if (Chance(s, 0.34))
            {
                string[] pool = lv >= 3
                    ? new[]
                    {
                        "【高空偵照比對】滬寧與華北鐵路樞紐昨夜貨運專列激增，共軍似乎正悄悄將重裝備向華南沿海轉運。",
                        "【電偵總站分析】福州與廣州軍區同步更改加密呼叫呼號並縮減明語，敵方總參似乎正在密謀調整戰區編制。",
                        "【港澳密線回報】大陸駐港澳人員近期頻密採購電子與艦用零件，對岸海空軍力疑似正進行秘密升級。"
                    }
                    : new[]
                    {
                        "監聽台捕捉到對岸各大軍區同步切換加密頻率並進入短暫靜默，解放軍似乎正在密謀某項秘密兵力調動……",
                        "東南外圍海域昨晚出現數批未掛燈共軍快艇編隊試探中線，隨後迅速回撤，對岸海防似正測試我方反應……",
                        "大陸沿海多個主要軍用機場昨日全天禁止民航接近、機群起降頻仍，共軍空軍疑似正籌備祕密轉場。"
                    };
                string chosen = pool[(int)(Rng(s) * pool.Length)];
                AddIntel(s, "電偵動態", "info", chosen, "routine_whisper", 7);
            }
        }

        public static double FogError(GameState s)
        {
            if (s.Techs.Contains("sat_elint") || s.Techs.Contains("formosat_recon")) return 0.0;
            if (s.Techs.Contains("black_cat")) return 0.06;
            if (s.Techs.Contains("comint")) return s.Techs.Contains("p2v_patrol") ? 0.09 : 0.12;
            if (s.Techs.Contains("recon_coast")) return s.Techs.Contains("p2v_patrol") ? 0.14 : 0.18;
            return s.Techs.Contains("p2v_patrol") ? 0.24 : 0.32;
        }

        public static void RollFog(GameState s)
        {
            double err = FogError(s);
            foreach (var id in GameData.NodeIds)
                s.Fog[id] = (Rng(s) * 2.0 - 1.0) * err;
        }

        public static bool HasIntel(GameState s, string id)
        {
            if (s.Control[id] == Faction.ROC || LandStr(UnitsAt(s, id, UnitDomain.Land)) > 0) return true;
            if ((s.Techs.Contains("sat_elint") || s.Techs.Contains("formosat_recon")) && GameData.Nodes[id].Kind == NodeKind.Mainland) return true;
            if (s.Techs.Contains("hkmacau_net") && (id == "GD" || id == "GX" || id == "JS" || id == "FJ" || id == "ZJ")) return true;
            if (s.Techs.Contains("se_asia_listen") && (id == "SC" || id == "GZ" || id == "YN" || id == "GX")) return true;
            if (s.Techs.Contains("recon_coast") && (id == "FJ" || id == "ZJ" || id == "GD" || id == "GX" || id == "JS"))
            {
                if (GameData.Nodes[id].Zones.Any(z => RocSea(s, z) >= 5.0 || RocAir(s, z) >= 5.0))
                    return true;
            }
            foreach (var kv in SightMap)
                if (s.Control[kv.Key] == Faction.ROC && kv.Value.Contains(id)) return true;
            foreach (var adj in GameData.Nodes[id].Adj)
                if (s.Control[adj] == Faction.ROC) return true;
            return false;
        }

        public static (double val, bool exact) EstGarrison(GameState s, string id)
        {
            double g = s.Garrison[id];
            if (HasIntel(s, id)) return (g, true);
            return (Math.Max(0.0, g * (1.0 + s.Fog[id])), false);
        }

        // ---------- 移動與調度 ----------
        public static double LiftCapacity(GameState s)
        {
            double c = s.LiftBonus;
            foreach (var u in UnitsAt(s, "TW", UnitDomain.Amph))
                c += GameData.UnitTypes["amph"].LiftCap * (u.Str / 100.0);
            foreach (var u in s.Units.Where(x => x.Type == "fleet"))
                c += 1.0 * (u.Str / 100.0);
            return c;
        }

        public static double LiftUsed(GameState s) => s.Orders.Where(o => o.Kind == "sea").Sum(o => o.Lift);

        public static string SharedZone(string a, string b)
        {
            var za = GameData.Nodes[a].Zones;
            var zb = GameData.Nodes[b].Zones;
            return za.FirstOrDefault(z => zb.Contains(z));
        }

        public static int PrepRequiredDays(GameState s)
        {
            int req = 8;
            if (s.Techs.Contains("landing_craft")) req -= 2;
            if (s.Techs.Contains("beach_ops")) req -= 2;
            return Math.Max(4, req);
        }

        public static List<string> FindFriendlyLandPath(GameState s, string from, string dest, bool allowHostileDest)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(dest) || from == dest) return null;
            if (!GameData.Nodes.ContainsKey(from) || !GameData.Nodes.ContainsKey(dest)) return null;
            bool destHostile = IsHostileTarget(s, dest);
            if (destHostile && !allowHostileDest) return null;

            var q = new Queue<string>();
            var prev = new Dictionary<string, string> { [from] = null };
            q.Enqueue(from);

            while (q.Count > 0)
            {
                string cur = q.Dequeue();
                foreach (var next in GameData.Nodes[cur].Adj)
                {
                    if (prev.ContainsKey(next)) continue;
                    if (next == dest)
                    {
                        var path = new List<string> { dest };
                        string trace = cur;
                        while (trace != null)
                        {
                            path.Add(trace);
                            trace = prev[trace];
                        }
                        path.Reverse();
                        return path;
                    }
                    if (!IsHostileTarget(s, next) && s.Control.TryGetValue(next, out var c) && c == Faction.ROC)
                    {
                        prev[next] = cur;
                        q.Enqueue(next);
                    }
                }
            }
            return null;
        }

        public static int ComputeTravelDays(GameState s, Unit u, List<string> path, string kind)
        {
            if (kind == "air") return 2;
            int logiReduce = s.Techs.Contains("auto_logi") ? 1 : 0;
            if (kind == "sea")
            {
                string f = path != null && path.Count > 0 ? path[0] : u.Loc;
                string t = path != null && path.Count > 0 ? path[^1] : u.Loc;
                if (GameData.Nodes.TryGetValue(f, out var nf) && GameData.Nodes.TryGetValue(t, out var nt))
                {
                    double dist = Math.Sqrt(Math.Pow(nf.Lon - nt.Lon, 2) + Math.Pow(nf.Lat - nt.Lat, 2));
                    int baseDays = dist <= 6.5 ? 4 : 5;
                    if (s.Techs.Contains("sea_shuttle")) baseDays = Math.Max(3, baseDays - 1);
                    return baseDays;
                }
                return 4;
            }
            int hops = Math.Max(1, (path?.Count ?? 2) - 1);
            if (hops == 1)
            {
                int d = u.Type == "arm" ? 3 : (u.Type == "mil" ? 5 : 4);
                return Math.Max(2, d - logiReduce);
            }
            return Math.Clamp(2 + hops * 2 - logiReduce, 3, 8);
        }

        public static (string segFrom, string segTo, double segT) GetOrderPosition(MoveOrder o)
        {
            if (o == null) return ("TW", "TW", 0.0);
            var p = o.Path != null && o.Path.Count >= 2 ? o.Path : new List<string> { o.From, o.To };
            int segs = p.Count - 1;
            double totalP = o.IsPrep ? 0.06 : o.VisualProgress;
            double scaled = Clamp(totalP, 0.0, 0.9999) * segs;
            int idx = Math.Min(segs - 1, (int)Math.Floor(scaled));
            double localT = scaled - idx;
            return (p[idx], p[idx + 1], localT);
        }

        public static (bool ok, string kind, double lift, string zone, string reason, List<string> path, int totalDays, bool needsPrep, int prepDays) CheckMoveDetailed(GameState s, Unit u, string dest)
        {
            var T = GameData.UnitTypes[u.Type];
            if (T.Domain != UnitDomain.Land) return (false, "", 0, "", "海空單位請直接指派至海域", null, 0, false, 0);
            if (u.Moved) return (false, "", 0, "", "本回合已下達命令", null, 0, false, 0);
            if (u.Loc == dest) return (false, "", 0, "", "已在此區域", null, 0, false, 0);
            var from = GameData.Nodes[u.Loc];
            var to = GameData.Nodes[dest];

            if (from.Adj.Contains(dest))
            {
                var p = new List<string> { u.Loc, dest };
                return (true, "land", 0, "", "", p, ComputeTravelDays(s, u, p, "land"), false, 0);
            }
            if (u.Type == "mil") return (false, "", 0, "", "民兵只能在陸上相鄰區域行動", null, 0, false, 0);

            // 大陸或我方控制區跨省陸路最短安全路徑（免逐省手拉）
            var landPath = FindFriendlyLandPath(s, u.Loc, dest, allowHostileDest: true);
            if (landPath != null)
            {
                return (true, "land", 0, "", "", landPath, ComputeTravelDays(s, u, landPath, "land"), false, 0);
            }

            // 我方控制且未遭封鎖／交戰之港島間跨海調防（如台灣/澎湖/金門 ↔ 舟山/未陷落海南/已光復港口）：免佔進攻登陸運力
            bool portBlockaded = dest == "HI" && s.HasFlag("hiPortBlocked");
            if (!IsHostileTarget(s, dest) && !portBlockaded && s.Control[u.Loc] == Faction.ROC && from.Zones.Length > 0 && to.Zones.Length > 0)
            {
                string friendlyZone = SharedZone(u.Loc, dest) ?? to.Zones[0];
                var p = new List<string> { u.Loc, dest };
                return (true, "sea", 0, friendlyZone, "", p, ComputeTravelDays(s, u, p, "sea"), false, 0);
            }

            if (T.Airborne && (u.Loc == "TW" || u.Loc == "PH" || u.Loc == "KM"))
            {
                if (to.Kind == NodeKind.Mainland && to.Zones.Length > 0)
                {
                    var p = new List<string> { u.Loc, dest };
                    return (true, "air", 0, to.Zones[0], "", p, ComputeTravelDays(s, u, p, "air"), false, 0);
                }
            }
            string z = SharedZone(u.Loc, dest);
            if (z == null) return (false, "", 0, "", "進攻或砲火逆登陸須具備相鄰陸路或共有海域直航", null, 0, false, 0);
            // 海南港口遭奪／封鎖時，逾千公里跨海逆登陸卸載重裝備需消耗 1.5× 登陸運力
            double need = portBlockaded ? Math.Round(T.Lift * 1.5, 2) : T.Lift;
            double left = LiftCapacity(s) - LiftUsed(s);
            if (left + 1e-9 < need)
            {
                string why = portBlockaded
                    ? $"海口港遭封鎖、砲火下跨海逆登陸運力不足（需 {need:F1}，剩餘 {left:F1} 師）"
                    : $"登陸兩棲運力不足（需 {need:F1}，剩餘 {left:F1} 師）";
                return (false, "", 0, "", why, null, 0, false, 0);
            }
            var seaPath = new List<string> { u.Loc, dest };
            return (true, "sea", need, z, "", seaPath, ComputeTravelDays(s, u, seaPath, "sea"), true, PrepRequiredDays(s));
        }

        public static (bool ok, string kind, double lift, string zone, string reason) CheckMove(GameState s, Unit u, string dest)
        {
            var d = CheckMoveDetailed(s, u, dest);
            return (d.ok, d.kind, d.lift, d.zone, d.reason);
        }

        public static bool IsHostileTarget(GameState s, string dest)
            => s.Control[dest] != Faction.ROC || s.Garrison[dest] > 0.01;

        public static bool NeedsContestedLanding(GameState s, string dest)
            => IsHostileTarget(s, dest) || (dest == "HI" && s.HasFlag("hiPortBlocked") && s.Control.GetValueOrDefault("GD") == Faction.PRC);

        public static bool WouldBreakTreaty(GameState s, string dest)
            => s.Treaty && s.TreatyRestrict && s.UsConsent <= 0 && GameData.Nodes[dest].Kind == NodeKind.Mainland
               && s.Control[dest] != Faction.ROC && !s.HasFlag("treatyBroken");

        public static (bool ok, string reason) IssueOrder(GameState s, int uid, string dest)
        {
            var u = UnitById(s, uid);
            if (u == null) return (false, "無此單位");
            var c = CheckMoveDetailed(s, u, dest);
            if (!c.ok) return (false, c.reason);
            s.Orders.Add(new MoveOrder
            {
                UnitId = uid,
                From = u.Loc,
                To = dest,
                Kind = c.kind,
                Lift = c.lift,
                Zone = c.zone,
                Path = c.path ?? new List<string> { u.Loc, dest },
                TotalDays = Math.Max(1, c.totalDays),
                ElapsedDays = 0,
                IsPrep = c.needsPrep,
                PrepDays = 0,
                PrepRequired = c.prepDays,
                ManualOverride = true,
                FromAutoFront = false
            });
            u.Moved = true;
            return (true, "");
        }

        public static (int moved, string error) AttackNow(GameState s, IEnumerable<int> unitIds, string dest, bool manual = true)
        {
            if (!IsHostileTarget(s, dest)) return (0, "目標沒有敵軍守備，無法進攻。");

            var selectedIds = unitIds?.Distinct().ToHashSet() ?? new HashSet<int>();
            var issuedOrders = new List<MoveOrder>();
            string lastError = "";
            foreach (int uid in selectedIds)
            {
                var result = IssueOrder(s, uid, dest);
                if (result.ok)
                {
                    var ord = s.Orders.LastOrDefault(o => o.UnitId == uid);
                    if (ord != null)
                    {
                        ord.ManualOverride = manual;
                        issuedOrders.Add(ord);
                    }
                }
                else lastError = result.reason;
            }
            if (issuedOrders.Count == 0) return (0, lastError);

            foreach (string zone in issuedOrders.Select(o => o.Zone).Where(z => !string.IsNullOrEmpty(z)).Distinct())
                AutoDeployReserveEscorts(s, zone);

            // 若未進兩棲籌備（例如空降或陸路直攻），非陸路且條約仍在時立即判定違約；兩棲海上登陸待正式起航瞬間判定
            if (issuedOrders.Any(o => !o.IsPrep && o.Kind != "land" && WouldBreakTreaty(s, dest)))
            {
                s.Flags["treatyBroken"] = 1;
                s.UsRel -= 30;
                s.UsAid = 0;
                s.UsProtect = false;
                Log(s, "國府片面發動反攻，違反中美共同防禦條約換文！美方宣布停止援助並撤回協防承諾。", "bad");
            }

            string destName = GameData.Nodes[dest].Name;
            if (issuedOrders.Any(o => o.IsPrep))
            {
                var firstSea = issuedOrders.First(o => o.IsPrep);
                double seaPct = SeaSup(s, firstSea.Zone) * 100.0;
                string zName = GameData.Zones.TryGetValue(firstSea.Zone, out var zv) ? zv.Name : firstSea.Zone;
                string portTag = GameData.Nodes[dest].Port ? "（奪港優先）" : "";
                Log(s, $"⚓ 建立兩棲登陸計畫前往【{destName}】{portTag}（{issuedOrders.Count} 支）：籌備 {firstSea.PrepRequired} 天，{zName}制海 {seaPct:F0}%（需 ≥ 50% 起航）。",
                    seaPct >= 50.0 ? "good" : "info");
            }
            else
            {
                int eta = issuedOrders.Max(o => o.TotalDays);
                string via = issuedOrders[0].Kind == "air" ? "空降突擊" : "陸路推進";
                Log(s, $"▶ 國軍發起{via}【{destName}】（{issuedOrders.Count} 支，預計 {eta} 天抵達交戰）。", "good");
            }

            Normalize(s);
            return (issuedOrders.Count, lastError);
        }

        public static (int moved, string error) MoveNow(GameState s, IEnumerable<int> unitIds, string dest, bool manual = true, bool fromAutoFront = false)
        {
            if (IsHostileTarget(s, dest)) return (0, "該地區有敵軍守備，請發動攻擊。");

            var selectedIds = unitIds?.Distinct().ToHashSet() ?? new HashSet<int>();
            var issuedOrders = new List<MoveOrder>();
            string lastError = "";
            foreach (int uid in selectedIds)
            {
                var result = IssueOrder(s, uid, dest);
                if (result.ok)
                {
                    var ord = s.Orders.LastOrDefault(o => o.UnitId == uid);
                    if (ord != null)
                    {
                        if (!NeedsContestedLanding(s, dest)) ord.IsPrep = false;
                        ord.ManualOverride = manual;
                        ord.FromAutoFront = fromAutoFront;
                        issuedOrders.Add(ord);
                    }
                }
                else lastError = result.reason;
            }
            if (issuedOrders.Count == 0) return (0, lastError);

            foreach (string zone in issuedOrders.Select(o => o.Zone).Where(z => !string.IsNullOrEmpty(z)).Distinct())
                AutoDeployReserveEscorts(s, zone);

            if (manual)
            {
                int eta = issuedOrders.Max(o => o.TotalDays);
                Log(s, $"▶ 國軍部隊啟程調防【{GameData.Nodes[dest].Name}】（{issuedOrders.Count} 支，預計 {eta} 天抵達）。", "good");
            }
            Normalize(s);
            return (issuedOrders.Count, lastError);
        }

        public static void CancelOrder(GameState s, int uid)
        {
            s.Orders.RemoveAll(o => o.UnitId == uid);
            var u = UnitById(s, uid);
            if (u != null) u.Moved = false;
        }

        public static (bool ok, string reason) DeployNavalAir(GameState s, int uid, string zone)
        {
            var u = UnitById(s, uid);
            if (u == null) return (false, "無此單位");
            var dom = GameData.UnitTypes[u.Type].Domain;
            if (dom != UnitDomain.Sea && dom != UnitDomain.Air) return (false, "僅限艦隊或航空聯隊");
            if (u.Moved) return (false, "本回合已調動");
            if (u.Loc == zone) return (false, "已在此海域");
            u.Loc = zone;
            u.Moved = true;
            return (true, "");
        }

        public static Unit FindNavalAirCandidate(GameState s, string zoneId, string unitType, bool pullIn)
        {
            if (pullIn)
            {
                return s.Units.Where(u => u.Type == unitType && !u.Moved && u.Loc != zoneId)
                              .OrderByDescending(u => u.Loc == "TW")
                              .ThenByDescending(u => u.Str)
                              .FirstOrDefault();
            }
            return s.Units.Where(u => u.Type == unitType && !u.Moved && u.Loc == zoneId)
                          .OrderBy(u => u.Str)
                          .FirstOrDefault();
        }

        public static (bool ok, string reason) AdjustZoneUnit(GameState s, string zoneId, string unitType, bool pullIn)
        {
            var cand = FindNavalAirCandidate(s, zoneId, unitType, pullIn);
            if (cand == null) return (false, pullIn ? "無待命預備單位可調入" : "本區無可撤回單位（已調動或已空）");
            return DeployNavalAir(s, cand.Id, pullIn ? zoneId : "TW");
        }

        public static (int fleetsMoved, int wingsMoved) AutoDeployReserveEscorts(GameState s, string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || !GameData.Zones.ContainsKey(zoneId)) return (0, 0);
            int fm = 0, wm = 0;
            if (UnitsAt(s, zoneId, UnitDomain.Sea).Count == 0 || SeaSup(s, zoneId) < 0.72)
            {
                var fl = s.Units.Where(u => u.Type == "fleet" && !u.Moved && u.Loc == "TW")
                                .OrderByDescending(u => u.Str).FirstOrDefault();
                if (fl != null && DeployNavalAir(s, fl.Id, zoneId).ok) fm++;
            }
            if (UnitsAt(s, zoneId, UnitDomain.Air).Count == 0 || AirSup(s, zoneId) < 0.72)
            {
                var aw = s.Units.Where(u => u.Type == "air" && !u.Moved && u.Loc == "TW")
                                .OrderByDescending(u => u.Str).FirstOrDefault();
                if (aw != null && DeployNavalAir(s, aw.Id, zoneId).ok) wm++;
            }
            if (fm + wm > 0)
                Log(s, $"⚓ 自動派駐預備海空護航【{GameData.Zones[zoneId].Name}】（艦隊 +{fm} / 航空聯隊 +{wm}）。", "good");
            return (fm, wm);
        }

        public static (int fleetsMoved, int wingsMoved) AutoBalanceNavalAir(GameState s)
        {
            var scores = new Dictionary<string, double>();
            foreach (var z in GameData.ZoneIds)
            {
                double w = GameData.Zones[z].PrcWeight * 10.0;
                if (s.Orders.Any(o => o.Zone == z)) w += (s.Orders.Any(o => o.Zone == z && o.IsPrep) ? 28.0 : 12.0);
                int rocNodesInZone = GameData.NodeIds.Count(nid =>
                    nid != "TW" && s.Control[nid] == Faction.ROC && GameData.Nodes[nid].Zones.Contains(z));
                w += rocNodesInZone * 5.0;
                if (z == "Z1" && rocNodesInZone == 0 && !s.Orders.Any(o => o.Zone == "Z1")) w = Math.Min(w, 1.5);
                scores[z] = Math.Max(1.0, w);
            }

            int MoveDomain(string unitType)
            {
                var all = s.Units.Where(u => u.Type == unitType).ToList();
                var unmoved = all.Where(u => !u.Moved).OrderByDescending(u => u.Loc == "TW").ThenByDescending(u => u.Str).ToList();
                if (unmoved.Count == 0) return 0;

                var targetCounts = GameData.ZoneIds.ToDictionary(z => z, _ => 0);
                var lockedCounts = GameData.ZoneIds.ToDictionary(z => z, z => all.Count(u => u.Moved && u.Loc == z));
                foreach (var z in GameData.ZoneIds) targetCounts[z] = lockedCounts[z];

                for (int i = 0; i < unmoved.Count; i++)
                {
                    string bestZ = GameData.ZoneIds
                        .OrderByDescending(z => scores[z] / (1.0 + targetCounts[z]))
                        .First();
                    targetCounts[bestZ]++;
                }

                var stay = new List<Unit>();
                var needReassign = new List<Unit>();
                var remainNeed = new Dictionary<string, int>(targetCounts);
                foreach (var z in GameData.ZoneIds) remainNeed[z] = Math.Max(0, targetCounts[z] - lockedCounts[z]);

                foreach (var u in unmoved)
                {
                    if (remainNeed.TryGetValue(u.Loc, out int cnt) && cnt > 0)
                    {
                        remainNeed[u.Loc] = cnt - 1;
                        stay.Add(u);
                    }
                    else
                    {
                        needReassign.Add(u);
                    }
                }

                int moved = 0;
                foreach (var z in GameData.ZoneIds)
                {
                    while (remainNeed[z] > 0 && needReassign.Count > 0)
                    {
                        var u = needReassign[0];
                        needReassign.RemoveAt(0);
                        if (DeployNavalAir(s, u.Id, z).ok) moved++;
                        remainNeed[z]--;
                    }
                }
                return moved;
            }

            int fm = MoveDomain("fleet");
            int wm = MoveDomain("air");
            if (fm + wm > 0)
                Log(s, $"⚓ 已按前線與出兵威脅度自動重新配置海空兵力（調動艦隊 {fm} 艘、航空聯隊 {wm} 隊）。", "good");
            return (fm, wm);
        }

        public static (int unitsMoved, int provincesReinforced) AutoBalanceLandForces(GameState s, bool quiet = false)
        {
            var fronts = GameData.MainlandIds
                .Where(id => s.Control[id] == Faction.ROC && s.Garrison[id] <= 0.01)
                .Select(id =>
                {
                    var node = GameData.Nodes[id];
                    double threat = 0;
                    int starBonus = s.PriorityTargets.Contains(id) ? 1 : 0;
                    foreach (var adjacent in node.Adj)
                    {
                        if (s.Control[adjacent] == Faction.PRC) threat += s.Garrison[adjacent];
                        else if (s.Control[adjacent] == Faction.USSR) threat += 5.0;
                        if (s.Control[adjacent] != Faction.ROC && s.PriorityTargets.Contains(adjacent)) starBonus = 1;
                    }
                    double defenseBonus = 1.0 + 0.3 * FortificationLevel(s, id);
                    int desired = threat <= 0 ? 0 : Math.Clamp((int)Math.Ceiling(threat / (8.0 * defenseBonus)) + (node.Major ? 1 : 0) + starBonus, 1, 4);
                    return (id, threat, desired, priority: threat + (node.Capital ? 12 : node.Major ? 5 : 0) + starBonus * 10);
                })
                .Where(x => x.desired > 0)
                .OrderByDescending(x => x.priority)
                .ToList();

            var busyIds = s.Orders.Select(o => o.UnitId).ToHashSet();
            int moved = 0, reinforced = 0;
            foreach (var front in fronts)
            {
                int localCount = UnitsAt(s, front.id, UnitDomain.Land).Count(u => !busyIds.Contains(u.Id));
                int incomingCount = s.Orders.Count(o => o.To == front.id && !o.IsPrep);
                int effectiveCount = localCount + incomingCount;
                int movedToFront = 0;
                while (effectiveCount < front.desired)
                {
                    var candidate = s.Units
                        .Where(u => GameData.UnitTypes[u.Type].Domain == UnitDomain.Land && !u.Moved && !busyIds.Contains(u.Id) && u.Loc != front.id)
                        .Where(u => s.Control[u.Loc] == Faction.ROC)
                        .Where(u =>
                        {
                            int atSource = UnitsAt(s, u.Loc, UnitDomain.Land).Count(x => !busyIds.Contains(x.Id));
                            var sourceNode = GameData.Nodes[u.Loc];
                            bool sourceFront = sourceNode.Kind == NodeKind.Mainland && sourceNode.Adj.Any(a => s.Control[a] != Faction.ROC);
                            if (sourceFront) return false; // 不從接敵前線省分抽兵去別的前線
                            int reserve = u.Loc == "TW" ? 4 : sourceNode.Kind == NodeKind.Island || sourceNode.Capital || sourceNode.Major ? 1 : 0;
                            if (atSource <= reserve) return false;
                            return CheckMove(s, u, front.id).ok;
                        })
                        .OrderBy(u => u.Loc == "TW")
                        .ThenBy(u => UnitsAt(s, u.Loc, UnitDomain.Land).Count)
                        .ThenByDescending(u => u.Str)
                        .FirstOrDefault();

                    if (candidate == null) break;
                    var result = MoveNow(s, new[] { candidate.Id }, front.id, manual: false, fromAutoFront: true);
                    if (result.moved == 0) break;
                    busyIds.Add(candidate.Id);
                    moved += result.moved;
                    movedToFront += result.moved;
                    effectiveCount += result.moved;
                }
                if (movedToFront > 0) reinforced++;
            }

            if (moved > 0)
                Log(s, $"⛨ 前線自動補防：後方共 {moved} 支部隊已啟程行軍增援 {reinforced} 個邊界戰區（台灣本島保留 ≥ 4 師）。", "good");
            else if (!quiet)
                Log(s, "前線各省駐軍已滿足目前需求，或後方暫無可安全調動之預備隊。", "info");
            return (moved, reinforced);
        }

        public static (string label, double ratio, string tier) EstimateMoveOdds(GameState s, List<Unit> attackers, string dest)
        {
            if (attackers == null || attackers.Count == 0) return ("", 0, "none");
            if (!IsHostileTarget(s, dest)) return ("調防", 2.0, "friendly");
            var (estG, _) = EstGarrison(s, dest);
            if (estG <= 0.05) return ("可佔領", 3.0, "high");

            var landed = new HashSet<int>();
            foreach (var u in attackers)
            {
                var chk = CheckMove(s, u, dest);
                if (chk.ok && chk.kind == "sea") landed.Add(u.Id);
            }
            var merged = UnitsAt(s, dest, UnitDomain.Land).Concat(attackers).ToList();
            double air = NodeAirSup(s, dest);
            double rp = RocPowerAt(s, dest, merged, true, landed) * (1.0 + 0.6 * air);
            double pp = EnemyPowerAt(s, dest, estG, true) * (1.0 + 0.6 * (1.0 - air));
            double ratio = rp / Math.Max(0.05, pp);
            if (ratio >= 3.0) return ("優勢", ratio, "high");
            if (ratio >= 1.0) return ("均勢", ratio, "even");
            return ("險戰", ratio, "risk");
        }

        // ---------- 建軍與研發 ----------
        public static double UnitCost(GameState s, string t) => GameData.UnitTypes[t].Cost * PriceIndex(s);

        public static bool IsBuildBase(GameState s, string loc)
        {
            if (string.IsNullOrEmpty(loc) || !GameData.Nodes.TryGetValue(loc, out var n)) return false;
            if (!s.Control.TryGetValue(loc, out var ctrl) || ctrl != Faction.ROC) return false;
            if (loc == "TW") return true;
            return n.Kind == NodeKind.Mainland && n.Port && (!s.Garrison.TryGetValue(loc, out double gar) || gar <= 0.05);
        }

        public static List<string> BuildBases(GameState s)
        {
            var list = new List<string>();
            if (IsBuildBase(s, "TW")) list.Add("TW");
            foreach (var id in GameData.MainlandIds)
                if (IsBuildBase(s, id)) list.Add(id);
            return list;
        }

        public static (bool ok, string reason) CanBuild(GameState s, string t, string loc = "TW")
        {
            var T = GameData.UnitTypes[t];
            if (T.NoBuild) return (false, "停用自行整編");
            if (T.Domain == UnitDomain.Land)
            {
                if (!IsBuildBase(s, loc)) return (false, "須選我方控制港口");
            }
            else if (!s.Control.TryGetValue("TW", out var tf) || tf != Faction.ROC)
            {
                return (false, "須控制台灣本島");
            }
            if (!string.IsNullOrEmpty(T.ReqTech) && !s.Techs.Contains(T.ReqTech))
                return (false, "需科技：" + GameData.Techs[T.ReqTech].Name);
            if (s.Treasury < UnitCost(s, t)) return (false, "國庫不足");
            if (s.Manpower < T.Man) return (false, "兵源不足");
            return (true, "");
        }

        public static (bool ok, string reason) BuildUnit(GameState s, string t, string loc = "TW")
        {
            var T = GameData.UnitTypes[t];
            string spawnLoc = T.Domain == UnitDomain.Land ? loc : "TW";
            var c = CanBuild(s, t, spawnLoc);
            if (!c.ok) return c;
            s.Treasury -= UnitCost(s, t);
            s.Manpower -= T.Man;
            var u = AddUnit(s, t, spawnLoc, 65.0);
            u.Moved = true;
            Log(s, $"新編成：{T.Name}（於{GameData.Nodes[spawnLoc].Name}整訓，戰力 65%）");
            return (true, "");
        }

        public static bool TechAvailable(GameState s, TechDef t)
        {
            if (s.Techs.Contains(t.Id)) return false;
            if (s.Year < t.Year) return false;
            if (!string.IsNullOrEmpty(t.ReqFlag) && !s.HasFlag(t.ReqFlag)) return false;
            if (t.Req != null && t.Req.Any(r => !s.Techs.Contains(r))) return false;
            return true;
        }

        public static bool HasActiveResearch(GameState s)
            => s != null && ((s.ActiveTechs != null && s.ActiveTechs.Count > 0) || !string.IsNullOrEmpty(s.ResearchId));

        public static bool IsResearching(GameState s, string id)
            => s != null && !string.IsNullOrEmpty(id)
               && !s.Techs.Contains(id)
               && ((s.ActiveTechs != null && s.ActiveTechs.Contains(id)) || s.ResearchId == id);

        private static void SyncPrimaryResearch(GameState s)
        {
            s.ActiveTechs ??= new();
            s.ResearchProgMap ??= new();
            s.ActiveTechs.RemoveAll(id => string.IsNullOrEmpty(id) || s.Techs.Contains(id) || !GameData.Techs.ContainsKey(id));
            s.ResearchId = s.ActiveTechs.Count > 0 ? s.ActiveTechs[0] : null;
            s.ResearchProg = (!string.IsNullOrEmpty(s.ResearchId) && s.ResearchProgMap.TryGetValue(s.ResearchId, out double p)) ? Math.Max(0.0, p) : 0.0;
        }

        public static double SavedResearchProg(GameState s, string id)
        {
            if (s == null || string.IsNullOrEmpty(id)) return 0;
            if (s.ResearchId == id) return Math.Max(0.0, s.ResearchProg);
            if (s.ResearchProgMap != null && s.ResearchProgMap.TryGetValue(id, out double p)) return Math.Max(0.0, p);
            return 0;
        }

        public static bool SetResearch(GameState s, string id)
        {
            if (!GameData.Techs.TryGetValue(id, out var t) || !TechAvailable(s, t)) return false;
            s.ResearchProgMap ??= new();
            s.ActiveTechs ??= new();
            s.ResearchQueue?.Clear();
            if (!string.IsNullOrEmpty(s.ResearchId) && !s.Techs.Contains(s.ResearchId))
            {
                s.ResearchProgMap[s.ResearchId] = Math.Max(0.0, s.ResearchProg);
                if (!s.ActiveTechs.Contains(s.ResearchId))
                    s.ActiveTechs.Add(s.ResearchId);
            }
            s.ActiveTechs.RemoveAll(x => x == id);
            s.ActiveTechs.Insert(0, id);
            s.ResearchId = id;
            s.ResearchProg = s.ResearchProgMap.TryGetValue(id, out double saved) ? Math.Max(0.0, saved) : 0.0;
            return true;
        }

        public static double TechStartCost(GameState s, TechDef t)
            => t == null ? 0.0 : Math.Round((0.05 + t.Cost * 0.015) * PriceIndex(s), 2);

        public static (string action, int activeCount) ClickTechAction(GameState s, string id)
        {
            if (!GameData.Techs.TryGetValue(id, out var t) || !TechAvailable(s, t))
                return ("unavailable", s.ActiveTechs?.Count ?? 0);
            s.ResearchProgMap ??= new();
            s.ActiveTechs ??= new();
            s.ResearchQueue?.Clear();

            if (IsResearching(s, id))
                return ("already_active", s.ActiveTechs.Count);

            if (!s.HasFlag("techPaid_" + id))
            {
                double fee = TechStartCost(s, t);
                if (s.Treasury + 1e-9 < fee)
                    return ("no_funds", s.ActiveTechs.Count);
                s.Treasury = Math.Max(0.0, s.Treasury - fee);
                s.Flags["techPaid_" + id] = 1;
                Log(s, $"撥付科研立項經費：${fee:F2}億（{t.Name}）");
            }

            if (!string.IsNullOrEmpty(s.ResearchId) && !s.Techs.Contains(s.ResearchId))
            {
                s.ResearchProgMap[s.ResearchId] = Math.Max(0.0, s.ResearchProg);
                if (!s.ActiveTechs.Contains(s.ResearchId))
                    s.ActiveTechs.Insert(0, s.ResearchId);
            }
            if (!s.ActiveTechs.Contains(id))
                s.ActiveTechs.Add(id);
            if (string.IsNullOrEmpty(s.ResearchId))
            {
                s.ResearchId = id;
                s.ResearchProg = s.ResearchProgMap.TryGetValue(id, out double saved) ? Math.Max(0.0, saved) : 0.0;
            }
            return ("started", s.ActiveTechs.Count);
        }

        public static string TechReqText(GameState s, TechDef t)
        {
            if (s.Techs.Contains(t.Id)) return "✔ 已完成";
            var missing = new List<string>();
            if (s.Year < t.Year) missing.Add($"需至 {t.Year} 年");
            if (!string.IsNullOrEmpty(t.ReqFlag) && !s.HasFlag(t.ReqFlag))
                missing.Add(t.ReqFlag == "prcNuke" ? "需中共先核試爆" : $"需事件條件");
            if (t.Req != null)
            {
                foreach (var r in t.Req)
                {
                    if (!s.Techs.Contains(r) && GameData.Techs.TryGetValue(r, out var rt))
                        missing.Add($"欠:{rt.Name}");
                }
            }
            return missing.Count > 0 ? string.Join(" · ", missing) : "可升級";
        }

        public static void GrantTech(GameState s, string id)
        {
            if (s.Techs.Contains(id) || !GameData.Techs.TryGetValue(id, out var t)) return;
            s.Techs.Add(id);
            t.Apply?.Invoke(s);
            Log(s, $"★ 研發完成：【{t.Name}】", "good");
        }

        public static double RpPerTurnFor(GameState s, string targetId)
        {
            double baseRp = s.ResLevel * (96.0 + s.Gdp / 30.0) + s.RpBonus * 6.0;
            return baseRp * (s.ResearchRush && !string.IsNullOrEmpty(targetId) ? 1.50 : 1.0);
        }

        public static double RpPerTurn(GameState s) => RpPerTurnFor(s, HasActiveResearch(s) ? (s.ResearchId ?? s.ActiveTechs[0]) : null);

        public static int EstDaysRemaining(GameState s, string id)
        {
            if (!GameData.Techs.TryGetValue(id, out var t)) return 0;
            double rem = Math.Max(0.0, t.Cost - SavedResearchProg(s, id));
            double ratePerDay = RpPerTurnFor(s, id) / 365.0;
            if (ratePerDay <= 1e-6) return 9999;
            return Math.Max(1, (int)Math.Ceiling(rem / ratePerDay));
        }

        public static double BaseResCost(GameState s) => s.Gdp * 0.012 * s.ResLevel;
        public static double ResCost(GameState s) => BaseResCost(s) * (s.ResearchRush && HasActiveResearch(s) ? 1.40 : 1.0);

        public static bool StepResearch(GameState s, double dt, bool chargeDailySurcharge)
        {
            s.ActiveTechs ??= new();
            s.ResearchProgMap ??= new();
            s.ResearchQueue?.Clear();

            if (!string.IsNullOrEmpty(s.ResearchId) && !s.Techs.Contains(s.ResearchId) && GameData.Techs.ContainsKey(s.ResearchId))
            {
                if (!s.ActiveTechs.Contains(s.ResearchId))
                    s.ActiveTechs.Insert(0, s.ResearchId);
                s.ResearchProgMap[s.ResearchId] = Math.Max(0.0, s.ResearchProg);
            }

            if (s.ActiveTechs.Count == 0)
            {
                s.ResearchId = null;
                s.ResearchProg = 0;
                return false;
            }

            if (chargeDailySurcharge && s.ResearchRush && dt > 0)
            {
                double surcharge = (ResCost(s) - BaseResCost(s)) * dt;
                s.Treasury -= surcharge;
                if (s.Treasury < 0)
                {
                    s.Debt += -s.Treasury;
                    s.Treasury = 0;
                }
            }

            bool completedAny = false;
            foreach (string tid in s.ActiveTechs.ToList())
            {
                if (!GameData.Techs.TryGetValue(tid, out var t) || s.Techs.Contains(tid))
                {
                    s.ActiveTechs.RemoveAll(x => x == tid);
                    continue;
                }
                double prog = s.ResearchProgMap.TryGetValue(tid, out double saved) ? Math.Max(0.0, saved) : 0.0;
                if (dt > 0) prog += RpPerTurnFor(s, tid) * dt;
                s.ResearchProgMap[tid] = prog;
                if (prog >= t.Cost)
                {
                    GrantTech(s, t.Id);
                    s.ResearchProgMap.Remove(t.Id);
                    s.ActiveTechs.RemoveAll(x => x == t.Id);
                    s.LastTechCompleted = t.Name;
                    s.LastTechDate = DateStr(s);
                    completedAny = true;
                }
            }

            SyncPrimaryResearch(s);
            if (completedAny)
            {
                RollFog(s);
                Normalize(s);
            }
            return completedAny;
        }

        // ---------- 財政 ----------
        public static double Upkeep(GameState s)
            => s.Units.Sum(u => GameData.UnitTypes[u.Type].Upkeep * (0.4 + 0.6 * (u.Str / 100.0)))
               * PriceIndex(s) * (s.Techs.Contains("auto_logi") ? 0.88 : 1.0) * (s.Techs.Contains("high_speed_logi") ? 0.92 : 1.0);

        public static double Income(GameState s)
        {
            double rate = s.Tax == "low" ? 0.12 : (s.Tax == "high" ? 0.20 : 0.16);
            double tax = s.Gdp * rate * 0.65;
            double occ = GameData.MainlandIds.Where(id => s.Control[id] == Faction.ROC).Sum(id => GameData.Nodes[id].Industry);
            return tax + s.UsAid * PriceIndex(s) + occ * PriceIndex(s) * 0.18;
        }

        public static double Interest(GameState s) => s.Debt * 0.04;

        public const double WarBondGdpPct = 0.18; // 仿美 War Bonds：單期募集規模嚴格錨定為台灣 GDP 的 18%
        public const double WarBondLowWaterPct = 0.04; // 國庫低於 GDP 的 4% 即自動續發 War Bonds

        public static double BondAmount(GameState s)
            => s == null ? 2.52 : Math.Round(s.Gdp * WarBondGdpPct, 2);

        public static double AutoBondThreshold(GameState s)
            => s == null ? 0.56 : Math.Round(s.Gdp * WarBondLowWaterPct, 2);

        private static readonly string[] AutoSupportedIds = { "bonds", "repay", "jp_trade", "lobby_us", "farm_team", "infra", "propaganda", "stockpile", "psy_drop" };
        public static readonly string[] AutoFiscalPresetIds = { "bonds", "repay", "jp_trade", "infra", "lobby_us" };

        public static bool IsAutoActionSupported(string id) => Array.IndexOf(AutoSupportedIds, id) >= 0;

        public static bool IsAutoActionEnabled(GameState s, string id)
            => s?.AutoActions != null && s.AutoActions.Contains(id);

        public static void ToggleAutoAction(GameState s, string id)
        {
            if (s == null || !IsAutoActionSupported(id)) return;
            s.AutoActions ??= new();
            if (!s.AutoActions.Remove(id))
            {
                s.AutoActions.Add(id);
                StepAutoActions(s, id);
            }
        }

        public static void SetAutoFiscalPreset(GameState s, bool enable)
        {
            if (s == null) return;
            s.AutoActions ??= new();
            if (enable)
            {
                foreach (var id in AutoFiscalPresetIds) s.AutoActions.Add(id);
                StepAutoActions(s);
            }
            else
            {
                s.AutoActions.Clear();
            }
        }

        public static void StepAutoActions(GameState s, string onlyId = null)
        {
            if (s == null || s.Over != null || s.AutoActions == null || s.AutoActions.Count == 0) return;
            double px = PriceIndex(s);
            double lowWater = AutoBondThreshold(s);
            double spendReserve = Math.Max(0.35 * px, s.Gdp * 0.025);

            foreach (string id in AutoSupportedIds)
            {
                if (onlyId != null && id != onlyId) continue;
                if (!s.AutoActions.Contains(id) || s.ActionsUsed.Contains(id)) continue;
                var a = Actions.FirstOrDefault(x => x.Id == id);
                if (a == null || (a.Avail != null && !a.Avail(s))) continue;

                double cost = a.Cost(s);
                bool trigger = id switch
                {
                    "bonds" => s.Treasury < lowWater && s.Debt < s.Gdp * 1.15,
                    "repay" => s.Debt > 0.01 && s.Treasury >= Math.Max(1.6 * px, s.Gdp * 0.12) + Math.Min(s.Debt, BondAmount(s) * 0.25),
                    "jp_trade" => s.Treasury >= cost + spendReserve,
                    "lobby_us" => s.UsRel < 96.0 && s.Treasury >= cost + spendReserve,
                    "farm_team" => s.Intl < 96.0 && s.Treasury >= cost + spendReserve,
                    "infra" => s.Treasury >= cost + spendReserve * 1.4,
                    "propaganda" => s.Morale < 90.0 && s.Treasury >= cost + spendReserve,
                    "stockpile" => s.Supply < Math.Max(20.0, s.SupplyCap * 3.0) && s.Treasury >= cost + spendReserve,
                    "psy_drop" => s.Treasury >= cost + spendReserve,
                    _ => false
                };

                if (trigger) DoAction(s, id, null, isAuto: true);
            }
        }

        public static (double income, double upkeep, double research, double interest, double net) Budget(GameState s)
        {
            double inc = Income(s), up = Upkeep(s), res = ResCost(s), it = Interest(s);
            return (inc, up, res, it, inc - up - res - it);
        }

        // ---------- 國策行動 ----------
        public static readonly List<ActionDef> Actions = new()
        {
            new ActionDef {
                Id = "lobby_us", Cat = "外交", Name = "遊說美國",
                Desc = "派員赴華府遊說國會與國務院。美國關係 +5",
                Cost = s => 0.15 * PriceIndex(s),
                Execute = (s, _) => { s.UsRel += s.UsRel > 80 ? 3 : 5; Log(s, "駐美外交團完成國會遊說，對美關係提升。"); }
            },
            new ActionDef {
                Id = "ask_consent", Cat = "外交", Name = "請求美方同意反攻",
                Desc = "依共同防禦條約換文，反攻須經美方同意。成功率取決於美國關係與中共動盪度",
                Cost = _ => 0,
                Avail = s => s.Treaty && s.TreatyRestrict && !s.HasFlag("treatyBroken") && s.UsConsent <= 0,
                Execute = (s, _) => {
                    double p = Clamp((s.UsRel - 50) / 90.0 + (s.PrcStab < 42 ? 0.18 : 0) + (s.HasFlag("vietnamHelp") ? 0.12 : 0), 0.08, 0.85);
                    if (Chance(s, p)) { s.UsConsent = 3; Log(s, "★ 美方默許反攻行動（3 年內有效），第七艦隊將提供後勤支援！", "good"); }
                    else { s.UsRel -= 4; Log(s, $"美方拒絕反攻請求（成功率 {Math.Round(p * 100)}%）：華府擔心捲入全面大戰。", "bad"); }
                }
            },
            new ActionDef {
                Id = "arms_buy", Cat = "外交", Name = "對美軍購",
                Desc = "以七折價購入一個航空聯隊或艦隊（美國關係 ≥ 50；八一七公報後需 ≥ 68）",
                Cost = s => GameData.UnitTypes["air"].Cost * PriceIndex(s) * 0.7,
                Avail = s => s.UsRel >= (s.HasFlag("comm817") ? 68 : 50) && s.Year >= 1951,
                Execute = (s, _) => {
                    string t = Chance(s, 0.5) ? "air" : "fleet";
                    var u = AddUnit(s, t, "TW", 85); u.Moved = true;
                    Log(s, $"美援軍購到貨：{GameData.UnitTypes[t].Name} 已編入戰鬥序列。", "good");
                }
            },
            new ActionDef {
                Id = "farm_team", Cat = "外交", Name = "先鋒案農耕隊",
                Desc = "派農耕隊援助友邦，鞏固外交承認。國際地位 +4",
                Cost = s => 0.10 * PriceIndex(s),
                Avail = s => s.Year >= 1960,
                Execute = (s, _) => { s.Intl += 4; Log(s, "農耕隊於亞非拉友邦展開技術合作，國際地位提升。"); }
            },
            new ActionDef {
                Id = "jp_trade", Cat = "外交", Name = "對日經貿合作",
                Desc = "日本關係 +5，台灣 GDP +1%",
                Cost = s => 0.08 * PriceIndex(s),
                Execute = (s, _) => { s.JpRel += 5; s.Gdp *= 1.01; Log(s, "擴大對日經貿技術合作，帶動出口成長。"); }
            },
            new ActionDef {
                Id = "soviet", Cat = "外交", Name = "秘密接觸蘇聯/俄國",
                Desc = "中蘇交惡後可行。對俄關係 +12、共軍北方邊境牽制 +10%，但若過度引狼入室恐誘發俄軍趁機侵占北疆！",
                Cost = s => 0.06 * PriceIndex(s),
                Avail = s => s.HasFlag("sinoSovietSplit"),
                Execute = (s, _) => {
                    s.SovRel += 12; s.UsRel -= 3;
                    s.PrcSovTension = Clamp(s.PrcSovTension + 0.10, 0, 1.0);
                    Log(s, "我方情報人員與莫斯科秘密接觸，蘇軍在北方邊境增兵施壓。");
                }
            },
            new ActionDef {
                Id = "demand_ussr", Cat = "外交", Name = "對俄交涉索還北疆",
                Desc = "當俄國占領東北、內蒙或新疆時可行。聯合國際施壓要求俄軍撤出中國領土",
                Cost = s => 0.20 * PriceIndex(s),
                Avail = s => UssrNodes(s).Count > 0,
                Execute = (s, _) => {
                    var uNodes = UssrNodes(s);
                    double p = (s.Control["HE"] == Faction.ROC ? 0.38 : 0.12) + (s.UsRel >= 65 ? 0.20 : 0) + (s.Intl >= 60 ? 0.15 : 0);
                    if (Chance(s, p)) {
                        string target = uNodes[0];
                        s.Control[target] = Faction.ROC; s.Garrison[target] = 0;
                        AddUnit(s, "inf", target, 85);
                        s.Morale += 10; s.Intl += 6;
                        Log(s, $"★ 經強硬外交交涉與國際施壓，俄軍同意撤出【{GameData.Nodes[target].Name}】，由國軍接管！", "big");
                    } else {
                        s.Morale += 3;
                        foreach (var id in uNodes) s.Garrison[id] = Math.Max(2.0, s.Garrison[id] - 1.5);
                        Log(s, "莫斯科拒絕撤出北疆占領區，但在國際輿論壓力下縮減了駐軍（各俄占區守軍 -1.5 師）。", "bad");
                    }
                }
            },
            new ActionDef {
                Id = "propaganda", Cat = "內政", Name = "精神動員",
                Desc = "「保密防諜，反攻必勝」。民心 +5",
                Cost = s => 0.08 * PriceIndex(s),
                Execute = (s, _) => { s.Morale += 5; Log(s, "展開全國精神總動員，民心士氣提升。"); }
            },
            new ActionDef {
                Id = "conscript", Cat = "內政", Name = "擴大徵兵",
                Desc = "兵源 +4，民心 -3",
                Cost = _ => 0,
                Execute = (s, _) => { s.Manpower += 4; s.Morale -= 3; Log(s, "發布後備動員召集令，兵源補充 +4。"); }
            },
            new ActionDef {
                Id = "stockpile", Cat = "內政", Name = "囤積戰備物資",
                Desc = "後勤補給儲備 +14",
                Cost = s => 0.22 * PriceIndex(s),
                Execute = (s, _) => { s.Supply += 14; Log(s, "採購油料彈藥入庫，補給儲備 +14。"); }
            },
            new ActionDef {
                Id = "infra", Cat = "內政", Name = "基礎建設投資",
                Desc = "經濟效率 +2%、港口吞吐 +0.6（最多 8 次）",
                Cost = s => s.Gdp * 0.03,
                Avail = s => s.InfraCount < 8,
                Execute = (s, _) => { s.InfraCount++; s.EconMult += 0.02; s.SupplyCap += 0.6; Log(s, "完成港口與電力擴建工程，經濟效率與吞吐量提升。"); }
            },
            new ActionDef {
                Id = "bonds", Cat = "內政", Name = "發行國防戰爭公債（War Bonds）",
                Desc = "仿美軍二戰 War Bonds 模式：籌措戰費 GDP×18%，提振戰時士氣（民心 +1），債務等額增加",
                Cost = _ => 0,
                Execute = (s, _) => {
                    double v = BondAmount(s);
                    s.Treasury += v;
                    s.Debt += v;
                    s.Morale = Math.Min(100.0, s.Morale + 1.0);
                    Log(s, $"仿美發行國防戰爭公債（War Bonds）籌集戰費 ${v:F2} 億（佔 GDP 18%），民心 +1。");
                }
            },
            new ActionDef {
                Id = "repay", Cat = "內政", Name = "償還戰爭公債",
                Desc = "以國庫償還最多 GDP×18% 之戰爭公債債務，降低年息負擔",
                Cost = _ => 0,
                Avail = s => s.Debt > 0.01 && s.Treasury > 0.05,
                Execute = (s, _) => {
                    double v = Math.Min(s.Debt, Math.Min(BondAmount(s), s.Treasury));
                    s.Treasury -= v;
                    s.Debt -= v;
                    Log(s, $"償還國防戰爭公債 ${v:F2} 億，減輕財政利息負擔。");
                }
            },
            new ActionDef {
                Id = "psy_drop", Cat = "情報", Name = "心戰空飄",
                Desc = "對大陸各省空飄傳單與物資。全境民心浮動 +4（心戰大隊 ×1.5）",
                Cost = s => 0.06 * PriceIndex(s) * (s.Techs.Contains("radio_liberty") ? 0.5 : 1.0),
                Execute = (s, _) => {
                    double k = s.Techs.Contains("psywar") ? 6.0 : 4.0;
                    foreach (var id in GameData.MainlandIds)
                        if (s.Control[id] == Faction.PRC) s.Unrest[id] = Clamp(s.Unrest[id] + k, 0, 100);
                    if (s.Techs.Contains("psywar")) s.PrcStab = Clamp(s.PrcStab - 1.0, 0, 100);
                    Log(s, "對大陸各省實施大規模高空心戰空飄，對岸民心浮動上升。");
                    AddIntel(s, "敵後線索", "good", "心戰人員實施高空傳單與物資空飄後，對岸多處地方電台緊急加播管制警語，民心明顯浮動。");
                }
            },
            new ActionDef {
                Id = "commando", Cat = "情報", Name = "敵後突擊",
                Desc = "需先選取一個敵軍控制的大陸省分。守軍 -0.6～1.2 師、民心浮動 +6（破譯電偵提升成功率）",
                NeedTarget = true,
                Cost = s => 0.12 * PriceIndex(s),
                TargetOk = (s, id) => id != null && GameData.Nodes.ContainsKey(id) && s.Control[id] != Faction.ROC && GameData.Nodes[id].Kind == NodeKind.Mainland,
                Execute = (s, id) => {
                    double p = 0.72 + (s.Techs.Contains("comint") ? 0.12 : 0.0);
                    if (Chance(s, p)) {
                        double d = 0.6 + Rng(s) * 0.6;
                        s.Garrison[id] = Math.Max(0, s.Garrison[id] - d);
                        s.Unrest[id] = Clamp(s.Unrest[id] + 6 + (s.Techs.Contains("comint") ? 2 : 0), 0, 100);
                        Log(s, $"突擊隊在{GameData.Nodes[id].Name}得手，炸毀敵軍雷達與彈藥庫（-{d:F1} 師）。", "good");
                        AddIntel(s, "敵後線索", "good", $"敵後特戰小組成功破壞【{GameData.Nodes[id].Name}】通信與軍需庫房，當地共軍短時間內調度陷入混亂。");
                    } else {
                        s.Morale -= 2;
                        Log(s, $"突擊隊在{GameData.Nodes[id].Name}遭伏擊，行動失敗。", "bad");
                    }
                }
            },
            new ActionDef {
                Id = "incite", Cat = "情報", Name = "策反守軍",
                Desc = "需「大陸地下工作網」並選取共軍省分。成功時守軍起義並加入我方投誠部隊",
                NeedTarget = true,
                Cost = s => 0.18 * PriceIndex(s) * (s.Techs.Contains("radio_liberty") ? 0.5 : 1.0),
                Avail = s => s.Techs.Contains("underground"),
                TargetOk = (s, id) => id != null && GameData.Nodes.ContainsKey(id) && s.Control[id] == Faction.PRC && GameData.Nodes[id].Kind == NodeKind.Mainland,
                Execute = (s, id) => {
                    double p = 0.18 + (100 - s.PrcStab) / 220.0 + s.Unrest[id] / 260.0 + (s.Techs.Contains("united_subvert") ? 0.20 : 0.0);
                    if (Chance(s, p)) {
                        int n = 1 + (Chance(s, 0.4) ? 1 : 0);
                        s.Garrison[id] = Math.Max(0, s.Garrison[id] - n);
                        string dest = NearestRocNode(s, id);
                        for (int i = 0; i < n; i++) AddUnit(s, "dfc", dest, 80);
                        Log(s, $"★ {GameData.Nodes[id].Name}共軍 {n} 個師陣前起義投誠，已轉進至{GameData.Nodes[dest].Name}！", "good");
                    } else {
                        if (!s.Techs.Contains("united_subvert")) s.Unrest[id] = Clamp(s.Unrest[id] - 5, 0, 100);
                        Log(s, $"策反工作在{GameData.Nodes[id].Name}受挫，聯絡線暫時隱蔽待機。", "bad");
                    }
                }
            }
        };

        public static string NearestRocNode(GameState s, string id)
        {
            if (LandStr(UnitsAt(s, id, UnitDomain.Land)) > 0) return id;
            foreach (var adj in GameData.Nodes[id].Adj)
                if (s.Control[adj] == Faction.ROC) return adj;
            var map = new Dictionary<string, string> { ["FJ"] = "KM", ["ZJ"] = "DC", ["JS"] = "ZS", ["GD"] = "HI", ["GX"] = "HI" };
            if (map.TryGetValue(id, out string isl) && s.Control[isl] == Faction.ROC) return isl;
            var held = HeldMainland(s);
            if (held.Count > 0) return held[0];
            return "TW";
        }

        public static (bool ok, string reason) ActionState(GameState s, ActionDef a, string target)
        {
            if (s.ActionsUsed.Contains(a.Id)) return (false, "本回合已執行");
            if (a.Avail != null && !a.Avail(s)) return (false, "條件未達成");
            if (a.NeedTarget && (a.TargetOk == null || !a.TargetOk(s, target))) return (false, "請先在地圖選取敵方大陸省分");
            if (s.Treasury < a.Cost(s)) return (false, "國庫不足");
            return (true, "");
        }

        public static (bool ok, string reason) DoAction(GameState s, string id, string target, bool isAuto = false)
        {
            var a = Actions.FirstOrDefault(x => x.Id == id);
            if (a == null) return (false, "無此行動");
            var st = ActionState(s, a, target);
            if (!st.ok) return st;
            s.Treasury -= a.Cost(s);
            s.ActionsUsed.Add(id);
            int logsBefore = s.Logs.Count;
            a.Execute(s, target);
            if (isAuto && s.Logs.Count > logsBefore && !s.Logs[0].Msg.StartsWith("[自動]"))
                s.Logs[0].Msg = "[自動] " + s.Logs[0].Msg;
            Normalize(s);
            return (true, "");
        }

        public static void Normalize(GameState s)
        {
            s.ResearchProgMap ??= new();
            s.ActiveTechs ??= new();
            s.ResearchQueue ??= new();
            s.AutoActions ??= new();
            if (!string.IsNullOrEmpty(s.ResearchId) && !s.Techs.Contains(s.ResearchId) && GameData.Techs.ContainsKey(s.ResearchId))
            {
                if (!s.ActiveTechs.Contains(s.ResearchId))
                    s.ActiveTechs.Insert(0, s.ResearchId);
                s.ResearchProgMap[s.ResearchId] = Math.Max(0.0, Math.Max(s.ResearchProg, s.ResearchProgMap.TryGetValue(s.ResearchId, out double p) ? p : 0.0));
            }
            foreach (string qid in s.ResearchQueue)
            {
                if (!string.IsNullOrEmpty(qid) && !s.Techs.Contains(qid) && !s.ActiveTechs.Contains(qid) && GameData.Techs.TryGetValue(qid, out var qt) && TechAvailable(s, qt) && s.HasFlag("techPaid_" + qid))
                    s.ActiveTechs.Add(qid);
            }
            s.ResearchQueue.Clear();
            SyncPrimaryResearch(s);
            s.Control ??= new();
            s.Garrison ??= new();
            s.Fortification ??= new();
            s.Unrest ??= new();
            s.Fog ??= new();
            foreach (var id in GameData.NodeIds)
            {
                var n = GameData.Nodes[id];
                if (!s.Control.ContainsKey(id)) s.Control[id] = n.Kind == NodeKind.Mainland ? Faction.PRC : Faction.ROC;
                if (!s.Garrison.ContainsKey(id)) s.Garrison[id] = n.BaseGarrison;
                if (!s.Fortification.ContainsKey(id)) s.Fortification[id] = n.Fort;
                if (!s.Unrest.ContainsKey(id)) s.Unrest[id] = n.Kind == NodeKind.Mainland ? 10.0 : 0.0;
            }
            s.Morale = Clamp(s.Morale, 0, 100);
            s.UsRel = Clamp(s.UsRel, 0, 100);
            s.JpRel = Clamp(s.JpRel, 0, 100);
            s.Intl = Clamp(s.Intl, 0, 100);
            s.SovRel = Clamp(s.SovRel, 0, 100);
            s.PrcStab = Clamp(s.PrcStab, 0, 100);
            s.PrcSovTension = Clamp(s.PrcSovTension, 0, 1.0);
            s.Manpower = Clamp(s.Manpower, 0, 60);
            s.Supply = Clamp(s.Supply, 0, 400);
            foreach (var id in GameData.MainlandIds)
            {
                s.Unrest[id] = Clamp(s.Unrest[id], 0, 100);
                s.Garrison[id] = Math.Max(0, s.Garrison[id]);
            }
        }

        // ---------- 戰鬥結算 ----------
        private static double EnemyPowerAt(GameState s, string nid, double force, bool defending)
        {
            var n = GameData.Nodes[nid];
            if (s.Control[nid] == Faction.USSR)
            {
                double ussrTech = s.Year < 1975 ? 2.4 : 3.2;
                return force * (1.0 + 0.25 * (ussrTech - 1.0)) * (defending ? 1.15 : 1.0);
            }
            double fort = defending && n.Kind == NodeKind.Mainland && n.Zones.Length > 0 ? 1.15 : 1.0;
            if (n.Capital && defending) fort = 1.3;
            double unrest = s.Unrest.TryGetValue(nid, out double u) ? u : 0;
            return force * (1.0 + 0.25 * (PrcTech(s, GameData.CurvePrcArmyTech) - 1.0)) * PrcStabFactor(s) * (1.0 - unrest / 250.0) * fort;
        }

        private static double RocPowerAt(GameState s, string nid, List<Unit> units, bool attacking, HashSet<int> landedIds)
        {
            var n = GameData.Nodes[nid];
            double p = 0;
            double tech = 1.0 + 0.25 * (s.ArmyTech - 1.0);
            double moraleF = 0.8 + 0.4 * (s.Morale / 100.0);
            foreach (var u in units)
            {
                var T = GameData.UnitTypes[u.Type];
                double v = (u.Str / 100.0) * (attacking ? T.Atk : T.Def) * (u.Type == "mil" ? 1.0 : tech) * u.SupFactor * (1.0 + u.Exp * 0.05);
                if (landedIds.Contains(u.Id)) v *= (0.68 + s.LandingBonus + (T.Marine ? 0.22 : 0.0));
                p += v;
            }
            double fort = !attacking ? 1.0 + 0.3 * FortificationLevel(s, nid) : 1.0;
            // 海南粵桂殘部指揮失調與瓊崖縱隊裡應外合拖累戰力（需掃蕩或收復華南）
            double localDrag = (nid == "HI" && s.HasFlag("hiQiongya")) ? 0.80 : 1.0;
            return p * moraleF * (1.0 + s.BattleBonus) * fort * localDrag;
        }

        public static void Battle(GameState s, string nid, bool rocAttacking, HashSet<int> landedIds)
        {
            var units = UnitsAt(s, nid, UnitDomain.Land);
            double air = NodeAirSup(s, nid);
            bool wasUssr = s.Control[nid] == Faction.USSR;
            double rp = RocPowerAt(s, nid, units, rocAttacking, landedIds) * (1.0 + 0.6 * air);
            double pp = EnemyPowerAt(s, nid, s.Garrison[nid], rocAttacking) * (1.0 + 0.6 * (1.0 - air));
            if (rp <= 0 || pp <= 0) return;

            double r = rp / pp;
            double rr = Clamp(r, 0.25, 4.0);
            double rocLossPct = 16.0 * Math.Pow(1.0 / rr, 0.85);
            double enemyLossFrac = 0.28 * Math.Pow(rr, 0.85);
            if (r > 1.9) enemyLossFrac += 0.28;
            if (r < 0.45) rocLossPct += 12.0;

            double before = LandStr(units), g0 = s.Garrison[nid];
            foreach (var u in units)
            {
                u.Str -= rocLossPct * (0.8 + 0.4 * Rng(s));
                u.Exp = Math.Min(5.0, u.Exp + 0.5);
            }
            s.Garrison[nid] = Math.Max(0, g0 * (1.0 - enemyLossFrac));
            if (s.Garrison[nid] < 0.7) s.Garrison[nid] = 0;

            PurgeDead(s);
            double after = LandStr(UnitsAt(s, nid, UnitDomain.Land));
            double rl = before - after, pl = g0 - s.Garrison[nid];
            s.RocLoss += rl;
            s.EnemyLoss += pl;
            string enemyName = wasUssr ? "俄軍" : "共軍";
            var n = GameData.Nodes[nid];

            if (s.Garrison[nid] <= 0 && after > 0)
            {
                if (s.Control[nid] != Faction.ROC)
                {
                    s.Control[nid] = Faction.ROC;
                    s.BattlesWon++;
                    s.Morale += n.Major ? 8 : 5;
                    if (!wasUssr) s.PrcStab -= n.Major ? 8 : 4;
                    else s.Intl += 8;
                    string portMsg = n.Kind == NodeKind.Mainland && n.Port ? "（⚓ 已搶下海岸港口，開通補給走廊與就地徵募！）" : "";
                    Log(s, $"★ 國軍擊潰{enemyName}，光復【{n.Name}（{n.City}）】！{portMsg}", "big");
                    EventSystem.OnCapture(s, nid);
                }
                else
                {
                    Log(s, $"{n.Name}守住了{enemyName}反撲。", "good");
                }
            }
            else if (after <= 0)
            {
                if (s.Control[nid] == Faction.ROC)
                {
                    s.Control[nid] = Faction.PRC;
                    s.Morale -= n.Kind == NodeKind.Taiwan ? 100 : 5;
                    Log(s, $"✖ {n.Name}失守。", "bad");
                }
                else
                {
                    s.Morale -= 3;
                    Log(s, $"✖ 進攻{n.Name}的部隊全軍覆沒。", "bad");
                }
            }
            else
            {
                Log(s, $"{n.Name}激戰：國軍折損 {rl:F1} 師，{enemyName}折損 {pl:F1} 師（戰力比 {r:F2}）。");
            }
        }

        public static void PurgeDead(GameState s)
        {
            var deadIds = s.Units.Where(u => u.Str < 8.0).Select(u => u.Id).ToList();
            if (deadIds.Count == 0) return;
            s.Units.RemoveAll(u => u.Str < 8.0);
            foreach (int id in deadIds) CancelOrder(s, id);
        }

        public static bool StepOrders(GameState s)
        {
            if (s.Orders == null || s.Orders.Count == 0) return false;
            bool stateChanged = false;

            // 1. 推進兩棲登陸籌備並檢查沿線 ≥ 50% 制海權
            foreach (var grp in s.Orders.Where(o => o.IsPrep).GroupBy(o => $"{o.From}->{o.To}:{o.Zone}").ToList())
            {
                var sample = grp.First();
                if (!NeedsContestedLanding(s, sample.To))
                {
                    foreach (var o in grp)
                    {
                        o.IsPrep = false;
                        o.Lift = 0;
                        o.ElapsedDays = 0;
                    }
                    stateChanged = true;
                    continue;
                }

                bool reached = true;
                foreach (var o in grp)
                {
                    o.PrepDays = Math.Min(Math.Max(1, o.PrepRequired), o.PrepDays + 1);
                    if (o.PrepDays < o.PrepRequired) reached = false;
                }

                double sup = string.IsNullOrEmpty(sample.Zone) ? 1.0 : SeaSup(s, sample.Zone);
                if (reached && sup >= 0.50)
                {
                    if (WouldBreakTreaty(s, sample.To))
                    {
                        s.Flags["treatyBroken"] = 1;
                        s.UsRel -= 30;
                        s.UsAid = 0;
                        s.UsProtect = false;
                        Log(s, "國府片面發動跨海反攻，違反中美共同防禦條約換文！美方宣布停止援助並撤回協防承諾。", "bad");
                    }
                    foreach (var o in grp)
                    {
                        o.IsPrep = false;
                        o.ElapsedDays = 0;
                    }
                    string zName = GameData.Zones.TryGetValue(sample.Zone ?? "", out var zv) ? zv.Name : "海域";
                    Log(s, $"⚓ 【{GameData.Nodes[sample.From].Name}→{GameData.Nodes[sample.To].Name}】登陸籌備就緒（{zName}制海 {sup * 100:F0}% ≥ 50%），船團正式起航！", "good");
                    stateChanged = true;
                }
            }

            // 2. 推進已起航／正在行軍之部隊，逐段前進並於抵達日即時結算
            var activeMarches = s.Orders.Where(o => !o.IsPrep).ToList();
            if (activeMarches.Count == 0) return stateChanged;

            var arrivedByDest = new Dictionary<string, List<MoveOrder>>();
            foreach (var o in activeMarches)
            {
                o.ElapsedDays++;
                var u = UnitById(s, o.UnitId);
                if (u == null)
                {
                    s.Orders.Remove(o);
                    continue;
                }

                // 多節點安全陸路推進時，隨行進進度更新經過之中繼友軍省分位置
                if (o.Kind == "land" && o.Path != null && o.Path.Count > 2 && o.ElapsedDays < o.TotalDays)
                {
                    var pos = GetOrderPosition(o);
                    if (pos.segFrom != u.Loc && !IsHostileTarget(s, pos.segFrom) && s.Control[pos.segFrom] == Faction.ROC)
                        u.Loc = pos.segFrom;
                }

                if (o.ElapsedDays >= Math.Max(1, o.TotalDays))
                {
                    if (!arrivedByDest.TryGetValue(o.To, out var list))
                    {
                        list = new List<MoveOrder>();
                        arrivedByDest[o.To] = list;
                    }
                    list.Add(o);
                }
            }

            if (arrivedByDest.Count == 0) return stateChanged;
            stateChanged = true;

            var rep = s.LastReport ?? new TurnReport();
            var activeZones = new HashSet<string>();
            foreach (var kv in arrivedByDest)
            {
                string dest = kv.Key;
                var orders = kv.Value;
                foreach (var o in orders) s.Orders.Remove(o);

                bool hostile = IsHostileTarget(s, dest);
                bool reverseBeachhead = dest == "HI" && (hostile || s.HasFlag("hiPortBlocked"));
                var landedIds = new HashSet<int>();
                var arrivedUnits = new List<Unit>();

                foreach (var order in orders)
                {
                    var unit = UnitById(s, order.UnitId);
                    if (unit == null) continue;
                    if (order.Kind == "sea")
                    {
                        double sea = SeaSup(s, order.Zone), air = AirSup(s, order.Zone);
                        double enemyPresence = PrcSea(s, order.Zone) + PrcAir(s, order.Zone);
                        if (enemyPresence > 0.5 || reverseBeachhead)
                        {
                            double baseLoss = hostile ? 28.0 : (reverseBeachhead ? 20.0 : 4.0);
                            double loss = baseLoss * (1.0 - 0.55 * sea) * (1.0 - 0.45 * air);
                            unit.Str -= loss * (0.7 + 0.6 * Rng(s));
                        }
                        if (hostile || reverseBeachhead) { activeZones.Add(order.Zone); landedIds.Add(unit.Id); }
                    }
                    else if (order.Kind == "air")
                    {
                        double air = AirSup(s, order.Zone);
                        unit.Str -= 30.0 * (1.0 - air) * (0.7 + 0.6 * Rng(s));
                        activeZones.Add(order.Zone);
                    }
                    unit.Loc = dest;
                    unit.Moved = hostile || reverseBeachhead; // 純友軍調防抵達後立即解除待機鎖定；逆登陸需整補
                    arrivedUnits.Add(unit);
                }

                PurgeDead(s);

                if (reverseBeachhead && !hostile && arrivedUnits.Any(x => s.Units.Any(u => u.Id == x.Id)))
                {
                    Log(s, "✖ 【海南島】海口港遭砲火封鎖，增援船團無港可卸重裝，在砲火下強行搶灘逆登陸蒙受戰損！", "bad");
                }

                if (hostile)
                {
                    if (s.Garrison[dest] > 0.01)
                    {
                        if (!rep.BattleNodes.Contains(dest)) rep.BattleNodes.Add(dest);
                        Battle(s, dest, rocAttacking: true, landedIds);
                    }
                    else if (UnitsAt(s, dest, UnitDomain.Land).Count > 0 && s.Control[dest] != Faction.ROC)
                    {
                        s.Control[dest] = Faction.ROC;
                        string portMsg = GameData.Nodes[dest].Kind == NodeKind.Mainland && GameData.Nodes[dest].Port
                            ? "（⚓ 已奪取海岸港口，開通補給與就地徵募）"
                            : "";
                        Log(s, $"國軍兵不血刃進駐並收復【{GameData.Nodes[dest].Name}】！{portMsg}", "good");
                        EventSystem.OnCapture(s, dest);
                    }
                    foreach (string zone in GameData.Nodes[dest].Zones) activeZones.Add(zone);
                }
                else
                {
                    int aliveArrived = arrivedUnits.Count(x => s.Units.Any(u => u.Id == x.Id));
                    if (aliveArrived > 0 && orders.Any(o => o.ManualOverride))
                        Log(s, $"✔ 國軍增援已行軍抵達【{GameData.Nodes[dest].Name}】（{aliveArrived} 支）。", "good");
                }
            }

            foreach (var zone in activeZones)
            {
                double enemySea = PrcSea(s, zone), enemyAir = PrcAir(s, zone);
                double rocSea = RocSea(s, zone), rocAir = RocAir(s, zone);
                foreach (var unit in UnitsAt(s, zone, UnitDomain.Sea))
                    unit.Str -= 8.0 * enemySea / (enemySea + rocSea + 0.01) * (0.6 + 0.8 * Rng(s));
                foreach (var unit in UnitsAt(s, zone, UnitDomain.Air))
                    unit.Str -= 8.0 * enemyAir / (enemyAir + rocAir + 0.01) * (0.6 + 0.8 * Rng(s));
            }
            PurgeDead(s);
            s.LastReport = rep;
            RollFog(s);
            CheckEnd(s, tickAnnualCounters: false);
            Normalize(s);
            return stateChanged;
        }

        // ---------- 即時日流推進 / 月度結算 / 年度里程碑 ----------
        public static bool AdvanceDay(GameState s)
        {
            if (s.Over != null || s.PendingEvents.Count > 0) return false;

            DateTime cur = new DateTime(s.Year, s.Month, s.Day).AddDays(1);
            if (cur.Year > GameData.EndYear)
            {
                s.Year = GameData.EndYear + 1;
                s.Month = 1;
                s.Day = 1;
                CheckEnd(s, true);
                return false;
            }

            bool monthChanged = cur.Month != s.Month || cur.Year != s.Year;
            bool yearChanged = cur.Year != s.Year;
            s.Year = cur.Year;
            s.Month = cur.Month;
            s.Day = cur.Day;

            StepOrders(s);
            if (s.Over != null || s.PendingEvents.Count > 0)
            {
                CheckEnd(s, yearChanged);
                return true;
            }

            if (monthChanged)
            {
                MonthlyTick(s, stepResearch: false, resolveOrders: false);
            }
            double dayDt = 1.0 / (DateTime.IsLeapYear(s.Year) ? 366.0 : 365.0);
            StepResearch(s, dayDt, chargeDailySurcharge: true);

            if (s.AutoFrontFill && s.Day % 3 == 1 && s.PendingEvents.Count == 0 && s.Over == null)
            {
                AutoBalanceLandForces(s, quiet: true);
            }

            if (s.Day == 1 || s.Day == 10 || s.Day == 20)
            {
                SampleIntelReports(s);
            }

            if (yearChanged)
            {
                YearlyTick(s);
            }

            EventSystem.CheckHistoricalForDate(s);
            int dynDay = 5 + ((s.Year * 17 + s.Month * 11) % 22);
            if (s.Day == dynDay && s.PendingEvents.Count == 0 && s.Over == null)
            {
                EventSystem.CheckDynamic(s);
            }
            CheckEnd(s, yearChanged);
            return true;
        }

        public static void EndTurn(GameState s)
        {
            if (s.Over != null || s.PendingEvents.Count > 0) return;
            for (int m = 0; m < 12; m++)
            {
                MonthlyTick(s, stepResearch: true);
                if (s.Over != null || s.PendingEvents.Count > 0) break;
            }
            s.Year++;
            YearlyTick(s);
            EventSystem.CheckEvents(s);
            CheckEnd(s, true);
        }

        private static void YearlyTick(GameState s)
        {
            s.Turn++;
            s.ActionsUsed.Clear();
            StepAutoActions(s);
            if (s.UsConsent > 0) s.UsConsent--;
            s.History.Add(Snapshot(s));
        }

        public static void MonthlyTick(GameState s, bool stepResearch = false, bool resolveOrders = true)
        {
            if (s.Over != null) return;
            const double dt = 1.0 / 12.0;
            var rep = new TurnReport();
            var landedIds = new HashSet<int>();
            var activeZones = new HashSet<string>();

            if (resolveOrders)
            {
                bool breach = s.Orders.Any(o => WouldBreakTreaty(s, o.To) && o.Kind != "land");
                if (breach)
                {
                    s.Flags["treatyBroken"] = 1;
                    s.UsRel -= 30; s.UsAid = 0; s.UsProtect = false;
                    Log(s, "國府片面發動反攻，違反中美共同防禦條約換文！美方宣布停止援助並撤回協防承諾。", "bad");
                }

                foreach (var o in s.Orders.ToList())
                {
                    var u = UnitById(s, o.UnitId);
                    if (u == null) continue;
                    bool hostile = IsHostileTarget(s, o.To);
                    if (o.Kind == "sea")
                    {
                        double sup = SeaSup(s, o.Zone), air = AirSup(s, o.Zone);
                        double prcPres = PrcSea(s, o.Zone) + PrcAir(s, o.Zone);
                        double baseLoss = hostile ? 28.0 : 4.0;
                        double loss = prcPres > 0.5 ? baseLoss * (1.0 - sup) * (1.0 - 0.5 * air) : 0.0;
                        u.Str -= loss * (0.7 + 0.6 * Rng(s));
                        if (hostile) { activeZones.Add(o.Zone); landedIds.Add(u.Id); }
                    }
                    else if (o.Kind == "air")
                    {
                        double air = AirSup(s, o.Zone);
                        u.Str -= 30.0 * (1.0 - air) * (0.7 + 0.6 * Rng(s));
                        activeZones.Add(o.Zone);
                    }
                    u.Loc = o.To;
                }
                PurgeDead(s);
                s.Orders.Clear();
            }

            PrcAi(s, rep, activeZones, dt);
            ResolveSupply(s, dt);

            foreach (var nid in GameData.NodeIds)
            {
                var units = UnitsAt(s, nid, UnitDomain.Land);
                if (units.Count == 0 || s.Garrison[nid] <= 0.01)
                {
                    if (units.Count > 0 && s.Control[nid] != Faction.ROC && s.Garrison[nid] <= 0.01)
                    {
                        s.Control[nid] = Faction.ROC;
                        Log(s, $"國軍兵不血刃收復{GameData.Nodes[nid].Name}。", "good");
                        EventSystem.OnCapture(s, nid);
                    }
                    continue;
                }
                bool rocAtk = s.Control[nid] != Faction.ROC;
                foreach (var z in GameData.Nodes[nid].Zones) activeZones.Add(z);
                rep.BattleNodes.Add(nid);
                Battle(s, nid, rocAtk, landedIds);
            }

            foreach (var nid in GameData.NodeIds)
            {
                if (s.Control[nid] == Faction.ROC && s.Garrison[nid] > 0.01 && UnitsAt(s, nid, UnitDomain.Land).Count == 0)
                {
                    s.Control[nid] = Faction.PRC;
                    rep.BattleNodes.Add(nid);
                    Log(s, $"✖ {GameData.Nodes[nid].Name}無兵防守，遭敵軍佔領。", "bad");
                }
            }

            foreach (var z in activeZones)
            {
                double ps = PrcSea(s, z), pa = PrcAir(s, z), rs = RocSea(s, z), ra = RocAir(s, z);
                foreach (var u in UnitsAt(s, z, UnitDomain.Sea)) u.Str -= 8.0 * ps / (ps + rs + 0.01) * (0.6 + 0.8 * Rng(s));
                foreach (var u in UnitsAt(s, z, UnitDomain.Air)) u.Str -= 8.0 * pa / (pa + ra + 0.01) * (0.6 + 0.8 * Rng(s));
            }
            PurgeDead(s);

            double frontRec = s.Techs.Contains("sea_shuttle") ? 1.6 : 0.8;
            foreach (var u in s.Units)
            {
                var dom = GameData.UnitTypes[u.Type].Domain;
                bool home = u.Loc == "TW" || (dom != UnitDomain.Land && rep.BattleNodes.Count == 0);
                double cap = u.Type == "mil" ? 80.0 : 100.0;
                if (home)
                {
                    u.Str = Math.Min(cap, u.Str + 1.6);
                }
                else if (dom != UnitDomain.Land)
                {
                    // 海空／兩棲單位駐防海域時僅在非交戰海域恢復戰力，避免誤將海域代碼(Z1~Z4)當陸地節點查表
                    if (!activeZones.Contains(u.Loc))
                        u.Str = Math.Min(cap, u.Str + frontRec);
                }
                else if (s.Control.TryGetValue(u.Loc, out var locCtrl) && locCtrl == Faction.ROC
                    && s.Garrison.GetValueOrDefault(u.Loc, 0.0) <= 0.01 && u.SupFactor > 0.8)
                {
                    u.Str = Math.Min(cap, u.Str + frontRec);
                }
                bool hasActiveOrder = s.Orders.Any(o => o.UnitId == u.Id);
                if (!hasActiveOrder) u.Moved = false;
                u.SupFactor = 1.0;
            }

            StepAutoActions(s);
            EconomyStep(s, dt, stepResearch);
            PrcUpdate(s, dt);
            RollFog(s);
            Normalize(s);
            TouchFrontClock(s);
            s.LastReport = rep;
        }

        public static void ResolveSupply(GameState s, double dt = 1.0 / 12.0)
        {
            double annualDemand = 0;
            var zonesUsed = new HashSet<string>();
            bool hiEmbattled = s.Control.GetValueOrDefault("HI") == Faction.ROC && (s.Garrison.GetValueOrDefault("HI") > 0.01 || s.HasFlag("hiPortBlocked"));
            int hiLandCount = UnitsAt(s, "HI", UnitDomain.Land).Count;
            foreach (var u in s.Units)
            {
                var T = GameData.UnitTypes[u.Type];
                if (T.Domain != UnitDomain.Land || u.Loc == "TW") continue;
                var n = GameData.Nodes[u.Loc];
                double k;
                if (u.Loc == "HI")
                    k = hiEmbattled ? 1.35 : (hiLandCount > 2 ? 1.05 : 0.65); // 海南逾千公里遠海運補：重兵屯駐與封鎖交戰時消耗劇增
                else
                    k = n.Kind == NodeKind.Island ? 0.5 : (u.Type == "mil" ? 0.3 : 1.0);
                annualDemand += T.Supply * (u.Str / 100.0) * k;
                foreach (var z in n.Zones) zonesUsed.Add(z);
            }
            if (s.Techs.Contains("auto_logi")) annualDemand *= 0.75;
            s.Supply += (5.0 + s.Gdp / 120.0) * (s.Techs.Contains("sea_shuttle") ? 1.5 : 1.0) * (s.Techs.Contains("high_speed_logi") ? 1.25 : 1.0) * dt;
            double need = annualDemand * dt;
            if (need <= 0) { s.SupplyRatio = 1.0; return; }
            double sup = zonesUsed.Count > 0 ? zonesUsed.Average(z => SeaSup(s, z)) : 1.0;
            int ports = GameData.MainlandIds.Count(id => s.Control[id] == Faction.ROC && GameData.Nodes[id].Port);
            double cap = (s.SupplyCap + ports * 1.8) * (0.4 + 0.6 * sup) * (LiftCapacity(s) > 0 ? 1.0 : 0.5) * dt;
            double delivered = Math.Min(need, Math.Min(cap, s.Supply));
            s.Supply -= delivered;
            double ratio = delivered / need;
            s.SupplyRatio = ratio;

            var connected = new HashSet<string>();
            foreach (var id in GameData.NodeIds)
            {
                var n = GameData.Nodes[id];
                if (n.Kind != NodeKind.Mainland) connected.Add(id);
                else if (n.Port && s.Control[id] == Faction.ROC)
                    connected.Add(id);
            }
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var id in GameData.MainlandIds)
                {
                    if (connected.Contains(id)) continue;
                    if (s.Control[id] != Faction.ROC && UnitsAt(s, id, UnitDomain.Land).Count == 0) continue;
                    if (GameData.Nodes[id].Adj.Any(a => connected.Contains(a) && GameData.Nodes[a].Kind == NodeKind.Mainland && s.Control[a] == Faction.ROC))
                    {
                        connected.Add(id);
                        changed = true;
                    }
                }
            }

            double z4Sea = SeaSup(s, "Z4");
            double hiAccess = hiEmbattled
                ? Clamp(0.22 + 0.72 * z4Sea * (s.HasFlag("hiPortBlocked") ? 0.65 : 1.0), 0.15, 0.90)
                : (hiLandCount > 3 ? Clamp(0.55 + 0.45 * z4Sea, 0.45, 1.0) : 1.0);

            foreach (var u in s.Units)
            {
                if (GameData.UnitTypes[u.Type].Domain != UnitDomain.Land || u.Loc == "TW") continue;
                double r = connected.Contains(u.Loc) ? ratio : 0.0;
                if (u.Loc == "HI") r *= hiAccess;
                if (u.Type == "mil") r = Math.Max(r, 0.65);
                u.SupFactor = 0.45 + 0.55 * r;
                if (r < 0.6) u.Str -= (0.6 - r) * 20.0 * dt;
            }
            PurgeDead(s);
        }

        private static void PrcAi(GameState s, TurnReport rep, HashSet<string> activeZones, double dt)
        {
            double reinf = (10.0 + (s.Year - 1950) * 0.18) * PrcStabFactor(s) * s.PrcReinfMod
                         * (s.Techs.Contains("tienma") ? 0.75 : 1.0) * (1.0 - 0.5 * s.PrcSovTension) * (1.0 - 0.6 * PrcLostIndustry(s)) * dt;

            foreach (var nid in GameData.MainlandIds)
            {
                if (s.Control[nid] != Faction.ROC) continue;
                double force = 0;
                foreach (var a in GameData.Nodes[nid].Adj)
                {
                    if (s.Control[a] != Faction.PRC) continue;
                    double spare = Math.Max(0, s.Garrison[a] - NominalGarrison(s, a) * 0.55) * 0.14;
                    s.Garrison[a] -= spare;
                    force += spare;
                    if (spare > 0.2) rep.PrcMoves.Add(new EnemyMoveRecord { From = a, To = nid });
                }
                if (force > 0.2) s.Garrison[nid] += force;
            }

            var needs = new List<(string id, double def, double target)>();
            foreach (var id in GameData.MainlandIds)
            {
                if (s.Control[id] != Faction.PRC) continue;
                double nom = NominalGarrison(s, id);
                double threat = LandStr(UnitsAt(s, id, UnitDomain.Land)) * 1.5;
                foreach (var a in GameData.Nodes[id].Adj)
                {
                    if (s.Control[a] == Faction.ROC || UnitsAt(s, a, UnitDomain.Land).Count > 0)
                        threat += LandStr(UnitsAt(s, a, UnitDomain.Land));
                    if (s.Control[a] == Faction.USSR) threat += 2.0;
                }
                double target = nom + threat;
                needs.Add((id, target - s.Garrison[id], target));
            }

            double pool = Math.Max(0, reinf);
            double totalDef = 0;
            foreach (var item in needs)
                if (item.def > 0.05) totalDef += item.def;
            double added = 0;
            if (pool > 0.01 && totalDef > 0.05)
            {
                double give = Math.Min(pool, totalDef);
                foreach (var item in needs)
                {
                    if (item.def <= 0.05) continue;
                    double amt = give * (item.def / totalDef);
                    s.Garrison[item.id] += amt;
                    added += amt;
                }
            }
            if (added >= 0.05)
            {
                int total = (int)Math.Round(PrcDeployed(s));
                int seen = s.GetFlag("prcDivSeen");
                if (seen <= 0) s.Flags["prcDivSeen"] = total;
                else if (total >= seen + 3)
                {
                    Log(s, $"共軍持續擴編，大陸部署增至 {total} 師。", "bad");
                    s.Flags["prcDivSeen"] = total;
                }
            }

            PrcCounterattack(s, rep);

            // 瓊州海峽 20–30 公里態勢：四野背靠華南大陸（廣東未復時）對仍由我方控制的海南島實施持續跨海壓迫與瓊崖縱隊裡應外合
            bool qiongzhouActive = s.Control["HI"] == Faction.ROC && s.Control["GD"] == Faction.PRC
                && (s.Year >= 1951 || (s.Year == 1950 && (s.Month >= 5 || s.HasFlag("holdHainan"))));
            if (qiongzhouActive)
            {
                double z4Sea = SeaSup(s, "Z4");
                double z4Air = AirSup(s, "Z4");
                double straitPass = Clamp(1.0 - 0.42 * z4Sea - 0.18 * z4Air, 0.35, 1.0);
                double gdExcess = Math.Max(0.0, s.Garrison["GD"] - NominalGarrison(s, "GD") * 1.05);
                if (s.Garrison["HI"] > 0.01)
                {
                    double southBack = (1.35 + (s.HasFlag("hiQiongya") ? 0.35 : 0.0)) * straitPass * PrcStabFactor(s);
                    double siphoned = Math.Min(gdExcess * 0.25, 0.45);
                    if (siphoned > 0.05) s.Garrison["GD"] -= siphoned;
                    double wave = southBack + siphoned;
                    s.Garrison["HI"] += wave;
                    s.Flags["hiPortBlocked"] = 1;
                    rep.PrcMoves.Add(new EnemyMoveRecord { From = "GD", To = "HI", Landing = true });
                    Log(s, $"⚠ 瓊州海峽僅二十餘公里寬，四野背靠華南大陸源源增援海南 +{wave:F1} 師（瓊崖縱隊裡應外合、海口港遭封鎖）！", "bad");
                }
                else
                {
                    double pCross = s.HasFlag("holdHainan") ? 0.72 : 0.36;
                    if (s.PrcStab > 40 && Chance(s, pCross))
                    {
                        double amph = GameData.Interp(GameData.CurvePrcAmph, s.Year) * 1.45;
                        double siphoned = Math.Min(gdExcess * 0.40, 1.1);
                        if (siphoned > 0.05) s.Garrison["GD"] -= siphoned;
                        double wave = Math.Max(2.8, amph + siphoned) * straitPass;
                        s.Garrison["HI"] = wave;
                        s.Flags["hiPortBlocked"] = 1;
                        activeZones.Add("Z4");
                        rep.PrcMoves.Add(new EnemyMoveRecord { From = "GD", To = "HI", Landing = true });
                        Log(s, $"共軍第四野戰軍自雷州半島強渡瓊州海峽登陸海南（{wave:F1} 師），海口港防線危殆！", "bad");
                    }
                }
            }

            double twDef = LandStr(UnitsAt(s, "TW", UnitDomain.Land));
            if (twDef >= 6.5) s.Flags.Remove("twEmptyWarned");
            if (s.Year >= 1950)
            {
                foreach (var iid in new[] { "KM", "MZ", "DC", "ZS", "HI", "PH" })
                {
                    if (iid == "HI" && qiongzhouActive) continue; // 海南已由上方瓊州海峽專項連續演算
                    if (s.Control[iid] != Faction.ROC || s.Garrison[iid] > 0) continue;
                    if (s.Year == 1950)
                    {
                        // 1950 韓戰美援尚未就位：僅當我方抽空舟山防線或 5 月以後仍固守孤懸舟山時，三野即時伺機攻奪
                        if (iid != "ZS") continue;
                        double zsDef = LandStr(UnitsAt(s, "ZS", UnitDomain.Land));
                        if (s.Month < 3 || (s.Month < 5 && zsDef >= 1.3)) continue;
                    }
                    string z = GameData.Nodes[iid].Zones[0];
                    double pP = PrcSea(s, z) + PrcAir(s, z);
                    double rP = RocSea(s, z) + RocAir(s, z) + (iid == "PH" ? UsDefense(s) : UsDefense(s) * 0.3);
                    double def = LandStr(UnitsAt(s, iid, UnitDomain.Land));
                    double amph = GameData.Interp(GameData.CurvePrcAmph, s.Year) * 1.2;
                    string src = iid == "KM" || iid == "MZ" || iid == "PH" ? "FJ" : iid == "DC" ? "ZJ" : iid == "HI" ? "GD" : "JS";
                    if (s.Control[src] != Faction.PRC) continue;
                    double force = Math.Min(amph, s.Garrison[src] * 0.4);
                    bool zsExposed1950 = iid == "ZS" && s.Year == 1950 && (def < 1.3 || s.Month >= 5);
                    double reqChance = zsExposed1950 ? (def < 1.3 ? 0.48 : 0.18) : 0.032;
                    if ((zsExposed1950 || pP > rP * 1.4) && force > def * (zsExposed1950 ? 1.05 : 1.3) && s.PrcStab > 40 && Chance(s, reqChance))
                    {
                        double landed = force * (1.0 - 0.5 * SeaSup(s, z));
                        s.Garrison[src] -= force;
                        s.Garrison[iid] = landed;
                        rep.PrcMoves.Add(new EnemyMoveRecord { From = src, To = iid, Landing = true });
                        Log(s, $"共軍渡海進犯{GameData.Nodes[iid].Name}！登陸兵力約 {landed:F1} 師。", "bad");
                    }
                }
            }

            double th = ThreatIndex(s);
            // 1950 韓戰美軍協防前，若我方把台灣本島主力大量抽往海南等外島（添油戰術致本島低於 5.8 師），三野將趁本島防禦空虛直接渡海攻台！
            bool exposedBeforeUs = !s.UsProtect && ((s.Year == 1950 && s.Month >= 3) || s.Year >= 1951)
                                   && twDef < 5.8 && s.Control["FJ"] == Faction.PRC;
            if (exposedBeforeUs && s.Garrison["TW"] <= 0 && !s.HasFlag("twEmptyWarned"))
            {
                s.Flags["twEmptyWarned"] = 1;
                Log(s, $"‼【本島極危】台灣本島防禦已抽空至 {twDef:F1} 師且尚無美軍協防，華東三野已完成渡海搶灘集結，若不回防將有覆滅之虞！", "bad");
            }
            if ((exposedBeforeUs || (s.Year >= 1952 && th > 2.0)) && !s.HasFlag("rocNuke") && s.PrcStab > 50 && s.Garrison["TW"] <= 0)
            {
                double invChance = exposedBeforeUs
                    ? Clamp(0.18 + Math.Max(0.0, 5.8 - twDef) * 0.14, 0.18, 0.72) * (1.0 - 0.45 * SeaSup(s, "Z3"))
                    : 0.016 * (th - 1.5);
                if (Chance(s, invChance))
                {
                    double f = GameData.Interp(GameData.CurvePrcAmph, s.Year) * (exposedBeforeUs ? 2.2 : 1.5);
                    double sz = SeaSup(s, "Z2") * 0.5 + SeaSup(s, "Z3") * 0.5;
                    s.Garrison["TW"] = f * (1.0 - 0.55 * sz);
                    activeZones.Add("Z2"); activeZones.Add("Z3");
                    rep.PrcMoves.Add(new EnemyMoveRecord { From = "FJ", To = "TW", Landing = true });
                    Log(s, exposedBeforeUs
                        ? $"‼ 台灣本島主力遭調空且尚無美援協防，共軍第三野戰軍趁虛發動渡海攻台（登陸 {s.Garrison["TW"]:F1} 師）！"
                        : $"‼ 解放軍發動渡海攻台作戰！約 {s.Garrison["TW"]:F1} 師登陸台灣本島。", "bad");
                }
            }
        }

        private static void PrcCounterattack(GameState s, TurnReport rep)
        {
            var candidates = new List<(string target, double force, double priority)>();
            var noLandingBonus = new HashSet<int>();

            foreach (string target in GameData.MainlandIds)
            {
                if (s.Control[target] != Faction.ROC) continue;

                var defenders = UnitsAt(s, target, UnitDomain.Land);
                double air = NodeAirSup(s, target);
                double rocPower = RocPowerAt(s, target, defenders, attacking: false, noLandingBonus)
                                * (1.0 + 0.6 * air);
                double prcPowerPerDivision = EnemyPowerAt(s, target, 1.0, defending: false)
                                           * (1.0 + 0.6 * (1.0 - air));
                if (prcPowerPerDivision <= 0.01) continue;

                double committedPrcPower = EnemyPowerAt(s, target, Math.Max(0, s.Garrison[target]), defending: false)
                                         * (1.0 + 0.6 * (1.0 - air));
                double forceNeeded = (rocPower * 3.0 - committedPrcPower) / prcPowerPerDivision;
                // Count both armies' combat power. Existing PRC troops already attacking this province reduce
                // the new force needed; require a 3:1 effective-power advantage before committing more troops.
                if (forceNeeded <= 0.01) continue;

                var sources = GameData.Nodes[target].Adj
                    .Where(from => s.Control[from] == Faction.PRC)
                    .Select(from => (id: from, available: Math.Max(0, s.Garrison[from] - NominalGarrison(s, from) * 0.55)))
                    .ToList();
                double totalAvailable = sources.Sum(x => x.available);
                double force = Math.Max(0.75, forceNeeded);
                if (totalAvailable + 1e-9 < force) continue;

                double priority = (GameData.Nodes[target].Capital ? 2.0 : 0.0)
                                + (GameData.Nodes[target].Major ? 1.0 : 0.0)
                                - force * 0.01;
                candidates.Add((target, force, priority));
            }

            // Resolve strategically important and lower-cost targets first. Source reserves are recalculated
            // after every attack, so several fronts can attack without stripping any province below its reserve.
            foreach (var candidate in candidates.OrderByDescending(x => x.priority).ThenBy(x => x.force))
            {
                double remaining = candidate.force;
                var fromNames = new List<string>();
                var sources = GameData.Nodes[candidate.target].Adj
                    .Where(from => s.Control[from] == Faction.PRC)
                    .Select(from => (id: from, available: Math.Max(0, s.Garrison[from] - NominalGarrison(s, from) * 0.55)))
                    .OrderByDescending(x => x.available)
                    .ToList();
                if (sources.Sum(x => x.available) + 1e-9 < candidate.force) continue;

                foreach (var source in sources)
                {
                    double sent = Math.Min(remaining, source.available);
                    if (sent <= 0.01) continue;
                    s.Garrison[source.id] -= sent;
                    s.Garrison[candidate.target] += sent;
                    rep.PrcMoves.Insert(0, new EnemyMoveRecord { From = source.id, To = candidate.target });
                    fromNames.Add(GameData.Nodes[source.id].Name);
                    remaining -= sent;
                    if (remaining <= 0.01) break;
                }

                if (remaining > 0.01) continue;
                Log(s, $"共軍由【{string.Join("、", fromNames)}】集結約 {candidate.force:F1} 師，向【{GameData.Nodes[candidate.target].Name}】發起反攻。", "bad");
            }
        }

        private static void EconomyStep(GameState s, double dt, bool stepResearch = false)
        {
            double resRate = stepResearch ? ResCost(s) : BaseResCost(s);
            double net = Income(s) - Upkeep(s) - resRate - Interest(s);
            s.Treasury += net * dt;
            if (s.Treasury < 0)
            {
                s.Debt += -s.Treasury;
                s.Treasury = 0;
                s.Morale -= 0.25;
            }
            if (stepResearch)
            {
                StepResearch(s, dt, chargeDailySurcharge: false);
            }
            double gNext = GameData.Interp(GameData.CurveRocGdp, s.Year + 1);
            double gCur = Math.Max(1.0, GameData.Interp(GameData.CurveRocGdp, s.Year));
            double gh = (gNext / gCur) - 1.0;
            double mil = Upkeep(s) / Math.Max(0.01, Income(s));
            double burden = Clamp(1.0 - Math.Max(0.0, mil - 0.55) * 0.5, 0.55, 1.0);
            double moraleF = 0.8 + 0.4 * (s.Morale / 100.0);
            double war = GameData.MainlandIds.Any(id => UnitsAt(s, id, UnitDomain.Land).Count > 0) ? 0.12 : 0.0;
            double annualG = Math.Max(-0.5, gh * s.EconMult * burden * moraleF * (1.0 - war));
            s.Gdp *= Math.Pow(1.0 + annualG, dt);
            s.Manpower += (1.4 + (s.Year - 1950) / 35.0 + HeldMainland(s).Count * 0.45) * dt;
            s.Morale += (56.0 - s.Morale) * 0.008 + (s.Debt > s.Gdp * 0.5 ? -0.20 : 0.0);
            if (s.UsAid > 0 && s.Year >= 1965) s.UsAid = Math.Max(0, s.UsAid - 0.05 * dt);
        }

        private static void PrcUpdate(GameState s, double dt)
        {
            double target = s.Year >= 1978 ? 75.0 : 65.0;
            int lostCount = GameData.MainlandIds.Count(id => s.Control[id] != Faction.PRC);
            bool radio = s.Techs.Contains("radio_liberty");
            bool cyber = s.Techs.Contains("cyber_psy");
            bool cult = s.Techs.Contains("cultural_psy");
            bool relief = s.Techs.Contains("mainland_relief");
            bool kinHorn = s.Techs.Contains("kinmen_horn");
            s.PrcStab += ((target - s.PrcStab) * 0.08 - lostCount * 1.6 - (radio ? 0.45 : 0.0) - (cyber ? 0.35 : 0.0) - (cult ? 0.20 : 0.0)) * dt;
            s.PrcGdpMod += (1.0 - s.PrcGdpMod) * 0.06 * dt;
            s.PrcSovTension = Math.Max(0, s.PrcSovTension - 0.04 * dt);
            foreach (var id in GameData.MainlandIds)
            {
                if (s.Control[id] != Faction.PRC) continue;
                bool adjRoc = GameData.Nodes[id].Adj.Any(a => s.Control[a] == Faction.ROC);
                double psyBoost = (radio ? 1.4 : 0) + (cyber ? 1.0 : 0) + (relief ? 0.6 : 0) + (kinHorn && (id == "FJ" || id == "ZJ") ? 1.2 : 0);
                s.Unrest[id] += ((adjRoc ? 3.5 : 0) - 1.5 + (s.PrcStab < 40 ? 2.5 : 0) + psyBoost) * dt;
            }
        }

        // ---------- 勝敗與 14 種結局判定 ----------
        public static void CheckEnd(GameState s, bool tickAnnualCounters = true)
        {
            if (s.Over != null) return;
            var held = HeldMainland(s);
            var ussr = UssrNodes(s);
            bool allMainlandRoc = GameData.MainlandIds.All(id => s.Control[id] == Faction.ROC);
            bool rocHasAnyNode = GameData.NodeIds.Any(id => s.Control[id] == Faction.ROC);
            bool rocHasLandOnRocNode = GameData.NodeIds.Any(id => s.Control[id] == Faction.ROC && UnitsAt(s, id, UnitDomain.Land).Count > 0);
            bool rocWipedOut = !rocHasAnyNode || (s.Control["TW"] != Faction.ROC && held.Count == 0 && !rocHasLandOnRocNode);

            if (tickAnnualCounters)
            {
                if (held.Count >= 2 && s.Morale < 25 && s.SupplyRatio < 0.7)
                    s.Flags["warlordRisk"] = s.GetFlag("warlordRisk") + 1;
                else
                    s.Flags["warlordRisk"] = 0;

                if (s.PrcStab <= 0)
                    s.Flags["prcZeroTurns"] = s.GetFlag("prcZeroTurns") + 1;
                else
                    s.Flags["prcZeroTurns"] = 0;
            }

            // 遊戲時間結束前，僅在「我方全滅」或「敵方大陸全數肅清全滅」時才觸發終局；其餘所有結局皆須待遊戲時間結束後方結算
            if (rocWipedOut) { Finish(s, "invaded"); return; }
            if (allMainlandRoc) { Finish(s, "full"); return; }
            if (CeasefireMatured(s)) { FinishPartition(s); return; }

            if (s.Control["HE"] == Faction.ROC && ussr.Count > 0 && !s.HasFlag("bjUssrChoiceFired"))
            {
                s.Flags["bjUssrChoiceFired"] = 1;
                EventSystem.TriggerBeijingUssrChoice(s);
                return;
            }

            if (s.Year > GameData.EndYear)
            {
                if (s.Control["TW"] != Faction.ROC && held.Count == 0) { Finish(s, "invaded"); return; }
                if (s.HasFlag("annexed")) { Finish(s, "annexed"); return; }
                if (s.HasFlag("armistice"))
                {
                    if (s.GetFlag("ceasefireAt") > 0) FinishPartition(s);
                    else Finish(s, "armistice");
                    return;
                }
                if (s.HasFlag("acceptUssrSplit") && ussr.Count > 0) { Finish(s, "split_ussr"); return; }
                if (s.Debt > s.Gdp * 1.2 && s.Treasury <= 0) { Finish(s, "bankrupt"); return; }
                if (s.Control["HE"] == Faction.ROC)
                {
                    Finish(s, ussr.Count > 0 ? "split_ussr" : "full");
                    return;
                }
                if (s.Morale <= 0) { Finish(s, "mutiny"); return; }
                if (s.GetFlag("warlordRisk") >= 3 && held.Count >= 2) { Finish(s, "warlordR"); return; }
                double rocLand = LandStr(s.Units);
                if (s.PrcStab <= 0 && s.GetFlag("prcZeroTurns") >= 2 && (held.Count < 4 || rocLand < 15))
                {
                    Finish(s, "warlordP");
                    return;
                }
                if (s.PrcStab <= 0 && held.Count >= 4 && s.Morale >= 40)
                {
                    Finish(s, ussr.Count > 0 ? "split_ussr" : "collapse");
                    return;
                }
                bool hasMajor = held.Contains("JS") || held.Contains("GD") || held.Contains("HN");
                if (held.Count >= 5 && hasMajor) { Finish(s, "nanbei"); return; }
                if (held.Count >= 1) { Finish(s, "beachhead"); return; }
                if (s.Gdp > 1800 && s.Intl > 40) { Finish(s, "status"); return; }
                Finish(s, "pianan");
            }
        }

        public const int CeasefireMonths = 36;

        public static bool CeasefireMatured(GameState s)
        {
            int at = s.GetFlag("ceasefireAt");
            return at > 0 && s.TotalMonths >= (at - 1) + CeasefireMonths;
        }

        public static void FinishPartition(GameState s)
        {
            Finish(s, "armistice");
            if (s.Over == null) return;
            var held = HeldMainland(s);
            var ussr = UssrNodes(s);
            var prc = GameData.MainlandIds.Where(id => s.Control.TryGetValue(id, out var f) && f == Faction.PRC).ToList();
            static string Names(IReadOnlyList<string> ids)
            {
                var shown = ids.Take(6).Select(id => GameData.Nodes[id].Name);
                string text = string.Join("、", shown);
                return ids.Count > 6 ? text + $"等 {ids.Count} 省" : text;
            }
            string heldTxt = held.Count == 0
                ? "國軍只保有台灣與外島，大陸沒有省分"
                : "國軍控制" + Names(held);
            string otherTxt;
            if (prc.Count == 0 && ussr.Count == 0)
                otherTxt = "大陸沒有對方控制區";
            else if (ussr.Count == 0)
                otherTxt = "其餘大陸由對方控制" + Names(prc);
            else if (prc.Count == 0)
                otherTxt = "其餘大陸由俄軍控制" + Names(ussr);
            else
                otherTxt = "其餘大陸由對方控制" + Names(prc) + "，另有俄軍占" + Names(ussr);
            s.Over.Title = "中國分治・停火線確立";
            s.Over.Group = "停止戰線";
            s.Over.Text = $"聯合國停火滿三年，雙方沿當時戰線停止交戰。{heldTxt}。{otherTxt}。中國就此分治。";
        }

        public static void Finish(GameState s, string key)
        {
            var e = GameData.Endings[key];
            s.Over = new GameOverInfo
            {
                Key = key,
                Group = e.Group,
                Kind = e.Kind,
                Title = e.Title,
                Text = e.Text,
                Score = Score(s)
            };
            Log(s, $"【結局】{e.Title}：{e.Text}", "big");
        }

        public static int Score(GameState s)
        {
            double sc = HeldMainland(s).Sum(id => GameData.Nodes[id].Major ? 150 : 80);
            sc -= UssrNodes(s).Count * 100;
            sc += Math.Round(s.Gdp / 20.0) + s.Intl + s.Morale + s.BattlesWon * 10;
            return (int)Math.Round(sc);
        }

        public static readonly CheckpointSpec[] CheckpointSpecs =
        {
            new CheckpointSpec
            {
                Key = "cp_1962",
                Title = "紀錄點 I · 1962 國光階段",
                RequirementDesc = "需自 1950 局推進至 1962 年，或光復 ≥1 省且 GDP ≥ $22 億",
                Cond = s => s.Control["TW"] == Faction.ROC && (s.Year >= 1962 || (HeldMainland(s).Count >= 1 && s.Gdp >= 22.0))
            },
            new CheckpointSpec
            {
                Key = "cp_1967",
                Title = "紀錄點 II · 1967 文革動盪",
                RequirementDesc = "需推進至 1967 年，或提前光復 ≥3 個大陸省分",
                Cond = s => s.Control["TW"] == Faction.ROC && (s.Year >= 1967 || HeldMainland(s).Count >= 3)
            },
            new CheckpointSpec
            {
                Key = "cp_1978",
                Title = "紀錄點 III · 1978 世紀轉折",
                RequirementDesc = "需推進至 1978 年，或攻克江南/兩廣/華北任一重鎮（或光復 ≥5 省）",
                Cond = s => s.Control["TW"] == Faction.ROC && (s.Year >= 1978 || HeldMainland(s).Count >= 5
                            || s.Control["JS"] == Faction.ROC || s.Control["GD"] == Faction.ROC || s.Control["HE"] == Faction.ROC)
            }
        };

        public static List<string> EvaluateCheckpoints(GameState s, Dictionary<string, CheckpointRecord> store)
        {
            var unlocked = new List<string>();
            if (s == null || store == null) return unlocked;
            if (s.Over != null || s.PendingEvents.Count > 0 || s.Turn < 1) return unlocked;

            int sc = Score(s);
            int heldCount = HeldMainland(s).Count;

            foreach (var spec in CheckpointSpecs)
            {
                string flagKey = "cpRecorded_" + spec.Key;
                if (s.HasFlag(flagKey)) continue;
                if (!spec.Cond(s)) continue;

                bool exists = store.TryGetValue(spec.Key, out var prev);
                if (!exists || sc > prev.Score)
                {
                    s.Flags[flagKey] = 1;
                    string snapJson = System.Text.Json.JsonSerializer.Serialize(s);
                    store[spec.Key] = new CheckpointRecord
                    {
                        Key = spec.Key,
                        Title = spec.Title,
                        Year = s.Year,
                        HeldCount = heldCount,
                        RocGdp = s.Gdp,
                        Score = sc,
                        UnlockHint = spec.RequirementDesc,
                        StateJson = snapJson
                    };
                    if (!exists)
                    {
                        Log(s, $"★ 達成里程碑，解鎖【{spec.Title}】！（已保存當下戰局，下次可自此紀錄點開始不同分支）", "big");
                        unlocked.Add(spec.Key);
                    }
                    else
                    {
                        Log(s, $"★ 刷新最佳紀錄點【{spec.Title}】！（{s.Year}年 · 光復 {heldCount} 省 · 評分 {sc}）", "good");
                    }
                }
                else
                {
                    s.Flags[flagKey] = 1;
                }
            }
            return unlocked;
        }
    }
}
