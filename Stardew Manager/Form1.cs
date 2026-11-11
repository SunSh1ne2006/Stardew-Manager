using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stardew_Manager
{
    public partial class Form1 : Form
    {
        private DataGridView Data;
        private DataGridView CatalogGrid;
        private TabControl ResultTabs;
        private TabPage CatalogTab;
        private TabPage PlanTab;
        private TextBox QuantityTextBox;
        private TextBox MoneyQuantityTextBox;
        private Button CalculateButton;
        private Label ResultSummaryLabel;
        private Label RecommendationLabel;
        private ComboBox SeasonComboBox;
        private Label SeasonLabel;

        private List<Crop> _availableCrops = new List<Crop>();
        private List<Fertilizer> _fertilizers = new List<Fertilizer>();
        private readonly Dictionary<string, Image> _iconCache = new Dictionary<string, Image>();
        private readonly DatabaseService _dbService;
        private string _currentSeason = string.Empty;

        public Form1()
        {
            InitializeCustomCard();

            _dbService = new DatabaseService(ConnectionStringProvider.Get());
            CatalogGrid.CellFormatting += OnCatalogCellFormatting;
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
            catch (Exception)
            {
                // Таблицы удобрений недоступны — считаем без них, приложение продолжает работать.
                _fertilizers = new List<Fertilizer>();
            }

            if (!_fertilizers.Any(IsNeutralFertilizer))
                _fertilizers.Insert(0, Fertilizer.CreateNone());
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
                    SeasonComboBox.Items.Add(season);

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
                await LoadDataForSelectedSeasonAsync(SeasonComboBox.SelectedItem.ToString());
        }

        private async Task LoadDataForSelectedSeasonAsync(string selectedSeason)
        {
            try
            {
                CalculateButton.Enabled = false;

                _currentSeason = selectedSeason;
                _availableCrops = await _dbService.GetCropsBySeasonAsync(selectedSeason);

                var analysis = _availableCrops.Count > 0 && _fertilizers.Any(f => f.HasAnyEffect)
                    ? FertilizerAnalyzer.Analyze(_availableCrops, _fertilizers)
                    : null;

                DisplayCatalog(analysis);
                UpdateRecommendations(analysis);
                ShowPlanHint();
                ResultTabs.SelectedTab = CatalogTab;
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

        /// <summary>Каталог культур сезона: базовая экономика и лучшее удобрение для каждой.</summary>
        private void DisplayCatalog(FertilizerAnalysisResult? analysis)
        {
            var bestByCrop = new Dictionary<string, FertilizerComparisonRow>();

            if (analysis != null)
            {
                foreach (var group in analysis.Rows
                             .Where(r => !r.IsBaseline && r.DeltaVsNoFertilizer > 0)
                             .GroupBy(r => r.CropName))
                {
                    bestByCrop[group.Key] = group.OrderByDescending(r => r.DeltaVsNoFertilizer).First();
                }
            }

            DataTable table = new DataTable();
            table.Columns.Add("Иконка", typeof(Image));
            table.Columns.Add("Культура", typeof(string));
            table.Columns.Add("Семена (g)", typeof(int));
            table.Columns.Add("Цена плода (g)", typeof(int));
            table.Columns.Add("Дней до урожая", typeof(int));
            table.Columns.Add("Урожаев за сезон", typeof(int));
            table.Columns.Add("Прибыль без удобрений (g)", typeof(int));
            table.Columns.Add("Лучшее удобрение", typeof(string));
            table.Columns.Add("Прибыль с удобрением (g)", typeof(int));
            table.Columns.Add("Прирост (g)", typeof(int));

            foreach (var crop in _availableCrops)
            {
                int baseProfit = crop.CalculateProfitForSeason(1, OptimizationService.SeasonLength);

                bestByCrop.TryGetValue(crop.Name, out FertilizerComparisonRow? best);

                table.Rows.Add(
                    GetIcon(crop),
                    crop.Name,
                    crop.SeedPrice,
                    crop.SellPrice,
                    crop.GetGrowthDays(null),
                    crop.GetHarvestCount(1, OptimizationService.SeasonLength, null),
                    baseProfit,
                    best != null ? best.FertilizerName : "—",
                    best != null ? best.ProfitPerPlot : baseProfit,
                    best != null ? best.DeltaVsNoFertilizer : 0);
            }

            CatalogGrid.DataSource = table;
            ApplyGridFormatting(CatalogGrid);
        }

        /// <summary>Подсказка в блоке итогов, пока план не рассчитан.</summary>
        private void ShowPlanHint()
        {
            Data.DataSource = null;

            if (_availableCrops.Count == 0)
            {
                ResultSummaryLabel.Text = $"В сезоне «{_currentSeason}» в базе нет культур — сажать нечего.";
                return;
            }

            ResultSummaryLabel.Text =
                $"Сезон «{_currentSeason}»: {_availableCrops.Count} культур. " +
                "Укажите количество грядок и бюджет, затем нажмите «Рассчитать план» —\r\n" +
                "программа сама выберет, что сажать, и на какие культуры стоит покупать удобрения.";
        }

        /// <summary>Автоматический вывод по удобрениям для культур текущего сезона.</summary>
        private void UpdateRecommendations(FertilizerAnalysisResult? analysis)
        {
            if (_availableCrops.Count == 0)
            {
                RecommendationLabel.Text = "Нет культур для анализа.";
                return;
            }

            var effective = _fertilizers.Where(f => f.HasAnyEffect).ToList();
            if (effective.Count == 0 || analysis == null)
            {
                RecommendationLabel.Text = "В базе нет удобрений с эффектом — план считается без них.";
                return;
            }

            var lines = new List<string>();
            lines.Add($"Проанализировано {_availableCrops.Count} культур сезона «{_currentSeason}» " +
                      $"и {effective.Count} удобрений (посадка в 1-й день, 28 дней сезона).");

            var bestPerCrop = analysis.Rows
                .Where(r => !r.IsBaseline && r.DeltaVsNoFertilizer > 0)
                .GroupBy(r => r.CropName)
                .Select(g => g.OrderByDescending(r => r.DeltaVsNoFertilizer).First())
                .OrderByDescending(r => r.DeltaVsNoFertilizer)
                .ToList();

            if (bestPerCrop.Count == 0)
            {
                lines.Add("Ни одно удобрение не окупается на культурах этого сезона — программа посадит без удобрений.");
            }
            else
            {
                lines.Add($"Удобрения окупаются на {bestPerCrop.Count} из {_availableCrops.Count} культур. Лучшие варианты:");

                foreach (var row in bestPerCrop.Take(3))
                {
                    lines.Add($"    • {row.CropName} + {row.FertilizerName}: +{row.DeltaVsNoFertilizer:N0} g с грядки " +
                              $"({row.CostSource}, {row.FertilizerCost:N0} g)");
                }
            }

            var useless = effective
                .Where(f => !analysis.Rows.Any(r => !r.IsBaseline
                                                    && r.FertilizerName == f.Name
                                                    && r.DeltaVsNoFertilizer > 0))
                .Select(f => f.Name)
                .ToList();

            if (useless.Count > 0)
                lines.Add($"Не окупаются нигде в этом сезоне: {string.Join(", ", useless)}.");

            RecommendationLabel.Text = string.Join("\r\n", lines);
        }

        private void OnCalculateButtonClick(object sender, EventArgs e)
        {
            if (!int.TryParse(QuantityTextBox.Text, out int plots) || plots <= 0)
            {
                MessageBox.Show("Введите корректное количество грядок (целое число больше 0).",
                    "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                QuantityTextBox.Focus();
                return;
            }

            if (!int.TryParse(MoneyQuantityTextBox.Text, out int money) || money <= 0)
            {
                MessageBox.Show("Введите корректный бюджет (целое число больше 0).",
                    "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                MoneyQuantityTextBox.Focus();
                return;
            }

            if (_availableCrops.Count == 0)
            {
                MessageBox.Show($"В сезоне «{_currentSeason}» нет культур в базе.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var fertilizers = BuildFertilizerOptions();
            var plan = OptimizationService.CalculateBestPlan(_availableCrops, money, plots, fertilizers);

            DisplayPlan(plan);
            UpdateSummary(plan, money, plots);
            ResultTabs.SelectedTab = PlanTab;
        }

        private void DisplayPlan(OptimizationService.PlanResult plan)
        {
            DataTable table = new DataTable();
            table.Columns.Add("Иконка", typeof(Image));
            table.Columns.Add("День", typeof(int));
            table.Columns.Add("Культура", typeof(string));
            table.Columns.Add("Удобрение", typeof(string));
            table.Columns.Add("Грядок", typeof(int));
            table.Columns.Add("Семена (g)", typeof(int));
            table.Columns.Add("Удобрение (g)", typeof(int));
            table.Columns.Add("Итого (g)", typeof(int));

            foreach (var planting in plan.Schedule)
            {
                var crop = _availableCrops.Find(c => c.Name == planting.CropName);

                table.Rows.Add(
                    GetIcon(crop),
                    planting.Day,
                    planting.CropName,
                    planting.FertilizerName,
                    planting.Quantity,
                    planting.SeedCost,
                    planting.FertilizerCost,
                    planting.Cost);
            }

            Data.DataSource = table;
            ApplyGridFormatting(Data);
        }

        private void UpdateSummary(OptimizationService.PlanResult plan, int money, int plots)
        {
            var neutralPlan = OptimizationService.CalculateBestPlan(_availableCrops, money, plots,
                new List<Fertilizer> { GetNeutralFertilizer() });

            int invested = plan.TotalSpentOnSeeds + plan.TotalSpentOnFertilizer;
            double roi = invested > 0 ? (double)plan.TotalNetProfit / invested * 100.0 : 0.0;
            int plants = plan.Schedule.Sum(s => s.Quantity);
            int fertilizerDelta = plan.TotalNetProfit - neutralPlan.TotalNetProfit;

            if (plan.Schedule.Count == 0)
            {
                ResultSummaryLabel.Text =
                    $"План пуст: бюджета {money:N0} g не хватает даже на самые дешёвые семена.";
                return;
            }

            var lines = new List<string>();

            lines.Add($"Затраты: семена {plan.TotalSpentOnSeeds:N0} g · удобрения {plan.TotalSpentOnFertilizer:N0} g " +
                      $"· всего {invested:N0} g");

            string roiText = roi >= 0 ? $"+{roi:0}%" : $"{roi:0}%";
            lines.Add($"Баланс в конце сезона: {plan.FinalMoney:N0} g · чистая прибыль {plan.TotalNetProfit:N0} g " +
                      $"({roiText} к вложению)");

            if (plan.TotalSpentOnFertilizer > 0)
            {
                lines.Add($"Удобрения дают {fertilizerDelta:+#,0;-#,0;0} g к плану без удобрений " +
                          $"(без них было бы {neutralPlan.TotalNetProfit:N0} g)");
            }
            else
            {
                lines.Add($"Удобрения не использованы: в этом плане они не увеличивают прибыль " +
                          $"(план без них — {neutralPlan.TotalNetProfit:N0} g).");
            }

            lines.Add($"Растений по плану: {plants} · культур: {plan.TotalPlantedSummary.Count} " +
                      $"· грядок в наличии: {plots}");

            ResultSummaryLabel.Text = string.Join("\r\n", lines);
        }

        private void OnCatalogCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.CellStyle == null || e.ColumnIndex < 0 || e.ColumnIndex >= CatalogGrid.Columns.Count)
                return;

            string columnName = CatalogGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "Прибыль без удобрений (g)" && e.Value is int profit && profit < 0)
                e.CellStyle.ForeColor = Color.DarkRed;

            if (columnName == "Прирост (g)" && e.Value is int delta)
            {
                if (delta > 0)
                    e.CellStyle.ForeColor = Color.DarkGreen;
                else if (delta == 0)
                    e.CellStyle.ForeColor = Color.Gray;
            }
        }

        private static void ApplyGridFormatting(DataGridView grid)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column is DataGridViewImageColumn imageColumn)
                {
                    imageColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
                    imageColumn.Width = 44;
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    continue;
                }

                column.SortMode = DataGridViewColumnSortMode.Automatic;

                if (column.ValueType == typeof(int) || column.ValueType == typeof(double))
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    column.DefaultCellStyle.Format = column.ValueType == typeof(double) ? "N2" : "N0";
                }
                else
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                }
            }
        }

        /// <summary>Иконки кэшируются: Image.FromFile на каждый показ — это утечка GDI-хэндлов.</summary>
        private Image GetIcon(Crop crop)
        {
            if (crop == null || string.IsNullOrWhiteSpace(crop.ImageName))
                return null;

            if (_iconCache.TryGetValue(crop.ImageName, out Image cached))
                return cached;

            Image image = crop.Icon;
            _iconCache[crop.ImageName] = image;
            return image;
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

        /// <summary>Планировщик перебирает все удобрения и сам решает, где они окупаются.</summary>
        private List<Fertilizer> BuildFertilizerOptions()
        {
            var list = new List<Fertilizer>(_fertilizers);
            if (!list.Any(IsNeutralFertilizer))
                list.Add(Fertilizer.CreateNone());

            return list;
        }
    }
}
