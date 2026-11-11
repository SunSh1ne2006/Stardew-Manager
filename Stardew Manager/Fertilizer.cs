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

        /// <summary>Цена покупки за штуку. NULL или 0 — удобрение не продаётся, только крафтится.</summary>
        public int? PurchasePrice { get; set; }

        /// <summary>
        /// Значение колонки Growth_Multiplier — доля сокращения времени до первого урожая:
        /// 0.10 — на 10% быстрее (СкороРост), 0.25 — на 25%, 0.33 — на 33%, 0 — без ускорения.
        /// Период повторного плодоношения (Regrow_Time) не меняется.
        /// </summary>
        public double GrowthMultiplier { get; set; }

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
                GrowthMultiplier = 0.0
            };
        }

        public bool IsNone
        {
            get { return Id == 0; }
        }

        /// <summary>
        /// Множитель времени до первого урожая: ускорение 0.25 → культура созревает за 75% дней.
        /// На Regrow_Time не распространяется.
        /// </summary>
        public double TimeFactor
        {
            get
            {
                double reduction = GrowthMultiplier;

                if (reduction <= 0)
                    return 1.0;

                // Защита от мусорных значений: ускорение не может быть 100% и более.
                if (reduction >= 1.0)
                    reduction = 0.99;

                return 1.0 - reduction;
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

        /// <summary>Можно ли купить удобрение в магазине (Price &gt; 0).</summary>
        public bool CanBuy
        {
            get { return PurchasePrice.HasValue && PurchasePrice.Value > 0; }
        }

        /// <summary>Есть ли рецепт крафта.</summary>
        public bool CanCraft
        {
            get { return Recipe.Count > 0; }
        }

        /// <summary>
        /// Сколько стоит применить удобрение на одну грядку по самому дешёвому способу:
        /// покупка (Price) или крафт из ингредиентов по их цене продажи.
        /// </summary>
        public int CostPerTile
        {
            get
            {
                int purchase = PurchasePrice.HasValue ? PurchasePrice.Value : 0;
                bool canBuy = purchase > 0;
                bool canCraft = Recipe.Count > 0;

                if (canBuy && canCraft)
                    return Math.Min(purchase, RecipeCost);

                if (canBuy)
                    return purchase;

                if (canCraft)
                    return RecipeCost;

                return 0;
            }
        }

        /// <summary>Как выгоднее получить удобрение — для подсказки в интерфейсе.</summary>
        public string CostSource
        {
            get
            {
                int purchase = PurchasePrice.HasValue ? PurchasePrice.Value : 0;
                bool canBuy = purchase > 0;
                bool canCraft = Recipe.Count > 0;

                if (canBuy && canCraft)
                    return purchase <= RecipeCost ? "покупка" : "крафт";

                if (canBuy)
                    return "покупка";

                if (canCraft)
                    return "крафт";

                return "бесплатно";
            }
        }

        /// <summary>Короткое человекочитаемое описание эффекта для подсказок в UI.</summary>
        public string EffectDescription
        {
            get
            {
                var parts = new List<string>();

                if (HasGrowthEffect)
                    parts.Add($"первый урожай раньше на {(1.0 - TimeFactor) * 100.0:0.#}%");

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
