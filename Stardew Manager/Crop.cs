using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Stardew_Manager
{
    public class Crop
    {
        public string Name { get; set; } = string.Empty;
        public int SeedPrice { get; set; }
        public int SellPrice { get; set; }
        public int GrowthTime { get; set; }
        public bool Regrows { get; set; }
        public int RegrowTime { get; set; }
        public int MinYield { get; set; }
        public int MaxYield { get; set; }
        public double ExtraCropChance { get; set; }
        public string ImageName { get; set; } = string.Empty;

        public Image Icon
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ImageName))
                    return null;

                string fullPath = Path.Combine(Application.StartupPath, "Images", "Crops", ImageName);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        return Image.FromFile(fullPath);
                    }
                    catch
                    {
                        return null;
                    }
                }
                return null;
            }
        }

        /// <summary>Среднее количество плодов с одного сбора.</summary>
        public double AvgYield
        {
            get
            {
                if (MinYield == MaxYield)
                    return MinYield;

                int extraAttempts = MaxYield - MinYield;
                return MinYield + (extraAttempts * ExtraCropChance);
            }
        }

        /// <summary>Сколько дней занимает рост с учётом удобрения.</summary>
        public int GetGrowthDays(Fertilizer? fertilizer)
        {
            double factor = fertilizer == null ? 1.0 : fertilizer.TimeFactor;
            return Math.Max(1, (int)Math.Ceiling(GrowthTime * factor));
        }

        /// <summary>
        /// Сколько дней до повторного урожая (0 — культура не даёт повторных урожаев).
        /// Удобрение ускоряет только созревание до первого урожая, поэтому Regrow_Time не меняется.
        /// Параметр оставлен для единообразия вызовов.
        /// </summary>
        public int GetRegrowDays(Fertilizer? fertilizer)
        {
            if (!Regrows)
                return 0;

            return Math.Max(1, RegrowTime);
        }

        /// <summary>Средняя цена продажи одного плода с учётом качества, которое даёт удобрение.</summary>
        public double GetExpectedUnitPrice(Fertilizer? fertilizer)
        {
            double qualityMultiplier = fertilizer == null ? 1.0 : fertilizer.QualityPriceMultiplier;
            return SellPrice * qualityMultiplier;
        }

        /// <summary>Стоимость посадки одной грядки: семена + удобрение.</summary>
        public int GetInvestmentPerPlot(Fertilizer? fertilizer)
        {
            int fertilizerCost = fertilizer == null ? 0 : fertilizer.CostPerTile;
            return SeedPrice + fertilizerCost;
        }

        /// <summary>Сколько урожаев успеет дать культура, посаженная в startDay, до конца сезона.</summary>
        public int GetHarvestCount(int startDay, int totalDays, Fertilizer? fertilizer)
        {
            int growthDays = GetGrowthDays(fertilizer);
            if (startDay + growthDays > totalDays)
                return 0;

            if (!Regrows)
                return 1;

            int regrowDays = GetRegrowDays(fertilizer);
            if (regrowDays <= 0)
                regrowDays = 1;

            return 1 + (totalDays - (startDay + growthDays)) / regrowDays;
        }

        /// <summary>Ожидаемая выручка с одной грядки за сезон (без вычета затрат).</summary>
        public double CalculateExpectedRevenue(int startDay, int totalDays, Fertilizer? fertilizer)
        {
            int harvests = GetHarvestCount(startDay, totalDays, fertilizer);
            if (harvests <= 0)
                return 0.0;

            return harvests * AvgYield * GetExpectedUnitPrice(fertilizer);
        }

        public int CalculateProfitForSeason(int startDay, int totalDays = 28)
        {
            return CalculateProfitForSeason(startDay, totalDays, null);
        }

        /// <summary>
        /// Чистая прибыль с одной грядки за сезон с учётом затрат на семена и удобрение.
        /// Возвращает 0, если культура не успевает дать урожай.
        /// </summary>
        public int CalculateProfitForSeason(int startDay, int totalDays, Fertilizer? fertilizer)
        {
            int harvests = GetHarvestCount(startDay, totalDays, fertilizer);
            if (harvests <= 0)
                return 0;

            double revenue = harvests * AvgYield * GetExpectedUnitPrice(fertilizer);
            return (int)Math.Floor(revenue) - GetInvestmentPerPlot(fertilizer);
        }

        public override bool Equals(object obj)
        {
            if (obj is Crop other)
                return this.Name == other.Name;
            return false;
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }
    }
}
