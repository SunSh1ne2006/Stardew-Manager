using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

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

        private readonly List<Crop> _availableCrops = new List<Crop>();
        private readonly string _connectionString = @"Data Source=localhost\MSSQLSERVER02;Initial Catalog=Stardew Manager;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Command Timeout=0;";

        public Form1()
        {
            InitializeCustomCard();
            LoadSeasonsFromDatabase();
        }

        private void LoadSeasonsFromDatabase()
        {
            string query = "SELECT Season_Name FROM Seasons";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    SeasonComboBox.Items.Clear();
                    while (reader.Read())
                    {
                        SeasonComboBox.Items.Add(reader["Season_Name"].ToString().Trim());
                    }
                }

                if (SeasonComboBox.Items.Count > 0)
                    SeasonComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке сезонов: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSeasonChanged(object sender, EventArgs e)
        {
            if (SeasonComboBox.SelectedItem != null)
            {
                LoadDataFromDatabase(SeasonComboBox.SelectedItem.ToString());
            }
        }

        private void LoadDataFromDatabase(string selectedSeason)
        {
            string query = @"
                SELECT c.Name, c.Seed_Price, c.Price, c.Growth_Time, c.Reusable, c.Regrow_Time, c.Min_Yield_Quantity, c.Max_Yield_Quantity, c.Extra_Crop_Chance 
                FROM Crops c
                INNER JOIN Crop_Season cs ON c.Id = cs.CropId
                INNER JOIN Seasons s ON cs.SeasonID = s.Id
                WHERE RTRIM(s.Season_Name) = @SeasonName";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@SeasonName", selectedSeason);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    _availableCrops.Clear();

                    while (reader.Read())
                    {
                        _availableCrops.Add(new Crop
                        {
                            Name = reader["Name"].ToString(),
                            SeedPrice = Convert.ToInt32(reader["Seed_Price"]),
                            SellPrice = Convert.ToInt32(reader["Price"]),
                            GrowthTime = Convert.ToInt32(reader["Growth_Time"]),
                            Regrows = Convert.ToBoolean(reader["Reusable"]),
                            RegrowTime = reader["Regrow_Time"] != DBNull.Value ? Convert.ToInt32(reader["Regrow_Time"]) : 0,
                            MinYield = Convert.ToInt32(reader["Min_Yield_Quantity"]),
                            MaxYield = Convert.ToInt32(reader["Max_Yield_Quantity"]),
                            ExtraCropChance = reader["Extra_Crop_Chance"] != DBNull.Value
        ? Convert.ToDouble(reader["Extra_Crop_Chance"])
        : 0.0
                        });
                    }
                }

                DisplayInitialCrops();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке культур: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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