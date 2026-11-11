using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stardew_Manager
{
    public partial class Form1 : Form
    {
        private DataGridView Data;
        private TextBox QuantityTextBox;
        private TextBox MoneyQuantityTextBox;
        private Button CalculateButton;
        private Button CompareFertilizersButton;
        private Label ResultSummaryLabel;
        private Label FertilizerSummaryLabel;
        private ComboBox SeasonComboBox;
        private ComboBox FertilizerComboBox;
        private Label SeasonLabel;

        private List<Crop> _availableCrops = new List<Crop>();
        private List<Fertilizer> _fertilizers = new List<Fertilizer>();
        private readonly DatabaseService _dbService;

        public Form1()
        {
            InitializeCustomCard();

            _dbService = new DatabaseService(ConnectionStringProvider.Get());
            this.Load += OnFormLoad;
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                var initializer = new DatabaseInitializer(ConnectionStringProvider.Get());
                await initializer.InitializeDatabaseAsync();

                await LoadFertilizersAsync();
                await LoadSeasonsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}", "Ошибка БД",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadFertilizersAsync()
        {
            try
            {
                _fertilizers = await _dbService.GetFertilizersAsync();
            }
            catch (Exception ex)
            {
                // Даже если таблицы удобрений недоступны, приложение должно работать.
                _fertilizers = new List<Fertilizer>();
                MessageBox.Show($"Не удалось загрузить удобрения: {ex.Message}", "Удобрения",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Если в базе нет «пустого» удобрения, добавляем его сами — иначе не с чем сравнивать.
            if (!_fertilizers.Any(IsNeutralFertilizer))
                _fertilizers.Insert(0, Fertilizer.CreateNone());

            FertilizerComboBox.Items.Clear();
            FertilizerComboBox.Items.Add(new Fertilizer
            {
                Id = -1,
                Name = "Авто (подбор)",
                PurchasePrice = 0,
                GrowthMultiplier = 1.0
            });

            foreach (var fertilizer in _fertilizers)
                FertilizerComboBox.Items.Add(fertilizer);

            FertilizerComboBox.SelectedIndex = 0;
            CompareFertilizersButton.Enabled = true;
        }

        private async Task LoadSeasonsAsync()
        {
            try
            {
                SeasonComboBox.Enabled = false;

                var seasons = await _dbService.GetSeasonsAsync();

                SeasonComboBox.SelectedIndexChanged -= OnSeasonChanged;
                SeasonComboBox.Items.Clear();

                foreach (var season in seasons)
                {
                    SeasonComboBox.Items.Add(season);
                }

                if (SeasonComboBox.Items.Count > 0)
                {
                    SeasonComboBox.SelectedIndex = 0;

                    await LoadDataForSelectedSeasonAsync(SeasonComboBox.SelectedItem.ToString());
                }

                SeasonComboBox.SelectedIndexChanged += OnSeasonChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке сезонов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SeasonComboBox.Enabled = true;
            }
        }

        private async void OnSeasonChanged(object sender, EventArgs e)
        {
            if (SeasonComboBox.SelectedItem != null)
            {
                await LoadDataForSelectedSeasonAsync(SeasonComboBox.SelectedItem.ToString());
            }
        }

        private async Task LoadDataForSelectedSeasonAsync(string selectedSeason)
        {
            try
            {
                CalculateButton.Enabled = false;
                _availableCrops = await _dbService.GetCropsBySeasonAsync(selectedSeason);
                DisplayInitialCrops();
                FertilizerSummaryLabel.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке культур: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CalculateButton.Enabled = true;
            }
        }

        private void DisplayInitialCrops()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Иконка", typeof(Image));
            table.Columns.Add("Название культуры", typeof(string));

            foreach (var crop in _availableCrops)
            {
                table.Rows.Add(crop.Icon, crop.Name);
            }

            Data.DataSource = table;
            ConfigureGridImageColumn();
        }

        /// <summary>«Пустое» удобрение: бесплатное и без ускорения роста — база для сравнения.</summary>
        private static bool IsNeutralFertilizer(Fertilizer fertilizer)
        {
            return fertilizer.CostPerTile == 0 && !fertilizer.HasGrowthEffect;
        }

        private Fertilizer GetNeutralFertilizer()
        {
            var neutral = _fertilizers.FirstOrDefault(IsNeutralFertilizer);
            return neutral ?? Fertilizer.CreateNone();
        }

        /// <summary>
        /// Какие удобрения учитывать в плане: «авто» — все, конкретное — выбранное
        /// (планировщик применит его там, где это выгодно, иначе оставит грядку без удобрения).
        /// </summary>
        private List<Fertilizer> GetFertilizersForPlan()
        {
            var selected = FertilizerComboBox.SelectedItem as Fertilizer;

            if (selected == null || selected.Id == -1)
            {
                var list = new List<Fertilizer>(_fertilizers);
                if (!list.Any(IsNeutralFertilizer))
                    list.Add(Fertilizer.CreateNone());

                return list;
            }

            if (IsNeutralFertilizer(selected))
                return new List<Fertilizer> { selected };

            return new List<Fertilizer> { selected, GetNeutralFertilizer() };
        }

        private void OnCalculateButtonClick(object sender, EventArgs e)
        {
            if (!int.TryParse(QuantityTextBox.Text, out int plots) || plots <= 0)
            {
                MessageBox.Show("Введите корректное количество грядок!", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(MoneyQuantityTextBox.Text, out int money) || money <= 0)
            {
                MessageBox.Show("Введите корректное количество денег!", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_availableCrops.Count == 0)
            {
                MessageBox.Show("Для выбранного сезона нет культур в базе.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var fertilizers = GetFertilizersForPlan();
            var result = OptimizationService.CalculateBestPlan(_availableCrops, money, plots, fertilizers);

            DataTable scheduleTable = new DataTable();
            scheduleTable.Columns.Add("День сезона", typeof(string));
            scheduleTable.Columns.Add("Иконка", typeof(Image));
            scheduleTable.Columns.Add("Культура", typeof(string));
            scheduleTable.Columns.Add("Удобрение", typeof(string));
            scheduleTable.Columns.Add("Количество (шт)", typeof(int));
            scheduleTable.Columns.Add("Семена (g)", typeof(int));
            scheduleTable.Columns.Add("Удобрение (g)", typeof(int));
            scheduleTable.Columns.Add("Затраты (g)", typeof(int));

            foreach (var planting in result.Schedule)
            {
                var cropObj = _availableCrops.Find(c => c.Name == planting.CropName);
                Image icon = cropObj?.Icon;

                scheduleTable.Rows.Add($"День {planting.Day}", icon, planting.CropName,
                    planting.FertilizerName, planting.Quantity, planting.SeedCost,
                    planting.FertilizerCost, planting.Cost);
            }

            Data.DataSource = scheduleTable;
            ConfigureGridImageColumn();

            string summary = $"Семена: {result.TotalSpentOnSeeds}g | Удобрения: {result.TotalSpentOnFertilizer}g | " +
                             $"Баланс в конце: {result.FinalMoney}g | Чистая прибыль: {result.TotalNetProfit}g";

            var chosen = FertilizerComboBox.SelectedItem as Fertilizer;
            bool autoMode = chosen == null || chosen.Id == -1;

            var withoutFertilizer = OptimizationService.CalculateBestPlan(_availableCrops, money, plots,
                new List<Fertilizer> { GetNeutralFertilizer() });
            int delta = result.TotalNetProfit - withoutFertilizer.TotalNetProfit;
            string sign = delta >= 0 ? "+" : string.Empty;

            summary += $"\r\nПлан без удобрений: {withoutFertilizer.TotalNetProfit}g → " +
                       $"удобрения дают {sign}{delta}g";

            if (!autoMode && chosen != null && !IsNeutralFertilizer(chosen) && result.TotalSpentOnFertilizer == 0)
                summary += " (выбранное удобрение не окупается — план построен без него)";

            ResultSummaryLabel.Text = summary;
            FertilizerSummaryLabel.Text = string.Empty;
        }

        private void OnCompareFertilizersClick(object sender, EventArgs e)
        {
            if (_availableCrops.Count == 0)
            {
                MessageBox.Show("Сначала выберите сезон: культуры ещё не загружены.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var fertilizers = _fertilizers.Where(f => f.HasAnyEffect).ToList();
            if (fertilizers.Count == 0)
            {
                MessageBox.Show("В базе нет удобрений с эффектом (таблица Fertilizers пуста или недоступна).",
                    "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var analysis = FertilizerAnalyzer.Analyze(_availableCrops, fertilizers);

            DataTable table = new DataTable();
            table.Columns.Add("Культура", typeof(string));
            table.Columns.Add("Удобрение", typeof(string));
            table.Columns.Add("Дней до урожая", typeof(int));
            table.Columns.Add("Урожаев за сезон", typeof(int));
            table.Columns.Add("Ср. цена шт (g)", typeof(double));
            table.Columns.Add("Выручка с грядки (g)", typeof(double));
            table.Columns.Add("Затраты (g)", typeof(int));
            table.Columns.Add("Способ", typeof(string));
            table.Columns.Add("Прибыль с грядки (g)", typeof(int));
            table.Columns.Add("Δ к «без удобрения»", typeof(int));
            table.Columns.Add("Вердикт", typeof(string));

            foreach (var row in analysis.Rows)
            {
                table.Rows.Add(row.CropName, row.FertilizerName, row.GrowthDays, row.Harvests,
                    Math.Round(row.ExpectedUnitPrice, 2), Math.Round(row.RevenuePerPlot, 1),
                    row.InvestmentPerPlot, row.CostSource, row.ProfitPerPlot,
                    row.DeltaVsNoFertilizer, row.Verdict);
            }

            Data.DataSource = table;
            ConfigureGridImageColumn();

            int plots = int.TryParse(QuantityTextBox.Text, out int parsedPlots) && parsedPlots > 0 ? parsedPlots : 1;

            var lines = new List<string>();
            lines.Add($"Сравнение удобрений: сезон 28 дней, посадка в день 1, {plots} грядок.");

            foreach (var recommendation in analysis.Recommendations.Take(3))
                lines.Add("• " + recommendation);

            if (analysis.Recommendations.Count == 0)
                lines.Add("• Ни одно удобрение из базы не окупается на этих культурах.");

            int totalDelta = FertilizerAnalyzer.CalculateTotalDelta(analysis, plots);
            if (totalDelta > 0)
            {
                lines.Add($"Лучший вариант на {plots} грядок: {analysis.BestCropName} + " +
                          $"{analysis.BestFertilizerName} → +{totalDelta}g за сезон.");
            }

            lines.Add(analysis.Summary);
            FertilizerSummaryLabel.Text = string.Join("\r\n", lines);
        }

        private void ConfigureGridImageColumn()
        {
            if (Data.Columns.Contains("Иконка") && Data.Columns["Иконка"] is DataGridViewImageColumn imageColumn)
            {
                imageColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
                imageColumn.Width = 40;
            }
        }
    }
}
