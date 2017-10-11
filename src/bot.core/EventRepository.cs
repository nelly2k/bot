using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface IEventRepository:IService
    {
        Task UpdateLastEvent(string platform, string eventName, string value);
        Task<string> GetLastEventValue(string platform, string eventName);
    }

    public class EventRepository : IEventRepository
    {
        private readonly string _connectionString;

        public EventRepository()
        {
            _connectionString = ConfigurationManager.AppSettings["db"];
        }

        public async Task UpdateLastEvent(string platform, string eventName, string value)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                var text = @"
                if exists (select * from lastEvent where name = @name and platform=@platform)
                begin
                   update lastEvent set datetime=getdate(), value=@value where name=@name and platform=@platform
                end
                else
                begin
                 insert into lastEvent(platform, name, datetime, value) VALUES(@platform, @name, getdate(), @value)
                end";
                con.Open();
                using (var com = new SqlCommand(text, con))
                {
                    com.Parameters.AddWithValue("@name", eventName);
                    com.Parameters.AddWithValue("@value", value);
                    com.Parameters.AddWithValue("@platform", platform);
                    await com.ExecuteNonQueryAsync();
                    con.Close();
                }
            }
        }

        public async Task<string> GetLastEventValue(string platform, string eventName)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                var text = @"select value from lastEvent where name=@eventName and platform=@platform";
                con.Open();
                using (var com = new SqlCommand(text, con))
                {
                    com.Parameters.AddWithValue("@name", eventName);
                    com.Parameters.AddWithValue("@platform", platform);
                    try
                    {
                        var result = await com.ExecuteScalarAsync();
                        return result?.ToString();
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
        }

    }
}