using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
        private Label ResultSummaryLabel;
        private ComboBox SeasonComboBox;
        private Label SeasonLabel;

        private List<Crop> _availableCrops = new List<Crop>();
        private readonly DatabaseService _dbService;

        public Form1()
        {
            InitializeCustomCard();

            string connectionString = ConfigurationManager.ConnectionStrings["StardewDb"]?.ConnectionString
                ?? @"Data Source=localhost\MSSQLSERVER02;Initial Catalog=Stardew Manager;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Command Timeout=0;";

            _dbService = new DatabaseService(connectionString);
            this.Load += OnFormLoad;
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["StardewDb"]?.ConnectionString
                    ?? @"Data Source=localhost\MSSQLSERVER02;Initial Catalog=Stardew Manager;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Command Timeout=0;";

                var initializer = new DatabaseInitializer(connectionString);
                await initializer.InitializeDatabaseAsync();

                await LoadSeasonsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}", "Ошибка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                MessageBox.Show($"Ошибка при загрузке сезонов: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке культур: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                CalculateButton.Enabled = true;
            }
        }

        private void DisplayInitialCrops()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Название культуры", typeof(string));

            foreach (var crop in _availableCrops)
            {
                table.Rows.Add(crop.Name);
            }

            Data.DataSource = table;
        }

        private void OnCalculateButtonClick(object sender, EventArgs e)
        {
            if (!int.TryParse(QuantityTextBox.Text, out int plots) || plots <= 0)
            {
                MessageBox.Show("Введите корректное количество грядок!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(MoneyQuantityTextBox.Text, out int money) || money <= 0)
            {
                MessageBox.Show("Введите корректное количество денег!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = OptimizationService.CalculateBestPlan(_availableCrops, money, plots);

            DataTable scheduleTable = new DataTable();
            scheduleTable.Columns.Add("День сезона", typeof(string));
            scheduleTable.Columns.Add("Культура", typeof(string));
            scheduleTable.Columns.Add("Количество (шт)", typeof(int));
            scheduleTable.Columns.Add("Затраты (g)", typeof(int));

            foreach (var planting in result.Schedule)
            {
                scheduleTable.Rows.Add($"День {planting.Day}", planting.CropName, planting.Quantity, planting.Cost);
            }

            Data.DataSource = scheduleTable;

            ResultSummaryLabel.Text = $"Затраты: {result.TotalSpentOnSeeds}g | Баланс в конце: {result.FinalMoney}g | Чистая прибыль: {result.TotalNetProfit}g";
        }

    }
}