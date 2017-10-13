using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace bot.core
{
    public class BaseRepository
    {
        protected string ConnectionString { get; set; }

        public BaseRepository()
        {
            ConnectionString = ConfigurationManager.AppSettings["db"];
        }

        protected void Get(string query, Dictionary<string, object> parameters, Action<SqlDataReader> readerAction)
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                using (var cmd = new SqlCommand(query,con))
                {
                    con.Open();
                }
            }
        }
    }
}