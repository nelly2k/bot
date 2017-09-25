using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace bot.kraken
{
    public class DatabaseService
    {

        private string connectionString = "Server=(local);Database=bot;Trusted_Connection=True;";

        public async Task Save(List<Trade> trades)
        {
            using (var con = new SqlConnection(connectionString))
            {
                var commandText = @"INSERT INTO [dbo].[Trades]
           ([altname]
           ,[price]
           ,[volume]
           ,[tradeTime]
           ,[buy_sell]
           ,[market_limit]
           ,[misc])
     VALUES
           (@altname
           ,@price
           ,@volume
           ,@tradeTime
           ,@buy_sell
           ,@market_limit
           ,@misc)";
                con.Open();
                foreach (var trade in trades)
                {
                    using (var command = new SqlCommand(commandText, con))
                    {
                        command.Parameters.AddWithValue("@altname", trade.PairName);
                        command.Parameters.AddWithValue("@price", trade.Price);
                        command.Parameters.AddWithValue("@volume", trade.Volume);
                        command.Parameters.AddWithValue("@tradeTime", trade.DateTime);
                        command.Parameters.AddWithValue("@buy_sell", trade.TransactionType== TransactionType.Buy?"b":"s");
                        command.Parameters.AddWithValue("@market_limit", trade.PriceType== PriceType.Limit?"l":"m");
                        command.Parameters.AddWithValue("@misc", trade.Misc);
                        await command.ExecuteNonQueryAsync();
                    }
                }
               con.Close();
            }
            
        }


        public async Task<string> GetId(string altname)
        {

            using (var con = new SqlConnection(connectionString))
            {
                var commandText = @"select top 1 id from lastid where altname = @altname order by time desc";
                con.Open();

                using (var command = new SqlCommand(commandText, con))
                {
                    command.Parameters.AddWithValue("@altname", altname);

                    var result =  await command.ExecuteScalarAsync();
                    con.Close();
                    return result?.ToString();
                }
              
            }
    }

        public async Task SaveLastId(string altname, string lastId)
        {
            using (var con = new SqlConnection(connectionString))
            {
                var commandText = @"INSERT INTO [dbo].[lastid]
           ([altname]
           ,[id])
     VALUES
           (@altname
           ,@id)
";
                con.Open();
               
                    using (var command = new SqlCommand(commandText, con))
                    {
                        command.Parameters.AddWithValue("@altname", altname);
                        command.Parameters.AddWithValue("@id", lastId);
                      
                        await command.ExecuteNonQueryAsync();
                    }
               
                con.Close();
            }

        }
    }
}
