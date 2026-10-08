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
          Application.Run(new Form1());
           
        }
    }
}1