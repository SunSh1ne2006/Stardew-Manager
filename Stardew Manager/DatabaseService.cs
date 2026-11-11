using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Stardew_Manager
{
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
            string query = "SELECT Season_Name FROM dbo.Seasons ORDER BY Id";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            seasons.Add(reader["Season_Name"].ToString().Trim());
                        }
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
                FROM dbo.Crops c
                INNER JOIN dbo.Crop_Season cs ON c.Id = cs.CropId
                INNER JOIN dbo.Seasons s ON cs.SeasonID = s.Id
                WHERE RTRIM(s.Season_Name) = @SeasonName
                ORDER BY c.Name";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@SeasonName", SqlDbType.NVarChar, 50).Value = seasonName ?? string.Empty;

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            crops.Add(new Crop
                            {
                                Name = reader["Name"].ToString() ?? string.Empty,
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
                                ImageName = reader["Image_Name"] != DBNull.Value ? reader["Image_Name"].ToString() : string.Empty
                            });
                        }
                    }
                }
            }

            return crops;
        }

        /// <summary>
        /// Читает удобрения вместе с шансами качества и рецептами крафта.
        /// Если в базе ещё нет таблиц удобрений, возвращает пустой список — приложение продолжает работать.
        /// </summary>
        public async Task<List<Fertilizer>> GetFertilizersAsync()
        {
            var fertilizers = new List<Fertilizer>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var qualities = new Dictionary<int, QualityTier>();
                    using (SqlCommand command = new SqlCommand(
                        "SELECT Id, Code, Name, Price_Multiplier FROM dbo.Qualities", connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int id = Convert.ToInt32(reader["Id"]);
                            qualities[id] = new QualityTier
                            {
                                Id = id,
                                Code = NormalizeText(reader["Code"].ToString()),
                                Name = NormalizeText(reader["Name"].ToString()),
                                PriceMultiplier = Convert.ToDouble(reader["Price_Multiplier"])
                            };
                        }
                    }

                    var byId = new Dictionary<int, Fertilizer>();
                    using (SqlCommand command = new SqlCommand(
                        "SELECT Id, Name, Price, Growth_Multiplier FROM dbo.Fertilizers ORDER BY Id", connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var fertilizer = new Fertilizer
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = NormalizeText(reader["Name"].ToString()),
                                PurchasePrice = reader["Price"] == DBNull.Value
                                    ? (int?)null
                                    : Convert.ToInt32(reader["Price"]),
                                GrowthMultiplier = Convert.ToDouble(reader["Growth_Multiplier"])
                            };

                            byId[fertilizer.Id] = fertilizer;
                            fertilizers.Add(fertilizer);
                        }
                    }

                    using (SqlCommand command = new SqlCommand(
                        "SELECT FertilizerId, QualityId, Chance FROM dbo.Quality_Chances", connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fertilizerId = Convert.ToInt32(reader["FertilizerId"]);
                            int qualityId = Convert.ToInt32(reader["QualityId"]);

                            if (!byId.TryGetValue(fertilizerId, out Fertilizer fertilizer))
                                continue;

                            if (!qualities.TryGetValue(qualityId, out QualityTier quality))
                                continue;

                            fertilizer.QualityChances.Add(new QualityChance
                            {
                                Quality = quality,
                                Chance = Convert.ToDouble(reader["Chance"])
                            });
                        }
                    }

                    using (SqlCommand command = new SqlCommand(@"
                        SELECT fr.FertilizerId, i.Id, i.Name, i.Sell_Price, fr.Quantity
                        FROM dbo.Fertilizer_Recipes fr
                        INNER JOIN dbo.Ingredients i ON i.Id = fr.IngredientId", connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int fertilizerId = Convert.ToInt32(reader["FertilizerId"]);

                            if (!byId.TryGetValue(fertilizerId, out Fertilizer fertilizer))
                                continue;

                            fertilizer.Recipe.Add(new FertilizerRecipeItem
                            {
                                Ingredient = new Ingredient
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Name = NormalizeText(reader["Name"].ToString()),
                                    SellPrice = Convert.ToInt32(reader["Sell_Price"])
                                },
                                Quantity = Convert.ToInt32(reader["Quantity"])
                            });
                        }
                    }
                }
            }
            catch (SqlException)
            {
                // Таблиц удобрений ещё нет (старая версия БД) — работаем без них.
                return new List<Fertilizer>();
            }

            return fertilizers;
        }

        /// <summary>
        /// Убирает мусор из текстовых полей БД: хвостовые пробелы nchar, переводы строк,
        /// табуляции и попавшие при импорте имена файлов вроде "Deluxe Speed-Gro.png".
        /// </summary>
        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string text = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');

            text = Regex.Replace(text, @"\S+\.(png|jpe?g|bmp|gif)", " ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s{2,}", " ");

            return text.Trim();
        }
    }
}
