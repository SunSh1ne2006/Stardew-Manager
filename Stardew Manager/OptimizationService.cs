using System;
using System.Collections.Generic;
using System.Linq;

namespace Stardew_Manager
{
    public class OptimizationService
    {
        public class PlantingEvent
        {
            public int Day { get; set; }
            public string CropName { get; set; }
            public string FertilizerName { get; set; }
            public int Quantity { get; set; }
            public int Cost { get; set; }
        }

        public class PlantedCrop
        {
            public Crop Crop { get; set; }
            public string FertilizerName { get; set; }
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
        }

        public static PlanResult CalculateBestPlan(
            List<Crop> availableCrops,
            List<IngredientItem> inventoryItems, // Запасы инвентаря из БД
            int startMoney,
            int totalPlots)
        {
            int currentMoney = startMoney;
            int totalSpent = 0;
            List<PlantedCrop> activePlots = new List<PlantedCrop>();

            // Словарь остатков удобрений в инвентаре для списания при расчете
            var fertilizerStock = inventoryItems
                .Where(item => item.AvailableQuantity > 0)
                .ToDictionary(item => item.Name, item => item.AvailableQuantity);

            var result = new PlanResult();

            for (int day = 1; day <= 28; day++)
            {
                // 1. Сбор урожая
                for (int i = activePlots.Count - 1; i >= 0; i--)
                {
                    var plot = activePlots[i];
                    plot.DaysToNextHarvest--;

                    if (plot.DaysToNextHarvest <= 0)
                    {
                        double expectedYield = plot.Crop.AvgYield;
                        int revenue = (int)Math.Floor(expectedYield * plot.Crop.SellPrice);
                        currentMoney += revenue;

                        if (!result.TotalYieldSummary.ContainsKey(plot.Crop.Name))
                            result.TotalYieldSummary[plot.Crop.Name] = 0;
                        result.TotalYieldSummary[plot.Crop.Name] += expectedYield;

                        if (plot.Crop.Regrows)
                        {
                            plot.DaysToNextHarvest = plot.Crop.RegrowTime;
                            plot.IsHarvestedOnce = true;
                        }
                        else
                        {
                            activePlots.RemoveAt(i);
                        }
                    }
                }

                // 2. Посадка
                int freePlots = totalPlots - activePlots.Count;

                if (freePlots > 0 && currentMoney > 0)
                {
                    var bestPlan = FindBestPlantingForDay(availableCrops, currentMoney, freePlots, day);

                    foreach (var kvp in bestPlan)
                    {
                        Crop cropToPlant = kvp.Key;
                        int count = kvp.Value;
                        int cost = cropToPlant.SeedPrice * count;

                        currentMoney -= cost;
                        totalSpent += cost;

                        // Назначаем доступное удобрение из запасов инвентаря
                        string usedFertilizerName = SelectBestFertilizer(fertilizerStock, count);

                        for (int c = 0; c < count; c++)
                        {
                            activePlots.Add(new PlantedCrop
                            {
                                Crop = cropToPlant,
                                FertilizerName = usedFertilizerName,
                                PlantedDay = day,
                                DaysToNextHarvest = cropToPlant.GrowthTime
                            });
                        }

                        result.Schedule.Add(new PlantingEvent
                        {
                            Day = day,
                            CropName = cropToPlant.Name,
                            FertilizerName = usedFertilizerName,
                            Quantity = count,
                            Cost = cost
                        });

                        if (!result.TotalPlantedSummary.ContainsKey(cropToPlant.Name))
                            result.TotalPlantedSummary[cropToPlant.Name] = 0;
                        result.TotalPlantedSummary[cropToPlant.Name] += count;
                    }
                }
            }

            result.FinalMoney = currentMoney;
            result.TotalSpentOnSeeds = totalSpent;
            result.TotalNetProfit = currentMoney - startMoney;

            return result;
        }

        private static string SelectBestFertilizer(Dictionary<string, int> stock, int requiredCount)
        {
            foreach (var key in stock.Keys.ToList())
            {
                if (stock[key] >= requiredCount)
                {
                    stock[key] -= requiredCount;
                    return key;
                }
            }

            return "Без удобрения";
        }

        private static Dictionary<Crop, int> FindBestPlantingForDay(List<Crop> crops, int money, int plots, int currentDay)
        {
            var validCrops = crops
                .Select(c => new { Crop = c, Profit = c.CalculateProfitForSeason(currentDay) })
                .Where(x => x.Profit > 0 && x.Crop.SeedPrice <= money)
                .ToList();

            if (!validCrops.Any() || plots <= 0 || money <= 0)
                return new Dictionary<Crop, int>();

            int[,] dp = new int[money + 1, plots + 1];
            int[,] parent = new int[money + 1, plots + 1];

            for (int w = 0; w <= money; w++)
                for (int p = 0; p <= plots; p++)
                    parent[w, p] = -1;

            for (int i = 0; i < validCrops.Count; i++)
            {
                int cost = validCrops[i].Crop.SeedPrice;
                int profit = validCrops[i].Profit;

                for (int w = cost; w <= money; w++)
                {
                    for (int p = 1; p <= plots; p++)
                    {
                        if (dp[w - cost, p - 1] + profit > dp[w, p])
                        {
                            dp[w, p] = dp[w - cost, p - 1] + profit;
                            parent[w, p] = i;
                        }
                    }
                }
            }

            int currW = money;
            int currP = plots;
            var selected = new Dictionary<Crop, int>();

            while (currW > 0 && currP > 0 && parent[currW, currP] != -1)
            {
                int cropIdx = parent[currW, currP];
                var crop = validCrops[cropIdx].Crop;

                if (selected.ContainsKey(crop))
                    selected[crop]++;
                else
                    selected[crop] = 1;

                currW -= crop.SeedPrice;
                currP -= 1;
            }

            return selected;
        }
    }
}