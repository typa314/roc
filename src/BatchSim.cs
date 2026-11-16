using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RocGame
{
    public sealed class SimRunResult
    {
        public string Policy { get; init; } = "";
        public uint Seed { get; init; }
        public int EndYear { get; init; }
        public string EndingKey { get; init; } = "";
        public int HeldProvinces { get; init; }
        public int UssrProvinces { get; init; }
        public double Gdp { get; init; }
        public int Score { get; init; }
        public int TotalDecisions { get; init; }
        public int TotalMonths { get; init; }
        public int ZeroDecisionMonths { get; init; }
        public int ZeroSixMonthStreaks { get; init; }
        public int FirstCaptureYear { get; init; }
    }

    public static class BatchSim
    {
        public static readonly string[] AllPolicies = { "passive", "econ", "rush1950", "opportunist" };

        public static int RunCli(string[] args)
        {
            int count = 10;
            string policyArg = "all";
            uint startSeed = 1000u;
            string outPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--sim" && i + 1 < args.Length && int.TryParse(args[i + 1], out int n) && n > 0)
                    count = n;
                else if (args[i] == "--policy" && i + 1 < args.Length)
                    policyArg = args[i + 1].Trim().ToLowerInvariant();
                else if (args[i] == "--seed" && i + 1 < args.Length && uint.TryParse(args[i + 1], out uint sd))
                    startSeed = sd;
                else if (args[i] == "--out" && i + 1 < args.Length)
                    outPath = args[i + 1];
            }

            string[] policies = policyArg == "all"
                ? AllPolicies
                : (AllPolicies.Contains(policyArg) ? new[] { policyArg } : AllPolicies);

            var results = new List<SimRunResult>();
            foreach (string pol in policies)
            {
                for (int i = 0; i < count; i++)
                {
                    results.Add(RunSingle(pol, startSeed + (uint)i));
                }
            }

            string csv = BuildCsv(results);
            string summary = BuildSummary(results);

            if (!string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(outPath));
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(outPath, csv, new UTF8Encoding(false));
            }
            else
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sim_results.csv"), csv, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sim_summary.txt"), summary, new UTF8Encoding(false));
            }

            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine(summary);
                Console.Write(csv);
            }
            catch { }

            return 0;
        }

        public static string BuildCsv(IEnumerable<SimRunResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Policy,Seed,EndYear,EndingKey,HeldProvinces,Gdp,Score,TotalDecisions,ZeroDecisionMonths,ZeroSixMonthStreaks");
            foreach (var r in results)
            {
                sb.AppendLine(string.Join(",",
                    r.Policy,
                    r.Seed.ToString(CultureInfo.InvariantCulture),
                    r.EndYear.ToString(CultureInfo.InvariantCulture),
                    r.EndingKey,
                    r.HeldProvinces.ToString(CultureInfo.InvariantCulture),
                    r.Gdp.ToString("F1", CultureInfo.InvariantCulture),
                    r.Score.ToString(CultureInfo.InvariantCulture),
                    r.TotalDecisions.ToString(CultureInfo.InvariantCulture),
                    r.ZeroDecisionMonths.ToString(CultureInfo.InvariantCulture),
                    r.ZeroSixMonthStreaks.ToString(CultureInfo.InvariantCulture)));
            }
            return sb.ToString();
        }

        public static string BuildSummary(IReadOnlyList<SimRunResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== 批次戰略模擬彙總 ===");
            foreach (var grp in results.GroupBy(r => r.Policy))
            {
                int n = grp.Count();
                var endings = grp.GroupBy(r => r.EndingKey)
                    .OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key}:{g.Count()}");
                double avgHeld = grp.Average(r => r.HeldProvinces);
                double avgDec = grp.Average(r => r.TotalDecisions);
                double avgZeroMonths = grp.Average(r => r.ZeroDecisionMonths);
                double avgSixStreaks = grp.Average(r => r.ZeroSixMonthStreaks);
                double avgGdp = grp.Average(r => r.Gdp);
                sb.AppendLine($"[{grp.Key}] n={n} | 平均光復={avgHeld:F1}省 | 平均GDP=${avgGdp:F0}億 | 決策={avgDec:F1} | 零決策月={avgZeroMonths:F1} | 六月靜滯段={avgSixStreaks:F2} | 結局=({string.Join(", ", endings)})");
            }
            return sb.ToString();
        }

        public static SimRunResult RunSingle(string policy, uint seed, IReadOnlyList<string> mutators = null)
        {
            var s = EventSystem.NewScenario("1950", seed);
            if (mutators != null && mutators.Count > 0)
            {
                // Keep clean compatibility before/after H mutators are added
                foreach (var m in mutators)
                    s.Flags["mut_" + m] = 1;
            }

            int totalDecisions = 0;
            int totalMonths = 0;
            int zeroDecisionMonths = 0;
            int zeroStreak = 0;
            int zeroSixMonthStreaks = 0;
            int firstCaptureYear = 0;
            bool monthHadDecision = false;

            const int MaxDays = 30000;
            for (int dayGuard = 0; dayGuard < MaxDays && s.Over == null; dayGuard++)
            {
                while (s.PendingEvents.Count > 0 && s.Over == null)
                {
                    int idx = PickEventChoice(s, s.PendingEvents[0], policy);
                    EventSystem.ResolveChoice(s, idx);
                    totalDecisions++;
                    monthHadDecision = true;
                }
                if (s.Over != null) break;

                if (s.Day == 2)
                {
                    int acted = ApplyPolicyMonthAction(s, policy);
                    if (acted > 0)
                    {
                        totalDecisions += acted;
                        monthHadDecision = true;
                    }
                }

                int prevMonth = s.Month;
                int prevYear = s.Year;
                bool moved = SimEngine.AdvanceDay(s);

                if (firstCaptureYear == 0 && SimEngine.HeldMainland(s).Count > 0)
                    firstCaptureYear = s.Year;

                while (s.PendingEvents.Count > 0 && s.Over == null)
                {
                    int idx = PickEventChoice(s, s.PendingEvents[0], policy);
                    EventSystem.ResolveChoice(s, idx);
                    totalDecisions++;
                    monthHadDecision = true;
                }

                if (s.Month != prevMonth || s.Year != prevYear || s.Over != null)
                {
                    totalMonths++;
                    if (!monthHadDecision)
                    {
                        zeroDecisionMonths++;
                        zeroStreak++;
                        if (zeroStreak == 6) zeroSixMonthStreaks++;
                    }
                    else
                    {
                        zeroStreak = 0;
                    }
                    monthHadDecision = false;
                }

                if (!moved && s.PendingEvents.Count == 0 && s.Over == null)
                    break;
            }

            if (s.Over == null)
            {
                s.Year = GameData.EndYear + 1;
                SimEngine.CheckEnd(s, tickAnnualCounters: false);
            }

            return new SimRunResult
            {
                Policy = policy,
                Seed = seed,
                EndYear = s.Year,
                EndingKey = s.Over?.Key ?? "unknown",
                HeldProvinces = SimEngine.HeldMainland(s).Count,
                UssrProvinces = SimEngine.UssrNodes(s).Count,
                Gdp = s.Gdp,
                Score = SimEngine.Score(s),
                TotalDecisions = totalDecisions,
                TotalMonths = totalMonths,
                ZeroDecisionMonths = zeroDecisionMonths,
                ZeroSixMonthStreaks = zeroSixMonthStreaks,
                FirstCaptureYear = firstCaptureYear
            };
        }

        private static int PickEventChoice(GameState s, GameEvent ev, string policy)
        {
            if (ev?.Choices == null || ev.Choices.Count <= 1) return 0;

            int bestIdx = 0;
            double bestScore = double.NegativeInfinity;

            for (int i = 0; i < ev.Choices.Count; i++)
            {
                var c = ev.Choices[i];
                string txt = $"{c.Label} {c.Hint}";
                double sc = 0;

                if (policy == "passive")
                {
                    if (txt.Contains("撤退") || txt.Contains("撤回") || txt.Contains("撤離") || txt.Contains("按兵") || txt.Contains("護航") || txt.Contains("旁聽")) sc += 4;
                    if (txt.Contains("經濟") || txt.Contains("GDP") || txt.Contains("工業") || txt.Contains("科研") || txt.Contains("科學園區")) sc += 3;
                    if (txt.Contains("停火") || txt.Contains("雙重代表")) sc += 2;
                    if (txt.Contains("死守") || txt.Contains("全面反攻") || txt.Contains("引狼入室") || txt.Contains("壓境")) sc -= 5;
                }
                else if (policy == "econ")
                {
                    if (txt.Contains("GDP") || txt.Contains("經濟") || txt.Contains("科技") || txt.Contains("科研") || txt.Contains("學人") || txt.Contains("工業")) sc += 5;
                    if (txt.Contains("美國關係") || txt.Contains("美援") || txt.Contains("國際地位")) sc += 2.5;
                    if (txt.Contains("撤退") && s.Year <= 1951) sc += 3;
                    if (txt.Contains("破襲") || txt.Contains("反攻")) sc += (s.Year >= 1966 ? 2 : -2);
                    if (txt.Contains("引狼入室") || txt.Contains("城下")) sc -= 8;
                }
                else if (policy == "rush1950")
                {
                    if (txt.Contains("反攻") || txt.Contains("搶灘") || txt.Contains("突擊") || txt.Contains("破襲") || txt.Contains("起義") || txt.Contains("增援")) sc += 5;
                    if (txt.Contains("兵源") || txt.Contains("裝甲") || txt.Contains("登陸")) sc += 3;
                    if (txt.Contains("撤退") && txt.Contains("海南")) sc += 1;
                    if (txt.Contains("停火") || txt.Contains("妥協") || txt.Contains("旁聽")) sc -= 6;
                }
                else // opportunist
                {
                    if (txt.Contains("撤退") && txt.Contains("海南") && s.Year <= 1951) sc += 4;
                    if (txt.Contains("情報") || txt.Contains("破襲") || txt.Contains("起義") || txt.Contains("動盪")) sc += 3.5;
                    if (txt.Contains("經濟") || txt.Contains("科研") || txt.Contains("登陸") || txt.Contains("兵源")) sc += 2.5;
                    if (txt.Contains("增援") || txt.Contains("改駐")) sc += 2.0;
                    if (txt.Contains("引狼入室") || txt.Contains("城下") || txt.Contains("接受會談")) sc -= 8;
                }

                // Prefer options we can actually afford if they mention treasury
                if (txt.Contains("國庫 -") && s.Treasury < 1.0 * SimEngine.PriceIndex(s))
                    sc -= 2.5;

                if (sc > bestScore)
                {
                    bestScore = sc;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        private static int ApplyPolicyMonthAction(GameState s, string policy)
        {
            int decisions = 0;

            SimEngine.AutoBalanceNavalAir(s);

            if (policy == "passive")
                return 0;

            // Research management
            int maxResLv = policy == "econ" ? 3 : 2;
            int desiredLv = s.Treasury > s.Gdp * 0.14 ? maxResLv : 1;
            if (s.ResLevel != desiredLv)
            {
                s.ResLevel = desiredLv;
                decisions++;
            }

            if ((s.ActiveTechs?.Count ?? 0) < 2)
            {
                string[] branchOrder = policy switch
                {
                    "econ" => new[] { "science", "logi", "intel", "arms", "subv" },
                    "rush1950" => new[] { "arms", "logi", "intel", "subv", "science" },
                    _ => new[] { "intel", "arms", "logi", "subv", "science" }
                };
                var nextTech = branchOrder
                    .SelectMany(br => GameData.TechList.Where(t => t.Branch == br))
                    .FirstOrDefault(t => SimEngine.TechAvailable(s, t) && !SimEngine.IsResearching(s, t.Id)
                                         && s.Treasury >= SimEngine.TechStartCost(s, t) + SimEngine.PriceIndex(s) * 0.2);
                if (nextTech != null && SimEngine.ClickTechAction(s, nextTech.Id).action == "started")
                    decisions++;
            }

            // Unit recruitment (avoid bankrupting treasury)
            double reserve = SimEngine.PriceIndex(s) * 0.6;
            string[] buildOrder = policy == "rush1950"
                ? new[] { "arm", "inf", "fleet", "air" }
                : new[] { "fleet", "air", "arm", "inf" };
            if (s.Units.Count < 38)
            {
                foreach (var ut in buildOrder)
                {
                    if (ut == "fleet" && SimEngine.CountType(s, "fleet") >= 8) continue;
                    if (ut == "air" && SimEngine.CountType(s, "air") >= 8) continue;
                    if (s.Treasury >= SimEngine.UnitCost(s, ut) + reserve && SimEngine.CanBuild(s, ut, "TW").ok)
                    {
                        if (SimEngine.BuildUnit(s, ut, "TW").ok)
                        {
                            decisions++;
                            break;
                        }
                    }
                }
            }

            // Offensive logic
            if (policy == "econ" && s.Year < 1966)
                return decisions;

            if (s.Treaty && s.TreatyRestrict && s.UsConsent <= 0 && !s.HasFlag("treatyBroken") && policy != "rush1950")
                return decisions;

            if (policy == "opportunist")
            {
                s.PriorityTargets ??= new();
                s.PriorityTargets.Clear();
                var weakestTarget = GameData.MainlandIds
                    .Where(id => s.Control[id] != Faction.ROC && (GameData.Nodes[id].Zones.Length > 0 || GameData.Nodes[id].Adj.Any(a => s.Control[a] == Faction.ROC)))
                    .OrderBy(id => s.Garrison[id])
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(weakestTarget))
                    s.PriorityTargets.Add(weakestTarget);
            }

            // Find an offensive launch site (held mainland frontier first, then TW/KM)
            var launchNodes = SimEngine.HeldMainland(s)
                .Concat(new[] { "KM", "TW", "ZS" })
                .Distinct()
                .Where(id => s.Control.TryGetValue(id, out var f) && f == Faction.ROC)
                .ToList();

            foreach (var src in launchNodes)
            {
                int minHold = src == "TW" ? (s.Year <= 1951 && !s.UsProtect ? 7 : 4) : 1;
                var idle = SimEngine.UnitsAt(s, src, UnitDomain.Land)
                    .Where(u => !u.Moved)
                    .OrderByDescending(u => u.Str)
                    .ToList();
                if (idle.Count <= minHold) continue;

                var attackers = idle.Take(Math.Min(6, idle.Count - minHold)).ToList();
                if (attackers.Count == 0) continue;

                var targets = GameData.Nodes[src].Adj
                    .Concat(GameData.MainlandIds.Where(m => GameData.Nodes[m].Zones.Length > 0))
                    .Distinct()
                    .Where(dest => SimEngine.IsHostileTarget(s, dest)
                                   && SimEngine.CheckMove(s, attackers[0], dest).ok)
                    .OrderBy(dest => s.Garrison[dest])
                    .ToList();

                foreach (var dest in targets)
                {
                    var odds = SimEngine.EstimateMoveOdds(s, attackers, dest);
                    bool shouldAttack = policy switch
                    {
                        "rush1950" => odds.ratio >= 1.15,
                        "econ" => odds.tier == "high",
                        _ => odds.tier == "high" || (s.PrcStab < 48 && odds.ratio >= 1.65)
                    };
                    if (!shouldAttack) continue;

                    var res = SimEngine.AttackNow(s, attackers.Select(u => u.Id), dest, manual: false);
                    if (res.moved > 0)
                    {
                        decisions++;
                        return decisions;
                    }
                }
            }

            return decisions;
        }
    }
}
