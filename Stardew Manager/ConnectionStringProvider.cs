using System.Configuration;

namespace Stardew_Manager
{
    /// <summary>
    /// Единая точка получения строки подключения: App.config ("StardewDb"), иначе — значение по умолчанию.
    /// </summary>
    public static class ConnectionStringProvider
    {
        private const string DefaultConnectionString =
            @"Data Source=localhost\MSSQLSERVER02;Initial Catalog=Stardew Manager;Integrated Security=True;" +
            "Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Command Timeout=30;";

        public static string Get()
        {
            ConnectionStringSettings? settings = ConfigurationManager.ConnectionStrings["StardewDb"];
            if (settings != null && !string.IsNullOrWhiteSpace(settings.ConnectionString))
                return settings.ConnectionString;

            return DefaultConnectionString;
        }
    }
}
