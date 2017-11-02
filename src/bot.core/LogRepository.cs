using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface ILogRepository:IService
    {
        Task Log(string platform, string status, string what);
    }

    public class LogRepository:BaseRepository, ILogRepository
    {
        public async Task Log(string platform, string status, string what)
        {
            if (string.IsNullOrEmpty(what))
            {
                return;
            }
            await Execute(async cmd =>
            {
                cmd.CommandText = @"INSERT INTO [dbo].[log]
                   ([platform]
                   ,[status]
                   ,[event])
             VALUES (@platform,@status,@event)";

                cmd.Parameters.AddWithValue("@platform", "kraken");
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@event", what);
                await cmd.ExecuteNonQueryAsync();
            });
        }
    }
}