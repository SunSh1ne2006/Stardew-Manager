using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Stardew_Manager
{
    public class DatabaseInitializer
    {
        private readonly string _serverConnectionString;
        private readonly string _databaseName = "Stardew Manager";

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
            await EnsureDatabaseAsync();
            await EnsureSchemaAsync();
        }

        /// <summary>Создаёт базу, если её ещё нет.</summary>
        private async Task EnsureDatabaseAsync()
        {
            using (SqlConnection connection = new SqlConnection(_serverConnectionString))
            {
                await connection.OpenAsync();

                string checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = @Name";
                using (SqlCommand checkCmd = new SqlCommand(checkDbQuery, connection))
                {
                    checkCmd.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 128).Value = _databaseName;

                    int dbCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (dbCount == 0)
                    {
                        string createDbQuery = $"CREATE DATABASE [{_databaseName}]";
                        using (SqlCommand createCmd = new SqlCommand(createDbQuery, connection))
                        {
                            await createCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Приводит схему к актуальной. Выполняется при каждом запуске и ничего не ломает:
        /// каждая таблица создаётся только если её нет, данные добавляются только в пустые таблицы.
        /// </summary>
        private async Task EnsureSchemaAsync()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_serverConnectionString)
            {
                InitialCatalog = _databaseName
            };

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();

                string script = @"
                    /* ---------- 1. Сезоны ---------- */
                    IF OBJECT_ID(N'dbo.Seasons', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Seasons (
                            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Seasons PRIMARY KEY,
                            Season_Name NVARCHAR(20) NOT NULL,
                            CONSTRAINT UQ_Seasons_Season_Name UNIQUE (Season_Name),
                            CONSTRAINT CHK_Seasons_Season_Name CHECK (Season_Name IN (N'Spring', N'Summer', N'Fall', N'Winter'))
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.Seasons)
                        INSERT INTO dbo.Seasons (Season_Name) VALUES (N'Spring'), (N'Summer'), (N'Fall'), (N'Winter');

                    /* ---------- 2. Культуры ---------- */
                    IF OBJECT_ID(N'dbo.Crops', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Crops (
                            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Crops PRIMARY KEY,
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
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.Crops)
                        INSERT INTO dbo.Crops
                        (Name, Seed_Price, Price, Growth_Time, Reusable, Regrow_Time, Min_Yield_Quantity, Max_Yield_Quantity, Extra_Crop_Chance, Image_Name)
                        VALUES
                        (N'Пастернак', 20, 35, 4, 0, NULL, 1, 1, NULL, 'Parsnip.png'),
                        (N'Клубника', 100, 120, 8, 1, 4, 1, 1, 0.02, 'Strawberry.png'),
                        (N'Цветная капуста', 80, 175, 12, 0, NULL, 1, 1, NULL, 'Cauliflower.png');

                    /* ---------- 3. Связь культур и сезонов ---------- */
                    IF OBJECT_ID(N'dbo.Crop_Season', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Crop_Season (
                            CropId INT NOT NULL,
                            SeasonID INT NOT NULL,
                            CONSTRAINT PK_Crop_Season PRIMARY KEY (CropId, SeasonID),
                            CONSTRAINT FK_Crop_Season_Crops FOREIGN KEY (CropId) REFERENCES dbo.Crops(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Crop_Season_Seasons FOREIGN KEY (SeasonID) REFERENCES dbo.Seasons(Id) ON DELETE CASCADE
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Crop_Season_SeasonID'
                                   AND object_id = OBJECT_ID(N'dbo.Crop_Season'))
                        CREATE INDEX IX_Crop_Season_SeasonID ON dbo.Crop_Season (SeasonID);

                    INSERT INTO dbo.Crop_Season (CropId, SeasonID)
                    SELECT c.Id, s.Id
                    FROM dbo.Crops c
                    INNER JOIN dbo.Seasons s ON RTRIM(s.Season_Name) = N'Spring'
                    WHERE c.Name IN (N'Пастернак', N'Клубника', N'Цветная капуста')
                      AND NOT EXISTS (SELECT 1 FROM dbo.Crop_Season cs WHERE cs.CropId = c.Id AND cs.SeasonID = s.Id);

                    /* ---------- 4. Ингредиенты для крафта ---------- */
                    IF OBJECT_ID(N'dbo.Ingredients', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Ingredients (
                            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ingredients PRIMARY KEY,
                            Name NVARCHAR(50) NOT NULL,
                            Sell_Price INT NOT NULL,
                            CONSTRAINT UQ_Ingredients_Name UNIQUE (Name),
                            CONSTRAINT CHK_Ingredients_Sell_Price CHECK (Sell_Price >= 0)
                        );
                    END;

                    /* ---------- 5. Удобрения ---------- */
                    IF OBJECT_ID(N'dbo.Fertilizers', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Fertilizers (
                            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Fertilizers PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            Price INT NULL,
                            Growth_Multiplier DECIMAL(3,2) NOT NULL CONSTRAINT DF_Fertilizers_Growth_Multiplier DEFAULT (1.00),
                            CONSTRAINT UQ_Fertilizers_Name UNIQUE (Name),
                            CONSTRAINT CHK_Fertilizers_Growth_Multiplier CHECK (Growth_Multiplier > 0),
                            CONSTRAINT CHK_Fertilizers_Price CHECK (Price IS NULL OR Price >= 0)
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.Fertilizers)
                        INSERT INTO dbo.Fertilizers (Name, Price, Growth_Multiplier) VALUES
                        (N'Без удобрения', 0, 1.00),
                        (N'Базовая подкормка', 100, 1.00),
                        (N'Качественное удобрение', NULL, 1.00),
                        (N'Ускоренная подкормка', NULL, 0.90),
                        (N'Люкс-подкормка', NULL, 0.75);

                    /* ---------- 6. Рецепты удобрений ---------- */
                    IF OBJECT_ID(N'dbo.Fertilizer_Recipes', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Fertilizer_Recipes (
                            FertilizerId INT NOT NULL,
                            IngredientId INT NOT NULL,
                            Quantity INT NOT NULL,
                            CONSTRAINT PK_Fertilizer_Recipes PRIMARY KEY (FertilizerId, IngredientId),
                            CONSTRAINT FK_Fertilizer_Recipes_Fertilizers FOREIGN KEY (FertilizerId) REFERENCES dbo.Fertilizers(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Fertilizer_Recipes_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id),
                            CONSTRAINT CHK_Fertilizer_Recipes_Quantity CHECK (Quantity > 0)
                        );
                    END;

                    /* ---------- 7. Качества урожая ---------- */
                    IF OBJECT_ID(N'dbo.Qualities', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Qualities (
                            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Qualities PRIMARY KEY,
                            Code NVARCHAR(50) NOT NULL,
                            Name NVARCHAR(50) NOT NULL,
                            Price_Multiplier DECIMAL(4,2) NOT NULL,
                            CONSTRAINT UQ_Qualities_Code UNIQUE (Code),
                            CONSTRAINT CHK_Qualities_Price_Multiplier CHECK (Price_Multiplier > 0)
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.Qualities)
                        INSERT INTO dbo.Qualities (Code, Name, Price_Multiplier) VALUES
                        (N'normal', N'Обычное', 1.00),
                        (N'silver', N'Серебряное', 1.25),
                        (N'gold', N'Золотое', 1.50),
                        (N'iridium', N'Иридиевое', 2.00);

                    /* ---------- 8. Шансы качества по удобрениям ---------- */
                    IF OBJECT_ID(N'dbo.Quality_Chances', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Quality_Chances (
                            FertilizerId INT NOT NULL,
                            QualityId INT NOT NULL,
                            Chance DECIMAL(5,4) NOT NULL,
                            CONSTRAINT PK_Quality_Chances PRIMARY KEY (FertilizerId, QualityId),
                            CONSTRAINT FK_Quality_Chances_Fertilizers FOREIGN KEY (FertilizerId) REFERENCES dbo.Fertilizers(Id) ON DELETE CASCADE,
                            CONSTRAINT FK_Quality_Chances_Qualities FOREIGN KEY (QualityId) REFERENCES dbo.Qualities(Id),
                            CONSTRAINT CHK_Quality_Chances_Chance CHECK (Chance >= 0 AND Chance <= 1)
                        );
                    END;

                    /* ---------- 9. Инвентарь игрока ---------- */
                    IF OBJECT_ID(N'dbo.User_Inventory', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.User_Inventory (
                            IngredientId INT NOT NULL CONSTRAINT PK_User_Inventory PRIMARY KEY,
                            Available_Quantity INT NOT NULL CONSTRAINT DF_User_Inventory_Available_Quantity DEFAULT (0),
                            CONSTRAINT FK_User_Inventory_Ingredients FOREIGN KEY (IngredientId) REFERENCES dbo.Ingredients(Id) ON DELETE CASCADE,
                            CONSTRAINT CHK_User_Inventory_Available_Quantity CHECK (Available_Quantity >= 0)
                        );
                    END;

                    /* ---------- Демонстрационные данные (только если справочники пустые) ---------- */
                    IF NOT EXISTS (SELECT 1 FROM dbo.Ingredients)
                        INSERT INTO dbo.Ingredients (Name, Sell_Price) VALUES
                        (N'Сосновая смола', 100),
                        (N'Дубовая смола', 150),
                        (N'Раковина', 50),
                        (N'Коралл', 80),
                        (N'Сок', 2),
                        (N'Рыба', 100);

                    IF NOT EXISTS (SELECT 1 FROM dbo.Fertilizer_Recipes)
                    BEGIN
                        INSERT INTO dbo.Fertilizer_Recipes (FertilizerId, IngredientId, Quantity)
                        SELECT f.Id, i.Id, x.Quantity
                        FROM (VALUES
                            (N'Ускоренная подкормка', N'Сосновая смола', 1),
                            (N'Ускоренная подкормка', N'Раковина', 1),
                            (N'Люкс-подкормка', N'Дубовая смола', 1),
                            (N'Люкс-подкормка', N'Коралл', 1),
                            (N'Качественное удобрение', N'Сок', 5),
                            (N'Качественное удобрение', N'Рыба', 1)
                        ) AS x(FertilizerName, IngredientName, Quantity)
                        INNER JOIN dbo.Fertilizers f ON f.Name = x.FertilizerName
                        INNER JOIN dbo.Ingredients i ON i.Name = x.IngredientName;
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.Quality_Chances)
                    BEGIN
                        INSERT INTO dbo.Quality_Chances (FertilizerId, QualityId, Chance)
                        SELECT f.Id, q.Id, x.Chance
                        FROM (VALUES
                            (N'Без удобрения', N'normal', CAST(1.0000 AS DECIMAL(5,4))),
                            (N'Базовая подкормка', N'normal', CAST(0.6000 AS DECIMAL(5,4))),
                            (N'Базовая подкормка', N'silver', CAST(0.3000 AS DECIMAL(5,4))),
                            (N'Базовая подкормка', N'gold', CAST(0.1000 AS DECIMAL(5,4))),
                            (N'Качественное удобрение', N'normal', CAST(0.4000 AS DECIMAL(5,4))),
                            (N'Качественное удобрение', N'silver', CAST(0.3000 AS DECIMAL(5,4))),
                            (N'Качественное удобрение', N'gold', CAST(0.3000 AS DECIMAL(5,4))),
                            (N'Ускоренная подкормка', N'normal', CAST(1.0000 AS DECIMAL(5,4))),
                            (N'Люкс-подкормка', N'normal', CAST(1.0000 AS DECIMAL(5,4)))
                        ) AS x(FertilizerName, QualityCode, Chance)
                        INNER JOIN dbo.Fertilizers f ON f.Name = x.FertilizerName
                        INNER JOIN dbo.Qualities q ON q.Code = x.QualityCode;
                    END;
                ";

                using (SqlCommand command = new SqlCommand(script, connection))
                {
                    command.CommandTimeout = 60;
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
