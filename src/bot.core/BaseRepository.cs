using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace bot.core
{
    public class BaseRepository
    {
        protected string ConnectionString { get; set; }

        public BaseRepository()
        {
            ConnectionString = ConfigurationManager.AppSettings["db"];
        }

        protected async Task Execute(Func<SqlCommand, Task> command)
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                using (var cms = new SqlCommand())
                {
                    cms.Connection = con;
                    con.Open();
                    await command(cms);
                    con.Close();
                }
            }
        }
    }
}