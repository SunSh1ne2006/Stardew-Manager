using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Stardew_Manager
{
    public class DatabaseInitializer
    {
        private readonly string _serverConnectionString;
        private readonly string _databaseName = "Stardew Manage";

        public DatabaseInitializer(string fullConnectionString)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(fullConnectionString)
            {
                InitialCatalog = "master"
            };
            _serverConnectionString = builder.ConnectionString;
        }

        public async Task InitializeDatabaseAsync()
        {
            using (SqlConnection connection = new SqlConnection(_serverConnectionString))
            {
                await connection.OpenAsync();

                string checkDbQuery = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{_databaseName}'";
                using (SqlCommand checkCmd = new SqlCommand(checkDbQuery, connection))
                {
                    int dbCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (dbCount == 0)
                    {
                        string createDbQuery = $"CREATE DATABASE [{_databaseName}]";
                        using (SqlCommand createCmd = new SqlCommand(createDbQuery, connection))
                        {
                            await createCmd.ExecuteNonQueryAsync();
                        }

                        await CreateTablesAndSeedDataAsync();
                    }
                }
            }
        }

        private async Task CreateTablesAndSeedDataAsync()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_serverConnectionString)
            {
                InitialCatalog = _databaseName
            };

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();

                string script = @"
                    -- 1. Таблица Seasons
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Seasons')
                    BEGIN
                        CREATE TABLE dbo.Seasons (
                            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            Season_Name VARCHAR(10) NOT NULL,
                            CONSTRAINT CHK_Season_Name CHECK (Season_Name IN ('Spring', 'Summer', 'Fall', 'Winter'))
                        );

                        INSERT INTO dbo.Seasons (Season_Name) VALUES 
                        ('Spring'), ('Summer'), ('Fall'), ('Winter');
                    END;

                    -- 2. Таблица Crops
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Crops')
                    BEGIN
                        CREATE TABLE dbo.Crops (
                            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            Name NVARCHAR(50) NOT NULL,
                            Seed_Price INT NOT NULL,
                            Price INT NOT NULL,
                            Growth_Time INT NOT NULL,
                            Reusable BIT NOT NULL,
                            Regrow_Time INT NULL,
                            Min_Yield_Quantity INT NOT NULL,
                            Max_Yield_Quantity INT NOT NULL,
                            Extra_Crop_Chance FLOAT NULL,
                            Image_Name NVARCHAR(100) NULL
                        );

                        INSERT INTO dbo.Crops 
                        (Name, Seed_Price, Price, Growth_Time, Reusable, Regrow_Time, Min_Yield_Quantity, Max_Yield_Quantity, Extra_Crop_Chance, Image_Name)
                        VALUES 
                        (N'Пастернак', 20, 35, 4, 0, NULL, 1, 1, NULL, 'parsnip.png'),
                        (N'Клубника', 100, 120, 8, 1, 4, 1, 1, 0.02, 'strawberry.png'),
                        (N'Цветная капуста', 80, 175, 12, 0, NULL, 1, 1, NULL, 'cauliflower.png');
                    END;

                    -- 3. Связующая таблица Crop_Season
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Crop_Season')
                    BEGIN
                        CREATE TABLE dbo.Crop_Season (
                            CropId INT NOT NULL,
                            SeasonID INT NOT NULL,
                            CONSTRAINT PK_Crop_Season PRIMARY KEY (CropId, SeasonID),
                            CONSTRAINT FK_CropSeason_Crops FOREIGN KEY (CropId) REFERENCES dbo.Crops(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_CropSeason_Seasons FOREIGN KEY (SeasonID) REFERENCES dbo.Seasons(Id) ON DELETE CASCADE
                        );

                        INSERT INTO dbo.Crop_Season (CropId, SeasonID) VALUES 
                        (1, 1), (2, 1), (3, 1);
                    END;

                    -- 4. Таблица Ingredients
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Ingredients')
                    BEGIN
                        CREATE TABLE dbo.Ingredients (
                            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            Name NCHAR(100) NOT NULL,
                            Sell_Price INT NOT NULL
                        );

                        INSERT INTO dbo.Ingredients (Name, Sell_Price) VALUES 
                        (N'Сок (Sap)', 2),
                        (N'Костная мука (Bone Meal)', 5),
                        (N'Кора (Moss)', 5);
                    END;

                    -- 5. Таблица User_Inventory
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'User_Inventory')
                    BEGIN
                        CREATE TABLE dbo.User_Inventory (
                            IngredientId INT NOT NULL PRIMARY KEY,
                            Available_Quantity INT NOT NULL,
                            CONSTRAINT FK_UserInventory_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id) ON DELETE CASCADE
                        );
                    END;
                ";

                using (SqlCommand command = new SqlCommand(script, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}