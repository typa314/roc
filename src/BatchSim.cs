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

        public static readonly string[] EndingArchetypePolicies =
        {
            "end_full", "end_collapse", "end_split_ussr", "end_armistice",
            "end_nanbei", "end_beachhead", "end_warlordP", "end_warlordR",
            "end_junta", "end_mutiny", "end_fortress", "end_status",
            "end_pianan", "end_invaded", "end_annexed", "end_bankrupt"
        };

        public static string ExpectedEndingForPolicy(string policy)
            => policy != null && policy.StartsWith("end_", StringComparison.Ordinal)
                ? policy.Substring(4)
                : "";

        public static Dictionary<string, SimRunResult> RunEndingCoverageSuite(uint baseSeed = 1000u)
        {
            var reached = new Dictionary<string, SimRunResult>();
            foreach (string pol in EndingArchetypePolicies)
            {
                string want = ExpectedEndingForPolicy(pol);
                SimRunResult last = null;
                for (uint off = 0; off < 6; off++)
                {
                    var res = RunSingle(pol, baseSeed + off);
                    last = res;
                    if (res.EndingKey == want)
                    {
                        reached[want] = res;
                        break;
                    }
                }
                if (!reached.ContainsKey(want) && last != null)
                    reached[want] = last;
            }
            return reached;
        }

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
                    policyArg = args[i + 1].Trim();
                else if (args[i] == "--seed" && i + 1 < args.Length && uint.TryParse(args[i + 1], out uint sd))
                    startSeed = sd;
                else if (args[i] == "--out" && i + 1 < args.Length)
                    outPath = args[i + 1];
            }

            string lowArg = policyArg.ToLowerInvariant();
            string[] policies;
            if (lowArg == "all")
            {
                policies = AllPolicies;
            }
            else if (lowArg == "endings")
            {
                policies = EndingArchetypePolicies;
            }
            else
            {
                string match = AllPolicies.Concat(EndingArchetypePolicies)
                    .FirstOrDefault(p => string.Equals(p, policyArg, StringComparison.OrdinalIgnoreCase));
                policies = match != null ? new[] { match } : AllPolicies;
            }

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
            var s = EventSystem.NewScenario("1950", seed, easyMode: false, mutators: mutators);

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

            string basePol = policy switch
            {
                "end_full" => "rush1950",
                "end_status" => "passive",
                "end_armistice" => "econ",
                _ => policy
            };

            string eid = ev.Id ?? "";

            // Archetype-specific strategic event policies (100% legal event choices)
            if (basePol.StartsWith("end_", StringComparison.Ordinal))
            {
                // Keep 1991 USSR collapse behavior aligned with ending goals
                if (eid == "ussr_fall_1991" && ev.Choices.Count >= 2)
                {
                    if (basePol == "end_split_ussr") return 0;
                    if (basePol == "end_collapse" || basePol == "end_nanbei" || basePol == "end_beachhead") return 1;
                }
                if (eid == "zhenbao_1969" && ev.Choices.Count >= 2)
                {
                    if (basePol == "end_split_ussr") return 0;
                }
                if (eid.StartsWith("dyn_armistice_offer") && ev.Choices.Count >= 2)
                {
                    return 1; // keep fighting until 2025 for non-armistice archetypes (armistice handled by econ)
                }
                if (eid == "dyn_ultimatum" && ev.Choices.Count >= 2)
                {
                    return basePol == "end_annexed" ? 1 : 0;
                }
                if (eid == "dyn_warlord_crisis" && ev.Choices.Count >= 2)
                {
                    return basePol == "end_warlordP" ? 0 : 1;
                }
                if (eid.StartsWith("dyn_roc_warlord") && ev.Choices.Count >= 2)
                {
                    return basePol == "end_warlordR" ? 0 : 1;
                }
                if (eid.StartsWith("dyn_faction_backlash_") && ev.Choices.Count >= 2)
                {
                    if (basePol == "end_warlordR") return 1;
                    if (basePol == "end_junta")
                    {
                        for (int k = 0; k < ev.Choices.Count; k++)
                            if (SimEngine.InferChoiceFaction(ev.Choices[k]) == "army") return k;
                    }
                    return 0;
                }
                if (eid.StartsWith("lib_policy_") && ev.Choices.Count >= 3)
                {
                    if (basePol == "end_warlordR") return 2; // 綏靖自治 (+warlordRisk)
                    if (basePol == "end_collapse") return 1;
                    return 0;
                }
                if (eid.StartsWith("era_dir_") && ev.Choices.Count >= 3)
                {
                    if (basePol == "end_fortress") return 1; // dir_fort_islands
                    if (basePol == "end_warlordP" || basePol == "end_collapse")
                    {
                        for (int k = 0; k < ev.Choices.Count; k++)
                            if ((ev.Choices[k].Label + ev.Choices[k].Hint).Contains("文化") || (ev.Choices[k].Label + ev.Choices[k].Hint).Contains("反共"))
                                return k;
                    }
                    if (basePol == "end_junta")
                    {
                        for (int k = 0; k < ev.Choices.Count; k++)
                            if (SimEngine.InferChoiceFaction(ev.Choices[k]) == "army") return k;
                    }
                    if (basePol == "end_warlordR")
                    {
                        // Pick least-dominant faction so no single faction crosses >= 80 into junta
                        int bestK = 0;
                        double minInf = double.MaxValue;
                        for (int k = 0; k < ev.Choices.Count; k++)
                        {
                            string facK = SimEngine.InferChoiceFaction(ev.Choices[k]);
                            double curInf = s.FactionInfluence?.GetValueOrDefault(facK, 50.0) ?? 50.0;
                            if (curInf < minInf) { minInf = curInf; bestK = k; }
                        }
                        return bestK;
                    }
                }
                if (eid == "zhoushan_1950")
                {
                    if (basePol == "end_invaded") return 2; // keep HI/ZS divisions away from TW in April 1950
                    return 0; // cleanly evacuate HI/ZS without losing islands in battle
                }
                if (eid == "mutual_treaty_1954" && basePol == "end_invaded")
                {
                    return 0; // sign treaty with restriction so breaking it later revokes US protection
                }
                if (eid == "dachen_1955" && ev.Choices.Count >= 2)
                {
                    if (basePol is "end_fortress" or "end_pianan" or "end_annexed" or "end_mutiny" or "end_bankrupt" or "end_junta")
                        return 0;
                }
                if (eid.StartsWith("ug_mature_", StringComparison.Ordinal) && ev.Choices.Count >= 2)
                {
                    if (basePol is "end_pianan" or "end_fortress" or "end_status" or "end_junta" or "end_mutiny" or "end_annexed" or "end_bankrupt" or "end_warlordP")
                        return 1; // keep underground cells covert without risking uprising capture
                }
                if (eid == "korean_war_1950" && basePol == "end_invaded")
                {
                    return 0; // avoid spawning YN guerrilla units so mainland stays 0
                }
                if ((eid.Contains("tibet") || eid.Contains("yunnan") || eid.Contains("li_mi")) && basePol == "end_invaded")
                {
                    return ev.Choices.Count - 1;
                }
            }

            if (basePol == "end_armistice" || basePol == "econ")
            {
                // handled seamlessly below as econ; end_armistice maps to econ above
            }

            int bestIdx = 0;
            double bestScore = double.NegativeInfinity;

            for (int i = 0; i < ev.Choices.Count; i++)
            {
                var c = ev.Choices[i];
                string txt = $"{c.Label} {c.Hint}";
                double sc = 0;

                if (basePol == "passive")
                {
                    if (txt.Contains("撤退") || txt.Contains("撤回") || txt.Contains("撤離") || txt.Contains("按兵") || txt.Contains("護航") || txt.Contains("旁聽")) sc += 4;
                    if (txt.Contains("經濟") || txt.Contains("GDP") || txt.Contains("工業") || txt.Contains("科研") || txt.Contains("科學園區")) sc += 3;
                    if (txt.Contains("停火") || txt.Contains("雙重代表")) sc += 2;
                    if (txt.Contains("死守") || txt.Contains("全面反攻") || txt.Contains("引狼入室") || txt.Contains("壓境")) sc -= 5;
                }
                else if (basePol == "econ")
                {
                    if (txt.Contains("GDP") || txt.Contains("經濟") || txt.Contains("科技") || txt.Contains("科研") || txt.Contains("學人") || txt.Contains("工業")) sc += 5;
                    if (txt.Contains("美國關係") || txt.Contains("美援") || txt.Contains("國際地位")) sc += 2.5;
                    if (txt.Contains("撤退") && s.Year <= 1951) sc += 3;
                    if (txt.Contains("破襲") || txt.Contains("反攻")) sc += (s.Year >= 1966 ? 2 : -2);
                    if (txt.Contains("引狼入室") || txt.Contains("城下")) sc -= 8;
                }
                else if (basePol == "rush1950")
                {
                    if (txt.Contains("反攻") || txt.Contains("搶灘") || txt.Contains("突擊") || txt.Contains("破襲") || txt.Contains("起義") || txt.Contains("增援")) sc += 5;
                    if (txt.Contains("兵源") || txt.Contains("裝甲") || txt.Contains("登陸")) sc += 3;
                    if (txt.Contains("撤退") && txt.Contains("海南")) sc += 1;
                    if (txt.Contains("停火") || txt.Contains("妥協") || txt.Contains("旁聽")) sc -= 6;
                }
                else if (basePol == "opportunist" || basePol == "end_split_ussr")
                {
                    if (txt.Contains("撤退") && txt.Contains("海南") && s.Year <= 1951) sc += 4;
                    if (txt.Contains("情報") || txt.Contains("破襲") || txt.Contains("起義") || txt.Contains("動盪")) sc += 3.5;
                    if (txt.Contains("經濟") || txt.Contains("科研") || txt.Contains("登陸") || txt.Contains("兵源")) sc += 2.5;
                    if (txt.Contains("增援") || txt.Contains("改駐")) sc += 2.0;
                    if (basePol == "end_split_ussr" && (txt.Contains("莫斯科") || txt.Contains("蘇聯") || txt.Contains("聯俄") || txt.Contains("牽制") || txt.Contains("南北夾擊") || txt.Contains("引狼入室"))) sc += 5.0;
                    if (basePol != "end_split_ussr" && txt.Contains("引狼入室")) sc -= 8;
                    if (txt.Contains("城下") || txt.Contains("接受會談")) sc -= 8;
                }
                else if (basePol == "end_junta")
                {
                    string fac = SimEngine.InferChoiceFaction(c);
                    if (fac == "army") sc += 12.0;
                    if (s.Morale < 32 && txt.Contains("民心 -")) sc -= 15.0;
                    if (s.Morale < 32 && txt.Contains("民心 +")) sc += 8.0;
                    if (txt.Contains("停火") || txt.Contains("城下") || txt.Contains("妥協")) sc -= 20.0;
                }
                else if (basePol == "end_invaded")
                {
                    if (txt.Contains("穩定 -") || txt.Contains("動盪 +") || txt.Contains("反攻") || txt.Contains("破襲") || txt.Contains("起義")) sc -= 20.0;
                    if (txt.Contains("莫斯科") || txt.Contains("蘇聯")) sc -= 15.0;
                    if (txt.Contains("停火") || txt.Contains("城下") || txt.Contains("協防")) sc -= 12.0;
                }
                else if (basePol == "end_annexed")
                {
                    if (txt.Contains("接受") || txt.Contains("妥協") || txt.Contains("旁聽") || txt.Contains("無作為")) sc += 4;
                    if (txt.Contains("國際地位 +") || txt.Contains("反攻")) sc -= 6;
                    if (txt.Contains("停火") || txt.Contains("停戰")) sc -= 15;
                }
                else if (basePol == "end_mutiny" || basePol == "end_bankrupt" || basePol == "end_pianan")
                {
                    if (txt.Contains("城下") || txt.Contains("停火") || txt.Contains("停戰")) sc -= 20;
                    if (basePol == "end_mutiny" && (txt.Contains("民心 -") || txt.Contains("徵兵") || txt.Contains("壓制"))) sc += 8;
                    if (basePol == "end_mutiny" && (txt.Contains("民心 +") || txt.Contains("士氣 +"))) sc -= 25;
                    if (basePol == "end_bankrupt" && (txt.Contains("國庫 -") || txt.Contains("舉債") || txt.Contains("軍購"))) sc += 4;
                    if (basePol == "end_pianan")
                    {
                        if (txt.Contains("國際地位 +")) sc -= 25;
                        if (txt.Contains("國際地位 -")) sc += 10;
                        if (txt.Contains("民心 +")) sc += 6;
                    }
                    if (txt.Contains("撤退") && s.Year <= 1955) sc += 3;
                }
                else if (basePol == "end_warlordP" || basePol == "end_collapse")
                {
                    if (txt.Contains("起義") || txt.Contains("心戰") || txt.Contains("地下") || txt.Contains("動盪") || txt.Contains("穩定 -") || txt.Contains("叛變")) sc += 8;
                    if (txt.Contains("國共") && txt.Contains("妥協")) sc -= 15;
                    if (txt.Contains("停火") || txt.Contains("城下") || txt.Contains("停戰")) sc -= 20;
                    if (basePol == "end_collapse" && (txt.Contains("民心 +") || txt.Contains("贖回"))) sc += 5;
                }
                else if (basePol == "end_warlordR")
                {
                    string fac = SimEngine.InferChoiceFaction(c);
                    if (s.FactionInfluence != null && s.FactionInfluence.GetValueOrDefault(fac, 50.0) >= 72.0) sc -= 8;
                    if (txt.Contains("割據") || txt.Contains("就地籌餉") || txt.Contains("自治")) sc += 10;
                    if (txt.Contains("民心 -") && s.Morale > 24) sc += 3;
                    if (txt.Contains("民心 +") && s.Morale < 16) sc += 5;
                    if (txt.Contains("停火") || txt.Contains("城下") || txt.Contains("撤退")) sc -= 15;
                }
                else // end_nanbei / end_beachhead / end_fortress / end_invaded
                {
                    if (txt.Contains("反攻") || txt.Contains("搶灘") || txt.Contains("破襲") || txt.Contains("兵源")) sc += (basePol == "end_fortress" ? -4 : 3);
                    if (basePol == "end_fortress" && (txt.Contains("守") || txt.Contains("工事") || txt.Contains("海軍") || txt.Contains("艦隊") || txt.Contains("撤退"))) sc += 4;
                    if (txt.Contains("停火") || txt.Contains("城下") || txt.Contains("旁聽")) sc -= 20;
                }

                // Prevent unintended junta monopoly (>= 80) on all non-junta ending archetypes
                if (basePol.StartsWith("end_", StringComparison.Ordinal) && basePol != "end_junta")
                {
                    string fac = SimEngine.InferChoiceFaction(c);
                    double inf = s.FactionInfluence?.GetValueOrDefault(fac, 50.0) ?? 50.0;
                    if (inf >= 62.0) sc -= 40.0;
                    else if (inf <= 35.0) sc += 5.0;
                }

                // Prefer options we can actually afford if they mention treasury
                if (txt.Contains("國庫 -") && s.Treasury < 1.0 * SimEngine.PriceIndex(s) && basePol != "end_bankrupt")
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

            if (policy == "end_invaded")
                return ApplyInvadedPolicy(s);

            SimEngine.AutoBalanceNavalAir(s);

            if (policy == "passive" || policy == "end_status")
                return 0;

            string basePol = policy switch
            {
                "end_full" => "rush1950",
                "end_armistice" => "econ",
                _ => policy
            };

            if (basePol.StartsWith("end_", StringComparison.Ordinal))
                return ApplyEndingArchetypeMonthAction(s, basePol);

            // Research management
            int maxResLv = basePol == "econ" ? 3 : 2;
            int desiredLv = s.Treasury > s.Gdp * 0.14 ? maxResLv : 1;
            if (s.ResLevel != desiredLv)
            {
                s.ResLevel = desiredLv;
                decisions++;
            }

            if ((s.ActiveTechs?.Count ?? 0) < 2)
            {
                string[] branchOrder = basePol switch
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
            string[] buildOrder = basePol == "rush1950"
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
            if (basePol == "econ" && s.Year < 1966)
                return decisions;

            if (s.Treaty && s.TreatyRestrict && s.UsConsent <= 0 && !s.HasFlag("treatyBroken") && basePol != "rush1950")
                return decisions;

            if (basePol == "opportunist" || basePol == "end_split_ussr")
            {
                if (basePol == "end_split_ussr" && s.HasFlag("sinoSovietSplit") && SimEngine.DoAction(s, "soviet", null).ok)
                    decisions++;
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
                    bool shouldAttack = basePol switch
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

        private static int ApplyInvadedPolicy(GameState s)
        {
            int dec = 0;
            s.AutoActions?.Clear();
            if (s.AutoFrontFill)
            {
                s.AutoFrontFill = false;
                dec++;
            }
            // If treaty is active and not yet broken, deploy fleets/air to Z3 once to launch a 1-unit raid to FJ and revoke US protection
            if (s.Treaty && !s.HasFlag("treatyBroken"))
            {
                foreach (var fl in s.Units.Where(u => (u.Type == "fleet" || u.Type == "air") && u.Loc != "Z3" && !u.Moved).ToList())
                    SimEngine.DeployNavalAir(s, fl.Id, "Z3");
                var raidU = SimEngine.UnitsAt(s, "KM", UnitDomain.Land).FirstOrDefault(u => !u.Moved)
                         ?? SimEngine.UnitsAt(s, "PH", UnitDomain.Land).FirstOrDefault(u => !u.Moved)
                         ?? SimEngine.UnitsAt(s, "TW", UnitDomain.Land).FirstOrDefault(u => !u.Moved);
                if (raidU != null && SimEngine.SeaSup(s, "Z3") >= 0.50 && SimEngine.IsHostileTarget(s, "FJ"))
                {
                    if (SimEngine.AttackNow(s, new[] { raidU.Id }, "FJ", manual: false).moved > 0) dec++;
                    return dec;
                }
            }
            // After TW falls, commit surviving fleets/air & remaining island garrisons into desperate counter-landings so TW stays PRC and rocWipedOut settles
            if (s.Control.GetValueOrDefault("TW") != Faction.ROC)
            {
                foreach (var fl in s.Units.Where(u => (u.Type == "fleet" || u.Type == "air") && u.Loc != "Z3" && !u.Moved).ToList())
                    if (SimEngine.DeployNavalAir(s, fl.Id, "Z3").ok) dec++;
                var islandPlan = new (string src, string dst)[]
                {
                    ("KM", "FJ"), ("PH", "FJ"), ("MZ", "FJ"), ("DC", "ZJ"), ("ZS", "JS"), ("HI", "GD")
                };
                foreach (var (src, dst) in islandPlan)
                {
                    if (s.Control.GetValueOrDefault(src) != Faction.ROC || !SimEngine.IsHostileTarget(s, dst)) continue;
                    var remUnits = SimEngine.UnitsAt(s, src, UnitDomain.Land).Where(u => !u.Moved).ToList();
                    if (remUnits.Count > 0 && SimEngine.AttackNow(s, remUnits.Select(u => u.Id), dst, manual: false).moved > 0)
                        dec++;
                }
                return dec;
            }
            // Pull fleets & air to Z1 FIRST whenever TW already empty or after evacuation
            if (SimEngine.UnitsAt(s, "TW", UnitDomain.Land).Count == 0)
            {
                foreach (var u in s.Units.Where(u => GameData.UnitTypes[u.Type].Domain != UnitDomain.Land && u.Loc != "Z1" && !u.Moved).ToList())
                {
                    var r = SimEngine.DeployNavalAir(s, u.Id, "Z1");
                    if (r.ok) dec++;
                }
            }
            // Evacuate TW land units to PH (stays ROC longer than KM) so TW remains at 0 defense
            if (s.Control["TW"] == Faction.ROC)
            {
                var twUnits = SimEngine.UnitsAt(s, "TW", UnitDomain.Land).Where(u => !u.Moved).ToList();
                string destIsl = s.Control.GetValueOrDefault("PH") == Faction.ROC ? "PH"
                               : (s.Control.GetValueOrDefault("KM") == Faction.ROC ? "KM" : null);
                if (twUnits.Count > 0 && destIsl != null)
                {
                    var r = SimEngine.MoveNow(s, twUnits.Select(u => u.Id), destIsl, manual: false);
                    if (r.moved > 0) dec++;
                }
                else if (twUnits.Count > 0 && SimEngine.IsHostileTarget(s, "FJ"))
                {
                    foreach (var fl in s.Units.Where(u => (u.Type == "fleet" || u.Type == "air") && u.Loc != "Z3" && !u.Moved).ToList())
                        SimEngine.DeployNavalAir(s, fl.Id, "Z3");
                    if (SimEngine.SeaSup(s, "Z3") >= 0.50 && SimEngine.AttackNow(s, new[] { twUnits[0].Id }, "FJ", manual: false).moved > 0)
                        dec++;
                    return dec;
                }
            }
            return dec;
        }

        private static int ApplyEndingArchetypeMonthAction(GameState s, string pol)
        {
            int dec = 0;
            s.AutoActions ??= new();

            void EnsureAuto(params string[] ids)
            {
                s.AutoActions.Clear();
                foreach (var id in ids) s.AutoActions.Add(id);
            }

            int TryTechs(string[] branches, int maxParallel = 2, int maxLv = 3)
            {
                int n = 0;
                if (s.ResLevel != maxLv && s.Treasury > s.Gdp * 0.08)
                {
                    s.ResLevel = maxLv;
                    n++;
                }
                if ((s.ActiveTechs?.Count ?? 0) < maxParallel)
                {
                    var next = branches.SelectMany(br => GameData.TechList.Where(t => t.Branch == br))
                        .FirstOrDefault(t => SimEngine.TechAvailable(s, t) && !SimEngine.IsResearching(s, t.Id)
                                             && s.Treasury >= SimEngine.TechStartCost(s, t) + SimEngine.PriceIndex(s) * 0.1);
                    if (next != null && SimEngine.ClickTechAction(s, next.Id).action == "started")
                        n++;
                }
                return n;
            }

            int TryBuild(string[] order, int cap = 38, bool forceMax = false)
            {
                double reserve = forceMax ? 0.0 : SimEngine.PriceIndex(s) * 0.5;
                if (s.Units.Count >= cap) return 0;
                foreach (var ut in order)
                {
                    if (!forceMax && ut == "fleet" && SimEngine.CountType(s, "fleet") >= 8) continue;
                    if (!forceMax && ut == "air" && SimEngine.CountType(s, "air") >= 8) continue;
                    if (s.Treasury >= SimEngine.UnitCost(s, ut) + reserve && SimEngine.CanBuild(s, ut, "TW").ok)
                    {
                        if (SimEngine.BuildUnit(s, ut, "TW").ok) return 1;
                    }
                }
                return 0;
            }

            int AttackBounded(IEnumerable<string> allowedTargets, int targetHeldMax, int minHeldGoal, bool allowUssr = false)
            {
                int heldNow = SimEngine.HeldMainland(s).Count;
                if (heldNow >= targetHeldMax) return 0;
                var allowed = new HashSet<string>(allowedTargets);
                var launches = SimEngine.HeldMainland(s).Concat(new[] { "KM", "TW", "ZS", "HI" })
                    .Distinct().Where(id => s.Control.GetValueOrDefault(id) == Faction.ROC).ToList();
                foreach (var src in launches)
                {
                    int keep = src == "TW" ? 5 : 1;
                    var idle = SimEngine.UnitsAt(s, src, UnitDomain.Land).Where(u => !u.Moved).OrderByDescending(u => u.Str).ToList();
                    if (idle.Count <= keep) continue;
                    var atk = idle.Take(Math.Min(6, idle.Count - keep)).ToList();
                    var cands = GameData.Nodes[src].Adj
                        .Concat(GameData.MainlandIds.Where(m => GameData.Nodes[m].Zones.Length > 0))
                        .Distinct()
                        .Where(d => allowed.Contains(d) && d != "HE"
                                    && (allowUssr || s.Control.GetValueOrDefault(d) == Faction.PRC)
                                    && SimEngine.IsHostileTarget(s, d)
                                    && SimEngine.CheckMove(s, atk[0], d).ok)
                        .OrderBy(d => s.Garrison[d]).ToList();
                    foreach (var d in cands)
                    {
                        var odds = SimEngine.EstimateMoveOdds(s, atk, d);
                        double needRatio = heldNow < minHeldGoal ? 1.10 : 1.55;
                        if (odds.ratio < needRatio && odds.tier != "high") continue;
                        if (SimEngine.AttackNow(s, atk.Select(u => u.Id), d, manual: false).moved > 0) return 1;
                    }
                }
                return 0;
            }

            if (pol == "end_split_ussr")
            {
                EnsureAuto("bonds", "repay", "jp_trade", "infra", "lobby_us", "farm_team", "propaganda");
                dec += TryTechs(new[] { "arms", "logi", "intel", "science" }, maxParallel: 2, maxLv: 3);
                dec += TryBuild(new[] { "arm", "inf", "fleet", "air" });
                if (s.HasFlag("sinoSovietSplit") && SimEngine.DoAction(s, "soviet", null).ok) dec++;
                // Allow capturing HE once USSR occupies at least 1 border province (guaranteeing split_ussr)
                var targetsU = GameData.MainlandIds.Where(id => !GameData.BorderUssrIds.Contains(id) && (SimEngine.UssrNodes(s).Count > 0 || id != "HE"));
                dec += AttackBounded(targetsU, 21, 14);
                return dec;
            }

            if (pol == "end_beachhead")
            {
                EnsureAuto("bonds", "jp_trade", "infra", "lobby_us", "farm_team", "propaganda");
                s.Tax = "mid";
                dec += TryTechs(new[] { "arms", "logi", "science" });
                dec += TryBuild(new[] { "inf", "arm", "fleet", "air" });
                dec += AttackBounded(new[] { "FJ", "GD", "ZJ" }, 2, 1);
                return dec;
            }

            if (pol == "end_nanbei")
            {
                EnsureAuto("bonds", "jp_trade", "infra", "lobby_us", "farm_team", "propaganda");
                s.Tax = "mid";
                dec += TryTechs(new[] { "arms", "logi", "science" });
                dec += TryBuild(new[] { "arm", "inf", "fleet", "air" });
                if (SimEngine.UssrNodes(s).Count > 0 && SimEngine.DoAction(s, "demand_ussr", null).ok) dec++;
                dec += AttackBounded(new[] { "FJ", "GD", "GX", "HN", "JX", "ZJ", "JS", "AH", "HB", "GZ", "YN", "SC" }, 7, 5);
                return dec;
            }

            if (pol == "end_collapse")
            {
                EnsureAuto("bonds", "repay", "jp_trade", "infra", "lobby_us", "farm_team", "propaganda", "psy_drop");
                s.Tax = s.Morale < 52 ? "low" : "mid";
                dec += TryTechs(new[] { "subv", "arms", "logi", "science" }, maxParallel: 3, maxLv: 3);
                if (SimEngine.DoAction(s, "psy_drop", null).ok) dec++;
                var comTgt = GameData.MainlandIds.FirstOrDefault(id => s.Control[id] == Faction.PRC && s.Garrison[id] > 1.0);
                if (comTgt != null && SimEngine.DoAction(s, "commando", comTgt).ok) dec++;
                var incTgt = GameData.MainlandIds.Where(id => s.Control[id] == Faction.PRC).OrderByDescending(id => s.Unrest[id]).FirstOrDefault();
                if (incTgt != null && SimEngine.DoAction(s, "incite", incTgt).ok) dec++;
                if (SimEngine.UssrNodes(s).Count > 0 && SimEngine.DoAction(s, "demand_ussr", null).ok) dec++;
                dec += TryBuild(new[] { "arm", "inf", "fleet", "air" });
                dec += AttackBounded(new[] { "FJ", "GD", "GX", "ZJ", "JX", "JS", "AH", "HB", "HN", "SC" }, 10, 6);
                return dec;
            }

            if (pol == "end_warlordP")
            {
                EnsureAuto("bonds", "jp_trade", "infra", "lobby_us", "farm_team", "propaganda", "psy_drop");
                s.Tax = "mid";
                dec += TryTechs(new[] { "subv", "science", "intel", "logi" }, maxParallel: 2, maxLv: 4);
                if (SimEngine.DoAction(s, "psy_drop", null).ok) dec++;
                var comTgt = GameData.MainlandIds.FirstOrDefault(id => s.Control[id] == Faction.PRC && s.Garrison[id] > 1.0);
                if (comTgt != null && SimEngine.DoAction(s, "commando", comTgt).ok) dec++;
                var incTgt = GameData.MainlandIds.Where(id => s.Control[id] == Faction.PRC).OrderByDescending(id => s.Unrest[id]).FirstOrDefault();
                if (incTgt != null && SimEngine.DoAction(s, "incite", incTgt).ok) dec++;
                dec += TryBuild(new[] { "fleet", "air", "inf" }, cap: 26);
                return dec;
            }

            if (pol == "end_warlordR")
            {
                EnsureAuto("bonds", "jp_trade", "farm_team", "repay");
                s.Tax = s.Morale < 16 ? "low" : (s.Morale > 24 ? "high" : "mid");
                if (s.Morale < 14 && SimEngine.DoAction(s, "propaganda", null).ok) dec++;
                dec += TryTechs(new[] { "arms", "logi" }, maxParallel: 1, maxLv: 1);
                dec += TryBuild(new[] { "arm", "inf", "fleet" }, cap: 32);
                dec += AttackBounded(new[] { "FJ", "GD", "ZJ" }, 3, 2);
                return dec;
            }

            if (pol == "end_junta")
            {
                EnsureAuto("bonds", "jp_trade", "farm_team", "repay");
                bool monopoly = s.FactionInfluence != null && s.FactionInfluence.Values.Any(v => v >= 80.0);
                s.Tax = monopoly ? (s.Morale < 35 ? "low" : (s.Morale > 38 ? "high" : "mid")) : "mid";
                if (monopoly && s.Morale < 33 && SimEngine.DoAction(s, "propaganda", null).ok) dec++;
                dec += TryTechs(new[] { "science", "logi" }, maxParallel: 1, maxLv: 2);
                dec += TryBuild(new[] { "fleet", "air", "inf" }, cap: 26);
                return dec;
            }

            if (pol == "end_mutiny")
            {
                EnsureAuto("bonds", "repay", "jp_trade", "farm_team");
                s.Tax = "high";
                dec += TryTechs(new[] { "science", "logi" }, maxParallel: 1, maxLv: 1);
                if (s.Morale > 1 && SimEngine.DoAction(s, "conscript", null).ok) dec++;
                return dec;
            }

            if (pol == "end_fortress")
            {
                EnsureAuto("bonds", "jp_trade", "infra", "lobby_us", "farm_team", "propaganda", "stockpile");
                s.Tax = "mid";
                dec += TryTechs(new[] { "arms", "logi", "intel", "science" }, maxParallel: 2, maxLv: 3);
                dec += TryBuild(new[] { "fleet", "air", "inf" }, cap: 34);
                foreach (var isl in new[] { "KM", "MZ", "PH" })
                {
                    if (s.Control[isl] != Faction.ROC) continue;
                    if (SimEngine.UnitsAt(s, isl, UnitDomain.Land).Count < 3)
                    {
                        var twIdle = SimEngine.UnitsAt(s, "TW", UnitDomain.Land).Where(u => !u.Moved).FirstOrDefault();
                        if (twIdle != null && SimEngine.UnitsAt(s, "TW", UnitDomain.Land).Count > 5)
                            SimEngine.MoveNow(s, new[] { twIdle.Id }, isl, manual: false);
                    }
                }
                return dec;
            }

            if (pol == "end_pianan")
            {
                EnsureAuto("bonds", "repay", "jp_trade", "propaganda");
                s.ResLevel = 1;
                s.Tax = s.Morale < 52 ? "low" : "mid";
                if (s.Morale < 48 && SimEngine.DoAction(s, "propaganda", null).ok) dec++;
                // Disable farm_team so post-1972 diplomatic erosion keeps Intl <= 40 while Morale >= 48 avoids junta/mutiny
                return dec;
            }

            if (pol == "end_annexed")
            {
                s.AutoActions.Clear();
                s.AutoActions.Add("bonds");
                s.ResLevel = 1;
                s.Tax = "mid";
                return 0;
            }

            if (pol == "end_bankrupt")
            {
                EnsureAuto("bonds");
                s.Tax = "low";
                s.ResLevel = 4;
                s.ResearchRush = true;
                if (s.Morale < 16 && SimEngine.DoAction(s, "propaganda", null).ok) dec++;
                if (s.Manpower < 4.0 && SimEngine.DoAction(s, "conscript", null).ok) dec++;
                dec += TryTechs(new[] { "arms", "science", "logi", "intel" }, maxParallel: 4, maxLv: 4);
                dec += TryBuild(new[] { "fleet", "air", "arm", "inf" }, cap: 60, forceMax: true);
                return dec;
            }

            return dec;
        }
    }
}
