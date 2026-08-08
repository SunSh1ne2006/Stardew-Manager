using Microsoft.Data.SqlClient;
using System.Configuration;

namespace Stardew_Manager
{
    internal static class Program
    {

        [STAThread]
        static void Main()
        {     
          ApplicationConfiguration.Initialize();

            SqlConnection cn = new SqlConnection();

            cn.ConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                ?? @"Data Source=localhost\MSSQLSERVER02;Initial Catalog=Stardew Manager;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Command Timeout=0;";

          Application.Run(new Form1());
           
        }
    }
}