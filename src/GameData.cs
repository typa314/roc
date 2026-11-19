using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace RocGame
{
    public enum Faction { ROC, PRC, USSR }
    public enum NodeKind { Taiwan, Island, Mainland }
    public enum UnitDomain { Land, Sea, Amph, Air }

    public class SeaZoneDef
    {
        public string Id;
        public string Name;
        public float Lon, Lat;
        public float MapX, MapY;
        public double PrcWeight;
    }

    public class NodeDef
    {
        public string Id;
        public string Name;
        public string City;
        public NodeKind Kind;
        public float Lon, Lat;
        public float CalloutLon, CalloutLat;
        public bool HasCallout => CalloutLon > 0.1f;
        public string[] Zones = Array.Empty<string>();
        public string[] Adj = Array.Empty<string>();
        public bool Port;
        public bool Airfield;
        public int Fort;
        public int Industry;
        public double BaseGarrison;
        public bool Major;
        public bool Capital;
        public bool BorderUssr;
        public PointF[] Poly = Array.Empty<PointF>();
    }

    public class UnitTypeDef
    {
        public string Id;
        public string Name;
        public string Short;
        public UnitDomain Domain;
        public double Atk;
        public double Def;
        public double Power;
        public double LiftCap;
        public double Cost;
        public double Upkeep;
        public double Man;
        public double Lift;
        public double Supply;
        public string ReqTech;
        public bool Marine;
        public bool Airborne;
        public bool NoBuild;
    }

    public class TechDef
    {
        public string Id;
        public string Branch; // science(科技), arms(軍備), intel(情蒐), subv(策反), logi(後勤)
        public int Tier;      // 0..4 科技樹階層位置
        public string Name;
        public int Year;
        public double Cost;
        public bool Real;
        public string[] Req = Array.Empty<string>();
        public string ReqFlag;
        public string Desc;
        public Action<GameState> Apply;
    }

    public class EndingDef
    {
        public string Key;
        public string Kind;   // win, partial, chaos, draw, lose
        public string Group;  // 完全收復, 北疆俄占, 停止戰線, 軍閥割據, 維持現狀, 反被併吞
        public string Title;
        public string Text;
    }

    public class MutatorDef
    {
        public string Id;
        public string Title;
        public string Desc;
        public double ScoreBonus;
    }

    public static class GameData
    {
        public const string GameTitle = "國光：反攻大陸";
        public const int StartYear = 1950;
        public const int EndYear = 2025; // 1950–2025，終戰結算年；途中仍只在全滅或大陸肅清時立即結束
        public const int MapW = 1040;
        public const int MapH = 760;

        // 蘭伯特正形圓錐投影（Lambert Conformal Conic，中央經線 105°E、標準緯線 25°N & 47°N，1:1 等比例無扭曲）
        public static PointF Project(double lon, double lat)
        {
            const double n = 0.590220366;
            const double F = 1.809600375;
            const double rho0 = 1.385981140;
            const double lam0 = 105.0 * Math.PI / 180.0;

            double phi = lat * Math.PI / 180.0;
            double lam = lon * Math.PI / 180.0;
            double rho = F / Math.Pow(Math.Tan(Math.PI / 4.0 + phi / 2.0), n);
            double theta = n * (lam - lam0);
            double rx = rho * Math.Sin(theta);
            double ry = rho0 - rho * Math.Cos(theta);

            double x = 472.0 + rx * 1180.0;
            double y = 588.0 - ry * 1180.0;
            return new PointF((float)x, (float)y);
        }

        public static double Interp(SortedDictionary<int, double> curve, double year)
        {
            var keys = curve.Keys.ToList();
            if (year <= keys[0]) return curve[keys[0]];
            if (year >= keys[keys.Count - 1]) return curve[keys[keys.Count - 1]];
            for (int i = 0; i < keys.Count - 1; i++)
            {
                int y0 = keys[i], y1 = keys[i + 1];
                if (year >= y0 && year <= y1)
                {
                    double t = (year - y0) / (y1 - y0);
                    return curve[y0] + (curve[y1] - curve[y0]) * t;
                }
            }
            return curve[keys[0]];
        }

        public static readonly SortedDictionary<int, double> CurvePrcGdp = new()
        {
            {1950, 250}, {1952, 300}, {1957, 500}, {1960, 580}, {1962, 460}, {1965, 700},
            {1968, 700}, {1970, 930}, {1975, 1480}, {1978, 1550}, {1982, 2200}, {1986, 3200},
            {1990, 3900}, {1993, 6200}, {1996, 8600}, {2000, 12100}, {2008, 46000},
            {2015, 111000}, {2020, 147000}, {2025, 182000}
        };

        public static readonly SortedDictionary<int, double> CurveRocGdp = new()
        {
            {1950, 14}, {1955, 19}, {1960, 25}, {1965, 38}, {1970, 65}, {1975, 165},
            {1980, 430}, {1985, 660}, {1990, 1680}, {1996, 2890}, {2000, 3310},
            {2008, 4150}, {2015, 5350}, {2020, 6700}, {2025, 8150}
        };

        public static readonly SortedDictionary<int, double> CurvePrcAir = new()
        {
            {1950, 8}, {1952, 32}, {1956, 60}, {1960, 78}, {1966, 88}, {1976, 95}, {1985, 105}, {1996, 135},
            {2005, 170}, {2015, 230}, {2025, 310}
        };
        public static readonly SortedDictionary<int, double> CurvePrcAirTech = new()
        {
            {1950, 1.0}, {1955, 1.6}, {1960, 2.0}, {1970, 2.2}, {1980, 2.5}, {1990, 3.0}, {1996, 3.8}, {2025, 5.2}
        };
        public static readonly SortedDictionary<int, double> CurvePrcNavy = new()
        {
            {1950, 6}, {1955, 11}, {1960, 16}, {1970, 24}, {1980, 36}, {1990, 52}, {1996, 80},
            {2005, 110}, {2015, 180}, {2025, 280}
        };
        public static readonly SortedDictionary<int, double> CurvePrcNavyTech = new()
        {
            {1950, 1.0}, {1960, 1.4}, {1970, 1.7}, {1980, 2.1}, {1990, 2.7}, {1996, 3.5}, {2025, 4.6}
        };
        public static readonly SortedDictionary<int, double> CurvePrcArmyTech = new()
        {
            {1950, 1.0}, {1960, 1.5}, {1966, 1.6}, {1976, 1.8}, {1985, 2.3}, {1996, 3.3}, {2025, 4.4}
        };
        // 相對 1950 名目師數的倍數。地圖籌碼與頂欄「共軍 N 師」用這個漲，不跟海空曲線混在一起。
        public static readonly SortedDictionary<int, double> CurvePrcGarrison = new()
        {
            {1950, 1.00}, {1952, 1.20}, {1956, 1.45}, {1962, 1.80}, {1970, 2.15},
            {1980, 2.55}, {1990, 3.00}, {1996, 3.30}, {2008, 3.75}, {2016, 4.05}, {2025, 4.45}
        };
        public static readonly SortedDictionary<int, double> CurvePrcAmph = new()
        {
            {1950, 2.0}, {1960, 3.0}, {1975, 3.8}, {1985, 4.8}, {1996, 8.5}, {2010, 12.0}, {2025, 20.0}
        };
        public static readonly SortedDictionary<int, double> CurvePrcMissile = new()
        {
            {1950, 0}, {1988, 0}, {1992, 8}, {1996, 35}, {2010, 55}, {2020, 90}, {2025, 130}
        };
        public static readonly SortedDictionary<int, double> CurvePriceIndex = new()
        {
            {1950, 1.0}, {1960, 1.6}, {1970, 3.0}, {1980, 10.0}, {1990, 24.0}, {1996, 34.0},
            {2005, 40.0}, {2015, 48.0}, {2025, 58.0}
        };

        public static readonly PointF[] MainlandOutline = new PointF[]
        {
            new(121.5f, 53.3f), new(125.5f, 52.8f), new(127.6f, 50.0f), new(130.8f, 48.5f), new(134.5f, 48.2f),
            new(134.0f, 45.2f), new(131.2f, 42.8f), new(128.0f, 41.5f), new(124.3f, 40.0f),
            new(121.2f, 38.9f), new(122.1f, 39.2f), new(119.5f, 40.0f), new(117.8f, 39.0f), new(118.9f, 37.8f),
            new(120.8f, 37.8f), new(122.6f, 37.4f), new(121.5f, 36.5f), new(119.6f, 35.4f),
            new(120.3f, 34.0f), new(121.0f, 32.5f), new(121.9f, 31.3f), new(121.2f, 30.6f), new(121.8f, 29.9f),
            new(121.6f, 28.8f), new(121.1f, 28.0f), new(120.5f, 27.2f),
            new(119.9f, 26.5f), new(119.7f, 25.8f), new(119.1f, 25.1f), new(118.2f, 24.5f), new(117.2f, 23.6f),
            new(116.0f, 22.9f), new(114.5f, 22.5f), new(113.5f, 22.1f), new(111.5f, 21.5f), new(110.1f, 20.3f),
            new(109.6f, 21.4f), new(108.2f, 21.5f),
            new(106.5f, 22.2f), new(103.5f, 22.6f), new(101.2f, 21.6f), new(98.8f, 24.0f), new(97.5f, 28.2f),
            new(94.0f, 29.0f), new(89.0f, 27.6f), new(84.0f, 28.8f), new(80.0f, 30.8f), new(78.2f, 33.5f),
            new(75.0f, 36.2f), new(73.8f, 39.5f), new(75.8f, 41.2f), new(80.0f, 42.5f), new(82.5f, 45.5f),
            new(86.0f, 48.5f), new(88.0f, 49.2f),
            new(92.0f, 45.2f), new(96.5f, 42.8f), new(101.5f, 42.2f), new(106.0f, 42.5f), new(111.5f, 43.8f),
            new(115.5f, 45.8f), new(117.5f, 48.0f), new(119.5f, 50.2f)
        };

        public static readonly PointF[] TaiwanOutline = new PointF[]
        {
            new(121.55f, 25.3f), new(121.95f, 25.05f), new(121.85f, 24.6f), new(121.65f, 24.1f),
            new(121.5f, 23.5f), new(121.25f, 22.9f), new(120.9f, 22.35f), new(120.85f, 21.92f),
            new(120.6f, 22.3f), new(120.2f, 22.8f), new(120.1f, 23.5f), new(120.3f, 24.1f),
            new(120.7f, 24.7f), new(121.15f, 25.1f)
        };

        public static readonly PointF[] HainanOutline = new PointF[]
        {
            new(110.2f, 20.05f), new(110.9f, 19.8f), new(110.5f, 18.8f),
            new(109.5f, 18.25f), new(108.7f, 18.9f), new(109.2f, 19.7f)
        };

        public static readonly string[] ZoneIds = { "Z1", "Z2", "Z3", "Z4" };
        public static readonly Dictionary<string, SeaZoneDef> Zones = new()
        {
            ["Z1"] = new SeaZoneDef { Id = "Z1", Name = "黃渤海", Lon = 121.2f, Lat = 38.2f, MapX = 955f, MapY = 360f, PrcWeight = 0.22 },
            ["Z2"] = new SeaZoneDef { Id = "Z2", Name = "東海",   Lon = 124.5f, Lat = 33.6f, MapX = 955f, MapY = 445f, PrcWeight = 0.28 },
            ["Z3"] = new SeaZoneDef { Id = "Z3", Name = "台海",   Lon = 119.6f, Lat = 23.0f, MapX = 955f, MapY = 530f, PrcWeight = 0.32 },
            ["Z4"] = new SeaZoneDef { Id = "Z4", Name = "南海",   Lon = 114.0f, Lat = 19.0f, MapX = 955f, MapY = 615f, PrcWeight = 0.18 },
        };

        public static readonly string[] NodeIds;
        public static readonly string[] MainlandIds;
        public static readonly string[] BorderUssrIds = { "HLJ", "JL", "LN", "IM", "XJ" };
        public static readonly Dictionary<string, NodeDef> Nodes;

        public static readonly Dictionary<string, UnitTypeDef> UnitTypes;
        public static readonly List<TechDef> TechList;
        public static readonly Dictionary<string, TechDef> Techs;
        public static readonly Dictionary<string, EndingDef> Endings;
        public static readonly Dictionary<string, MutatorDef> Mutators;

        static PointF[] P(params float[] coords)
        {
            var arr = new PointF[coords.Length / 2];
            for (int i = 0; i < arr.Length; i++) arr[i] = new PointF(coords[i * 2], coords[i * 2 + 1]);
            return arr;
        }

        static GameData()
        {
            Nodes = new Dictionary<string, NodeDef>
            {
                ["TW"] = new NodeDef { Id = "TW", Name = "台灣本島", City = "台北", Kind = NodeKind.Taiwan, Lon = 120.95f, Lat = 23.70f, CalloutLon = 125.2f, CalloutLat = 22.1f, Zones = new[] { "Z1", "Z2", "Z3", "Z4" }, Port = true, Airfield = true, Fort = 2 },
                ["PH"] = new NodeDef { Id = "PH", Name = "澎湖", City = "馬公", Kind = NodeKind.Island, Lon = 119.56f, Lat = 23.56f, CalloutLon = 121.8f, CalloutLat = 19.4f, Zones = new[] { "Z3", "Z4" }, Port = true, Airfield = true, Fort = 1 },
                ["KM"] = new NodeDef { Id = "KM", Name = "金門", City = "金城", Kind = NodeKind.Island, Lon = 118.35f, Lat = 24.45f, CalloutLon = 117.0f, CalloutLat = 20.1f, Zones = new[] { "Z3" }, Port = true, Fort = 1 },
                ["MZ"] = new NodeDef { Id = "MZ", Name = "馬祖", City = "南竿", Kind = NodeKind.Island, Lon = 119.95f, Lat = 26.16f, CalloutLon = 125.8f, CalloutLat = 25.8f, Zones = new[] { "Z2", "Z3" }, Port = true, Fort = 1 },
                ["DC"] = new NodeDef { Id = "DC", Name = "大陳島", City = "大陳", Kind = NodeKind.Island, Lon = 121.90f, Lat = 28.45f, CalloutLon = 126.5f, CalloutLat = 29.4f, Zones = new[] { "Z2" }, Port = true, Fort = 0 },
                ["ZS"] = new NodeDef { Id = "ZS", Name = "舟山群島", City = "定海", Kind = NodeKind.Island, Lon = 122.20f, Lat = 30.00f, CalloutLon = 126.2f, CalloutLat = 32.3f, Zones = new[] { "Z2" }, Port = true, Fort = 0 },
                ["HI"] = new NodeDef { Id = "HI", Name = "海南島", City = "海口", Kind = NodeKind.Island, Lon = 109.70f, Lat = 19.20f, CalloutLon = 109.70f, CalloutLat = 17.2f, Zones = new[] { "Z4" }, Port = true, Fort = 0, Industry = 1 },

            };

            var provinceSpecs = new Dictionary<string, (string[] Adj, string[] Zones, bool Port, int Industry, double Garrison, bool Major, bool Capital, bool BorderUssr)>
            {
                ["HLJ"] = (new[] { "IM", "JL" }, new[] { "Z1" }, true, 2, 2.3, false, false, true),
                ["JL"]  = (new[] { "HLJ", "IM", "LN" }, new[] { "Z1" }, true, 2, 2.3, false, false, true),
                ["LN"]  = (new[] { "HE", "IM", "JL" }, new[] { "Z1" }, true, 2, 2.4, true, false, true),
                ["IM"]  = (new[] { "GS", "HE", "HLJ", "JL", "LN", "NX", "SN", "SX" }, Array.Empty<string>(), false, 2, 4, false, false, true),
                ["XJ"]  = (new[] { "GS", "QH", "XZ" }, Array.Empty<string>(), false, 2, 4, false, false, true),
                ["XZ"]  = (new[] { "QH", "SC", "XJ", "YN" }, Array.Empty<string>(), false, 1, 2, false, false, false),
                ["QH"]  = (new[] { "GS", "SC", "XJ", "XZ" }, Array.Empty<string>(), false, 0, 0.7, false, false, false),
                ["GS"]  = (new[] { "IM", "NX", "QH", "SC", "SN", "XJ" }, Array.Empty<string>(), false, 1, 1.1, false, false, false),
                ["NX"]  = (new[] { "GS", "IM", "SN" }, Array.Empty<string>(), false, 0, 0.5, false, false, false),
                ["SN"]  = (new[] { "GS", "HA", "HB", "IM", "NX", "SC", "SX" }, Array.Empty<string>(), false, 1, 1.7, false, false, false),
                ["SX"]  = (new[] { "HA", "HE", "IM", "SN" }, Array.Empty<string>(), false, 3, 5, false, false, false),
                ["HE"]  = (new[] { "HA", "IM", "LN", "SD", "SX" }, new[] { "Z1" }, true, 3, 5, true, true, false),
                ["SD"]  = (new[] { "HA", "HE", "JS" }, new[] { "Z1" }, true, 3, 5, false, false, false),
                ["HA"]  = (new[] { "AH", "HB", "HE", "SD", "SN", "SX" }, Array.Empty<string>(), false, 1, 1.7, false, false, false),
                ["HB"]  = (new[] { "AH", "HA", "HN", "JX", "SC", "SN" }, Array.Empty<string>(), false, 1, 1.7, true, false, false),
                ["HN"]  = (new[] { "GD", "GX", "GZ", "HB", "JX", "SC" }, Array.Empty<string>(), false, 2, 1.6, false, false, false),
                ["SC"]  = (new[] { "GS", "GZ", "HB", "HN", "QH", "SN", "XZ", "YN" }, Array.Empty<string>(), false, 1, 1.5, false, false, false),
                ["GZ"]  = (new[] { "GX", "HN", "SC", "YN" }, Array.Empty<string>(), false, 1, 1.25, false, false, false),
                ["YN"]  = (new[] { "GX", "GZ", "SC", "XZ" }, Array.Empty<string>(), false, 1, 1.25, false, false, false),
                ["GX"]  = (new[] { "GD", "GZ", "HN", "YN" }, new[] { "Z4" }, true, 1, 2, false, false, false),
                ["GD"]  = (new[] { "FJ", "GX", "HN", "JX" }, new[] { "Z4" }, true, 3, 4, true, false, false),
                ["JX"]  = (new[] { "AH", "FJ", "GD", "HB", "HN", "ZJ" }, Array.Empty<string>(), false, 1, 2, false, false, false),
                ["AH"]  = (new[] { "HA", "HB", "JS", "JX", "ZJ" }, Array.Empty<string>(), false, 1, 2, false, false, false),
                ["JS"]  = (new[] { "AH", "SD", "ZJ" }, new[] { "Z2" }, true, 6, 8, true, false, false),
                ["ZJ"]  = (new[] { "AH", "FJ", "JS", "JX" }, new[] { "Z2" }, true, 3, 5, false, false, false),
                ["FJ"]  = (new[] { "GD", "JX", "ZJ" }, new[] { "Z2", "Z3" }, true, 4, 11, false, false, false),
            };

            foreach (var province in ChinaMapGeo.RealProvinces)
            {
                if (province.NodeId == "TW" || province.NodeId == "HI" || Nodes.ContainsKey(province.NodeId)) continue;
                if (!provinceSpecs.TryGetValue(province.NodeId, out var spec))
                    throw new InvalidOperationException($"Missing province configuration for {province.NodeId}.");

                bool fujian = province.NodeId == "FJ";
                Nodes.Add(province.NodeId, new NodeDef
                {
                    Id = province.NodeId,
                    Name = fujian ? "\u798f\u5efa" : province.Name,
                    City = fujian ? "\u798f\u5dde/\u53a6\u95e8" : province.Name,
                    Kind = NodeKind.Mainland,
                    Lon = fujian ? 118.3f : province.LabelLon,
                    Lat = fujian ? 26.1f : province.LabelLat,
                    Zones = spec.Zones,
                    Adj = spec.Adj,
                    Port = spec.Port,
                    Industry = spec.Industry,
                    BaseGarrison = spec.Garrison,
                    Major = spec.Major,
                    Capital = spec.Capital,
                    BorderUssr = spec.BorderUssr,
                    Poly = province.Rings.FirstOrDefault() ?? Array.Empty<PointF>()
                });
            }

            foreach (var province in ChinaMapGeo.RealProvinces)
            {
                if (Nodes.TryGetValue(province.NodeId, out var node) && province.Rings.Length > 0)
                    node.Poly = province.Rings[0];
            }
            foreach (var kv in Nodes)
            {
                if (ChinaMapGeo.NodeOuterRings.TryGetValue(kv.Key, out var rings) && rings.Length > 0)
                    kv.Value.Poly = rings[0];
            }
            NodeIds = Nodes.Keys.ToArray();
            MainlandIds = Nodes.Values.Where(n => n.Kind == NodeKind.Mainland).Select(n => n.Id).ToArray();

            UnitTypes = new Dictionary<string, UnitTypeDef>
            {
                ["inf"]   = new UnitTypeDef { Id = "inf", Name = "步兵師", Short = "步", Domain = UnitDomain.Land, Atk = 1.0, Def = 1.0, Cost = 0.15, Upkeep = 0.035, Man = 1.0, Lift = 1.0, Supply = 1.0 },
                ["mar"]   = new UnitTypeDef { Id = "mar", Name = "陸戰師", Short = "陸戰", Domain = UnitDomain.Land, Atk = 1.25, Def = 1.0, Cost = 0.25, Upkeep = 0.045, Man = 1.0, Lift = 1.0, Supply = 1.0, Marine = true, NoBuild = true },
                ["arm"]   = new UnitTypeDef { Id = "arm", Name = "裝甲突擊師", Short = "裝", Domain = UnitDomain.Land, Atk = 1.85, Def = 1.35, Cost = 0.55, Upkeep = 0.08, Man = 1.0, Lift = 1.5, Supply = 1.8, ReqTech = "armor" },
                ["abn"]   = new UnitTypeDef { Id = "abn", Name = "空降旅", Short = "空降", Domain = UnitDomain.Land, Atk = 0.85, Def = 0.85, Cost = 0.30, Upkeep = 0.045, Man = 0.5, Lift = 0.5, Supply = 0.5, ReqTech = "airborne", Airborne = true, NoBuild = true },
                ["mil"]   = new UnitTypeDef { Id = "mil", Name = "民兵/游擊", Short = "民", Domain = UnitDomain.Land, Atk = 0.55, Def = 0.65, Cost = 0, Upkeep = 0.01, Man = 0.5, Lift = 0.5, Supply = 0.3, NoBuild = true },
                ["dfc"]   = new UnitTypeDef { Id = "dfc", Name = "投誠部隊", Short = "投誠", Domain = UnitDomain.Land, Atk = 0.95, Def = 0.95, Cost = 0, Upkeep = 0.03, Man = 1.0, Lift = 1.0, Supply = 1.0, NoBuild = true },
                ["fleet"] = new UnitTypeDef { Id = "fleet", Name = "艦隊", Short = "艦", Domain = UnitDomain.Sea, Power = 10.0, Cost = 0.75, Upkeep = 0.08, Man = 0.3 },
                ["amph"]  = new UnitTypeDef { Id = "amph", Name = "兩棲船團", Short = "船", Domain = UnitDomain.Amph, LiftCap = 3.0, Cost = 0.38, Upkeep = 0.04, Man = 0.2, NoBuild = true },
                ["air"]   = new UnitTypeDef { Id = "air", Name = "航空聯隊", Short = "空", Domain = UnitDomain.Air, Power = 10.0, Cost = 0.75, Upkeep = 0.08, Man = 0.2 },
            };

            TechList = new List<TechDef>
            {
                // ==================== 1. 科技 (science) ====================
                new TechDef { Id = "land_reform", Branch = "science", Tier = 0, Name = "耕者有其田", Year = 1950, Cost = 5, Real = true, Desc = "三七五減租與公地放領奠定農工基礎。民心 +8、經濟效率 +6%", Apply = s => { s.Morale += 8; s.EconMult += 0.06; } },
                new TechDef { Id = "ntd_reform", Branch = "science", Tier = 0, Name = "新台幣幣制改革", Year = 1950, Cost = 5, Real = true, Desc = "重整央行黃金準備平抑物價。國庫 +1.2 億（隨物價換算）、民心 +5", Apply = s => { s.Treasury += 1.2 * SimEngine.PriceIndex(s); s.Morale += 5; } },
                new TechDef { Id = "import_sub", Branch = "science", Tier = 1, Name = "進口替代工業", Year = 1952, Cost = 7, Real = true, Req = new[] { "land_reform" }, Desc = "發展民生紡織與基礎輕工業。經濟效率 +8%", Apply = s => { s.EconMult += 0.08; } },
                new TechDef { Id = "us_aid_ind", Branch = "science", Tier = 1, Name = "美援電力工業計畫", Year = 1953, Cost = 7, Real = true, Req = new[] { "ntd_reform" }, Desc = "活用美援擴建發電廠與水泥紡織網。經濟效率 +7%、美國關係 +4", Apply = s => { s.EconMult += 0.07; s.UsRel += 4; } },
                new TechDef { Id = "export_zone", Branch = "science", Tier = 2, Name = "加工出口區", Year = 1965, Cost = 10, Real = true, Req = new[] { "import_sub" }, Desc = "高雄加工出口區帶動外匯騰飛。經濟效率 +10%、對日關係 +6", Apply = s => { s.EconMult += 0.10; s.JpRel += 6; } },
                new TechDef { Id = "csist", Branch = "science", Tier = 2, Name = "中山科學研究院", Year = 1969, Cost = 12, Real = true, Req = new[] { "import_sub" }, Desc = "整合國防科研人才與自製兵器基礎。研發點數 +3/年", Apply = s => { s.RpBonus += 3; } },
                new TechDef { Id = "nuclear_power", Branch = "science", Tier = 2, Name = "核能發電與重工", Year = 1971, Cost = 13, Real = true, Req = new[] { "us_aid_ind" }, Desc = "核一廠起造突破能源瓶頸，支撐重化工業起飛。經濟效率 +8%、研發點數 +2/年", Apply = s => { s.EconMult += 0.08; s.RpBonus += 2; } },
                new TechDef { Id = "uranium_procure", Branch = "science", Tier = 2, Name = "鈾料籌措與提煉", Year = 1965, Cost = 11, Real = true, Req = new[] { "export_zone" }, Desc = "自友邦引進天然鈾料並掌握重水提純，奠定自主裂變材料基礎。研發點數 +2/年", Apply = s => { s.RpBonus += 2; } },
                new TechDef { Id = "biotech_pharma", Branch = "science", Tier = 2, Name = "戰備醫藥與生醫研發", Year = 1975, Cost = 11, Real = true, Req = new[] { "export_zone" }, Desc = "戰傷防護自給自足與防疫動員體系，大幅降低部隊作戰非戰鬥減員。民心 +4", Apply = s => { s.Morale += 4; } },
                new TechDef { Id = "itri_lab", Branch = "science", Tier = 3, Name = "工業技術研究院", Year = 1973, Cost = 14, Real = true, Req = new[] { "export_zone", "csist" }, Desc = "引進積體電路移轉與應用工業研發。研發點數 +4/年、經濟效率 +5%", Apply = s => { s.RpBonus += 4; s.EconMult += 0.05; } },
                new TechDef { Id = "sci_park", Branch = "science", Tier = 3, Name = "新竹科學園區", Year = 1980, Cost = 16, Real = true, Req = new[] { "ten_projects", "csist" }, Desc = "高科技聚落成形。經濟效率 +8%、研發點數 +4/年", Apply = s => { s.EconMult += 0.08; s.RpBonus += 4; } },
                new TechDef { Id = "space_launch", Branch = "science", Tier = 3, Name = "太空探空與自主火箭", Year = 1991, Cost = 17, Real = true, Req = new[] { "csist", "sci_park" }, Desc = "探空火箭突破固態燃料與高空載具技術。全軍作戰 +6%、研發點數 +3/年", Apply = s => { s.BattleBonus += 0.06; s.RpBonus += 3; } },
                new TechDef { Id = "nuke_program", Branch = "science", Tier = 3, Name = "新竹計畫（核研）", Year = 1966, Cost = 18, Real = true, Req = new[] { "uranium_procure" }, Desc = "桃園龍潭核能研究所突破同位素提煉與臨界計算。美國關係 -8、獲得戰略核武研發資格", Apply = s => { s.UsRel -= 8; s.Flags["nukeResearchReady"] = 1; } },
                new TechDef { Id = "semicon", Branch = "science", Tier = 4, Name = "半導體產業（矽盾）", Year = 1987, Cost = 22, Real = true, Req = new[] { "sci_park", "itri_lab" }, Desc = "晶圓代工稱霸全球。經濟效率 +12%、美國關係 +10、美方防衛承諾 +12", Apply = s => { s.EconMult += 0.12; s.UsRel += 10; s.UsDefBonus += 12; } },
                new TechDef { Id = "nuke_weapon", Branch = "science", Tier = 4, Name = "核武原型", Year = 1978, Cost = 26, Real = false, Req = new[] { "nuke_program" }, Desc = "內爆型核裝置成功成形，建立核威懾力量！獲得 2 枚戰術核彈頭庫存，共軍不敢妄動；美國關係 -20、國際 -10", Apply = s => { s.UsRel -= 20; s.Intl -= 10; s.Flags["rocNuke"] = 1; s.NukeStockpile += 2; } },
                new TechDef { Id = "nuke_hbomb", Branch = "science", Tier = 4, Name = "熱核武器預研", Year = 1985, Cost = 26, Real = false, Req = new[] { "nuke_weapon" }, Desc = "多級聚變熱核構型研究，賦予中華民國不可撼動之終極嚇阻力。核彈庫存 +1 枚、全軍作戰 +6%", Apply = s => { s.BattleBonus += 0.06; s.NukeStockpile += 1; } },
                new TechDef { Id = "biotech_ai", Branch = "science", Tier = 4, Name = "先進製程與算力樞紐", Year = 2005, Cost = 24, Real = true, Req = new[] { "semicon" }, Desc = "高階晶片與自主算力奠定戰略韌性。研發點數 +5/年、經濟效率 +10%", Apply = s => { s.RpBonus += 5; s.EconMult += 0.10; } },

                // ==================== 2. 軍備 (arms) ====================
                new TechDef { Id = "us_reorg", Branch = "arms", Tier = 0, Name = "美式整編", Year = 1951, Cost = 6, Real = true, Desc = "依美軍顧問團建議整編陸軍火力與編制。陸軍科技 +0.5", Apply = s => { s.ArmyTech += 0.5; } },
                new TechDef { Id = "jets", Branch = "arms", Tier = 0, Name = "F-86 噴射機換裝", Year = 1953, Cost = 7, Real = true, Desc = "空軍進入噴射時代爭奪海峽制空權。空軍科技 +0.6", Apply = s => { s.AirTech += 0.6; } },
                new TechDef { Id = "armor", Branch = "arms", Tier = 1, Name = "裝甲兵現代化", Year = 1955, Cost = 9, Real = true, Req = new[] { "us_reorg" }, Desc = "換裝 M41/M48 戰車。解鎖建軍「裝甲突擊師」，陸軍科技 +0.3", Apply = s => { s.ArmyTech += 0.3; } },
                new TechDef { Id = "airborne", Branch = "arms", Tier = 1, Name = "空降特戰兵團", Year = 1957, Cost = 9, Real = true, Req = new[] { "us_reorg" }, Desc = "編組精銳空降旅駐防本島（可越海空降至大陸沿海），全軍作戰 +5%", Apply = s => { SimEngine.AddUnit(s, "abn", "TW", 90); s.BattleBonus += 0.05; } },
                new TechDef { Id = "amphib_doc", Branch = "arms", Tier = 1, Name = "兩棲作戰準則", Year = 1960, Cost = 10, Real = true, Req = new[] { "us_reorg" }, Desc = "國光作業室研擬聯合搶灘戰法。登陸戰力 +15%", Apply = s => { s.LandingBonus += 0.15; } },
                new TechDef { Id = "sidewinder", Branch = "arms", Tier = 1, Name = "響尾蛇飛彈", Year = 1958, Cost = 8, Real = true, Req = new[] { "jets" }, Desc = "世界首次空對空飛彈實戰壓制米格機。空軍科技 +0.4", Apply = s => { s.AirTech += 0.4; } },
                new TechDef { Id = "heavy_artillery", Branch = "arms", Tier = 1, Name = "美援八吋巨砲與重砲火網", Year = 1958, Cost = 8, Real = true, Req = new[] { "us_reorg" }, Desc = "引進 M115 八吋巨砲與重砲兵集群，建立前線壓倒性火力。陸軍科技 +0.4、防空能力 +5%", Apply = s => { s.ArmyTech += 0.4; s.AirDefense += 0.05; } },
                new TechDef { Id = "nike_sam", Branch = "arms", Tier = 2, Name = "勝利女神防空飛彈營", Year = 1959, Cost = 10, Real = true, Req = new[] { "us_reorg" }, Desc = "部署高空防空飛彈連防護台灣本島領空。本島防空能力 +18%、空軍科技 +0.2", Apply = s => { s.AirDefense += 0.18; s.AirTech += 0.2; } },
                new TechDef { Id = "f104", Branch = "arms", Tier = 2, Name = "F-104 星式戰機", Year = 1960, Cost = 11, Real = true, Req = new[] { "sidewinder" }, Desc = "兩倍音速全天候攔截機。空軍科技 +0.5", Apply = s => { s.AirTech += 0.5; } },
                new TechDef { Id = "yang_ddg", Branch = "arms", Tier = 2, Name = "陽字號武進現代化", Year = 1966, Cost = 11, Real = true, Req = new[] { "lst_fleet" }, Desc = "驅逐艦換裝現代射控與反潛聲納，強化海峽制海。海軍科技 +0.4", Apply = s => { s.NavyTech += 0.4; } },
                new TechDef { Id = "subs", Branch = "arms", Tier = 2, Name = "潛艦部隊", Year = 1971, Cost = 12, Real = true, Req = new[] { "lst_fleet" }, Desc = "海獅、海豹與劍龍級潛艦水下封鎖。海軍科技 +0.5", Apply = s => { s.NavyTech += 0.5; } },
                new TechDef { Id = "f5e", Branch = "arms", Tier = 3, Name = "中正號 F-5E 合製", Year = 1974, Cost = 13, Real = true, Req = new[] { "f104" }, Desc = "航發中心大量合製戰機維持空優。空軍科技 +0.5", Apply = s => { s.AirTech += 0.5; } },
                new TechDef { Id = "hsiung_feng", Branch = "arms", Tier = 3, Name = "雄風反艦飛彈", Year = 1978, Cost = 14, Real = true, Req = new[] { "subs" }, Desc = "海上反艦拒止利器，大幅強化水面艦打擊。海軍科技 +0.6", Apply = s => { s.NavyTech += 0.6; } },
                new TechDef { Id = "sky_sword", Branch = "arms", Tier = 3, Name = "天劍主動雷達導引飛彈", Year = 1986, Cost = 16, Real = true, Req = new[] { "f5e", "csist" }, Desc = "自製天劍一、二型空對空飛彈，具備超視距精準空戰能力。空軍科技 +0.6", Apply = s => { s.AirTech += 0.6; } },
                new TechDef { Id = "tienma", Branch = "arms", Tier = 3, Name = "天馬中程飛彈", Year = 1981, Cost = 18, Real = false, Req = new[] { "csist" }, Desc = "精準打擊大陸鐵路樞紐，共軍全境增援速度 -25%", Apply = s => { } },
                new TechDef { Id = "tienkung", Branch = "arms", Tier = 3, Name = "天弓防空飛彈", Year = 1985, Cost = 16, Real = true, Req = new[] { "csist" }, Desc = "台灣本島高空防禦網成形，本島防空能力 +30%", Apply = s => { s.AirDefense += 0.3; } },
                new TechDef { Id = "m48h", Branch = "arms", Tier = 3, Name = "M48H 勇虎戰車", Year = 1986, Cost = 18, Real = true, Req = new[] { "armor", "csist" }, Desc = "自製現代射控主力戰車。陸軍科技 +0.6", Apply = s => { s.ArmyTech += 0.6; } },
                new TechDef { Id = "tactical_nuke", Branch = "arms", Tier = 4, Name = "天馬戰術微型核彈頭", Year = 1982, Cost = 25, Real = false, Req = new[] { "tienma", "nuke_weapon" }, Desc = "核彈頭微型化整合至天馬彈道飛彈，解鎖對大陸前線防區「☢ 戰術核打擊」精準發射能力！核彈庫存 +2 枚", Apply = s => { s.NukeStockpile += 2; s.BattleBonus += 0.08; } },
                new TechDef { Id = "idf", Branch = "arms", Tier = 4, Name = "IDF 經國號戰機", Year = 1989, Cost = 20, Real = true, Req = new[] { "f5e", "csist" }, Desc = "國防自主輕型防禦戰機，突破外購封鎖。空軍科技 +0.8", Apply = s => { s.AirTech += 0.8; } },
                new TechDef { Id = "perry", Branch = "arms", Tier = 4, Name = "成功級巡防艦", Year = 1989, Cost = 20, Real = true, Req = new[] { "hsiung_feng" }, Desc = "新一代區域防空反潛主戰艦。海軍科技 +0.6", Apply = s => { s.NavyTech += 0.6; } },
                new TechDef { Id = "tuojiang_corvette", Branch = "arms", Tier = 4, Name = "沱江級雙體隱形反艦艦", Year = 2014, Cost = 22, Real = true, Req = new[] { "hsiung_feng", "c4i" }, Desc = "穿浪雙體隱形艦身搭配雄風三型超音速反艦飛彈，形成台海刺蝟殺手。海軍科技 +0.7", Apply = s => { s.NavyTech += 0.7; } },
                new TechDef { Id = "c4i", Branch = "arms", Tier = 4, Name = "三軍聯合作戰指管", Year = 1992, Cost = 22, Real = false, Req = new[] { "m48h", "sci_park" }, Desc = "三軍聯合指管通情系統 (C4I)。所有地面戰鬥 +12%", Apply = s => { s.BattleBonus += 0.12; } },
                new TechDef { Id = "patriot_pac", Branch = "arms", Tier = 4, Name = "愛國者與天弓反導網", Year = 1998, Cost = 22, Real = true, Req = new[] { "tienkung", "c4i" }, Desc = "構建多層次反彈道飛彈防禦體系抵禦共軍導彈威懾。本島防空 +35%、美軍防衛承諾 +5", Apply = s => { s.AirDefense += 0.35; s.UsDefBonus += 5; } },
                new TechDef { Id = "yunfeng_uav", Branch = "arms", Tier = 4, Name = "雲峰巡弋與無人機群", Year = 2012, Cost = 24, Real = true, Req = new[] { "tienma", "c4i" }, Desc = "長程精準源頭打擊與無人偵打蜂群壓制沿海集結點。陸軍 +0.4、空軍 +0.4、全軍作戰 +6%", Apply = s => { s.ArmyTech += 0.4; s.AirTech += 0.4; s.BattleBonus += 0.06; } },

                // ==================== 3. 情蒐 (intel) ====================
                new TechDef { Id = "recon_coast", Branch = "intel", Tier = 0, Name = "外島雷達與偵巡網", Year = 1950, Cost = 4, Real = true, Desc = "金馬外島雷達站配合機艦巡弋：情報迷霧誤差降至 ±18%，海空控場海域可透視沿海敵軍", Apply = s => { } },
                new TechDef { Id = "p2v_patrol", Branch = "intel", Tier = 0, Name = "黑蝙蝠中隊夜偵", Year = 1953, Cost = 5, Real = true, Desc = "特種電子偵察機低空滲透大陸沿海與內陸：空軍科技 +0.2、登陸作戰 +4%，迷霧進一步收斂", Apply = s => { s.AirTech += 0.2; s.LandingBonus += 0.04; } },
                new TechDef { Id = "comint", Branch = "intel", Tier = 1, Name = "特種電訊破譯中心", Year = 1954, Cost = 7, Real = true, Req = new[] { "recon_coast" }, Desc = "無線電截聽與密碼破譯：迷霧誤差降至 ±12%，『敵後突擊』行動成功率 +12%", Apply = s => { } },
                new TechDef { Id = "underground_cells", Branch = "intel", Tier = 1, Name = "大陸深層潛伏情報小組", Year = 1955, Cost = 7, Real = true, Req = new[] { "recon_coast" }, Desc = "在南京、上海、廣州建立深層潛伏敵後小組。迷霧誤差降至 ±9%，敵後情報更精確", Apply = s => { } },
                new TechDef { Id = "coastal_frogmen", Branch = "intel", Tier = 1, Name = "兩棲水文偵察蛙人隊", Year = 1957, Cost = 7, Real = true, Req = new[] { "recon_coast" }, Desc = "爆破清障與搶灘潮汐水文測繪，提升跨海作戰效率。登陸戰力 +8%", Apply = s => { s.LandingBonus += 0.08; } },
                new TechDef { Id = "black_cat", Branch = "intel", Tier = 2, Name = "黑貓中隊 U-2", Year = 1961, Cost = 9, Real = true, Req = new[] { "comint", "jets" }, Desc = "高空偵察深入大陸全境腹地，將全圖守軍情報誤差壓低至 ±6%", Apply = s => { } },
                new TechDef { Id = "hkmacau_net", Branch = "intel", Tier = 2, Name = "港澳與東南情報站", Year = 1964, Cost = 11, Real = true, Req = new[] { "comint" }, Desc = "兩廣、閩南、閩北、浙江與滬寧五省常駐精確守軍情報，研發點數 +1.5/年", Apply = s => { s.RpBonus += 1.5; } },
                new TechDef { Id = "se_asia_listen", Branch = "intel", Tier = 2, Name = "西南邊境聽音站", Year = 1966, Cost = 11, Real = true, Req = new[] { "comint", "p2v_patrol" }, Desc = "整合中南半島情報據點，常駐透視西南四省（川雲貴桂）共軍部署；對美情資合作 +3", Apply = s => { s.UsRel += 3; } },
                new TechDef { Id = "sigint_intercept", Branch = "intel", Tier = 2, Name = "陽明山極機密電監站", Year = 1968, Cost = 12, Real = true, Req = new[] { "comint" }, Desc = "截獲共軍軍區作戰密碼通訊，完全透視鄰接國軍省分之共軍調動，研發點數 +2/年", Apply = s => { s.RpBonus += 2; } },
                new TechDef { Id = "sat_elint", Branch = "intel", Tier = 3, Name = "天頻電偵預警網", Year = 1984, Cost = 17, Real = false, Req = new[] { "black_cat", "sci_park" }, Desc = "全大陸 15 省戰略敵情透明化（精確透視），所有地面作戰預警加成 +8%", Apply = s => { s.BattleBonus += 0.08; } },
                new TechDef { Id = "leishan_radar", Branch = "intel", Tier = 3, Name = "長程相控陣預警雷達", Year = 1990, Cost = 16, Real = true, Req = new[] { "hkmacau_net", "comint" }, Desc = "高空長程雷達即時掌握敵機艦集結與試射動態。本島防空 +15%、全軍作戰 +5%", Apply = s => { s.AirDefense += 0.15; s.BattleBonus += 0.05; } },
                new TechDef { Id = "formosat_recon", Branch = "intel", Tier = 4, Name = "軍民遙測衛星群", Year = 2004, Cost = 22, Real = true, Req = new[] { "sat_elint", "leishan_radar" }, Desc = "全天候軌道光電與合成孔徑雷達覆蓋大陸。全軍作戰 +8%、研發點數 +3/年", Apply = s => { s.BattleBonus += 0.08; s.RpBonus += 3; } },
                new TechDef { Id = "cyber_command", Branch = "intel", Tier = 4, Name = "資通電軍數位電子作戰", Year = 2017, Cost = 24, Real = true, Req = new[] { "formosat_recon", "c4i" }, Desc = "第四軍種網路空間作戰，癱瘓敵指管通訊並壓制沿海雷達網。全軍作戰 +8%、空軍科技 +0.3", Apply = s => { s.BattleBonus += 0.08; s.AirTech += 0.3; } },

                // ==================== 4. 策反 (subv) ====================
                new TechDef { Id = "psywar", Branch = "subv", Tier = 0, Name = "心戰大隊與廣播網", Year = 1951, Cost = 5, Real = true, Desc = "空飄傳單與對陸心理廣播：『心戰空飄』效果 ×1.5 並動搖中共中樞穩定", Apply = s => { } },
                new TechDef { Id = "kinmen_horn", Branch = "subv", Tier = 0, Name = "金門心戰廣播與氣球", Year = 1954, Cost = 5, Real = true, Desc = "前線高分貝播音站與海漂氣球持續瓦解沿海士氣，閩浙民心浮動加倍，民心 +3", Apply = s => { s.Morale += 3; } },
                new TechDef { Id = "hongkong_psy", Branch = "subv", Tier = 1, Name = "海漂與義士接應站", Year = 1956, Cost = 7, Real = true, Req = new[] { "psywar" }, Desc = "台灣民心 +4；大陸光復與進軍時感召敵軍陣前起義投誠機率顯著提高", Apply = s => { s.Morale += 4; } },
                new TechDef { Id = "defector_award", Branch = "subv", Tier = 1, Name = "義士駕機來歸重賞", Year = 1958, Cost = 7, Real = true, Req = new[] { "psywar" }, Desc = "明定黃金重賞號召共軍官兵與機艦起義投誠。空軍科技 +0.2、敵軍起義機率大增", Apply = s => { s.AirTech += 0.2; } },
                new TechDef { Id = "underground", Branch = "subv", Tier = 2, Name = "大陸地下工作網", Year = 1962, Cost = 11, Real = false, Req = new[] { "hongkong_psy" }, Desc = "重建敵後潛伏組織，解鎖『策反守軍』行動並大幅提高民兵起事率", Apply = s => { } },
                new TechDef { Id = "mainland_relief", Branch = "subv", Tier = 2, Name = "大陸空投救濟與糧票", Year = 1963, Cost = 10, Real = true, Req = new[] { "hongkong_psy", "kinmen_horn" }, Desc = "透過港澳與空投救濟糧包爭取民間向背。國際地位 +4、大陸民心浮動累積加快", Apply = s => { s.Intl += 4; } },
                new TechDef { Id = "rebel_league", Branch = "subv", Tier = 2, Name = "敵後反共義勇同盟", Year = 1965, Cost = 11, Real = false, Req = new[] { "underground" }, Desc = "整合大陸各省農村與邊疆反共義民，收復省分時自動湧現整編正規師，民心 +5", Apply = s => { s.Morale += 5; } },
                new TechDef { Id = "defector_airforce", Branch = "subv", Tier = 2, Name = "駕機投誠安全導引走廊", Year = 1967, Cost = 10, Real = true, Req = new[] { "defector_award" }, Desc = "劃設海峽投誠空層航線，共軍飛行員駕機起義機率大幅提高，空軍科技 +0.3", Apply = s => { s.AirTech += 0.3; } },
                new TechDef { Id = "united_subvert", Branch = "subv", Tier = 3, Name = "軍區統戰策反網", Year = 1968, Cost = 14, Real = false, Req = new[] { "underground" }, Desc = "深化軍區高層聯繋：『策反守軍』成功率 +20% 且不曝光，攻城倍增投誠師", Apply = s => { } },
                new TechDef { Id = "cultural_psy", Branch = "subv", Tier = 3, Name = "中華文化與短波號召", Year = 1972, Cost = 13, Real = true, Req = new[] { "mainland_relief" }, Desc = "以傳統文化保存對比對岸動亂，加深民間與幹部離心。民心 +4、持續動搖大陸穩定", Apply = s => { s.Morale += 4; } },
                new TechDef { Id = "satellite_broadcast", Branch = "subv", Tier = 3, Name = "跨洲衛視心戰廣播", Year = 1990, Cost = 16, Real = true, Req = new[] { "cultural_psy" }, Desc = "突破中共短波干擾，將自由世界影音直投大陸客廳。中共政權穩定度自然下降，國際地位 +5", Apply = s => { s.Intl += 5; s.PrcStab = Math.Max(0, s.PrcStab - 4); } },
                new TechDef { Id = "radio_liberty", Branch = "subv", Tier = 4, Name = "全頻短波心戰中心", Year = 1982, Cost = 18, Real = false, Req = new[] { "united_subvert", "sci_park" }, Desc = "每月自動瓦解大陸敵心與穩定度，且心戰空飄與策反行動經費減半", Apply = s => { } },
                new TechDef { Id = "cyber_psy", Branch = "subv", Tier = 4, Name = "數位破網與資訊戰", Year = 1995, Cost = 21, Real = false, Req = new[] { "radio_liberty" }, Desc = "跨牆網路與衛星資訊戰直透大陸都市。全軍作戰 +6%、研發點數 +2/年、對岸穩定持續下降", Apply = s => { s.BattleBonus += 0.06; s.RpBonus += 2; } },

                // ==================== 5. 後勤 (logi) ====================
                new TechDef { Id = "lst_fleet", Branch = "logi", Tier = 0, Name = "中字號登陸艦隊", Year = 1950, Cost = 6, Real = true, Desc = "擴編美製 LST 兩棲輸運艦隊。兩棲跨海總運力 +3 師", Apply = s => { s.LiftBonus += 3; } },
                new TechDef { Id = "keelung_kaohsiung", Branch = "logi", Tier = 0, Name = "基高雙港軍用深水擴建", Year = 1952, Cost = 6, Real = true, Desc = "擴建深水軍商碼頭與修船塢。海外吞吐上限 +3、兩棲總運力 +2 師", Apply = s => { s.SupplyCap += 3; s.LiftBonus += 2; } },
                new TechDef { Id = "front_depot", Branch = "logi", Tier = 1, Name = "金馬澎前進儲運庫", Year = 1954, Cost = 7, Real = true, Req = new[] { "lst_fleet" }, Desc = "設立前線戰備油彈洞庫：補給 +12、海外後勤吞吐上限 +4", Apply = s => { s.Supply += 12; s.SupplyCap += 4; } },
                new TechDef { Id = "cpc_oil_reserve", Branch = "logi", Tier = 1, Name = "戰略地下儲油洞庫", Year = 1958, Cost = 8, Real = true, Req = new[] { "front_depot" }, Desc = "開鑿苗栗與高雄戰略儲油洞庫，確保戰時燃油無虞。補給儲備 +20，維持率提升", Apply = s => { s.Supply += 20; } },
                new TechDef { Id = "cross_island_hw", Branch = "logi", Tier = 1, Name = "中橫與東部洞庫要塞", Year = 1956, Cost = 8, Real = true, Req = new[] { "keelung_kaohsiung" }, Desc = "貫通東西橫貫補給動脈與山區地下戰備庫。補給 +15、兵源 +2.5、本島防空 +10%", Apply = s => { s.Supply += 15; s.Manpower += 2.5; s.AirDefense += 0.10; } },
                new TechDef { Id = "amphib_group", Branch = "logi", Tier = 2, Name = "兩棲突擊運輸群", Year = 1962, Cost = 13, Real = false, Req = new[] { "lst_fleet", "amphib_doc" }, Desc = "大型船塢登陸運輸群。兩棲跨海總運力再 +4 師", Apply = s => { s.LiftBonus += 4; } },
                new TechDef { Id = "ten_projects", Branch = "logi", Tier = 2, Name = "十大建設・港埠運輸", Year = 1973, Cost = 15, Real = true, Req = new[] { "export_zone" }, Desc = "高速公路、台中/蘇澳港與造船骨幹。經濟效率 +8%、後勤吞吐 +4", Apply = s => { s.EconMult += 0.08; s.SupplyCap += 4; } },
                new TechDef { Id = "csc_steel", Branch = "logi", Tier = 2, Name = "中鋼特種裝甲鋼材", Year = 1974, Cost = 13, Real = true, Req = new[] { "ten_projects" }, Desc = "一貫作業大鋼廠投產，提供自製戰車、飛彈快艇與防護工事鋼材。陸軍科技 +0.3", Apply = s => { s.ArmyTech += 0.3; } },
                new TechDef { Id = "sea_shuttle", Branch = "logi", Tier = 3, Name = "高速滾裝與戰備油管", Year = 1976, Cost = 14, Real = true, Req = new[] { "front_depot", "amphib_group" }, Desc = "戰時滾裝運補與島嶼輸油網：月度補給產出 +50%、前線月補員速度翻倍、運力 +2 師", Apply = s => { s.LiftBonus += 2; } },
                new TechDef { Id = "merchant_reserve", Branch = "logi", Tier = 3, Name = "遠洋貨櫃商船動員網", Year = 1979, Cost = 14, Real = true, Req = new[] { "ten_projects", "keelung_kaohsiung" }, Desc = "徵用現代貨櫃商船隊納入戰時海運編組。跨海總運力 +5 師、海外吞吐 +4", Apply = s => { s.LiftBonus += 5; s.SupplyCap += 4; } },
                new TechDef { Id = "railway_electrification", Branch = "logi", Tier = 3, Name = "西部鐵路電氣化動員", Year = 1979, Cost = 15, Real = true, Req = new[] { "ten_projects" }, Desc = "基隆至高雄鐵路全面電氣化，24 小時內完成全島重裝備調度與前運。兵源 +2.0 師、經濟 +5%", Apply = s => { s.Manpower += 2.0; s.EconMult += 0.05; } },
                new TechDef { Id = "auto_logi", Branch = "logi", Tier = 4, Name = "自動化後勤調度網", Year = 1988, Cost = 18, Real = false, Req = new[] { "sea_shuttle", "sci_park" }, Desc = "全軍電腦化補保系統：海外吞吐 +8、跨海補給消耗 -25%、軍費維持降 12%", Apply = s => { s.SupplyCap += 8; } },
                new TechDef { Id = "high_speed_logi", Branch = "logi", Tier = 4, Name = "戰略空運與智能補保", Year = 1999, Cost = 21, Real = true, Req = new[] { "auto_logi", "merchant_reserve" }, Desc = "C-130H 空投聯運與貨櫃自動化調撥：補給產出再 +25%、軍費維持額外降 8%、運力 +3 師", Apply = s => { s.LiftBonus += 3; } },
            };
            Techs = TechList.ToDictionary(t => t.Id);

            Endings = new Dictionary<string, EndingDef>
            {
                ["full"]       = new EndingDef { Key = "full", Kind = "win", Group = "完全收復", Title = "完全收復・還我河山", Text = "國軍光復北京並肅清北疆外侮，青天白日滿地紅旗再度飄揚於神州全境。反攻大陸歷史任務圓滿達成！" },
                ["collapse"]   = new EndingDef { Key = "collapse", Kind = "win", Group = "完全收復", Title = "光復大陸・中共崩潰", Text = "中共政權在內外交迫下瓦解，各地軍區紛紛通電易幟，國府重返大陸完成統一。" },
                ["split_ussr"] = new EndingDef { Key = "split_ussr", Kind = "partial", Group = "北疆俄占", Title = "山河破碎・北疆俄占", Text = "國軍雖推翻中共、光復關內與北京，但俄國趁亂強占東北、內蒙或新疆拒不撤軍，北疆國土淪於外強之手。" },
                ["armistice"]  = new EndingDef { Key = "armistice", Kind = "partial", Group = "停止戰線", Title = "停止戰線・停戰協定", Text = "雙方在拉鋸戰中簽署國際調停之停戰協定，沿現有戰線劃設非軍事區，進入南北分治時代。" },
                ["nanbei"]     = new EndingDef { Key = "nanbei", Kind = "partial", Group = "停止戰線", Title = "停止戰線・劃江而治", Text = "國府控制大陸半壁江山，兩岸形成南北朝式的長期對峙。" },
                ["beachhead"]  = new EndingDef { Key = "beachhead", Kind = "partial", Group = "停止戰線", Title = "停止戰線・灘頭國府", Text = "國府在大陸沿海省分站穩腳跟，卻無力再向內陸推進，戰線凍結。" },
                ["warlordP"]   = new EndingDef { Key = "warlordP", Kind = "chaos", Group = "軍閥割據", Title = "軍閥割據・群雄並起", Text = "中共政權崩潰，但國府兵力不足以填補權力真空。各大軍區司令自立為王，大陸重回民初軍閥混戰。" },
                ["warlordR"]   = new EndingDef { Key = "warlordR", Kind = "chaos", Group = "軍閥割據", Title = "軍閥割據・藩鎮之禍", Text = "前線戰區將領擁兵自重、不再聽從台北號令，各據一方。國府自身陷入藩鎮內亂。" },
                ["mutiny"]     = new EndingDef { Key = "mutiny", Kind = "chaos", Group = "軍閥割據", Title = "軍閥割據・兵變", Text = "民心潰散，軍中發生兵變，各軍頭擁兵割據，國府體制瓦解。" },
                ["status"]     = new EndingDef { Key = "status", Kind = "draw", Group = "維持現狀", Title = "維持現狀・台灣奇蹟", Text = "反攻未竟，但台灣成為繁榮的科技經濟體。歷史將以另一種方式評價這一切。" },
                ["pianan"]     = new EndingDef { Key = "pianan", Kind = "lose", Group = "維持現狀", Title = "維持現狀・偏安一隅", Text = "反攻大陸終成口號，對岸國力已遙遙領先，台灣前途未卜。" },
                ["junta"]      = new EndingDef { Key = "junta", Kind = "chaos", Group = "軍閥割據", Title = "軍閥割據・強人軍政", Text = "單一軍政派系膨脹至極致且民心疲憊，憲政讓位於軍人執政，內部危機四伏。" },
                ["fortress"]   = new EndingDef { Key = "fortress", Kind = "draw", Group = "維持現狀", Title = "維持現狀・金馬鐵壁", Text = "台澎金馬七十五年寸土未失、外島構築為鋼鐵前哨，隔海維持高張力對峙均勢。" },
                ["bankrupt"]   = new EndingDef { Key = "bankrupt", Kind = "lose", Group = "反被併吞", Title = "反被併吞・財政崩潰", Text = "軍費無以為繼，惡性通膨重演，國府失去統治能力，北京趁勢接收。" },
                ["invaded"]    = new EndingDef { Key = "invaded", Kind = "lose", Group = "反被併吞", Title = "反被併吞・武力統一", Text = "解放軍登陸台灣本島，中華民國政府流亡海外。反攻大業徹底失敗。" },
                ["annexed"]    = new EndingDef { Key = "annexed", Kind = "lose", Group = "反被併吞", Title = "反被併吞・城下之盟", Text = "在國際孤立與軍事壓力下，台北被迫接受北京條件，中華民國走入歷史。" },
            };

            Mutators = new Dictionary<string, MutatorDef>
            {
                ["mut_aid_delayed"] = new MutatorDef
                {
                    Id = "mut_aid_delayed",
                    Title = "美援遲滯",
                    Desc = "1955年前華府協防與經援減半、初始外援與國際聲望下調",
                    ScoreBonus = 0.15,
                },
                ["mut_ussr_early"] = new MutatorDef
                {
                    Id = "mut_ussr_early",
                    Title = "北疆鷹派",
                    Desc = "1955年起蘇聯邊境警戒常態升溫、華北／東北出兵傾向提高",
                    ScoreBonus = 0.15,
                },
                ["mut_storm_decade"] = new MutatorDef
                {
                    Id = "mut_storm_decade",
                    Title = "狂濤年代",
                    Desc = "1950–1965年台海與東海風浪加劇，兩棲整備與跨海損耗升高",
                    ScoreBonus = 0.10,
                },
                ["mut_prc_harvest"] = new MutatorDef
                {
                    Id = "mut_prc_harvest",
                    Title = "大躍順風",
                    Desc = "1950年代初期大陸政權穩定基準提高、動員韌性更強",
                    ScoreBonus = 0.20,
                },
            };
        }
    }
}
