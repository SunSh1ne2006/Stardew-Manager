using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Stardew_Manager
{
    public class Crop
    {
        public string Name { get; set; }
        public int SeedPrice { get; set; }
        public int SellPrice { get; set; }
        public int GrowthTime { get; set; }
        public bool Regrows { get; set; }
        public int RegrowTime { get; set; }
        public int MinYield { get; set; }
        public int MaxYield { get; set; }
        public double ExtraCropChance { get; set; }
        public string ImageName { get; set; }

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

        public int CalculateProfitForSeason(int startDay, int totalDays = 28)
        {
            if (startDay + GrowthTime > totalDays)
                return 0;

            if (!Regrows)
            {
                double grossIncome = AvgYield * SellPrice;
                return (int)Math.Floor(grossIncome) - SeedPrice;
            }
            else
            {
                int harvests = 1 + (int)Math.Floor((double)(totalDays - (startDay + GrowthTime)) / RegrowTime);
                double grossIncome = harvests * AvgYield * SellPrice;
                return (int)Math.Floor(grossIncome) - SeedPrice;
            }
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