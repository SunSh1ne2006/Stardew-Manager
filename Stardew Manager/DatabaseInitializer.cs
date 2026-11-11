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
                            CONSTRAINT CHK_Seasons_Season_Name CHECK (Season_Name IN (N'Spring', N'Summer', N'Autumn', N'Fall', N'Winter'))
                        );
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.Seasons)
                        INSERT INTO dbo.Seasons (Season_Name) VALUES (N'Spring'), (N'Summer'), (N'Autumn'), (N'Winter');

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
                    BEGIN
                        INSERT INTO dbo.Crops
                        (Name, Seed_Price, Price, Growth_Time, Reusable, Regrow_Time, Min_Yield_Quantity, Max_Yield_Quantity, Extra_Crop_Chance, Image_Name)
                        VALUES
                        (N'Пастернак', 20, 35, 4, 0, NULL, 1, 1, 0.000, N'Parsnip.png'),
                        (N'Картофель', 50, 80, 6, 0, NULL, 1, 2, 0.200, N'Potato.png'),
                        (N'Цветная Капуста', 80, 175, 12, 0, NULL, 1, 1, 0.000, N'Cauliflower.png'),
                        (N'Клубника', 100, 120, 8, 1, 4, 1, 2, 0.020, N'Strawberry.png'),
                        (N'Зелёная Фасоль', 60, 40, 10, 1, 3, 1, 1, 0.000, N'Green_Bean.png'),
                        (N'Чеснок', 40, 60, 4, 0, NULL, 1, 1, 0.000, N'Garlic.png'),
                        (N'Кудрявая Капуста', 70, 110, 6, 0, NULL, 1, 1, 0.000, N'Kale.png'),
                        (N'Ревень', 100, 220, 13, 0, NULL, 1, 1, 0.000, N'Rhubarb.png'),
                        (N'Неочищенный Рис', 40, 30, 8, 0, NULL, 1, 2, 0.000, N'Unmilled_Rice.png'),
                        (N'Синий Яркоцвет', 30, 50, 7, 0, NULL, 1, 1, 0.000, N'Blue_Jazz.png'),
                        (N'Тюльпан', 20, 30, 6, 0, NULL, 1, 1, 0.000, N'Tulip.png'),
                        (N'Черника', 80, 50, 13, 1, 4, 3, 6, 0.020, N'Blueberry.png'),
                        (N'Дыня', 80, 250, 12, 0, NULL, 1, 1, 0.000, N'Melon.png'),
                        (N'Помидор', 50, 60, 11, 1, 4, 1, 2, 0.050, N'Tomato.png'),
                        (N'Карамбола', 400, 750, 13, 0, NULL, 1, 1, 0.000, N'Starfruit.png'),
                        (N'Острый Перец', 40, 40, 5, 1, 3, 1, 2, 0.030, N'Hot_Pepper.png'),
                        (N'Редис', 40, 90, 6, 0, NULL, 1, 1, 0.000, N'Radish.png'),
                        (N'Краснокочанная Капуста', 100, 260, 9, 0, NULL, 1, 1, 0.000, N'Red_Cabbage.png'),
                        (N'Кукуруза', 150, 50, 14, 1, 4, 1, 1, 0.000, N'Corn.png'),
                        (N'Хмель', 60, 25, 11, 1, 1, 1, 1, 0.000, N'Hops.png'),
                        (N'Летний Блеск', 50, 90, 8, 0, NULL, 1, 1, 0.000, N'Summer_Spangle.png'),
                        (N'Мак', 100, 140, 7, 0, NULL, 1, 1, 0.000, N'Poppy.png'),
                        (N'Пшеница', 10, 25, 4, 0, NULL, 1, 1, 0.000, N'Wheat.png'),
                        (N'Подсолнух', 200, 80, 8, 0, NULL, 1, 1, 0.000, N'Sunflower.png'),
                        (N'Клюква', 240, 75, 7, 1, 5, 2, 4, 0.100, N'Cranberries.png'),
                        (N'Тыква', 100, 320, 13, 0, NULL, 1, 1, 0.000, N'Pumpkin.png'),
                        (N'Виноград', 60, 80, 10, 1, 3, 1, 1, 0.000, N'Grape.png'),
                        (N'Баклажан', 20, 60, 5, 1, 5, 1, 2, 0.002, N'Eggplant.png'),
                        (N'Амарант', 70, 150, 7, 0, NULL, 1, 1, 0.000, N'Amaranth.png'),
                        (N'Бок-чой', 50, 80, 4, 0, NULL, 1, 1, 0.000, N'Bok_Choy.png'),
                        (N'Батат', 60, 160, 10, 0, NULL, 1, 1, 0.000, N'Yam.png'),
                        (N'Артишок', 30, 160, 8, 0, NULL, 1, 1, 0.000, N'Artichoke.png'),
                        (N'Свекла', 20, 100, 6, 0, NULL, 1, 1, 0.000, N'Beet.png'),
                        (N'Сказочная Роза', 200, 290, 12, 0, NULL, 1, 1, 0.000, N'Fairy_Rose.png'),
                        (N'Колокольчик', 50, 100, 7, 0, NULL, 1, 1, 0.000, N'1.png');
                    END;

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
                    WHERE c.Name IN (N'Пастернак', N'Картофель', N'Цветная Капуста', N'Клубника', N'Зелёная Фасоль', N'Чеснок', N'Кудрявая Капуста', N'Ревень', N'Неочищенный Рис', N'Синий Яркоцвет', N'Тюльпан')
                      AND NOT EXISTS (SELECT 1 FROM dbo.Crop_Season cs WHERE cs.CropId = c.Id AND cs.SeasonID = s.Id);

                    INSERT INTO dbo.Crop_Season (CropId, SeasonID)
                    SELECT c.Id, s.Id
                    FROM dbo.Crops c
                    INNER JOIN dbo.Seasons s ON RTRIM(s.Season_Name) = N'Summer'
                    WHERE c.Name IN (N'Черника', N'Дыня', N'Помидор', N'Карамбола', N'Острый Перец', N'Редис', N'Краснокочанная Капуста', N'Кукуруза', N'Хмель', N'Летний Блеск', N'Мак', N'Пшеница', N'Подсолнух')
                      AND NOT EXISTS (SELECT 1 FROM dbo.Crop_Season cs WHERE cs.CropId = c.Id AND cs.SeasonID = s.Id);

                    INSERT INTO dbo.Crop_Season (CropId, SeasonID)
                    SELECT c.Id, s.Id
                    FROM dbo.Crops c
                    INNER JOIN dbo.Seasons s ON RTRIM(s.Season_Name) = N'Autumn'
                    WHERE c.Name IN (N'Кукуруза', N'Пшеница', N'Подсолнух', N'Клюква', N'Тыква', N'Виноград', N'Баклажан', N'Амарант', N'Бок-чой', N'Батат', N'Артишок', N'Свекла', N'Сказочная Роза', N'Колокольчик')
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

                    IF NOT EXISTS (SELECT 1 FROM dbo.Ingredients)
                        INSERT INTO dbo.Ingredients (Name, Sell_Price) VALUES
                        (N'Древесный сок', 1),
                        (N'Рыба', 100),
                        (N'Иридиевый слиток', 1000),
                        (N'Сосновая смола', 100),
                        (N'Мох', 5),
                        (N'Дубовая смола', 150),
                        (N'Кусочек кости', 12),
                        (N'Радиоактивная руда', 300),
                        (N'Солнечная эссенция', 40);

                    /* ---------- 5. Удобрения ---------- */
                    IF OBJECT_ID(N'dbo.Fertilizers', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.Fertilizers (
                            Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Fertilizers PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            Price INT NULL,
                            Growth_Multiplier DECIMAL(3,2) NOT NULL CONSTRAINT DF_Fertilizers_Growth_Multiplier DEFAULT (0.00),
                            CONSTRAINT UQ_Fertilizers_Name UNIQUE (Name),
                            CONSTRAINT CHK_Fertilizers_Growth_Multiplier CHECK (Growth_Multiplier >= 0 AND Growth_Multiplier < 1),
                            CONSTRAINT CHK_Fertilizers_Price CHECK (Price IS NULL OR Price >= 0)
                        );
                    END;

                    /* Growth_Multiplier — доля ускорения до первого урожая: 0.10 = на 10% быстрее. */
                    IF NOT EXISTS (SELECT 1 FROM dbo.Fertilizers)
                        INSERT INTO dbo.Fertilizers (Name, Price, Growth_Multiplier) VALUES
                        (N'Без удобрений', 0, 0.00),
                        (N'Обычное удобрение', 100, 0.00),
                        (N'Высококачественное удобрение', 150, 0.00),
                        (N'Удобрение Делюкс', 0, 0.00),
                        (N'СкороРост', 100, 0.10),
                        (N'СкороРост Делюкс', 150, 0.25),
                        (N'Гипер СкороРост', 0, 0.33);

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

                    IF NOT EXISTS (SELECT 1 FROM dbo.Fertilizer_Recipes)
                    BEGIN
                        INSERT INTO dbo.Fertilizer_Recipes (FertilizerId, IngredientId, Quantity)
                        SELECT f.Id, i.Id, x.Quantity
                        FROM (VALUES
                            (N'Обычное удобрение', N'Древесный сок', 2),
                            (N'Высококачественное удобрение', N'Древесный сок', 2),
                            (N'Высококачественное удобрение', N'Рыба', 1),
                            (N'Удобрение Делюкс', N'Древесный сок', 40),
                            (N'Удобрение Делюкс', N'Иридиевый слиток', 1),
                            (N'СкороРост', N'Сосновая смола', 1),
                            (N'СкороРост', N'Мох', 5),
                            (N'СкороРост Делюкс', N'Дубовая смола', 1),
                            (N'СкороРост Делюкс', N'Кусочек кости', 5),
                            (N'Гипер СкороРост', N'Кусочек кости', 3),
                            (N'Гипер СкороРост', N'Радиоактивная руда', 1),
                            (N'Гипер СкороРост', N'Солнечная эссенция', 1)
                        ) AS x(FertilizerName, IngredientName, Quantity)
                        INNER JOIN dbo.Fertilizers f ON f.Name = x.FertilizerName
                        INNER JOIN dbo.Ingredients i ON i.Name = x.IngredientName;
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
                        (N'Normal', N'Обычное', 1.00),
                        (N'Silver', N'Серябряное', 1.25),
                        (N'Gold', N'Золотое', 1.50),
                        (N'Iridium', N'Иридиевое', 2.00);

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

                    IF NOT EXISTS (SELECT 1 FROM dbo.Quality_Chances)
                    BEGIN
                        INSERT INTO dbo.Quality_Chances (FertilizerId, QualityId, Chance)
                        SELECT f.Id, q.Id, x.Chance
                        FROM (VALUES
                            (N'Без удобрений', N'Normal', CAST(0.9700 AS DECIMAL(5,4))),
                            (N'Без удобрений', N'Silver', CAST(0.0200 AS DECIMAL(5,4))),
                            (N'Без удобрений', N'Gold', CAST(0.0100 AS DECIMAL(5,4))),
                            (N'Без удобрений', N'Iridium', CAST(0.0000 AS DECIMAL(5,4))),
                            (N'Обычное удобрение', N'Normal', CAST(0.8800 AS DECIMAL(5,4))),
                            (N'Обычное удобрение', N'Silver', CAST(0.0800 AS DECIMAL(5,4))),
                            (N'Обычное удобрение', N'Gold', CAST(0.0400 AS DECIMAL(5,4))),
                            (N'Обычное удобрение', N'Iridium', CAST(0.0000 AS DECIMAL(5,4))),
                            (N'Высококачественное удобрение', N'Normal', CAST(0.7800 AS DECIMAL(5,4))),
                            (N'Высококачественное удобрение', N'Silver', CAST(0.1400 AS DECIMAL(5,4))),
                            (N'Высококачественное удобрение', N'Gold', CAST(0.0800 AS DECIMAL(5,4))),
                            (N'Высококачественное удобрение', N'Iridium', CAST(0.0000 AS DECIMAL(5,4))),
                            (N'Удобрение Делюкс', N'Normal', CAST(0.0000 AS DECIMAL(5,4))),
                            (N'Удобрение Делюкс', N'Silver', CAST(0.8400 AS DECIMAL(5,4))),
                            (N'Удобрение Делюкс', N'Gold', CAST(0.1000 AS DECIMAL(5,4))),
                            (N'Удобрение Делюкс', N'Iridium', CAST(0.0600 AS DECIMAL(5,4))),
                            (N'СкороРост', N'Normal', CAST(0.9700 AS DECIMAL(5,4))),
                            (N'СкороРост', N'Silver', CAST(0.0200 AS DECIMAL(5,4))),
                            (N'СкороРост', N'Gold', CAST(0.0100 AS DECIMAL(5,4))),
                            (N'СкороРост', N'Iridium', CAST(0.0000 AS DECIMAL(5,4))),
                            (N'СкороРост Делюкс', N'Normal', CAST(0.9700 AS DECIMAL(5,4))),
                            (N'СкороРост Делюкс', N'Silver', CAST(0.0200 AS DECIMAL(5,4))),
                            (N'СкороРост Делюкс', N'Gold', CAST(0.0100 AS DECIMAL(5,4))),
                            (N'СкороРост Делюкс', N'Iridium', CAST(0.0000 AS DECIMAL(5,4))),
                            (N'Гипер СкороРост', N'Normal', CAST(0.9700 AS DECIMAL(5,4))),
                            (N'Гипер СкороРост', N'Silver', CAST(0.0200 AS DECIMAL(5,4))),
                            (N'Гипер СкороРост', N'Gold', CAST(0.0100 AS DECIMAL(5,4))),
                            (N'Гипер СкороРост', N'Iridium', CAST(0.0000 AS DECIMAL(5,4)))
                        ) AS x(FertilizerName, QualityCode, Chance)
                        INNER JOIN dbo.Fertilizers f ON f.Name = x.FertilizerName
                        INNER JOIN dbo.Qualities q ON q.Code = x.QualityCode;
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
