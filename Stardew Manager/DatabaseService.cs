using System;
using System.Collections.Generic;
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
    }
}