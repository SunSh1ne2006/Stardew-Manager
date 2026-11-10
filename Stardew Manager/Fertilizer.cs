using System;
using System.Collections.Generic;
using System.Linq;

namespace Stardew_Manager
{
    /// <summary>
    /// Уровень качества урожая (обычное, серебряное, золотое, иридиевое).
    /// Соответствует таблице dbo.Qualities.
    /// </summary>
    public class QualityTier
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        /// <summary>Множитель цены продажи для этого качества (1.00 / 1.25 / 1.50 / 2.00).</summary>
        public double PriceMultiplier { get; set; } = 1.0;

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Шанс получить определённое качество при использовании удобрения.
    /// Соответствует строке таблицы dbo.Quality_Chances.
    /// </summary>
    public class QualityChance
    {
        public QualityTier Quality { get; set; } = new QualityTier();
        public double Chance { get; set; }
    }

    /// <summary>Ингредиент для крафта удобрений (dbo.Ingredients).</summary>
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Цена продажи — используется как альтернативная стоимость ингредиента при крафте.</summary>
        public int SellPrice { get; set; }

        /// <summary>Сколько ингредиента есть у игрока (dbo.User_Inventory).</summary>
        public int AvailableQuantity { get; set; }
    }

    /// <summary>Строка рецепта удобрения (dbo.Fertilizer_Recipes).</summary>
    public class FertilizerRecipeItem
    {
        public Ingredient Ingredient { get; set; } = new Ingredient();
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Удобрение: ускоряет рост (Growth_Multiplier) и/или повышает шансы качества (Quality_Chances).
    /// </summary>
    public class Fertilizer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Цена покупки за штуку. NULL — удобрение нельзя купить, только скрафтить.</summary>
        public int? PurchasePrice { get; set; }

        /// <summary>
        /// Значение колонки Growth_Multiplier.
        /// Трактовка: множитель времени роста. 0.90 — рост быстрее на 10%, 0.75 — на 25%.
        /// Если в базе лежит множитель скорости (&gt; 1), он автоматически переводится во время (см. TimeFactor).
        /// </summary>
        public double GrowthMultiplier { get; set; } = 1.0;

        public List<QualityChance> QualityChances { get; } = new List<QualityChance>();
        public List<FertilizerRecipeItem> Recipe { get; } = new List<FertilizerRecipeItem>();

        /// <summary>Удобрение «ничего не делать» — база для сравнения.</summary>
        public static Fertilizer CreateNone()
        {
            return new Fertilizer
            {
                Id = 0,
                Name = "Без удобрения",
                PurchasePrice = 0,
                GrowthMultiplier = 1.0
            };
        }

        public bool IsNone
        {
            get { return Id == 0; }
        }

        /// <summary>Множитель времени роста: 0.75 → культура растёт за 75% дней.</summary>
        public double TimeFactor
        {
            get
            {
                if (GrowthMultiplier <= 0)
                    return 1.0;

                // Часть схем хранит множитель скорости (1.10 = +10% скорости), а не времени.
                return GrowthMultiplier > 1.0 ? 1.0 / GrowthMultiplier : GrowthMultiplier;
            }
        }

        /// <summary>
        /// Средний множитель цены продажи с учётом шансов качества.
        /// Например 0.6 обычное + 0.3 серебро + 0.1 золото = 1.025.
        /// </summary>
        public double QualityPriceMultiplier
        {
            get
            {
                if (QualityChances.Count == 0)
                    return 1.0;

                double totalChance = 0.0;
                double weighted = 0.0;

                foreach (var chance in QualityChances)
                {
                    if (chance == null || chance.Chance <= 0)
                        continue;

                    totalChance += chance.Chance;
                    weighted += chance.Chance * chance.Quality.PriceMultiplier;
                }

                if (totalChance <= 0)
                    return 1.0;

                // Если данные неполные — остаток вероятности считаем обычным качеством.
                if (totalChance < 1.0)
                    weighted += 1.0 - totalChance;

                // Если данных больше 100% — нормализуем.
                if (totalChance > 1.0)
                    weighted /= totalChance;

                return weighted;
            }
        }

        public bool HasGrowthEffect
        {
            get { return Math.Abs(TimeFactor - 1.0) > 0.0001; }
        }

        public bool HasQualityEffect
        {
            get { return Math.Abs(QualityPriceMultiplier - 1.0) > 0.0001; }
        }

        public bool HasAnyEffect
        {
            get { return HasGrowthEffect || HasQualityEffect; }
        }

        /// <summary>Альтернативная стоимость крафта: сумма цен ингредиентов.</summary>
        public int RecipeCost
        {
            get
            {
                int sum = 0;
                foreach (var item in Recipe)
                {
                    if (item == null || item.Ingredient == null)
                        continue;
                    sum += item.Quantity * item.Ingredient.SellPrice;
                }
                return sum;
            }
        }

        /// <summary>Сколько стоит применить удобрение на одну грядку (покупка или крафт).</summary>
        public int CostPerTile
        {
            get { return PurchasePrice ?? RecipeCost; }
        }

        public bool IsCraftOnly
        {
            get { return PurchasePrice == null; }
        }

        /// <summary>Короткое человекочитаемое описание эффекта для подсказок в UI.</summary>
        public string EffectDescription
        {
            get
            {
                var parts = new List<string>();

                if (HasGrowthEffect)
                    parts.Add($"рост быстрее на {(1.0 - TimeFactor) * 100.0:0.#}%");

                if (HasQualityEffect)
                    parts.Add($"ср. цена x{QualityPriceMultiplier:0.###}");

                if (parts.Count == 0)
                    parts.Add("без эффекта");

                return string.Join(", ", parts);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
