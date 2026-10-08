using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Stardew_Manager
{
    public class IngredientItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SellPrice { get; set; }
        public int AvailableQuantity { get; set; }
    }

    public class FertilizerItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal GrowthMultiplier { get; set; }
    }

    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<string>> GetSeasonsAsync()
        {
            var seasons = new List<string>();
            string query = "SELECT Season_Name FROM Seasons";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        seasons.Add(reader["Season_Name"].ToString().Trim());
                    }
                }
            }

            return seasons;
        }

        public async Task<List<Crop>> GetCropsBySeasonAsync(string seasonName)
        {
            var crops = new List<Crop>();

            string query = @"
                SELECT c.Name, c.Seed_Price, c.Price, c.Growth_Time, c.Reusable, c.Regrow_Time, 
                       c.Min_Yield_Quantity, c.Max_Yield_Quantity, c.Extra_Crop_Chance, c.Image_Name 
                FROM Crops c
                INNER JOIN Crop_Season cs ON c.Id = cs.CropId
                INNER JOIN Seasons s ON cs.SeasonID = s.Id
                WHERE RTRIM(s.Season_Name) = @SeasonName";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SeasonName", seasonName);

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        crops.Add(new Crop
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
                                : 0.0,
                            ImageName = reader["Image_Name"] != DBNull.Value ? reader["Image_Name"].ToString() : null
                        });
                    }
                }
            }

            return crops;
        }

        public async Task<List<IngredientItem>> GetUserInventoryAsync()
        {
            var list = new List<IngredientItem>();
            string query = @"
                SELECT i.Id, i.Name, i.Sell_Price, ISNULL(inv.Available_Quantity, 0) AS Available_Quantity
                FROM Ingredients i
                LEFT JOIN User_Inventory inv ON i.Id = inv.IngredientId";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new IngredientItem
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString().Trim(),
                            SellPrice = Convert.ToInt32(reader["Sell_Price"]),
                            AvailableQuantity = Convert.ToInt32(reader["Available_Quantity"])
                        });
                    }
                }
            }

            return list;
        }

        public async Task SaveUserInventoryAsync(List<IngredientItem> items)
        {
            string mergeQuery = @"
                MERGE INTO User_Inventory AS target
                USING (SELECT @IngredientId AS IngredientId, @Quantity AS Available_Quantity) AS source
                ON (target.IngredientId = source.IngredientId)
                WHEN MATCHED THEN
                    UPDATE SET Available_Quantity = source.Available_Quantity
                WHEN NOT MATCHED THEN
                    INSERT (IngredientId, Available_Quantity) VALUES (source.IngredientId, source.Available_Quantity);";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in items)
                {
                    using (SqlCommand command = new SqlCommand(mergeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@IngredientId", item.Id);
                        command.Parameters.AddWithValue("@Quantity", item.AvailableQuantity);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<List<Fertilizer>> GetFertilizersAsync()
        {
            var fertilizers = new List<Fertilizer>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Используем только реальные существующие столбцы таблицы Fertilizers
                string query = "SELECT Id, Name FROM Fertilizers";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        fertilizers.Add(new Fertilizer
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }

            return fertilizers;
        }
    }
}