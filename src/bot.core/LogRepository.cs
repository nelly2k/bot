using System.Threading.Tasks;

namespace bot.core
{
    public interface ILogRepository
    {
        Task Log(string platform, string status, string what);
    }

    public class LogRepository:BaseRepository, ILogRepository
    {
        public async Task Log(string platform, string status, string what)
        {
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