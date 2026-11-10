using System;
using System.Collections.Generic;
using System.Linq;

namespace Stardew_Manager
{
    public class OptimizationService
    {
        public const int SeasonLength = 28;

        /// <summary>Предохранитель на размер таблицы динамического программирования.</summary>
        private const long MaxKnapsackCells = 2_000_000;

        public class PlantingEvent
        {
            public int Day { get; set; }
            public string CropName { get; set; } = string.Empty;
            public string FertilizerName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public int SeedCost { get; set; }
            public int FertilizerCost { get; set; }
            public int Cost { get; set; }
        }

        public class PlantedCrop
        {
            public Crop Crop { get; set; } = new Crop();
            public Fertilizer Fertilizer { get; set; } = Fertilizer.CreateNone();
            public int PlantedDay { get; set; }
            public int DaysToNextHarvest { get; set; }
            public bool IsHarvestedOnce { get; set; }
        }

        public class PlanResult
        {
            public List<PlantingEvent> Schedule { get; set; } = new List<PlantingEvent>();
            public Dictionary<string, int> TotalPlantedSummary { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, double> TotalYieldSummary { get; set; } = new Dictionary<string, double>();
            public int FinalMoney { get; set; }
            public int TotalNetProfit { get; set; }
            public int TotalSpentOnSeeds { get; set; }
            public int TotalSpentOnFertilizer { get; set; }
        }

        /// <summary>Вариант посадки: культура + удобрение, их стоимость и прибыль с одной грядки.</summary>
        private class PlantingOption
        {
            public Crop Crop { get; set; } = new Crop();
            public Fertilizer Fertilizer { get; set; } = Fertilizer.CreateNone();
            public int Cost { get; set; }
            public int Profit { get; set; }
        }

        public class PlantingChoice
        {
            public Crop Crop { get; set; } = new Crop();
            public Fertilizer Fertilizer { get; set; } = Fertilizer.CreateNone();
            public int Quantity { get; set; }
        }

        /// <summary>
        /// Строит план посадок на сезон.
        /// fertilizers = null или пустой список — считаем без удобрений;
        /// иначе оптимизатор сам решает, какое удобрение и на какую культуру выгодно применить.
        /// </summary>
        public static PlanResult CalculateBestPlan(List<Crop> availableCrops, int startMoney, int totalPlots,
            List<Fertilizer>? fertilizers = null)
        {
            int currentMoney = startMoney;
            int totalSpentOnSeeds = 0;
            int totalSpentOnFertilizer = 0;
            List<PlantedCrop> activePlots = new List<PlantedCrop>();

            var result = new PlanResult();
            var fertilizerOptions = NormalizeFertilizers(fertilizers);

            for (int day = 1; day <= SeasonLength; day++)
            {
                for (int i = activePlots.Count - 1; i >= 0; i--)
                {
                    var plot = activePlots[i];
                    plot.DaysToNextHarvest--;

                    if (plot.DaysToNextHarvest <= 0)
                    {
                        double expectedYield = plot.Crop.AvgYield;
                        double unitPrice = plot.Crop.GetExpectedUnitPrice(plot.Fertilizer);
                        int revenue = (int)Math.Floor(expectedYield * unitPrice);
                        currentMoney += revenue;

                        if (!result.TotalYieldSummary.ContainsKey(plot.Crop.Name))
                            result.TotalYieldSummary[plot.Crop.Name] = 0;
                        result.TotalYieldSummary[plot.Crop.Name] += expectedYield;

                        if (plot.Crop.Regrows)
                        {
                            plot.DaysToNextHarvest = Math.Max(1, plot.Crop.GetRegrowDays(plot.Fertilizer));
                            plot.IsHarvestedOnce = true;
                        }
                        else
                        {
                            activePlots.RemoveAt(i);
                        }
                    }
                }

                int freePlots = totalPlots - activePlots.Count;

                if (freePlots > 0 && currentMoney > 0)
                {
                    var choices = FindBestPlantingForDay(availableCrops, fertilizerOptions, currentMoney, freePlots, day);

                    foreach (var choice in choices)
                    {
                        int seedCost = choice.Crop.SeedPrice * choice.Quantity;
                        int fertilizerCost = choice.Fertilizer.CostPerTile * choice.Quantity;

                        currentMoney -= seedCost + fertilizerCost;
                        totalSpentOnSeeds += seedCost;
                        totalSpentOnFertilizer += fertilizerCost;

                        for (int c = 0; c < choice.Quantity; c++)
                        {
                            activePlots.Add(new PlantedCrop
                            {
                                Crop = choice.Crop,
                                Fertilizer = choice.Fertilizer,
                                PlantedDay = day,
                                DaysToNextHarvest = choice.Crop.GetGrowthDays(choice.Fertilizer)
                            });
                        }

                        result.Schedule.Add(new PlantingEvent
                        {
                            Day = day,
                            CropName = choice.Crop.Name,
                            FertilizerName = choice.Fertilizer.Name,
                            Quantity = choice.Quantity,
                            SeedCost = seedCost,
                            FertilizerCost = fertilizerCost,
                            Cost = seedCost + fertilizerCost
                        });

                        if (!result.TotalPlantedSummary.ContainsKey(choice.Crop.Name))
                            result.TotalPlantedSummary[choice.Crop.Name] = 0;
                        result.TotalPlantedSummary[choice.Crop.Name] += choice.Quantity;
                    }
                }
            }

            result.FinalMoney = currentMoney;
            result.TotalSpentOnSeeds = totalSpentOnSeeds;
            result.TotalSpentOnFertilizer = totalSpentOnFertilizer;
            result.TotalNetProfit = currentMoney - startMoney;

            return result;
        }

        private static List<Fertilizer> NormalizeFertilizers(List<Fertilizer>? fertilizers)
        {
            var result = new List<Fertilizer>();

            if (fertilizers != null)
            {
                foreach (var fertilizer in fertilizers)
                {
                    if (fertilizer != null)
                        result.Add(fertilizer);
                }
            }

            if (result.Count == 0)
                result.Add(Fertilizer.CreateNone());

            return result;
        }

        /// <summary>
        /// Подбирает, что посадить на свободные грядки в текущий день.
        /// Задача о рюкзаке: бюджет (деньги) + количество грядок, прибыль с грядки зависит от дня посадки.
        /// </summary>
        private static List<PlantingChoice> FindBestPlantingForDay(List<Crop> crops, List<Fertilizer> fertilizers,
            int money, int freePlots, int currentDay)
        {
            var result = new List<PlantingChoice>();

            if (crops == null || crops.Count == 0 || freePlots <= 0 || money <= 0)
                return result;

            var options = BuildOptions(crops, fertilizers, currentDay, money);
            if (options.Count == 0)
                return result;

            int maxCost = options[options.Count - 1].Cost;

            long capacityBound = Math.Min((long)money, (long)freePlots * maxCost);
            int capacity = capacityBound > int.MaxValue ? int.MaxValue : (int)capacityBound;
            if (capacity < 0)
                capacity = 0;

            long cells = (long)(freePlots + 1) * ((long)capacity + 1);
            if (cells > MaxKnapsackCells)
                return GreedySelection(options, money, freePlots);

            var best = new int[freePlots + 1, capacity + 1];
            var choice = new int[freePlots + 1, capacity + 1];

            for (int p = 0; p <= freePlots; p++)
            {
                for (int w = 0; w <= capacity; w++)
                    choice[p, w] = -1;
            }

            for (int plots = 1; plots <= freePlots; plots++)
            {
                for (int budget = 0; budget <= capacity; budget++)
                {
                    best[plots, budget] = best[plots - 1, budget];
                    choice[plots, budget] = -1;

                    for (int i = 0; i < options.Count; i++)
                    {
                        int cost = options[i].Cost;
                        if (cost > budget)
                            continue;

                        int candidate = best[plots - 1, budget - cost] + options[i].Profit;
                        if (candidate > best[plots, budget])
                        {
                            best[plots, budget] = candidate;
                            choice[plots, budget] = i;
                        }
                    }
                }
            }

            var byKey = new Dictionary<string, PlantingChoice>();
            int remainingPlots = freePlots;
            int remainingMoney = capacity;

            while (remainingPlots > 0)
            {
                int optionIndex = choice[remainingPlots, remainingMoney];
                if (optionIndex < 0)
                {
                    remainingPlots--;
                    continue;
                }

                var option = options[optionIndex];
                string key = option.Crop.Name + "|" + option.Fertilizer.Name;

                if (!byKey.TryGetValue(key, out PlantingChoice? selected))
                {
                    selected = new PlantingChoice
                    {
                        Crop = option.Crop,
                        Fertilizer = option.Fertilizer,
                        Quantity = 0
                    };
                    byKey[key] = selected;
                    result.Add(selected);
                }

                selected.Quantity++;
                remainingMoney -= option.Cost;
                remainingPlots--;
            }

            return result;
        }

        /// <summary>
        /// Список вариантов посадки, очищенный от заведомо худших:
        /// для одинаковой стоимости остаётся самый прибыльный, доминируемые варианты отбрасываются.
        /// </summary>
        private static List<PlantingOption> BuildOptions(List<Crop> crops, List<Fertilizer> fertilizers,
            int currentDay, int budget)
        {
            var byCost = new Dictionary<int, PlantingOption>();

            foreach (var crop in crops)
            {
                foreach (var fertilizer in fertilizers)
                {
                    int cost = crop.SeedPrice + fertilizer.CostPerTile;
                    if (cost > budget)
                        continue;

                    int profit = crop.CalculateProfitForSeason(currentDay, SeasonLength, fertilizer);
                    if (profit <= 0)
                        continue;

                    if (byCost.TryGetValue(cost, out PlantingOption? existing))
                    {
                        if (existing.Profit < profit)
                        {
                            byCost[cost] = new PlantingOption
                            {
                                Crop = crop,
                                Fertilizer = fertilizer,
                                Cost = cost,
                                Profit = profit
                            };
                        }
                    }
                    else
                    {
                        byCost[cost] = new PlantingOption
                        {
                            Crop = crop,
                            Fertilizer = fertilizer,
                            Cost = cost,
                            Profit = profit
                        };
                    }
                }
            }

            var pareto = new List<PlantingOption>();
            int bestProfit = int.MinValue;

            foreach (var option in byCost.Values.OrderBy(o => o.Cost))
            {
                if (option.Profit > bestProfit)
                {
                    pareto.Add(option);
                    bestProfit = option.Profit;
                }
            }

            return pareto;
        }

        /// <summary>Запасной жадный алгоритм для очень больших бюджетов.</summary>
        private static List<PlantingChoice> GreedySelection(List<PlantingOption> options, int money, int freePlots)
        {
            var result = new List<PlantingChoice>();
            int remainingMoney = money;
            int remainingPlots = freePlots;

            foreach (var option in options.OrderByDescending(o => o.Profit))
            {
                if (remainingPlots <= 0)
                    break;

                while (remainingPlots > 0 && remainingMoney >= option.Cost)
                {
                    var selected = result.FirstOrDefault(r =>
                        r.Crop.Name == option.Crop.Name && r.Fertilizer.Name == option.Fertilizer.Name);

                    if (selected == null)
                    {
                        selected = new PlantingChoice
                        {
                            Crop = option.Crop,
                            Fertilizer = option.Fertilizer,
                            Quantity = 0
                        };
                        result.Add(selected);
                    }

                    selected.Quantity++;
                    remainingMoney -= option.Cost;
                    remainingPlots--;
                }
            }

            return result;
        }
    }
}
