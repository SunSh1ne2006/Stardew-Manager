using System;
using System.Collections.Generic;
using System.Linq;

namespace Stardew_Manager
{
    /// <summary>Одна строка сравнения «культура + удобрение» — основа ответа «выгодно ли покупать».</summary>
    public class FertilizerComparisonRow
    {
        public string CropName { get; set; } = string.Empty;
        public string FertilizerName { get; set; } = string.Empty;
        public int GrowthDays { get; set; }
        public int Harvests { get; set; }
        public double ExpectedUnitPrice { get; set; }
        public double RevenuePerPlot { get; set; }
        public int InvestmentPerPlot { get; set; }
        public int FertilizerCost { get; set; }
        public int ProfitPerPlot { get; set; }
        public int DeltaVsNoFertilizer { get; set; }
        public string Verdict { get; set; } = string.Empty;
        public string CostSource { get; set; } = string.Empty;
        public bool IsBaseline { get; set; }
    }

    public class FertilizerAnalysisResult
    {
        public List<FertilizerComparisonRow> Rows { get; } = new List<FertilizerComparisonRow>();
        public List<string> Recommendations { get; } = new List<string>();
        public string Summary { get; set; } = string.Empty;
        public int ProfitableCombinations { get; set; }
        public int UnprofitableCombinations { get; set; }
        public string BestCropName { get; set; } = string.Empty;
        public string BestFertilizerName { get; set; } = string.Empty;
        public int BestDeltaPerPlot { get; set; }
    }

    /// <summary>
    /// Считает, окупается ли удобрение на конкретной культуре:
    /// сравнивает прибыль с удобрением и без него, с учётом ускорения роста и качества урожая.
    /// </summary>
    public static class FertilizerAnalyzer
    {
        public static FertilizerAnalysisResult Analyze(IEnumerable<Crop> crops, IEnumerable<Fertilizer> fertilizers,
            int startDay = 1, int totalDays = OptimizationService.SeasonLength)
        {
            var result = new FertilizerAnalysisResult();
            if (crops == null)
                return result;

            var fertilizerList = fertilizers == null
                ? new List<Fertilizer>()
                : fertilizers.Where(f => f != null).ToList();

            // База сравнения: удобрение без затрат и без ускорения роста (обычно «Без удобрения»).
            var none = fertilizerList.FirstOrDefault(f => f.CostPerTile == 0 && !f.HasGrowthEffect)
                       ?? Fertilizer.CreateNone();

            var recommendations = new List<Recommendation>();

            foreach (var crop in crops)
            {
                int baselineProfit = crop.CalculateProfitForSeason(startDay, totalDays, none);
                result.Rows.Add(BuildRow(crop, none, startDay, totalDays, baselineProfit, 0, true));

                Fertilizer? bestFertilizer = null;
                int bestDelta = 0;

                foreach (var fertilizer in fertilizerList)
                {
                    if (fertilizer == none || !fertilizer.HasAnyEffect)
                        continue;

                    int profit = crop.CalculateProfitForSeason(startDay, totalDays, fertilizer);
                    int delta = profit - baselineProfit;

                    result.Rows.Add(BuildRow(crop, fertilizer, startDay, totalDays, profit, delta, false));

                    if (delta > 0)
                        result.ProfitableCombinations++;
                    else
                        result.UnprofitableCombinations++;

                    if (delta > bestDelta)
                    {
                        bestDelta = delta;
                        bestFertilizer = fertilizer;
                    }
                }

                if (bestFertilizer != null)
                {
                    recommendations.Add(new Recommendation
                    {
                        Delta = bestDelta,
                        CropName = crop.Name,
                        FertilizerName = bestFertilizer.Name,
                        Percent = baselineProfit > 0 ? (double)bestDelta / baselineProfit * 100.0 : 0.0
                    });
                }
            }

            foreach (var item in recommendations.OrderByDescending(r => r.Delta))
            {
                string suffix = item.Percent > 0
                    ? string.Format(" (+{0:0.#}% к прибыли)", item.Percent)
                    : string.Empty;

                result.Recommendations.Add(string.Format("{0}: {1} → +{2} g с грядки{3}",
                    item.CropName, item.FertilizerName, item.Delta, suffix));
            }

            var best = recommendations.OrderByDescending(r => r.Delta).FirstOrDefault();
            if (best != null)
            {
                result.BestCropName = best.CropName;
                result.BestFertilizerName = best.FertilizerName;
                result.BestDeltaPerPlot = best.Delta;
            }

            result.Summary = string.Format("Выгодных сочетаний: {0}, невыгодных: {1}",
                result.ProfitableCombinations, result.UnprofitableCombinations);

            return result;
        }

        /// <summary>Сколько денег принесёт выбранное удобрение на заданном количестве грядок.</summary>
        public static int CalculateTotalDelta(FertilizerAnalysisResult analysis, int plots)
        {
            if (analysis == null || plots <= 0)
                return 0;

            return analysis.BestDeltaPerPlot * plots;
        }

        private static FertilizerComparisonRow BuildRow(Crop crop, Fertilizer fertilizer, int startDay, int totalDays,
            int profit, int delta, bool baseline)
        {
            string verdict;
            if (baseline)
                verdict = "—";
            else if (delta > 0)
                verdict = "Выгодно";
            else if (delta < 0)
                verdict = "Невыгодно";
            else
                verdict = "Без изменений";

            return new FertilizerComparisonRow
            {
                CropName = crop.Name,
                FertilizerName = fertilizer.Name,
                GrowthDays = crop.GetGrowthDays(fertilizer),
                Harvests = crop.GetHarvestCount(startDay, totalDays, fertilizer),
                ExpectedUnitPrice = crop.GetExpectedUnitPrice(fertilizer),
                RevenuePerPlot = crop.CalculateExpectedRevenue(startDay, totalDays, fertilizer),
                InvestmentPerPlot = crop.GetInvestmentPerPlot(fertilizer),
                FertilizerCost = fertilizer.CostPerTile,
                ProfitPerPlot = profit,
                DeltaVsNoFertilizer = delta,
                Verdict = verdict,
                CostSource = fertilizer.CostSource,
                IsBaseline = baseline
            };
        }

        private class Recommendation
        {
            public int Delta { get; set; }
            public string CropName { get; set; } = string.Empty;
            public string FertilizerName { get; set; } = string.Empty;
            public double Percent { get; set; }
        }
    }
}
